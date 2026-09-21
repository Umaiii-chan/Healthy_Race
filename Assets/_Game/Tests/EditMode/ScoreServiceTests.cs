using HealthyRace.Scoring;
using NUnit.Framework;

namespace HealthyRace.Tests.EditMode
{
    [TestFixture]
    public sealed class ScoreServiceTests
    {
        [Test]
        public void CalculatePlayerScore_ArtBibleReferenceP1_CalculatesExactExpectedBreakdownAndScore()
        {
            var p1Input = new ScoreInput
            {
                PlayerIndex = 1,
                FinishTime = 179f,
                Finished = true,
                ArrivalPlacement = 2,
                FinalWellbeing = 88,
                HealthyPointsEarned = 800,
                HealthyFoodsCount = 7,
                UltraProcessedFoodsCount = 1,
                PuzzlesSolvedCount = 2,
                FatigueEpisodesCount = 0,
            };

            var result = ScoreService.CalculatePlayerScore(p1Input);

            Assert.That(result.PlayerIndex, Is.EqualTo(1));
            Assert.That(result.HealthyPoints, Is.EqualTo(800));
            Assert.That(result.PuzzlePoints, Is.EqualTo(300));
            Assert.That(result.TimeBonus, Is.EqualTo(600));
            Assert.That(result.ArrivalBonus, Is.EqualTo(100));
            Assert.That(result.WellbeingBonus, Is.EqualTo(440));
            Assert.That(result.UltraPenalty, Is.EqualTo(100));
            Assert.That(result.FatiguePenalty, Is.EqualTo(0));
            Assert.That(result.DnfPenalty, Is.EqualTo(0));
            Assert.That(result.FinalScore, Is.EqualTo(2140));
        }

        [Test]
        public void CalculatePlayerScore_ArtBibleReferenceP2_CalculatesExactExpectedBreakdownAndScore()
        {
            var p2Input = new ScoreInput
            {
                PlayerIndex = 2,
                FinishTime = 163f,
                Finished = true,
                ArrivalPlacement = 1,
                FinalWellbeing = 35,
                HealthyPointsEarned = 430,
                HealthyFoodsCount = 4,
                UltraProcessedFoodsCount = 3,
                PuzzlesSolvedCount = 1,
                FatigueEpisodesCount = 1,
            };

            var result = ScoreService.CalculatePlayerScore(p2Input);

            Assert.That(result.PlayerIndex, Is.EqualTo(2));
            Assert.That(result.HealthyPoints, Is.EqualTo(430));
            Assert.That(result.PuzzlePoints, Is.EqualTo(150));
            Assert.That(result.TimeBonus, Is.EqualTo(600));
            Assert.That(result.ArrivalBonus, Is.EqualTo(200));
            Assert.That(result.WellbeingBonus, Is.EqualTo(175));
            Assert.That(result.UltraPenalty, Is.EqualTo(300));
            Assert.That(result.FatiguePenalty, Is.EqualTo(200));
            Assert.That(result.DnfPenalty, Is.EqualTo(0));
            Assert.That(result.FinalScore, Is.EqualTo(1055));
        }

        [Test]
        public void CalculatePlayerScore_UnfinishedPlayer_ReceivesDnfPenaltyAndNoArrivalOrTimeBonus()
        {
            var dnfInput = new ScoreInput
            {
                PlayerIndex = 2,
                FinishTime = -1f,
                Finished = false,
                ArrivalPlacement = 0,
                FinalWellbeing = 60,
                HealthyPointsEarned = 200,
                HealthyFoodsCount = 2,
                UltraProcessedFoodsCount = 0,
                PuzzlesSolvedCount = 0,
                FatigueEpisodesCount = 0,
            };

            var result = ScoreService.CalculatePlayerScore(dnfInput);

            Assert.That(result.TimeBonus, Is.EqualTo(0));
            Assert.That(result.ArrivalBonus, Is.EqualTo(0));
            Assert.That(result.DnfPenalty, Is.EqualTo(500));
            // 200 (healthy) + 0 + 0 + 0 + 300 (60 * 5) - 0 - 0 - 500 = 0
            Assert.That(result.FinalScore, Is.EqualTo(0));
        }

        [Test]
        public void EvaluateMatch_WinnerByScore_WhenScoreDiffers()
        {
            var p1 = new ScoreInput
            {
                PlayerIndex = 1,
                Finished = true,
                FinishTime = 179f,
                ArrivalPlacement = 2,
                FinalWellbeing = 88,
                HealthyPointsEarned = 800,
                PuzzlesSolvedCount = 2,
                UltraProcessedFoodsCount = 1,
            };

            var p2 = new ScoreInput
            {
                PlayerIndex = 2,
                Finished = true,
                FinishTime = 163f,
                ArrivalPlacement = 1,
                FinalWellbeing = 35,
                HealthyPointsEarned = 430,
                PuzzlesSolvedCount = 1,
                UltraProcessedFoodsCount = 3,
                FatigueEpisodesCount = 1,
            };

            var match = ScoreService.EvaluateMatch(p1, p2);

            Assert.That(match.WinnerPlayerIndex, Is.EqualTo(1));
            Assert.That(match.IsTie, Is.False);
            Assert.That(match.Reason, Is.EqualTo(TieBreakReason.ScoreDifference));
        }

