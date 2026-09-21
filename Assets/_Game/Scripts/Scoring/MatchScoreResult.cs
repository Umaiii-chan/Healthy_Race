using System;

namespace HealthyRace.Scoring
{
    public enum TieBreakReason
    {
        ScoreDifference,
        WellbeingDifference,
        FinishTimeDifference,
        HealthyFoodsDifference,
        FatigueEpisodesDifference,
        AbsoluteTie,
    }

    /// <summary>
    /// Consolidated match scoring results, ranking, and deterministic tie-break evaluation.
    /// </summary>
    [Serializable]
    public struct MatchScoreResult
    {
        public PlayerScoreResult Player1;
        public PlayerScoreResult Player2;
        public int WinnerPlayerIndex; // 0 = tie, 1 = P1, 2 = P2
        public bool IsTie;
        public TieBreakReason Reason;
    }
}
