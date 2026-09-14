using HealthyRace.Input;
using HealthyRace.WellBeing;
using UnityEngine;

namespace HealthyRace.Player
{
    /// <summary>
    /// Orchestrates local input, movement, and locomotion state without owning the
    /// low-level physics or input bindings themselves.
    /// </summary>
    [RequireComponent(typeof(PlayerGroundCheck), typeof(PlayerInputReader), typeof(PlayerMovement))]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int _playerIndex = 1;

        private bool _movementEnabled = true;
        private PlayerInputReader _playerInputReader;
        private PlayerMovement _playerMovement;
        private PlayerStateMachine _stateMachine;
        private WellbeingComponent _wellbeing;

        public PlayerState CurrentState => _stateMachine?.CurrentState ?? PlayerState.Idle;

        public int PlayerIndex => _playerIndex;

        public bool IsGrounded => _playerMovement != null && _playerMovement.IsGrounded;

        public bool IsMovementEnabled => _movementEnabled;

        public void Configure(int playerIndex)
        {
            _playerIndex = Mathf.Max(1, playerIndex);

            _playerInputReader ??= GetComponent<PlayerInputReader>();

            _playerInputReader?.Configure(
                _playerIndex == 1 ? PlayerInputId.Player1 : PlayerInputId.Player2
            );
        }

        public void SetMovementEnabled(bool movementEnabled)
        {
            _movementEnabled = movementEnabled;
            _playerMovement?.SetMovementEnabled(_movementEnabled);
        }

        private void Awake()
        {
            _playerInputReader = GetComponent<PlayerInputReader>();
            _playerMovement = GetComponent<PlayerMovement>();
            _wellbeing = GetComponent<WellbeingComponent>();
            _stateMachine = new PlayerStateMachine();
            _playerMovement.SetMovementEnabled(_movementEnabled);
        }

        private void Update()
        {
            if (_playerInputReader == null || _playerMovement == null)
            {
                return;
            }

            _playerMovement.SetHorizontalInput(_playerInputReader.MoveInput.x);

            if (_playerInputReader.JumpPressed)
            {
                _playerMovement.RequestJump();
            }
        }

        private void FixedUpdate()
        {
            if (_playerMovement == null)
            {
                return;
            }

            _playerMovement.SetMovementEnabled(_movementEnabled);
            _playerMovement.SetMovementMultipliers(
                _wellbeing?.MovementMultiplier ?? 1f,
                _wellbeing?.JumpMultiplier ?? 1f
            );
        }

        private void LateUpdate()
        {
            if (_playerMovement == null || _stateMachine == null)
            {
                return;
            }

            _stateMachine.UpdateState(
                _playerMovement.IsGrounded,
                _playerMovement.MoveInput,
                _playerMovement.Velocity.y
            );
        }
    }
}
