using System;

namespace HealthyRace.Scoring
{
    /// <summary>
    /// Pure, deterministic service for computing player score breakdowns, totals,
    /// and match rankings with a strict tie-break hierarchy.
    /// Does not depend on MonoBehaviour, GameObjects, or UI.
    /// </summary>
    public static class ScoreService
    {
        public static PlayerScoreResult CalculatePlayerScore(
            ScoreInput input,
            ScoreConfig config = null
        )
        {
            if (config == null)
            {
                config = ScoreConfig.CreateDefault();
            }

            int healthyPoints = input.HealthyPointsEarned;
            int puzzlePoints = input.PuzzlesSolvedCount * config.PointsPerPuzzle;

            int timeBonus = 0;
            if (
                input.Finished
                && input.FinishTime > 0f
                && input.FinishTime <= config.TargetTimeSeconds
            )
            {
                timeBonus = config.MaxTimeBonus;
            }

            int arrivalBonus = 0;
            if (input.Finished)
            {
                if (input.ArrivalPlacement == 1)
                {
                    arrivalBonus = config.FirstPlaceArrivalBonus;
                }
                else if (input.ArrivalPlacement == 2)
                {
                    arrivalBonus = config.SecondPlaceArrivalBonus;
                }
            }

            int wellbeingBonus = input.FinalWellbeing * config.WellbeingMultiplier;
            int ultraPenalty = input.UltraProcessedFoodsCount * config.UltraPenaltyPerItem;
            int fatiguePenalty = input.FatigueEpisodesCount * config.FatiguePenaltyPerEpisode;
            int dnfPenalty = input.Finished ? 0 : config.DnfPenalty;

            int finalScore =
                healthyPoints
                + puzzlePoints
                + timeBonus
                + arrivalBonus
                + wellbeingBonus
                - ultraPenalty
                - fatiguePenalty
                - dnfPenalty;

            return new PlayerScoreResult
            {
                PlayerIndex = input.PlayerIndex,
                HealthyPoints = healthyPoints,
                PuzzlePoints = puzzlePoints,
                TimeBonus = timeBonus,
                ArrivalBonus = arrivalBonus,
                WellbeingBonus = wellbeingBonus,
                UltraPenalty = ultraPenalty,
                FatiguePenalty = fatiguePenalty,
                DnfPenalty = dnfPenalty,
                FinalScore = finalScore,
            };
        }

        public static MatchScoreResult EvaluateMatch(
            ScoreInput p1,
            ScoreInput p2,
            ScoreConfig config = null
        )
        {
            var r1 = CalculatePlayerScore(p1, config);
            var r2 = CalculatePlayerScore(p2, config);

            // Deterministic tie-break hierarchy:
            // 1. Final Score
            if (r1.FinalScore > r2.FinalScore)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 1,
                    IsTie = false,
                    Reason = TieBreakReason.ScoreDifference,
                };
            }
            if (r2.FinalScore > r1.FinalScore)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 2,
                    IsTie = false,
                    Reason = TieBreakReason.ScoreDifference,
                };
            }

            // 2. Final Wellbeing
            if (p1.FinalWellbeing > p2.FinalWellbeing)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 1,
                    IsTie = false,
                    Reason = TieBreakReason.WellbeingDifference,
                };
            }
            if (p2.FinalWellbeing > p1.FinalWellbeing)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 2,
                    IsTie = false,
                    Reason = TieBreakReason.WellbeingDifference,
                };
            }

            // 3. Finish Time (Finished beats DNF; lower time beats higher time)
            if (p1.Finished && !p2.Finished)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 1,
                    IsTie = false,
                    Reason = TieBreakReason.FinishTimeDifference,
                };
            }
            if (p2.Finished && !p1.Finished)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 2,
                    IsTie = false,
                    Reason = TieBreakReason.FinishTimeDifference,
                };
            }
            if (p1.Finished && p2.Finished)
            {
                if (p1.FinishTime < p2.FinishTime)
                {
                    return new MatchScoreResult
                    {
                        Player1 = r1,
                        Player2 = r2,
                        WinnerPlayerIndex = 1,
                        IsTie = false,
                        Reason = TieBreakReason.FinishTimeDifference,
                    };
                }
                if (p2.FinishTime < p1.FinishTime)
                {
                    return new MatchScoreResult
                    {
                        Player1 = r1,
                        Player2 = r2,
                        WinnerPlayerIndex = 2,
                        IsTie = false,
                        Reason = TieBreakReason.FinishTimeDifference,
                    };
                }
            }

            // 4. Healthy Foods Count (more is better)
            if (p1.HealthyFoodsCount > p2.HealthyFoodsCount)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 1,
                    IsTie = false,
                    Reason = TieBreakReason.HealthyFoodsDifference,
                };
            }
            if (p2.HealthyFoodsCount > p1.HealthyFoodsCount)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 2,
                    IsTie = false,
                    Reason = TieBreakReason.HealthyFoodsDifference,
                };
            }

            // 5. Fatigue Episodes Count (fewer is better)
            if (p1.FatigueEpisodesCount < p2.FatigueEpisodesCount)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 1,
                    IsTie = false,
                    Reason = TieBreakReason.FatigueEpisodesDifference,
                };
            }
            if (p2.FatigueEpisodesCount < p1.FatigueEpisodesCount)
            {
                return new MatchScoreResult
                {
                    Player1 = r1,
                    Player2 = r2,
                    WinnerPlayerIndex = 2,
                    IsTie = false,
                    Reason = TieBreakReason.FatigueEpisodesDifference,
                };
            }

            // 6. Absolute Tie
            return new MatchScoreResult
            {
                Player1 = r1,
                Player2 = r2,
                WinnerPlayerIndex = 0,
                IsTie = true,
                Reason = TieBreakReason.AbsoluteTie,
            };
        }
    }
}
