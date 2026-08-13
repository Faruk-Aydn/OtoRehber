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
    [Authorize]
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
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest(new { error = "Mesaj boş olamaz." });
            }

            // Veritabanındaki araçların kısa bir özetini çekiyoruz
            var cars = await _context.Cars.ToListAsync();
            
            var contextBuilder = new StringBuilder();
            foreach (var car in cars)
            {
                contextBuilder.AppendLine($"- {car.Brand} {car.ModelName} ({car.Segment} Segment): Fiyat: {car.MinPrice}-{car.MaxPrice} TL, Güvenilirlik: {car.ReliabilityScore}/10");
            }

            string availableCarsContext = contextBuilder.ToString();
            
            if (string.IsNullOrWhiteSpace(availableCarsContext))
            {
                availableCarsContext = "Şu anda sistemde kayıtlı hiçbir araç yok.";
            }

            // Yapay zekaya soruyoruz
            var responseText = await _aiService.GetCarRecommendationAsync(request.Message, availableCarsContext);

            // Gelen cevaptaki Markdown yıldızlarını HTML'e veya JS'in daha kolay parse edebileceği bir şeye çevirebiliriz (isteğe bağlı)
            // Ama frontend'de marked.js kullanmak daha profesyoneldir. Biz şimdilik olduğu gibi dönüyoruz.
            return Ok(new { response = responseText });
        }
    }
}
