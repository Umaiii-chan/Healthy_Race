using System;
using UnityEngine;

namespace HealthyRace.Puzzles
{
    /// <summary>
    /// Moving kinematic platform that translates smoothly between resting and activated
    /// positions. Carries riding physics bodies without slippage.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class ActivatedPlatform : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField]
        private Vector2 _targetOffset = new(0f, 4f);

        [SerializeField, Min(0.1f)]
        private float _moveSpeed = 3f;

        [SerializeField]
        private bool _returnWhenDeactivated = true;

        private Vector2 _originPosition;
        private Vector2 _targetPosition;
        private bool _isActive;
        private Rigidbody2D _rigidbody;
        private BoxCollider2D _collider;

        public bool IsActive => _isActive;
        public Vector2 TargetOffset => _targetOffset;
        public float MoveSpeed => _moveSpeed;
        public Vector2 OriginPosition => _originPosition;
        public Vector2 TargetPosition => _targetPosition;

        public event Action<bool> OnStateChanged;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();

            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            _originPosition = transform.position;
            _targetPosition = _originPosition + _targetOffset;
        }

        private void FixedUpdate()
        {
            var destination = _isActive
                ? _targetPosition
                : (_returnWhenDeactivated ? _originPosition : (Vector2)transform.position);

            var current = (Vector2)transform.position;
            if (Vector2.Distance(current, destination) > 0.001f)
            {
                var next = Vector2.MoveTowards(
                    current,
                    destination,
                    _moveSpeed * Time.fixedDeltaTime
                );
                _rigidbody.MovePosition(next);
            }
        }

        public void Configure(
            Vector2 targetOffset,
            float moveSpeed,
            bool returnWhenDeactivated = true
        )
        {
            _targetOffset = targetOffset;
            _moveSpeed = moveSpeed;
            _returnWhenDeactivated = returnWhenDeactivated;
            _originPosition = transform.position;
            _targetPosition = _originPosition + _targetOffset;
        }

        public void Activate()
        {
            SetState(true);
        }

        public void Deactivate()
        {
            SetState(false);
        }

        public void Toggle()
        {
            SetState(!_isActive);
        }

        public void SetState(bool active)
        {
            if (_isActive == active)
            {
                return;
            }

            _isActive = active;
            OnStateChanged?.Invoke(_isActive);
        }

        public void ResetPlatform()
        {
            _isActive = false;
            transform.position = _originPosition;
            if (_rigidbody != null)
            {
                _rigidbody.position = _originPosition;
                _rigidbody.linearVelocity = Vector2.zero;
            }
        }
    }
}
