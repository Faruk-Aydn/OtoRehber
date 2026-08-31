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

        /// <summary>Motor gücü hazır aralıkları (id, etiket, min HP, max HP). Filtre panelinde çoklu seçim.</summary>
        public static readonly (string Id, string Label, int Min, int Max)[] PowerBuckets =
        {
            ("0-50",    "50 HP'ye kadar",      0,   50),
            ("51-75",   "51 - 75 HP",          51,  75),
            ("76-100",  "76 - 100 HP",         76,  100),
            ("101-125", "101 - 125 HP",        101, 125),
            ("126-150", "126 - 150 HP",        126, 150),
            ("151-175", "151 - 175 HP",        151, 175),
            ("176-200", "176 - 200 HP",        176, 200),
            ("201-250", "201 - 250 HP",        201, 250),
            ("251-300", "251 - 300 HP",        251, 300),
            ("301+",    "301 HP ve üzeri",     301, 100000),
        };

        /// <summary>Motor hacmi hazır aralıkları (id, etiket, min cm³, max cm³).</summary>
        public static readonly (string Id, string Label, int Min, int Max)[] CcBuckets =
        {
            ("0-1300",    "1300 cm³'e kadar",    0,    1300),
            ("1301-1600", "1301 - 1600 cm³",     1301, 1600),
            ("1601-1800", "1601 - 1800 cm³",     1601, 1800),
            ("1801-2000", "1801 - 2000 cm³",     1801, 2000),
            ("2001-2500", "2001 - 2500 cm³",     2001, 2500),
            ("2501-3000", "2501 - 3000 cm³",     2501, 3000),
            ("3001+",     "3001 cm³ ve üzeri",   3001, 100000),
        };

        public static (int Min, int Max)? PowerRange(string? id) => Range(PowerBuckets, id);
        public static (int Min, int Max)? CcRange(string? id) => Range(CcBuckets, id);

        private static (int, int)? Range((string Id, string Label, int Min, int Max)[] set, string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var b in set)
                if (b.Id == id) return (b.Min, b.Max);
            return null;
        }

        public static string? PowerLabel(string? id) => Label(PowerBuckets, id);
        public static string? CcLabel(string? id) => Label(CcBuckets, id);
        private static string? Label((string Id, string Label, int Min, int Max)[] set, string? id)
        {
            foreach (var b in set)
                if (b.Id == id) return b.Label;
            return null;
        }

        public static bool IsValidFuel(string? v) => In(FuelTypes, v);
        public static bool IsValidTransmission(string? v) => In(Transmissions, v);
        public static bool IsValidBody(string? v) => In(BodyTypes, v);
        public static bool IsValidDrivetrain(string? v) => In(Drivetrains, v);
        public static bool IsValidCondition(string? v) => In(Conditions, v);

        private static bool In(string[] set, string? v) =>
            !string.IsNullOrWhiteSpace(v) && Array.Exists(set, s => s == v);
    }
}
