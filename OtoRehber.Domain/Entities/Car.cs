using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OtoRehber.Domain.Entities
{
    public class Car
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Brand { get; set; }

        [Required, MaxLength(100)]
        public string ModelName { get; set; }

        [MaxLength(40)]
        public string ProductionYears { get; set; }

        [MaxLength(120)]
        public string Engine { get; set; }

        [MaxLength(60)]
        public string Segment { get; set; }

        public string ExpertSummary { get; set; }

        public double ReliabilityScore { get; set; }
        public long MinPrice { get; set; }
        public long MaxPrice { get; set; }
        public int EstimatedMaintenanceCostEUR { get; set; }

        public string UserFeedbackSummary { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Kaydın kaynağı: "catalog" → `Data/catalog/*.json` seeder yönetir (senkron/prune);
        /// null → HasData seed veya admin panelden elle eklendi (seeder dokunmaz).
        /// </summary>
        [MaxLength(20)]
        public string? Source { get; set; }

        /// <summary>
        /// Verinin güvenilirlik bilgisi (PRD v5 §1.5). EF'de aynı tabloya gömülür.
        /// Ayarlanmamışsa null → <see cref="DataConfidenceLevel.Unknown"/> kabul edilir.
        /// Katalog seeder küratörlü satırlara Medium atar.
        /// </summary>
        public CarDataConfidence? DataConfidence { get; set; }

        /// <summary>DataConfidence null ise Unknown döndürür (view'lar bunu kullanır).</summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DataConfidenceLevel EffectiveDataConfidence
            => DataConfidence?.Overall ?? DataConfidenceLevel.Unknown;

        /// <summary>
        /// Verinin gerçek son güncelleme tarihi (PRD v5 §1.6). Bilinmiyorsa null —
        /// asla oluşturma tarihiyle doldurulmaz; UI "Güncelleme tarihi bilinmiyor" gösterir.
        /// </summary>
        public DateTime? LastUpdatedUtc { get; set; }

        // --- Yapılandırılmış özellikler (filtreleme için; serbest metin Engine'den türetilir/elle girilir) ---
        /// <summary>Benzin | Benzin+LPG | Dizel | Hibrit | Plug-in Hibrit | Elektrik | Benzin (Hafif Hibrit) | Dizel (Hafif Hibrit)</summary>
        [MaxLength(24)] public string? FuelType { get; set; }
        /// <summary>Manuel | Otomatik (ince detay — DSG/CVT/robotize — Engine metninde kalır)</summary>
        [MaxLength(16)] public string? Transmission { get; set; }
        /// <summary>Hatchback | Sedan | Station Wagon | SUV | MPV | Coupe | Cabrio | Pickup | Panelvan</summary>
        [MaxLength(24)] public string? BodyType { get; set; }
        /// <summary>Önden Çekiş | Arkadan İtiş | 4WD | AWD</summary>
        [MaxLength(16)] public string? Drivetrain { get; set; }
        /// <summary>İkinci El | Sıfır</summary>
        [MaxLength(12)] public string? Condition { get; set; }
        public int? PowerHp { get; set; }
        public int? EngineDisplacementCc { get; set; }
        public int? YearStart { get; set; }
        public int? YearEnd { get; set; }
        /// <summary>Elektrikli araçlarda menzil (km) — diğerlerinde null.</summary>
        public int? RangeKm { get; set; }
        /// <summary>Elektrikli araçlarda hızlı şarj süresi (dakika) — diğerlerinde null.</summary>
        public int? FastChargeMinutes { get; set; }

        public List<ProsCons> ProsConsList { get; set; } = new List<ProsCons>();
        public List<ChronicIssue> ChronicIssues { get; set; } = new List<ChronicIssue>();
        public List<MileageMilestone> MileageMilestones { get; set; } = new List<MileageMilestone>();
        public List<CarReview> Reviews { get; set; } = new List<CarReview>();
        public List<CarPriceHistory> PriceHistory { get; set; } = new List<CarPriceHistory>();
        public List<CarImage> Images { get; set; } = new List<CarImage>();
    }
}
