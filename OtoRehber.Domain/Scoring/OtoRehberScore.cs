using System;
using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Scoring
{
    /// <summary>
    /// <b>Canonical OtoRehber Skoru hesabı</b> (PRD v5 §1.2). Tek kaynak — Home, Araç Detay,
    /// Karşılaştırma, AI Sihirbaz, İstatistik ve Arama/Ranking hepsi bu fonksiyonun sonucunu
    /// kullanır. Frontend ve AI skor hesaplayamaz/üretemez; controller formül içermez.
    ///
    /// Ağırlıklar <see cref="ScoreWeights"/>'ten gelir (değiştirilemez). Alt skorlar Session 1'de
    /// mevcut veriden türetilir (ürün kararı 2026-09-03):
    /// <list type="bullet">
    ///   <item>Reliability = mevcut <see cref="Car.ReliabilityScore"/></item>
    ///   <item>ChronicRisk = en kötü kronik sorun şiddet bandı (risk ↑ → skor ↓)</item>
    ///   <item>MaintenanceCost = tahmini yıllık € bakım maliyeti eşik tablosu</item>
    ///   <item>ResaleValue = N/A (2.el değer verisi henüz yok)</item>
    ///   <item>UserSatisfaction = kullanıcı yorum ortalaması (yorum sayısı eşiğin altındaysa N/A)</item>
    /// </list>
    /// Eksik bileşen politikası: en az <see cref="MinComponentsForOverall"/> bileşen mevcut
    /// olmalı; değilse Overall = N/A. Mevcut bileşenlerin ağırlıkları toplam 1.00'a orantılı
    /// normalize edilir (PRD v5 §1.3.1).
    /// </summary>
    public static class OtoRehberScore
    {
        /// <summary>Algoritma versiyonu (PRD v5 §1.2.2).</summary>
        public const string ScoreVersion = "v1";

        /// <summary>Overall'ın hesaplanabilmesi için gereken minimum mevcut bileşen sayısı.</summary>
        public const int MinComponentsForOverall = 3;

        /// <summary>Yorum sayısı bu değerin altındaysa UserSatisfaction = N/A (PRD v5 §1.7 ile aynı eşik).</summary>
        public const int DefaultCommunityReviewThreshold = 10;

        public const string OverallUnavailableMessage =
            "Yeterli veri olmadığı için OtoRehber Skoru hesaplanamıyor.";

        /// <summary>
        /// Ham skoru UI için <b>tek tutarlı kuralla</b> yuvarlar (1 ondalık, yarımı yukarı).
        /// Tüm ekranlar bunu kullanır ki aynı araç her yerde aynı sayıyı göstersin
        /// (PRD v5 §1.2.1 / Ek C). Ham (canonical) değer asla değişmez.
        /// </summary>
        public static double? RoundForDisplay(double? rawOverall)
            => rawOverall is double v ? Math.Round(v, 1, MidpointRounding.AwayFromZero) : null;

        /// <summary>UI metni: yuvarlanmış skor (örn. "8,4") ya da veri yoksa "—".</summary>
        public static string FormatForDisplay(double? rawOverall)
            => RoundForDisplay(rawOverall) is double v ? v.ToString("0.#") : "—";

        public static ScoreResult Calculate(
            Car car,
            int userReviewCount,
            double? averageUserRating,
            int communityReviewThreshold = DefaultCommunityReviewThreshold)
        {
            if (car is null) throw new ArgumentNullException(nameof(car));

            return Calculate(
                car.ReliabilityScore,
                car.ChronicIssues?.Select(i => i.Severity) ?? Enumerable.Empty<string>(),
                car.EstimatedMaintenanceCostEUR,
                userReviewCount,
                averageUserRating,
                communityReviewThreshold);
        }

        public static ScoreResult Calculate(
            double rawReliabilityScore,
            IEnumerable<string?> chronicIssueSeverities,
            int estimatedMaintenanceCostEur,
            int userReviewCount,
            double? averageUserRating,
            int communityReviewThreshold = DefaultCommunityReviewThreshold)
        {
            var reliability = DeriveReliability(rawReliabilityScore);
            var chronicRisk = DeriveChronicRisk(chronicIssueSeverities);
            var maintenance = DeriveMaintenanceCost(estimatedMaintenanceCostEur);
            var resale = ScoreComponent.NotAvailable("2.el değer / piyasa verisi henüz yok.");
            var satisfaction = DeriveUserSatisfaction(userReviewCount, averageUserRating, communityReviewThreshold);

            var weighted = new (ScoreComponent Component, double Weight)[]
            {
                (reliability,   ScoreWeights.Reliability),
                (chronicRisk,   ScoreWeights.ChronicRisk),
                (maintenance,   ScoreWeights.MaintenanceCost),
                (resale,        ScoreWeights.ResaleValue),
                (satisfaction,  ScoreWeights.UserSatisfaction),
            };

            var present = weighted.Where(w => w.Component.IsAvailable).ToList();

            double? overall = null;
            string? reason = null;
            if (present.Count < MinComponentsForOverall)
            {
                reason = OverallUnavailableMessage;
            }
            else
            {
                // Mevcut bileşenlerin ağırlıkları toplam 1.00'a orantılı normalize edilir.
                double totalWeight = present.Sum(w => w.Weight);
                double weightedSum = present.Sum(w => w.Component.Value!.Value * w.Weight);
                overall = weightedSum / totalWeight; // ham decimal — yuvarlama UI'da
            }

            return new ScoreResult
            {
                Version = ScoreVersion,
                Overall = overall,
                UnavailableReason = reason,
                Reliability = reliability,
                ChronicRisk = chronicRisk,
                MaintenanceCost = maintenance,
                ResaleValue = resale,
                UserSatisfaction = satisfaction,
                AvailableComponentCount = present.Count,
            };
        }

        private static ScoreComponent DeriveReliability(double raw)
            => raw > 0
                ? ScoreComponent.Available(Clamp(raw))
                : ScoreComponent.NotAvailable("Güvenilirlik verisi girilmemiş.");

        /// <summary>
        /// En kötü şiddet bandı belirler (adet/maliyet etkilemez — PRD kararı 2026-09-03):
        /// hiç sorun yok → 10 · sadece Düşük → 9 · en az bir Orta → 7 · en az bir Kritik → 1.5.
        /// Risk yükseldikçe skor düşer; frontend bu yönü asla tersine çevirmez (PRD v5 §1.3).
        /// </summary>
        private static ScoreComponent DeriveChronicRisk(IEnumerable<string?> severities)
        {
            bool hasCritical = false, hasMedium = false, hasLow = false;
            foreach (var s in severities)
            {
                switch (NormalizeSeverity(s))
                {
                    case Severity.Critical: hasCritical = true; break;
                    case Severity.Medium: hasMedium = true; break;
                    case Severity.Low: hasLow = true; break;
                }
            }

            double value = hasCritical ? 1.5
                : hasMedium ? 7.0
                : hasLow ? 9.0
                : 10.0; // bilinen kronik sorun yok
            return ScoreComponent.Available(value);
        }

        private enum Severity { Unknown, Low, Medium, Critical }

        private static Severity NormalizeSeverity(string? raw)
        {
            var s = raw?.Trim().ToLowerInvariant();
            return s switch
            {
                "kritik" or "critical" or "yüksek" or "high" => Severity.Critical,
                "orta" or "medium" or "moderate" => Severity.Medium,
                "düşük" or "dusuk" or "low" or "minor" => Severity.Low,
                _ => Severity.Unknown,
            };
        }

        /// <summary>
        /// Tahmini yıllık € bakım maliyeti → skor (maliyet düşükse skor yüksek). Eşikler
        /// mevcut katalog veri dağılımına göre belirlendi (ürün kararı 2026-09-03):
        /// ≤200 → 9.5 · 201-300 → 7.5 · 301-450 → 5.5 · 451-650 → 3.5 · &gt;650 → 1.0.
        /// </summary>
        private static ScoreComponent DeriveMaintenanceCost(int eur)
        {
            if (eur <= 0) return ScoreComponent.NotAvailable("Bakım maliyeti verisi girilmemiş.");
            double value = eur <= 200 ? 9.5
                : eur <= 300 ? 7.5
                : eur <= 450 ? 5.5
                : eur <= 650 ? 3.5
                : 1.0;
            return ScoreComponent.Available(value);
        }

        private static ScoreComponent DeriveUserSatisfaction(int reviewCount, double? avgRating, int threshold)
        {
            if (reviewCount < threshold || avgRating is not > 0)
                return ScoreComponent.NotAvailable("Yeterli kullanıcı yorumu yok.");
            return ScoreComponent.Available(Clamp(avgRating.Value));
        }

        private static double Clamp(double v) => v < 0 ? 0 : v > 10 ? 10 : v;
    }
}
