using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.DTOs;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Mappings
{
    /// <summary>
    /// Car &lt;-&gt; DTO dönüşümleri. (AutoMapper yerine elle yazıldı — küçük ve sabit
    /// bir şekil olduğu için bağımlılık + CVE + lisans yükü taşımaya değmiyor.)
    /// </summary>
    public static class CarMappings
    {
        public static CarListDto ToListDto(this Car c) => new()
        {
            Id = c.Id,
            Brand = c.Brand,
            ModelName = c.ModelName,
            ProductionYears = c.ProductionYears,
            Engine = c.Engine,
            Segment = c.Segment,
            ReliabilityScore = c.ReliabilityScore,
            MinPrice = c.MinPrice,
            MaxPrice = c.MaxPrice,
            ExpertSummary = c.ExpertSummary,
            UserFeedbackSummary = c.UserFeedbackSummary,
            ImageUrl = c.ImageUrl,
            FuelType = c.FuelType,
            Transmission = c.Transmission,
            BodyType = c.BodyType,
            PowerHp = c.PowerHp,
            EngineDisplacementCc = c.EngineDisplacementCc
        };

        public static List<CarListDto> ToListDto(this IEnumerable<Car> cars)
            => cars.Select(ToListDto).ToList();

        public static CarDetailDto ToDetailDto(this Car c) => new()
        {
            Id = c.Id,
            Brand = c.Brand,
            ModelName = c.ModelName,
            ProductionYears = c.ProductionYears,
            Engine = c.Engine,
            Segment = c.Segment,
            ExpertSummary = c.ExpertSummary,
            UserFeedbackSummary = c.UserFeedbackSummary,
            ReliabilityScore = c.ReliabilityScore,
            MinPrice = c.MinPrice,
            MaxPrice = c.MaxPrice,
            EstimatedMaintenanceCostEUR = c.EstimatedMaintenanceCostEUR,
            ImageUrl = c.ImageUrl,
            FuelType = c.FuelType,
            Transmission = c.Transmission,
            BodyType = c.BodyType,
            Drivetrain = c.Drivetrain,
            Condition = c.Condition,
            PowerHp = c.PowerHp,
            EngineDisplacementCc = c.EngineDisplacementCc,
            YearStart = c.YearStart,
            YearEnd = c.YearEnd,
            RangeKm = c.RangeKm,
            FastChargeMinutes = c.FastChargeMinutes,
            ProsConsList = c.ProsConsList ?? new List<ProsCons>(),
            ChronicIssues = c.ChronicIssues ?? new List<ChronicIssue>(),
            MileageMilestones = c.MileageMilestones ?? new List<MileageMilestone>(),
            Reviews = c.Reviews ?? new List<CarReview>()
        };

        public static CarCreateDto ToCreateDto(this Car c) => new()
        {
            Id = c.Id,
            Brand = c.Brand,
            ModelName = c.ModelName,
            Engine = c.Engine,
            Segment = c.Segment,
            ExpertSummary = c.ExpertSummary,
            ReliabilityScore = c.ReliabilityScore,
            MinPrice = c.MinPrice,
            MaxPrice = c.MaxPrice,
            EstimatedMaintenanceCostEUR = c.EstimatedMaintenanceCostEUR,
            ImageUrl = c.ImageUrl,
            FuelType = c.FuelType,
            Transmission = c.Transmission,
            BodyType = c.BodyType,
            Drivetrain = c.Drivetrain,
            Condition = c.Condition,
            PowerHp = c.PowerHp,
            EngineDisplacementCc = c.EngineDisplacementCc,
            YearStart = c.YearStart,
            YearEnd = c.YearEnd
        };

        public static Car ToEntity(this CarCreateDto d)
        {
            var car = new Car();
            d.ApplyTo(car);
            return car;
        }

        /// <summary>
        /// DTO'daki alanları mevcut entity üzerine uygular. DTO'da olmayan alanlar
        /// (ProductionYears, UserFeedbackSummary, navigation'lar) ve Id korunur.
        /// </summary>
        public static void ApplyTo(this CarCreateDto d, Car car)
        {
            car.Brand = d.Brand;
            car.ModelName = d.ModelName;
            car.Engine = d.Engine;
            car.Segment = d.Segment;
            car.ExpertSummary = d.ExpertSummary;
            car.ReliabilityScore = d.ReliabilityScore;
            car.MinPrice = d.MinPrice;
            car.MaxPrice = d.MaxPrice;
            car.EstimatedMaintenanceCostEUR = d.EstimatedMaintenanceCostEUR;
            car.ImageUrl = d.ImageUrl;
            car.FuelType = Blank(d.FuelType);
            car.Transmission = Blank(d.Transmission);
            car.BodyType = Blank(d.BodyType);
            car.Drivetrain = Blank(d.Drivetrain);
            car.Condition = Blank(d.Condition);
            car.PowerHp = d.PowerHp is > 0 ? d.PowerHp : null;
            car.EngineDisplacementCc = d.EngineDisplacementCc is > 0 ? d.EngineDisplacementCc : null;
            car.YearStart = d.YearStart;
            car.YearEnd = d.YearEnd;
        }

        private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
