using System;
using HealthyRace.WellBeing;
using UnityEngine;

namespace HealthyRace.Collectibles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FoodCollectible : MonoBehaviour
    {
        [SerializeField]
        private FoodData _foodData;

        private bool _isCollected;

        public FoodData FoodData => _foodData;

        public bool IsCollected => _isCollected;

        public event Action<FoodCollectible, WellbeingComponent> Collected;

        private void Awake()
        {
            UpdateVisual();
        }

        public void Configure(FoodData foodData)
        {
            _foodData = foodData;
            UpdateVisual();
        }

        public void ResetState()
        {
            _isCollected = false;
            gameObject.SetActive(true);
        }

        private void UpdateVisual()
        {
            if (_foodData != null && _foodData.Sprite != null)
            {
                var spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = _foodData.Sprite;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected || _foodData == null)
            {
                return;
            }

            var wellbeing = other.GetComponentInParent<WellbeingComponent>();

            if (wellbeing == null)
            {
                return;
            }

            _isCollected = true;
            wellbeing.ApplyDelta(_foodData.WellbeingDelta);
            Collected?.Invoke(this, wellbeing);

            if (_foodData.CollectSound != null)
            {
                AudioSource.PlayClipAtPoint(_foodData.CollectSound, transform.position);
            }

            gameObject.SetActive(false);
        }
    }
}
