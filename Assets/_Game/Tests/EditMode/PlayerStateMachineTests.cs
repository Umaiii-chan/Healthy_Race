using HealthyRace.Player;
using NUnit.Framework;

namespace HealthyRace.Tests.EditMode
{
    public sealed class PlayerStateMachineTests
    {
        [Test]
        public void GroundedStates_TransitionBetweenIdleAndMove()
        {
            var stateMachine = new PlayerStateMachine();

            stateMachine.Initialize(true);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Idle));

            stateMachine.UpdateState(true, 1f, 0f);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Move));

            stateMachine.UpdateState(true, 0f, 0f);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Idle));
        }

        [Test]
        public void AirborneStates_TransitionFromJumpToFallAndLand()
        {
            var stateMachine = new PlayerStateMachine();
            stateMachine.Initialize(true);

            stateMachine.UpdateState(false, 1f, 3f);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Jump));

            stateMachine.UpdateState(false, 1f, -2f);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Fall));

            stateMachine.UpdateState(true, 1f, 0f);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Land));

            stateMachine.UpdateState(true, 1f, 0f);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Move));
        }

        [Test]
        public void InitialAirborneState_IsFall()
        {
            var stateMachine = new PlayerStateMachine();

            stateMachine.Initialize(false);

            Assert.That(stateMachine.CurrentState, Is.EqualTo(PlayerState.Fall));
        }
    }
}
