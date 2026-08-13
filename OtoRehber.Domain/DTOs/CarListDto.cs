namespace OtoRehber.Domain.DTOs
{
    public class CarListDto
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string ModelName { get; set; }
        public string ProductionYears { get; set; }
        public string Engine { get; set; }
        public string Segment { get; set; }
        public double ReliabilityScore { get; set; }
        public int MinPrice { get; set; }
        public int MaxPrice { get; set; }
        public string ExpertSummary { get; set; }
        public string UserFeedbackSummary { get; set; }
        public string? ImageUrl { get; set; }
    }
}
