namespace OtoRehber.Models
{
    public class ChronicIssue
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string IssueTitle { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; } // Düşük, Orta, Kritik
        public int EstimatedCostEUR { get; set; }
        public string AffectedYears { get; set; }

        public Car Car { get; set; }
    }
}
