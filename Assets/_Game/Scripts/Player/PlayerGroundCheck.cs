using UnityEngine;

namespace HealthyRace.Player
{
    /// <summary>
    /// Queries the configured ground layer underneath this player's collider.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerGroundCheck : MonoBehaviour
    {
        private const int GroundLayer = 6;

        [SerializeField, Min(0.01f)]
        private float _checkDistance = 0.08f;

        [SerializeField, Range(0.1f, 1f)]
        private float _checkWidthMultiplier = 0.82f;

        [SerializeField, Min(0.01f)]
        private float _checkHeight = 0.04f;

        [SerializeField]
        private LayerMask _groundMask = 1 << GroundLayer;

        private Collider2D _collider;

        public float CheckDistance => _checkDistance;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void FixedUpdate()
        {
            Refresh();
        }

        private void OnDrawGizmosSelected()
        {
            var playerCollider = _collider != null ? _collider : GetComponent<Collider2D>();
            if (playerCollider == null)
            {
                return;
            }

            GetCheckBox(playerCollider.bounds, out var origin, out var size);
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(origin + Vector2.down * _checkDistance, size);
        }

        public bool Refresh()
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }

            if (_collider == null)
            {
                IsGrounded = false;
                return false;
            }

            GetCheckBox(_collider.bounds, out var origin, out var size);
            IsGrounded = Physics2D.BoxCast(
                origin,
                size,
                0f,
                Vector2.down,
                _checkDistance,
                _groundMask
            );
            return IsGrounded;
        }

        private void GetCheckBox(Bounds bounds, out Vector2 origin, out Vector2 size)
        {
            size = new Vector2(bounds.size.x * _checkWidthMultiplier, _checkHeight);
            origin = new Vector2(bounds.center.x, bounds.min.y + _checkHeight * 0.5f);
        }
    }
}
