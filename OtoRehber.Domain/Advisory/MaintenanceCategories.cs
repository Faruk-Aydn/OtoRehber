using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Advisory
{
    /// <summary>
    /// Bakım maliyetleri kategorilere ayrılır (PRD v5 §2.4) — tek kalemde birleştirilmez:
    /// rutin yıllık bakım · kilometre bakım barajları · büyük arıza riski (kronik sorunlar).
    /// Tüm tutarlar € (backend şeması korunur); TL'ye çevrim <see cref="CurrencyContext"/>.
    /// </summary>
    public sealed class MaintenanceCategories
    {
        public int RoutineAnnualEur { get; init; }

        public IReadOnlyList<(string Mileage, string Work, int Eur)> Milestones { get; init; }
            = new List<(string, string, int)>();

        public IReadOnlyList<(string Title, string Severity, int Eur)> MajorRisks { get; init; }
            = new List<(string, string, int)>();

        public int MilestoneTotalEur => Milestones.Sum(m => m.Eur);
        public int MajorRiskTotalEur => MajorRisks.Sum(r => r.Eur);
        public bool HasMilestones => Milestones.Count > 0;
        public bool HasMajorRisks => MajorRisks.Count > 0;

        public static MaintenanceCategories Build(Car car)
        {
            var issues = car.ChronicIssues ?? new List<ChronicIssue>();
            var milestones = car.MileageMilestones ?? new List<MileageMilestone>();

            return new MaintenanceCategories
            {
                RoutineAnnualEur = car.EstimatedMaintenanceCostEUR,
                Milestones = milestones
                    .Select(m => (m.Mileage, m.ExpectedIssues, m.EstimatedCostEUR))
                    .ToList(),
                MajorRisks = issues
                    .Where(i => i.EstimatedCostEUR > 0)
                    .OrderByDescending(i => i.EstimatedCostEUR)
                    .Select(i => (i.IssueTitle, i.Severity, i.EstimatedCostEUR))
                    .ToList(),
            };
        }
    }
}
