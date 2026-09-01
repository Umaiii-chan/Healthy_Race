using HealthyRace.Core;
using UnityEngine;

namespace HealthyRace.Player
{
    /// <summary>
    /// Gives MovementTest collision blocks a runtime placeholder visual.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class MovementTestPlatform : MonoBehaviour
    {
        [SerializeField]
        private Color _color = new(0.15f, 0.62f, 0.43f);

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyVisual();
        }

        public void Configure(Color color)
        {
            _color = color;

            if (_spriteRenderer != null)
            {
                ApplyVisual();
            }
        }

        private void ApplyVisual()
        {
            _spriteRenderer.sprite = PlaceholderSpriteFactory.Square;
            _spriteRenderer.color = _color;
            _spriteRenderer.sortingOrder = 0;
        }
    }
}
