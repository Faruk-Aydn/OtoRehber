using System.Diagnostics;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using OtoRehber.Domain.Entities;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Models;
using OtoRehber.Domain.DTOs;
using AutoMapper;

namespace OtoRehber.Controllers
{
    public class HomeController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IMapper _mapper;

        public HomeController(OtoRehberDbContext context, ILogger<HomeController> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        public IActionResult Index(string searchQuery, string segment, string brand, string sortBy, int page = 1)
        {
            var carsQuery = _context.Cars.AsQueryable();

            // Arama
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var lowerSearch = searchQuery.ToLower();
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

            // Sayfalama (Pagination)
            int pageSize = 12; // Her sayfada 12 araç
            int totalItems = carsQuery.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Düzeltme: Skip ve Take parametrelerini açıkça ayırıyoruz
            int skipAmount = (page - 1) * pageSize;
            var cars = carsQuery.Skip(skipAmount).Take(pageSize).ToList();
            var carDtos = _mapper.Map<List<CarListDto>>(cars);

            // View'a parametreleri gönderelim ki filtreler seçili kalsın
            ViewData["CurrentSearch"] = searchQuery;
            ViewData["CurrentSegment"] = segment;
            ViewData["CurrentBrand"] = brand;
            ViewData["CurrentSort"] = sortBy;
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            
            // Dropdown için tüm markaları gönder
            ViewBag.Brands = _context.Cars.Select(c => c.Brand).Distinct().OrderBy(b => b).ToList();

            // --- Dinamik Leaderboard ---
            // En çok yorum yapılan araçlar (gerçek veri)
            ViewBag.TopReviewed = _context.CarReviews
                .GroupBy(r => new { r.CarId, r.Car.Brand, r.Car.ModelName })
                .Select(g => new { Label = g.Key.Brand + " " + g.Key.ModelName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList<dynamic>();

            // En yüksek güvenilirlik puanlı araçlar — Top 10
            ViewBag.TopByScore = _context.Cars
                .OrderByDescending(c => c.ReliabilityScore)
                .Take(10)
                .Select(c => new { Label = c.Brand + " " + c.ModelName, Score = c.ReliabilityScore })
                .ToList<dynamic>();

            return View(carDtos);
        }

        public IActionResult Privacy() => View();

        public IActionResult Kvkk() => View();

        public IActionResult KullanimKosullari() => View();

        public IActionResult Cerez() => View();

        public IActionResult Hakkimizda() => View();

        public IActionResult Iletisim() => View();

        // GEÇİCİ TEŞHİS — antiforgery/DataProtection sorununu çözünce kaldırılacak.
        // 1. çağrı: GET /__diag            -> protect edilmiş string + AF token döner, cookie set eder
        // 2. çağrı: GET /__diag?enc=...     (aynı cookie ile) -> önceki isteğin protect'ini unprotect dener
        //           + header RequestVerificationToken: <rt>   -> AF doğrulamayı dener
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpGet("/__diag")]
        public async Task<IActionResult> Diag(
            [FromServices] IDataProtectionProvider dpProvider,
            [FromServices] IAntiforgery antiforgery,
            string? enc)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("diag v6");
            sb.AppendLine($"scheme={Request.Scheme} isHttps={Request.IsHttps}");
            sb.AppendLine($"xfproto=[{Request.Headers["X-Forwarded-Proto"]}] xff=[{Request.Headers["X-Forwarded-For"]}]");
            sb.AppendLine($"incoming cookies: {string.Join(",", Request.Cookies.Keys)}");

            var prot = dpProvider.CreateProtector("__diag");

            if (!string.IsNullOrEmpty(enc))
            {
                // 2. istek: önceki protect'i çöz
                try { sb.AppendLine($"UNPROTECT (onceki istekten): OK -> {prot.Unprotect(enc)}"); }
                catch (Exception ex) { sb.AppendLine($"UNPROTECT HATA: {ex.GetType().Name}: {ex.Message}"); }

                // AF doğrulama
                try
                {
                    var valid = await antiforgery.IsRequestValidAsync(HttpContext);
                    sb.AppendLine($"AF IsRequestValidAsync: {valid}");
                }
                catch (Exception ex) { sb.AppendLine($"AF validate HATA: {ex.GetType().Name}: {ex.Message}"); }
            }
            else
            {
                // 1. istek: yeni protect + AF token üret
                var e = prot.Protect("diag-payload-123");
                sb.AppendLine($"PROTECT: {e}");
                // ayni istekte unprotect (ring stabil mi?)
                try { sb.AppendLine($"  ayni istekte unprotect: {prot.Unprotect(e)}"); }
                catch (Exception ex) { sb.AppendLine($"  ayni istekte unprotect HATA: {ex.Message}"); }

                var tokens = antiforgery.GetAndStoreTokens(HttpContext);
                sb.AppendLine($"AF requestToken: {tokens.RequestToken}");
            }

            return Content(sb.ToString(), "text/plain; charset=utf-8");
        }

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
