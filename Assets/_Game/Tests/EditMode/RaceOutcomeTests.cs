using HealthyRace.Scoring;
using NUnit.Framework;

namespace HealthyRace.Tests.EditMode
{
    /// <summary>Decisão D1: vence quem chega primeiro; estrelas por jogador.</summary>
    public sealed class RaceOutcomeTests
    {
        private static ScoreInput Player(
            int index,
            float time,
            bool finished = true,
            int wellbeing = 100,
            int fatigue = 0,
            int healthyPoints = 0
        ) =>
            new()
            {
                PlayerIndex = index,
                FinishTime = time,
                Finished = finished,
                FinalWellbeing = wellbeing,
                FatigueEpisodesCount = fatigue,
                HealthyPointsEarned = healthyPoints,
            };

        [Test]
        public void FirstToArrive_Wins_EvenWithFewerPoints()
        {
            var fast = Player(2, 118f, healthyPoints: 300);
            var collector = Player(1, 124f, healthyPoints: 3000);

            var race = RaceOutcome.Decide(collector, fast);
            Assert.That(race.Winner, Is.EqualTo(2));
            Assert.That(race.Verdict, Is.EqualTo(RaceVerdict.Arrival));
            Assert.That(race.MarginSeconds, Is.EqualTo(6f).Within(0.001f));

            // pelos pontos, o resultado seria o contrário: é isso que a D1 muda
            var config = ScoreConfig.CreateForRace(130f);
            collector.ArrivalPlacement = 2;
            fast.ArrivalPlacement = 1;
            var score = ScoreService.EvaluateMatch(collector, fast, config);
            Assert.That(score.WinnerPlayerIndex, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void OnlyFinisher_Wins_AndNobodyFinished_IsATie()
        {
            var race = RaceOutcome.Decide(Player(1, 0f, finished: false), Player(2, 200f));
            Assert.That(race.Winner, Is.EqualTo(2));
            Assert.That(race.Verdict, Is.EqualTo(RaceVerdict.OnlyFinisher));

            race = RaceOutcome.Decide(
                Player(1, 0f, finished: false),
                Player(2, 0f, finished: false)
            );
            Assert.That(race.Winner, Is.Zero);
            Assert.That(race.Verdict, Is.EqualTo(RaceVerdict.NobodyFinished));
        }

        [Test]
        public void SameInstant_IsDecidedByWellbeing_ThenFatigue_ThenTie()
        {
            var race = RaceOutcome.Decide(
                Player(1, 120f, wellbeing: 70),
                Player(2, 120.005f, wellbeing: 90)
            );
            Assert.That(race.Winner, Is.EqualTo(2));
            Assert.That(race.Verdict, Is.EqualTo(RaceVerdict.WellbeingTieBreak));

            race = RaceOutcome.Decide(Player(1, 120f, fatigue: 0), Player(2, 120f, fatigue: 1));
            Assert.That(race.Winner, Is.EqualTo(1));
            Assert.That(race.Verdict, Is.EqualTo(RaceVerdict.FatigueTieBreak));

            race = RaceOutcome.Decide(Player(1, 120f), Player(2, 120f));
            Assert.That(race.Winner, Is.Zero);
            Assert.That(race.Verdict, Is.EqualTo(RaceVerdict.Tie));
        }

        [Test]
        public void Stars_AreIndependentOfTheRival()
        {
            Assert.That(RaceOutcome.Stars(Player(1, 0f, finished: false), 130f, 60), Is.Zero);
            Assert.That(RaceOutcome.Stars(Player(1, 150f, wellbeing: 40), 130f, 60), Is.EqualTo(1));
            Assert.That(RaceOutcome.Stars(Player(1, 150f, wellbeing: 60), 130f, 60), Is.EqualTo(2));
            Assert.That(RaceOutcome.Stars(Player(1, 130f, wellbeing: 40), 130f, 60), Is.EqualTo(2));
            Assert.That(RaceOutcome.Stars(Player(1, 120f, wellbeing: 80), 130f, 60), Is.EqualTo(3));
        }

        [Test]
        public void PickupPopup_ComposesNameValueAndEffect()
        {
            Assert.That(
                HealthyRace.Visuals.PickupPopup.Compose("Maçã", 10, string.Empty),
                Is.EqualTo("Maçã +10")
            );
            Assert.That(
                HealthyRace.Visuals.PickupPopup.Compose("Hambúrguer", -18, "PICO!"),
                Is.EqualTo("Hambúrguer −18\nPICO!")
            );
        }

        [Test]
        public void Describe_SaysWhyInPortuguese()
        {
            Assert.That(
                RaceOutcome.Describe(new RaceResult(1, RaceVerdict.Arrival, 3.24f)),
                Is.EqualTo("P1 chegou 3,2 s antes.")
            );
            Assert.That(
                RaceOutcome.Describe(new RaceResult(2, RaceVerdict.OnlyFinisher, 0f)),
                Is.EqualTo("Só P2 chegou à porta.")
            );
            Assert.That(
                RaceOutcome.Describe(new RaceResult(0, RaceVerdict.NobodyFinished, 0f)),
                Is.EqualTo("Ninguém chegou à porta.")
            );
        }
    }
}
