using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>
    /// Bolhas que sobem e estouram na superfície da lama: junto com a textura, é o
    /// que a faz ler como perigo mesmo parada. Só enfeite, sem física nem regra.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MudBubbles : MonoBehaviour
    {
        private const float Rise = 0.34f;
        private const float Period = 1.6f;

        [SerializeField]
        private Sprite _sprite;

        [SerializeField, Min(0.1f)]
        private float _width = 2f;

        [SerializeField]
        private float _surfaceY;

        private SpriteRenderer[] _bubbles;
        private float[] _offsets;
        private float[] _positions;

        /// <summary>Desliga as bolhas (opção "reduzir flashes e tremores").</summary>
        public static bool ReduceMotion { get; set; }

        public int BubbleCount => _bubbles != null ? _bubbles.Length : 0;

        public void Configure(Sprite sprite, float width, float surfaceY)
        {
            _sprite = sprite;
            _width = Mathf.Max(0.1f, width);
            _surfaceY = surfaceY;
        }

        private void Start()
        {
            // uma bolha a cada ~1,3 unidade de lama, no mínimo duas
            var count = Mathf.Max(2, Mathf.RoundToInt(_width / 1.3f));
            _bubbles = new SpriteRenderer[count];
            _offsets = new float[count];
            _positions = new float[count];
            for (var i = 0; i < count; i++)
            {
                var bubble = new GameObject("Bolha");
                bubble.transform.SetParent(transform, false);
                var renderer = bubble.AddComponent<SpriteRenderer>();
                renderer.sprite = _sprite;
                renderer.sortingOrder = 3;
                _bubbles[i] = renderer;
                _offsets[i] = (float)i / count;
                _positions[i] = NextPosition(i);
            }
        }

        private void Update()
        {
            if (_bubbles == null)
            {
                return;
            }

            for (var i = 0; i < _bubbles.Length; i++)
            {
                var renderer = _bubbles[i];
                renderer.enabled = !ReduceMotion;
                if (ReduceMotion)
                {
                    continue;
                }

                var phase = Time.time / Period + _offsets[i];
                var t = phase - Mathf.Floor(phase);
                if (t < Time.deltaTime / Period)
                {
                    _positions[i] = NextPosition(i);
                }

                // sobe crescendo e some de repente no fim, como bolha que estoura
                var size = 0.1f + 0.2f * Mathf.Sin(t * Mathf.PI * 0.5f);
                renderer.transform.localPosition = new Vector3(
                    _positions[i],
                    _surfaceY + t * Rise,
                    0f
                );
                renderer.transform.localScale = new Vector3(size, size, 1f);
                renderer.color = new Color(0.8f, 0.6f, 0.42f, t < 0.85f ? 0.9f : (1f - t) * 6f);
            }
        }

        private float NextPosition(int index)
        {
            var slot = _width / _bubbles.Length;
            return -_width * 0.5f + slot * (index + Random.Range(0.2f, 0.8f));
        }
    }
}
