using System.Diagnostics;
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
                "price_asc" => carsQuery.OrderBy(c => c.PriceRange),
                "price_desc" => carsQuery.OrderByDescending(c => c.PriceRange),
                "score_desc" => carsQuery.OrderByDescending(c => c.ReliabilityScore),
                "score_asc" => carsQuery.OrderBy(c => c.ReliabilityScore),
                _ => carsQuery.OrderByDescending(c => c.Id) // Varsayılan: En son eklenenler
            };

            // Sayfalama (Pagination)
            int pageSize = 12; // Her sayfada 12 araç
            int totalItems = carsQuery.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            var cars = carsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();
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

        public IActionResult FixSegments()
        {
            var cars = _context.Cars.ToList();
            int updatedCount = 0;
            foreach (var car in cars)
            {
                var model = car.ModelName?.ToLower() ?? "";
                var currentSeg = car.Segment?.ToLower().Trim() ?? "";
                var engine = car.Engine?.ToLower() ?? "";
                string newSegment = null;

                // 1. Elektrikli Araç Kontrolü (Öncelikli)
                if (engine.Contains("kwh") || engine.Contains("batarya") || currentSeg.Contains("elektrik") || model.Contains("tesla") || model.Contains("togg") || model.Contains("byd") || model.Contains("spring") || model.Contains("e-tech"))
                {
                    // Hibritleri hariç tutalım
                    if (!engine.Contains("hibrit") && !engine.Contains("hybrid") && !engine.Contains("mild-hybrid") && !model.Contains("hybrid"))
                    {
                        newSegment = "Elektrikli";
                    }
                }

                if (newSegment == null)
                {
                    // Önce var olan Karmaşık Segment datasını temizleyelim
                    if (currentSeg.Contains("suv") || currentSeg.Contains("crossover")) newSegment = "SUV";
                    else if (currentSeg.Contains("ticari") || currentSeg.Contains("minivan") || currentSeg.Contains("panelvan") || currentSeg.Contains("minibüs")) newSegment = "Ticari";
                    else if (currentSeg.Contains("spor") || currentSeg.Contains("roadster")) newSegment = "Spor";
                    else if (currentSeg.Contains(" a ") || currentSeg.StartsWith("a-") || currentSeg.StartsWith("a ")) newSegment = "A";
                    else if (currentSeg.Contains(" b ") || currentSeg.StartsWith("b-") || currentSeg.StartsWith("b ") || currentSeg.StartsWith("b+")) newSegment = "B";
                    else if (currentSeg.Contains(" c ") || currentSeg.StartsWith("c-") || currentSeg.StartsWith("c ") || currentSeg.StartsWith("c+")) newSegment = "C";
                    else if (currentSeg.Contains(" d ") || currentSeg.StartsWith("d-") || currentSeg.StartsWith("d ") || currentSeg.StartsWith("d+")) newSegment = "D";
                    else if (currentSeg.Contains(" e ") || currentSeg.StartsWith("e-") || currentSeg.StartsWith("e ") || currentSeg.StartsWith("e+")) newSegment = "E";
                    else if (currentSeg.StartsWith("a")) newSegment = "A";
                    else if (currentSeg.StartsWith("b")) newSegment = "B";
                    else if (currentSeg.StartsWith("c")) newSegment = "C";
                    else if (currentSeg.StartsWith("d")) newSegment = "D";
                    else if (currentSeg.StartsWith("e")) newSegment = "E";
                else
                {
                    // Segment verisi yoksa veya tamamen yanlışsa, modele göre tahmin et
                    if (model.Contains("clio") || model.Contains("polo") || model.Contains("corsa") || model.Contains("fiesta") || model.Contains("i20") || model.Contains("yaris") || model.Contains("208") || model.Contains("symbol") || model.Contains("301") || model.Contains("elysee") || model.Contains("linea") || model.Contains("accent") || model.Contains("era") || model.Contains("jazz"))
                        newSegment = "B";
                    else if (model.Contains("golf") || model.Contains("corolla") || model.Contains("civic") || model.Contains("focus") || model.Contains("a3") || model.Contains("astra") || model.Contains("leon") || model.Contains("megane") || model.Contains("308") || model.Contains("egea") || model.Contains("bravo") || model.Contains("elantra") || model.Contains("ceed") || model.Contains("1 serisi") || model.Contains("auris") || model.Contains("jetta") || model.Contains("fluence") || model.Contains("cla"))
                        newSegment = "C";
                    else if (model.Contains("passat") || model.Contains("320") || model.Contains("316") || model.Contains("a4") || model.Contains("a5") || model.Contains("c200") || model.Contains("c180") || model.Contains("superb") || model.Contains("insignia") || model.Contains("talisman") || model.Contains("508") || model.Contains("mondeo") || model.Contains("3 serisi") || model.Contains("c serisi"))
                        newSegment = "D";
                    else if (model.Contains("520") || model.Contains("e200") || model.Contains("e250") || model.Contains("a6") || model.Contains("e180") || model.Contains("5 serisi") || model.Contains("e serisi") || model.Contains("s90"))
                        newSegment = "E";
                    else if (model.Contains("3008") || model.Contains("tucson") || model.Contains("tiguan") || model.Contains("qashqai") || model.Contains("sportage") || model.Contains("duster") || model.Contains("x3") || model.Contains("glc") || model.Contains("kuga") || model.Contains("kadjar") || model.Contains("t10x") || model.Contains("model y") || model.Contains("atto 3") || model.Contains("spring") || model.Contains("cr-v") || model.Contains("c-hr") || model.Contains("rav4"))
                        newSegment = "SUV";
                    else if (model.Contains("i10") || model.Contains("picanto"))
                        newSegment = "A";
                    else if (model.Contains("caddy") || model.Contains("transporter") || model.Contains("fiorino") || model.Contains("tourneo") || model.Contains("transit"))
                        newSegment = "Ticari";
                    else if (model.Contains("mx-5") || model.Contains("miata"))
                        newSegment = "Spor";
                }
                } // <-- Added missing closing brace here

                if (newSegment != null && car.Segment != newSegment)
                {
                    car.Segment = newSegment;
                    updatedCount++;
                }
            }

            _context.SaveChanges();
            TempData["SuccessMessage"] = $"{updatedCount} adet aracın karmaşık segment verisi sistem tarafından otomatik olarak A, B, C, D, E, SUV, Ticari formatında düzeltildi!";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult DebugSegments()
        {
            var segments = _context.Cars.Select(c => c.Segment).Distinct().ToList();
            var totalCars = _context.Cars.Count();
            return Json(new { TotalCars = totalCars, Segments = segments });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
