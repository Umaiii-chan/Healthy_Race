using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HealthyRace.Input
{
    /// <summary>
    /// Exposes a single local player's input intent without taking ownership of
    /// movement, animation, or any gameplay state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private const string InteractActionName = "Interact";
        private const string JumpActionName = "Jump";
        private const string MoveActionName = "Move";

        [SerializeField]
        private InputActionAsset _inputActions;

        [SerializeField]
        private PlayerInputId _playerInputId;

        private InputAction _interactAction;
        private bool _isSubscribed;
        private InputAction _jumpAction;
        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputActionAsset _runtimeInputActions;

        public event Action InteractPerformed;

        public event Action JumpPerformed;

        public bool InteractPressed { get; private set; }

        public bool IsConfigured => _playerActionMap != null;

        public PlayerInputId PlayerInputId => _playerInputId;

        public bool JumpPressed { get; private set; }

        public Vector2 MoveInput { get; private set; }

        public void Configure(InputActionAsset inputActions, PlayerInputId playerInputId)
        {
            var wasEnabled = isActiveAndEnabled;
            var inputActionsChanged = _inputActions != inputActions;
            if (wasEnabled)
            {
                DisableActions();
            }

            _inputActions = inputActions;
            _playerInputId = playerInputId;

            if (Application.isPlaying && (inputActionsChanged || _runtimeInputActions == null))
            {
                RecreateRuntimeInputActions();
            }

            ResolveActions();

            if (wasEnabled)
            {
                EnableActions();
            }
        }

        public void Configure(PlayerInputId playerInputId)
        {
            Configure(_inputActions, playerInputId);
        }

        private void Awake()
        {
            RecreateRuntimeInputActions();
            ResolveActions();
        }

        private void LateUpdate()
        {
            JumpPressed = false;
            InteractPressed = false;
        }

        private void OnDestroy()
        {
            DestroyRuntimeInputActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        private void OnEnable()
        {
            ResolveActions();
            EnableActions();
        }

        private void DestroyRuntimeInputActions()
        {
            if (_runtimeInputActions == null)
            {
                return;
            }

            Destroy(_runtimeInputActions);
            _runtimeInputActions = null;
        }

        private void DisableActions()
        {
            if (_playerActionMap == null)
            {
                return;
            }

            Unsubscribe();
            _playerActionMap.Disable();
            MoveInput = Vector2.zero;
            JumpPressed = false;
            InteractPressed = false;
        }

        private void EnableActions()
        {
            if (_playerActionMap == null)
            {
                return;
            }

            Subscribe();
            _playerActionMap.Enable();
        }

        private void HandleInteract(InputAction.CallbackContext context)
        {
            InteractPressed = true;
            InteractPerformed?.Invoke();
        }

        private void HandleJump(InputAction.CallbackContext context)
        {
            JumpPressed = true;
            JumpPerformed?.Invoke();
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
        }

        private InputActionAsset GetActiveInputActions()
        {
            return _runtimeInputActions != null ? _runtimeInputActions : _inputActions;
        }

        private void RecreateRuntimeInputActions()
        {
            DestroyRuntimeInputActions();

            if (_inputActions != null)
            {
                _runtimeInputActions = Instantiate(_inputActions);
            }
        }

        private void ResolveActions()
        {
            var inputActions = GetActiveInputActions();
            if (inputActions == null)
            {
                Debug.LogError("Player Input Reader requires an Input Actions asset.", this);
                return;
            }

            _playerActionMap = inputActions.FindActionMap(_playerInputId.ToString(), false);
            _moveAction = _playerActionMap?.FindAction(MoveActionName, false);
            _jumpAction = _playerActionMap?.FindAction(JumpActionName, false);
            _interactAction = _playerActionMap?.FindAction(InteractActionName, false);

            if (
                _playerActionMap == null
                || _moveAction == null
                || _jumpAction == null
                || _interactAction == null
            )
            {
                Debug.LogError(
                    $"Input map '{_playerInputId}' must contain Move, Jump, and Interact actions.",
                    this
                );
            }
        }

        private void Subscribe()
        {
            if (
                _isSubscribed
                || _moveAction == null
                || _jumpAction == null
                || _interactAction == null
            )
            {
                return;
            }

            _moveAction.performed += HandleMove;
            _moveAction.canceled += HandleMove;
            _jumpAction.performed += HandleJump;
            _interactAction.performed += HandleInteract;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _moveAction.performed -= HandleMove;
            _moveAction.canceled -= HandleMove;
            _jumpAction.performed -= HandleJump;
            _interactAction.performed -= HandleInteract;
            _isSubscribed = false;
        }
    }
}
