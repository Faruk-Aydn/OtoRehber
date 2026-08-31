using System;

namespace OtoRehber.Domain
{
    /// <summary>
    /// Araç yapılandırılmış özelliklerinin sabit değer listeleri — admin dropdown'ları,
    /// filtre seçenekleri ve seeder doğrulaması için ortak kaynak (bkz. <see cref="CarSegments"/>).
    /// </summary>
    public static class CarSpecs
    {
        public static readonly string[] FuelTypes =
        {
            "Benzin", "Benzin+LPG", "Dizel", "Hibrit", "Plug-in Hibrit", "Elektrik",
            "Benzin (Hafif Hibrit)", "Dizel (Hafif Hibrit)"
        };

        public static readonly string[] Transmissions = { "Manuel", "Otomatik" };

        public static readonly string[] BodyTypes =
        {
            "Hatchback", "Sedan", "Station Wagon", "SUV", "MPV", "Coupe", "Cabrio", "Pickup", "Panelvan"
        };

        public static readonly string[] Drivetrains = { "Önden Çekiş", "Arkadan İtiş", "4WD", "AWD" };

        public static readonly string[] Conditions = { "İkinci El", "Sıfır" };

        public static bool IsValidFuel(string? v) => In(FuelTypes, v);
        public static bool IsValidTransmission(string? v) => In(Transmissions, v);
        public static bool IsValidBody(string? v) => In(BodyTypes, v);
        public static bool IsValidDrivetrain(string? v) => In(Drivetrains, v);
        public static bool IsValidCondition(string? v) => In(Conditions, v);

        private static bool In(string[] set, string? v) =>
            !string.IsNullOrWhiteSpace(v) && Array.Exists(set, s => s == v);
    }
}
