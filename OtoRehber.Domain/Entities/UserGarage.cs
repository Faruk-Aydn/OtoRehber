using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoRehber.Domain.Entities
{
    public class UserGarage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Microsoft.AspNetCore.Identity.IdentityUser Id'si ile eşleşir

        [Required]
        public int CarId { get; set; }

        [ForeignKey("CarId")]
        public Car Car { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
