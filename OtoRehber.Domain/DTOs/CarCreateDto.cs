using System.ComponentModel.DataAnnotations;

namespace OtoRehber.Domain.DTOs
{
    public class CarCreateDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Marka zorunludur.")]
        [StringLength(50)]
        public string Brand { get; set; }

        [Required(ErrorMessage = "Model adı zorunludur.")]
        [StringLength(100)]
        public string ModelName { get; set; }

        [Required(ErrorMessage = "Motor bilgisi zorunludur.")]
        public string Engine { get; set; }

        [Required(ErrorMessage = "Segment zorunludur.")]
        public string Segment { get; set; }

        public string ExpertSummary { get; set; }

        [Required]
        [Range(1, 10)]
        public double ReliabilityScore { get; set; }

        [Required]
        [Range(100000, 50000000)]
        public long MinPrice { get; set; }

        [Required]
        [Range(100000, 50000000)]
        public long MaxPrice { get; set; }

        [Range(0, 10000, ErrorMessage = "Geçerli bir bakım maliyeti girin.")]
        public int EstimatedMaintenanceCostEUR { get; set; }

        public string? ImageUrl { get; set; }

        // Yapılandırılmış özellikler (opsiyonel)
        public string? FuelType { get; set; }
        public string? Transmission { get; set; }
        public string? BodyType { get; set; }
        public string? Drivetrain { get; set; }
        public string? Condition { get; set; }
        [Range(0, 2000)] public int? PowerHp { get; set; }
        [Range(0, 9000)] public int? EngineDisplacementCc { get; set; }
        [Range(1950, 2100)] public int? YearStart { get; set; }
        [Range(1950, 2100)] public int? YearEnd { get; set; }
    }
}
