using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Domain.Entities;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Users")]
    public class AdminUserController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly OtoRehberDbContext _context;
        private readonly ILogger<AdminUserController> _logger;

        public AdminUserController(UserManager<AppUser> userManager, OtoRehberDbContext context, ILogger<AdminUserController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public record UserRow(string Id, string? Email, bool EmailConfirmed, bool IsAdmin,
            bool LockedOut, int ReviewCount, int GarageCount, bool IsSelf);

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            const int pageSize = 30;
            var meId = _userManager.GetUserId(User);

            var query = _userManager.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var lower = q.Trim().ToLowerInvariant();
                query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(lower));
            }

            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var users = await query.OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var ids = users.Select(u => u.Id).ToList();
            var reviewCounts = (await _context.CarReviews.AsNoTracking()
                .Where(r => ids.Contains(r.UserId))
                .GroupBy(r => r.UserId).Select(g => new { g.Key, C = g.Count() }).ToListAsync())
                .ToDictionary(x => x.Key, x => x.C);
            var garageCounts = (await _context.UserGarages.AsNoTracking()
                .Where(g => ids.Contains(g.UserId))
                .GroupBy(g => g.UserId).Select(g => new { g.Key, C = g.Count() }).ToListAsync())
                .ToDictionary(x => x.Key, x => x.C);

            var rows = new List<UserRow>();
            foreach (var u in users)
            {
                rows.Add(new UserRow(
                    u.Id, u.Email, u.EmailConfirmed,
                    IsAdmin: await _userManager.IsInRoleAsync(u, "Admin"),
                    LockedOut: u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    ReviewCount: reviewCounts.GetValueOrDefault(u.Id),
                    GarageCount: garageCounts.GetValueOrDefault(u.Id),
                    IsSelf: u.Id == meId));
            }

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total = total;
            ViewBag.Query = q;
            return View(rows);
        }

        [HttpPost("ToggleAdmin/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { TempData["ErrorMessage"] = "Kullanıcı bulunamadı."; return Redirect(); }
            if (id == _userManager.GetUserId(User)) { TempData["ErrorMessage"] = "Kendi rolünüzü değiştiremezsiniz."; return Redirect(); }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                await AuditAsync("RoleRemove", id, $"{user.Email} → Admin rolü kaldırıldı");
                TempData["SuccessMessage"] = $"{user.Email} artık admin değil.";
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                await AuditAsync("RoleAdd", id, $"{user.Email} → Admin rolü verildi");
                TempData["SuccessMessage"] = $"{user.Email} artık admin.";
            }
            return Redirect();
        }

        [HttpPost("Unlock/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { TempData["ErrorMessage"] = "Kullanıcı bulunamadı."; return Redirect(); }
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            TempData["SuccessMessage"] = $"{user.Email} kilidi açıldı.";
            return Redirect();
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == _userManager.GetUserId(User)) { TempData["ErrorMessage"] = "Kendi hesabınızı buradan silemezsiniz."; return Redirect(); }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { TempData["ErrorMessage"] = "Kullanıcı bulunamadı."; return Redirect(); }

            var garage = await _context.UserGarages.Where(g => g.UserId == id).ToListAsync();
            if (garage.Count > 0) { _context.UserGarages.RemoveRange(garage); await _context.SaveChangesAsync(); }

            var email = user.Email;
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Silinemedi: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return Redirect();
            }
            await AuditAsync("Delete", id, $"Kullanıcı silindi: {email}");
            _logger.LogWarning("Admin kullanıcı sildi: {UserId} ({Email})", id, email);
            TempData["SuccessMessage"] = $"{email} ve verileri silindi.";
            return Redirect();
        }

        private IActionResult Redirect() => RedirectToAction(nameof(Index));

        private async Task AuditAsync(string action, string entityId, string detail)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = User.Identity?.Name,
                Action = action,
                Entity = "AppUser",
                EntityId = entityId,
                Detail = detail.Length > 1000 ? detail[..1000] : detail,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
        }
    }
}
