using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Interfaces;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Infrastructure.Services
{
    /// <summary>
    /// YouTube AI import işlerini request thread'i dışında, tek tek işler.
    /// (Gemini rate-limit için parça aralarında bekleme burada olur, istekte değil.)
    /// </summary>
    public class YoutubeImportHostedService : BackgroundService
    {
        // HomeController.CacheKeyBrands / CacheKeyLeaderboard ile aynı olmalı.
        private const string CacheKeyBrands = "home:brands";
        private const string CacheKeyLeaderboard = "home:leaderboard";

        private readonly YoutubeImportQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<YoutubeImportHostedService> _logger;

        public YoutubeImportHostedService(
            YoutubeImportQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<YoutubeImportHostedService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var jobId in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                var job = _queue.Get(jobId);
                if (job is null) continue;

                job.State = ImportJobState.Running;
                _logger.LogInformation("YouTube import işi başladı: {JobId} ({Url})", job.Id, job.YoutubeUrl);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var ai = scope.ServiceProvider.GetRequiredService<IAiCarDataService>();

                    var cars = await ai.AnalyzeAndSaveFromYoutubeAsync(job.YoutubeUrl, stoppingToken);

                    if (cars is { Count: > 0 })
                    {
                        job.CarCount = cars.Count;
                        job.State = ImportJobState.Succeeded;
                        job.Message = $"{cars.Count} adet araç yapay zeka ile başarıyla eklendi.";

                        await WriteAuditAndInvalidateCacheAsync(scope, job, stoppingToken);
                        _logger.LogInformation("YouTube import işi tamamlandı: {JobId} → {Count} araç", job.Id, cars.Count);
                    }
                    else
                    {
                        job.State = ImportJobState.Failed;
                        job.Message = "Videodan işlenebilecek bir araç bilgisi çıkarılamadı.";
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    job.State = ImportJobState.Failed;
                    job.Message = "Uygulama kapanırken işlem durduruldu.";
                    job.CompletedUtc = DateTime.UtcNow;
                    throw;
                }
                catch (InvalidOperationException ex)
                {
                    // Transkript/altyazı yok gibi beklenen hatalar — mesajı kullanıcıya göster.
                    job.State = ImportJobState.Failed;
                    job.Message = ex.Message;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "YouTube import işi başarısız: {JobId} ({Url})", job.Id, job.YoutubeUrl);
                    job.State = ImportJobState.Failed;
                    job.Message = "İşlem sırasında beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
                }
                finally
                {
                    job.CompletedUtc = DateTime.UtcNow;
                }
            }
        }

        private static async Task WriteAuditAndInvalidateCacheAsync(IServiceScope scope, ImportJobStatus job, CancellationToken ct)
        {
            var db = scope.ServiceProvider.GetRequiredService<OtoRehberDbContext>();

            var detail = $"YouTube AI import: {job.CarCount} araç ({job.YoutubeUrl})";
            db.AuditLogs.Add(new AuditLog
            {
                UserId = job.RequestedByUserId,
                UserName = job.RequestedByUserName,
                Action = "Import",
                Entity = "Car",
                Detail = detail.Length > 1000 ? detail[..1000] : detail
            });
            await db.SaveChangesAsync(ct);

            var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            cache.Remove(CacheKeyBrands);
            cache.Remove(CacheKeyLeaderboard);
        }
    }
}
