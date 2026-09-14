using UnityEngine;

namespace HealthyRace.Obstacles
{
    /// <summary>
    /// Represents a dynamic, pushable puzzle crate with continuous collision detection
    /// and frozen rotation for predictable 2D puzzle mechanics.
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

        private Rigidbody2D _rigidbody;
        private Collider2D _collider;

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

        private void Awake()
        {
            InitializePhysics();
        }

        private void Reset()
        {
            InitializePhysics();
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
