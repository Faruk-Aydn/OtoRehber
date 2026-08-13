using System.Collections.Generic;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.DTOs
{
    public class CarDetailDto
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string ModelName { get; set; }
        public string ProductionYears { get; set; }
        public string Engine { get; set; }
        public string Segment { get; set; }
        public string ExpertSummary { get; set; }
        public string UserFeedbackSummary { get; set; }
        public double ReliabilityScore { get; set; }
        public int MinPrice { get; set; }
        public int MaxPrice { get; set; }
        public int EstimatedMaintenanceCostEUR { get; set; }
        public string? ImageUrl { get; set; }

        // Navigation properties (İdealde bunlar da kendi DTO'larına map'lenir)
        public List<ProsCons> ProsConsList { get; set; } = new List<ProsCons>();
        public List<ChronicIssue> ChronicIssues { get; set; } = new List<ChronicIssue>();
        public List<MileageMilestone> MileageMilestones { get; set; } = new List<MileageMilestone>();
        public List<CarReview> Reviews { get; set; } = new List<CarReview>();
    }
}
