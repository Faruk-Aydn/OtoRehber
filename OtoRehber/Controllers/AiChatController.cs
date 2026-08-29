using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;
using System.Linq;
using System.Text;
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

        public AiChatController(OtoRehberDbContext context, IAiCarDataService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public class ChatRequest
        {
            public string Message { get; set; }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            var message = request?.Message?.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new { error = "Mesaj boş olamaz." });
            }
            // Prompt injection / maliyet: kullanıcı mesajını sınırla
            if (message.Length > 2000) message = message[..2000];

            // Sadece prompt'a giren kolonlar
            var cars = await _context.Cars.AsNoTracking()
                .Select(c => new { c.Brand, c.ModelName, c.Segment, c.MinPrice, c.MaxPrice, c.ReliabilityScore })
                .ToListAsync();

            var contextBuilder = new StringBuilder();
            foreach (var car in cars)
            {
                contextBuilder.AppendLine($"- {car.Brand} {car.ModelName} ({car.Segment} Segment): Fiyat: {car.MinPrice}-{car.MaxPrice} TL, Güvenilirlik: {car.ReliabilityScore}/10");
            }

            string availableCarsContext = contextBuilder.Length > 0
                ? contextBuilder.ToString()
                : "Şu anda sistemde kayıtlı hiçbir araç yok.";

            // Yapay zekaya soruyoruz
            var responseText = await _aiService.GetCarRecommendationAsync(message, availableCarsContext);

            // Ham Markdown döndürülür; istemci tarafında marked + DOMPurify ile güvenli şekilde render edilir (_Layout).
            return Ok(new { response = responseText });
        }
    }
}
