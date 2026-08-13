using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Domain.Entities;
using OtoRehber.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace OtoRehber.Controllers
{
    [Authorize]
    public class GarageController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public GarageController(OtoRehberDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Garage
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var garageItems = await _context.UserGarages
                .Include(ug => ug.Car)
                .Where(ug => ug.UserId == user.Id)
                .OrderByDescending(ug => ug.AddedAt)
                .ToListAsync();

            return View(garageItems);
        }

        // POST: /Garage/Toggle
        [HttpPost]
        public async Task<IActionResult> Toggle([FromBody] int carId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { error = "Lütfen giriş yapın." });
            }

            var existing = await _context.UserGarages
                .FirstOrDefaultAsync(ug => ug.UserId == user.Id && ug.CarId == carId);

            if (existing != null)
            {
                // Zaten var, çıkart (Remove from favorites)
                _context.UserGarages.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { added = false, message = "Garajdan çıkarıldı." });
            }
            else
            {
                // Yok, ekle (Add to favorites)
                var newGarageItem = new UserGarage
                {
                    UserId = user.Id,
                    CarId = carId
                };
                _context.UserGarages.Add(newGarageItem);
                await _context.SaveChangesAsync();
                return Ok(new { added = true, message = "Garaja eklendi!" });
            }
        }
    }
}
