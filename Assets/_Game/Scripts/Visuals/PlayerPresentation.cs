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

        [SerializeField]
        private CharacterRig _rig;

        private const float BlockedSecondsToPush = 0.08f;
        private const float FeetOffset = 0.6f;

        // A sonda cruza a borda do corpo (meia largura 0,425): o terreno tem colisor
        // só de contorno, então uma sonda inteira dentro da parede não acha nada.
        private const float ProbeDistance = 0.48f;
        private static readonly Vector2 ProbeSize = new(0.2f, 0.6f);
        private static readonly Collider2D[] ProbeHits = new Collider2D[8];

        private PlayerMovement _movement;
        private float _blockedFor;
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
        public CharacterRig Rig => _rig;

        /// <summary>Verdadeiro enquanto o jogador anda contra algo sólido (caixa, parede).</summary>
        public bool IsBlocked { get; private set; }

        /// <summary>Liga o personagem articulado; sem ele vale o desenho único antigo.</summary>
        public void ConfigureRig(CharacterRig rig)
        {
            _rig = rig;
            PresentWellbeing(_lastWellbeing ?? 100);
        }

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
            _movement = controller != null ? controller.GetComponent<PlayerMovement>() : null;
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
            if (_catalog != null && (_body != null || _rig != null))
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
                if (_rig != null && _catalog.TryGetSkin(next, out var skin))
                    _rig.Apply(skin);
                if (_body != null)
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
            var previous = _action;
            _action = action;
            if (_animator != null && _animator.runtimeAnimatorController != null)
                _animator.SetInteger("Action", (int)action);
            if (_rig != null)
                _rig.SetExpression(ExpressionFor(action));
            // poeira nos dois contatos com o chão: a saída do pulo e a aterrissagem
            if (
                Application.isPlaying
                && previous != action
                && (action == VisualAction.Jump || action == VisualAction.Land)
            )
                VisualFeedback.Emit(
                    _catalog,
                    FeedbackKind.Dust,
                    transform.position + Vector3.down * FeetOffset,
                    ReduceFlashes
                );
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
            var blocked = UpdateRig();
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
            // andando contra algo que não cede (caixa, parede): pose de empurrar
            if (action == VisualAction.Move && blocked)
                action = VisualAction.Push;
            if (_action != action)
                Play(action, action == VisualAction.Land ? 0.12f : 0f);
        }

        /// <summary>
        /// Repassa ao rig o que ele precisa do jogador (lado e velocidade) e diz se o
        /// jogador está há algum tempo andando sem sair do lugar.
        /// </summary>
        private bool UpdateRig()
        {
            // antes de qualquer retorno: vale também para rigs soltos (galeria, menu)
            if (_rig != null)
            {
                _rig.ReduceMotion = ReduceFlashes;
            }

            if (_movement == null)
            {
                _blockedFor = 0f;
                IsBlocked = false;
                return false;
            }

            var input = _movement.IsMovementEnabled ? _movement.MoveInput : 0f;
            var velocity = _movement.Velocity;
            if (_rig != null)
            {
                _rig.Velocity = velocity;
                _rig.SetFacing(input);
                // a queda do lanche aparece no rosto enquanto dura
                _rig.SetExpression(ExpressionFor(_action));
            }

            var pushing = Mathf.Abs(input) > 0.5f && _movement.IsGrounded && HasSolidAhead(input);
            _blockedFor = pushing ? _blockedFor + Time.deltaTime : 0f;
            IsBlocked = _blockedFor >= BlockedSecondsToPush;
            return IsBlocked;
        }

        /// <summary>Esforço ao empurrar, pular, interagir ou na queda do lanche (sonolento).</summary>
        private CharacterExpression ExpressionFor(VisualAction action) =>
            action == VisualAction.Push
            || action == VisualAction.Jump
            || action == VisualAction.Interact
            || (_movement != null && _movement.IsCrashing)
                ? CharacterExpression.Effort
                : CharacterExpression.Normal;

        /// <summary>Consulta (só leitura) se há algo sólido encostado na frente do jogador.</summary>
        private bool HasSolidAhead(float direction)
        {
            var body = _movement.GetComponent<Rigidbody2D>();
            var center =
                (Vector2)_movement.transform.position
                + new Vector2(Mathf.Sign(direction) * ProbeDistance, 0f);
            var filter = new ContactFilter2D { useTriggers = false };
            var count = Physics2D.OverlapBox(center, ProbeSize, 0f, filter, ProbeHits);
            for (var i = 0; i < count; i++)
            {
                if (ProbeHits[i] != null && ProbeHits[i].attachedRigidbody != body)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
