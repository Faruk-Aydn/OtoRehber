using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OtoRehber.Domain.Entities;

namespace OtoRehber.Domain.Advisory
{
    public enum MilestoneStatus { Passed, Due, Upcoming, Far }

    public readonly record struct MilestoneCheck(
        MileageMilestone Milestone, int? Km, MilestoneStatus Status);

    /// <summary>
    /// Km bazlı bakım kontrolü (PRD v5 §2.5) — <b>statik eşik karşılaştırması</b>,
    /// AI çağrısı yoktur. Kullanıcının girdiği km, aracın bakım barajlarıyla kıyaslanır.
    /// </summary>
    public static class MileageAdvisor
    {
        // "60.000 km", "100.000 km", "120-150k km", "90bin km" → ilk sayı (bin ayracı '.'/',' atılır, 'k'/'bin' ×1000).
        public static int? ParseKm(string? mileage)
        {
            if (string.IsNullOrWhiteSpace(mileage)) return null;
            // "k" yalnızca "km" birimi değilse binler çarpanı ("150k" evet, "40.000 km" hayır).
            var m = Regex.Match(mileage, @"(\d[\d.,]*)\s*(k(?!m)|bin)?", RegexOptions.IgnoreCase);
            if (!m.Success) return null;

            var digits = m.Groups[1].Value.Replace(".", "").Replace(",", "");
            if (!long.TryParse(digits, out var n)) return null;

            bool thousand = m.Groups[2].Success;
            if (thousand) n *= 1000;
            // "150k" → 150000; "60" (nadiren) → muhtemelen bin cinsinden değil, olduğu gibi bırak.
            if (n <= 0 || n > 2_000_000) return null;
            return (int)n;
        }

        /// <param name="userKm">Kullanıcının girdiği güncel km. null ise tüm barajlar "bilgi amaçlı" listelenir.</param>
        public static IReadOnlyList<MilestoneCheck> Check(IEnumerable<MileageMilestone> milestones, int? userKm)
        {
            var list = (milestones ?? Enumerable.Empty<MileageMilestone>())
                .Select(ms => new { ms, km = ParseKm(ms.Mileage) })
                .OrderBy(x => x.km ?? int.MaxValue)
                .ToList();

            return list.Select(x =>
            {
                MilestoneStatus status;
                if (userKm is not int u || x.km is not int k)
                    status = MilestoneStatus.Far;
                else if (k <= u - 10_000) status = MilestoneStatus.Passed;
                else if (k <= u + 5_000) status = MilestoneStatus.Due;
                else if (k <= u + 30_000) status = MilestoneStatus.Upcoming;
                else status = MilestoneStatus.Far;
                return new MilestoneCheck(x.ms, x.km, status);
            }).ToList();
        }
    }
}
