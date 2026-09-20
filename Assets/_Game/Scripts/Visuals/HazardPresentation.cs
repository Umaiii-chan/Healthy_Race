using UnityEngine;

namespace HealthyRace.Visuals
{
    public sealed class HazardPresentation : MonoBehaviour
    {
        [SerializeField]
        private Transform _art;

        [SerializeField]
        private TextMesh _warning;
        private Vector3 _rest;
        private float _anticipation;
        public bool ReduceFlashes { get; set; }

        public void Configure(Transform art, TextMesh warning)
        {
            _art = art;
            _warning = warning;
            _rest = art.localPosition;
        }

        private void Awake()
        {
            if (_art != null)
                _rest = _art.localPosition;
        }

        public void PresentWarning(float progress)
        {
            _anticipation = Mathf.Clamp01(progress);
            if (_warning != null)
                _warning.text = _anticipation > 0f ? "!  VAI CAIR  !" : "!  INSTÁVEL";
        }

        private void Update()
        {
            if (_art != null)
                _art.localPosition =
                    _rest
                    + Vector3.right
                        * (ReduceFlashes ? 0f : Mathf.Sin(Time.time * 40) * 0.045f * _anticipation);
        }

        private void OnDisable()
        {
            if (_art != null)
                _art.localPosition = _rest;
        }
    }
}
