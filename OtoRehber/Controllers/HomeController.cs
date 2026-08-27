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

        public IActionResult Privacy()
        {
            return View();
        }

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
