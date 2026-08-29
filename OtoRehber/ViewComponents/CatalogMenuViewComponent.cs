using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtoRehber.Controllers;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.ViewComponents
{
    // Footer'daki "Markalar" / "Segmentler" listesi. 10 dk cache (nadiren değişir).
    public class CatalogMenuViewComponent : ViewComponent
    {
        private readonly OtoRehberDbContext _context;
        private readonly IMemoryCache _cache;

        public CatalogMenuViewComponent(OtoRehberDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public record MenuLink(string Name, string Slug);

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await _cache.GetOrCreateAsync("catalog-menu", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                var rows = await _context.Cars.AsNoTracking()
                    .Select(c => new { c.Brand, c.Segment })
                    .ToListAsync();

                var brands = rows.Select(r => r.Brand)
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .Distinct()
                    .OrderBy(b => b)
                    .Select(b => new MenuLink(b, CatalogController.Slugify(b)))
                    .ToList();

                var segments = rows.Select(r => r.Segment)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .Select(s => new MenuLink(s, CatalogController.Slugify(s)))
                    .ToList();

                return (Brands: brands, Segments: segments);
            });

            return View(model);
        }
    }
}
