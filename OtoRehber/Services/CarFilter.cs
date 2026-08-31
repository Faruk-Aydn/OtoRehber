using System.Linq;

namespace OtoRehber.Services
{
    /// <summary>
    /// /araclar (ve Home) filtre parametreleri. Query string'den model binding ile dolar
    /// (`?brand=BMW&brand=Audi&fuel=Dizel&powerMin=120`). Boş/geçersiz değerler yok sayılır.
    /// </summary>
    public class CarFilter
    {
        public string? SearchQuery { get; set; }
        public string[]? Brand { get; set; }
        public string[]? Segment { get; set; }
        public string[]? Fuel { get; set; }
        public string[]? Transmission { get; set; }
        public string[]? BodyType { get; set; }
        public string[]? Drivetrain { get; set; }
        public string[]? Condition { get; set; }

        /// <summary>Motor gücü hazır aralık id'leri (CarSpecs.PowerBuckets), çoklu seçim.</summary>
        public string[]? Power { get; set; }
        /// <summary>Motor hacmi hazır aralık id'leri (CarSpecs.CcBuckets), çoklu seçim.</summary>
        public string[]? Cc { get; set; }

        public long? PriceMin { get; set; }
        public long? PriceMax { get; set; }
        public double? MinScore { get; set; }
        public int? YearMin { get; set; }
        public int? YearMax { get; set; }

        public string? SortBy { get; set; }

        public static string[] Clean(string[]? a) =>
            (a ?? System.Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToArray();

        public bool HasAny =>
            !string.IsNullOrWhiteSpace(SearchQuery)
            || Clean(Brand).Any() || Clean(Segment).Any() || Clean(Fuel).Any()
            || Clean(Transmission).Any() || Clean(BodyType).Any() || Clean(Drivetrain).Any()
            || Clean(Condition).Any() || Clean(Power).Any() || Clean(Cc).Any()
            || PriceMin is > 0 || PriceMax is > 0 || MinScore is > 0
            || YearMin is > 0 || YearMax is > 0;
    }
}
