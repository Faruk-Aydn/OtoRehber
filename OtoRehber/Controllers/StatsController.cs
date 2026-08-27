using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Controllers
{
    public class StatsController : Controller
    {
        private readonly OtoRehberDbContext _context;

        public StatsController(OtoRehberDbContext context)
        {
            _context = context;
        }

        [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Index()
        {
            // Araçları belleğe al; gruplama/yuvarlama SQL'e çevrilmeye çalışılmasın
            // (Postgres'te ROUND(double precision, int) yok → 500).
            var cars = await _context.Cars.AsNoTracking().ToListAsync();

            ViewBag.TotalCars = cars.Count;
            ViewBag.TotalReviews = await _context.CarReviews.CountAsync();
            ViewBag.TotalGarage = await _context.UserGarages.CountAsync();
            ViewBag.AvgScore = cars.Count > 0 ? Math.Round(cars.Average(c => c.ReliabilityScore), 2) : 0;

            var segmentData = cars
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Segment) ? "Belirsiz" : c.Segment)
                .Select(g => new { Segment = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            ViewBag.SegmentLabels = segmentData.Select(s => s.Segment).ToList();
            ViewBag.SegmentCounts = segmentData.Select(s => s.Count).ToList();

            var brandScores = cars
                .GroupBy(c => string.IsNullOrWhiteSpace(c.Brand) ? "Belirsiz" : c.Brand)
                .Select(g => new
                {
                    Brand = g.Key,
                    AvgScore = Math.Round(g.Average(c => c.ReliabilityScore), 1),
                    CarCount = g.Count(),
                    AvgCost = g.Average(c => c.EstimatedMaintenanceCostEUR)
                })
                .OrderByDescending(x => x.AvgScore)
                .Take(15)
                .ToList();
            ViewBag.BrandLabels = brandScores.Select(b => b.Brand).ToList();
            ViewBag.BrandAvgScores = brandScores.Select(b => b.AvgScore).ToList();
            ViewBag.BrandCarCounts = brandScores.Select(b => b.CarCount).ToList();
            ViewBag.BrandAvgCosts = brandScores.Select(b => (int)b.AvgCost).ToList();

            ViewBag.Top5Cars = cars
                .OrderByDescending(c => c.ReliabilityScore)
                .Take(5)
                .Select(c => new { Name = c.Brand + " " + c.ModelName, Score = c.ReliabilityScore, Segment = c.Segment })
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
