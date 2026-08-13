using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OtoRehber.Controllers
{
    public class CompareController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IAiCarDataService _aiService;

        public CompareController(OtoRehberDbContext context, IAiCarDataService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        // GET: /Compare
        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars.OrderBy(c => c.Brand).ThenBy(c => c.ModelName).ToListAsync();
            return View(cars);
        }

        // GET: /Compare/Result?car1Id=1&car2Id=2
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "car1Id", "car2Id" })]
        public async Task<IActionResult> Result(int car1Id, int car2Id)
        {
            if (car1Id == car2Id)
            {
                TempData["ErrorMessage"] = "Lütfen karşılaştırmak için iki farklı araç seçin.";
                return RedirectToAction(nameof(Index));
            }

            var car1 = await _context.Cars
                .Include(c => c.ProsConsList)
                .Include(c => c.ChronicIssues)
                .FirstOrDefaultAsync(c => c.Id == car1Id);

            var car2 = await _context.Cars
                .Include(c => c.ProsConsList)
                .Include(c => c.ChronicIssues)
                .FirstOrDefaultAsync(c => c.Id == car2Id);

            if (car1 == null || car2 == null)
            {
                TempData["ErrorMessage"] = "Seçilen araçlardan biri veya ikisi bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // AI Yorumunu al
            string verdict = await _aiService.GetComparisonVerdictAsync(car1, car2);

            var viewModel = new CarCompareViewModel
            {
                Car1 = car1,
                Car2 = car2,
                AiVerdict = verdict
            };

            return View(viewModel);
        }
    }
}
