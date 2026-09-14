using UnityEngine;

namespace HealthyRace.Player
{
    /// <summary>
    /// Keeps locomotion state transitions independent from input devices and physics APIs.
    /// </summary>
    public sealed class PlayerStateMachine
    {
        private const float HorizontalInputThreshold = 0.01f;
        private const float UpwardVelocityThreshold = 0.01f;

        private bool _isInitialized;
        private bool _wasGrounded;

        public PlayerState CurrentState { get; private set; }

        public void Initialize(bool isGrounded)
        {
            _isInitialized = true;
            _wasGrounded = isGrounded;
            CurrentState = isGrounded ? PlayerState.Idle : PlayerState.Fall;
        }

        public PlayerState UpdateState(
            bool isGrounded,
            float horizontalInput,
            float verticalVelocity
        )
        {
            if (!_isInitialized)
            {
                Initialize(isGrounded);
            }

            if (!_wasGrounded && isGrounded)
            {
                CurrentState = PlayerState.Land;
            }
            else if (!isGrounded)
            {
                CurrentState =
                    verticalVelocity > UpwardVelocityThreshold
                        ? PlayerState.Jump
                        : PlayerState.Fall;
            }
            else
            {
                CurrentState = ResolveGroundedState(horizontalInput);
            }

            _wasGrounded = isGrounded;
            return CurrentState;
        }

        private static PlayerState ResolveGroundedState(float horizontalInput)
        {
            return Mathf.Abs(horizontalInput) > HorizontalInputThreshold
                ? PlayerState.Move
                : PlayerState.Idle;
        }
    }
}
