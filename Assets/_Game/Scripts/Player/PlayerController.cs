using HealthyRace.WellBeing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HealthyRace.Player
{
    /// <summary>
    /// Owns only local player movement and input. Match state and wellbeing remain in
    /// their dedicated components.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
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
        private InputAction _jumpAction;
        private InputAction _moveAction;
        private bool _jumpRequested;
        private WellbeingComponent _wellbeing;

        public int PlayerIndex => _playerIndex;

        public bool IsGrounded { get; private set; }

        public bool IsMovementEnabled => _movementEnabled;

        public void Configure(int playerIndex)
        {
            _playerIndex = Mathf.Max(1, playerIndex);

            if (isActiveAndEnabled)
            {
                InitializeInput();
            }
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
            _wellbeing = GetComponent<WellbeingComponent>();
        }

        private void OnEnable()
        {
            InitializeInput();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
        }

        private void OnDestroy()
        {
            DisposeInput();
        }

        private void Update()
        {
            if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
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
            var movement = _moveAction?.ReadValue<float>() ?? 0f;
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

        private void InitializeInput()
        {
            DisposeInput();

            _moveAction = new InputAction("Move", InputActionType.Value);
            _jumpAction = new InputAction("Jump", InputActionType.Button);

            if (_playerIndex == 1)
            {
                AddHorizontalBindings(_moveAction, "a", "d");
                _jumpAction.AddBinding("<Keyboard>/w");
                _jumpAction.AddBinding("<Keyboard>/space");
            }
            else
            {
                AddHorizontalBindings(_moveAction, "leftArrow", "rightArrow");
                _jumpAction.AddBinding("<Keyboard>/upArrow");
            }

            _moveAction.Enable();
            _jumpAction.Enable();
        }

        private void DisposeInput()
        {
            _moveAction?.Dispose();
            _jumpAction?.Dispose();
            _moveAction = null;
            _jumpAction = null;
        }

        private static void AddHorizontalBindings(
            InputAction action,
            string negativeKey,
            string positiveKey
        )
        {
            action
                .AddCompositeBinding("1DAxis")
                .With("Negative", $"<Keyboard>/{negativeKey}")
                .With("Positive", $"<Keyboard>/{positiveKey}");
        }
    }
}
