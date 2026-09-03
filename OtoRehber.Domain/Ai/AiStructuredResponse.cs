using System;
using System.Collections.Generic;
using System.Linq;

namespace OtoRehber.Domain.Ai
{
    /// <summary>
    /// AI'nın döndürdüğü yapılandırılmış yanıt (PRD v5 §4.6). Her sayısal/faktüel iddia
    /// (<see cref="AiClaim"/>) bir <c>referenceId</c>'ye bağlıdır; backend doğrular.
    /// </summary>
    public sealed class AiStructuredResponse
    {
        public string Summary { get; set; } = "";
        public List<AiClaim> Claims { get; set; } = new();
    }

    public sealed class AiClaim
    {
        /// <summary>"known_issue" | "maintenance"</summary>
        public string Type { get; set; } = "";
        public string ReferenceId { get; set; } = "";
    }

    public enum ClaimRejectReason { UnknownType, IdNotFound, NotLinkedToVehicle }

    public readonly record struct RejectedClaim(AiClaim Claim, ClaimRejectReason Reason);

    public sealed class ClaimValidationResult
    {
        public List<AiClaim> Accepted { get; init; } = new();
        public List<RejectedClaim> Rejected { get; init; } = new();
        public bool HasRejections => Rejected.Count > 0;
    }

    /// <summary>
    /// Claim doğrulama (PRD v5 §4.6): referenceId gerçekten var mı, ilgili araca ait mi,
    /// claim türü geçerli mi. Başarısız → REJECT (loglanır, özet yine gösterilir).
    /// </summary>
    public static class AiClaimValidator
    {
        public const string KnownIssue = "known_issue";
        public const string Maintenance = "maintenance";

        public static string IssueRef(int id) => $"issue-{id}";
        public static string MaintenanceRef(int id) => $"maint-{id}";

        public static ClaimValidationResult Validate(
            IEnumerable<AiClaim>? claims,
            ISet<string> allowedIssueRefs,
            ISet<string> allowedMaintenanceRefs)
        {
            var result = new ClaimValidationResult();
            foreach (var claim in claims ?? Enumerable.Empty<AiClaim>())
            {
                var type = (claim.Type ?? "").Trim().ToLowerInvariant();
                var refId = (claim.ReferenceId ?? "").Trim();

                if (type == KnownIssue)
                {
                    if (allowedIssueRefs.Contains(refId)) result.Accepted.Add(claim);
                    else result.Rejected.Add(new(claim, ClaimRejectReason.IdNotFound));
                }
                else if (type == Maintenance)
                {
                    if (allowedMaintenanceRefs.Contains(refId)) result.Accepted.Add(claim);
                    else result.Rejected.Add(new(claim, ClaimRejectReason.IdNotFound));
                }
                else
                {
                    result.Rejected.Add(new(claim, ClaimRejectReason.UnknownType));
                }
            }
            return result;
        }
    }
}
