namespace OtoRehber.Models
{
    public class Car
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string ModelName { get; set; }
        public string Engine { get; set; }
        public string Segment { get; set; }
        public string ExpertSummary { get; set; }
        public double ReliabilityScore { get; set; }
        public string PriceRange { get; set; }
        public int EstimatedMaintenanceCostEUR { get; set; }
        public List<ProsCons> ProsConsList { get; set; } = new List<ProsCons>();
        public List<ChronicIssue> ChronicIssues { get; set; } = new List<ChronicIssue>();

    }
}
