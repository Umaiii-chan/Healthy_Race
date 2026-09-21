using System;
using HealthyRace.Visuals;

namespace HealthyRace.Scoring
{
    /// <summary>
    /// Detailed score breakdown and total for a single player.
    /// </summary>
    [Serializable]
    public struct PlayerScoreResult
    {
        public int PlayerIndex;
        public int HealthyPoints;
        public int PuzzlePoints;
        public int TimeBonus;
        public int ArrivalBonus;
        public int WellbeingBonus;
        public int UltraPenalty;
        public int FatiguePenalty;
        public int DnfPenalty;
        public int FinalScore;

        public PlayerResultData ToVisualResultData(
            PlayerHudData hudData,
            float seconds,
            bool finished
        )
        {
            return new PlayerResultData
            {
                Player = hudData,
                Seconds = seconds,
                Finished = finished,
                HealthyPoints = HealthyPoints,
                PuzzlePoints = PuzzlePoints,
                TimeBonus = TimeBonus,
                ArrivalBonus = ArrivalBonus,
                WellbeingBonus = WellbeingBonus,
                UltraPenalty = UltraPenalty,
                FatiguePenalty = FatiguePenalty,
                UnfinishedPenalty = DnfPenalty,
            };
        }
    }
}
