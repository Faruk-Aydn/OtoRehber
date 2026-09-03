using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Services;

namespace OtoRehber.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly OtoRehberDbContext _context;
        private readonly CarScoreService _scores;
        public SearchController(OtoRehberDbContext context, CarScoreService scores)
        {
            _context = context;
            _scores = scores;
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Ok(Array.Empty<object>());

            var lower = q.Trim().ToLowerInvariant();
            var matches = await _context.Cars
                .AsNoTracking()
                .Where(c => c.Brand.ToLower().Contains(lower) || c.ModelName.ToLower().Contains(lower))
                .Take(30)
                .ToListAsync();

            var scores = await _scores.ForCarsAsync(matches);
            // Canonical Ranking → Diversity/Re-ranking (PRD v5 §3.2) → ilk 8.
            var ranked = _scores.PresentationRanking(_scores.CanonicalRanking(matches, scores));
            var results = ranked
                .Take(8)
                .Select(c => new
                {
                    id = c.Id,
                    label = c.Brand + " " + c.ModelName,
                    segment = c.Segment,
                    score = OtoRehber.Domain.Scoring.OtoRehberScore.RoundForDisplay(scores[c.Id].Overall)
                })
                .ToList();

            return Ok(results);
        }
    }
}
