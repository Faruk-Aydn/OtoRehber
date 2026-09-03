using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtoRehber.Domain.Entities;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Models;
using OtoRehber.Domain.DTOs;
using OtoRehber.Domain.Mappings;

namespace OtoRehber.Controllers
{
    public class HomeController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IMemoryCache _cache;
        private readonly OtoRehber.Services.CarScoreService _scores;

        // Araç/yorum eklendiğinde AdminCarController/CarController bu anahtarları temizler.
        public const string CacheKeyBrands = "home:brands";
        public const string CacheKeyLeaderboard = "home:leaderboard";

        public HomeController(OtoRehberDbContext context, ILogger<HomeController> logger, IMemoryCache cache,
            OtoRehber.Services.CarScoreService scores)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _scores = scores;
        }

        public async Task<IActionResult> Index(OtoRehber.Services.CarFilter filter, int page = 1)
        {
            // Filtre /araclar ile ortak; sıralama + sayfalama canonical skora göre (CarScoreService).
            const int pageSize = 12;
            var carsQuery = OtoRehber.Services.CarCatalogQuery.ApplyFilters(_context.Cars.AsNoTracking(), filter);
            var (cars, pageScores, totalItems) = await _scores.SortAndPageAsync(carsQuery, filter.SortBy, filter.MinScore, page, pageSize);
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);
            var carDtos = cars.ToListDto();
            ViewBag.CarScores = pageScores;

            // Bu sayfadaki araçların ortalama kullanıcı puanı (kart rozetinde gösterilir)
            var pageCarIds = carDtos.Select(d => d.Id).ToList();
            var ratingRows = await _context.CarReviews.AsNoTracking()
                .Where(r => pageCarIds.Contains(r.CarId))
                .GroupBy(r => r.CarId)
                .Select(g => new { CarId = g.Key, Sum = g.Sum(r => r.Rating), Count = g.Count() })
                .ToListAsync();
            ViewBag.CarRatings = ratingRows.ToDictionary(
                x => x.CarId,
                x => (Avg: Math.Round((double)x.Sum / x.Count, 1), Count: x.Count));

            // Home hero'da yalnızca arama var; sayfalama linkleri arama + sıralamayı taşır.
            ViewData["CurrentSearch"] = filter.SearchQuery;
            ViewData["CurrentSort"] = filter.SortBy;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            
            // Dropdown için tüm markalar (5 dk cache — nadiren değişir)
            ViewBag.Brands = await _cache.GetOrCreateAsync(CacheKeyBrands, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.Cars.AsNoTracking()
                    .Select(c => c.Brand).Distinct().OrderBy(b => b).ToListAsync();
            });

            // --- Dinamik Leaderboard --- (View'lar IList<dynamic> bekliyor, 5 dk cache)
            var leaderboard = await _cache.GetOrCreateAsync(CacheKeyLeaderboard, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                // "En yüksek OtoRehber Skoru" = Presentation Ranking (PRD v5 §3.2):
                // skor → Canonical Ranking → Diversity/Re-ranking (maxSameMainModel) → Top 10.
                var allCars = await _context.Cars.AsNoTracking().ToListAsync();
                var (topCars, allScores) = await _scores.TopRankedAsync(allCars, 10);
                var topByScore = topCars
                    .Select(c => (object)new
                    {
                        Label = c.Brand + " " + c.ModelName,
                        Score = OtoRehber.Domain.Scoring.OtoRehberScore.RoundForDisplay(allScores[c.Id].Overall)
                    })
                    .ToList();

                // §1.7: yeterli topluluk verisi yoksa "en çok yorum alan" başlığı gösterilmez.
                var reviewCounts = await _context.CarReviews.AsNoTracking()
                    .GroupBy(r => new { r.CarId, r.Car.Brand, r.Car.ModelName })
                    .Select(g => new { g.Key.Brand, g.Key.ModelName, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();
                bool communityLimited = reviewCounts.Count == 0 || reviewCounts[0].Count < _scores.CommunityReviewThreshold;
                var topReviewed = communityLimited
                    ? new List<object>()
                    : reviewCounts.Select(x => (object)new { Label = x.Brand + " " + x.ModelName, x.Count }).ToList();

                return (TopReviewed: topReviewed, TopByScore: topByScore, CommunityLimited: communityLimited);
            });
            ViewBag.TopReviewed = leaderboard.TopReviewed;
            ViewBag.TopByScore = leaderboard.TopByScore;
            ViewBag.CommunityLimited = leaderboard.CommunityLimited;

            return View(carDtos);
        }

        public IActionResult Privacy() => View();

        public IActionResult Kvkk() => View();

        public IActionResult KullanimKosullari() => View();

        public IActionResult Cerez() => View();

        public IActionResult Hakkimizda() => View();

        public IActionResult Iletisim() => View();

        [IgnoreAntiforgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? code)
        {
            var status = code ?? 0;
            Response.StatusCode = status is >= 400 and < 600 ? status : 500;
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = status
            });
        }
    }
}
