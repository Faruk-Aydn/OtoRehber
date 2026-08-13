using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Index()
        {
            ViewBag.TotalCars    = _context.Cars.Count();
            ViewBag.TotalReviews = _context.CarReviews.Count();
            ViewBag.TotalGarage  = _context.UserGarages.Count();
            ViewBag.AvgScore     = _context.Cars.Any()
                                    ? Math.Round(_context.Cars.Average(c => c.ReliabilityScore), 2)
                                    : 0;

            var segmentData = _context.Cars
                .GroupBy(c => c.Segment)
                .Select(g => new { Segment = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            ViewBag.SegmentLabels = segmentData.Select(s => s.Segment).ToList();
            ViewBag.SegmentCounts = segmentData.Select(s => s.Count).ToList();

            var brandScores = _context.Cars
                .GroupBy(c => c.Brand)
                .Select(g => new {
                    Brand    = g.Key,
                    AvgScore = Math.Round(g.Average(c => c.ReliabilityScore), 1),
                    CarCount = g.Count(),
                    AvgCost  = g.Average(c => c.EstimatedMaintenanceCostEUR)
                })
                .OrderByDescending(x => x.AvgScore)
                .Take(15)
                .ToList();
            ViewBag.BrandLabels    = brandScores.Select(b => b.Brand).ToList();
            ViewBag.BrandAvgScores = brandScores.Select(b => b.AvgScore).ToList();
            ViewBag.BrandCarCounts = brandScores.Select(b => b.CarCount).ToList();
            ViewBag.BrandAvgCosts  = brandScores.Select(b => (int)b.AvgCost).ToList();

            ViewBag.Top5Cars = _context.Cars
                .OrderByDescending(c => c.ReliabilityScore)
                .Take(5)
                .Select(c => new { Name = c.Brand + " " + c.ModelName, Score = c.ReliabilityScore, Segment = c.Segment })
                .ToList<dynamic>();

            ViewBag.HighCostCars = _context.Cars
                .OrderByDescending(c => c.EstimatedMaintenanceCostEUR)
                .Take(5)
                .Select(c => new { Name = c.Brand + " " + c.ModelName, Cost = c.EstimatedMaintenanceCostEUR, Segment = c.Segment })
                .ToList<dynamic>();

            return View();
        }
    }
}
