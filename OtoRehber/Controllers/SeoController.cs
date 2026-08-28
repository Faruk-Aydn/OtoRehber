using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Controllers
{
    // robots.txt ve sitemap.xml — arama motorları için.
    public class SeoController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IMemoryCache _cache;

        public SeoController(OtoRehberDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

        [HttpGet("/robots.txt")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public IActionResult Robots()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine("Disallow: /AdminCar");
            sb.AppendLine("Disallow: /Account");
            sb.AppendLine("Disallow: /Garage");
            sb.AppendLine();
            sb.AppendLine($"Sitemap: {BaseUrl}/sitemap.xml");
            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        [HttpGet("/sitemap.xml")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Sitemap()
        {
            var carIds = await _cache.GetOrCreateAsync("seo:sitemap-carids", async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _context.Cars.AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => c.Id)
                    .ToListAsync();
            }) ?? new();

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            void Url(string path, string priority, string changefreq)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{BaseUrl}{path}</loc>");
                sb.AppendLine($"    <lastmod>{today}</lastmod>");
                sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
                sb.AppendLine($"    <priority>{priority}</priority>");
                sb.AppendLine("  </url>");
            }

            Url("/", "1.0", "daily");
            Url("/Compare", "0.8", "weekly");
            Url("/AiWizard", "0.8", "weekly");
            Url("/Stats", "0.6", "weekly");
            Url("/Home/Hakkimizda", "0.3", "yearly");
            Url("/Home/Iletisim", "0.3", "yearly");
            Url("/Home/Privacy", "0.2", "yearly");
            Url("/Home/Kvkk", "0.2", "yearly");
            Url("/Home/KullanimKosullari", "0.2", "yearly");
            Url("/Home/Cerez", "0.2", "yearly");

            foreach (var id in carIds)
                Url($"/Car/Details/{id}", "0.9", "weekly");

            sb.AppendLine("</urlset>");
            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
