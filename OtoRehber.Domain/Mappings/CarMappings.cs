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
            ImageUrl = c.ImageUrl
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
            ImageUrl = c.ImageUrl
        };

        public static Car ToEntity(this CarCreateDto d) => new()
        {
            Brand = d.Brand,
            ModelName = d.ModelName,
            Engine = d.Engine,
            Segment = d.Segment,
            ExpertSummary = d.ExpertSummary,
            ReliabilityScore = d.ReliabilityScore,
            MinPrice = d.MinPrice,
            MaxPrice = d.MaxPrice,
            EstimatedMaintenanceCostEUR = d.EstimatedMaintenanceCostEUR,
            ImageUrl = d.ImageUrl
        };

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
        }
    }
}
