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

        [Tooltip("Aceleração horizontal no chão (unidades/s²). No ar a resposta é imediata.")]
        [SerializeField, Min(0f)]
        private float _groundAcceleration = 90f;

        [Tooltip("Tempo após sair de uma borda em que o pulo ainda é aceito.")]
        [SerializeField, Min(0f)]
        private float _coyoteTime = 0.1f;

        [Tooltip("Tempo em que um pulo apertado antes de tocar o chão fica guardado.")]
        [SerializeField, Min(0f)]
        private float _jumpBufferTime = 0.12f;

        private PlayerGroundCheck _groundCheck;
        private bool _isMovementEnabled = true;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _risingFromJump;
        private float _jumpMultiplier = 1f;
        private float _movementMultiplier = 1f;
        private Rigidbody2D _rigidbody;

        public bool IsGrounded => _groundCheck != null && _groundCheck.IsGrounded;

        /// <summary>Velocidade vertical base do pulo, sem multiplicadores.</summary>
        public float JumpVelocity => _jumpVelocity;

        /// <summary>Velocidade horizontal base, sem multiplicadores.</summary>
        public float MoveSpeed => _moveSpeed;

        public bool IsMovementEnabled => _isMovementEnabled;

        public float CoyoteTime => _coyoteTime;

        public float JumpBufferTime => _jumpBufferTime;

        public float GroundAcceleration => _groundAcceleration;

        public float JumpMultiplier => _jumpMultiplier;

        public float MovementMultiplier => _movementMultiplier;

        public float MoveInput { get; private set; }

        public Vector2 Velocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector2.zero;

        public event System.Action Jumped;

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
            var deltaTime = Time.fixedDeltaTime;

            // Logo depois de pular o sensor de chão ainda enxerga o piso por um ou
            // dois passos; sem esta trava a tolerância de borda viraria pulo duplo.
            if (_risingFromJump && (velocity.y <= 0.01f || !isGrounded))
            {
                _risingFromJump = false;
            }

            _coyoteTimer =
                isGrounded && !_risingFromJump
                    ? _coyoteTime
                    : Mathf.Max(0f, _coyoteTimer - deltaTime);
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - deltaTime);

            if (_isMovementEnabled)
            {
                var targetSpeed = MoveInput * _moveSpeed * _movementMultiplier;
                velocity.x =
                    isGrounded && _groundAcceleration > 0f
                        ? Mathf.MoveTowards(
                            velocity.x,
                            targetSpeed,
                            _groundAcceleration * deltaTime
                        )
                        : targetSpeed;

                var canJump = (isGrounded && !_risingFromJump) || _coyoteTimer > 0f;
                if (_jumpBufferTimer > 0f && canJump)
                {
                    velocity.y = _jumpVelocity * _jumpMultiplier;
                    _jumpBufferTimer = 0f;
                    _coyoteTimer = 0f;
                    _risingFromJump = true;
                    Jumped?.Invoke();
                }
            }
            else
            {
                velocity.x = 0f;
                _jumpBufferTimer = 0f;
            }

            _rigidbody.linearVelocity = velocity;
        }

        public void RequestJump()
        {
            // Guarda o pedido por pelo menos um passo de física, mesmo com buffer zero.
            _jumpBufferTimer = Mathf.Max(_jumpBufferTime, Time.fixedDeltaTime * 1.5f);
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
                _jumpBufferTimer = 0f;
            }
        }

        public void SetMovementMultipliers(float movementMultiplier, float jumpMultiplier)
        {
            _movementMultiplier = Mathf.Max(0f, movementMultiplier);
            _jumpMultiplier = Mathf.Max(0f, jumpMultiplier);
        }
    }
}
