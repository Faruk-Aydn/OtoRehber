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

        public CatalogController(OtoRehberDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: /araclar  — filtre + sıralama + sayfalama
        [HttpGet("/araclar")]
        public async Task<IActionResult> Index(string? searchQuery, string? segment, string? brand, string? sortBy, int page = 1)
        {
            const int pageSize = 12;

            var query = CarCatalogQuery.ApplyFilters(_context.Cars.AsNoTracking(), searchQuery, segment, brand);
            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var cars = await CarCatalogQuery.ApplySort(query, sortBy)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            await FillRatingsAsync(cars.Select(c => c.Id).ToList());

            ViewBag.Brands = await _cache.GetOrCreateAsync(HomeController.CacheKeyBrands, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.Cars.AsNoTracking().Select(c => c.Brand).Distinct().OrderBy(b => b).ToListAsync();
            });
            ViewBag.Segments = await _cache.GetOrCreateAsync("catalog:segments", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _context.Cars.AsNoTracking()
                    .Where(c => c.Segment != null && c.Segment != "")
                    .Select(c => c.Segment).Distinct().OrderBy(s => s).ToListAsync();
            });

            ViewData["Title"] = "Tüm Araçlar";
            ViewData["Description"] = "OtoRehber'deki tüm araçları markaya, segmente ve fiyata göre filtreleyin; güvenilirlik puanları ve kullanıcı yorumlarıyla karşılaştırın.";
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewData["CurrentSearch"] = searchQuery;
            ViewData["CurrentSegment"] = segment;
            ViewData["CurrentBrand"] = brand;
            ViewData["CurrentSort"] = sortBy;
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

            var cars = await _context.Cars.AsNoTracking()
                .Where(c => c.Brand == brand)
                .OrderByDescending(c => c.ReliabilityScore)
                .ToListAsync();

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

            var cars = await _context.Cars.AsNoTracking()
                .Where(c => c.Segment == segment)
                .OrderByDescending(c => c.ReliabilityScore)
                .ToListAsync();

            await FillRatingsAsync(cars.Select(c => c.Id).ToList());

            ViewData["Title"] = $"{segment} Segmenti Araçlar";
            ViewData["Description"] = $"{segment} segmentindeki araçların karşılaştırması: güvenilirlik, fiyat aralığı, kronik sorunlar ve kullanıcı puanları.";
            ViewBag.Heading = $"{segment} Segmenti Araçlar";
            ViewBag.Kind = "segment";
            return View("List", cars.ToListDto());
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
