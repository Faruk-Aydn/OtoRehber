namespace OtoRehber.Domain.Scoring
{
    /// <summary>
    /// Canonical OtoRehber Skoru sonucu. <b>Ham decimal değerler</b> taşır — yuvarlama
    /// yalnızca UI gösteriminde yapılır (PRD v5 §1.2.1). Ranking, karşılaştırma ve
    /// ortalama hesapları her zaman <see cref="Overall"/> ham değeri üzerinden yapılır.
    /// </summary>
    public sealed class ScoreResult
    {
        /// <summary>Hesaplama algoritmasının versiyonu (PRD v5 §1.2.2). Frontend/AI değiştiremez.</summary>
        public string Version { get; init; } = OtoRehberScore.ScoreVersion;

        /// <summary>Ağırlıklı genel skor (0.0–10.0, ham). Minimum veri kapsamı sağlanmıyorsa null → N/A.</summary>
        public double? Overall { get; init; }

        /// <summary>Genel skor N/A ise nedeni (UI'da sabit mesaj gösterilir).</summary>
        public string? UnavailableReason { get; init; }

        public ScoreComponent Reliability { get; init; }
        public ScoreComponent ChronicRisk { get; init; }
        public ScoreComponent MaintenanceCost { get; init; }
        public ScoreComponent ResaleValue { get; init; }
        public ScoreComponent UserSatisfaction { get; init; }

        public bool IsAvailable => Overall.HasValue;

        /// <summary>Ağırlığı hesaba katılan (mevcut) bileşen sayısı.</summary>
        public int AvailableComponentCount { get; init; }
    }
}
