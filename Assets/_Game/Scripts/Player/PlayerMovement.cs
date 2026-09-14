using UnityEngine;

namespace HealthyRace.Player
{
    /// <summary>
    /// Applies local locomotion intent to this player's Rigidbody2D.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerGroundCheck), typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _jumpVelocity = 12f;

        [SerializeField, Min(0f)]
        private float _moveSpeed = 7f;

        private PlayerGroundCheck _groundCheck;
        private bool _isMovementEnabled = true;
        private bool _jumpRequested;
        private float _jumpMultiplier = 1f;
        private float _movementMultiplier = 1f;
        private Rigidbody2D _rigidbody;

        public bool IsGrounded => _groundCheck != null && _groundCheck.IsGrounded;

        public bool IsMovementEnabled => _isMovementEnabled;

        public float JumpMultiplier => _jumpMultiplier;

        public float MovementMultiplier => _movementMultiplier;

        public float MoveInput { get; private set; }

        public Vector2 Velocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;

        private void Awake()
        {
            _groundCheck = GetComponent<PlayerGroundCheck>();
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (_groundCheck == null || _rigidbody == null)
            {
                return;
            }

            var isGrounded = _groundCheck.Refresh();
            var velocity = _rigidbody.linearVelocity;

            if (_isMovementEnabled)
            {
                velocity.x = MoveInput * _moveSpeed * _movementMultiplier;

                if (_jumpRequested && isGrounded)
                {
                    velocity.y = _jumpVelocity * _jumpMultiplier;
                }
            }
            else
            {
                velocity.x = 0f;
            }

            _rigidbody.linearVelocity = velocity;
            _jumpRequested = false;
        }

        public void RequestJump()
        {
            _jumpRequested = true;
        }

        public void SetHorizontalInput(float horizontalInput)
        {
            MoveInput = Mathf.Clamp(horizontalInput, -1f, 1f);
        }

        public void SetMovementEnabled(bool movementEnabled)
        {
            _isMovementEnabled = movementEnabled;

            if (!_isMovementEnabled)
            {
                _jumpRequested = false;
            }
        }

        public void SetMovementMultipliers(float movementMultiplier, float jumpMultiplier)
        {
            _movementMultiplier = Mathf.Max(0f, movementMultiplier);
            _jumpMultiplier = Mathf.Max(0f, jumpMultiplier);
        }
    }
}
