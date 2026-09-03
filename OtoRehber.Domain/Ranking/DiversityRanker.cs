using System;
using System.Collections.Generic;
using System.Linq;

namespace OtoRehber.Domain.Ranking
{
    /// <summary>
    /// Presentation Ranking (PRD v5 §3.2.1) — <b>Canonical Ranking'i değiştirmeden</b>
    /// yeniden düzenler: aynı ana modelin sıralı listeyi domine etmesini engeller.
    ///
    /// - Canonical (skora göre) sıra korunur; sadece cap'i aşan varyantlar listenin
    ///   <b>sonuna ötelenir</b> — hiçbir araç atılmaz (farklı nesil/motor kaybolmaz).
    /// - <paramref name="maxSameMainModel"/> ≤ 0 → diversity kapalı (saf canonical).
    /// - Deterministik: canonical sıra deterministikse çıktı da deterministiktir.
    /// </summary>
    public static class DiversityRanker
    {
        public static List<T> Presentation<T>(
            IReadOnlyList<T> canonicalOrdered,
            Func<T, string> mainModelKey,
            int maxSameMainModel)
        {
            if (maxSameMainModel <= 0)
                return canonicalOrdered.ToList();

            var counts = new Dictionary<string, int>();
            var kept = new List<T>(canonicalOrdered.Count);
            var deferred = new List<T>();

            foreach (var item in canonicalOrdered)
            {
                var key = mainModelKey(item);
                counts.TryGetValue(key, out var c);
                if (c < maxSameMainModel)
                {
                    kept.Add(item);
                    counts[key] = c + 1;
                }
                else
                {
                    deferred.Add(item);
                }
            }

            kept.AddRange(deferred);
            return kept;
        }
    }
}
