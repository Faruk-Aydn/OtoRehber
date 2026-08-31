using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using OtoRehber.Domain;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Services
{
    // Home ve /araclar (CatalogController) aynı filtre/sıralama mantığını paylaşır.
    public static class CarCatalogQuery
    {
        public static IQueryable<Car> ApplyFilters(IQueryable<Car> query, CarFilter f)
        {
            if (!string.IsNullOrWhiteSpace(f.SearchQuery))
            {
                var s = f.SearchQuery.Trim().ToLowerInvariant();
                query = query.Where(c => c.Brand.ToLower().Contains(s) || c.ModelName.ToLower().Contains(s));
            }

            var brand = CarFilter.Clean(f.Brand);
            if (brand.Length > 0) query = query.Where(c => brand.Contains(c.Brand));

            var segment = CarFilter.Clean(f.Segment);
            if (segment.Length > 0) query = query.Where(c => segment.Contains(c.Segment));

            var fuel = CarFilter.Clean(f.Fuel);
            if (fuel.Length > 0) query = query.Where(c => c.FuelType != null && fuel.Contains(c.FuelType));

            var trans = CarFilter.Clean(f.Transmission);
            if (trans.Length > 0) query = query.Where(c => c.Transmission != null && trans.Contains(c.Transmission));

            var body = CarFilter.Clean(f.BodyType);
            if (body.Length > 0) query = query.Where(c => c.BodyType != null && body.Contains(c.BodyType));

            var drive = CarFilter.Clean(f.Drivetrain);
            if (drive.Length > 0) query = query.Where(c => c.Drivetrain != null && drive.Contains(c.Drivetrain));

            var cond = CarFilter.Clean(f.Condition);
            if (cond.Length > 0) query = query.Where(c => c.Condition != null && cond.Contains(c.Condition));

            // Fiyat: aracın [MinPrice, MaxPrice] aralığı filtre aralığıyla kesişiyorsa dahil.
            if (f.PriceMin is > 0) query = query.Where(c => c.MaxPrice >= f.PriceMin);
            if (f.PriceMax is > 0) query = query.Where(c => c.MinPrice <= f.PriceMax);

            if (f.MinScore is > 0) query = query.Where(c => c.ReliabilityScore >= f.MinScore);

            // Yıl: model üretim aralığı [YearStart, YearEnd] filtre aralığıyla kesişiyorsa dahil.
            if (f.YearMin is > 0) query = query.Where(c => c.YearEnd == null || c.YearEnd >= f.YearMin);
            if (f.YearMax is > 0) query = query.Where(c => c.YearStart == null || c.YearStart <= f.YearMax);

            // Motor gücü / hacmi: seçili hazır aralıklar OR ile birleştirilir (boşluk atlamaz).
            var powerRanges = CarFilter.Clean(f.Power).Select(CarSpecs.PowerRange).Where(r => r != null).Select(r => r!.Value).ToList();
            if (powerRanges.Count > 0)
                query = query.Where(BucketOr<Car>(c => c.PowerHp, powerRanges));

            var ccRanges = CarFilter.Clean(f.Cc).Select(CarSpecs.CcRange).Where(r => r != null).Select(r => r!.Value).ToList();
            if (ccRanges.Count > 0)
                query = query.Where(BucketOr<Car>(c => c.EngineDisplacementCc, ccRanges));

            return query;
        }

        /// <summary>
        /// `entity => value != null && ((value >= min1 && value <= max1) || (value >= min2 && value <= max2) ...)`
        /// ifadesini kurar — çoklu hazır aralık seçimini SQL'e çevrilebilir tek bir OR koşuluna dönüştürür.
        /// </summary>
        private static Expression<Func<T, bool>> BucketOr<T>(Expression<Func<T, int?>> selector, List<(int Min, int Max)> ranges)
        {
            var p = selector.Parameters[0];
            var val = selector.Body; // int?
            var notNull = Expression.NotEqual(val, Expression.Constant(null, typeof(int?)));
            var valInt = Expression.Convert(val, typeof(int)); // (int)value — null zaten notNull ile elenir

            Expression? orChain = null;
            foreach (var (min, max) in ranges)
            {
                var inRange = Expression.AndAlso(
                    Expression.GreaterThanOrEqual(valInt, Expression.Constant(min)),
                    Expression.LessThanOrEqual(valInt, Expression.Constant(max)));
                orChain = orChain == null ? inRange : Expression.OrElse(orChain, inRange);
            }

            var body = Expression.AndAlso(notNull, orChain!);
            return Expression.Lambda<Func<T, bool>>(body, p);
        }

        public static IQueryable<Car> ApplySort(IQueryable<Car> query, string? sortBy) => sortBy switch
        {
            "price_asc" => query.OrderBy(c => c.MinPrice),
            "price_desc" => query.OrderByDescending(c => c.MinPrice),
            "score_desc" => query.OrderByDescending(c => c.ReliabilityScore),
            "score_asc" => query.OrderBy(c => c.ReliabilityScore),
            "year_desc" => query.OrderByDescending(c => c.YearEnd ?? c.YearStart ?? 0),
            _ => query.OrderByDescending(c => c.Id)
        };
    }
}
