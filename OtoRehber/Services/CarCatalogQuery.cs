using System.Linq;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Services
{
    // Home ve /araclar (CatalogController) aynı filtre/sıralama mantığını paylaşır.
    public static class CarCatalogQuery
    {
        public static IQueryable<Car> ApplyFilters(
            IQueryable<Car> query,
            string? search,
            string[]? segment,
            string[]? brand,
            long? priceMin = null,
            long? priceMax = null,
            double? minScore = null)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(c => c.Brand.ToLower().Contains(s) || c.ModelName.ToLower().Contains(s));
            }
            if (segment != null && segment.Any())
                query = query.Where(c => segment.Contains(c.Segment));
            if (brand != null && brand.Any())
                query = query.Where(c => brand.Contains(c.Brand));

            // Aracın [MinPrice, MaxPrice] aralığı, filtre [priceMin, priceMax] ile kesişiyorsa dahil et.
            if (priceMin is > 0)
                query = query.Where(c => c.MaxPrice >= priceMin);
            if (priceMax is > 0)
                query = query.Where(c => c.MinPrice <= priceMax);

            if (minScore is > 0)
                query = query.Where(c => c.ReliabilityScore >= minScore);

            return query;
        }

        public static IQueryable<Car> ApplySort(IQueryable<Car> query, string? sortBy) => sortBy switch
        {
            "price_asc" => query.OrderBy(c => c.MinPrice),
            "price_desc" => query.OrderByDescending(c => c.MinPrice),
            "score_desc" => query.OrderByDescending(c => c.ReliabilityScore),
            "score_asc" => query.OrderBy(c => c.ReliabilityScore),
            _ => query.OrderByDescending(c => c.Id)
        };
    }
}
