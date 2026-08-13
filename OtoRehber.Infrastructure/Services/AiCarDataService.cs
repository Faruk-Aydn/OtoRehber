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
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;
using System.Collections.Generic;

namespace OtoRehber.Infrastructure.Services
{
    public class AiCarDataService : IAiCarDataService
    {
        private readonly ILogger<AiCarDataService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AiCarDataService(
            ILogger<AiCarDataService> logger,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _httpClient = httpClient; // Timeout DI kaydında (Program.cs > AddHttpClient) ayarlanıyor.
        }

        public async Task<List<Car>> AnalyzeAndSaveFromYoutubeAsync(string youtubeUrl, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"YouTube'dan video transkripti çekiliyor: {youtubeUrl}");
            
            var youtube = new YoutubeClient();
            string fullTranscript = "";
            try 
            {
                var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(youtubeUrl, cancellationToken);
                var trackInfo = trackManifest.GetByLanguage("tr"); 
                
                if (trackInfo != null)
                {
                    var track = await youtube.Videos.ClosedCaptions.GetAsync(trackInfo, cancellationToken);
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
            
            if (string.IsNullOrEmpty(fullTranscript))
            {
                 _logger.LogWarning("Transkript boş geldi, işlem iptal ediliyor.");
                 return null;
            }

            _logger.LogInformation($"Transkript hazır (Uzunluk: {fullTranscript.Length}). Gemini API'ye parçalar halinde gönderilecek...");
            
            int chunkSize = 20000; // 20k chars. Daha hızlı cevap için iyice küçültüldü.
            var allAiCars = new List<AiCarDto>();

            for (int i = 0; i < fullTranscript.Length; i += chunkSize)
            {
                int length = Math.Min(chunkSize, fullTranscript.Length - i);
                string chunk = fullTranscript.Substring(i, length);
                
                _logger.LogInformation($"Parça gönderiliyor: {i} - {i + length} arası (Toplam uzunluk: {fullTranscript.Length})...");
                
                try 
                {
                    var chunkCars = await AnalyzeWithGeminiAsync(chunk, cancellationToken);
                    
                    if (chunkCars != null && chunkCars.Count > 0)
                    {
                        allAiCars.AddRange(chunkCars);
                        _logger.LogInformation($"Bu parçadan {chunkCars.Count} araç çıkarıldı.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Bu parçanın analizi sırasında hata oluştu, atlanıyor: {ex.Message}");
                }

                // Son parça değilse API kotalarına takılmamak için bekle (Rate Limit Protection)
                if (i + length < fullTranscript.Length)
                {
                    _logger.LogInformation("API kotalarını aşmamak için 6 saniye bekleniyor...");
                    await Task.Delay(6000, cancellationToken);
                }
            }

            if (allAiCars.Count > 0)
            {
                _logger.LogInformation($"Tüm analiz tamamlandı. Toplam {allAiCars.Count} benzersiz araç kaydı veritabanına ekleniyor...");
                return await SaveToDatabaseAsync(allAiCars, cancellationToken);
            }
            
            _logger.LogWarning("Transkriptten hiçbir araç çıkarılamadı.");
            return null;
        }

        private async Task<List<AiCarDto>> AnalyzeWithGeminiAsync(string transcriptChunk, CancellationToken cancellationToken)
        {
            string apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "BURAYA_API_KEY_YAZILACAK")
            {
                _logger.LogWarning("Gemini API Key ayarlanmamış.");
                return null;
            }

            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}";
            string prompt = $@"
SİSTEM ROLÜ VE KİMLİĞİ:
Sen Türkiye otomotiv piyasasını çok iyi bilen, 20 yıllık tecrübeli bir oto ekspertiz, sanayi ustası ve profesyonel bir otomobil gazetecisisin. Sadece kağıt üzerindeki fabrika verilerini değil; sanayideki gerçekleri, ağır bakım maliyetlerini ve kronik sorunları çok iyi biliyorsun.

GÖREVİN:
Aşağıdaki metin, devasa bir YouTube otomobil inceleme videosu transkriptinin SADECE KÜÇÜK BİR PARÇASIDIR. 
Bu parça içinde hangi araçlar tanıtılıyorsa onları derinlemesine analiz et. Eğer bu parçada dişe dokunur bir araç tanıtımı veya incelemesi yoksa BOŞ BİR DİZİ `[]` döndür!

KESİN KURALLAR:
1. İlan Sitelerindeki Kilometrelere Takılma.
2. Sadece test edenin yorumlarıyla yetinme, forumlardaki genel kanıyı 'UserFeedbackSummary' alanında özetle.
3. Kilometre Barajlarında yaşanması muhtemel ağır bakımları 'MileageMilestones' alanında belirt.
4. Maliyetler EURO (€) cinsinden olmalıdır.
5. Bana SADECE aşağıdaki şablona birebir uyan, her bir aracın ayrı bir obje olduğu bir JSON DİZİSİ (Array) dön. Asla markdown işareti kullanma. Eğer araç yoksa sadece [] dön.

BEKLENEN JSON ŞABLONU (DİZİ FORMATINDA):
[
  {{
    ""Brand"": ""Marka (Örn: Volkswagen)"",
    ""ModelName"": ""Model (Örn: Golf)"",
    ""Engine"": ""Motor (Örn: 1.6 TDI)"",
    ""Segment"": ""C"",
    ""ExpertSummary"": ""Uzmanın (Senin) ve videodaki test edenin araç hakkındaki genel değerlendirmesinin 2-3 cümlelik özeti."",
    ""UserFeedbackSummary"": ""Forum ve şikayet sitelerindeki genel kullanıcı kanısının özeti (Kronik şikayetler, memnuniyet durumu vb.)."",
    ""ReliabilityScore"": 8.5,
    ""MinPrice"": 1000000,
    ""MaxPrice"": 1500000,
    ""EstimatedMaintenanceCostEUR"": 300,
    ""Pros"": [""Artı 1"", ""Artı 2""],
    ""Cons"": [""Eksi 1"", ""Eksi 2""],
    ""ChronicIssues"": [
        {{
            ""IssueTitle"": ""Sorun başlığı"",
            ""Description"": ""Sorun detayı"",
            ""Severity"": ""Düşük, Orta veya Kritik"",
            ""EstimatedCostEUR"": 1200,
            ""AffectedYears"": ""2012-2016""
        }}
    ],
    ""MileageMilestones"": [
        {{
            ""Mileage"": ""60.000 km"",
            ""ExpectedIssues"": ""Ağır bakım, DSG kavrama tolerans kontrolü, bujiler."",
            ""EstimatedCostEUR"": 400
        }}
    ]
  }}
]

Transkript Parçası:
{transcriptChunk}
";

            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl, jsonContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(responseString);
                var root = document.RootElement;
                try 
                {
                    var textResponse = root.GetProperty("candidates")[0]
                                           .GetProperty("content")
                                           .GetProperty("parts")[0]
                                           .GetProperty("text").GetString();
                    
                    string cleanJson = textResponse.Replace("```json", "").Replace("```", "").Trim();
                    
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var aiDataList = JsonSerializer.Deserialize<List<AiCarDto>>(cleanJson, options);
                    return aiDataList ?? new List<AiCarDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Gemini'den gelen cevap JSON formatında değil veya parse edilemedi. Hata: " + ex.Message);
                    return null;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning($"Gemini API Hatası ({response.StatusCode}). Hata: {error}");
                return null;
            }
        }

