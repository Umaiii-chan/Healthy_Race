using UnityEngine;

namespace HealthyRace.Visuals
{
    public sealed class VisualEffectEmitter : MonoBehaviour
    {
        [SerializeField]
        private VisualCatalog _catalog;

        [SerializeField]
        private FeedbackKind _kind;
        public bool ReduceFlashes { get; set; }

        public void Configure(VisualCatalog catalog, FeedbackKind kind)
        {
            _catalog = catalog;
            _kind = kind;
        }

        public void Play() =>
            VisualFeedback.Emit(_catalog, _kind, transform.position, ReduceFlashes);
    }
}
