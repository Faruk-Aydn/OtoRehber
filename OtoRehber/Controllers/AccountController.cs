using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Models;

namespace OtoRehber.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IAppEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            IAppEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToLocal(returnUrl);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToLocal(returnUrl);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "Çok fazla başarısız giriş denemesi. Hesabınız geçici olarak kilitlendi, lütfen daha sonra tekrar deneyin.");
                return View(model);
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty,
                    "Giriş yapabilmek için e-posta adresinizi doğrulamanız gerekiyor. Gelen kutunuzu kontrol edin.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi. E-posta veya şifre hatalı.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new AppUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await SendConfirmationEmailAsync(user);
                return RedirectToAction(nameof(RegisterConfirmation));
            }

            // Hesap enumerasyonunu önlemek için "e-posta zaten kayıtlı" bilgisini sızdırma.
            if (result.Errors.Any(e => e.Code.Contains("DuplicateUserName") || e.Code.Contains("DuplicateEmail")))
            {
                _logger.LogInformation("Kayıtlı e-posta ile tekrar kayıt denemesi: {Email}", model.Email);
                return RedirectToAction(nameof(RegisterConfirmation));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterConfirmation() => View();

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? code)
        {
            if (userId == null || code == null)
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return View("ConfirmEmailResult", false);

            try
            {
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch
            {
                return View("ConfirmEmailResult", false);
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View("ConfirmEmailResult", result.Succeeded);
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            // Kullanıcı var olsun olmasın aynı sonuç — hesap enumerasyonu engellenir.
            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var callbackUrl = Url.Action(nameof(ResetPassword), "Account",
                    new { code, email = model.Email }, protocol: Request.Scheme);

                await _emailSender.SendAsync(model.Email, "OtoRehber — Şifre sıfırlama",
                    $"Şifrenizi sıfırlamak için <a href=\"{HtmlEncoder.Default.Encode(callbackUrl!)}\">buraya tıklayın</a>. " +
                    "Bu isteği siz yapmadıysanız bu e-postayı görmezden gelebilirsiniz.");
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        public IActionResult ResetPassword(string? code = null, string? email = null)
        {
            if (code == null) return RedirectToAction("Index", "Home");
            return View(new ResetPasswordViewModel { Code = code, Email = email ?? "" });
        }

        [HttpPost]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation)); // enumerasyon önleme

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Şifre sıfırlama bağlantısı geçersiz.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        private async Task SendConfirmationEmailAsync(AppUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account",
                new { userId = user.Id, code }, protocol: Request.Scheme);

            await _emailSender.SendAsync(user.Email!, "OtoRehber — E-posta adresinizi doğrulayın",
                $"OtoRehber'e hoş geldiniz! Hesabınızı etkinleştirmek için " +
                $"<a href=\"{HtmlEncoder.Default.Encode(callbackUrl!)}\">buraya tıklayın</a>.");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
