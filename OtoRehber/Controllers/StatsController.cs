using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OtoRehber.Domain.Advisory;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Controllers
{
    public class StatsController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly OtoRehber.Services.CarScoreService _scores;
        private readonly IConfiguration _configuration;

        public StatsController(OtoRehberDbContext context, OtoRehber.Services.CarScoreService scores, IConfiguration configuration)
        {
            _context = context;
            _scores = scores;
            _configuration = configuration;
        }

        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Index()
        {
            // Araçları belleğe al; gruplama/yuvarlama SQL'e çevrilmeye çalışılmasın
            // (Postgres'te ROUND(double precision, int) yok → 500).
            var cars = await _context.Cars.AsNoTracking().ToListAsync();

            // Para birimi tutarlılığı (PRD v5.1 §11): Detail/Listing ile aynı € → ₺ dönüşümü.
            ViewBag.Currency = new CurrencyContext
            {
                EurToTry = _configuration.GetValue<double?>("Currency:EurToTry"),
                RateDate = _configuration["Currency:RateDate"]
            };

            // Canonical OtoRehber Skoru (PRD v5 §1.2) — istatistikler ham ReliabilityScore değil bunu kullanır.
            var scores = await _scores.ForCarsAsync(cars);
            double? Overall(int carId) => scores[carId].Overall;
            var scoredCars = cars.Where(c => Overall(c.Id).HasValue).ToList();

            ViewBag.TotalCars = cars.Count;
            ViewBag.TotalReviews = await _context.CarReviews.CountAsync();
            ViewBag.TotalGarage = await _context.UserGarages.CountAsync();
            ViewBag.AvgScore = scoredCars.Count > 0 ? Math.Round(scoredCars.Average(c => Overall(c.Id)!.Value), 2) : 0;

            var segmentData = cars
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Segment) ? "Belirsiz" : c.Segment)
                .Select(g => new { Segment = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            ViewBag.SegmentLabels = segmentData.Select(s => s.Segment).ToList();
            ViewBag.SegmentCounts = segmentData.Select(s => s.Count).ToList();

            var brandScores = cars
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Brand) ? "Belirsiz" : c.Brand)
                .Select(g =>
                {
                    var scored = g.Where(c => Overall(c.Id).HasValue).ToList();
                    return new
                    {
                        Brand = g.Key,
                        AvgScore = scored.Count > 0 ? Math.Round(scored.Average(c => Overall(c.Id)!.Value), 1) : 0d,
                        CarCount = g.Count(),
                        AvgCost = g.Average(c => c.EstimatedMaintenanceCostEUR)
                    };
                })
                .OrderByDescending(x => x.AvgScore)
                .Take(15)
                .ToList();
            ViewBag.BrandLabels = brandScores.Select(b => b.Brand).ToList();
            ViewBag.BrandAvgScores = brandScores.Select(b => b.AvgScore).ToList();
            ViewBag.BrandCarCounts = brandScores.Select(b => b.CarCount).ToList();
            ViewBag.BrandAvgCosts = brandScores.Select(b => (int)b.AvgCost).ToList();

            // Presentation Ranking (PRD v5 §3.2): Canonical → Diversity/Re-ranking → Top 5.
            var top5 = _scores.PresentationRanking(_scores.CanonicalRanking(scoredCars, scores)).Take(5);
            ViewBag.Top5Cars = top5
                .Select(c => new { Name = c.Brand + " " + c.ModelName, Score = OtoRehber.Domain.Scoring.OtoRehberScore.RoundForDisplay(Overall(c.Id)), Segment = c.Segment })
                .ToList<dynamic>();

            ViewBag.HighCostCars = cars
                .OrderByDescending(c => c.EstimatedMaintenanceCostEUR)
                .Take(5)
                .Select(c => new { Name = c.Brand + " " + c.ModelName, Cost = c.EstimatedMaintenanceCostEUR, Segment = c.Segment })
                .ToList<dynamic>();

            return View();
        }
    }
}
