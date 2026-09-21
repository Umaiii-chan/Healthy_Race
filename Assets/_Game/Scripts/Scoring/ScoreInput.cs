using System;

namespace HealthyRace.Scoring
{
    /// <summary>
    /// Immutable input data representing a player's performance during a match,
    /// used by ScoreService to calculate scores without coupling to game actors or UI.
    /// </summary>
    [Serializable]
    public struct ScoreInput
    {
        public int PlayerIndex;
        public float FinishTime;
        public bool Finished;
        public int ArrivalPlacement; // 1 = 1st, 2 = 2nd, 0 = DNF/none
        public int FinalWellbeing;
        public int HealthyPointsEarned;
        public int HealthyFoodsCount;
        public int UltraProcessedFoodsCount;
        public int PuzzlesSolvedCount;
        public int FatigueEpisodesCount;
    }
}
