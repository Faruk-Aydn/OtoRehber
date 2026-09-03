using System.Collections.Generic;
using System.Linq;
using System.Text;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Scoring;

namespace OtoRehber.Domain.Ai
{
    public sealed class AiContext
    {
        public string Text { get; init; } = "";
        public HashSet<string> IssueRefs { get; init; } = new();
        public HashSet<string> MaintenanceRefs { get; init; } = new();
    }

    /// <summary>
    /// Yapılandırılmış AI prompt bağlamı (PRD v5 §4.7). Yalnızca DB verisi + canonical skor.
    /// Kronik sorun / bakım kalemleri <c>[issue-N] / [maint-N]</c> referans id'leriyle listelenir
    /// ki AI iddialarını bunlara bağlayabilsin (§4.6).
    /// </summary>
    public static class AiContextBuilder
    {
        public static AiContext ForVehicle(Car car, ScoreResult score, ICurrencyFormatter? currency = null)
        {
            var sb = new StringBuilder();
            var issueRefs = new HashSet<string>();
            var maintRefs = new HashSet<string>();

            sb.AppendLine($"ARAÇ #{car.Id}: {car.Brand} {car.ModelName}");
            sb.AppendLine($"- Motor: {car.Engine}");
            var specs = new List<string>();
            if (!string.IsNullOrWhiteSpace(car.FuelType)) specs.Add($"Yakıt: {car.FuelType}");
            if (!string.IsNullOrWhiteSpace(car.Transmission)) specs.Add($"Vites: {car.Transmission}");
            if (!string.IsNullOrWhiteSpace(car.BodyType)) specs.Add($"Kasa: {car.BodyType}");
            if (car.PowerHp is > 0) specs.Add($"{car.PowerHp} HP");
            if (specs.Count > 0) sb.AppendLine("- " + string.Join(" | ", specs));

            sb.AppendLine("OTOREHBER SKORLARI (0-10, canonical):");
            sb.AppendLine($"- Genel: {Fmt(score.Overall)} | Güvenilirlik: {Fmt(score.Reliability.Value)} | "
                + $"Kronik risk: {Fmt(score.ChronicRisk.Value)} | Bakım maliyeti: {Fmt(score.MaintenanceCost.Value)} | "
                + $"2.el değeri: {Fmt(score.ResaleValue.Value)} | Kullanıcı memnuniyeti: {Fmt(score.UserSatisfaction.Value)}");

            if (car.ChronicIssues is { Count: > 0 })
            {
                sb.AppendLine("BİLİNEN KRONİK SORUNLAR:");
                foreach (var i in car.ChronicIssues)
                {
                    var refId = AiClaimValidator.IssueRef(i.Id);
                    issueRefs.Add(refId);
                    sb.AppendLine($"- [{refId}] {i.IssueTitle} — Şiddet: {i.Severity} — Tahmini: {Money(i.EstimatedCostEUR, currency)}");
                }
            }

            if (car.MileageMilestones is { Count: > 0 })
            {
                sb.AppendLine("BAKIM / KM BARAJLARI:");
                foreach (var m in car.MileageMilestones)
                {
                    var refId = AiClaimValidator.MaintenanceRef(m.Id);
                    maintRefs.Add(refId);
                    sb.AppendLine($"- [{refId}] {m.Mileage}: {m.ExpectedIssues} — Tahmini: {Money(m.EstimatedCostEUR, currency)}");
                }
            }
            sb.AppendLine($"- Rutin yıllık bakım tahmini: {Money(car.EstimatedMaintenanceCostEUR, currency)}");

            string conf = (car.DataConfidence?.Overall ?? DataConfidenceLevel.Unknown) switch
            {
                DataConfidenceLevel.High => "Yüksek",
                DataConfidenceLevel.Medium => "Orta",
                DataConfidenceLevel.Low => "Düşük",
                _ => "Belirsiz"
            };
            sb.AppendLine($"PİYASA: Fiyat aralığı {car.MinPrice:N0} - {car.MaxPrice:N0} ₺ | Veri güvenilirliği: {conf}");

            return new AiContext { Text = sb.ToString(), IssueRefs = issueRefs, MaintenanceRefs = maintRefs };
        }

        public static AiContext ForCandidates(
            IEnumerable<(Car Car, int Rank, ScoreResult Score)> candidates, ICurrencyFormatter? currency = null)
        {
            var sb = new StringBuilder();
            var issueRefs = new HashSet<string>();
            var maintRefs = new HashSet<string>();

            foreach (var (car, rank, score) in candidates.OrderBy(c => c.Rank))
            {
                sb.AppendLine($"=== ADAY {rank} ===");
                var ctx = ForVehicle(car, score, currency);
                sb.AppendLine(ctx.Text);
                issueRefs.UnionWith(ctx.IssueRefs);
                maintRefs.UnionWith(ctx.MaintenanceRefs);
            }

            return new AiContext { Text = sb.ToString(), IssueRefs = issueRefs, MaintenanceRefs = maintRefs };
        }

        public static string ForPreferences(WizardPreferences p)
        {
            var sb = new StringBuilder();
            sb.AppendLine("KULLANICI TERCİHLERİ (yalnızca yorum/açıklama için — sıralamayı belirlemez):");
            string budget = (p.BudgetMin, p.BudgetMax) switch
            {
                ( > 0, > 0) => $"{p.BudgetMin:N0} - {p.BudgetMax:N0} ₺",
                ( > 0, _) => $"en az {p.BudgetMin:N0} ₺",
                (_, > 0) => $"en fazla {p.BudgetMax:N0} ₺",
                _ => "belirtilmemiş"
            };
            sb.AppendLine($"- Bütçe: {budget}");
            if (!WizardPreferences.IsUnset(p.Fuel)) sb.AppendLine($"- Yakıt: {p.Fuel}");
            if (!WizardPreferences.IsUnset(p.Transmission)) sb.AppendLine($"- Vites: {p.Transmission}");
            if (!WizardPreferences.IsUnset(p.BodyType)) sb.AppendLine($"- Kasa: {p.BodyType}");
            if (!WizardPreferences.IsUnset(p.FamilySize)) sb.AppendLine($"- Aile: {p.FamilySize}");
            if (!WizardPreferences.IsUnset(p.UsageType)) sb.AppendLine($"- Kullanım: {p.UsageType}");
            if (p.Priorities.Count > 0) sb.AppendLine($"- Öncelikler: {string.Join(", ", p.Priorities)}");
            if (!string.IsNullOrWhiteSpace(p.Notes)) sb.AppendLine($"- Ek not: {p.Notes}");
            return sb.ToString();
        }

        private static string Fmt(double? v) => v.HasValue ? v.Value.ToString("0.#") : "N/A";

        private static string Money(int eur, ICurrencyFormatter? c)
            => c is not null ? c.Eur(eur) : $"~{eur:N0} €";
    }

    /// <summary>Infra katmanındaki CurrencyContext'e bağımlılık kurmadan € formatı almak için.</summary>
    public interface ICurrencyFormatter
    {
        string Eur(int eur);
    }
}
