using System;

namespace OtoRehber.Domain.Interfaces
{
    public enum ImportJobState
    {
        Pending,
        Running,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Bir YouTube AI import işinin durumu. Bellekte tutulur (admin-only, tek instance).
    /// </summary>
    public class ImportJobStatus
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string YoutubeUrl { get; init; } = "";
        public string? RequestedByUserId { get; init; }
        public string? RequestedByUserName { get; init; }

        public ImportJobState State { get; set; } = ImportJobState.Pending;
        public string? Message { get; set; }
        public int CarCount { get; set; }

        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
        public DateTime? CompletedUtc { get; set; }

        public bool IsFinished => State == ImportJobState.Succeeded || State == ImportJobState.Failed;
    }

    /// <summary>
    /// YouTube AI import işlerini arka plan servisine kuyruklar.
    /// </summary>
    public interface IYoutubeImportQueue
    {
        ImportJobStatus Enqueue(string youtubeUrl, string? userId, string? userName);
        ImportJobStatus? Get(Guid id);
    }
}
