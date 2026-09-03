using System;
using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.Advisory;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Listing
{
    /// <summary>
    /// İlan Analizi kural motoru (PRD v5 §5, Aşama A). <b>Tamamen kural bazlı</b> — AI yoktur,
    /// eksik veri tahmin edilmez, yalnızca gerçek aritmetik yapılır. Renk/verdict eşiği yok
    /// (nötr UI — ürün kararı 2026-09-03).
    /// </summary>
    public static class ListingAnalyzer
    {
        public const int DefaultAnnualKm = 15_000;

        // Beklenen km aralığının genişliği (± oran). Renk/iyi-kötü eşiği DEĞİL — sadece
        // "beklenen aralık" tanımı. Yıllık ort. km ile birlikte config'e taşınabilir.
        private const double ExpectedKmSpread = 0.25;

        public static ListingAnalysisResult Analyze(
            Car car, ListingInput input, int currentYear, int annualKm = DefaultAnnualKm)
        {
            if (car is null) throw new ArgumentNullException(nameof(car));

            return new ListingAnalysisResult
            {
                Car = car,
                Price = AnalyzePrice(car, input),
                Mileage = AnalyzeMileage(car, input, currentYear, annualKm),
                ChronicIssues = car.ChronicIssues ?? new List<ChronicIssue>(),
                Maintenance = MileageAdvisor.Check(car.MileageMilestones ?? new List<MileageMilestone>(), input.Mileage),
                Damage = AnalyzeDamage(input),
                Checklist = PurchaseChecklist.Build(car),
            };
        }

        private static PriceFinding AnalyzePrice(Car car, ListingInput input)
        {
            bool bandOk = car.MinPrice > 0 && car.MaxPrice > car.MinPrice;
            if (!bandOk)
                return new PriceFinding
                {
                    Coverage = DataCoverage.None,
                    ListingPrice = input.Price,
                    Message = "Bu araç için katalogda güvenilir bir fiyat aralığı bulunmuyor; fiyat karşılaştırması yapılamıyor."
                };

            if (input.Price is not > 0)
                return new PriceFinding
                {
                    Coverage = DataCoverage.Partial,
                    BandMin = car.MinPrice,
                    BandMax = car.MaxPrice,
                    Message = $"İlan fiyatı girilmedi. Katalog fiyat aralığı: {car.MinPrice:N0} – {car.MaxPrice:N0} ₺."
                };

            long p = input.Price.Value;
            string msg;
            if (p < car.MinPrice)
                msg = $"İlan fiyatı ({p:N0} ₺) katalog fiyat aralığının ({car.MinPrice:N0} – {car.MaxPrice:N0} ₺) altında.";
            else if (p > car.MaxPrice)
                msg = $"İlan fiyatı ({p:N0} ₺) katalog fiyat aralığının ({car.MinPrice:N0} – {car.MaxPrice:N0} ₺) üstünde.";
            else
            {
                int pct = (int)Math.Round((p - car.MinPrice) * 100.0 / (car.MaxPrice - car.MinPrice));
                msg = $"İlan fiyatı, katalog fiyat aralığının %{pct}'inde ({car.MinPrice:N0} – {car.MaxPrice:N0} ₺).";
            }

            return new PriceFinding
            {
                Coverage = DataCoverage.Full,
                ListingPrice = p,
                BandMin = car.MinPrice,
                BandMax = car.MaxPrice,
                Message = msg
            };
        }

        private static MileageFinding AnalyzeMileage(Car car, ListingInput input, int currentYear, int annualKm)
        {
            int? year = input.Year is > 1950 ? input.Year : (car.YearStart is > 1950 ? car.YearStart : null);
            int? age = year.HasValue && currentYear > year.Value ? currentYear - year.Value : null;

            if (input.Mileage is not > 0)
                return new MileageFinding
                {
                    Coverage = DataCoverage.None,
                    AgeYears = age,
                    Message = "İlan kilometresi girilmedi; kilometre değerlendirmesi yapılamıyor."
                };

            int km = input.Mileage.Value;

            if (age is not > 0)
                return new MileageFinding
                {
                    Coverage = DataCoverage.Partial,
                    ListingKm = km,
                    Message = $"Model yılı girilmedi; beklenen kilometre hesaplanamadı. İlan kilometresi: {km:N0} km."
                };

            int mid = age.Value * annualKm;
            int lo = (int)(mid * (1 - ExpectedKmSpread));
            int hi = (int)(mid * (1 + ExpectedKmSpread));

            string msg = km < lo
                ? $"İlan kilometresi ({km:N0} km), {age} yıllık kullanım için beklenen aralığın ({lo:N0} – {hi:N0} km) altında."
                : km > hi
                    ? $"İlan kilometresi ({km:N0} km), {age} yıllık kullanım için beklenen aralığın ({lo:N0} – {hi:N0} km) üstünde."
                    : $"İlan kilometresi ({km:N0} km), {age} yıllık kullanım için beklenen aralıkta ({lo:N0} – {hi:N0} km).";

            return new MileageFinding
            {
                Coverage = DataCoverage.Full,
                ListingKm = km,
                AgeYears = age,
                ExpectedMin = lo,
                ExpectedMax = hi,
                Message = msg + $" (yıllık ~{annualKm:N0} km varsayımıyla)"
            };
        }

        private static DamageFinding AnalyzeDamage(ListingInput input)
        {
            if (input.HasDamageRecord is null && input.PaintedPanels is null)
                return new DamageFinding
                {
                    Coverage = DataCoverage.None,
                    Message = "Hasar kaydı ve boya bilgisi girilmedi. Bu bilgiler tahmin edilmez — ekspertizde ve tramer sorgusunda mutlaka kontrol ettirin."
                };

            var parts = new List<string>();
            if (input.HasDamageRecord is bool hd)
                parts.Add(hd ? "Kullanıcı beyanı: hasar/tramer kaydı var." : "Kullanıcı beyanı: hasar/tramer kaydı yok.");
            if (input.PaintedPanels is int pp)
                parts.Add(pp == 0 ? "Boyalı parça beyan edilmedi." : $"Kullanıcı beyanı: {pp} boyalı/değişen parça.");

            return new DamageFinding
            {
                Coverage = input.HasDamageRecord is not null && input.PaintedPanels is not null
                    ? DataCoverage.Full : DataCoverage.Partial,
                Message = string.Join(" ", parts) + " Beyanı ekspertiz ve resmi tramer sorgusuyla doğrulayın."
            };
        }
    }
}
