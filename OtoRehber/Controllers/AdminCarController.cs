using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.DTOs;
using AutoMapper;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;

namespace OtoRehber.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCarController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IAiCarDataService _aiService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminCarController> _logger;
        private readonly IMemoryCache _cache;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private static readonly string[] AllowedImageContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        public AdminCarController(OtoRehberDbContext context, IAiCarDataService aiService, IWebHostEnvironment hostEnvironment, IMapper mapper, ILogger<AdminCarController> logger, IMemoryCache cache)
        {
            _context = context;
            _aiService = aiService;
            _hostEnvironment = hostEnvironment;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
        }

        private void InvalidateHomeCache()
        {
            _cache.Remove(HomeController.CacheKeyBrands);
            _cache.Remove(HomeController.CacheKeyLeaderboard);
        }

        // GET: /AdminCar/ImportFromYoutube
        public IActionResult ImportFromYoutube()
        {
            return View();
        }

        // POST: /AdminCar/ImportFromYoutube
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromYoutube(string youtubeUrl)
        {
            if (string.IsNullOrEmpty(youtubeUrl))
            {
                ViewBag.Error = "Lütfen geçerli bir YouTube linki girin.";
                return View();
            }

            try
            {
                var cars = await _aiService.AnalyzeAndSaveFromYoutubeAsync(youtubeUrl);

                if (cars != null && cars.Any())
                {
                    InvalidateHomeCache();
                    TempData["SuccessMessage"] = $"{cars.Count} adet araç yapay zeka ile başarıyla eklendi!";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Error = "Videodan işlenebilecek bir araç bilgisi çıkarılamadı.";
                return View();
            }
            catch (InvalidOperationException ex)
            {
                // Transkript/altyazı bulunamadı gibi beklenen hatalar
                ViewBag.Error = ex.Message;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YouTube AI import başarısız: {Url}", youtubeUrl);
                ViewBag.Error = "İşlem sırasında beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
                return View();
            }
        }

        // GET: AdminCar
        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars.ToListAsync();
            var carDtos = _mapper.Map<List<CarListDto>>(cars);
            
            // Dashboard İstatistikleri
            ViewBag.TotalCars = cars.Count;
            ViewBag.TotalReviews = await _context.CarReviews.CountAsync();
            ViewBag.AvgReliability = cars.Any() ? Math.Round(cars.Average(c => c.ReliabilityScore), 1) : 0;
            
            var topCar = cars.OrderByDescending(c => c.ReliabilityScore).FirstOrDefault();
            ViewBag.TopCarName = topCar != null ? $"{topCar.Brand} {topCar.ModelName}" : "-";

            // Segmentleri basitleştirmek için yardımcı fonksiyon
            Func<OtoRehber.Domain.Entities.Car, string> SimplifySegment = (c) => {
                var eng = (c.Engine ?? "").ToLowerInvariant();
                if (eng.Contains("elektrik") || eng.Contains("ev") || eng == "elektrikli") return "Elektrikli";

                var s = (c.Segment ?? "").ToLowerInvariant();
                if (string.IsNullOrEmpty(s)) return "Belirsiz";
                
                if (s.Contains("suv") || s.Contains("crossover")) return "SUV";
                if (s.Contains("ticari") || s.Contains("minivan") || s.Contains("panelvan")) return "Ticari";
                if (s.Contains("spor") || s.Contains("coupe") || s.Contains("cabrio")) return "Spor";
                
                if (s.Contains("a-") || s.StartsWith("a ")) return "A Segmenti";
                if (s.Contains("b-") || s.StartsWith("b ")) return "B Segmenti";
                if (s.StartsWith("c") && (s.Length == 1 || s[1] == '-' || s[1] == ' ')) return "C Segmenti";
                if (s.StartsWith("d") && (s.Length == 1 || s[1] == '-' || s[1] == ' ')) return "D Segmenti";
                if (s.StartsWith("e") && (s.Length == 1 || s[1] == '-' || s[1] == ' ')) return "E Segmenti";
                
                return "Diğer";
            };

            // Grafik Verileri (Chart.js için)
            var segmentData = cars.GroupBy(c => SimplifySegment(c))
                                  .Select(g => new { Label = g.Key, Count = g.Count() })
                                  .OrderByDescending(x => x.Count)
                                  .ToList();
            
            var brandData = cars.GroupBy(c => c.Brand ?? "Belirsiz")
                                .Select(g => new { Label = g.Key, Count = g.Count() })
                                .OrderByDescending(x => x.Count)
                                .Take(10) // En çok aracı olan 10 marka
                                .ToList();

            ViewBag.SegmentLabels = System.Text.Json.JsonSerializer.Serialize(segmentData.Select(x => x.Label));
            ViewBag.SegmentCounts = System.Text.Json.JsonSerializer.Serialize(segmentData.Select(x => x.Count));
            
            ViewBag.BrandLabels = System.Text.Json.JsonSerializer.Serialize(brandData.Select(x => x.Label));
            ViewBag.BrandCounts = System.Text.Json.JsonSerializer.Serialize(brandData.Select(x => x.Count));

            return View(carDtos);
        }

        public IActionResult Create()

        {
            return View();
        }

        // POST: AdminCar/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CarCreateDto carDto, IFormFile? imageFile)
        {
            ModelState.Remove("UserFeedbackSummary"); // İsteğe bağlı olabilir

            if (imageFile != null && imageFile.Length > 0 && !IsValidImage(imageFile))
            {
                ModelState.AddModelError(nameof(imageFile), "Geçersiz görsel. Sadece JPG, PNG, WEBP veya GIF (maks. 5 MB) yükleyebilirsiniz.");
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    carDto.ImageUrl = await SaveImageAsync(imageFile);
                }

                var car = _mapper.Map<Car>(carDto);
                _context.Add(car);
                await _context.SaveChangesAsync();
                InvalidateHomeCache();
                TempData["SuccessMessage"] = $"{car.Brand} {car.ModelName} başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(carDto);
        }

        // GET: AdminCar/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                return NotFound();
            }
            
            var carDto = _mapper.Map<CarCreateDto>(car);
            return View(carDto);
        }

        // POST: AdminCar/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CarCreateDto carDto, IFormFile? imageFile)
        {
            if (id != carDto.Id)
            {
                return NotFound();
            }

            ModelState.Remove("UserFeedbackSummary"); // İsteğe bağlı, formda yoksa patlamasın diye

            if (imageFile != null && imageFile.Length > 0 && !IsValidImage(imageFile))
            {
                ModelState.AddModelError(nameof(imageFile), "Geçersiz görsel. Sadece JPG, PNG, WEBP veya GIF (maks. 5 MB) yükleyebilirsiniz.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCar = await _context.Cars.FindAsync(id);
                    if (existingCar == null)
                    {
                        return NotFound();
                    }

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string newImageUrl = await SaveImageAsync(imageFile);

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingCar.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingCar.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        carDto.ImageUrl = newImageUrl;
                    }
                    else
                    {
                        // Resim güncellenmediyse mevcut resmi koru
                        carDto.ImageUrl = existingCar.ImageUrl;
                    }

                    // AutoMapper ile alanları mevcut nesneye aktarıyoruz (Overposting engellenir)
                    _mapper.Map(carDto, existingCar);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(carDto.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                InvalidateHomeCache();
                TempData["SuccessMessage"] = $"{carDto.Brand} {carDto.ModelName} başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(carDto);
        }

        // GET: AdminCar/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(m => m.Id == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // POST: AdminCar/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                var name = $"{car.Brand} {car.ModelName}";
                _context.Cars.Remove(car);
                await _context.SaveChangesAsync();
                InvalidateHomeCache();
                TempData["SuccessMessage"] = $"{name} başarıyla silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id);
        }

        private static bool IsValidImage(IFormFile file)
        {
            if (file.Length <= 0 || file.Length > MaxImageSizeBytes)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(extension))
                return false;

            if (string.IsNullOrEmpty(file.ContentType) || !AllowedImageContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return false;

            return true;
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "cars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Orijinal dosya adını yok sayıyoruz; path traversal ve dosya adı enjeksiyonunu engellemek için sadece uzantıyı koruyoruz.
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/images/cars/" + uniqueFileName;
        }
    }
}
