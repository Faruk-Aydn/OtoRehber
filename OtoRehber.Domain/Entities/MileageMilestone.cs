using System.ComponentModel.DataAnnotations;

namespace OtoRehber.Domain.Entities
{
    public class MileageMilestone
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }

        [MaxLength(60)]
        public string Mileage { get; set; }

        [Required]
        public string ExpectedIssues { get; set; }

        public int EstimatedCostEUR { get; set; }
    }
}
