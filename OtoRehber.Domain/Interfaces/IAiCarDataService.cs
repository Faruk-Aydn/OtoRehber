using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OtoRehber.Domain.Ai;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Interfaces
{
    /// <summary>
    /// AI = Explanation + Interpretation + Clarification Layer (PRD v5 §4). AI:
    /// skor hesaplamaz · ranking yapmaz · filtre uygulamaz · kazananı seçmez ·
    /// backend aday listesini değiştirmez · DB'de olmayan veri üretmez.
    ///
    /// <paramref name="issueRefOwner"/> / <paramref name="maintenanceRefOwner"/>: bağlamdaki
    /// <c>issue-N</c> / <c>maint-N</c> referanslarının hangi araca ait olduğu — claim
    /// doğrulaması (§4.6) referansın gerçek + doğru araca bağlı olduğunu kontrol eder.
    /// </summary>
    public interface IAiCarDataService
    {
        Task<List<Car>> AnalyzeAndSaveFromYoutubeAsync(string youtubeUrl, CancellationToken cancellationToken = default);

        Task<AiExplanation> ExplainWizardCandidatesAsync(
            string candidatesContext, string preferencesText, string? eliminatedContext,
            IReadOnlyDictionary<string, int> issueRefOwner, IReadOnlyDictionary<string, int> maintenanceRefOwner,
            CancellationToken cancellationToken = default);

        Task<AiExplanation> ExplainComparisonAsync(
            string bothVehiclesContext, ComparisonWinner winner, string vehicleALabel, string vehicleBLabel,
            IReadOnlyDictionary<string, int> issueRefOwner, IReadOnlyDictionary<string, int> maintenanceRefOwner,
            CancellationToken cancellationToken = default);

        Task<AiExplanation> AnswerQuestionAsync(
            string question, string? vehicleContext,
            IReadOnlyDictionary<string, int> issueRefOwner, IReadOnlyDictionary<string, int> maintenanceRefOwner,
            CancellationToken cancellationToken = default);
    }
}
