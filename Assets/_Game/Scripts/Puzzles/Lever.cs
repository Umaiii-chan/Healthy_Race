using System;
using HealthyRace.Player;
using UnityEngine;

namespace HealthyRace.Puzzles
{
    /// <summary>
    /// Interactive physical lever toggled by player contact or external trigger.
    /// Provides visual angle/color feedback, debouncing, and decoupled state events.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class Lever : MonoBehaviour
    {
        [Header("Behavior")]
        [SerializeField]
        private bool _isToggleable = true;

        [SerializeField]
        private float _toggleCooldown = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField]
        private Transform _handleTransform;

        [SerializeField]
        private SpriteRenderer _visualRenderer;

        [SerializeField]
        private float _inactiveAngle = -35f;

        [SerializeField]
        private float _activeAngle = 35f;

        [SerializeField]
        private Color _inactiveColor = new(0.9f, 0.3f, 0.3f);

        [SerializeField]
        private Color _activeColor = new(0.25f, 0.85f, 0.35f);

        private bool _isActivated;
        private float _lastToggleTime = -10f;
        private BoxCollider2D _collider;

        public bool IsActivated => _isActivated;
        public bool IsToggleable => _isToggleable;
        public float ToggleCooldown => _toggleCooldown;

        public event Action<bool> OnToggled;
        public event Action OnActivated;
        public event Action OnDeactivated;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            UpdateVisuals();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (
                other.GetComponentInParent<PlayerController>() == null
                && other.GetComponentInParent<PlayerMovement>() == null
            )
            {
                return;
            }

            if (Time.time < _lastToggleTime + _toggleCooldown)
            {
                return;
            }

            if (!_isToggleable && _isActivated)
            {
                return;
            }

            Toggle();
        }

        public void Toggle()
        {
            SetState(!_isActivated);
        }

        public void SetState(bool activated)
        {
            if (_isActivated == activated)
            {
                return;
            }

            _isActivated = activated;
            _lastToggleTime = Time.time;
            UpdateVisuals();

            OnToggled?.Invoke(_isActivated);
            if (_isActivated)
            {
                OnActivated?.Invoke();
            }
            else
            {
                OnDeactivated?.Invoke();
            }
        }

        public void ResetState()
        {
            _isActivated = false;
            _lastToggleTime = -10f;
            UpdateVisuals();
            OnToggled?.Invoke(false);
            OnDeactivated?.Invoke();
        }

        public void Configure(
            Transform handleTransform,
            SpriteRenderer visualRenderer,
            bool isToggleable = true
        )
        {
            _handleTransform = handleTransform;
            _visualRenderer = visualRenderer;
            _isToggleable = isToggleable;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_handleTransform != null)
            {
                var targetAngle = _isActivated ? _activeAngle : _inactiveAngle;
                _handleTransform.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
            }

            if (_visualRenderer != null)
            {
                _visualRenderer.color = _isActivated ? _activeColor : _inactiveColor;
            }
        }
    }
}
