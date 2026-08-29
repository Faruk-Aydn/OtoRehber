using System.ComponentModel.DataAnnotations;

namespace OtoRehber.Domain.Entities
{
    // Araç başına ek galeri görselleri. Kapak görseli hâlâ Car.ImageUrl.
    public class CarImage
    {
        public int Id { get; set; }
        public int CarId { get; set; }

        [Required, MaxLength(500)]
        public string Url { get; set; } = "";

        public int SortOrder { get; set; }

        public Car Car { get; set; } = null!;
    }
}
