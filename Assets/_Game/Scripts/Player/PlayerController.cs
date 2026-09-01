using HealthyRace.Input;
using HealthyRace.WellBeing;
using UnityEngine;

namespace HealthyRace.Player
{
    /// <summary>
    /// Owns only local player movement and input. Match state and wellbeing remain in
    /// their dedicated components.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(PlayerInputReader))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const int GroundLayer = 6;

        [SerializeField, Min(1)]
        private int _playerIndex = 1;

        [SerializeField, Min(0f)]
        private float _moveSpeed = 7f;

        [SerializeField, Min(0f)]
        private float _jumpVelocity = 12f;

        [SerializeField, Min(0f)]
        private float _groundCheckDistance = 0.08f;

        [SerializeField]
        private LayerMask _groundMask = 1 << GroundLayer;

        private Collider2D _collider;
        private bool _movementEnabled = true;
        private Rigidbody2D _rigidbody;
        private PlayerInputReader _playerInputReader;
        private bool _jumpRequested;
        private WellbeingComponent _wellbeing;

        public int PlayerIndex => _playerIndex;

        public bool IsGrounded { get; private set; }

        public bool IsMovementEnabled => _movementEnabled;

        public void Configure(int playerIndex)
        {
            _playerIndex = Mathf.Max(1, playerIndex);

            _playerInputReader.Configure(
                _playerIndex == 1 ? PlayerInputId.Player1 : PlayerInputId.Player2
            );
        }

        public void SetMovementEnabled(bool movementEnabled)
        {
            _movementEnabled = movementEnabled;

            if (!_movementEnabled)
            {
                _jumpRequested = false;
            }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _playerInputReader = GetComponent<PlayerInputReader>();
            _wellbeing = GetComponent<WellbeingComponent>();
        }

        private void Update()
        {
            if (_playerInputReader != null && _playerInputReader.JumpPressed)
            {
                _jumpRequested = true;
            }
        }

        private void FixedUpdate()
        {
            IsGrounded = CheckGrounded();

            if (!_movementEnabled)
            {
                _rigidbody.linearVelocity = new Vector2(0f, _rigidbody.linearVelocity.y);
                return;
            }

            var movementMultiplier = _wellbeing?.MovementMultiplier ?? 1f;
            var jumpMultiplier = _wellbeing?.JumpMultiplier ?? 1f;
            var movement = _playerInputReader?.MoveInput.x ?? 0f;
            _rigidbody.linearVelocity = new Vector2(
                movement * _moveSpeed * movementMultiplier,
                _rigidbody.linearVelocity.y
            );

            if (_jumpRequested && IsGrounded)
            {
                _rigidbody.linearVelocity = new Vector2(
                    _rigidbody.linearVelocity.x,
                    _jumpVelocity * jumpMultiplier
                );
            }

            _jumpRequested = false;
        }

        private bool CheckGrounded()
        {
            var bounds = _collider.bounds;
            var origin = new Vector2(bounds.center.x, bounds.min.y + 0.02f);
            var size = new Vector2(bounds.size.x * 0.82f, 0.04f);
            return Physics2D.BoxCast(
                origin,
                size,
                0f,
                Vector2.down,
                _groundCheckDistance,
                _groundMask
            );
        }
    }
}
