using System;
using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Advisory
{
    public readonly record struct SuitabilityItem(string Audience, string Reason);

    public sealed class SuitabilityResult
    {
        public List<SuitabilityItem> SuitableFor { get; init; } = new();
        public List<SuitabilityItem> NotSuitableFor { get; init; } = new();
        public bool HasData => SuitableFor.Count > 0 || NotSuitableFor.Count > 0;
    }

    /// <summary>
    /// "Kimlere uygun / kimlere uygun değil" (PRD v5 §2.3) — <b>kural bazlı türetilir</b>,
    /// AI serbest metin üretmez. Girdi: kronik sorun şiddeti, motor gücü, yakıt tipi,
    /// kasa/segment, tahmini bakım maliyeti. (Kurallar ürün kararıdır — 2026-09-03.)
    /// </summary>
    public static class SuitabilityRules
    {
        public static SuitabilityResult Evaluate(Car car)
        {
            var issues = car.ChronicIssues ?? new List<ChronicIssue>();
            bool hasCritical = issues.Any(i => Normalize(i.Severity) == "kritik");
            int hp = car.PowerHp ?? 0;
            int cc = car.EngineDisplacementCc ?? 0;
            string fuel = (car.FuelType ?? "").ToLowerInvariant();
            string seg = (car.Segment ?? "").Trim().ToUpperInvariant();
            bool ecoFuel = fuel.Contains("hibrit") || fuel.Contains("lpg") || fuel.Contains("elektrik");

            var result = new SuitabilityResult();

            // --- Uygun ---
            if (!hasCritical && (seg is "C" or "D" or "E" or "SUV" or "MPV"))
                result.SuitableFor.Add(new("Aile ve günlük kullanım",
                    "Uygun segment ve kritik seviyeli kronik sorun kaydı yok."));

            if (ecoFuel || (cc is > 0 and <= 1600))
                result.SuitableFor.Add(new("Şehir içi ve düşük yakıt maliyeti",
                    ecoFuel ? "Ekonomik yakıt tipi (hibrit / LPG / elektrik)." : "Küçük hacimli motor (≤1.6 L)."));

            if (fuel.Contains("dizel") && hp >= 130)
                result.SuitableFor.Add(new("Uzun yol ve otoyol kullanımı",
                    "Dizel motor ve yeterli çekiş gücü (≥130 HP)."));

            if (hp >= 180)
                result.SuitableFor.Add(new("Performans öncelikli sürüş",
                    $"Yüksek motor gücü ({hp} HP)."));

            // --- Uygun değil ---
            if (hasCritical || hp >= 200)
                result.NotSuitableFor.Add(new("İlk araç / yeni sürücüler",
                    hasCritical ? "Kritik seviyeli kronik sorun kaydı var." : $"Yüksek motor gücü ({hp} HP)."));

            if (hasCritical || car.EstimatedMaintenanceCostEUR > 450)
                result.NotSuitableFor.Add(new("Bakıma sınırlı bütçe ayıracaklar",
                    hasCritical ? "Kritik seviyeli kronik sorun ağır masraf çıkarabilir."
                                : $"Tahmini yıllık bakım maliyeti yüksek (~{car.EstimatedMaintenanceCostEUR} €)."));

            return result;
        }

        private static string Normalize(string? s) => (s ?? "").Trim().ToLowerInvariant() switch
        {
            "kritik" or "critical" or "yüksek" or "high" => "kritik",
            "orta" or "medium" => "orta",
            "düşük" or "dusuk" or "low" => "düşük",
            _ => ""
        };
    }
}
