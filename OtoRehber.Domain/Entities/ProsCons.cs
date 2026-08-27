using System.ComponentModel.DataAnnotations;

namespace OtoRehber.Domain.Entities
{
    public class ProsCons
    {
        public int Id { get; set; }
        public int CarId { get; set; }

        // "Pro" (Artı) veya "Con" (Eksi) değerlerini alacak
        [MaxLength(10)]
        public string Type { get; set; }

        [Required, MaxLength(600)]
        public string Description { get; set; }

        public Car Car { get; set; }
    }
}
