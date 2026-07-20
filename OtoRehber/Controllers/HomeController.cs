using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OtoRehber.Models;

namespace OtoRehber.Controllers
{
    public class HomeController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(OtoRehberDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index(string searchQuery)
        {
            var carsQuery = _context.Cars.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                carsQuery = carsQuery.Where(c => c.Brand.Contains(searchQuery) || c.ModelName.Contains(searchQuery));
            }

            var cars = carsQuery.ToList();
            return View(cars);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
