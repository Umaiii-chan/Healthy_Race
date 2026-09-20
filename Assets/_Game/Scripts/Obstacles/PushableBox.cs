using System;
using UnityEngine;

namespace HealthyRace.Obstacles
{
    /// <summary>
    /// Represents a dynamic, pushable puzzle crate with continuous collision detection,
    /// frozen rotation, spawn caching, and anti-deadlock recovery methods.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class PushableBox : MonoBehaviour
    {
        [Header("Physics Configuration")]
        [SerializeField, Min(0.1f)]
        private float _mass = 1.5f;

        [SerializeField, Min(0f)]
        private float _linearDamping = 1.5f;

        [SerializeField, Min(0.1f)]
        private float _gravityScale = 2.4f;

        [Header("Anti-Deadlock & Recovery")]
        [SerializeField]
        private bool _autoResetOnFall = true;

        [SerializeField]
        private float _fallResetYThreshold = -10f;

        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private Vector2 _initialSpawnPosition;
        private bool _hasSpawnPosition;

        public Rigidbody2D RigidbodyComponent
        {
            get
            {
                if (_rigidbody == null)
                {
                    _rigidbody = GetComponent<Rigidbody2D>();
                }
                return _rigidbody;
            }
        }

        public Collider2D ColliderComponent
        {
            get
            {
                if (_collider == null)
                {
                    _collider = GetComponent<Collider2D>();
                }
                return _collider;
            }
        }

        public float Mass => _mass;
        public float LinearDamping => _linearDamping;
        public float GravityScale => _gravityScale;
        public Vector2 InitialSpawnPosition => _initialSpawnPosition;
        public bool AutoResetOnFall => _autoResetOnFall;
        public float FallResetYThreshold => _fallResetYThreshold;

        public event Action OnResetToSpawn;

        private void Awake()
        {
            if (!_hasSpawnPosition)
            {
                _initialSpawnPosition = transform.position;
                _hasSpawnPosition = true;
            }

            InitializePhysics();
        }

        private void Reset()
        {
            InitializePhysics();
        }

        private void FixedUpdate()
        {
            if (_autoResetOnFall && transform.position.y < _fallResetYThreshold)
            {
                ResetToSpawn();
            }
        }

        public void InitializePhysics()
        {
            var rb = RigidbodyComponent;
            if (rb == null)
            {
                return;
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = _mass;
            rb.linearDamping = _linearDamping;
            rb.gravityScale = _gravityScale;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void SetSpawnPosition(Vector2 spawnPosition)
        {
            _initialSpawnPosition = spawnPosition;
            _hasSpawnPosition = true;
        }

        public void ResetToSpawn()
        {
            Respawn(_initialSpawnPosition);
            OnResetToSpawn?.Invoke();
        }

        public void Respawn(Vector2 position)
        {
            transform.position = position;

            var rb = RigidbodyComponent;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.position = position;
            }
        }

        public void SetMass(float mass)
        {
            _mass = Mathf.Max(0.1f, mass);
            if (RigidbodyComponent != null)
            {
                RigidbodyComponent.mass = _mass;
            }
        }

        public void SetLinearDamping(float damping)
        {
            _linearDamping = Mathf.Max(0f, damping);
            if (RigidbodyComponent != null)
            {
                RigidbodyComponent.linearDamping = _linearDamping;
            }
        }
    }
}
