using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.Advisory;
using OtoRehber.Domain.Entities;
using Xunit;

namespace OtoRehber.Tests;

/// <summary>Session 2 kural bazlı katman (PRD v5 §2.2–2.6) birim testleri.</summary>
public class AdvisoryTests
{
    // --- §2.2 OtoRehber Değerlendirmesi ---
    [Theory]
    [InlineData(9.1, "Genel olarak mantıklı")]
    [InlineData(8.0, "Genel olarak mantıklı")]
    [InlineData(7.9, "Dikkatli incelenmeli")]
    [InlineData(6.5, "Dikkatli incelenmeli")]
    [InlineData(6.4, "Riskli")]
    [InlineData(5.0, "Riskli")]
    [InlineData(4.9, "Genel olarak önerilmiyor")]
    public void Verdict_ThresholdBands(double score, string expected)
        => Assert.Equal(expected, OtoRehberVerdict.FromScore(score).Label);

    [Fact]
    public void Verdict_NullScore_IsUnknown()
    {
        var v = OtoRehberVerdict.FromScore(null);
        Assert.Equal(VerdictTone.Unknown, v.Tone);
    }

    [Fact]
    public void Verdict_HasNoBannedPhrases()
    {
        string[] banned = { "Alınabilir", "Kesin alınır", "Kesinlikle alınmalı" };
        foreach (var s in new double?[] { 9, 7, 6, 3, null })
            Assert.DoesNotContain(banned, b => OtoRehberVerdict.FromScore(s).Label.Contains(b));
    }

    // --- §2.3 Kimlere uygun ---
    [Fact]
    public void Suitability_CleanFamilyCar_SuggestsFamily_NotBeginnerBlocked()
    {
        var car = new Car { Segment = "C", PowerHp = 110, FuelType = "Benzin", EstimatedMaintenanceCostEUR = 250 };
        var r = SuitabilityRules.Evaluate(car);
        Assert.Contains(r.SuitableFor, x => x.Audience.Contains("Aile"));
        Assert.DoesNotContain(r.NotSuitableFor, x => x.Audience.Contains("İlk araç"));
    }

    [Fact]
    public void Suitability_CriticalIssue_BlocksBeginnersAndTightBudget_AndNoFamily()
    {
        var car = new Car
        {
            Segment = "C", PowerHp = 120, FuelType = "Dizel", EstimatedMaintenanceCostEUR = 300,
            ChronicIssues = new List<ChronicIssue> { new() { IssueTitle = "DSG", Severity = "Kritik" } }
        };
        var r = SuitabilityRules.Evaluate(car);
        Assert.Contains(r.NotSuitableFor, x => x.Audience.Contains("İlk araç"));
        Assert.Contains(r.NotSuitableFor, x => x.Audience.Contains("sınırlı bütçe"));
        Assert.DoesNotContain(r.SuitableFor, x => x.Audience.Contains("Aile"));
    }

    [Fact]
    public void Suitability_HighPower_SuggestsPerformance_BlocksBeginners()
    {
        var car = new Car { Segment = "D", PowerHp = 250, FuelType = "Benzin", EstimatedMaintenanceCostEUR = 400 };
        var r = SuitabilityRules.Evaluate(car);
        Assert.Contains(r.SuitableFor, x => x.Audience.Contains("Performans"));
        Assert.Contains(r.NotSuitableFor, x => x.Audience.Contains("İlk araç"));
    }

    // --- §2.5 Km bazlı bakım ---
    [Theory]
    [InlineData("60.000 km", 60000)]
    [InlineData("120.000 km", 120000)]
    [InlineData("150k km", 150000)]
    [InlineData("90 bin km", 90000)]
    [InlineData("belirsiz", null)]
    public void MileageParse(string text, int? expected)
        => Assert.Equal(expected, MileageAdvisor.ParseKm(text));

    [Fact]
    public void MileageCheck_ClassifiesByUserKm()
    {
        var ms = new List<MileageMilestone>
        {
            new() { Mileage = "40.000 km", ExpectedIssues = "a", EstimatedCostEUR = 100 },
            new() { Mileage = "100.000 km", ExpectedIssues = "b", EstimatedCostEUR = 300 },
            new() { Mileage = "160.000 km", ExpectedIssues = "c", EstimatedCostEUR = 500 },
        };
        var checks = MileageAdvisor.Check(ms, 100_000);
        Assert.Equal(MilestoneStatus.Passed, checks[0].Status);   // 40k ≪ 100k
        Assert.Equal(MilestoneStatus.Due, checks[1].Status);      // 100k ≈ now
        Assert.Equal(MilestoneStatus.Far, checks[2].Status);      // 160k ≫ 100k+30k
    }

    // --- §2.6 Kontrol listesi ---
    [Fact]
    public void Checklist_ChronicIssuesFirst_ThenFixedItems()
    {
        var car = new Car
        {
            ChronicIssues = new List<ChronicIssue>
            {
                new() { IssueTitle = "Turbo", Severity = "Kritik" },
                new() { IssueTitle = "EGR", Severity = "Orta" },
            }
        };
        var items = PurchaseChecklist.Build(car);
        Assert.True(items[0].FromChronicIssue);
        Assert.Contains("Turbo", items[0].Text);
        Assert.Equal(PurchaseChecklist.FixedItems.Length + 2, items.Count);
        Assert.False(items[^1].FromChronicIssue);
    }

    // --- §2.4 € → TL ---
    [Fact]
    public void Currency_NoRate_ShowsEuro()
    {
        var c = new CurrencyContext { EurToTry = null };
        Assert.Contains("€", c.Eur(300));
        Assert.Null(c.RateNote);
    }

    [Fact]
    public void Currency_WithRate_ConvertsAndNotes()
    {
        var c = new CurrencyContext { EurToTry = 47.5, RateDate = "3 Eylül 2026" };
        Assert.Contains("₺", c.Eur(300));
        Assert.Contains("14", c.Eur(300)); // 300 * 47.5 = 14.250
        Assert.Contains("2026", c.RateNote);
    }
}
