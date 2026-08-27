using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoRehber.Domain.Entities
{
    public class CarReview
    {
        [Key]
        public int Id { get; set; }

        public int CarId { get; set; }

        [ForeignKey("CarId")]
        public Car Car { get; set; }

        /// <summary>Yorumu yapan kullanıcının Identity Id'si. Sunucuda set edilir.</summary>
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        /// <summary>Görüntülenen ad (kullanıcının e-posta yerel kısmı). Sunucuda set edilir.</summary>
        [Required]
        [StringLength(100)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Lütfen 1 ile 10 arasında bir puan verin.")]
        [Range(1, 10, ErrorMessage = "Puan 1 ile 10 arasında olmalıdır.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Yorum alanı zorunludur.")]
        [StringLength(1000)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ReviewLike> ReviewLikes { get; set; } = new List<ReviewLike>();
    }
}
