using System;
using OtoRehber.Domain.Ai;

namespace OtoRehber.Domain.Advisory
{
    /// <summary>
    /// € → TL gösterim dönüşümü (PRD v5 §2.4). Backend maliyetleri € cinsinden saklar;
    /// UI'da kur ve <b>kur tarihi</b> gösterilerek TL'ye çevrilir. Kur yapılandırılmamışsa
    /// € gösterilir — sahte/uydurma dönüşüm yapılmaz.
    /// </summary>
    public sealed class CurrencyContext : ICurrencyFormatter
    {
        public double? EurToTry { get; init; }
        public string? RateDate { get; init; }

        public bool HasRate => EurToTry is > 0;

        public string Eur(int eur) => HasRate
            ? $"~{Math.Round(eur * EurToTry!.Value):N0} ₺"
            : $"~{eur:N0} €";

        public string EurRange(int minEur, int maxEur) => HasRate
            ? $"~{Math.Round(minEur * EurToTry!.Value):N0} – {Math.Round(maxEur * EurToTry!.Value):N0} ₺"
            : $"~{minEur:N0} – {maxEur:N0} €";

        public string? RateNote => HasRate
            ? $"1 € = {EurToTry!.Value.ToString("N2")} ₺" +
              (string.IsNullOrWhiteSpace(RateDate) ? " (yaklaşık kur)" : $" · {RateDate} kuru")
            : null;
    }
}
