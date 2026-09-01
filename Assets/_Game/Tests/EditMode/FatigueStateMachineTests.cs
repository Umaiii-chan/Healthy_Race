using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class FatigueStateMachineTests
    {
        [Test]
        public void CrossingTwenty_TriggersOneFatigueEpisode()
        {
            var machine = CreateMachine();

            machine.ProcessWellbeingChange(21, 20, true);

            Assert.That(machine.State, Is.EqualTo(FatigueState.Fatigued));
            Assert.That(machine.EpisodeCount, Is.EqualTo(1));
            Assert.That(machine.Readiness, Is.EqualTo(FatigueReadiness.Disarmed));
        }

        [Test]
        public void RemainingBelowThreshold_DoesNotTriggerAnotherEpisode()
        {
            var machine = CreateMachine();
            machine.ProcessWellbeingChange(21, 20, true);
            machine.BeginRecovery();
            machine.FinishRecovery(20);

            machine.ProcessWellbeingChange(20, 19, true);

            Assert.That(machine.State, Is.EqualTo(FatigueState.Normal));
            Assert.That(machine.EpisodeCount, Is.EqualTo(1));
        }

        [Test]
        public void CompletingFatigue_AddsRecoveryWellbeing()
        {
            var wellbeing = CreateWellbeing(20, out var gameObject);
            var machine = CreateMachine();

            try
            {
                machine.ProcessWellbeingChange(21, 20, true);
                machine.BeginRecovery();
                wellbeing.ApplyDelta(15);
                machine.FinishRecovery(wellbeing.CurrentValue);

                Assert.That(wellbeing.CurrentValue, Is.EqualTo(35));
                Assert.That(machine.State, Is.EqualTo(FatigueState.Normal));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RecoveryAtTwenty_RemainsDisarmedUntilWellbeingExceedsTwentyFive()
        {
            var machine = CreateMachine();
            machine.ProcessWellbeingChange(21, 5, true);
            machine.BeginRecovery();
            machine.FinishRecovery(20);

            Assert.That(machine.Readiness, Is.EqualTo(FatigueReadiness.Disarmed));

            machine.ProcessWellbeingChange(20, 10, true);

            Assert.That(machine.EpisodeCount, Is.EqualTo(1));
        }

        [Test]
        public void WellbeingAtTwentySix_RearmsAndAllowsANewFatigueEpisode()
        {
            var machine = CreateMachine();
            machine.ProcessWellbeingChange(21, 5, true);
            machine.BeginRecovery();
            machine.FinishRecovery(20);

            machine.ProcessWellbeingChange(20, 26, true);

            Assert.That(machine.Readiness, Is.EqualTo(FatigueReadiness.Armed));

            machine.ProcessWellbeingChange(26, 20, true);

            Assert.That(machine.State, Is.EqualTo(FatigueState.Fatigued));
            Assert.That(machine.EpisodeCount, Is.EqualTo(2));
        }

        [Test]
        public void CrossingThresholdInAir_WaitsForGroundBeforeStartingFatigue()
        {
            var machine = CreateMachine();

            machine.ProcessWellbeingChange(21, 20, false);

            Assert.That(machine.State, Is.EqualTo(FatigueState.FatiguePending));

            machine.TryBeginPendingFatigue(false);
            Assert.That(machine.State, Is.EqualTo(FatigueState.FatiguePending));

            machine.TryBeginPendingFatigue(true);
            Assert.That(machine.State, Is.EqualTo(FatigueState.Fatigued));
        }

        [Test]
        public void SeparateFatigueInstances_DoNotSharePlayerState()
        {
            var playerOneFatigue = CreateMachine();
            var playerTwoFatigue = CreateMachine();

            playerOneFatigue.ProcessWellbeingChange(21, 20, true);

            Assert.That(playerOneFatigue.State, Is.EqualTo(FatigueState.Fatigued));
            Assert.That(playerOneFatigue.EpisodeCount, Is.EqualTo(1));
            Assert.That(playerTwoFatigue.State, Is.EqualTo(FatigueState.Normal));
            Assert.That(playerTwoFatigue.EpisodeCount, Is.EqualTo(0));
        }

        private static FatigueStateMachine CreateMachine()
        {
            return new FatigueStateMachine(20, 25);
        }

        private static WellbeingComponent CreateWellbeing(int value, out GameObject gameObject)
        {
            gameObject = new GameObject("Fatigue Wellbeing Test");
            var wellbeing = gameObject.AddComponent<WellbeingComponent>();
            wellbeing.SetValue(value);
            return wellbeing;
        }
    }
}
