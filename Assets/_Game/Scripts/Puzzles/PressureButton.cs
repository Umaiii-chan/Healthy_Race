using System;
using System.Collections.Generic;
using HealthyRace.Obstacles;
using HealthyRace.Player;
using UnityEngine;

namespace HealthyRace.Puzzles
{
    /// <summary>
    /// Interactive pressure switch activated by physical contact with players or pushable crates.
    /// Emits events upon state transitions and provides optional visual feedback.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class PressureButton : MonoBehaviour
    {
        [Header("Activation Rules")]
        [SerializeField]
        private bool _allowPlayerActivation = true;

        [SerializeField]
        private bool _allowBoxActivation = true;

        [Header("Visual Feedback (Optional)")]
        [SerializeField]
        private Transform _buttonCap;

        [SerializeField]
        private float _pressedYOffset = -0.08f;

        [SerializeField]
        private SpriteRenderer _visualRenderer;

        [SerializeField]
        private Color _unpressedColor = new Color(0.9f, 0.25f, 0.25f);

        [SerializeField]
        private Color _pressedColor = new Color(0.25f, 0.85f, 0.35f);

        private readonly HashSet<Collider2D> _activeColliders = new();
        private Vector3 _originalCapLocalPosition;
        private bool _hasOriginalCapPosition;

        public bool IsPressed { get; private set; }
        public int ActivatorCount => _activeColliders.Count;
        public bool AllowPlayerActivation => _allowPlayerActivation;
        public bool AllowBoxActivation => _allowBoxActivation;

        public event Action<bool> OnPressedStateChanged;
        public event Action OnPressed;
        public event Action OnReleased;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (_buttonCap != null)
            {
                _originalCapLocalPosition = _buttonCap.localPosition;
                _hasOriginalCapPosition = true;
            }

            UpdateVisuals();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsValidActivator(other))
            {
                _activeColliders.Add(other);
                EvaluateState();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_activeColliders.Remove(other))
            {
                EvaluateState();
            }
        }

        public bool IsValidActivator(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (_allowBoxActivation)
            {
                if (other.GetComponentInParent<PushableBox>() != null)
                {
                    return true;
                }
            }

            if (_allowPlayerActivation)
            {
                if (
                    other.GetComponentInParent<PlayerController>() != null
                    || other.GetComponentInParent<PlayerMovement>() != null
                )
                {
                    return true;
                }
            }

            return false;
        }

        public void ForceSetPressed(bool pressed)
        {
            if (IsPressed == pressed)
            {
                return;
            }

            IsPressed = pressed;
            UpdateVisuals();
            OnPressedStateChanged?.Invoke(IsPressed);

            if (IsPressed)
            {
                OnPressed?.Invoke();
            }
            else
            {
                OnReleased?.Invoke();
            }
        }

        private void EvaluateState()
        {
            // Clean up any destroyed colliders
            _activeColliders.RemoveWhere(c => c == null);

            var shouldBePressed = _activeColliders.Count > 0;
            if (shouldBePressed != IsPressed)
            {
                ForceSetPressed(shouldBePressed);
            }
        }

        /// <summary>Define o renderer que recebe a cor de pressionado e solto.</summary>
        public void SetVisualRenderer(SpriteRenderer renderer)
        {
            _visualRenderer = renderer;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_visualRenderer != null)
            {
                _visualRenderer.color = IsPressed ? _pressedColor : _unpressedColor;
            }

            if (_buttonCap != null && _hasOriginalCapPosition)
            {
                var targetY = IsPressed
                    ? _originalCapLocalPosition.y + _pressedYOffset
                    : _originalCapLocalPosition.y;

                _buttonCap.localPosition = new Vector3(
                    _originalCapLocalPosition.x,
                    targetY,
                    _originalCapLocalPosition.z
                );
            }
        }
    }
}
