using System.ComponentModel.DataAnnotations;

namespace OtoRehber.Domain.Entities
{
    public class ChronicIssue
    {
        public int Id { get; set; }
        public int CarId { get; set; }

        [Required, MaxLength(300)]
        public string IssueTitle { get; set; }

        [Required]
        public string Description { get; set; }

        [MaxLength(30)]
        public string Severity { get; set; } // Düşük, Orta, Kritik

        public int EstimatedCostEUR { get; set; }

        [MaxLength(60)]
        public string AffectedYears { get; set; }

        public Car Car { get; set; }
    }
}
