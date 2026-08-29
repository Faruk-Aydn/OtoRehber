using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtoRehber.Domain.Entities;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Reviews")]
    public class AdminReviewController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AdminReviewController> _logger;

        public AdminReviewController(OtoRehberDbContext context, IMemoryCache cache, ILogger<AdminReviewController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // GET: /Admin/Reviews
        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 25;
            var total = await _context.CarReviews.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var reviews = await _context.CarReviews.AsNoTracking()
                .Include(r => r.Car)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total = total;
            return View(reviews);
        }

        // POST: /Admin/Reviews/Delete/5
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.CarReviews.FindAsync(id);
            if (review == null)
            {
                TempData["ErrorMessage"] = "Yorum bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            _context.CarReviews.Remove(review);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                UserName = User.Identity?.Name,
                Action = "Delete",
                Entity = "CarReview",
                EntityId = id.ToString(),
                Detail = $"Araç #{review.CarId} — {review.UserName} — {review.Rating}/10",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();

            _cache.Remove(HomeController.CacheKeyLeaderboard);
            _logger.LogInformation("Admin yorumu sildi: review #{ReviewId} (araç #{CarId})", id, review.CarId);
            TempData["SuccessMessage"] = "Yorum silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
