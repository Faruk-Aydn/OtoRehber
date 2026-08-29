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
    /// `Data/catalog/*.json` dosyalarındaki araç varyantlarını veritabanına idempotent şekilde ekler.
    /// Anahtar: (Brand + ModelName + Engine). Kayıt varsa DOKUNULMAZ (admin düzenlemeleri korunur),
    /// yoksa alt kayıtlarıyla (kronik arıza / artı-eksi / km barajı) birlikte eklenir.
    /// `forceUpdate = true` ise mevcut kayıtların alanları ve alt listeleri katalogdan yeniden yazılır.
    /// </summary>
    public static class CatalogSeeder
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static async Task SeedAsync(OtoRehberDbContext db, string catalogDir, ILogger logger, bool forceUpdate = false)
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

            if (incoming.Count == 0) return;

            // Mevcut araçları anahtarlarıyla çek (alt listeleriyle, forceUpdate için).
            var existing = await db.Cars
                .Include(c => c.ProsConsList)
                .Include(c => c.ChronicIssues)
                .Include(c => c.MileageMilestones)
                .ToListAsync();

            static string Key(string brand, string model, string engine) =>
                $"{brand?.Trim().ToLowerInvariant()}|{model?.Trim().ToLowerInvariant()}|{engine?.Trim().ToLowerInvariant()}";

            var byKey = existing
                .GroupBy(c => Key(c.Brand, c.ModelName, c.Engine))
                .ToDictionary(g => g.Key, g => g.First());

            int added = 0, updated = 0, skipped = 0, invalid = 0;

            foreach (var src in incoming)
            {
                if (string.IsNullOrWhiteSpace(src.Brand) || string.IsNullOrWhiteSpace(src.ModelName) || string.IsNullOrWhiteSpace(src.Engine))
                {
                    invalid++;
                    continue;
                }
                var segment = CarSegments.IsValid(src.Segment) ? src.Segment.Trim() : "C";

                if (byKey.TryGetValue(Key(src.Brand, src.ModelName, src.Engine), out var car))
                {
                    if (!forceUpdate) { skipped++; continue; }

                    car.ProductionYears = src.ProductionYears;
                    car.Segment = segment;
                    car.ReliabilityScore = src.ReliabilityScore;
                    car.MinPrice = src.MinPrice;
                    car.MaxPrice = src.MaxPrice;
                    car.EstimatedMaintenanceCostEUR = src.EstimatedMaintenanceCostEUR;
                    car.ExpertSummary = src.ExpertSummary;
                    car.UserFeedbackSummary = src.UserFeedbackSummary;
                    if (!string.IsNullOrWhiteSpace(src.ImageUrl)) car.ImageUrl = src.ImageUrl;

                    db.ProsCons.RemoveRange(car.ProsConsList);
                    db.ChronicIssues.RemoveRange(car.ChronicIssues);
                    db.MileageMilestones.RemoveRange(car.MileageMilestones);
                    car.ProsConsList = BuildProsCons(src);
                    car.ChronicIssues = BuildIssues(src);
                    car.MileageMilestones = BuildMilestones(src);
                    updated++;
                }
                else
                {
                    var newCar = new Car
                    {
                        Brand = src.Brand.Trim(),
                        ModelName = src.ModelName.Trim(),
                        ProductionYears = src.ProductionYears,
                        Engine = src.Engine.Trim(),
                        Segment = segment,
                        ReliabilityScore = src.ReliabilityScore,
                        MinPrice = src.MinPrice,
                        MaxPrice = src.MaxPrice,
                        EstimatedMaintenanceCostEUR = src.EstimatedMaintenanceCostEUR,
                        ExpertSummary = src.ExpertSummary,
                        UserFeedbackSummary = src.UserFeedbackSummary,
                        ImageUrl = string.IsNullOrWhiteSpace(src.ImageUrl) ? null : src.ImageUrl,
                        ProsConsList = BuildProsCons(src),
                        ChronicIssues = BuildIssues(src),
                        MileageMilestones = BuildMilestones(src)
                    };
                    db.Cars.Add(newCar);
                    byKey[Key(newCar.Brand, newCar.ModelName, newCar.Engine)] = newCar;
                    added++;
                }
            }

            if (added > 0 || updated > 0)
                await db.SaveChangesAsync();

            logger.LogInformation(
                "Katalog seeder: {Files} dosya, {Incoming} varyant → eklendi {Added}, güncellendi {Updated}, atlandı {Skipped}, geçersiz {Invalid}.",
                files.Count, incoming.Count, added, updated, skipped, invalid);
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
                    AffectedYears = Trim(i.AffectedYears, 60)
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
