using Microsoft.AspNetCore.Mvc;
using OtoRehber.Domain.Entities;
using OtoRehber.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using OtoRehber.Models;

namespace OtoRehber.Controllers
{
    public class CarController : Controller
    {
        private readonly OtoRehberDbContext _context;

        // Dependency Injection: Veritabanını Controller'a bağlıyoruz
        public CarController(OtoRehberDbContext context)
        {
            _context = context;
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
                return NotFound("Aradığınız araç veritabanında bulunamadı.");
            }

            return View(car);
        }

        [HttpPost]
        public IActionResult AddReview(int carId, string userName, int rating, string comment)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(comment) || rating < 1 || rating > 10)
            {
                TempData["ErrorMessage"] = "Lütfen tüm alanları geçerli şekilde doldurun.";
                return RedirectToAction("Details", new { id = carId });
            }

            var review = new CarReview
            {
                CarId = carId,
                UserName = userName,
                Rating = rating,
                Comment = comment,
                CreatedAt = System.DateTime.Now
            };

            _context.CarReviews.Add(review);
            _context.SaveChanges();

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