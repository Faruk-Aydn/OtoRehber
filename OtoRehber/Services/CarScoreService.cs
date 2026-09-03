using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Ranking;
using OtoRehber.Domain.Scoring;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Services
{
    /// <summary>
    /// Canonical OtoRehber Skoru'nu bir araç kümesi için toplu hesaplar (PRD v5 §1.2).
    /// Formül <see cref="OtoRehberScore"/>'da; bu servis yalnızca gerekli veriyi (kronik
    /// sorun şiddetleri + kullanıcı yorum agregası) tek sorguda toplayıp fonksiyonu çağırır.
    /// Controller/View skor formülü içermez — hepsi bu sonucu kullanır.
    /// </summary>
    public sealed class CarScoreService
    {
        private readonly OtoRehberDbContext _db;
        private readonly int _communityThreshold;
        private readonly int _maxSameMainModel;

        public CarScoreService(OtoRehberDbContext db, IConfiguration config)
        {
            _db = db;
            _communityThreshold = config.GetValue<int?>("Scoring:CommunityReviewThreshold")
                ?? OtoRehberScore.DefaultCommunityReviewThreshold;
            // PRD v5 §3.1 — ürün kararı 2026-09-03: aynı ana model sıralı listede en fazla 2 kez.
            _maxSameMainModel = config.GetValue<int?>("Ranking:MaxSameMainModel") ?? 2;
        }

        public int CommunityReviewThreshold => _communityThreshold;
        public int MaxSameMainModel => _maxSameMainModel;

        /// <summary>Yüklenmiş araçlar için skor sözlüğü. Araçların navigation'ları yüklü olmak zorunda değil.</summary>
        public async Task<IReadOnlyDictionary<int, ScoreResult>> ForCarsAsync(IReadOnlyCollection<Car> cars)
        {
            if (cars.Count == 0) return new Dictionary<int, ScoreResult>();

            var ids = cars.Select(c => c.Id).ToList();

            var severities = await _db.ChronicIssues.AsNoTracking()
                .Where(i => ids.Contains(i.CarId))
                .Select(i => new { i.CarId, i.Severity })
                .ToListAsync();
            var sevByCar = severities.GroupBy(s => s.CarId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Severity).ToList());

            var reviewAgg = await _db.CarReviews.AsNoTracking()
                .Where(r => ids.Contains(r.CarId))
                .GroupBy(r => r.CarId)
                .Select(g => new { CarId = g.Key, Count = g.Count(), Sum = g.Sum(x => x.Rating) })
                .ToListAsync();
            var revByCar = reviewAgg.ToDictionary(x => x.CarId, x => (x.Count, Avg: (double)x.Sum / x.Count));

            var result = new Dictionary<int, ScoreResult>(cars.Count);
            foreach (var c in cars)
            {
                sevByCar.TryGetValue(c.Id, out var sev);
                var hasRev = revByCar.TryGetValue(c.Id, out var rev);
                result[c.Id] = OtoRehberScore.Calculate(
                    c.ReliabilityScore,
                    sev ?? Enumerable.Empty<string>(),
                    c.EstimatedMaintenanceCostEUR,
                    hasRev ? rev.Count : 0,
                    hasRev ? rev.Avg : (double?)null,
                    _communityThreshold);
            }
            return result;
        }

        /// <summary>
        /// Filtrelenmiş araçları canonical skora göre süzer (min. skor), sıralar ve sayfalar
        /// (PRD v5 §3.2: "Score Calculation → Ranking → Top N"). Dataset küçük olduğu için
        /// tüm filtre sonucu belleğe alınır ve tek matematikle işlenir — böylece min. skor
        /// filtresi ve skor sıralaması her zaman canonical (ham) değeri kullanır. N/A skorlar
        /// sona; eşitlikte Id desc → deterministik (PRD v5 §3.3).
        /// </summary>
        public async Task<(List<Car> Page, IReadOnlyDictionary<int, ScoreResult> Scores, int Total)>
            SortAndPageAsync(IQueryable<Car> filtered, string? sortBy, double? minScore, int page, int pageSize)
        {
            var all = await filtered.ToListAsync();
            var scores = await ForCarsAsync(all);

            if (minScore is > 0)
                all = all.Where(c => scores[c.Id].Overall is double ov && ov >= minScore.Value).ToList();

            double SortKeyScore(Car c) => scores[c.Id].Overall ?? double.MinValue;
            IEnumerable<Car> ordered = sortBy switch
            {
                "score_asc" => all.OrderByDescending(c => scores[c.Id].Overall.HasValue) // N/A daima sona
                                  .ThenBy(SortKeyScore).ThenByDescending(c => c.Id),
                "score_desc" => all.OrderByDescending(SortKeyScore).ThenByDescending(c => c.Id),
                "price_asc" => all.OrderBy(c => c.MinPrice).ThenByDescending(c => c.Id),
                "price_desc" => all.OrderByDescending(c => c.MinPrice).ThenByDescending(c => c.Id),
                "year_desc" => all.OrderByDescending(c => c.YearEnd ?? c.YearStart ?? 0).ThenByDescending(c => c.Id),
                _ => all.OrderByDescending(c => c.Id)
            };

            int total = all.Count;
            int totalPages = System.Math.Max(1, (int)System.Math.Ceiling(total / (double)pageSize));
            page = System.Math.Clamp(page, 1, totalPages);
            var pageCars = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (pageCars, scores, total);
        }

        /// <summary>
        /// Canonical Ranking (PRD v5 §3.2.1) — yalnızca canonical skora göre deterministik sıra.
        /// N/A skorlar sona; eşitlikte Id desc.
        /// </summary>
        public IReadOnlyList<Car> CanonicalRanking(
            IEnumerable<Car> cars, IReadOnlyDictionary<int, ScoreResult> scores)
            => cars.OrderByDescending(c => scores[c.Id].Overall ?? double.MinValue)
                   .ThenByDescending(c => c.Id)
                   .ToList();

        /// <summary>
        /// Presentation Ranking (PRD v5 §3.2.1) — Canonical Ranking'e configurable diversity
        /// (<see cref="MaxSameMainModel"/>) uygulanmış hâli. Skorları/canonical sırayı değiştirmez.
        /// </summary>
        public IReadOnlyList<Car> PresentationRanking(IReadOnlyList<Car> canonicalOrdered)
            => DiversityRanker.Presentation(canonicalOrdered,
                c => MainModel.Key(c.Brand, c.ModelName), _maxSameMainModel);

        /// <summary>
        /// Sıralı liste pipeline'ı (PRD v5 §3.2): skor → Canonical Ranking → Diversity/Re-ranking → Top N.
        /// Ana Sayfa/İstatistik "en yüksek skor" listeleri ve arama önerileri bunu kullanır.
        /// </summary>
        public async Task<(IReadOnlyList<Car> Top, IReadOnlyDictionary<int, ScoreResult> Scores)>
            TopRankedAsync(IReadOnlyCollection<Car> cars, int take, bool onlyScored = true)
        {
            var scores = await ForCarsAsync(cars);
            var pool = onlyScored
                ? cars.Where(c => scores[c.Id].Overall.HasValue).ToList()
                : cars.ToList();
            var canonical = CanonicalRanking(pool, scores);
            var presentation = PresentationRanking(canonical);
            return (presentation.Take(take).ToList(), scores);
        }

        /// <summary>
        /// Tek araç için skor. <paramref name="loadedChronicIssues"/> verilirse (araç detay
        /// sayfası zaten Include ediyor) tekrar sorgu yapılmaz; null ise servis kendisi çeker.
        /// </summary>
        public async Task<ScoreResult> ForCarAsync(Car car, IEnumerable<ChronicIssue>? loadedChronicIssues = null)
        {
            IEnumerable<string?> severities = loadedChronicIssues is not null
                ? loadedChronicIssues.Select(i => i.Severity)
                : await _db.ChronicIssues.AsNoTracking()
                    .Where(i => i.CarId == car.Id).Select(i => i.Severity).ToListAsync();

            var agg = await _db.CarReviews.AsNoTracking()
                .Where(r => r.CarId == car.Id)
                .GroupBy(r => 1)
                .Select(g => new { Count = g.Count(), Sum = g.Sum(x => x.Rating) })
                .FirstOrDefaultAsync();

            int count = agg?.Count ?? 0;
            double? avg = count > 0 ? (double)agg!.Sum / count : null;

            return OtoRehberScore.Calculate(
                car.ReliabilityScore, severities, car.EstimatedMaintenanceCostEUR,
                count, avg, _communityThreshold);
        }
    }
}
