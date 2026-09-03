using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OtoRehber.Domain.Advisory;
using OtoRehber.Domain.Ai;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Domain.Mappings;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OtoRehber.Controllers
{
    [AllowAnonymous]
    [EnableRateLimiting("ai")]
    public class AiWizardController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IAiCarDataService _aiService;
        private readonly CarScoreService _scores;
        private readonly IConfiguration _configuration;

        public AiWizardController(OtoRehberDbContext context, IAiCarDataService aiService,
            CarScoreService scores, IConfiguration configuration)
        {
            _context = context;
            _aiService = aiService;
            _scores = scores;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index() => View();

        // PRD v5 §4.3: Kullanıcı kriterleri → Backend filtreleme → Backend ranking → İlk 3 aday → AI açıklaması.
        [HttpPost]
        public async Task<IActionResult> Analyze(
            long? budgetMin, long? budgetMax,
            string? bodyType, string? transmission, string? fuel,
            string? familySize, string? usageType,
            string[]? priorities, string? notes)
        {
            if (budgetMin is null or <= 0 && budgetMax is null or <= 0)
            {
                ViewBag.Error = "Lütfen en azından bir bütçe aralığı girin.";
                return View("Index");
            }
            if (budgetMin is > 0 && budgetMax is > 0 && budgetMin > budgetMax)
                (budgetMin, budgetMax) = (budgetMax, budgetMin);

            static string Clip(string? s, int max) => (s ?? "").Trim() is var t && t.Length > max ? t[..max] : t;
            var prefs = new WizardPreferences
            {
                BudgetMin = budgetMin,
                BudgetMax = budgetMax,
                BodyType = Clip(bodyType, 40),
                Transmission = Clip(transmission, 40),
                Fuel = Clip(fuel, 40),
                FamilySize = Clip(familySize, 60),
                UsageType = Clip(usageType, 80),
                Notes = Clip(notes, 400),
                Priorities = (priorities ?? Array.Empty<string>())
                    .Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => Clip(p, 40)).Take(8).ToList()
            };

            // 1) Tüm araçlar + canonical skor (rule engine bunu kullanır — AI değil).
            var allCars = await _context.Cars.AsNoTracking().ToListAsync();
            var scoreMap = await _scores.ForCarsAsync(allCars);

            // 2-3) Backend rule engine: katı filtre (bütçe+yakıt+vites+kasa) → canonical rank → diversity → 3 aday.
            var result = WizardRuleEngine.Evaluate(
                allCars, prefs, c => scoreMap[c.Id].Overall,
                maxCandidates: 3, maxSameMainModel: _scores.MaxSameMainModel);

            var cur = new CurrencyContext
            {
                EurToTry = _configuration.GetValue<double?>("Currency:EurToTry"),
                RateDate = _configuration["Currency:RateDate"]
            };

            // 4) Adayların tam bağlamı (kronik sorun + km barajları dahil) — §4.7.
            var candidateIds = result.Candidates.Select(c => c.Car.Id).ToList();
            var candidateEntities = candidateIds.Count == 0
                ? new List<Domain.Entities.Car>()
                : await _context.Cars.AsNoTracking()
                    .Include(c => c.ChronicIssues).Include(c => c.MileageMilestones)
                    .Where(c => candidateIds.Contains(c.Id)).ToListAsync();

            var ordered = result.Candidates
                .Select(c => (Car: candidateEntities.First(e => e.Id == c.Car.Id), c.Rank, Score: scoreMap[c.Car.Id]))
                .ToList();

            AiExplanation explanation;
            if (ordered.Count == 0)
            {
                explanation = new AiExplanation { Ok = true, Summary = "" };
            }
            else
            {
                var ctx = AiContextBuilder.ForCandidates(ordered, cur);
                explanation = await _aiService.ExplainWizardCandidatesAsync(
                    ctx.Text, AiContextBuilder.ForPreferences(prefs), ctx.IssueRefs, ctx.MaintenanceRefs);
            }

            // View modeli: kartlar backend'den; AI yalnızca açıklama.
            ViewBag.Candidates = result.Candidates
                .Select(c => new
                {
                    Dto = candidateEntities.First(e => e.Id == c.Car.Id).ToListDto(),
                    c.Rank,
                    Score = OtoRehber.Domain.Scoring.OtoRehberScore.RoundForDisplay(c.Score)
                }).ToList();
            ViewBag.NearMisses = result.NearMisses
                .Select(n => new
                {
                    Label = $"{n.Car.Brand} {n.Car.ModelName}",
                    n.Car.Id,
                    Reasons = n.Reasons.ToList()
                }).ToList();
            ViewBag.TotalPassed = result.TotalPassed;
            ViewBag.Explanation = explanation;
            ViewBag.Preferences = prefs;
            return View("Result");
        }
    }
}
