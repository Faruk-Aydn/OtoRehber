using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OtoRehber.Infrastructure.Data.CatalogSeed
{
    /// <summary>
    /// `Engine` (ör. "1.6 TDI 115 HP (CRKB) + 7 İleri DSG DQ200") ve `ProductionYears`
    /// ("2013-2016") serbest metinlerinden yapılandırılmış özellikleri türetir.
    /// JSON'da alan zaten doluysa seeder türetmeye başvurmaz.
    /// </summary>
    public static class CatalogSpecInference
    {
        private static readonly Regex HpRx = new(@"(\d{2,3})\s*HP", RegexOptions.IgnoreCase);
        private static readonly Regex CcRx = new(@"(?<!\d)(\d)\.(\d{1,2})(?!\d)", RegexOptions.Compiled); // "1.6", "1.33", "0.9"
        private static readonly Regex YearsRx = new(@"(\d{4})\s*[-–]\s*(\d{4})", RegexOptions.Compiled);

        private static readonly string[] DieselTokens =
        {
            "tdi", "tdci", "crdi", "cdti", "dci", "multijet", "d-4d", "d4d", "hdi", "bluehdi",
            "i-dtec", "jtd", "dtd", "dizel", "bitdi", "d-cat", "cdi", "blumotion tdi"
        };
        private static readonly string[] HybridTokens = { "hybrid", "hibrit", "e-cvt" };
        private static readonly string[] MildHybridTokens = { "mhev", "hafif hibrit", "eqboost", "48v" };
        private static readonly string[] ElectricTokens = { "elektrik", "electric", " ev ", "kwh", "batarya" };
        private static readonly string[] AutoTokens =
        {
            "otomatik", "dsg", "cvt", "edc", "multimode", "multidrive", "tiptronic", "steptronic",
            "powershift", "i-shift", "dualogic", "speedgear", "7g-tronic", "g-tronic", "robotize",
            "s tronic", "s-tronic", "pdk", "dct", "çift kavrama", "torqueflite", "aisin", "at)"
        };

        public static void Fill(CatalogCar src)
        {
            var e = (src.Engine ?? "").ToLowerInvariant();

            src.FuelType ??= InferFuel(e);
            src.Transmission ??= InferTransmission(e);
            src.PowerHp ??= InferHp(src.Engine);
            src.EngineDisplacementCc ??= InferCc(src.Engine);
            src.Condition ??= "İkinci El";

            var (ys, ye) = InferYears(src.ProductionYears);
            src.YearStart ??= ys;
            src.YearEnd ??= ye;
        }

        private static string InferFuel(string e)
        {
            if (ContainsAny(e, ElectricTokens)) return "Elektrik";
            var diesel = ContainsAny(e, DieselTokens);
            var mild = ContainsAny(e, MildHybridTokens);
            var hybrid = ContainsAny(e, HybridTokens);
            if (e.Contains("plug-in") || e.Contains("phev")) return "Plug-in Hibrit";
            if (diesel && mild) return "Dizel (Hafif Hibrit)";
            if (!diesel && mild) return "Benzin (Hafif Hibrit)";
            if (hybrid) return "Hibrit";
            if (diesel) return "Dizel";
            if (e.Contains("lpg")) return "Benzin+LPG";
            return "Benzin";
        }

        private static string InferTransmission(string e) =>
            ContainsAny(e, AutoTokens) ? "Otomatik" : "Manuel";

        private static int? InferHp(string? engine)
        {
            if (string.IsNullOrWhiteSpace(engine)) return null;
            var m = HpRx.Match(engine);
            return m.Success && int.TryParse(m.Groups[1].Value, out var hp) ? hp : (int?)null;
        }

        private static int? InferCc(string? engine)
        {
            if (string.IsNullOrWhiteSpace(engine)) return null;
            var m = CcRx.Match(engine);
            if (!m.Success) return null;
            // "1.6" → 1600, "1.33" → 1330, "0.9" → 900
            if (!double.TryParse($"{m.Groups[1].Value}.{m.Groups[2].Value}", NumberStyles.Any, CultureInfo.InvariantCulture, out var litre))
                return null;
            if (litre < 0.5 || litre > 8.5) return null;
            return (int)Math.Round(litre * 1000);
        }

        private static (int?, int?) InferYears(string? years)
        {
            if (string.IsNullOrWhiteSpace(years)) return (null, null);
            var m = YearsRx.Match(years);
            if (!m.Success) return (null, null);
            var s = int.Parse(m.Groups[1].Value);
            var en = int.Parse(m.Groups[2].Value);
            return (s, en);
        }

        private static bool ContainsAny(string haystack, string[] needles)
        {
            foreach (var n in needles)
                if (haystack.Contains(n)) return true;
            return false;
        }
    }
}
