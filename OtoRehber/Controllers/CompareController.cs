using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OtoRehber.Controllers
{
    public class CompareController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IAiCarDataService _aiService;
        private readonly IMemoryCache _cache;

        public CompareController(OtoRehberDbContext context, IAiCarDataService aiService, IMemoryCache cache)
        {
            _context = context;
            _aiService = aiService;
            _cache = cache;
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
                .Where(c => c.Id == car1Id || c.Id == car2Id)
                .ToListAsync();

            var car1 = cars.FirstOrDefault(c => c.Id == car1Id);
            var car2 = cars.FirstOrDefault(c => c.Id == car2Id);

            if (car1 == null || car2 == null)
            {
                TempData["ErrorMessage"] = "Seçilen araçlardan biri veya ikisi bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // AI yorumu: aynı ikili için Gemini'yi tekrar çağırma (6 saat cache).
            // Araç verisi nadiren değişir; hata/kota mesajları cache'lenmez.
            var cacheKey = $"compare-verdict:{car1Id}-{car2Id}";
            if (!_cache.TryGetValue(cacheKey, out string? verdict) || string.IsNullOrEmpty(verdict))
            {
                verdict = await _aiService.GetComparisonVerdictAsync(car1, car2);
                if (!string.IsNullOrWhiteSpace(verdict) && verdict.Length > 150)
                {
                    _cache.Set(cacheKey, verdict, TimeSpan.FromHours(6));
                }
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
