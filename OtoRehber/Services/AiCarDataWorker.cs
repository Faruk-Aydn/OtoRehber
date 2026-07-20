using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using OtoRehber.Models;
using System.Collections.Generic;
using System.Linq;

namespace OtoRehber.Services
{
    public class AiCarDataWorker : BackgroundService
    {
        private readonly ILogger<AiCarDataWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        
        // MVP testi için örnek bir otomobil inceleme videosu
        // Örn: Bir araba inceleme videosu. Gerçekte bu dışarıdan dinamik de verilebilir.
        private const string TargetVideoUrl = "https://www.youtube.com/watch?v=FjI5uDkF5Ew"; 

        public AiCarDataWorker(
            ILogger<AiCarDataWorker> logger, 
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AI Car Data Worker başlatıldı...");

            // Gerçek projelerde bu delay çok daha uzun olur (Örn: 24 saatte bir çalış).
            // MVP test edebilmemiz için uygulama başladıktan 15 saniye sonra 1 kez çalışmasını sağlıyoruz.
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("YouTube'dan video transkripti çekiliyor...");
                    
                    var youtube = new YoutubeClient();
                    
                    string fullTranscript = "";
                    try 
                    {
                        var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(TargetVideoUrl, stoppingToken);
                        var trackInfo = trackManifest.GetByLanguage("tr"); 
                        
                        if (trackInfo != null)
                        {
                            var track = await youtube.Videos.ClosedCaptions.GetAsync(trackInfo, stoppingToken);
                            StringBuilder transcriptBuilder = new StringBuilder();
                            foreach (var caption in track.Captions)
                            {
                                transcriptBuilder.Append(caption.Text + " ");
                            }
                            fullTranscript = transcriptBuilder.ToString();
                        }
                    } 
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"YouTube'dan video çekilemedi: {ex.Message}. Yedek (Fallback) test metni kullanılıyor...");
                        fullTranscript = "Herkese merhaba, bugün konuğumuz 2023 model Toyota Corolla 1.5 Vision. Araç 1.5 litrelik atmosferik bir motora sahip, C segmenti bir sedan. Biliyorsunuz Corolla, yılların efsanesidir. Çok sağlam ve sorunsuz bir araçtır, o yüzden güvenilirlik puanı 9.5 diyebiliriz. Uzman olarak söylüyorum, fiyat performans açısından harika bir aile otomobili. Fiyatları şu an 1.2M - 1.5M TL arasında değişiyor. Tahmini yıllık bakım masrafı oldukça uygun, yaklaşık 150 Euro civarı. Aracın artılarına gelirsek; yakıt tüketimi çok makul, iç hacmi geniş ve ikinci eli çok kuvvetli. Eksileri ise; ses yalıtımı zayıf, 120km üstünde yol sesi alıyor ve malzeme kalitesi bazı rakiplerinin gerisinde. Kronik sorun olarak bazı kullanıcılar direksiyon kutusundan tıkırtı geldiğini söylüyor. Bu orta şiddette bir sorun, 2019-2021 modellerinde görülüyor ve masrafı 200 Euro civarı.";
                    }
                    
