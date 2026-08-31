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
        public long MinPrice { get; set; }
        public long MaxPrice { get; set; }
        public int EstimatedMaintenanceCostEUR { get; set; }
        public string? ImageUrl { get; set; }

        public string? FuelType { get; set; }
        public string? Transmission { get; set; }
        public string? BodyType { get; set; }
        public string? Drivetrain { get; set; }
        public string? Condition { get; set; }
        public int? PowerHp { get; set; }
        public int? EngineDisplacementCc { get; set; }
        public int? YearStart { get; set; }
        public int? YearEnd { get; set; }
        public int? RangeKm { get; set; }
        public int? FastChargeMinutes { get; set; }

        // Navigation properties (İdealde bunlar da kendi DTO'larına map'lenir)
        public List<ProsCons> ProsConsList { get; set; } = new List<ProsCons>();
        public List<ChronicIssue> ChronicIssues { get; set; } = new List<ChronicIssue>();
        public List<MileageMilestone> MileageMilestones { get; set; } = new List<MileageMilestone>();
        public List<CarReview> Reviews { get; set; } = new List<CarReview>();
    }
}
