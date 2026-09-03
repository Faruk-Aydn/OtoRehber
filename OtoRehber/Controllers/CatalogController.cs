using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtoRehber.Domain.Mappings;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Services;

namespace OtoRehber.Controllers
{
    // /araclar tam katalog + marka/segment landing sayfaları (/marka/{slug}, /segment/{slug}).
    public class CatalogController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly CarScoreService _scores;

        public CatalogController(OtoRehberDbContext context, IMemoryCache cache, CarScoreService scores)
        {
            _context = context;
            _cache = cache;
            _scores = scores;
        }

        // GET: /araclar  — zengin filtre + sıralama + sayfalama
        [HttpGet("/araclar")]
        public async Task<IActionResult> Index(CarFilter filter, int page = 1)
        {
            const int pageSize = 12;

            var query = CarCatalogQuery.ApplyFilters(_context.Cars.AsNoTracking(), filter);
            var (cars, pageScores, total) = await _scores.SortAndPageAsync(query, filter.SortBy, filter.MinScore, page, pageSize);
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);
            ViewBag.CarScores = pageScores;

            await FillRatingsAsync(cars.Select(c => c.Id).ToList());

            ViewBag.Brands = await _cache.GetOrCreateAsync(HomeController.CacheKeyBrands, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.Cars.AsNoTracking().Select(c => c.Brand).Distinct().OrderBy(b => b).ToListAsync();
            });
            ViewBag.Filter = filter;

            ViewData["Title"] = "Tüm Araçlar";
            ViewData["Description"] = "OtoRehber'deki tüm araçları marka, yakıt, vites, kasa tipi, fiyat ve yıla göre filtreleyin; güvenilirlik puanları ve kullanıcı yorumlarıyla karşılaştırın.";
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            return View(cars.ToListDto());
        }

        // "Volkswagen" → "volkswagen", "Alfa Romeo" → "alfa-romeo", Türkçe karakterleri sadeleştirir.
        public static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var s = value.Trim().ToLowerInvariant()
                .Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u')
                .Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c');
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (ch is ' ' or '-' or '_' or '/') sb.Append('-');
            }
            return string.Join('-', sb.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        }

        [HttpGet("/marka/{slug}")]
        public async Task<IActionResult> Brand(string slug)
        {
            var brands = await _context.Cars.AsNoTracking()
                .Select(c => c.Brand).Distinct().ToListAsync();
            var brand = brands.FirstOrDefault(b => Slugify(b) == slug);
            if (brand == null) return NotFound();

            var cars = await OrderByScoreAsync(_context.Cars.AsNoTracking().Where(c => c.Brand == brand));
            await FillRatingsAsync(cars.Select(c => c.Id).ToList());

            ViewData["Title"] = $"{brand} Modelleri";
            ViewData["Description"] = $"{brand} ikinci el modellerinin güvenilirlik puanları, kronik sorunları, bakım maliyetleri ve kullanıcı yorumları — OtoRehber.";
            ViewBag.Heading = $"{brand} Modelleri";
            ViewBag.Kind = "marka";
            return View("List", cars.ToListDto());
        }

        [HttpGet("/segment/{slug}")]
        public async Task<IActionResult> Segment(string slug)
        {
            var segments = await _context.Cars.AsNoTracking()
                .Where(c => c.Segment != null && c.Segment != "")
                .Select(c => c.Segment).Distinct().ToListAsync();
            var segment = segments.FirstOrDefault(s => Slugify(s) == slug);
            if (segment == null) return NotFound();

            var cars = await OrderByScoreAsync(_context.Cars.AsNoTracking().Where(c => c.Segment == segment));
            await FillRatingsAsync(cars.Select(c => c.Id).ToList());

            ViewData["Title"] = $"{segment} Segmenti Araçlar";
            ViewData["Description"] = $"{segment} segmentindeki araçların karşılaştırması: güvenilirlik, fiyat aralığı, kronik sorunlar ve kullanıcı puanları.";
            ViewBag.Heading = $"{segment} Segmenti Araçlar";
            ViewBag.Kind = "segment";
            return View("List", cars.ToListDto());
        }

        // Marka/segment landing — canonical OtoRehber Skoru'na göre sırala (N/A sona), ViewBag.CarScores doldur.
        private async Task<List<OtoRehber.Domain.Entities.Car>> OrderByScoreAsync(IQueryable<OtoRehber.Domain.Entities.Car> q)
        {
            var list = await q.ToListAsync();
            var scores = await _scores.ForCarsAsync(list);
            ViewBag.CarScores = scores;
            return list
                .OrderByDescending(c => scores[c.Id].Overall ?? double.MinValue)
                .ThenByDescending(c => c.Id)
                .ToList();
        }

        private async Task FillRatingsAsync(List<int> carIds)
        {
            var rows = await _context.CarReviews.AsNoTracking()
                .Where(r => carIds.Contains(r.CarId))
                .GroupBy(r => r.CarId)
                .Select(g => new { CarId = g.Key, Sum = g.Sum(r => r.Rating), Count = g.Count() })
                .ToListAsync();
            ViewBag.CarRatings = rows.ToDictionary(
                x => x.CarId,
                x => (Avg: Math.Round((double)x.Sum / x.Count, 1), Count: x.Count));
        }
    }
}
