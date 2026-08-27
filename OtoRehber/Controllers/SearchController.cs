using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly OtoRehberDbContext _context;
        public SearchController(OtoRehberDbContext context) { _context = context; }

        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Ok(Array.Empty<object>());

            var lower = q.Trim().ToLowerInvariant();
            var results = await _context.Cars
                .AsNoTracking()
                .Where(c => c.Brand.ToLower().Contains(lower) || c.ModelName.ToLower().Contains(lower))
                .OrderByDescending(c => c.ReliabilityScore)
                .Take(8)
                .Select(c => new
                {
                    id = c.Id,
                    label = c.Brand + " " + c.ModelName,
                    segment = c.Segment,
                    score = c.ReliabilityScore
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
