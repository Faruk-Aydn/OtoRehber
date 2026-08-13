using OtoRehber.Domain.Entities;

namespace OtoRehber.Models
{
    public class CarCompareViewModel
    {
        public Car Car1 { get; set; }
        public Car Car2 { get; set; }
        public string AiVerdict { get; set; }
    }
}