                    if (!string.IsNullOrEmpty(fullTranscript))
                    {
                        _logger.LogInformation($"Transkript hazır (Uzunluk: {fullTranscript.Length}). Gemini API'ye gönderiliyor...");

                        // AI Analizi yap
                        var carDataJson = await AnalyzeWithGeminiAsync(fullTranscript, stoppingToken);

                        if (!string.IsNullOrEmpty(carDataJson))
                        {
                            _logger.LogInformation("Gemini analizi başarılı. Veritabanına kaydediliyor...");
                            await SaveToDatabaseAsync(carDataJson, stoppingToken);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Videoda Türkçe altyazı bulunamadı.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker çalışırken hata oluştu.");
                }

                // Sadece 1 kere çalışması için delay'i çok yüksek veriyoruz
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async Task<string> AnalyzeWithGeminiAsync(string transcript, CancellationToken stoppingToken)
        {
            string apiKey = _configuration["GeminiApiKey"];
            
            if (string.IsNullOrEmpty(apiKey) || apiKey == "BURAYA_API_KEY_YAZILACAK")
            {
                _logger.LogWarning("Gemini API Key ayarlanmamış. Analiz iptal edildi.");
                return null;
            }

            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

            string prompt = $@"
Aşağıdaki YouTube otomobil inceleme videosunun transkript metnini okuyarak, incelenen aracı analiz et.
Bana SADECE JSON formatında bir veri dön. Hiçbir açıklama metni, markdown bloğu (```json) kullanma! Sadece saf JSON objesi dön.

JSON Formatı şu şekilde olmalıdır:
{{
    ""Brand"": ""Marka (Örn: Honda)"",
    ""ModelName"": ""Model (Örn: Civic)"",
    ""Engine"": ""Motor (Örn: 1.5 VTEC)"",
    ""Segment"": ""C"",
    ""ExpertSummary"": ""Uzmanın araç hakkındaki genel görüşünün 2-3 cümlelik özeti."",
    ""ReliabilityScore"": 8.5,
    ""PriceRange"": ""Örn: 1M - 1.5M TL"",
    ""EstimatedMaintenanceCostEUR"": 300,
    ""Pros"": [""Artı yön 1"", ""Artı yön 2""],
    ""Cons"": [""Eksi yön 1"", ""Eksi yön 2""],
    ""ChronicIssues"": [
        {{
            ""IssueTitle"": ""Sorun başlığı"",
            ""Description"": ""Sorun detayı"",
            ""Severity"": ""Düşük, Orta veya Kritik"",
            ""EstimatedCostEUR"": 500,
            ""AffectedYears"": ""2018-2021""
        }}
    ]
}}

Transkript Metni:
{transcript}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl, jsonContent, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(stoppingToken);
                
                // Gemini response yapısını çöz
                using var document = JsonDocument.Parse(responseString);
                var root = document.RootElement;
                
                try 
                {
                    var textResponse = root.GetProperty("candidates")[0]
                                           .GetProperty("content")
                                           .GetProperty("parts")[0]
                                           .GetProperty("text").GetString();

                    // Eğer markdown backtickleri varsa temizle
                    textResponse = textResponse.Replace("```json", "").Replace("```", "").Trim();
                    return textResponse;
                }
                catch(Exception ex)
                {
                    _logger.LogError("Gemini'den gelen JSON parse edilemedi. " + ex.Message);
                    return null;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(stoppingToken);
                _logger.LogError($"Gemini API Hatası: {response.StatusCode} - {error}");
                return null;
            }
        }

        private async Task SaveToDatabaseAsync(string jsonResult, CancellationToken stoppingToken)
        {
            try
            {
                // JsonSerializer options
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Geçici bir DTO nesnesi ile JSON'ı deserialize ediyoruz (Pros ve Cons string listesi olduğu için)
                var aiData = JsonSerializer.Deserialize<AiCarDto>(jsonResult, options);

                if (aiData != null)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<OtoRehberDbContext>();

                        // Aracı Oluştur
                        var newCar = new Car
                        {
                            Brand = aiData.Brand,
                            ModelName = aiData.ModelName,
                            Engine = aiData.Engine,
                            Segment = aiData.Segment,
                            ExpertSummary = aiData.ExpertSummary,
                            ReliabilityScore = aiData.ReliabilityScore,
                            PriceRange = aiData.PriceRange,
                            EstimatedMaintenanceCostEUR = aiData.EstimatedMaintenanceCostEUR
                        };

                        // Artıları Ekle
                        foreach (var pro in aiData.Pros ?? new List<string>())
                        {
                            newCar.ProsConsList.Add(new ProsCons { Type = "Pro", Description = pro });
                        }

                        // Eksileri Ekle
                        foreach (var con in aiData.Cons ?? new List<string>())
                        {
                            newCar.ProsConsList.Add(new ProsCons { Type = "Con", Description = con });
                        }

                        // Kronik Sorunları Ekle
                        foreach (var issue in aiData.ChronicIssues ?? new List<ChronicIssue>())
                        {
                            newCar.ChronicIssues.Add(issue);
                        }

                        dbContext.Cars.Add(newCar);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        
                        _logger.LogInformation($"[BAŞARILI] {newCar.Brand} {newCar.ModelName} aracı yapay zeka tarafından veritabanına eklendi!");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON veritabanına kaydedilirken hata oluştu.");
            }
        }

        // Gemini'den dönecek JSON formatına uygun geçici DTO
        private class AiCarDto
        {
            public string Brand { get; set; }
            public string ModelName { get; set; }
            public string Engine { get; set; }
            public string Segment { get; set; }
            public string ExpertSummary { get; set; }
            public double ReliabilityScore { get; set; }
            public string PriceRange { get; set; }
            public int EstimatedMaintenanceCostEUR { get; set; }
            public List<string> Pros { get; set; }
            public List<string> Cons { get; set; }
            public List<ChronicIssue> ChronicIssues { get; set; }
        }
    }
}
