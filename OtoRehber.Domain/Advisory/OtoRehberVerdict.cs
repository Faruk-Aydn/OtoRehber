namespace OtoRehber.Domain.Advisory
{
    public enum VerdictTone { Positive, Caution, Risk, Negative, Unknown }

    /// <summary>
    /// "OtoRehber Değerlendirmesi" (PRD v5 §2.2) — canonical OtoRehber Skoru'ndan
    /// <b>sabit eşiklerle</b> türetilir. AI her seferinde yeniden değerlendirmez.
    /// PRD'nin yasakladığı zorlayıcı ifadeler kullanılmaz; kullanıcıda "bu aracı mutlaka al"
    /// algısı oluşturmayan bir dil tercih edilir.
    /// </summary>
    public readonly record struct OtoRehberVerdict(string Label, VerdictTone Tone)
    {
        public const string Disclaimer =
            "Bu değerlendirme mevcut teknik, maliyet ve piyasa verilerine dayanır. " +
            "İkinci el araç alımında bağımsız ekspertiz ve servis geçmişi kontrolü önerilir.";

        public static OtoRehberVerdict FromScore(double? overall)
        {
            if (overall is not double s)
                return new OtoRehberVerdict("Değerlendirme için yeterli veri yok", VerdictTone.Unknown);

            if (s >= 8.0) return new OtoRehberVerdict("Genel olarak mantıklı", VerdictTone.Positive);
            if (s >= 6.5) return new OtoRehberVerdict("Dikkatli incelenmeli", VerdictTone.Caution);
            if (s >= 5.0) return new OtoRehberVerdict("Riskli", VerdictTone.Risk);
            return new OtoRehberVerdict("Genel olarak önerilmiyor", VerdictTone.Negative);
        }
    }
}
