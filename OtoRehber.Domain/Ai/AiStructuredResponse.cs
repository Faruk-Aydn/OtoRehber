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
        /// <summary>İddianın ait olduğu araç (PRD v5 §4.6 kontrol 2). Bağlamda "ARAÇ #N" olarak verilir.</summary>
        public int? VehicleId { get; set; }
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
    /// Claim doğrulama (PRD v5 §4.6): (1) referenceId gerçekten var mı, (2) ilgili araca
    /// ait mi, (3) claim türü geçerli mi. Başarısız → REJECT (loglanır, özet yine gösterilir).
    /// </summary>
    public static class AiClaimValidator
    {
        public const string KnownIssue = "known_issue";
        public const string Maintenance = "maintenance";

        public static string IssueRef(int id) => $"issue-{id}";
        public static string MaintenanceRef(int id) => $"maint-{id}";

        /// <param name="issueRefOwner">issue-ref → araç Id (kontrol 2 için).</param>
        /// <param name="maintenanceRefOwner">maint-ref → araç Id.</param>
        public static ClaimValidationResult Validate(
            IEnumerable<AiClaim>? claims,
            IReadOnlyDictionary<string, int> issueRefOwner,
            IReadOnlyDictionary<string, int> maintenanceRefOwner)
        {
            var result = new ClaimValidationResult();
            foreach (var claim in claims ?? Enumerable.Empty<AiClaim>())
            {
                var type = (claim.Type ?? "").Trim().ToLowerInvariant();
                var refId = (claim.ReferenceId ?? "").Trim();
                var owner = type == KnownIssue ? issueRefOwner
                          : type == Maintenance ? maintenanceRefOwner
                          : null;

                if (owner is null)
                {
                    result.Rejected.Add(new(claim, ClaimRejectReason.UnknownType));
                }
                else if (!owner.TryGetValue(refId, out var ownerVehicleId))
                {
                    result.Rejected.Add(new(claim, ClaimRejectReason.IdNotFound));
                }
                else if (claim.VehicleId is int vid && ownerVehicleId != 0 && vid != ownerVehicleId)
                {
                    // Kontrol 2: AI referansı başka bir araca atfetti (owner 0 = araç bağı bilinmiyor, atla).
                    result.Rejected.Add(new(claim, ClaimRejectReason.NotLinkedToVehicle));
                }
                else
                {
                    result.Accepted.Add(claim);
                }
            }
            return result;
        }
    }
}
