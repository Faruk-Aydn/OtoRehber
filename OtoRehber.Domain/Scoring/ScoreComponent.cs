namespace OtoRehber.Domain.Scoring
{
    /// <summary>
    /// Bir alt skorun sonucu. <see cref="Value"/> null ise bileşen için yeterli veri
    /// yoktur (N/A) — 0 veya nötr bir değerle doldurulmaz (PRD v5 §1.3.1).
    /// </summary>
    public readonly record struct ScoreComponent(double? Value, string? UnavailableReason)
    {
        public bool IsAvailable => Value.HasValue;

        public static ScoreComponent Available(double value) => new(value, null);
        public static ScoreComponent NotAvailable(string reason) => new(null, reason);
    }
}
