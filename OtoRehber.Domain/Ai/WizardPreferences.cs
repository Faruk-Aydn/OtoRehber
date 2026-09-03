using System.Collections.Generic;

namespace OtoRehber.Domain.Ai
{
    /// <summary>
    /// AI Sihirbaz kullanıcı girdisi. Backend rule engine bunu <b>kural bazlı</b> uygular
    /// (PRD v5 §4.3) — AI değil.
    /// </summary>
    public sealed class WizardPreferences
    {
        public long? BudgetMin { get; init; }
        public long? BudgetMax { get; init; }

        /// <summary>Katı filtre kriterleri (boş/"Farketmez" → uygulanmaz).</summary>
        public string? Fuel { get; init; }
        public string? Transmission { get; init; }
        public string? BodyType { get; init; }

        /// <summary>Yalnızca AI açıklama girdisi — sıralamayı/filtreyi etkilemez (ürün kararı 2026-09-03).</summary>
        public string? FamilySize { get; init; }
        public string? UsageType { get; init; }
        public IReadOnlyList<string> Priorities { get; init; } = new List<string>();
        public string? Notes { get; init; }

        public static bool IsUnset(string? v)
            => string.IsNullOrWhiteSpace(v) || v.Trim().Equals("Farketmez", System.StringComparison.OrdinalIgnoreCase);
    }
}
