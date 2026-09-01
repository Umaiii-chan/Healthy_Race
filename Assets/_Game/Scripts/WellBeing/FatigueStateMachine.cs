namespace HealthyRace.WellBeing
{
    public enum FatigueReadiness
    {
        Armed,
        Disarmed,
    }

    public enum FatigueState
    {
        Normal,
        FatiguePending,
        Fatigued,
        Recovering,
    }

    /// <summary>
    /// Encapsulates fatigue transitions independently from Unity timing and physics.
    /// The component supplies grounded state and performs the timed recovery.
    /// </summary>
    public sealed class FatigueStateMachine
    {
        private readonly int _rearmThreshold;
        private readonly int _triggerThreshold;

        public FatigueStateMachine(int triggerThreshold, int rearmThreshold)
        {
            _triggerThreshold = triggerThreshold;
            _rearmThreshold = rearmThreshold;
        }

        public int EpisodeCount { get; private set; }

        public FatigueReadiness Readiness { get; private set; } = FatigueReadiness.Armed;

        public FatigueState State { get; private set; } = FatigueState.Normal;

        public void BeginRecovery()
        {
            if (State == FatigueState.Fatigued)
            {
                State = FatigueState.Recovering;
            }
        }

        public void FinishRecovery(int wellbeingValue)
        {
            if (State != FatigueState.Recovering)
            {
                return;
            }

            State = FatigueState.Normal;
            TryRearm(wellbeingValue);
        }

        public void ProcessWellbeingChange(int previousValue, int currentValue, bool isGrounded)
        {
            if (State != FatigueState.Normal)
            {
                return;
            }

            TryRearm(currentValue);

            if (
                Readiness != FatigueReadiness.Armed
                || previousValue <= _triggerThreshold
                || currentValue > _triggerThreshold
            )
            {
                return;
            }

            EpisodeCount++;
            Readiness = FatigueReadiness.Disarmed;
            State = isGrounded ? FatigueState.Fatigued : FatigueState.FatiguePending;
        }

        public void TryBeginPendingFatigue(bool isGrounded)
        {
            if (State == FatigueState.FatiguePending && isGrounded)
            {
                State = FatigueState.Fatigued;
            }
        }

        private void TryRearm(int wellbeingValue)
        {
            if (Readiness == FatigueReadiness.Disarmed && wellbeingValue > _rearmThreshold)
            {
                Readiness = FatigueReadiness.Armed;
            }
        }
    }
}
