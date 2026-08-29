using System.Linq;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Services
{
    // Home ve /araclar (CatalogController) aynı filtre/sıralama mantığını paylaşır.
    public static class CarCatalogQuery
    {
        public static IQueryable<Car> ApplyFilters(IQueryable<Car> query, string? search, string? segment, string? brand)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(c => c.Brand.ToLower().Contains(s) || c.ModelName.ToLower().Contains(s));
            }
            if (!string.IsNullOrWhiteSpace(segment))
                query = query.Where(c => c.Segment == segment);
            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(c => c.Brand == brand);
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
