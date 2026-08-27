using System;
using System.ComponentModel.DataAnnotations;

namespace OtoRehber.Domain.Entities
{
    /// <summary>Admin işlemlerinin denetim kaydı.</summary>
    public class AuditLog
    {
        public int Id { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(256)]
        public string? UserName { get; set; }

        [Required, MaxLength(60)]
        public string Action { get; set; } = "";      // Create / Update / Delete / Import

        [MaxLength(60)]
        public string? Entity { get; set; }           // "Car" vb.

        [MaxLength(60)]
        public string? EntityId { get; set; }

        [MaxLength(1000)]
        public string? Detail { get; set; }

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
