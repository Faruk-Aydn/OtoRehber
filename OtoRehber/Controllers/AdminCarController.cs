using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OtoRehber.Controllers
{
    public class AdminCarController : Controller
    {
        private readonly OtoRehberDbContext _context;

        public AdminCarController(OtoRehberDbContext context)
        {
            _context = context;
        }

        // GET: AdminCar
        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars.ToListAsync();
            return View(cars);
        }

        // GET: AdminCar/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminCar/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Brand,ModelName,Engine,Segment,ExpertSummary,ReliabilityScore,PriceRange,EstimatedMaintenanceCostEUR")] Car car)
        {
            if (ModelState.IsValid)
            {
                _context.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(car);
        }

        // GET: AdminCar/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                return NotFound();
            }
            return View(car);
        }

        // POST: AdminCar/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Brand,ModelName,Engine,Segment,ExpertSummary,ReliabilityScore,PriceRange,EstimatedMaintenanceCostEUR")] Car car)
        {
            if (id != car.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(car);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(car.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(car);
        }

        // GET: AdminCar/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(m => m.Id == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // POST: AdminCar/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                _context.Cars.Remove(car);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id);
        }
    }
}
