using System;
using UnityEngine;

namespace HealthyRace.WellBeing
{
    /// <summary>
    /// Stores a single player's wellbeing. It deliberately contains no UI, score, or
    /// match-wide state so each player remains independent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WellbeingComponent : MonoBehaviour
    {
        public const int MinimumValue = 0;
        public const int MaximumValue = 100;

        [SerializeField, Range(MinimumValue, MaximumValue)]
        private int _initialValue = MaximumValue;

        [SerializeField]
        private WellbeingBandData[] _bands;

        private WellbeingBandData _currentBand;
        private int _currentValue = MaximumValue;

        public event Action<WellbeingBandData> WellbeingBandChanged;

        public event Action<int, int> WellbeingChanged;

        public event Action<int> ValueChanged;

        public event Action<float, float> MultipliersChanged;

        public WellbeingBandData CurrentBand => _currentBand;

        public int CurrentValue => _currentValue;

        public float JumpMultiplier => _currentBand?.JumpMultiplier ?? 1f;

        public float MovementMultiplier => _currentBand?.MovementMultiplier ?? 1f;

        public bool IsAtMinimum => _currentValue <= MinimumValue;

        public bool IsAtMaximum => _currentValue >= MaximumValue;

        public float NormalizedValue => Mathf.Clamp01((float)_currentValue / MaximumValue);

        public WellbeingBandData[] Bands => _bands;

        private void Awake()
        {
            _currentValue = Mathf.Clamp(_initialValue, MinimumValue, MaximumValue);
            _currentBand = ResolveBand(_currentValue);
        }

        public void ConfigureBands(WellbeingBandData[] bands)
        {
            _bands = bands ?? Array.Empty<WellbeingBandData>();
            UpdateCurrentBand(true);
        }

        public void Initialize(int initialValue, WellbeingBandData[] bands = null)
        {
            if (bands != null)
            {
                _bands = bands;
            }

            _initialValue = Mathf.Clamp(initialValue, MinimumValue, MaximumValue);
            _currentValue = _initialValue;
            UpdateCurrentBand(true);
        }

        public void ApplyDelta(int delta)
        {
            SetValue(_currentValue + delta);
        }

        public void SetValue(int value)
        {
            var clampedValue = Mathf.Clamp(value, MinimumValue, MaximumValue);

            if (clampedValue == _currentValue && _currentBand != null)
            {
                return;
            }

            var previousValue = _currentValue;
            _currentValue = clampedValue;
            ValueChanged?.Invoke(_currentValue);
            WellbeingChanged?.Invoke(previousValue, _currentValue);
            UpdateCurrentBand(true);
        }

        public WellbeingBandData ResolveBand(int wellbeingValue)
        {
            if (_bands == null)
            {
                return null;
            }

            foreach (var band in _bands)
            {
                if (band != null && band.Contains(wellbeingValue))
                {
                    return band;
                }
            }

            return null;
        }

        private void UpdateCurrentBand(bool notify)
        {
            var nextBand = ResolveBand(_currentValue);

            if (nextBand == _currentBand)
            {
                return;
            }

            _currentBand = nextBand;

            if (notify)
            {
                WellbeingBandChanged?.Invoke(_currentBand);
                MultipliersChanged?.Invoke(MovementMultiplier, JumpMultiplier);
            }
        }
    }
}
