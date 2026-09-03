using System;
using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Ranking;

namespace OtoRehber.Domain.Ai
{
    public readonly record struct WizardCandidate(Car Car, int Rank, double? Score);

    public readonly record struct EliminatedCar(Car Car, double? Score, IReadOnlyList<string> Reasons);

    public sealed class WizardResult
    {
        public IReadOnlyList<WizardCandidate> Candidates { get; init; } = Array.Empty<WizardCandidate>();
        public IReadOnlyList<EliminatedCar> NearMisses { get; init; } = Array.Empty<EliminatedCar>();
        public int TotalPassed { get; init; }
    }

    /// <summary>
    /// AI Sihirbaz backend kural motoru (PRD v5 §4.2–4.4): <c>Filter → Ranking → Top Candidates</c>.
    /// AI bu adımların hiçbirine karışmaz; yalnızca çıktı adaylarını açıklar.
    ///
    /// Katı filtre (ürün kararı 2026-09-03): bütçe + yakıt + vites + kasa tipi.
    /// Kullanım/aile/öncelikler filtreye/sıralamaya girmez — AI açıklama girdisidir.
    /// Sıralama: canonical OtoRehber Skoru + diversity (Presentation Ranking).
    /// Elenen her araç için hangi kriterin elediği <b>kaydedilir</b> (§4.4).
    /// </summary>
    public static class WizardRuleEngine
    {
        public const string ReasonBudget = "Bütçe aralığının dışında";
        public const string ReasonFuel = "İstenen yakıt tipine uymuyor";
        public const string ReasonTransmission = "İstenen vites tipine uymuyor";
        public const string ReasonBody = "İstenen kasa tipine uymuyor";

        public static WizardResult Evaluate(
            IEnumerable<Car> cars,
            WizardPreferences prefs,
            Func<Car, double?> scoreOf,
            int maxCandidates = 3,
            int maxSameMainModel = 2)
        {
            var evaluated = cars
                .Select(c => new { Car = c, Reasons = FailedConstraints(c, prefs) })
                .ToList();

            var passed = evaluated.Where(e => e.Reasons.Count == 0).Select(e => e.Car).ToList();

            // Sıralama: canonical skor → Canonical Ranking → Diversity → Top N.
            var ranked = passed
                .OrderByDescending(c => scoreOf(c) ?? double.MinValue)
                .ThenByDescending(c => c.Id)
                .ToList();
            var presented = DiversityRanker.Presentation(
                ranked, c => MainModel.Key(c.Brand, c.ModelName), maxSameMainModel);

            var candidates = presented
                .Take(maxCandidates)
                .Select((c, i) => new WizardCandidate(c, i + 1, scoreOf(c)))
                .ToList();

            // Elenen "yakın" adaylar: tam olarak 1 kriterden elenmiş, skora göre en iyi 3.
            var nearMisses = evaluated
                .Where(e => e.Reasons.Count == 1)
                .OrderByDescending(e => scoreOf(e.Car) ?? double.MinValue)
                .ThenByDescending(e => e.Car.Id)
                .Take(3)
                .Select(e => new EliminatedCar(e.Car, scoreOf(e.Car), e.Reasons))
                .ToList();

            return new WizardResult
            {
                Candidates = candidates,
                NearMisses = nearMisses,
                TotalPassed = passed.Count,
            };
        }

        private static IReadOnlyList<string> FailedConstraints(Car car, WizardPreferences p)
        {
            var reasons = new List<string>();

            // Bütçe: aracın [MinPrice, MaxPrice] aralığı kullanıcı bütçesiyle kesişmeli.
            if (p.BudgetMin is > 0 && car.MaxPrice < p.BudgetMin) reasons.Add(ReasonBudget);
            else if (p.BudgetMax is > 0 && car.MinPrice > p.BudgetMax) reasons.Add(ReasonBudget);

            if (!WizardPreferences.IsUnset(p.Fuel) && !Matches(car.FuelType, p.Fuel!))
                reasons.Add(ReasonFuel);

            if (!WizardPreferences.IsUnset(p.Transmission) && !SameValue(car.Transmission, p.Transmission!))
                reasons.Add(ReasonTransmission);

            if (!WizardPreferences.IsUnset(p.BodyType) && !SameValue(car.BodyType, p.BodyType!))
                reasons.Add(ReasonBody);

            return reasons;
        }

        // Yakıt: "Hibrit" → "Plug-in Hibrit" / "Benzin (Hafif Hibrit)" de kabul.
        private static bool Matches(string? carValue, string wanted)
            => !string.IsNullOrWhiteSpace(carValue)
               && carValue.Contains(wanted.Trim(), StringComparison.OrdinalIgnoreCase);

        private static bool SameValue(string? carValue, string wanted)
            => !string.IsNullOrWhiteSpace(carValue)
               && carValue.Trim().Equals(wanted.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
