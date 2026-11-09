using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>
    /// Flutuação decorativa de um elemento de UI. Usa tempo não escalado para
    /// continuar viva com a partida pausada.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiFloat : MonoBehaviour
    {
        [SerializeField]
        private float _amplitude = 12f;

        [SerializeField, Min(0.1f)]
        private float _period = 2.4f;

        [SerializeField]
        private float _phase;

        [SerializeField]
        private float _tilt;

        private bool _captured;
        private Vector2 _origin;
        private RectTransform _rect;

        /// <summary>Desliga o movimento (opção "reduzir flashes e tremores").</summary>
        public static bool ReduceMotion { get; set; }

        public void Configure(float amplitude, float period, float phase, float tilt)
        {
            _amplitude = amplitude;
            _period = Mathf.Max(0.1f, period);
            _phase = phase;
            _tilt = tilt;
        }

        private void Awake()
        {
            Capture();
        }

        private void Update()
        {
            Capture();
            if (_rect == null)
            {
                return;
            }

            var wave = ReduceMotion
                ? 0f
                : Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / _period) + _phase);
            _rect.anchoredPosition = _origin + Vector2.up * (wave * _amplitude);
            _rect.localRotation = Quaternion.Euler(0f, 0f, wave * _tilt);
        }

        private void Capture()
        {
            if (_captured)
            {
                return;
            }

            _rect = transform as RectTransform;
            if (_rect != null)
            {
                _origin = _rect.anchoredPosition;
                _captured = true;
            }
        }
    }
}
