using System.Collections.Generic;
using OtoRehber.Domain.Advisory;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Listing
{
    public enum DataCoverage { Full, Partial, None }

    /// <summary>Fiyat bulgusu (§5.1/1). Yalnızca gerçek aritmetik — piyasa değeri tahmini yok.</summary>
    public sealed class PriceFinding
    {
        public DataCoverage Coverage { get; init; }
        public long? ListingPrice { get; init; }
        public long BandMin { get; init; }
        public long BandMax { get; init; }
        /// <summary>Örn. "Bandın altında", "Fiyat aralığının %62'sinde", "Bandın üstünde".</summary>
        public string Message { get; init; } = "";
    }

    /// <summary>Kilometre bulgusu (§5.1/2). Beklenen km = yaş × yıllık ortalama (config).</summary>
    public sealed class MileageFinding
    {
        public DataCoverage Coverage { get; init; }
        public int? ListingKm { get; init; }
        public int? AgeYears { get; init; }
        public int? ExpectedMin { get; init; }
        public int? ExpectedMax { get; init; }
        public string Message { get; init; } = "";
    }

    /// <summary>Hasar/boya bulgusu (§5.1/5) — yalnızca kullanıcı girdisi.</summary>
    public sealed class DamageFinding
    {
        public DataCoverage Coverage { get; init; }
        public string Message { get; init; } = "";
    }

    public sealed class ListingAnalysisResult
    {
        public Car Car { get; init; } = default!;
        public PriceFinding Price { get; init; } = default!;
        public MileageFinding Mileage { get; init; } = default!;
        public IReadOnlyList<ChronicIssue> ChronicIssues { get; init; } = new List<ChronicIssue>();
        public IReadOnlyList<MilestoneCheck> Maintenance { get; init; } = new List<MilestoneCheck>();
        public DamageFinding Damage { get; init; } = default!;
        public IReadOnlyList<ChecklistItem> Checklist { get; init; } = new List<ChecklistItem>();
    }
}
