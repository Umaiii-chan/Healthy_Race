using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>
    /// Camada de fundo: acompanha a câmera por uma fração do deslocamento (quanto
    /// mais longe, mais perto de 1) e pode deslizar sozinha, como as nuvens.
    /// Com câmera fixa só o deslize aparece; em fases com rolagem as camadas se
    /// separam em profundidade.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        private float _followFactor = 0.5f;

        [SerializeField]
        private float _driftSpeed;

        [SerializeField, Min(0f)]
        private float _wrapWidth;

        private Transform _camera;
        private Vector3 _cameraOrigin;
        private float _drift;
        private Vector3 _origin;

        /// <summary>Desliga o deslize próprio (opção "reduzir flashes e tremores").</summary>
        public static bool ReduceMotion { get; set; }

        public float FollowFactor => _followFactor;

        /// <param name="phase">Deslize inicial: espalha camadas iguais (nuvens) pelo percurso.</param>
        public void Configure(
            Transform cameraTransform,
            float followFactor,
            float driftSpeed,
            float wrapWidth,
            float phase = 0f
        )
        {
            _drift = phase;
            _origin = transform.position;
            _followFactor = Mathf.Clamp01(followFactor);
            _driftSpeed = driftSpeed;
            _wrapWidth = Mathf.Max(0f, wrapWidth);
            Bind(cameraTransform);
        }

        /// <summary>Liga a câmera que a camada acompanha, sem mexer na origem.</summary>
        public void Bind(Transform cameraTransform)
        {
            _camera = cameraTransform;
            _cameraOrigin = cameraTransform != null ? cameraTransform.position : Vector3.zero;
            Refresh(0f);
        }

        private void LateUpdate()
        {
            Refresh(Time.deltaTime);
        }

        public void Refresh(float deltaTime)
        {
            if (!ReduceMotion)
            {
                _drift += _driftSpeed * deltaTime;
            }

            if (_wrapWidth > 0f)
            {
                _drift = Mathf.Repeat(_drift, _wrapWidth);
            }

            var follow =
                _camera != null ? (_camera.position - _cameraOrigin) * _followFactor : Vector3.zero;
            transform.position = new Vector3(
                _origin.x + follow.x + _drift,
                _origin.y + follow.y,
                _origin.z
            );
        }
    }
}
