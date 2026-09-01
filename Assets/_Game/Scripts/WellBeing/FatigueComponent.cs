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
        private FatigueStateMachine _stateMachine;
        private WellbeingComponent _wellbeing;

        public event Action<int> EpisodeCountChanged;

        public event Action<FatigueReadiness> ReadinessChanged;

        public event Action<FatigueState> StateChanged;

        public int FatigueEpisodeCount => _stateMachine?.EpisodeCount ?? 0;

        public FatigueReadiness Readiness => _stateMachine?.Readiness ?? FatigueReadiness.Armed;

        public FatigueState State => _stateMachine?.State ?? FatigueState.Normal;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _wellbeing = GetComponent<WellbeingComponent>();
            _stateMachine = new FatigueStateMachine(_triggerThreshold, _rearmThreshold);
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

            _playerController?.SetMovementEnabled(true);
        }

        private void OnEnable()
        {
            if (_wellbeing != null)
            {
                _wellbeing.WellbeingChanged += HandleWellbeingChanged;
            }
        }

        private void Update()
        {
            if (State != FatigueState.FatiguePending)
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