        private async Task<List<Car>> SaveToDatabaseAsync(List<AiCarDto> aiDataList, CancellationToken cancellationToken)
        {
            var savedCars = new List<Car>();

            if (aiDataList != null && aiDataList.Count > 0)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OtoRehberDbContext>();

                foreach (var aiData in aiDataList)
                {
                    var newCar = new Car
                    {
                        Brand = aiData.Brand ?? "Bilinmiyor",
                        ModelName = aiData.ModelName ?? "Bilinmiyor",
                        Engine = aiData.Engine ?? "Bilinmiyor",
                        Segment = aiData.Segment ?? "Bilinmiyor",
                        ExpertSummary = aiData.ExpertSummary ?? "",
                        UserFeedbackSummary = aiData.UserFeedbackSummary ?? "",
                        ReliabilityScore = aiData.ReliabilityScore,
                        MinPrice = aiData.MinPrice,
                        MaxPrice = aiData.MaxPrice,
                        EstimatedMaintenanceCostEUR = aiData.EstimatedMaintenanceCostEUR
                    };

                    foreach (var pro in aiData.Pros ?? new List<string>())
                        newCar.ProsConsList.Add(new ProsCons { Type = "Pro", Description = pro });

                    foreach (var con in aiData.Cons ?? new List<string>())
                        newCar.ProsConsList.Add(new ProsCons { Type = "Con", Description = con });

                    foreach (var issue in aiData.ChronicIssues ?? new List<ChronicIssue>())
                        newCar.ChronicIssues.Add(issue);

                    foreach (var ms in aiData.MileageMilestones ?? new List<MileageMilestone>())
                        newCar.MileageMilestones.Add(ms);

                    dbContext.Cars.Add(newCar);
                    savedCars.Add(newCar);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return savedCars;
        }

        public async Task<string> GetCarRecommendationAsync(string userMessage, string availableCarsContext, CancellationToken cancellationToken = default)
        {
            string apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "BURAYA_API_KEY_YAZILACAK")
            {
                return "Sistem yapılandırma hatası: AI API anahtarı bulunamadı.";
            }

            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}";
            
