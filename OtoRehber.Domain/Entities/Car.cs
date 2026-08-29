using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OtoRehber.Domain.Entities
{
    public class Car
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Brand { get; set; }

        [Required, MaxLength(100)]
        public string ModelName { get; set; }

        [MaxLength(40)]
        public string ProductionYears { get; set; }

        [MaxLength(120)]
        public string Engine { get; set; }

        [MaxLength(60)]
        public string Segment { get; set; }

        public string ExpertSummary { get; set; }

        public double ReliabilityScore { get; set; }
        public long MinPrice { get; set; }
        public long MaxPrice { get; set; }
        public int EstimatedMaintenanceCostEUR { get; set; }

        public string UserFeedbackSummary { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public List<ProsCons> ProsConsList { get; set; } = new List<ProsCons>();
        public List<ChronicIssue> ChronicIssues { get; set; } = new List<ChronicIssue>();
        public List<MileageMilestone> MileageMilestones { get; set; } = new List<MileageMilestone>();
        public List<CarReview> Reviews { get; set; } = new List<CarReview>();
        public List<CarPriceHistory> PriceHistory { get; set; } = new List<CarPriceHistory>();
        public List<CarImage> Images { get; set; } = new List<CarImage>();
    }
}
