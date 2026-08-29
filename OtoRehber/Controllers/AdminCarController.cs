using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.DTOs;
using OtoRehber.Domain.Mappings;
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
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<AdminCarController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IYoutubeImportQueue _importQueue;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private static readonly string[] AllowedImageContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        public AdminCarController(OtoRehberDbContext context, IWebHostEnvironment hostEnvironment, ILogger<AdminCarController> logger, IMemoryCache cache, IYoutubeImportQueue importQueue)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
            _cache = cache;
            _importQueue = importQueue;
        }

        private void InvalidateHomeCache()
        {
            _cache.Remove(HomeController.CacheKeyBrands);
            _cache.Remove(HomeController.CacheKeyLeaderboard);
            _cache.Remove("catalog-menu");
            _cache.Remove("seo:sitemap-cars");
        }

        private async Task AuditAsync(string action, string entity, string? entityId, string? detail)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                UserName = User.Identity?.Name,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Detail = detail?.Length > 1000 ? detail[..1000] : detail,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
        }

        // GET: /AdminCar/ImportFromYoutube
        public IActionResult ImportFromYoutube()
        {
            return View();
        }

        // POST: /AdminCar/ImportFromYoutube
        // İş kuyruğa alınır, arka planda işlenir; kullanıcı durum sayfasına yönlendirilir.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportFromYoutube(string youtubeUrl)
        {
            if (string.IsNullOrWhiteSpace(youtubeUrl))
            {
                ViewBag.Error = "Lütfen geçerli bir YouTube linki girin.";
                return View();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var job = _importQueue.Enqueue(youtubeUrl.Trim(), userId, User.Identity?.Name);
            _logger.LogInformation("YouTube AI import kuyruğa alındı: {JobId} ({Url})", job.Id, youtubeUrl);

            return RedirectToAction(nameof(ImportStatus), new { id = job.Id });
        }

        // GET: /AdminCar/ImportStatus/{id}
        public IActionResult ImportStatus(Guid id)
        {
            var job = _importQueue.Get(id);
            if (job == null)
            {
                TempData["ErrorMessage"] = "İçe aktarma işi bulunamadı (süresi dolmuş olabilir).";
                return RedirectToAction(nameof(Index));
            }
            return View(job);
        }

        // GET: /AdminCar/ImportStatusJson/{id} — durum sayfasının polling'i için.
        [HttpGet]
        public IActionResult ImportStatusJson(Guid id)
        {
            var job = _importQueue.Get(id);
            if (job == null)
                return NotFound();

            return Json(new
            {
                state = job.State.ToString(),
                finished = job.IsFinished,
                message = job.Message,
                carCount = job.CarCount
            });
        }

        // GET: AdminCar
        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars.ToListAsync();
            var carDtos = cars.ToListDto();
            
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

                var car = carDto.ToEntity();
                _context.Add(car);
                await _context.SaveChangesAsync();
                InvalidateHomeCache();
                await AuditAsync("Create", "Car", car.Id.ToString(), $"{car.Brand} {car.ModelName}");
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
            
            var carDto = car.ToCreateDto();
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

                    // Alanları mevcut nesneye aktarıyoruz (Overposting engellenir; Id + nav'lar korunur)
                    carDto.ApplyTo(existingCar);

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
                await AuditAsync("Update", "Car", carDto.Id.ToString(), $"{carDto.Brand} {carDto.ModelName}");
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
                await AuditAsync("Delete", "Car", id.ToString(), name);
                TempData["SuccessMessage"] = $"{name} başarıyla silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ---- Fiyat geçmişi (araç başına manuel fiyat kaydı) ----

        // GET: AdminCar/PriceHistory/5
        public async Task<IActionResult> PriceHistory(int id)
        {
            var car = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return NotFound();

            ViewBag.Car = car;
            var history = await _context.CarPriceHistories.AsNoTracking()
                .Where(h => h.CarId == id)
                .OrderByDescending(h => h.RecordedAt)
                .ToListAsync();
            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPriceHistory(int carId, int price, DateTime? recordedAt)
        {
            if (price < 1)
            {
                TempData["ErrorMessage"] = "Geçerli bir fiyat girin.";
                return RedirectToAction(nameof(PriceHistory), new { id = carId });
            }
            if (!await _context.Cars.AnyAsync(c => c.Id == carId))
                return NotFound();

            _context.CarPriceHistories.Add(new CarPriceHistory
            {
                CarId = carId,
                Price = price,
                RecordedAt = (recordedAt ?? DateTime.UtcNow).Date
            });
            await _context.SaveChangesAsync();
            await AuditAsync("Create", "CarPriceHistory", carId.ToString(), $"{price:N0} TL @ {(recordedAt ?? DateTime.UtcNow):yyyy-MM-dd}");
            TempData["SuccessMessage"] = "Fiyat kaydı eklendi.";
            return RedirectToAction(nameof(PriceHistory), new { id = carId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePriceHistory(int id)
        {
            var row = await _context.CarPriceHistories.FindAsync(id);
            if (row == null) return NotFound();
            var carId = row.CarId;
            _context.CarPriceHistories.Remove(row);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Fiyat kaydı silindi.";
            return RedirectToAction(nameof(PriceHistory), new { id = carId });
        }

        // ---- Araç görsel galerisi (kapak = Car.ImageUrl, ekstralar = CarImage) ----

        public async Task<IActionResult> Images(int id)
        {
            var car = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return NotFound();
            ViewBag.Car = car;
            var images = await _context.CarImages.AsNoTracking()
                .Where(i => i.CarId == id).OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToListAsync();
            return View(images);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> AddImages(int carId, List<IFormFile> files)
        {
            if (!await _context.Cars.AnyAsync(c => c.Id == carId)) return NotFound();

            int added = 0, skipped = 0;
            int nextSort = (await _context.CarImages.Where(i => i.CarId == carId).MaxAsync(i => (int?)i.SortOrder) ?? -1) + 1;

            foreach (var file in files ?? new List<IFormFile>())
            {
                if (file.Length == 0) continue;
                if (!IsValidImage(file)) { skipped++; continue; }
                var url = await SaveImageAsync(file);
                _context.CarImages.Add(new CarImage { CarId = carId, Url = url, SortOrder = nextSort++ });
                added++;
            }
            if (added > 0) await _context.SaveChangesAsync();

            InvalidateHomeCache();
            TempData[skipped > 0 ? "InfoMessage" : "SuccessMessage"] =
                $"{added} görsel eklendi" + (skipped > 0 ? $", {skipped} geçersiz dosya atlandı." : ".");
            return RedirectToAction(nameof(Images), new { id = carId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var img = await _context.CarImages.FindAsync(id);
            if (img == null) return NotFound();
            var carId = img.CarId;
            DeletePhysicalImage(img.Url);
            _context.CarImages.Remove(img);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Görsel silindi.";
            return RedirectToAction(nameof(Images), new { id = carId });
        }

        // Bu galeri görselini araç kapağı yap (Car.ImageUrl).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeCover(int id)
        {
            var img = await _context.CarImages.FindAsync(id);
            if (img == null) return NotFound();
            var car = await _context.Cars.FindAsync(img.CarId);
            if (car == null) return NotFound();
            car.ImageUrl = img.Url;
            await _context.SaveChangesAsync();
            InvalidateHomeCache();
            TempData["SuccessMessage"] = "Kapak görseli güncellendi.";
            return RedirectToAction(nameof(Images), new { id = car.Id });
        }

        private void DeletePhysicalImage(string? url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("/images/")) return;
            try
            {
                var path = Path.Combine(_hostEnvironment.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { /* yoksay */ }
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

            // Magic-byte (dosya imzası) kontrolü — uzantı/content-type sahtelenebilir.
            return HasValidImageSignature(file);
        }

        private static bool HasValidImageSignature(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                Span<byte> head = stackalloc byte[12];
                int read = stream.Read(head);
                if (read < 12) return false;

                // JPEG: FF D8 FF
                if (head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return true;
                // PNG: 89 50 4E 47 0D 0A 1A 0A
                if (head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47) return true;
                // GIF: "GIF87a" / "GIF89a"
                if (head[0] == 0x47 && head[1] == 0x49 && head[2] == 0x46) return true;
                // WEBP: "RIFF"...."WEBP"
                if (head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46 &&
                    head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50) return true;

                return false;
            }
            catch
            {
                return false;
            }
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
