using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using OtoRehber.Domain.Advisory;
using OtoRehber.Domain.Ai;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Models;
using OtoRehber.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OtoRehber.Controllers
{
    public class CompareController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IAiCarDataService _aiService;
        private readonly IMemoryCache _cache;
        private readonly CarScoreService _scores;
        private readonly IConfiguration _configuration;

        public CompareController(OtoRehberDbContext context, IAiCarDataService aiService, IMemoryCache cache,
            CarScoreService scores, IConfiguration configuration)
        {
            _context = context;
            _aiService = aiService;
            _cache = cache;
            _scores = scores;
            _configuration = configuration;
        }

        // GET: /Compare
        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars.OrderBy(c => c.Brand).ThenBy(c => c.ModelName).ToListAsync();
            return View(cars);
        }

        // GET: /Compare/Result?car1Id=1&car2Id=2
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "car1Id", "car2Id" })]
        public async Task<IActionResult> Result(int car1Id, int car2Id)
        {
            if (car1Id == car2Id)
            {
                TempData["ErrorMessage"] = "Lütfen karşılaştırmak için iki farklı araç seçin.";
                return RedirectToAction(nameof(Index));
            }

            var cars = await _context.Cars
                .AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.ProsConsList)
                .Include(c => c.ChronicIssues)
                .Include(c => c.MileageMilestones)
                .Where(c => c.Id == car1Id || c.Id == car2Id)
                .ToListAsync();

            var car1 = cars.FirstOrDefault(c => c.Id == car1Id);
            var car2 = cars.FirstOrDefault(c => c.Id == car2Id);

            if (car1 == null || car2 == null)
            {
                TempData["ErrorMessage"] = "Seçilen araçlardan biri veya ikisi bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Canonical OtoRehber Skoru (PRD v5 §1.2) + kazananı BACKEND belirler (§4.5).
            var scoreMap = await _scores.ForCarsAsync(new[] { car1, car2 });
            var score1 = scoreMap[car1.Id];
            var score2 = scoreMap[car2.Id];
            var winner = ComparisonVerdict.Decide(score1, score2);
            ViewBag.Score1 = score1;
            ViewBag.Score2 = score2;
            ViewBag.Winner = winner;

            var cur = new CurrencyContext
            {
                EurToTry = _configuration.GetValue<double?>("Currency:EurToTry"),
                RateDate = _configuration["Currency:RateDate"]
            };
            var ctx1 = AiContextBuilder.ForVehicle(car1, score1, cur);
            var ctx2 = AiContextBuilder.ForVehicle(car2, score2, cur);
            var issueRefs = new HashSet<string>(ctx1.IssueRefs); issueRefs.UnionWith(ctx2.IssueRefs);
            var maintRefs = new HashSet<string>(ctx1.MaintenanceRefs); maintRefs.UnionWith(ctx2.MaintenanceRefs);

            // AI açıklaması: aynı ikili için tekrar çağırma (6 saat cache). Hata mesajları cache'lenmez.
            var cacheKey = $"compare-verdict:{car1Id}-{car2Id}:{(int)winner}";
            if (!_cache.TryGetValue(cacheKey, out string? verdict) || string.IsNullOrEmpty(verdict))
            {
                var explanation = await _aiService.ExplainComparisonAsync(
                    ctx1.Text + "\n" + ctx2.Text, winner,
                    $"{car1.Brand} {car1.ModelName}", $"{car2.Brand} {car2.ModelName}",
                    issueRefs, maintRefs);
                verdict = explanation is { Ok: true } && !string.IsNullOrWhiteSpace(explanation.Summary)
                    ? explanation.Summary
                    : (explanation?.ErrorMessage ?? "AI yorumu şu anda üretilemedi.");
                if (explanation is { Ok: true } && verdict.Length > 150)
                    _cache.Set(cacheKey, verdict, TimeSpan.FromHours(6));
            }

            var viewModel = new CarCompareViewModel
            {
                Car1 = car1,
                Car2 = car2,
                AiVerdict = verdict
            };

            return View(viewModel);
        }
    }
}
