using System;
using UnityEngine;

namespace HealthyRace.Player
{
    public enum SnackState
    {
        None,

        /// <summary>Impulso logo depois do lanche: mais rápido por pouco tempo.</summary>
        Rush,

        /// <summary>Sonolência depois do impulso: mais lento até passar ou até comer fruta.</summary>
        Crash,
    }

    /// <summary>
    /// O efeito do lanche rápido (decisão D2): <b>pico</b> e depois <b>queda</b>.
    /// Comer outro lanche no pico renova o pico e alonga a queda que vem depois
    /// (encadear é possível e cada vez mais caro). Fruta ou água durante a queda a
    /// encerra. Lógica pura, sem física: <see cref="PlayerMovement"/> só aplica o
    /// <see cref="Multiplier"/>.
    /// </summary>
    public sealed class SnackRush
    {
        private readonly float _rushMultiplier;
        private readonly float _rushSeconds;
        private readonly float _crashMultiplier;
        private readonly float _crashSeconds;
        private readonly float _crashExtraPerChain;
        private readonly float _crashMaxSeconds;
        private readonly float _lowWellbeingRushFactor;
        private readonly float _lowWellbeingCrashFactor;

        private float _crashDuration;

        public SnackRush(
            float rushMultiplier,
            float rushSeconds,
            float crashMultiplier,
            float crashSeconds,
            float crashExtraPerChain,
            float crashMaxSeconds,
            float lowWellbeingRushFactor = 1f,
            float lowWellbeingCrashFactor = 1f
        )
        {
            _lowWellbeingRushFactor = Mathf.Clamp01(lowWellbeingRushFactor);
            _lowWellbeingCrashFactor = Mathf.Max(1f, lowWellbeingCrashFactor);
            _rushMultiplier = Mathf.Max(1f, rushMultiplier);
            _rushSeconds = Mathf.Max(0f, rushSeconds);
            _crashMultiplier = Mathf.Clamp(crashMultiplier, 0.05f, 1f);
            _crashSeconds = Mathf.Max(0f, crashSeconds);
            _crashExtraPerChain = Mathf.Max(0f, crashExtraPerChain);
            _crashMaxSeconds = Mathf.Max(_crashSeconds, crashMaxSeconds);
        }

        public SnackState State { get; private set; }

        /// <summary>Segundos que faltam no estado atual (0 sem efeito).</summary>
        public float Remaining { get; private set; }

        /// <summary>Duração da queda que vem depois deste pico.</summary>
        public float PendingCrashSeconds => State == SnackState.Rush ? _crashDuration : 0f;

        public float Multiplier =>
            State switch
            {
                SnackState.Rush => _rushMultiplier,
                SnackState.Crash => _crashMultiplier,
                _ => 1f,
            };

        public event Action<SnackState> StateChanged;

        /// <summary>
        /// Comeu um lanche rápido. <paramref name="wellbeing01"/> é o Bem-Estar de quem
        /// comeu, de 0 a 1: com ele baixo o pico encurta e a queda alonga, então o
        /// primeiro lanche rende e o décimo não.
        /// </summary>
        public void Eat(float wellbeing01 = 1f)
        {
            if (_rushSeconds <= 0f)
            {
                return;
            }

            var w = Mathf.Clamp01(wellbeing01);
            var crash = _crashSeconds * Mathf.Lerp(_lowWellbeingCrashFactor, 1f, w);
            _crashDuration =
                State == SnackState.Rush
                    ? Mathf.Max(_crashDuration, crash) + _crashExtraPerChain
                    : crash;
            _crashDuration = Mathf.Min(_crashDuration, _crashMaxSeconds);
            Remaining = _rushSeconds * Mathf.Lerp(_lowWellbeingRushFactor, 1f, w);
            Change(SnackState.Rush);
        }

        /// <summary>Comeu fruta ou água: encerra a queda, se houver. No pico não muda nada.</summary>
        public bool Cure()
        {
            if (State != SnackState.Crash)
            {
                return false;
            }

            Remaining = 0f;
            Change(SnackState.None);
            return true;
        }

        public void Clear()
        {
            Remaining = 0f;
            _crashDuration = 0f;
            Change(SnackState.None);
        }

        public void Tick(float deltaTime)
        {
            if (State == SnackState.None || deltaTime <= 0f)
            {
                return;
            }

            Remaining -= deltaTime;
            if (Remaining > 0f)
            {
                return;
            }

            if (State == SnackState.Rush && _crashDuration > 0f)
            {
                // o que sobrou do passo já conta na queda
                Remaining = _crashDuration + Remaining;
                Change(SnackState.Crash);
                if (Remaining <= 0f)
                {
                    Remaining = 0f;
                    Change(SnackState.None);
                }
            }
            else
            {
                Remaining = 0f;
                Change(SnackState.None);
            }
        }

        private void Change(SnackState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
