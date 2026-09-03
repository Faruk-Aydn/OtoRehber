using System.Collections.Generic;
using OtoRehber.Domain.Advisory;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Listing;
using Xunit;

namespace OtoRehber.Tests;

/// <summary>Session 5 — İlan Analizi kural motoru (PRD v5 §5). Renk/verdict yok, tahmin yok.</summary>
public class ListingAnalyzerTests
{
    private static Car Car(long min = 800_000, long max = 1_200_000, int yearStart = 2013)
        => new()
        {
            Id = 1, Brand = "Toyota", ModelName = "Corolla (E170)", Engine = "1.6",
            MinPrice = min, MaxPrice = max, YearStart = yearStart,
            ChronicIssues = new List<ChronicIssue> { new() { Id = 1, IssueTitle = "CVT", Severity = "Orta", EstimatedCostEUR = 800 } },
            MileageMilestones = new List<MileageMilestone>
            {
                new() { Id = 1, Mileage = "100.000 km", ExpectedIssues = "CVT yağı", EstimatedCostEUR = 300 }
            }
        };

    // --- Fiyat (§5.1/1) ---
    [Fact]
    public void Price_WithinBand_ReportsPercentPosition()
    {
        var r = ListingAnalyzer.Analyze(Car(), new ListingInput { CarId = 1, Price = 1_000_000 }, currentYear: 2026);
        Assert.Equal(DataCoverage.Full, r.Price.Coverage);
        Assert.Contains("%50", r.Price.Message); // (1.0M - 0.8M) / (1.2M - 0.8M) = %50
    }

    [Fact]
    public void Price_BelowBand_SaysBelow()
    {
        var r = ListingAnalyzer.Analyze(Car(), new ListingInput { CarId = 1, Price = 500_000 }, 2026);
        Assert.Contains("altında", r.Price.Message);
    }

    [Fact]
    public void Price_NoBand_CoverageNone_NoInvention()
    {
        var r = ListingAnalyzer.Analyze(Car(min: 0, max: 0), new ListingInput { CarId = 1, Price = 900_000 }, 2026);
        Assert.Equal(DataCoverage.None, r.Price.Coverage);
        Assert.DoesNotContain("%", r.Price.Message);
    }

    [Fact]
    public void Price_NoListingPrice_Partial()
    {
        var r = ListingAnalyzer.Analyze(Car(), new ListingInput { CarId = 1 }, 2026);
        Assert.Equal(DataCoverage.Partial, r.Price.Coverage);
    }

    // --- Kilometre (§5.1/2) ---
    [Fact]
    public void Mileage_WithYear_ComputesExpectedRange()
    {
        // 2016 model, 2026 → 10 yıl × 15.000 = 150.000 ± %25 → 112.500 – 187.500
        var r = ListingAnalyzer.Analyze(Car(), new ListingInput { CarId = 1, Year = 2016, Mileage = 145_000 }, 2026, annualKm: 15_000);
        Assert.Equal(DataCoverage.Full, r.Mileage.Coverage);
        Assert.Equal(112_500, r.Mileage.ExpectedMin);
        Assert.Equal(187_500, r.Mileage.ExpectedMax);
        Assert.Contains("beklenen aralıkta", r.Mileage.Message);
    }

    [Fact]
    public void Mileage_NoYear_Partial_NoExpectedRange()
    {
        var r = ListingAnalyzer.Analyze(Car(yearStart: 0), new ListingInput { CarId = 1, Mileage = 145_000 }, 2026);
        Assert.Equal(DataCoverage.Partial, r.Mileage.Coverage);
        Assert.Null(r.Mileage.ExpectedMin);
    }

    [Fact]
    public void Mileage_NoKm_CoverageNone()
    {
        var r = ListingAnalyzer.Analyze(Car(), new ListingInput { CarId = 1, Year = 2016 }, 2026);
        Assert.Equal(DataCoverage.None, r.Mileage.Coverage);
    }

    [Fact]
    public void Mileage_ComparesToMilestones()
    {
        var r = ListingAnalyzer.Analyze(Car(), new ListingInput { CarId = 1, Year = 2016, Mileage = 145_000 }, 2026);
        Assert.Single(r.Maintenance);
        Assert.Equal(MilestoneStatus.Passed, r.Maintenance[0].Status); // 100k ≪ 145k
    }

    // --- Hasar/boya (§5.1/5) — sadece kullanıcı girdisi ---
    [Fact]
    public void Damage_NotEntered_CoverageNone_TellsUserToCheck()
    {
        var r = ListingAnalyzer.Analyze(Car(), new ListingInput { CarId = 1 }, 2026);
        Assert.Equal(DataCoverage.None, r.Damage.Coverage);
        Assert.Contains("tahmin edilmez", r.Damage.Message);
    }

    [Fact]
    public void Damage_Entered_ReflectsUserDeclaration()
    {
        var r = ListingAnalyzer.Analyze(Car(),
            new ListingInput { CarId = 1, HasDamageRecord = true, PaintedPanels = 3 }, 2026);
        Assert.Equal(DataCoverage.Full, r.Damage.Coverage);
        Assert.Contains("3 boyalı", r.Damage.Message);
    }

    // --- Kronik + kontrol listesi (§5.1/3, 6 — Session 2 mantığı tekrar) ---
    [Fact]
    public void ChronicIssues_AndChecklist_ComeFromCar()
    {
        var r = ListingAnalyzer.Analyze(Car(), new ListingInput { CarId = 1 }, 2026);
        Assert.Single(r.ChronicIssues);
        Assert.Contains(r.Checklist, i => i.FromChronicIssue && i.Text.Contains("CVT"));
        Assert.Contains(r.Checklist, i => !i.FromChronicIssue); // sabit maddeler
    }
}
