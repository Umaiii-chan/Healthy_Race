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
        private float _terrainMultiplier = 1f;
        private SnackRush _snack;
        private float _airGravityScale = -1f;
        private bool _wasSupported;
        private Rigidbody2D _rigidbody;

        /// <summary>Distância máxima que o jogador é puxado para continuar colado ao piso.</summary>
        private const float GroundSnapDistance = 0.35f;

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

        /// <summary>Multiplicador do Bem-Estar (faixa atual).</summary>
        public float MovementMultiplier => _movementMultiplier;

        /// <summary>Multiplicador do terreno sob o jogador (lama), independente do Bem-Estar.</summary>
        public float TerrainMultiplier => _terrainMultiplier;

        /// <summary>Efeito do lanche rápido: pico e queda (ver <see cref="SnackRush"/>).</summary>
        public SnackRush Snack => _snack ??= CreateSnack();

        public SnackState SnackState => Snack.State;

        /// <summary>Multiplicador do lanche: maior que 1 no pico, menor na queda.</summary>
        public float SnackMultiplier => Snack.Multiplier;

        public bool IsRushing => Snack.State == SnackState.Rush;

        public bool IsCrashing => Snack.State == SnackState.Crash;

        /// <summary>Velocidade horizontal efetiva = base × Bem-Estar × terreno × lanche.</summary>
        public float EffectiveMoveSpeed =>
            _moveSpeed * _movementMultiplier * _terrainMultiplier * SnackMultiplier;

        public bool IsOnSlope => _groundCheck != null && _groundCheck.IsOnSlope;

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

            // As duas janelas só são consumidas no fim do passo, depois de o pulo
            // ter tido a sua chance; descontar antes encurtaria cada uma em um passo.
            var supported = isGrounded && !_risingFromJump;
            if (supported)
            {
                _coyoteTimer = _coyoteTime;
            }

            // Logo depois de sair do topo de uma rampa (ou de entrar numa descida) o
            // sensor perde o piso por um passo; sem isto o jogador "decolaria".
            if (!isGrounded && _wasSupported && !_risingFromJump)
            {
                var gap = _groundCheck.DistanceToGround(GroundSnapDistance);
                if (gap > 0f)
                {
                    _rigidbody.position += Vector2.down * gap;
                    velocity.y = 0f;
                    isGrounded = _groundCheck.Refresh();
                    supported = isGrounded;
                }
            }

            var onSlope = supported && _groundCheck.IsOnSlope;
            var jumped = false;

            if (_isMovementEnabled)
            {
                var targetSpeed = MoveInput * EffectiveMoveSpeed;
                var horizontal =
                    isGrounded && _groundAcceleration > 0f
                        ? Mathf.MoveTowards(
                            velocity.x,
                            targetSpeed,
                            _groundAcceleration * deltaTime
                        )
                        : targetSpeed;
                velocity.x = horizontal;

                var canJump = supported || _coyoteTimer > 0f;
                if (_jumpBufferTimer > 0f && canJump)
                {
                    velocity.y = _jumpVelocity * _jumpMultiplier;
                    _jumpBufferTimer = 0f;
                    _coyoteTimer = 0f;
                    _risingFromJump = true;
                    jumped = true;
                    Jumped?.Invoke();
                }
                else if (onSlope)
                {
                    // Na rampa a velocidade segue o piso: a mesma rapidez horizontal do
                    // plano, subindo ou descendo junto.
                    var normal = _groundCheck.GroundNormal;
                    var tangent = new Vector2(normal.y, -normal.x);
                    velocity = tangent * (horizontal / tangent.x);
                }
                else if (supported && velocity.y > 0f)
                {
                    // Chegou ao plano vindo de uma subida: não sai voando.
                    velocity.y = 0f;
                }
            }
            else
            {
                velocity.x = 0f;
                _jumpBufferTimer = 0f;
                if (onSlope)
                {
                    velocity = Vector2.zero;
                }
            }

            // Parado ou andando na rampa, sem gravidade: com o colisor sem atrito o
            // jogador escorregaria rampa abaixo.
            SetSlopeGravity(onSlope && !jumped);
            _wasSupported = supported && !jumped;

            if (!supported)
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - deltaTime);
            }

            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - deltaTime);
            Snack.Tick(deltaTime);
            _rigidbody.linearVelocity = velocity;
        }

        public void RequestJump()
        {
            // Mesmo com buffer zero o pedido sobrevive até o próximo passo de física.
            _jumpBufferTimer = Mathf.Max(_jumpBufferTime, 0.0001f);
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

        /// <summary>
        /// Efeito do terreno (lama). Separado do Bem-Estar: sair da lama não apaga o
        /// efeito da faixa de Bem-Estar, e mudar de faixa na lama não apaga a lama.
        /// </summary>
        /// <summary>Comeu lanche rápido: começa (ou renova) o pico.</summary>
        public void EatSnack(float wellbeing01 = 1f) => Snack.Eat(wellbeing01);

        /// <summary>Comeu fruta ou água: encerra a queda, se houver.</summary>
        public bool CureCrash() => Snack.Cure();

        public void ClearSnack() => Snack.Clear();

        private static SnackRush CreateSnack()
        {
            var balance = Core.BalanceConfig.Current;
            return new SnackRush(
                balance.RushMultiplier,
                balance.RushSeconds,
                balance.CrashMultiplier,
                balance.CrashSeconds,
                balance.CrashExtraPerChain,
                balance.CrashMaxSeconds,
                balance.LowWellbeingRushFactor,
                balance.LowWellbeingCrashFactor
            );
        }

        public void SetTerrainMultiplier(float multiplier)
        {
            _terrainMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
        }

        private void SetSlopeGravity(bool onSlope)
        {
            if (onSlope)
            {
                if (_airGravityScale < 0f)
                {
                    _airGravityScale = _rigidbody.gravityScale;
                }

                _rigidbody.gravityScale = 0f;
            }
            else if (_airGravityScale >= 0f)
            {
                _rigidbody.gravityScale = _airGravityScale;
                _airGravityScale = -1f;
            }
        }
    }
}
