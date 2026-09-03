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
    /// </summary>
    public interface IAiCarDataService
    {
        /// <summary>Admin: YouTube transkriptinden araç verisi çıkarma (Session 4 kapsamı dışı).</summary>
        Task<List<Car>> AnalyzeAndSaveFromYoutubeAsync(string youtubeUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sihirbaz: backend'in belirlediği adayları açıklar (PRD v5 §4.3). Listeye araç ekleyip
        /// çıkaramaz; yalnızca verilen adayları kullanıcının önceliklerine göre yorumlar.
        /// </summary>
        Task<AiExplanation> ExplainWizardCandidatesAsync(
            string candidatesContext, string preferencesText,
            ISet<string> allowedIssueRefs, ISet<string> allowedMaintenanceRefs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Karşılaştırma: kazanan <b>verilir</b> (PRD v5 §4.5), AI yalnızca nedenini ve hangi
        /// profil için diğer aracın mantıklı olduğunu açıklar. Winner'ı değiştiremez.
        /// </summary>
        Task<AiExplanation> ExplainComparisonAsync(
            string bothVehiclesContext, ComparisonWinner winner, string vehicleALabel, string vehicleBLabel,
            ISet<string> allowedIssueRefs, ISet<string> allowedMaintenanceRefs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Chat: verilen bağlamdaki (tek araç veya genel) soruyu yanıtlar (PRD v5 §4.7/§4.8).
        /// Bağlam dışı bilgi istenirse sabit "veritabanında yeterli bilgi yok" yanıtı.
        /// </summary>
        Task<AiExplanation> AnswerQuestionAsync(
            string question, string? vehicleContext,
            ISet<string> allowedIssueRefs, ISet<string> allowedMaintenanceRefs,
            CancellationToken cancellationToken = default);
    }
}
