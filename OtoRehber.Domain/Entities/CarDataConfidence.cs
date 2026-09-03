namespace OtoRehber.Domain.Entities
{
    /// <summary>
    /// Araç verisinin güvenilirlik bilgisi. EF'de <see cref="Car"/> ile aynı tabloya
    /// gömülür (owned type). Session 1'de yalnızca <see cref="Overall"/> kullanılır;
    /// alan bazlı seviyeler (PRD v5 §1.5 geleceğe uyumluluk) ileride doldurulacak —
    /// bu yüzden confidence tek düz string kolon değil, genişleyebilir bir yapıdır.
    /// </summary>
    public class CarDataConfidence
    {
        public DataConfidenceLevel Overall { get; set; } = DataConfidenceLevel.Unknown;

        public DataConfidenceLevel? TechnicalData { get; set; }
        public DataConfidenceLevel? ChronicIssue { get; set; }
        public DataConfidenceLevel? Maintenance { get; set; }
        public DataConfidenceLevel? MarketData { get; set; }
        public DataConfidenceLevel? Community { get; set; }
    }
}
