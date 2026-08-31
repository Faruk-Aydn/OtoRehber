using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtoRehber.Domain;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Infrastructure.Data.CatalogSeed
{
    /// <summary>
    /// `Data/catalog/*.json` dosyalarını veritabanına **bildirimsel** olarak senkronlar.
    /// JSON = doğruluk kaynağı. Anahtar: (Brand + ModelName + Engine), harf duyarsız.
    ///
    /// - Anahtar JSON'da var, DB'de yok → ekle (`Source = "catalog"`).
    /// - Anahtar JSON'da var, DB'de var → benimse (`Source = "catalog"`) + tüm alanları/alt
    ///   listeleri JSON'dan yeniden yaz. (Elle eklenen bir araç granüler bir anahtarı birebir
    ///   tutturamayacağı için bu güvenlidir; HasData anahtarları da eşleşmez.)
    /// - `Source == "catalog"` olup anahtarı JSON'da YOK → buda (sil). Yorumu / garaj kaydı /
    ///   fiyat geçmişi olan katalog aracı SİLİNMEZ (atlanır + uyarı loglanır).
    /// - `Source == null` (HasData / admin) satırlara asla dokunulmaz.
    /// </summary>
    public static class CatalogSeeder
    {
        public const string SourceCatalog = "catalog";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static async Task SeedAsync(OtoRehberDbContext db, string catalogDir, ILogger logger)
        {
            if (!Directory.Exists(catalogDir))
            {
                logger.LogInformation("Katalog klasörü yok, atlanıyor: {Dir}", catalogDir);
                return;
            }

            var files = Directory.GetFiles(catalogDir, "*.json").OrderBy(f => f).ToList();
            if (files.Count == 0)
            {
                logger.LogInformation("Katalog klasöründe JSON yok: {Dir}", catalogDir);
                return;
            }

            var incoming = new List<CatalogCar>();
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var cars = JsonSerializer.Deserialize<List<CatalogCar>>(json, JsonOpts) ?? new();
                    incoming.AddRange(cars);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Katalog dosyası okunamadı: {File}", Path.GetFileName(file));
                }
            }

            if (incoming.Count == 0)
            {
                logger.LogWarning("Katalog JSON'ları boş/okunamadı; senkron atlandı (mevcut kayıtlara dokunulmadı).");
                return;
            }

            static string Key(string? brand, string? model, string? engine) =>
                $"{brand?.Trim().ToLowerInvariant()}|{model?.Trim().ToLowerInvariant()}|{engine?.Trim().ToLowerInvariant()}";

            // Geçersizleri ele, anahtar bazında son kaydı tut (aynı anahtar iki kez yazıldıysa).
            var incomingByKey = new Dictionary<string, CatalogCar>();
            int invalid = 0;
            foreach (var src in incoming)
            {
                if (string.IsNullOrWhiteSpace(src.Brand) || string.IsNullOrWhiteSpace(src.ModelName) || string.IsNullOrWhiteSpace(src.Engine))
                {
                    invalid++;
                    continue;
                }
                incomingByKey[Key(src.Brand, src.ModelName, src.Engine)] = src;
            }

            var existing = await db.Cars
                .Include(c => c.ProsConsList)
                .Include(c => c.ChronicIssues)
                .Include(c => c.MileageMilestones)
                .ToListAsync();
            var existingByKey = existing
                .GroupBy(c => Key(c.Brand, c.ModelName, c.Engine))
                .ToDictionary(g => g.Key, g => g.First());

            // Prune adayları için referans (yorum/garaj/fiyat) olan araç Id'lerini topla.
            var referencedCarIds = new HashSet<int>(
                (await db.CarReviews.Select(r => r.CarId).Distinct().ToListAsync())
                .Concat(await db.UserGarages.Select(g => g.CarId).Distinct().ToListAsync())
                .Concat(await db.CarPriceHistories.Select(p => p.CarId).Distinct().ToListAsync()));

            int added = 0, adopted = 0, pruned = 0, prunedBlocked = 0;

            // 1) Ekle + Benimse/Güncelle
            foreach (var (key, src) in incomingByKey)
            {
                var segment = CarSegments.IsValid(src.Segment) ? src.Segment.Trim() : "C";

                if (existingByKey.TryGetValue(key, out var car))
                {
                    if (car.Source != SourceCatalog) { car.Source = SourceCatalog; adopted++; }
                    ApplyFields(car, src, segment);

                    db.ProsCons.RemoveRange(car.ProsConsList);
                    db.ChronicIssues.RemoveRange(car.ChronicIssues);
                    db.MileageMilestones.RemoveRange(car.MileageMilestones);
                    car.ProsConsList = BuildProsCons(src);
                    car.ChronicIssues = BuildIssues(src);
                    car.MileageMilestones = BuildMilestones(src);
                }
                else
                {
                    var newCar = new Car { Source = SourceCatalog };
                    ApplyFields(newCar, src, segment);
                    newCar.ProsConsList = BuildProsCons(src);
                    newCar.ChronicIssues = BuildIssues(src);
                    newCar.MileageMilestones = BuildMilestones(src);
                    db.Cars.Add(newCar);
                    existingByKey[key] = newCar;
                    added++;
                }
            }

            // 2) Buda: Source="catalog" olup artık JSON'da olmayanlar
            foreach (var car in existing)
            {
                if (car.Source != SourceCatalog) continue;
                var key = Key(car.Brand, car.ModelName, car.Engine);
                if (incomingByKey.ContainsKey(key)) continue;

                if (referencedCarIds.Contains(car.Id))
                {
                    prunedBlocked++;
                    logger.LogWarning(
                        "Katalog aracı budanamadı (yorum/garaj/fiyat kaydı var): #{Id} {Brand} {Model} — {Engine}. Elle temizleyin.",
                        car.Id, car.Brand, car.ModelName, car.Engine);
                    continue;
                }
                db.Cars.Remove(car);
                pruned++;
            }

            await db.SaveChangesAsync();

            logger.LogInformation(
                "Katalog seeder: {Files} dosya, {Keys} benzersiz varyant → eklendi {Added}, benimsendi {Adopted}, budandı {Pruned}, atlandı(referanslı) {Blocked}, geçersiz {Invalid}.",
                files.Count, incomingByKey.Count, added, adopted, pruned, prunedBlocked, invalid);
        }

        private static void ApplyFields(Car car, CatalogCar src, string segment)
        {
            car.Brand = src.Brand.Trim();
            car.ModelName = src.ModelName.Trim();
            car.ProductionYears = Trim(src.ProductionYears ?? "", 40);
            car.Engine = Trim(src.Engine.Trim(), 120);
            car.Segment = segment;
            car.ReliabilityScore = src.ReliabilityScore;
            car.MinPrice = src.MinPrice;
            car.MaxPrice = src.MaxPrice;
            car.EstimatedMaintenanceCostEUR = src.EstimatedMaintenanceCostEUR;
            car.ExpertSummary = src.ExpertSummary ?? "";
            car.UserFeedbackSummary = src.UserFeedbackSummary ?? "";
            car.ImageUrl = string.IsNullOrWhiteSpace(src.ImageUrl) ? null : Trim(src.ImageUrl!, 500);

            // Yapılandırılmış özellikler: JSON'da boş olanları Engine/ProductionYears'ten türet.
            CatalogSpecInference.Fill(src);
            car.FuelType = Norm(src.FuelType, OtoRehber.Domain.CarSpecs.IsValidFuel);
            car.Transmission = Norm(src.Transmission, OtoRehber.Domain.CarSpecs.IsValidTransmission);
            car.BodyType = Norm(src.BodyType, OtoRehber.Domain.CarSpecs.IsValidBody);
            car.Drivetrain = Norm(src.Drivetrain, OtoRehber.Domain.CarSpecs.IsValidDrivetrain);
            car.Condition = Norm(src.Condition, OtoRehber.Domain.CarSpecs.IsValidCondition) ?? "İkinci El";
            car.PowerHp = src.PowerHp is > 0 ? src.PowerHp : null;
            car.EngineDisplacementCc = src.EngineDisplacementCc is > 0 ? src.EngineDisplacementCc : null;
            car.YearStart = src.YearStart is > 1950 ? src.YearStart : null;
            car.YearEnd = src.YearEnd is > 1950 ? src.YearEnd : null;
            car.RangeKm = src.RangeKm is > 0 ? src.RangeKm : null;
            car.FastChargeMinutes = src.FastChargeMinutes is > 0 ? src.FastChargeMinutes : null;
        }

        private static string? Norm(string? value, Func<string?, bool> isValid)
        {
            var v = value?.Trim();
            return isValid(v) ? v : null;
        }

        private static string ClipSeverity(string? s)
        {
            s = (s ?? "").Trim();
            return s is "Düşük" or "Orta" or "Kritik" ? s : "Orta";
        }

        private static List<ProsCons> BuildProsCons(CatalogCar src)
        {
            var list = new List<ProsCons>();
            foreach (var p in src.Pros.Where(x => !string.IsNullOrWhiteSpace(x)))
                list.Add(new ProsCons { Type = "Pro", Description = Trim(p, 600) });
            foreach (var c in src.Cons.Where(x => !string.IsNullOrWhiteSpace(x)))
                list.Add(new ProsCons { Type = "Con", Description = Trim(c, 600) });
            return list;
        }

        private static List<ChronicIssue> BuildIssues(CatalogCar src) =>
            src.ChronicIssues
                .Where(i => !string.IsNullOrWhiteSpace(i.Title))
                .Select(i => new ChronicIssue
                {
                    IssueTitle = Trim(i.Title, 300),
                    Description = i.Description ?? "",
                    Severity = ClipSeverity(i.Severity),
                    EstimatedCostEUR = i.EstimatedCostEUR,
                    AffectedYears = Trim(i.AffectedYears ?? "", 60)
                })
                .ToList();

        private static List<MileageMilestone> BuildMilestones(CatalogCar src) =>
            src.Milestones
                .Where(m => !string.IsNullOrWhiteSpace(m.Mileage))
                .Select(m => new MileageMilestone
                {
                    Mileage = Trim(m.Mileage, 60),
                    ExpectedIssues = m.ExpectedIssues ?? "",
                    EstimatedCostEUR = m.EstimatedCostEUR
                })
                .ToList();

        private static string Trim(string s, int max) => s.Length > max ? s[..max] : s;
    }
}
