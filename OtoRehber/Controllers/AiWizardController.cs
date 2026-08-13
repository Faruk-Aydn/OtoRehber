using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;
using System.Text;
using System.Threading.Tasks;

namespace OtoRehber.Controllers
{
    [Authorize]
    [EnableRateLimiting("ai")]
    public class AiWizardController : Controller
    {
        private readonly OtoRehberDbContext _context;
        private readonly IAiCarDataService _aiService;

        public AiWizardController(OtoRehberDbContext context, IAiCarDataService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Analyze(string budget, string familySize, string usageType, string priority)
        {
            if (string.IsNullOrWhiteSpace(budget) || string.IsNullOrWhiteSpace(familySize))
            {
                ViewBag.Error = "Lütfen gerekli alanları doldurun.";
                return View("Index");
            }

            // Veritabanındaki araçların kısa bir özetini çekiyoruz
            var cars = await _context.Cars.ToListAsync();
            
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Aşağıdaki araçlar veritabanımızda mevcut. Eğer kullanıcının profiline uygunlarsa, lütfen özellikle bunlardan öner. Eğer veritabanında uyan yoksa, genel piyasadan en mantıklı 3 aracı öner ve nedenlerini açıkla.");
            contextBuilder.AppendLine("Veritabanı Araçları:");
            foreach (var car in cars)
            {
                contextBuilder.AppendLine($"- {car.Brand} {car.ModelName} ({car.Segment} Segment): Fiyat: {car.MinPrice}-{car.MaxPrice} TL, Güvenilirlik: {car.ReliabilityScore}/10");
            }

            string availableCarsContext = contextBuilder.ToString();

            // Kullanıcının profilinden gelen prompt
            string userPrompt = $"Merhaba, araç satın almayı düşünüyorum. Bütçem: {budget}. Aile durumum: {familySize}. Kullanım amacım çoğunlukla: {usageType}. Bir araçta benim için en önemli kriter: {priority}. Bana en uygun 3 aracı (mümkünse veritabanındaki araçlardan, değilse genel piyasadan) tavsiye edebilir misin? Tavsiyelerini yaparken artılarını ve eksilerini detaylıca anlatır mısın?";

            // Yapay zekadan tavsiye al
            var responseText = await _aiService.GetCarRecommendationAsync(userPrompt, availableCarsContext);

            // Sonucu göstermek için modeli aktar
            ViewBag.AiResponse = responseText;
            
            return View("Result");
        }
    }
}
