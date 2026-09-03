using System.Collections.Generic;
using System.Linq;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Advisory
{
    public readonly record struct ChecklistItem(string Text, bool FromChronicIssue);

    /// <summary>
    /// Satın alma kontrol listesi (PRD v5 §2.6) — kronik sorun tablosundan otomatik
    /// türetilir + sabit maddeler (ekspertiz, hasar kaydı, servis geçmişi).
    /// Aynı mantık İlan Analizi'nde de yeniden kullanılır (Session 5).
    /// </summary>
    public static class PurchaseChecklist
    {
        public static readonly string[] FixedItems =
        {
            "Yetkili veya güvendiğiniz bağımsız bir serviste ekspertiz yaptırın.",
            "Tramer / hasar kaydını sorgulayın; değişen ve boyalı parçaları not edin.",
            "Servis geçmişini ve düzenli bakım faturalarını isteyin.",
            "OBD arıza tarama cihazıyla hata kayıtlarını okutun.",
            "Şasi ve motor numaralarının ruhsat bilgileriyle uyumunu kontrol edin.",
        };

        public static IReadOnlyList<ChecklistItem> Build(Car car)
        {
            var items = new List<ChecklistItem>();

            foreach (var issue in (car.ChronicIssues ?? new List<ChronicIssue>())
                         .OrderByDescending(i => SeverityRank(i.Severity)))
            {
                items.Add(new ChecklistItem(
                    $"“{issue.IssueTitle}” açısından detaylı kontrol ettirin ({issue.Severity.ToLowerInvariant()} risk).",
                    FromChronicIssue: true));
            }

            items.AddRange(FixedItems.Select(t => new ChecklistItem(t, FromChronicIssue: false)));
            return items;
        }

        private static int SeverityRank(string? s) => (s ?? "").Trim().ToLowerInvariant() switch
        {
            "kritik" or "critical" => 3,
            "orta" or "medium" => 2,
            "düşük" or "dusuk" or "low" => 1,
            _ => 0
        };
    }
}
