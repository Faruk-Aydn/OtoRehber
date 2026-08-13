using System;

namespace OtoRehber.Domain.Entities
{
    public class CarPriceHistory
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public int Price { get; set; }
        public DateTime RecordedAt { get; set; }

        public Car Car { get; set; }
    }
}
