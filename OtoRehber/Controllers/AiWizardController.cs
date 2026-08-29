using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Domain.Mappings;
using OtoRehber.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtoRehber.Controllers
{
    [AllowAnonymous]
    [EnableRateLimiting("ai")]
    public class AiWizardController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IAiCarDataService _aiService;

        public AiWizardController(OtoRehberDbContext context, IAiCarDataService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        [HttpGet]
        public IActionResult Index() => View();

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

            // Girdi temizliği (prompt injection / maliyet)
            static string Clip(string? s, int max) => (s ?? "").Trim() is var t && t.Length > max ? t[..max] : t;
            bodyType = Clip(bodyType, 40);
            transmission = Clip(transmission, 40);
            fuel = Clip(fuel, 40);
            familySize = Clip(familySize, 60);
            usageType = Clip(usageType, 80);
            notes = Clip(notes, 400);
            var priorityList = (priorities ?? Array.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => Clip(p, 40)).Take(8).ToList();

            var cars = await _context.Cars.AsNoTracking()
                .Select(c => new { c.Id, c.Brand, c.ModelName, c.Segment, c.ProductionYears, c.MinPrice, c.MaxPrice, c.ReliabilityScore })
                .ToListAsync();

            var ctx = new StringBuilder();
            ctx.AppendLine("Veritabanındaki araçlar:");
            foreach (var c in cars)
                ctx.AppendLine($"- {c.Brand} {c.ModelName} ({c.ProductionYears}) | Segment: {c.Segment} | Fiyat: {c.MinPrice:N0}-{c.MaxPrice:N0} TL | Güvenilirlik: {c.ReliabilityScore}/10");

            // Yapılandırılmış profil
            var p = new StringBuilder();
            string Budget()
            {
                if (budgetMin is > 0 && budgetMax is > 0) return $"{budgetMin:N0} - {budgetMax:N0} TL";
                if (budgetMin is > 0) return $"en az {budgetMin:N0} TL";
                return $"en fazla {budgetMax:N0} TL";
            }
            p.AppendLine($"- Bütçe: {Budget()}");
            if (!string.IsNullOrWhiteSpace(bodyType)) p.AppendLine($"- Tercih edilen kasa tipi: {bodyType}");
            if (!string.IsNullOrWhiteSpace(transmission)) p.AppendLine($"- Vites: {transmission}");
            if (!string.IsNullOrWhiteSpace(fuel)) p.AppendLine($"- Yakıt: {fuel}");
            if (!string.IsNullOrWhiteSpace(familySize)) p.AppendLine($"- Aile durumu: {familySize}");
            if (!string.IsNullOrWhiteSpace(usageType)) p.AppendLine($"- Kullanım: {usageType}");
            if (priorityList.Count > 0) p.AppendLine($"- Öncelikler (önem sırasıyla): {string.Join(", ", priorityList)}");
            if (!string.IsNullOrWhiteSpace(notes)) p.AppendLine($"- Kullanıcının ek notu: {notes}");

            var userPrompt = "Bir araç arıyorum. Profilim:\n" + p;

            var responseText = await _aiService.GetCarRecommendationAsync(userPrompt, ctx.ToString());

            // AI metninde adı geçen veritabanı araçlarını eşleştir (Result'ta kart olarak göster)
            var matchedIds = cars
                .Where(c => responseText.Contains(c.ModelName, StringComparison.OrdinalIgnoreCase)
                            && responseText.Contains(c.Brand, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Id).ToList();

            var matchedCars = matchedIds.Count > 0
                ? (await _context.Cars.AsNoTracking().Where(c => matchedIds.Contains(c.Id)).ToListAsync()).ToListDto()
                : new List<Domain.DTOs.CarListDto>();

            ViewBag.AiResponse = responseText;
            ViewBag.MatchedCars = matchedCars;
            return View("Result");
        }
    }
}
