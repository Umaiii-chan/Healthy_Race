using UnityEngine;
using UnityEngine.InputSystem;

namespace HealthyRace.Input
{
    /// <summary>
    /// Test-scene-only feedback for the UI/Pause action. It does not pause gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseInputDebug : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset _inputActions;

        private InputAction _pauseAction;
        private InputActionMap _uiActionMap;

        public int PausePressCount { get; private set; }

        public void Configure(InputActionAsset inputActions)
        {
            if (isActiveAndEnabled)
            {
                OnDisable();
            }

            _inputActions = inputActions;
            ResolveActions();

            if (isActiveAndEnabled)
            {
                OnEnable();
            }
        }

        private void Awake()
        {
            ResolveActions();
        }

        private void ResolveActions()
        {
            _uiActionMap = _inputActions?.FindActionMap("UI", false);
            _pauseAction = _uiActionMap?.FindAction("Pause", false);

            if (_pauseAction == null)
            {
                Debug.LogError("Pause Input Debug requires the UI/Pause action.", this);
            }
        }

        private void OnDisable()
        {
            if (_pauseAction != null)
            {
                _pauseAction.performed -= HandlePause;
            }

            _uiActionMap?.Disable();
        }

        private void OnEnable()
        {
            if (_pauseAction == null)
            {
                return;
            }

            _pauseAction.performed += HandlePause;
            _uiActionMap.Enable();
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(Screen.width / 2f - 140f, 16f, 280f, 52f), "UI input");
            GUI.Label(
                new Rect(Screen.width / 2f - 128f, 40f, 256f, 22f),
                $"Pause (Esc) pressed: {PausePressCount}"
            );
        }

        private void HandlePause(InputAction.CallbackContext context)
        {
            PausePressCount++;
        }
    }
}
