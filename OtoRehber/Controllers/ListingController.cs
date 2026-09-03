using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using OtoRehber.Domain.Advisory;
using OtoRehber.Domain.Listing;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Controllers
{
    // İlan Analizi (PRD v5 §5) — "Bir ilan buldum, mantıklı mı?"
    public class ListingController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public ListingController(OtoRehberDbContext context, IMemoryCache cache, IConfiguration configuration)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
        }

        [HttpGet("/ilan-analizi")]
        public async Task<IActionResult> Index()
        {
            await FillCarOptionsAsync();
            ViewData["Title"] = "İlan Analizi";
            ViewData["Description"] = "İkinci el bir ilan buldunuz mu? Aracı seçin, fiyat, kilometre, hasar ve kronik sorunlar açısından OtoRehber verileriyle değerlendirelim.";
            return View();
        }

        [HttpPost("/ilan-analizi")]
        public async Task<IActionResult> Analyze(int carId, int? year, int? mileage, long? price,
            string? damage, int? paintedPanels, string? notes)
        {
            var car = await _context.Cars.AsNoTracking()
                .Include(c => c.ChronicIssues)
                .Include(c => c.MileageMilestones)
                .FirstOrDefaultAsync(c => c.Id == carId);

            if (car == null)
            {
                await FillCarOptionsAsync();
                ViewBag.Error = "Lütfen listeden bir araç seçin.";
                return View("Index");
            }

            var input = new ListingInput
            {
                CarId = carId,
                Year = year is > 1950 and < 2100 ? year : null,
                Mileage = mileage is > 0 and < 2_000_000 ? mileage : null,
                Price = price is > 0 ? price : null,
                HasDamageRecord = damage switch { "var" => true, "yok" => false, _ => (bool?)null },
                PaintedPanels = paintedPanels is >= 0 and <= 20 ? paintedPanels : null,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()[..Math.Min(notes.Trim().Length, 500)]
            };

            int annualKm = _configuration.GetValue<int?>("Listing:AnnualKm") ?? ListingAnalyzer.DefaultAnnualKm;
            var result = ListingAnalyzer.Analyze(car, input, DateTime.UtcNow.Year, annualKm);

            ViewBag.Currency = new CurrencyContext
            {
                EurToTry = _configuration.GetValue<double?>("Currency:EurToTry"),
                RateDate = _configuration["Currency:RateDate"]
            };
            ViewBag.Input = input;
            return View("Result", result);
        }

        private async Task FillCarOptionsAsync()
        {
            // Marka bazlı gruplu araç listesi (10 dk cache).
            ViewBag.CarGroups = await _cache.GetOrCreateAsync("listing:caroptions", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                var cars = await _context.Cars.AsNoTracking()
                    .Select(c => new { c.Id, c.Brand, c.ModelName, c.Engine })
                    .OrderBy(c => c.Brand).ThenBy(c => c.ModelName)
                    .ToListAsync();
                return cars
                    .GroupBy(c => c.Brand)
                    .Select(g => new
                    {
                        Brand = g.Key,
                        Cars = g.Select(c => new { c.Id, Label = $"{c.ModelName} — {c.Engine}" }).ToList()
                    })
                    .ToList<dynamic>();
            });
        }
    }
}
