using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Models;

namespace OtoRehber.Controllers
{
    // Kullanıcı hesap ayarları: şifre, e-posta, hesabı sil (KVKK — silme hakkı).
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IAppEmailSender _emailSender;
        private readonly OtoRehberDbContext _context;
        private readonly ILogger<ManageController> _logger;

        public ManageController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IAppEmailSender emailSender,
            OtoRehberDbContext context,
            ILogger<ManageController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var uid = user.Id;
            ViewBag.Email = user.Email;
            ViewBag.EmailConfirmed = user.EmailConfirmed;
            ViewBag.ReviewCount = await _context.CarReviews.CountAsync(r => r.UserId == uid);
            ViewBag.GarageCount = await _context.UserGarages.CountAsync(g => g.UserId == uid);
            return View();
        }

        // ---- Şifre değiştir ----
        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Kullanıcı şifresini değiştirdi: {UserId}", user.Id);
            TempData["SuccessMessage"] = "Şifreniz güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // ---- E-posta değiştir ----
        [HttpGet]
        public IActionResult ChangeEmail() => View(new ChangeEmailViewModel());

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var newEmail = model.NewEmail.Trim();
            if (string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.NewEmail), "Bu zaten mevcut e-posta adresiniz.");
                return View(model);
            }

            // Adres enumerasyonunu önlemek için: adres başkasına aitse de aynı jenerik yanıtı ver.
            var existing = await _userManager.FindByEmailAsync(newEmail);
            if (existing == null)
            {
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var link = Url.Action(nameof(ConfirmEmailChange), "Manage",
                    new { userId = user.Id, email = newEmail, code = encoded }, Request.Scheme);

                await _emailSender.SendAsync(newEmail, "OtoRehber — E-posta adresi değişikliği",
                    $"E-posta adresinizi değiştirmek için <a href=\"{link}\">bu bağlantıya</a> tıklayın. Siz talep etmediyseniz bu e-postayı yok sayın.");
                _logger.LogInformation("E-posta değişikliği talep edildi: {UserId} → {NewEmail}", user.Id, newEmail);
            }

            TempData["InfoMessage"] = $"{newEmail} adresine bir doğrulama bağlantısı gönderdik. Değişikliğin tamamlanması için bağlantıya tıklayın.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmailChange(string? userId, string? email, string? code)
        {
            if (userId == null || email == null || code == null)
                return RedirectToAction(nameof(Index));

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Id != userId)
                return Challenge();

            string token;
            try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
            catch { TempData["ErrorMessage"] = "Bağlantı geçersiz veya süresi dolmuş."; return RedirectToAction(nameof(Index)); }

            var result = await _userManager.ChangeEmailAsync(user, email, token);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "E-posta değişikliği doğrulanamadı. Bağlantının süresi dolmuş olabilir.";
                return RedirectToAction(nameof(Index));
            }

            // UserName da e-posta olduğu için onu da güncelle.
            await _userManager.SetUserNameAsync(user, email);
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Kullanıcı e-postasını değiştirdi: {UserId}", user.Id);
            TempData["SuccessMessage"] = "E-posta adresiniz güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Hesabı sil ----
        [HttpGet]
        public IActionResult DeleteAccount() => View(new DeleteAccountViewModel());

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Şifre hatalı.");
                return View(model);
            }

            var uid = user.Id;

            // Garaj kayıtlarının AppUser'a FK'sı yok → elle temizle.
            // (Yorumlar CarReview.User FK'sı Cascade olduğu için otomatik silinir.)
            var garageRows = await _context.UserGarages.Where(g => g.UserId == uid).ToListAsync();
            if (garageRows.Count > 0)
            {
                _context.UserGarages.RemoveRange(garageRows);
                await _context.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("Kullanıcı hesabını sildi: {UserId}", uid);
            TempData["SuccessMessage"] = "Hesabınız ve verileriniz kalıcı olarak silindi.";
            return RedirectToAction("Index", "Home");
        }
    }
}
