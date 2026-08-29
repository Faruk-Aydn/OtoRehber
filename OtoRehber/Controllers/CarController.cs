using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using OtoRehber.Domain.Entities;
using OtoRehber.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using OtoRehber.Models;
using OtoRehber.Domain.DTOs;
using OtoRehber.Domain.Mappings;

namespace OtoRehber.Controllers
{
    public class CarController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMemoryCache _cache;

        // Dependency Injection: Veritabanını Controller'a bağlıyoruz
        public CarController(OtoRehberDbContext context, UserManager<AppUser> userManager, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        public async Task<IActionResult> Details(int id)
        {
            // Artık veritabanından ID ile aracı getiriyoruz
            var car = await _context.Cars
                .AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.ChronicIssues)
                .Include(c => c.ProsConsList)
                .Include(c => c.MileageMilestones)
                .Include(c => c.Reviews)
                    .ThenInclude(r => r.ReviewLikes)
                .Include(c => c.PriceHistory)
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null)
            {
                TempData["ErrorMessage"] = "Aradığınız araç artık mevcut değil veya kaldırılmış olabilir.";
                return RedirectToAction("Index", "Home");
            }

            var currentUserId = _userManager.GetUserId(User);
            ViewBag.CurrentUserId = currentUserId;

            // Fiyat geçmişi grafiği için (en az 2 kayıt varsa)
            ViewBag.PriceHistory = car.PriceHistory
                .OrderBy(h => h.RecordedAt)
                .Select(h => new { d = h.RecordedAt.ToString("yyyy-MM-dd"), p = h.Price })
                .ToList();

            // Galeri: kapak (ImageUrl) + ek görseller
            var gallery = new List<string>();
            if (!string.IsNullOrEmpty(car.ImageUrl)) gallery.Add(car.ImageUrl);
            gallery.AddRange(car.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
                .Select(i => i.Url).Where(u => u != car.ImageUrl));
            ViewBag.Gallery = gallery;

            // Yorum başına "faydalı" oy sayısı + kullanıcının oy verip vermediği
            ViewBag.ReviewLikes = car.Reviews.ToDictionary(
                r => r.Id,
                r => (Count: r.ReviewLikes.Count,
                      Voted: currentUserId != null && r.ReviewLikes.Any(l => l.UserId == currentUserId)));

            var carDto = car.ToDetailDto();
            return View(carDto);
        }

        [HttpPost]
        [Authorize]
        [EnableRateLimiting("review")]
        public async Task<IActionResult> AddReview(int carId, int rating, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 10 || rating < 1 || rating > 10)
            {
                TempData["ErrorMessage"] = "Lütfen en az 10 karakterlik bir yorum ve 1-10 arası puan girin.";
                return RedirectToAction("Details", new { id = carId });
            }

            if (!await _context.Cars.AnyAsync(c => c.Id == carId))
            {
                TempData["ErrorMessage"] = "Yorum yapılacak araç bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (await _context.CarReviews.AnyAsync(r => r.CarId == carId && r.UserId == user.Id))
            {
                TempData["ErrorMessage"] = "Bu araç için zaten bir yorumunuz var.";
                return RedirectToAction("Details", new { id = carId });
            }

            var displayName = (user.Email ?? user.UserName ?? "Kullanıcı").Split('@')[0];

            var review = new CarReview
            {
                CarId = carId,
                UserId = user.Id,
                UserName = displayName,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = System.DateTime.UtcNow
            };

            _context.CarReviews.Add(review);
            await _context.SaveChangesAsync();
            _cache.Remove(HomeController.CacheKeyLeaderboard);

            TempData["SuccessMessage"] = "Yorumunuz başarıyla eklendi! Teşekkür ederiz.";
            return RedirectToAction("Details", new { id = carId });
        }

        [HttpPost]
        [Authorize]
        [EnableRateLimiting("review")]
        public async Task<IActionResult> EditReview(int reviewId, int rating, string comment)
        {
            var review = await _context.CarReviews.FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review == null)
                return NotFound();

            if (review.UserId != _userManager.GetUserId(User))
                return Forbid();

            if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 10 || rating < 1 || rating > 10)
            {
                TempData["ErrorMessage"] = "Lütfen en az 10 karakterlik bir yorum ve 1-10 arası puan girin.";
                return RedirectToAction("Details", new { id = review.CarId });
            }

            review.Rating = rating;
            review.Comment = comment.Trim();
            await _context.SaveChangesAsync();
            _cache.Remove(HomeController.CacheKeyLeaderboard);

            TempData["SuccessMessage"] = "Yorumunuz güncellendi.";
            return RedirectToAction("Details", new { id = review.CarId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var review = await _context.CarReviews.FirstOrDefaultAsync(r => r.Id == reviewId);
            if (review == null)
                return NotFound();

            if (review.UserId != _userManager.GetUserId(User))
                return Forbid();

            var carId = review.CarId;
            _context.CarReviews.Remove(review);
            await _context.SaveChangesAsync();
            _cache.Remove(HomeController.CacheKeyLeaderboard);

            TempData["SuccessMessage"] = "Yorumunuz silindi.";
            return RedirectToAction("Details", new { id = carId });
        }

        public class ReviewLikeRequest
        {
            public int ReviewId { get; set; }
        }

        // Yoruma "faydalı" oyu ver / geri al (AJAX). Kendi yorumuna oy verilemez.
        [HttpPost]
        [Authorize]
        [EnableRateLimiting("review")]
        public async Task<IActionResult> ToggleReviewLike([FromBody] ReviewLikeRequest req)
        {
            var review = await _context.CarReviews.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == req.ReviewId);
            if (review == null)
                return NotFound();

            var userId = _userManager.GetUserId(User)!;
            if (review.UserId == userId)
                return BadRequest(new { error = "Kendi yorumunuza oy veremezsiniz." });

            var existing = await _context.ReviewLikes
                .FirstOrDefaultAsync(l => l.ReviewId == req.ReviewId && l.UserId == userId);

            bool voted;
            if (existing != null)
            {
                _context.ReviewLikes.Remove(existing);
                voted = false;
            }
            else
            {
                _context.ReviewLikes.Add(new ReviewLike { ReviewId = req.ReviewId, UserId = userId, IsHelpful = true });
                voted = true;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Eşzamanlı çift tık → unique index ihlali; mevcut durumu döndür.
            }

            var count = await _context.ReviewLikes.CountAsync(l => l.ReviewId == req.ReviewId);
            return Json(new { count, voted });
        }
    }
}