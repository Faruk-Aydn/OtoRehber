using System.Collections.Generic;

namespace OtoRehber.Domain.Entities
{
    public class Car
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string ModelName { get; set; }
        public string ProductionYears { get; set; }
        public string Engine { get; set; }
        public string Segment { get; set; }
        public string ExpertSummary { get; set; }
        public double ReliabilityScore { get; set; }
        public int MinPrice { get; set; }
        public int MaxPrice { get; set; }
        public int EstimatedMaintenanceCostEUR { get; set; }
        public string UserFeedbackSummary { get; set; }
        public string? ImageUrl { get; set; }
        
        public List<ProsCons> ProsConsList { get; set; } = new List<ProsCons>();
        public List<ChronicIssue> ChronicIssues { get; set; } = new List<ChronicIssue>();
        public List<MileageMilestone> MileageMilestones { get; set; } = new List<MileageMilestone>();
        public List<CarReview> Reviews { get; set; } = new List<CarReview>();
        public List<CarPriceHistory> PriceHistory { get; set; } = new List<CarPriceHistory>();
    }
}
