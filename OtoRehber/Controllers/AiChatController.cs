using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OtoRehber.Domain.Advisory;
using OtoRehber.Domain.Ai;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OtoRehber.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [EnableRateLimiting("ai")]
    public class AiChatController : ControllerBase
    {
        private readonly OtoRehberDbContext _context;
        private readonly IAiCarDataService _aiService;
        private readonly CarScoreService _scores;
        private readonly IConfiguration _configuration;

        public AiChatController(OtoRehberDbContext context, IAiCarDataService aiService,
            CarScoreService scores, IConfiguration configuration)
        {
            _context = context;
            _aiService = aiService;
            _scores = scores;
            _configuration = configuration;
        }

        public class ChatRequest
        {
            public string? Message { get; set; }
            /// <summary>Araç detay sayfasından geldiyse o aracın Id'si — bağlam duyarlı yanıt (§4.8).</summary>
            public int? CarId { get; set; }
        }

        // PRD v5 §4.7/§4.8: yapılandırılmış bağlam, sadece DB verisi, araç önermez.
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            var message = request?.Message?.Trim();
            if (string.IsNullOrWhiteSpace(message))
                return BadRequest(new { error = "Mesaj boş olamaz." });
            if (message.Length > 2000) message = message[..2000];

            string? vehicleContext = null;
            ISet<string> issueRefs = new HashSet<string>();
            ISet<string> maintRefs = new HashSet<string>();

            if (request!.CarId is > 0)
            {
                var car = await _context.Cars.AsNoTracking()
                    .Include(c => c.ChronicIssues).Include(c => c.MileageMilestones)
                    .FirstOrDefaultAsync(c => c.Id == request.CarId);
                if (car != null)
                {
                    var score = await _scores.ForCarAsync(car, car.ChronicIssues);
                    var cur = new CurrencyContext
                    {
                        EurToTry = _configuration.GetValue<double?>("Currency:EurToTry"),
                        RateDate = _configuration["Currency:RateDate"]
                    };
                    var ctx = AiContextBuilder.ForVehicle(car, score, cur);
                    vehicleContext = ctx.Text;
                    issueRefs = ctx.IssueRefs;
                    maintRefs = ctx.MaintenanceRefs;
                }
            }

            var explanation = await _aiService.AnswerQuestionAsync(message, vehicleContext, issueRefs, maintRefs);

            if (!explanation.Ok)
                return Ok(new { response = explanation.ErrorMessage ?? "AI yanıtı şu anda üretilemedi." });

            return Ok(new { response = explanation.Summary });
        }
    }
}
