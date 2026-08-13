using Microsoft.AspNetCore.Mvc;
using OtoRehber.Domain.Entities;
using OtoRehber.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using OtoRehber.Models;
using OtoRehber.Domain.DTOs;
using AutoMapper;

namespace OtoRehber.Controllers
{
    public class CarController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IMapper _mapper;

        // Dependency Injection: Veritabanını Controller'a bağlıyoruz
        public CarController(OtoRehberDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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

            var carDto = _mapper.Map<CarDetailDto>(car);
            return View(carDto);
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
    }
}