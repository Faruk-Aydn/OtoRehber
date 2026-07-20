using Microsoft.AspNetCore.Mvc;
using OtoRehber.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
            var car = _context.Cars.Include(c => c.ChronicIssues).FirstOrDefault(c => c.Id == id);

            if (car == null)
            {
                return NotFound("Aradığınız araç veritabanında bulunamadı.");
            }

            return View(car);
        }
    }
}