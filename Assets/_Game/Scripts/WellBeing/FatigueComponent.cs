using System;
using System.Collections;
using HealthyRace.Player;
using UnityEngine;

namespace HealthyRace.WellBeing
{
    [RequireComponent(typeof(WellbeingComponent))]
    public sealed class FatigueComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _fatigueDurationSeconds = 2f;

        [SerializeField]
        private int _recoveryWellbeing = 15;

        [SerializeField]
        private int _triggerThreshold = 20;

        [SerializeField]
        private int _rearmThreshold = 25;

        private PlayerController _playerController;
        private Coroutine _fatigueRoutine;
        private bool _halted;
        private FatigueStateMachine _stateMachine;
        private WellbeingComponent _wellbeing;

        public event Action<int> EpisodeCountChanged;

        public event Action<FatigueReadiness> ReadinessChanged;

        public event Action<FatigueState> StateChanged;

        public int FatigueEpisodeCount => _stateMachine?.EpisodeCount ?? 0;

        public FatigueReadiness Readiness => _stateMachine?.Readiness ?? FatigueReadiness.Armed;

        public FatigueState State => _stateMachine?.State ?? FatigueState.Normal;

        public float FatigueDurationSeconds
        {
            get => _fatigueDurationSeconds;
            set => _fatigueDurationSeconds = Mathf.Max(0f, value);
        }

        public int RecoveryWellbeing
        {
            get => _recoveryWellbeing;
            set => _recoveryWellbeing = value;
        }

        public int TriggerThreshold => _triggerThreshold;

        public int RearmThreshold => _rearmThreshold;

        public FatigueStateMachine StateMachine => _stateMachine;

        /// <summary>Verdadeiro depois de <see cref="Halt"/>: a fadiga não age mais.</summary>
        public bool IsHalted => _halted;

        /// <summary>
        /// Fim de partida: cancela o episódio em curso sem devolver o movimento nem
        /// aplicar a recuperação, e para de reagir ao Bem-Estar.
        /// </summary>
        public void Halt()
        {
            _halted = true;
            Unsubscribe();

            if (_fatigueRoutine != null)
            {
                StopCoroutine(_fatigueRoutine);
                _fatigueRoutine = null;
            }
        }

        public void Configure(
            float fatigueDurationSeconds,
            int recoveryWellbeing,
            int triggerThreshold = 20,
            int rearmThreshold = 25
        )
        {
            _fatigueDurationSeconds = fatigueDurationSeconds;
            _recoveryWellbeing = recoveryWellbeing;
            _triggerThreshold = triggerThreshold;
            _rearmThreshold = rearmThreshold;
            _stateMachine = new FatigueStateMachine(_triggerThreshold, _rearmThreshold);
        }

        public void EnsureInitialized()
        {
            _playerController ??= GetComponent<PlayerController>();
            _wellbeing ??= GetComponent<WellbeingComponent>();
            _stateMachine ??= new FatigueStateMachine(_triggerThreshold, _rearmThreshold);

            if (_wellbeing != null)
            {
                _wellbeing.WellbeingChanged -= HandleWellbeingChanged;
                _wellbeing.WellbeingChanged += HandleWellbeingChanged;
            }
        }

        public void ProcessPendingFatigue(bool isGrounded)
        {
            Transition(() => _stateMachine.TryBeginPendingFatigue(isGrounded));
        }

        public void CompleteFatigueImmediately()
        {
            if (State != FatigueState.Fatigued && State != FatigueState.FatiguePending)
            {
                return;
            }

            if (_fatigueRoutine != null)
            {
                StopCoroutine(_fatigueRoutine);
                _fatigueRoutine = null;
            }

            Transition(_stateMachine.BeginRecovery);
            _wellbeing?.ApplyDelta(_recoveryWellbeing);
            _playerController?.SetMovementEnabled(true);
            Transition(() =>
                _stateMachine.FinishRecovery(_wellbeing != null ? _wellbeing.CurrentValue : 0)
            );
        }

        /// <summary>
        /// Desfaz <see cref="Halt"/> para uma partida nova na mesma cena: a máquina
        /// de fadiga recomeça do zero (sem episódio em curso nem contagem antiga).
        /// </summary>
        public void Resume()
        {
            if (!_halted)
            {
                return;
            }

            _halted = false;
            _stateMachine = new FatigueStateMachine(_triggerThreshold, _rearmThreshold);
            EnsureInitialized();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (_fatigueRoutine != null)
            {
                StopCoroutine(_fatigueRoutine);
                _fatigueRoutine = null;
            }

            if (!_halted)
            {
                _playerController?.SetMovementEnabled(true);
            }
        }

        private void OnEnable()
        {
            if (_halted)
            {
                return;
            }

            EnsureInitialized();

            if (_wellbeing != null)
            {
                _wellbeing.WellbeingChanged -= HandleWellbeingChanged;
                _wellbeing.WellbeingChanged += HandleWellbeingChanged;
            }
        }

        private void Update()
        {
            if (_halted || State != FatigueState.FatiguePending)
            {
                return;
            }

            Transition(() => _stateMachine.TryBeginPendingFatigue(IsPlayerGrounded()));
        }

        private IEnumerator CompleteAfterDelay()
        {
            yield return new WaitForSeconds(_fatigueDurationSeconds);
            _fatigueRoutine = null;

            if (!isActiveAndEnabled || State != FatigueState.Fatigued)
            {
                yield break;
            }

            Transition(_stateMachine.BeginRecovery);
            _wellbeing.ApplyDelta(_recoveryWellbeing);
            _playerController?.SetMovementEnabled(true);
            Transition(() => _stateMachine.FinishRecovery(_wellbeing.CurrentValue));
        }

        private void HandleWellbeingChanged(int previousValue, int currentValue)
        {
            Transition(() =>
                _stateMachine.ProcessWellbeingChange(
                    previousValue,
                    currentValue,
                    IsPlayerGrounded()
                )
            );
        }

        private bool IsPlayerGrounded()
        {
            return _playerController == null || _playerController.IsGrounded;
        }

        private void Transition(Action transition)
        {
            var previousEpisodeCount = FatigueEpisodeCount;
            var previousReadiness = Readiness;
            var previousState = State;

            transition();

            if (previousEpisodeCount != FatigueEpisodeCount)
            {
                EpisodeCountChanged?.Invoke(FatigueEpisodeCount);
            }

            if (previousReadiness != Readiness)
            {
                ReadinessChanged?.Invoke(Readiness);
            }

            if (previousState == State)
            {
                return;
            }

            StateChanged?.Invoke(State);

            if (State == FatigueState.Fatigued)
            {
                BeginFatigue();
            }
        }

        private void BeginFatigue()
        {
            _playerController?.SetMovementEnabled(false);
            _fatigueRoutine = StartCoroutine(CompleteAfterDelay());
        }

        private void Unsubscribe()
        {
            if (_wellbeing != null)
            {
                _wellbeing.WellbeingChanged -= HandleWellbeingChanged;
            }
        }
    }
}
