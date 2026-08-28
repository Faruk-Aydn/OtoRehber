using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;
using OtoRehber.Domain.Interfaces;

namespace OtoRehber.Infrastructure.Services
{
    /// <summary>
    /// Bellek içi kuyruk + iş durumu deposu. Singleton kaydedilir; hem
    /// <see cref="IYoutubeImportQueue"/> (controller) hem de <see cref="Reader"/>
    /// (arka plan servisi) aynı örnek üzerinden çalışır.
    /// </summary>
    public class YoutubeImportQueue : IYoutubeImportQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        private readonly ConcurrentDictionary<Guid, ImportJobStatus> _jobs = new();

        public ChannelReader<Guid> Reader => _channel.Reader;

        public ImportJobStatus Enqueue(string youtubeUrl, string? userId, string? userName)
        {
            PruneOldJobs();

            var job = new ImportJobStatus
            {
                YoutubeUrl = youtubeUrl,
                RequestedByUserId = userId,
                RequestedByUserName = userName
            };

            _jobs[job.Id] = job;
            _channel.Writer.TryWrite(job.Id);
            return job;
        }

        public ImportJobStatus? Get(Guid id) => _jobs.TryGetValue(id, out var job) ? job : null;

        // Biten ve 1 saatten eski işleri temizle (bellek şişmesin).
        private void PruneOldJobs()
        {
            var cutoff = DateTime.UtcNow.AddHours(-1);
            foreach (var stale in _jobs.Values
                         .Where(j => j.IsFinished && j.CompletedUtc is { } c && c < cutoff)
                         .ToList())
            {
                _jobs.TryRemove(stale.Id, out _);
            }
        }
    }
}
