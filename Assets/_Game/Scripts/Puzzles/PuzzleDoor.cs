using System;
using UnityEngine;

namespace HealthyRace.Puzzles
{
    /// <summary>
    /// Physical puzzle door barrier that blocks passage when closed and opens
    /// smoothly or instantaneously when triggered by interactive elements.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class PuzzleDoor : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField]
        private Vector2 _openOffset = new(0f, 3f);

        [SerializeField, Min(0.1f)]
        private float _moveSpeed = 4f;

        [SerializeField]
        private bool _instantMotion;

        [Header("Visual Elements")]
        [SerializeField]
        private SpriteRenderer _visualRenderer;

        [SerializeField]
        private Color _closedColor = new(0.2f, 0.45f, 0.6f);

        [SerializeField]
        private Color _openColor = new(0.3f, 0.8f, 0.5f, 0.4f);

        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private bool _isOpen;
        private BoxCollider2D _collider;

        public bool IsOpen => _isOpen;
        public Vector2 OpenOffset => _openOffset;
        public float MoveSpeed => _moveSpeed;
        public Vector3 ClosedPosition => _closedPosition;
        public Vector3 OpenPosition => _openPosition;

        public event Action<bool> OnStateChanged;
        public event Action OnOpened;
        public event Action OnClosed;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _closedPosition = transform.position;
            _openPosition = _closedPosition + (Vector3)_openOffset;

            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponent<SpriteRenderer>();
            }

            UpdateVisualState();
        }

        private void Update()
        {
            if (_instantMotion)
            {
                return;
            }

            var target = _isOpen ? _openPosition : _closedPosition;
            if (Vector3.Distance(transform.position, target) > 0.001f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    _moveSpeed * Time.deltaTime
                );
            }
        }

        public void Configure(Vector2 openOffset, float moveSpeed, bool instantMotion = false)
        {
            _openOffset = openOffset;
            _moveSpeed = moveSpeed;
            _instantMotion = instantMotion;
            _closedPosition = transform.position;
            _openPosition = _closedPosition + (Vector3)_openOffset;
        }

        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        public void Toggle()
        {
            SetOpen(!_isOpen);
        }

        public void SetOpen(bool open)
        {
            if (_isOpen == open)
            {
                return;
            }

            _isOpen = open;

            if (_collider != null)
            {
                // Disable blocking collider when fully open if instant motion, or keep collider on sliding geometry
                if (_instantMotion)
                {
                    _collider.enabled = !_isOpen;
                }
            }

            if (_instantMotion)
            {
                transform.position = _isOpen ? _openPosition : _closedPosition;
            }

            UpdateVisualState();

            OnStateChanged?.Invoke(_isOpen);
            if (_isOpen)
            {
                OnOpened?.Invoke();
            }
            else
            {
                OnClosed?.Invoke();
            }
        }

        public void ResetDoor()
        {
            _isOpen = false;
            transform.position = _closedPosition;
            if (_collider != null)
            {
                _collider.enabled = true;
            }
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_visualRenderer != null)
            {
                _visualRenderer.color = _isOpen ? _openColor : _closedColor;
            }
        }
    }
}
