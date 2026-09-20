using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>Short, bounded sprite bursts; no particle package or gameplay dependency.</summary>
    public sealed class VisualFeedback : MonoBehaviour
    {
        private SpriteRenderer[] _particles;
        private Vector3[] _directions;
        private Color _color;
        private float _age;
        private float _duration;
        private bool _reduced;
        private Vector3 _origin;
        private FeedbackKind _kind;

        public static VisualFeedback Emit(
            VisualCatalog catalog,
            FeedbackKind kind,
            Vector3 position,
            bool reduced = false
        )
        {
            if (catalog == null || catalog.Particle == null)
                return null;
            var effect = new GameObject("VFX_" + kind).AddComponent<VisualFeedback>();
            effect.Initialize(catalog, kind, position, reduced);
            return effect;
        }

        private void Initialize(
            VisualCatalog catalog,
            FeedbackKind kind,
            Vector3 position,
            bool reduced
        )
        {
            _origin = position;
            transform.position = position;
            _reduced = reduced;
            _kind = kind;
            var negative =
                kind == FeedbackKind.UltraProcessedPickup
                || kind == FeedbackKind.Fatigue
                || kind == FeedbackKind.Defeat;
            _color = negative ? new Color(1f, 0.55f, 0.24f) : new Color(0.7f, 1f, 0.4f);
            if (kind == FeedbackKind.Checkpoint || kind == FeedbackKind.Finish)
                _color = new Color(0.4f, 0.84f, 1f);
            if (kind == FeedbackKind.PuzzleActivation)
                _color = new Color(1f, 0.86f, 0.4f);
            _duration = kind == FeedbackKind.Victory ? 0.85f : 0.55f;
            var count =
                reduced ? 3
                : kind == FeedbackKind.Victory ? 10
                : 6;
            _particles = new SpriteRenderer[count];
            _directions = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var child = new GameObject("Spark");
                child.transform.SetParent(transform, false);
                var renderer = child.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = catalog.SpriteMaterial;
                renderer.sprite = kind switch
                {
                    FeedbackKind.Recovery or FeedbackKind.HealthyPickup => catalog.Heart,
                    FeedbackKind.Fatigue or FeedbackKind.Defeat => i % 2 == 0
                        ? catalog.Droplet
                        : catalog.Particle,
                    FeedbackKind.UltraProcessedPickup => catalog.Droplet,
                    FeedbackKind.BandChange
                    or FeedbackKind.WellbeingChange
                    or FeedbackKind.Finish => catalog.Ring,
                    _ => catalog.Particle,
                };
                renderer.sortingOrder = 35;
                renderer.color = _color;
                _particles[i] = renderer;
                var angle = i * Mathf.PI * 2 / count;
                _directions[i] = new Vector3(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle) + (negative ? -0.25f : 0.5f),
                    0f
                );
            }
        }

        private void Update()
        {
            if (_particles == null)
                return;
            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / _duration);
            for (var i = 0; i < _particles.Length; i++)
            {
                _particles[i].transform.position =
                    _origin + _directions[i] * (0.15f + t * (_reduced ? 0.3f : 0.7f));
                if (_kind == FeedbackKind.Fatigue)
                    _particles[i].transform.position =
                        _origin
                        + new Vector3(
                            Mathf.Cos(i + t * 4) * 0.5f,
                            0.9f + Mathf.Sin(i + t * 4) * 0.15f,
                            0
                        );
                _particles[i].transform.localScale = Vector3.one * (0.12f * (1 - t) + 0.025f);
                _particles[i].color = new Color(
                    _color.r,
                    _color.g,
                    _color.b,
                    (1 - t) * (_reduced ? 0.55f : 0.85f)
                );
            }
            if (t >= 1)
                Destroy(gameObject);
        }
    }
}
