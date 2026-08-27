using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtoRehber.Domain.Interfaces;

namespace OtoRehber.Infrastructure.Services
{
    public class ResendEmailOptions
    {
        public string ApiKey { get; set; } = "";
        public string FromEmail { get; set; } = "OtoRehber <onboarding@resend.dev>";
    }

    /// <summary>
    /// Resend (https://resend.com) HTTP API üzerinden e-posta gönderir.
    /// ApiKey verilmemişse hiçbir istek yapmaz; içeriği (link dahil) log'a yazar —
    /// böylece yerel geliştirme ve ilk kurulum e-posta servisi olmadan da çalışır.
    /// </summary>
    public class ResendEmailSender : IAppEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly ResendEmailOptions _options;
        private readonly ILogger<ResendEmailSender> _logger;

        public ResendEmailSender(HttpClient httpClient, IOptions<ResendEmailOptions> options, ILogger<ResendEmailSender> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _logger.LogWarning(
                    "Resend:ApiKey ayarlı değil. E-posta GÖNDERİLMEDİ. Alıcı: {To} | Konu: {Subject}\n{Body}",
                    toEmail, subject, htmlBody);
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = JsonContent.Create(new
                {
                    from = _options.FromEmail,
                    to = new[] { toEmail },
                    subject,
                    html = htmlBody
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Resend e-posta hatası ({Status}): {Body}", response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resend e-posta gönderilemedi. Alıcı: {To}", toEmail);
            }
        }
    }
}
