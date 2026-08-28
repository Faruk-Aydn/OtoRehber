using System.Collections.Generic;
using OtoRehber.Domain.DTOs;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Mappings;
using Xunit;

namespace OtoRehber.Tests;

public class CarMappingsTests
{
    private static Car SampleCar() => new()
    {
        Id = 7,
        Brand = "Toyota",
        ModelName = "Corolla",
        ProductionYears = "2019-2024",
        Engine = "1.6 Benzin",
        Segment = "C",
        ExpertSummary = "özet",
        UserFeedbackSummary = "kullanıcı özeti",
        ReliabilityScore = 9.2,
        MinPrice = 900_000,
        MaxPrice = 1_400_000,
        EstimatedMaintenanceCostEUR = 300,
        ImageUrl = "/images/cars/corolla.jpg",
        ProsConsList = { new ProsCons { Type = "Pro", Description = "dayanıklı" } },
        ChronicIssues = { new ChronicIssue { IssueTitle = "yok" } },
    };

    [Fact]
    public void ToListDto_CopiesScalarFields()
    {
        var dto = SampleCar().ToListDto();

        Assert.Equal(7, dto.Id);
        Assert.Equal("Toyota", dto.Brand);
        Assert.Equal("2019-2024", dto.ProductionYears);
        Assert.Equal(1_400_000, dto.MaxPrice);
        Assert.Equal("kullanıcı özeti", dto.UserFeedbackSummary);
    }

    [Fact]
    public void ToDetailDto_IncludesNavigationCollections()
    {
        var dto = SampleCar().ToDetailDto();

        Assert.Single(dto.ProsConsList);
        Assert.Single(dto.ChronicIssues);
        Assert.Equal(300, dto.EstimatedMaintenanceCostEUR);
    }

    [Fact]
    public void ApplyTo_UpdatesScalars_ButPreservesIdAndFieldsNotOnDto()
    {
        var existing = SampleCar();
        var dto = new CarCreateDto
        {
            Id = 999,                 // görmezden gelinmeli
            Brand = "Honda",
            ModelName = "Civic",
            Engine = "1.5 Turbo",
            Segment = "C",
            ExpertSummary = "yeni özet",
            ReliabilityScore = 8.0,
            MinPrice = 1_000_000,
            MaxPrice = 1_500_000,
            EstimatedMaintenanceCostEUR = 350,
            ImageUrl = "/images/cars/civic.jpg"
        };

        dto.ApplyTo(existing);

        Assert.Equal(7, existing.Id);                          // korunur
        Assert.Equal("Honda", existing.Brand);                 // güncellenir
        Assert.Equal("2019-2024", existing.ProductionYears);   // DTO'da yok → korunur
        Assert.Equal("kullanıcı özeti", existing.UserFeedbackSummary); // DTO'da yok → korunur
        Assert.Single(existing.ProsConsList);                  // nav'lar korunur
    }

    [Fact]
    public void ToEntity_MapsCreatableFields()
    {
        var dto = new CarCreateDto
        {
            Brand = "Ford", ModelName = "Focus", Engine = "1.5", Segment = "C",
            ReliabilityScore = 7.5, MinPrice = 800_000, MaxPrice = 1_100_000,
            EstimatedMaintenanceCostEUR = 320
        };

        var car = dto.ToEntity();

        Assert.Equal(0, car.Id);
        Assert.Equal("Ford", car.Brand);
        Assert.Equal(1_100_000, car.MaxPrice);
    }
}
