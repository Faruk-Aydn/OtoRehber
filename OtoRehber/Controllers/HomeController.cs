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

        // Araç/yorum eklendiğinde AdminCarController/CarController bu anahtarları temizler.
        public const string CacheKeyBrands = "home:brands";
        public const string CacheKeyLeaderboard = "home:leaderboard";

        public HomeController(OtoRehberDbContext context, ILogger<HomeController> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IActionResult> Index(string searchQuery, string segment, string brand, string sortBy, int page = 1)
        {
            var carsQuery = _context.Cars.AsNoTracking().AsQueryable();

            // Arama (Türkçe kültür bug'ı için ToLowerInvariant; kolon tarafı SQL LOWER'a çevrilir)
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerSearch = searchQuery.Trim().ToLowerInvariant();
                carsQuery = carsQuery.Where(c => c.Brand.ToLower().Contains(lowerSearch) || c.ModelName.ToLower().Contains(lowerSearch));
            }

            // Segment Filtresi
            if (!string.IsNullOrEmpty(segment))
            {
                carsQuery = carsQuery.Where(c => c.Segment == segment);
            }

            // Marka Filtresi
            if (!string.IsNullOrEmpty(brand))
            {
                carsQuery = carsQuery.Where(c => c.Brand == brand);
            }

            // Sıralama
            carsQuery = sortBy switch
            {
                "price_asc" => carsQuery.OrderBy(c => c.MinPrice),
                "price_desc" => carsQuery.OrderByDescending(c => c.MinPrice),
                "score_desc" => carsQuery.OrderByDescending(c => c.ReliabilityScore),
                "score_asc" => carsQuery.OrderBy(c => c.ReliabilityScore),
                _ => carsQuery.OrderByDescending(c => c.Id) // Varsayılan: En son eklenenler
            };

            // Sayfalama (Pagination) — sınır kontrolü
            const int pageSize = 12;
            int totalItems = await carsQuery.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            int skipAmount = (page - 1) * pageSize;
            var cars = await carsQuery.Skip(skipAmount).Take(pageSize).ToListAsync();
            var carDtos = cars.ToListDto();

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

            // View'a parametreleri gönderelim ki filtreler seçili kalsın
            ViewData["CurrentSearch"] = searchQuery;
            ViewData["CurrentSegment"] = segment;
            ViewData["CurrentBrand"] = brand;
            ViewData["CurrentSort"] = sortBy;
            
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
                var topReviewed = (await _context.CarReviews.AsNoTracking()
                    .GroupBy(r => new { r.CarId, r.Car.Brand, r.Car.ModelName })
                    .Select(g => new { Label = g.Key.Brand + " " + g.Key.ModelName, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync()).Cast<object>().ToList();

                var topByScore = (await _context.Cars.AsNoTracking()
                    .OrderByDescending(c => c.ReliabilityScore)
                    .Take(10)
                    .Select(c => new { Label = c.Brand + " " + c.ModelName, Score = c.ReliabilityScore })
                    .ToListAsync()).Cast<object>().ToList();

                return (TopReviewed: topReviewed, TopByScore: topByScore);
            });
            ViewBag.TopReviewed = leaderboard.TopReviewed;
            ViewBag.TopByScore = leaderboard.TopByScore;

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
