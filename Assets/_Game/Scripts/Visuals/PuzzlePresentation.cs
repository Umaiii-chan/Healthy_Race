using HealthyRace.Puzzles;
using UnityEngine;

namespace HealthyRace.Visuals
{
    public enum PuzzleVisualKind
    {
        Box,
        Button,
        Lever,
        Door,
        Platform,
    }

    public sealed class PuzzlePresentation : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer _art;

        [SerializeField]
        private TextMesh _status;

        [SerializeField]
        private VisualCatalog _catalog;

        [SerializeField]
        private PressureButton _source;

        [SerializeField]
        private PuzzleVisualKind _kind;
        private Vector3 _restScale = Vector3.one;
        private Vector3 _restPosition;
        public PuzzleAppearance State { get; private set; }
        public bool ReduceFlashes { get; set; }

        public void Configure(
            SpriteRenderer art,
            TextMesh status,
            VisualCatalog catalog,
            PuzzleVisualKind kind = PuzzleVisualKind.Box
        )
        {
            _art = art;
            _status = status;
            _catalog = catalog;
            _kind = kind;
            CacheRest();
            Present(PuzzleAppearance.Inactive);
        }

        private void Awake() => CacheRest();

        private void CacheRest()
        {
            if (_art != null)
            {
                _restScale = _art.transform.localScale;
                _restPosition = _art.transform.localPosition;
            }
        }

        public void Bind(PressureButton source)
        {
            Unsubscribe();
            _source = source;
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_source == null)
                return;
            _source.OnPressedStateChanged -= OnPressed;
            _source.OnPressedStateChanged += OnPressed;
            OnPressed(_source.IsPressed);
        }

        private void Unsubscribe()
        {
            if (_source != null)
                _source.OnPressedStateChanged -= OnPressed;
        }

        private void OnPressed(bool pressed) =>
            Present(pressed ? PuzzleAppearance.Active : PuzzleAppearance.Inactive);

        public void Present(PuzzleAppearance state)
        {
            var changed = State != state;
            State = state;
            if (_art != null)
                _art.color =
                    state == PuzzleAppearance.Blocked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
            if (_art != null)
            {
                var active = state == PuzzleAppearance.Active;
                _art.transform.localScale = _restScale;
                _art.transform.localPosition = _restPosition;
                _art.transform.localRotation = Quaternion.identity;
                if (_kind == PuzzleVisualKind.Button && active)
                    _art.transform.localPosition += Vector3.down * 0.1f;
                if (_kind == PuzzleVisualKind.Lever)
                    _art.transform.localRotation = Quaternion.Euler(0, 0, active ? -35 : 0);
                if (_kind == PuzzleVisualKind.Door && active)
                    _art.transform.localScale = new Vector3(
                        _restScale.x * 0.18f,
                        _restScale.y,
                        _restScale.z
                    );
            }
            if (_status != null)
            {
                _status.text = state switch
                {
                    PuzzleAppearance.Active => "●  ATIVO",
                    PuzzleAppearance.Blocked => "×  BLOQUEADO",
                    PuzzleAppearance.Interactable => "↔  INTERAGIR",
                    _ => "○  INATIVO",
                };
                _status.color =
                    state == PuzzleAppearance.Active
                        ? new Color(0.12f, 0.4f, 0.16f)
                        : new Color(0.11f, 0.18f, 0.16f);
            }
            if (Application.isPlaying && changed && state == PuzzleAppearance.Active)
                VisualFeedback.Emit(
                    _catalog,
                    FeedbackKind.PuzzleActivation,
                    transform.position,
                    ReduceFlashes
                );
        }
    }
}