        [Test]
        public void EvaluateMatch_WinnerByWellbeing_WhenScoresAreIdentical()
        {
            // Equal scores, but P1 has higher wellbeing
            var p1 = new ScoreInput
            {
                PlayerIndex = 1,
                Finished = true,
                FinishTime = 150f,
                ArrivalPlacement = 2,
                FinalWellbeing = 80,
                HealthyPointsEarned = 1000,
            };

            var p2 = new ScoreInput
            {
                PlayerIndex = 2,
                Finished = true,
                FinishTime = 140f,
                ArrivalPlacement = 1,
                FinalWellbeing = 60,
                HealthyPointsEarned = 1000,
            };

            // Force equal score by adjusting wellbeing and healthy points
            // p1: 1000 + 600 + 100 + (80 * 5 = 400) = 2100
            // p2: 1000 + 600 + 200 + (60 * 5 = 300) = 2100
            var match = ScoreService.EvaluateMatch(p1, p2);

            Assert.That(match.Player1.FinalScore, Is.EqualTo(2100));
            Assert.That(match.Player2.FinalScore, Is.EqualTo(2100));
            Assert.That(match.WinnerPlayerIndex, Is.EqualTo(1));
            Assert.That(match.Reason, Is.EqualTo(TieBreakReason.WellbeingDifference));
        }

        [Test]
        public void EvaluateMatch_WinnerByFinishTime_WhenScoreAndWellbeingAreIdentical()
        {
            var p1 = new ScoreInput
            {
                PlayerIndex = 1,
                Finished = true,
                FinishTime = 140f,
                ArrivalPlacement = 1,
                FinalWellbeing = 80,
                HealthyPointsEarned = 1000,
            };

            var p2 = new ScoreInput
            {
                PlayerIndex = 2,
                Finished = true,
                FinishTime = 150f,
                ArrivalPlacement = 1, // Simulated tie on arrival bonus
                FinalWellbeing = 80,
                HealthyPointsEarned = 1000,
            };

            var match = ScoreService.EvaluateMatch(p1, p2);

            Assert.That(match.Player1.FinalScore, Is.EqualTo(match.Player2.FinalScore));
            Assert.That(match.WinnerPlayerIndex, Is.EqualTo(1));
            Assert.That(match.Reason, Is.EqualTo(TieBreakReason.FinishTimeDifference));
        }

        [Test]
        public void EvaluateMatch_WinnerByHealthyFoods_WhenScoreWellbeingAndTimeAreIdentical()
        {
            var p1 = new ScoreInput
            {
                PlayerIndex = 1,
                Finished = true,
                FinishTime = 150f,
                ArrivalPlacement = 1,
                FinalWellbeing = 80,
                HealthyPointsEarned = 1000,
                HealthyFoodsCount = 6,
            };

            var p2 = new ScoreInput
            {
                PlayerIndex = 2,
                Finished = true,
                FinishTime = 150f,
                ArrivalPlacement = 1,
                FinalWellbeing = 80,
                HealthyPointsEarned = 1000,
                HealthyFoodsCount = 4,
            };

            var match = ScoreService.EvaluateMatch(p1, p2);

            Assert.That(match.WinnerPlayerIndex, Is.EqualTo(1));
            Assert.That(match.Reason, Is.EqualTo(TieBreakReason.HealthyFoodsDifference));
        }

        [Test]
        public void EvaluateMatch_WinnerByFewerFatigues_WhenOtherFactorsAreIdentical()
        {
            var p1 = new ScoreInput
            {
                PlayerIndex = 1,
                Finished = true,
                FinishTime = 150f,
                ArrivalPlacement = 1,
                FinalWellbeing = 80,
                HealthyPointsEarned = 1200,
                HealthyFoodsCount = 5,
                FatigueEpisodesCount = 0,
            };

            var p2 = new ScoreInput
            {
                PlayerIndex = 2,
                Finished = true,
                FinishTime = 150f,
                ArrivalPlacement = 1,
                FinalWellbeing = 80,
                HealthyPointsEarned = 1400, // +200 healthy offsets -200 fatigue penalty
                HealthyFoodsCount = 5,
                FatigueEpisodesCount = 1, // -200 fatigue penalty
            };

            var match = ScoreService.EvaluateMatch(p1, p2);

            Assert.That(match.Player1.FinalScore, Is.EqualTo(match.Player2.FinalScore));
            Assert.That(match.WinnerPlayerIndex, Is.EqualTo(1));
            Assert.That(match.Reason, Is.EqualTo(TieBreakReason.FatigueEpisodesDifference));
        }

        [Test]
        public void EvaluateMatch_AbsoluteTie_WhenAllMetricsAreEqual()
        {
            var p1 = new ScoreInput
            {
                PlayerIndex = 1,
                Finished = true,
                FinishTime = 150f,
                ArrivalPlacement = 1,
                FinalWellbeing = 80,
                HealthyPointsEarned = 1000,
                HealthyFoodsCount = 5,
                FatigueEpisodesCount = 0,
            };

            var p2 = new ScoreInput
            {
                PlayerIndex = 2,
                Finished = true,
                FinishTime = 150f,
                ArrivalPlacement = 1,
                FinalWellbeing = 80,
                HealthyPointsEarned = 1000,
                HealthyFoodsCount = 5,
                FatigueEpisodesCount = 0,
            };

            var match = ScoreService.EvaluateMatch(p1, p2);

            Assert.That(match.Player1.FinalScore, Is.EqualTo(match.Player2.FinalScore));
            Assert.That(match.WinnerPlayerIndex, Is.EqualTo(0));
            Assert.That(match.IsTie, Is.True);
            Assert.That(match.Reason, Is.EqualTo(TieBreakReason.AbsoluteTie));
        }
    }
}
