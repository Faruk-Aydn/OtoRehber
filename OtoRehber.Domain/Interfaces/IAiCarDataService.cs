using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Interfaces
{
    public interface IAiCarDataService
    {
        Task<List<Car>> AnalyzeAndSaveFromYoutubeAsync(string youtubeUrl, CancellationToken cancellationToken = default);
        Task<string> GetCarRecommendationAsync(string userMessage, string availableCarsContext, CancellationToken cancellationToken = default);
        Task<string> GetComparisonVerdictAsync(Car car1, Car car2, CancellationToken cancellationToken = default);
    }
}
