namespace OtoRehber.Domain
{
    /// <summary>
    /// Araç segmentleri sabit listesi. Admin formlarında dropdown, filtrelerde seçenek
    /// ve migration normalizasyonunda referans olarak kullanılır.
    /// </summary>
    public static class CarSegments
    {
        public static readonly string[] All =
        {
            "A", "B", "C", "D", "E", "SUV", "MPV", "Ticari", "Spor", "Elektrikli"
        };

        public static bool IsValid(string? value) =>
            !string.IsNullOrWhiteSpace(value) && System.Array.Exists(All, s => s == value);
    }
}
