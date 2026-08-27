using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OtoRehber.Domain.Entities;
using OtoRehber.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using OtoRehber.Models;
using OtoRehber.Domain.DTOs;
using AutoMapper;

namespace OtoRehber.Controllers
{
    public class CarController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;

        // Dependency Injection: Veritabanını Controller'a bağlıyoruz
        public CarController(OtoRehberDbContext context, IMapper mapper, UserManager<AppUser> userManager)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
        }

        public IActionResult Details(int id)
        {
            // Artık veritabanından ID ile aracı getiriyoruz
            var car = _context.Cars
                .Include(c => c.ChronicIssues)
                .Include(c => c.ProsConsList)
                .Include(c => c.MileageMilestones)
                .Include(c => c.Reviews)
                .FirstOrDefault(c => c.Id == id);

            if (car == null)
            {
                TempData["ErrorMessage"] = "Aradığınız araç artık mevcut değil veya kaldırılmış olabilir.";
                return RedirectToAction("Index", "Home");
            }

            var carDto = _mapper.Map<CarDetailDto>(car);
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

            TempData["SuccessMessage"] = "Yorumunuz başarıyla eklendi! Teşekkür ederiz.";
            return RedirectToAction("Details", new { id = carId });
        }

        public IActionResult Compare(int id1, int id2)
        {
            var car1 = _context.Cars
                .Include(c => c.ChronicIssues)
                .Include(c => c.ProsConsList)
                .Include(c => c.MileageMilestones)
                .FirstOrDefault(c => c.Id == id1);

            var car2 = _context.Cars
                .Include(c => c.ChronicIssues)
                .Include(c => c.ProsConsList)
                .Include(c => c.MileageMilestones)
                .FirstOrDefault(c => c.Id == id2);

            if (car1 == null || car2 == null)
            {
                return NotFound("Karşılaştırılacak araçlardan biri veya ikisi bulunamadı.");
            }

            var viewModel = new CarCompareViewModel
            {
                Car1 = car1,
                Car2 = car2
            };

            return View(viewModel);
        }
    }
}