using System;
using System.Linq;
using OtoRehber.Domain.Scoring;
using Xunit;

namespace OtoRehber.Tests;

/// <summary>
/// Canonical OtoRehber Skoru (PRD v5 §1.2) birim testleri: determinism, ağırlık toplamı,
/// eksik bileşen politikası, orantılı normalize, alt skor türetme kuralları.
/// </summary>
public class OtoRehberScoreTests
{
    private static ScoreResult Calc(
        double reliability = 8.0,
        string[]? severities = null,
        int maintEur = 300,
        int reviewCount = 0,
        double? avgRating = null,
        int threshold = 10)
        => OtoRehberScore.Calculate(reliability, severities ?? Array.Empty<string>(), maintEur, reviewCount, avgRating, threshold);

    [Fact]
    public void Weights_SumToOne()
    {
        var sum = ScoreWeights.Reliability + ScoreWeights.ChronicRisk + ScoreWeights.MaintenanceCost
                  + ScoreWeights.ResaleValue + ScoreWeights.UserSatisfaction;
        Assert.Equal(1.00, sum, 10);
    }

    [Fact]
    public void Version_IsV1()
        => Assert.Equal("v1", Calc().Version);

    [Fact]
    public void Calculation_IsDeterministic()
    {
        var a = Calc(7.3, new[] { "Orta", "Kritik" }, 420, 12, 8.1);
        var b = Calc(7.3, new[] { "Orta", "Kritik" }, 420, 12, 8.1);
        Assert.Equal(a.Overall, b.Overall);
        Assert.Equal(a.ChronicRisk.Value, b.ChronicRisk.Value);
    }

    [Fact]
    public void ResaleValue_IsAlwaysNotAvailable_InSession1()
        => Assert.False(Calc().ResaleValue.IsAvailable);

    [Theory]
    [InlineData(new string[0], 10.0)]          // bilinen kronik sorun yok
    [InlineData(new[] { "Düşük" }, 9.0)]
    [InlineData(new[] { "Düşük", "Orta" }, 7.0)]
    [InlineData(new[] { "Orta", "Kritik" }, 1.5)]
    [InlineData(new[] { "Kritik" }, 1.5)]
    public void ChronicRisk_WorstSeverityBandWins(string[] severities, double expected)
        => Assert.Equal(expected, Calc(severities: severities).ChronicRisk.Value);

    [Theory]
    [InlineData(200, 9.5)]
    [InlineData(201, 7.5)]
    [InlineData(300, 7.5)]
    [InlineData(301, 5.5)]
    [InlineData(450, 5.5)]
    [InlineData(451, 3.5)]
    [InlineData(650, 3.5)]
    [InlineData(651, 1.0)]
    public void MaintenanceCost_ThresholdTable(int eur, double expected)
        => Assert.Equal(expected, Calc(maintEur: eur).MaintenanceCost.Value);

    [Fact]
    public void MaintenanceCost_ZeroOrNegative_IsNotAvailable()
        => Assert.False(Calc(maintEur: 0).MaintenanceCost.IsAvailable);

    [Fact]
    public void UserSatisfaction_BelowThreshold_IsNotAvailable()
        => Assert.False(Calc(reviewCount: 9, avgRating: 8.0, threshold: 10).UserSatisfaction.IsAvailable);

    [Fact]
    public void UserSatisfaction_AtThreshold_UsesAverage()
    {
        var r = Calc(reviewCount: 10, avgRating: 7.4, threshold: 10);
        Assert.True(r.UserSatisfaction.IsAvailable);
        Assert.Equal(7.4, r.UserSatisfaction.Value);
    }

    [Fact]
    public void MissingComponents_BelowMinimum_OverallIsNull()
    {
        // reliability=0 (N/A) + maint=0 (N/A) + no reviews (N/A) + resale (N/A) → yalnızca ChronicRisk mevcut.
        var r = Calc(reliability: 0, severities: new[] { "Orta" }, maintEur: 0, reviewCount: 0);
        Assert.Null(r.Overall);
        Assert.False(r.IsAvailable);
        Assert.Equal(OtoRehberScore.OverallUnavailableMessage, r.UnavailableReason);
        Assert.Equal(1, r.AvailableComponentCount);
    }

    [Fact]
    public void ThreeComponents_MeetsMinimum_NormalizedProportionally()
    {
        // Reliability 10 (.35), ChronicRisk 10 (.25), MaintenanceCost 9.5 (.20); UserSatisfaction & Resale N/A.
        var r = Calc(reliability: 10, severities: Array.Empty<string>(), maintEur: 150, reviewCount: 0);
        Assert.Equal(3, r.AvailableComponentCount);
        var expected = (10 * 0.35 + 10 * 0.25 + 9.5 * 0.20) / (0.35 + 0.25 + 0.20);
        Assert.Equal(expected, r.Overall!.Value, 10);
    }

    [Fact]
    public void FourComponents_IncludesUserSatisfaction()
    {
        var r = Calc(reliability: 8, severities: new[] { "Düşük" }, maintEur: 260, reviewCount: 15, avgRating: 9.0);
        Assert.Equal(4, r.AvailableComponentCount);
        var expected = (8 * 0.35 + 9 * 0.25 + 7.5 * 0.20 + 9 * 0.05) / (0.35 + 0.25 + 0.20 + 0.05);
        Assert.Equal(expected, r.Overall!.Value, 10);
    }

    [Fact]
    public void Overall_IsRawNotRounded()
    {
        var r = Calc(reliability: 8.4267, severities: new[] { "Orta" }, maintEur: 260, reviewCount: 0);
        // Ham değer korunur — yuvarlanmış (8.4 gibi) değil.
        Assert.NotEqual(Math.Round(r.Overall!.Value, 1), r.Overall!.Value);
    }
}
