using System.Diagnostics;
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
        public IActionResult Diag(
            [FromServices] IDataProtectionProvider dpProvider,
            [FromServices] Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyManager keyManager)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("diag v7");
            sb.AppendLine($"now(utc)={DateTime.UtcNow:o}");

            // Key ring durumu
            try
            {
                var keys = keyManager.GetAllKeys();
                sb.AppendLine($"key sayisi: {keys.Count}");
                foreach (var k in keys)
                {
                    sb.AppendLine($"  {k.KeyId} create={k.CreationDate:o} activate={k.ActivationDate:o} expire={k.ExpirationDate:o} revoked={k.IsRevoked}");
                }
            }
            catch (Exception ex) { sb.AppendLine($"GetAllKeys HATA: {ex}"); }

            // Protect -> Unprotect (ayni istek, ayni protector)
            var prot = dpProvider.CreateProtector("__diag");
            try
            {
                var e = prot.Protect("payload-123");
                sb.AppendLine($"PROTECT ok (len {e.Length})");
                try
                {
                    var d = prot.Unprotect(e);
                    sb.AppendLine($"UNPROTECT ok -> {d}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"UNPROTECT HATA: {ex.GetType().Name}: {ex.Message}");
                    for (var ie = ex.InnerException; ie != null; ie = ie.InnerException)
                        sb.AppendLine($"   inner: {ie.GetType().Name}: {ie.Message}");
                }
            }
            catch (Exception ex) { sb.AppendLine($"PROTECT HATA: {ex}"); }

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