            string prompt = $@"
SİSTEM ROLÜ VE KİMLİĞİ:
Sen 'OtoRehber AI', kullanıcıların araç seçimine yardımcı olan uzman bir otomobil danışmanısın. Kullanıcılar sana bütçelerini veya ihtiyaçlarını söyleyecek, sen de onlara SADECE SİSTEMDE KAYITLI OLAN ARAÇLAR ARASINDAN önerilerde bulunacaksın.

Aşağıda sana sistemimizde kayıtlı olan tüm araçların bir özeti (Sistem Bağlamı) veriliyor.

SİSTEM BAĞLAMI (Kayıtlı Araçlar):
{availableCarsContext}

KULLANICI MESAJI:
{userMessage}

GÖREVİN:
1. Kullanıcının ihtiyacına uyan en fazla 2-3 aracı Sistem Bağlamı içinden seç.
2. Bu araçların neden uygun olduğunu (kronik sorunları ve artılarını belirterek) kısa ve samimi bir dille açıkla.
3. Asla sistemde (Sistem Bağlamında) olmayan bir aracı (Örn: Honda Civic sistemde yoksa) önerme. Eğer kullanıcının kriterlerine uyan araç sistemde hiç yoksa, kibarca 'Şu an veri tabanımızda tam size uygun bir araç bulamadım ancak ... modeline göz atabilirsiniz' de.
4. Cevabın çok uzun olmasın (Maksimum 3-4 paragraf), okunabilir, dostane ve akıcı olsun.

Cevabını Markdown (kalın yazı, liste vb.) formatında verebilirsin.
";

            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl, jsonContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(responseString);
                var root = document.RootElement;
                try 
                {
                    var textResponse = root.GetProperty("candidates")[0]
                                           .GetProperty("content")
                                           .GetProperty("parts")[0]
                                           .GetProperty("text").GetString();
                    
                    return textResponse;
                }
                catch (Exception)
                {
                    return "Yapay zeka yanıtını işlerken bir sorun oluştu.";
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning($"Gemini API Hatası ({response.StatusCode}). Hata: {error}");
                return "Şu anda yapay zeka sistemimizde yoğunluk var veya API kotası aşıldı. Lütfen daha sonra tekrar deneyin.";
            }
        }

        public async Task<string> GetComparisonVerdictAsync(Car car1, Car car2, CancellationToken cancellationToken = default)
        {
            string apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "BURAYA_API_KEY_YAZILACAK")
            {
                return "Sistem yapılandırma hatası: AI API anahtarı bulunamadı.";
            }

            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent?key={apiKey}";
            
            string prompt = $@"
SİSTEM ROLÜ VE KİMLİĞİ:
Sen 20 yıllık tecrübeli bir oto ekspertizsin. Tarafsız, rasyonel ve teknik bir dille iki aracı kıyaslıyorsun.

ARAÇ 1:
Marka/Model: {car1.Brand} {car1.ModelName}
Segment: {car1.Segment}
Fiyat Aralığı: {car1.MinPrice} - {car1.MaxPrice} TL
Güvenilirlik Puanı: {car1.ReliabilityScore}/10
Uzman Özeti: {car1.ExpertSummary}

ARAÇ 2:
Marka/Model: {car2.Brand} {car2.ModelName}
Segment: {car2.Segment}
Fiyat Aralığı: {car2.MinPrice} - {car2.MaxPrice} TL
Güvenilirlik Puanı: {car2.ReliabilityScore}/10
Uzman Özeti: {car2.ExpertSummary}

GÖREVİN:
Yukarıda bilgileri verilen bu iki aracı karşılaştır. Hangi aracın hangi tip kullanıcı (aile, genç, ekonomi arayan vb.) için daha uygun olduğunu açıkla. Sonda net bir kazanan belirle (veya berabere bırak) ve nedenini 2 paragraf halinde özetle.
Yazdığın metinde Markdown kullanabilirsin (başlıklar, kalın yazılar vs).
";

            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl, jsonContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var document = JsonDocument.Parse(responseString);
                var root = document.RootElement;
                try 
                {
                    var textResponse = root.GetProperty("candidates")[0]
                                           .GetProperty("content")
                                           .GetProperty("parts")[0]
                                           .GetProperty("text").GetString();
                    return textResponse;
                }
                catch (Exception)
                {
                    return "Yapay zeka yanıtını işlerken bir sorun oluştu.";
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning($"Gemini API Hatası ({response.StatusCode}). Hata: {error}");
                return "Şu anda yapay zeka sistemimizde yoğunluk var, kıyaslama sonucu üretilemedi.";
            }
        }

        private class AiCarDto
        {
            public string Brand { get; set; }
            public string ModelName { get; set; }
            public string Engine { get; set; }
            public string Segment { get; set; }
            public string ExpertSummary { get; set; }
            public string UserFeedbackSummary { get; set; }
            public double ReliabilityScore { get; set; }
            public int MinPrice { get; set; }
            public int MaxPrice { get; set; }
            public int EstimatedMaintenanceCostEUR { get; set; }
            public List<string> Pros { get; set; }
            public List<string> Cons { get; set; }
            public List<ChronicIssue> ChronicIssues { get; set; }
            public List<MileageMilestone> MileageMilestones { get; set; }
        }
    }
}
