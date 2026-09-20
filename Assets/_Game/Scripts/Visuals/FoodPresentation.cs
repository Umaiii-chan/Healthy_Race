using UnityEngine;

namespace HealthyRace.Visuals
{
    public sealed class FoodPresentation : MonoBehaviour
    {
        [SerializeField]
        private VisualCatalog _catalog;

        [SerializeField]
        private SpriteRenderer _art;

        [SerializeField]
        private bool _healthy;
        private Vector3 _rest;
        private Vector3 _scale;
        private float _pickupAt = -1f;
        public bool ReduceFlashes { get; set; }

        public void Configure(VisualCatalog catalog, SpriteRenderer art, int foodIndex)
        {
            _catalog = catalog;
            _art = art;
            _healthy = foodIndex < 10;
            _art.sprite = catalog.Foods[foodIndex];
        }

        private void OnEnable()
        {
            if (_art == null)
                return;
            _rest = _art.transform.localPosition;
            _scale = _art.transform.localScale;
            _pickupAt = -1f;
            _art.enabled = true;
        }

        private void OnDisable()
        {
            if (_art == null)
                return;
            _art.transform.localPosition = _rest;
            _art.transform.localScale = _scale;
        }

        public void Pickup()
        {
            if (_pickupAt >= 0f)
                return;
            _pickupAt = Time.time;
            VisualFeedback.Emit(
                _catalog,
                _healthy ? FeedbackKind.HealthyPickup : FeedbackKind.UltraProcessedPickup,
                transform.position,
                ReduceFlashes
            );
        }

        public void ResetPresentation()
        {
            _pickupAt = -1f;
            if (_art != null)
            {
                _art.enabled = true;
                _art.transform.localScale = _scale;
            }
        }

        private void Update()
        {
            if (_art == null)
                return;
            if (_pickupAt >= 0f)
            {
                var t = Mathf.Clamp01((Time.time - _pickupAt) / 0.2f);
                _art.transform.localScale = _scale * (1 - t);
                _art.transform.localPosition = _rest + Vector3.up * t * 0.3f;
                if (t >= 1f)
                    _art.enabled = false;
            }
            else
                _art.transform.localPosition =
                    _rest
                    + Vector3.up * (Mathf.Sin(Time.time * 2.5f + transform.position.x) * 0.055f);
        }
    }
}
