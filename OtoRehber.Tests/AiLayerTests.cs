using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.Ai;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Scoring;
using Xunit;

namespace OtoRehber.Tests;

/// <summary>Session 4 — AI açıklama katmanı domain testleri (PRD v5 §4.3–4.7).</summary>
public class AiLayerTests
{
    private static Car Car(int id, long min, long max, string fuel = "Benzin", string trans = "Manuel",
        string body = "Sedan", string seg = "C")
        => new() { Id = id, Brand = "X", ModelName = $"M{id}", MinPrice = min, MaxPrice = max,
                   FuelType = fuel, Transmission = trans, BodyType = body, Segment = seg };

    // --- §4.3/4.4 Wizard rule engine ---
    [Fact]
    public void Wizard_BudgetFilter_EliminatesOutOfRange_WithReason()
    {
        var cars = new[] { Car(1, 400_000, 600_000), Car(2, 1_200_000, 1_500_000) };
        var prefs = new WizardPreferences { BudgetMin = 500_000, BudgetMax = 800_000 };
        var r = WizardRuleEngine.Evaluate(cars, prefs, _ => 8.0);

        Assert.Single(r.Candidates);
        Assert.Equal(1, r.Candidates[0].Car.Id);
        Assert.Contains(r.NearMisses, n => n.Car.Id == 2 && n.Reasons.Contains(WizardRuleEngine.ReasonBudget));
    }

    [Fact]
    public void Wizard_HardFilters_FuelTransmissionBody()
    {
        var cars = new[]
        {
            Car(1, 100, 900_000, fuel: "Dizel", trans: "Otomatik", body: "SUV"),
            Car(2, 100, 900_000, fuel: "Benzin", trans: "Otomatik", body: "SUV"),
        };
        var prefs = new WizardPreferences { BudgetMax = 1_000_000, Fuel = "Benzin", Transmission = "Otomatik", BodyType = "SUV" };
        var r = WizardRuleEngine.Evaluate(cars, prefs, _ => 7.0);

        Assert.Single(r.Candidates);
        Assert.Equal(2, r.Candidates[0].Car.Id);
    }

    [Fact]
    public void Wizard_FarketmezValues_AreNotConstraints()
    {
        var cars = new[] { Car(1, 100, 900_000, fuel: "Dizel", trans: "Manuel", body: "Hatchback") };
        var prefs = new WizardPreferences { BudgetMax = 1_000_000, Fuel = "Farketmez", Transmission = "", BodyType = null };
        var r = WizardRuleEngine.Evaluate(cars, prefs, _ => 6.0);
        Assert.Single(r.Candidates);
    }

    [Fact]
    public void Wizard_RanksByCanonicalScore_Top3()
    {
        var cars = Enumerable.Range(1, 6).Select(i => Car(i, 100, 900_000)).ToArray();
        var prefs = new WizardPreferences { BudgetMax = 1_000_000 };
        var scores = new Dictionary<int, double?> { [1] = 5, [2] = 9, [3] = 7, [4] = 8, [5] = 6, [6] = 4 };
        var r = WizardRuleEngine.Evaluate(cars, prefs, c => scores[c.Id], maxCandidates: 3, maxSameMainModel: 0);

        Assert.Equal(new[] { 2, 4, 3 }, r.Candidates.Select(c => c.Car.Id));
        Assert.Equal(6, r.TotalPassed);
    }

    [Fact]
    public void Wizard_Priorities_DoNotAffectRanking()
    {
        var cars = new[] { Car(1, 100, 900_000), Car(2, 100, 900_000) };
        var scores = new Dictionary<int, double?> { [1] = 6, [2] = 9 };
        var withPrio = new WizardPreferences { BudgetMax = 1_000_000, Priorities = new[] { "Ekonomi & yakıt" } };
        var without = new WizardPreferences { BudgetMax = 1_000_000 };

        var a = WizardRuleEngine.Evaluate(cars, withPrio, c => scores[c.Id]).Candidates.Select(c => c.Car.Id);
        var b = WizardRuleEngine.Evaluate(cars, without, c => scores[c.Id]).Candidates.Select(c => c.Car.Id);
        Assert.Equal(b, a);
    }

    // --- §4.5 Comparison winner (backend) ---
    private static ScoreResult S(double? overall) => new() { Overall = overall };

    [Fact]
    public void Comparison_HigherOverall_Wins()
    {
        Assert.Equal(ComparisonWinner.VehicleA, ComparisonVerdict.Decide(S(8.5), S(7.9)));
        Assert.Equal(ComparisonWinner.VehicleB, ComparisonVerdict.Decide(S(6.0), S(8.0)));
    }

    [Fact]
    public void Comparison_VeryClose_IsTie()
        => Assert.Equal(ComparisonWinner.Tie, ComparisonVerdict.Decide(S(8.04), S(8.0)));

    [Fact]
    public void Comparison_MissingScore_IsUndetermined()
        => Assert.Equal(ComparisonWinner.Undetermined, ComparisonVerdict.Decide(S(8.0), S(null)));

    // --- §4.6 Claim validation ---
    [Fact]
    public void Claims_UnknownType_Rejected()
    {
        var r = AiClaimValidator.Validate(
            new[] { new AiClaim { Type = "market_price", ReferenceId = "x" } },
            new HashSet<string>(), new HashSet<string>());
        Assert.Empty(r.Accepted);
        Assert.Equal(ClaimRejectReason.UnknownType, r.Rejected[0].Reason);
    }

    [Fact]
    public void Claims_ValidAndInvalid_Split()
    {
        var issues = new HashSet<string> { "issue-1" };
        var maint = new HashSet<string> { "maint-2" };
        var r = AiClaimValidator.Validate(new[]
        {
            new AiClaim { Type = "known_issue", ReferenceId = "issue-1" },
            new AiClaim { Type = "known_issue", ReferenceId = "issue-42" },
            new AiClaim { Type = "maintenance", ReferenceId = "maint-2" },
        }, issues, maint);

        Assert.Equal(2, r.Accepted.Count);
        Assert.Single(r.Rejected);
        Assert.Equal("issue-42", r.Rejected[0].Claim.ReferenceId);
    }

    // --- §4.7 Context builder emits reference ids ---
    [Fact]
    public void Context_ForVehicle_ListsIssueAndMaintenanceRefs()
    {
        var car = new Car
        {
            Id = 7, Brand = "Toyota", ModelName = "Corolla", Engine = "1.6", MinPrice = 800_000, MaxPrice = 1_200_000,
            ChronicIssues = new List<ChronicIssue> { new() { Id = 3, IssueTitle = "CVT", Severity = "Orta", EstimatedCostEUR = 800 } },
            MileageMilestones = new List<MileageMilestone> { new() { Id = 9, Mileage = "100.000 km", ExpectedIssues = "CVT yağı", EstimatedCostEUR = 300 } }
        };
        var ctx = AiContextBuilder.ForVehicle(car, new ScoreResult { Overall = 8.5, Reliability = ScoreComponent.Available(9) });

        Assert.Contains("issue-3", ctx.Text);
        Assert.Contains("maint-9", ctx.Text);
        Assert.Contains("issue-3", ctx.IssueRefs);
        Assert.Contains("maint-9", ctx.MaintenanceRefs);
    }
}
