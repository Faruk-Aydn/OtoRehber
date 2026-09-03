namespace OtoRehber.Domain.Listing
{
    /// <summary>
    /// İlan Analizi kullanıcı girdisi (PRD v5 §5). İlk sürümde yalnızca backend'in
    /// güvenilir desteklediği alanlar: araç (katalogdan), yıl, km, fiyat, hasar, boya.
    /// Model, ileride Şanzıman/Yakıt/Tramer tutarı/Değişen parça/Servis geçmişi/Ekspertiz
    /// sonucu/İlan açıklaması eklenebilecek şekilde genişletilebilir tutulur —
    /// bu alanların hepsi ilk sürümde implement edilmek zorunda değildir.
    /// </summary>
    public sealed class ListingInput
    {
        /// <summary>Katalogdan seçilen tam varyant.</summary>
        public int CarId { get; init; }

        public int? Year { get; init; }
        public int? Mileage { get; init; }
        public long? Price { get; init; }

        /// <summary>Kullanıcının girdiği gerçek bilgi — girilmemişse tahmin edilmez (§5).</summary>
        public bool? HasDamageRecord { get; init; }
        public int? PaintedPanels { get; init; }

        public string? Notes { get; init; }

        // --- İleride (v2) ---
        // public string? Transmission { get; init; }
        // public string? Fuel { get; init; }
        // public long? TramerAmount { get; init; }
        // public string? ChangedParts { get; init; }
        // public bool? HasServiceHistory { get; init; }
        // public string? InspectionResult { get; init; }
        // public string? ListingDescription { get; init; }
    }
}
