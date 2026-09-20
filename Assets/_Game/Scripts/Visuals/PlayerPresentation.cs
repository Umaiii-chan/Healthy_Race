using HealthyRace.Player;
using HealthyRace.WellBeing;
using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>Attach the visual prefab as a child. No physics, input or gameplay writes.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerPresentation : MonoBehaviour
    {
        [SerializeField]
        private VisualCatalog _catalog;

        [SerializeField]
        private SpriteRenderer _body;

        [SerializeField]
        private SpriteRenderer _identity;

        [SerializeField]
        private TextMesh _badge;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private int _player = 1;

        [SerializeField]
        private WellbeingComponent _wellbeing;

        [SerializeField]
        private FatigueComponent _fatigue;

        [SerializeField]
        private PlayerController _controller;
        private float _actionUntil;
        private bool _terminal;
        private bool _subscribed;
        private bool _fatigued;
        private VisualAction _action;
        private int? _lastWellbeing;
        public bool ReduceFlashes { get; set; }
        public WellbeingAppearance Appearance { get; private set; }
        public VisualAction Action => _action;
        public int PlayerIndex => _player;
        public int PresentationUpdates { get; private set; }

        public void Configure(
            VisualCatalog catalog,
            SpriteRenderer body,
            SpriteRenderer identity,
            TextMesh badge,
            Animator animator,
            int player
        )
        {
            _catalog = catalog;
            _body = body;
            _identity = identity;
            _badge = badge;
            _animator = animator;
            SetIdentity(player);
            PresentWellbeing(100);
        }

        public void Bind(
            WellbeingComponent wellbeing,
            FatigueComponent fatigue = null,
            PlayerController controller = null
        )
        {
            Unsubscribe();
            _wellbeing = wellbeing;
            _fatigue = fatigue;
            _controller = controller;
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed)
                return;
            _subscribed = true;
            if (_wellbeing != null)
            {
                _wellbeing.ValueChanged += PresentWellbeing;
                PresentWellbeing(_wellbeing.CurrentValue);
            }
            if (_fatigue != null)
            {
                _fatigue.StateChanged += PresentFatigue;
                PresentFatigue(_fatigue.State);
            }
            SetIdentity(_controller != null ? _controller.PlayerIndex : _player);
        }

        private void Unsubscribe()
        {
            if (_wellbeing != null)
                _wellbeing.ValueChanged -= PresentWellbeing;
            if (_fatigue != null)
                _fatigue.StateChanged -= PresentFatigue;
            _subscribed = false;
        }

        public void SetIdentity(int player)
        {
            _player = player == 2 ? 2 : 1;
            if (_identity != null && _catalog != null)
                _identity.color = _catalog.IdentityColor(_player);
            if (_badge != null)
                _badge.text = _player == 1 ? "P1  •" : "P2  ◆";
        }

        public void PresentWellbeing(int value)
        {
            var next = VisualCatalog.AppearanceFor(value);
            if (_catalog != null && _body != null)
            {
                if (Application.isPlaying && Appearance != next)
                    VisualFeedback.Emit(
                        _catalog,
                        FeedbackKind.BandChange,
                        transform.position,
                        ReduceFlashes
                    );
                else if (
                    Application.isPlaying
                    && _lastWellbeing.HasValue
                    && _lastWellbeing.Value != value
                )
                    VisualFeedback.Emit(
                        _catalog,
                        FeedbackKind.WellbeingChange,
                        transform.position,
                        ReduceFlashes
                    );
                _body.sprite = _catalog.Character(next);
            }
            Appearance = next;
            _lastWellbeing = value;
            PresentationUpdates++;
        }

        private void PresentFatigue(FatigueState state)
        {
            _fatigued = state == FatigueState.Fatigued;
            if (_fatigued)
                Play(VisualAction.Fatigue);
            else if (state == FatigueState.Recovering)
                Play(VisualAction.Recover, 0.45f);
        }

        public void Play(VisualAction action, float holdSeconds = 0f)
        {
            _terminal = action == VisualAction.Victory || action == VisualAction.Defeat;
            _actionUntil = Time.time + Mathf.Max(0f, holdSeconds);
            if (_action == action && _animator != null && _animator.isInitialized)
                return;
            if (Application.isPlaying && _action != action)
            {
                if (action == VisualAction.Fatigue)
                    VisualFeedback.Emit(
                        _catalog,
                        FeedbackKind.Fatigue,
                        transform.position,
                        ReduceFlashes
                    );
                if (action == VisualAction.Recover)
                    VisualFeedback.Emit(
                        _catalog,
                        FeedbackKind.Recovery,
                        transform.position,
                        ReduceFlashes
                    );
                if (action == VisualAction.Victory)
                    VisualFeedback.Emit(
                        _catalog,
                        FeedbackKind.Victory,
                        transform.position,
                        ReduceFlashes
                    );
                if (action == VisualAction.Defeat)
                    VisualFeedback.Emit(
                        _catalog,
                        FeedbackKind.Defeat,
                        transform.position,
                        ReduceFlashes
                    );
            }
            _action = action;
            if (_animator != null && _animator.runtimeAnimatorController != null)
                _animator.SetInteger("Action", (int)action);
        }

        public void ResetPresentation()
        {
            _terminal = false;
            _actionUntil = 0f;
            _fatigued = false;
            Play(VisualAction.Idle);
        }

        private void LateUpdate()
        {
            if (_controller == null || _terminal || _fatigued || Time.time < _actionUntil)
                return;
            var state = _controller.CurrentState;
            var action = state switch
            {
                PlayerState.Move => VisualAction.Move,
                PlayerState.Jump => VisualAction.Jump,
                PlayerState.Fall => VisualAction.Fall,
                PlayerState.Land => VisualAction.Land,
                _ => VisualAction.Idle,
            };
            if (_action != action)
                Play(action, action == VisualAction.Land ? 0.12f : 0f);
        }
    }
}
