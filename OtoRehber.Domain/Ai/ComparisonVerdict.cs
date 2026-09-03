using OtoRehber.Domain.Scoring;

namespace OtoRehber.Domain.Ai
{
    public enum ComparisonWinner { VehicleA, VehicleB, Tie, Undetermined }

    /// <summary>
    /// AI Karşılaştırma kazananı <b>backend</b> tarafından belirlenir (PRD v5 §4.5) —
    /// canonical OtoRehber Skoru'na göre. AI winner'ı değiştiremez, yeni skor üretemez.
    /// </summary>
    public static class ComparisonVerdict
    {
        /// <summary>İki skor 0.1'den yakınsa berabere; biri N/A ise kazanan belirlenemez.</summary>
        public const double TieThreshold = 0.1;

        public static ComparisonWinner Decide(ScoreResult a, ScoreResult b)
        {
            if (a.Overall is not double oa || b.Overall is not double ob)
                return ComparisonWinner.Undetermined;

            var diff = oa - ob;
            if (diff > TieThreshold) return ComparisonWinner.VehicleA;
            if (diff < -TieThreshold) return ComparisonWinner.VehicleB;
            return ComparisonWinner.Tie;
        }
    }
}
