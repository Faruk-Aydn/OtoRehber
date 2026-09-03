namespace OtoRehber.Domain.Scoring
{
    /// <summary>
    /// Canonical OtoRehber Skoru ağırlıkları — <b>ürün kararıdır, değiştirilemez</b>
    /// (PRD v5 §1.2). Toplam = 1.00. Skor bu değerlerin tek kaynağıdır; kod içinde
    /// başka hiçbir yerde ağırlık sabiti tanımlanmaz.
    /// </summary>
    public static class ScoreWeights
    {
        public const double Reliability = 0.35;
        public const double ChronicRisk = 0.25;
        public const double MaintenanceCost = 0.20;
        public const double ResaleValue = 0.15;
        public const double UserSatisfaction = 0.05;
    }
}
