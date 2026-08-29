using System.Collections.Generic;

namespace OtoRehber.Infrastructure.Data.CatalogSeed
{
    /// <summary>JSON katalog dosyalarındaki bir araç varyantı (motor + şanzıman kombinasyonu ayrı kayıt).</summary>
    public class CatalogCar
    {
        public string Brand { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string ProductionYears { get; set; } = "";
        public string Engine { get; set; } = "";
        public string Segment { get; set; } = "";
        public double ReliabilityScore { get; set; }
        public long MinPrice { get; set; }
        public long MaxPrice { get; set; }
        public int EstimatedMaintenanceCostEUR { get; set; }
        public string ExpertSummary { get; set; } = "";
        public string UserFeedbackSummary { get; set; } = "";
        public string? ImageUrl { get; set; }

        public List<CatalogChronicIssue> ChronicIssues { get; set; } = new();
        public List<string> Pros { get; set; } = new();
        public List<string> Cons { get; set; } = new();
        public List<CatalogMilestone> Milestones { get; set; } = new();
    }

    public class CatalogChronicIssue
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "Orta"; // Düşük | Orta | Kritik
        public int EstimatedCostEUR { get; set; }
        public string AffectedYears { get; set; } = "";
    }

    public class CatalogMilestone
    {
        public string Mileage { get; set; } = "";
        public string ExpectedIssues { get; set; } = "";
        public int EstimatedCostEUR { get; set; }
    }
}
