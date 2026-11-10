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

        /// <summary>Normal do piso sob o jogador; para cima quando não há piso.</summary>
        public Vector2 GroundNormal { get; private set; } = Vector2.up;

        /// <summary>Verdadeiro sobre rampa (piso inclinado, mas ainda caminhável).</summary>
        public bool IsOnSlope => IsGrounded && GroundNormal.y < 0.98f && GroundNormal.y > 0.4f;

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

            var bounds = _collider.bounds;
            GetCheckBox(bounds, out var origin, out var size);
            var hit = Physics2D.BoxCast(
                origin,
                size,
                0f,
                Vector2.down,
                _checkDistance,
                _groundMask
            );
            if (hit.collider == null)
            {
                // Na rampa de 45° só a quina do corpo toca o piso: sob a borda do
                // sensor (mais estreito que o corpo) o piso fica mais longe que a
                // distância de checagem. A folga extra vale só para piso inclinado.
                var allowance = (bounds.size.x - size.x) * 0.5f + 0.02f;
                var slopeHit = Physics2D.BoxCast(
                    origin,
                    size,
                    0f,
                    Vector2.down,
                    _checkDistance + allowance,
                    _groundMask
                );
                if (slopeHit.collider != null && IsWalkableSlope(slopeHit.normal))
                {
                    hit = slopeHit;
                }
            }

            IsGrounded = hit.collider != null;
            GroundNormal = IsGrounded ? hit.normal : Vector2.up;
            return IsGrounded;
        }

        private static bool IsWalkableSlope(Vector2 normal) => normal.y < 0.98f && normal.y > 0.4f;

        /// <summary>
        /// Distância até o piso logo abaixo, até <paramref name="maxDistance"/>; negativo
        /// quando não há piso. Usado para manter o jogador colado na rampa.
        /// </summary>
        public float DistanceToGround(float maxDistance)
        {
            if (_collider == null)
            {
                return -1f;
            }

            GetCheckBox(_collider.bounds, out var origin, out var size);
            var hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, maxDistance, _groundMask);
            return hit.collider != null ? hit.distance : -1f;
        }

        private void GetCheckBox(Bounds bounds, out Vector2 origin, out Vector2 size)
        {
            size = new Vector2(bounds.size.x * _checkWidthMultiplier, _checkHeight);
            origin = new Vector2(bounds.center.x, bounds.min.y + _checkHeight * 0.5f);
        }
    }
}
