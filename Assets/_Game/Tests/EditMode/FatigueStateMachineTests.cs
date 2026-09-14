using HealthyRace.Input;
using HealthyRace.Player;
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
        public void CrossingThirtyToTen_TriggersOneFatigueEpisode()
        {
            var machine = CreateMachine();

            machine.ProcessWellbeingChange(30, 10, true);

            Assert.That(machine.State, Is.EqualTo(FatigueState.Fatigued));
            Assert.That(machine.EpisodeCount, Is.EqualTo(1));
            Assert.That(machine.Readiness, Is.EqualTo(FatigueReadiness.Disarmed));
        }

        [Test]
        public void WellbeingAtTwentyFive_DoesNotRearm()
        {
            var machine = CreateMachine();
            machine.ProcessWellbeingChange(21, 5, true);
            machine.BeginRecovery();
            machine.FinishRecovery(20);

            // Rises to 25 exactly: must NOT rearm because rule is strictly > 25
            machine.ProcessWellbeingChange(20, 25, true);

            Assert.That(machine.Readiness, Is.EqualTo(FatigueReadiness.Disarmed));

            // Drops again to 20: must NOT trigger because still disarmed
            machine.ProcessWellbeingChange(25, 20, true);

            Assert.That(machine.EpisodeCount, Is.EqualTo(1));
        }

        [Test]
        public void MultipleDropsBelowThreshold_WithoutRearm_DoNotTriggerEpisodes()
        {
            var machine = CreateMachine();
            machine.ProcessWellbeingChange(21, 20, true);
            machine.BeginRecovery();
            machine.FinishRecovery(20);

            // Progressive drops while remaining below rearm threshold
            machine.ProcessWellbeingChange(20, 18, true);
            machine.ProcessWellbeingChange(18, 15, true);
            machine.ProcessWellbeingChange(15, 10, true);
            machine.ProcessWellbeingChange(10, 0, true);

            Assert.That(machine.EpisodeCount, Is.EqualTo(1));
            Assert.That(machine.Readiness, Is.EqualTo(FatigueReadiness.Disarmed));
        }

        [Test]
        public void PlayerTwo_EnteringFatigue_DoesNotAffectPlayerOne()
        {
            var playerOneFatigue = CreateMachine();
            var playerTwoFatigue = CreateMachine();

            playerTwoFatigue.ProcessWellbeingChange(25, 18, true);

            Assert.That(playerTwoFatigue.State, Is.EqualTo(FatigueState.Fatigued));
            Assert.That(playerTwoFatigue.EpisodeCount, Is.EqualTo(1));
            Assert.That(playerOneFatigue.State, Is.EqualTo(FatigueState.Normal));
            Assert.That(playerOneFatigue.EpisodeCount, Is.EqualTo(0));
        }

        [Test]
        public void FatigueComponent_Integration_LocksMovementAndAppliesRecovery()
        {
            var go = new GameObject("Fatigue Component Integration Test");
            try
            {
                go.AddComponent<Rigidbody2D>();
                go.AddComponent<BoxCollider2D>();
                go.AddComponent<PlayerGroundCheck>();
                go.AddComponent<PlayerInputReader>();
                var movement = go.AddComponent<PlayerMovement>();
                var controller = go.AddComponent<PlayerController>();
                var wellbeing = go.AddComponent<WellbeingComponent>();
                var fatigue = go.AddComponent<FatigueComponent>();

                wellbeing.Initialize(100);
                controller.EnsureInitialized();
                fatigue.EnsureInitialized();

                Assert.That(controller.IsMovementEnabled, Is.True);
                Assert.That(fatigue.State, Is.EqualTo(FatigueState.Normal));

                // Trigger fatigue by dropping to 20 while in air
                wellbeing.SetValue(20);

                Assert.That(fatigue.State, Is.EqualTo(FatigueState.FatiguePending));
                Assert.That(fatigue.FatigueEpisodeCount, Is.EqualTo(1));

                // Touch ground: transitions to Fatigued and locks movement
                fatigue.ProcessPendingFatigue(true);

                Assert.That(fatigue.State, Is.EqualTo(FatigueState.Fatigued));
                Assert.That(controller.IsMovementEnabled, Is.False);

                // Complete fatigue recovery immediately
                fatigue.CompleteFatigueImmediately();

                Assert.That(fatigue.State, Is.EqualTo(FatigueState.Normal));
                Assert.That(wellbeing.CurrentValue, Is.EqualTo(35)); // 20 + 15
                Assert.That(controller.IsMovementEnabled, Is.True);
                Assert.That(fatigue.Readiness, Is.EqualTo(FatigueReadiness.Armed)); // 35 > 25
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
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
