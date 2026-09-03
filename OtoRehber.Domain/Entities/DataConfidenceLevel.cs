namespace OtoRehber.Domain.Entities
{
    /// <summary>
    /// Bir aracın verisine ne kadar güvenildiği (PRD v5 §1.5). Kriterler net:
    /// <list type="bullet">
    ///   <item><b>High</b> — birden fazla güvenilir kaynak + doğrulanmış teknik veri + güncel piyasa + yeterli kullanıcı verisi</item>
    ///   <item><b>Medium</b> — ana teknik veriler mevcut, kaynak sınırlı, bazı piyasa/kullanıcı verisi eksik</item>
    ///   <item><b>Low</b> — veri büyük ölçüde tahmini, kaynak çok az, kullanıcı verisi yok/az</item>
    ///   <item><b>Unknown</b> — güven seviyesi belirlenemiyor (asla otomatik "High" gösterilmez)</item>
    /// </list>
    /// Default olarak hiçbir araca "Low" atanmaz — yalnızca gerçekten tahmini veri varsa.
    /// </summary>
    public enum DataConfidenceLevel
    {
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }
}
