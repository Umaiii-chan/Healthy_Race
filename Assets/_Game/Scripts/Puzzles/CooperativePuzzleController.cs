using System;
using HealthyRace.Obstacles;
using UnityEngine;

namespace HealthyRace.Puzzles
{
    /// <summary>
    /// Orchestrates cooperative multi-element puzzles.
    /// Combines pressure buttons, levers, crates, doors, and moving platforms
    /// with strict anti-deadlock safeguards and impossible state recovery.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CooperativePuzzleController : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField]
        private PressureButton _button;

        [SerializeField]
        private Lever _lever;

        [SerializeField]
        private PushableBox _box;

        [Header("Recovery Switch (Anti-Deadlock)")]
        [SerializeField]
        private Lever _resetSwitch;

        [Header("Outputs")]
        [SerializeField]
        private PuzzleDoor _door;

        [SerializeField]
        private ActivatedPlatform _platform;

        [Header("Solving Logic")]
        [SerializeField]
        private bool _requireAll = true;

        public bool IsSolved { get; private set; }
        public bool RequireAll => _requireAll;
        public PressureButton Button => _button;
        public Lever Lever => _lever;
        public PushableBox Box => _box;
        public PuzzleDoor Door => _door;
        public ActivatedPlatform Platform => _platform;
        public Lever ResetSwitch => _resetSwitch;

        public event Action<bool> OnPuzzleStateChanged;
        public event Action OnPuzzleSolved;
        public event Action OnPuzzleReset;

        public void Bind(
            PressureButton button,
            Lever lever,
            PushableBox box,
            PuzzleDoor door,
            ActivatedPlatform platform,
            Lever resetSwitch = null,
            bool requireAll = true
        )
        {
            Unbind();

            _button = button;
            _lever = lever;
            _box = box;
            _door = door;
            _platform = platform;
            _resetSwitch = resetSwitch;
            _requireAll = requireAll;

            SubscribeEvents();
            SetSolved(false, force: true);
            EvaluateState();
        }

        private void Awake()
        {
            SubscribeEvents();
            SetSolved(false, force: true);
            EvaluateState();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void SubscribeEvents()
        {
            if (_button != null)
            {
                _button.OnPressedStateChanged += HandleButtonStateChanged;
            }

            if (_lever != null)
            {
                _lever.OnToggled += HandleLeverStateChanged;
            }

            if (_resetSwitch != null)
            {
                _resetSwitch.OnActivated += HandleResetSwitchActivated;
            }
        }

        public void Unbind()
        {
            if (_button != null)
            {
                _button.OnPressedStateChanged -= HandleButtonStateChanged;
            }

            if (_lever != null)
            {
                _lever.OnToggled -= HandleLeverStateChanged;
            }

            if (_resetSwitch != null)
            {
                _resetSwitch.OnActivated -= HandleResetSwitchActivated;
            }
        }

        private void HandleButtonStateChanged(bool isPressed)
        {
            EvaluateState();
        }

        private void HandleLeverStateChanged(bool isActivated)
        {
            EvaluateState();
        }

        private void HandleResetSwitchActivated()
        {
            ResetPuzzle();
        }

        public void EvaluateState()
        {
            var hasButton = _button != null;
            var hasLever = _lever != null;

            if (!hasButton && !hasLever)
            {
                SetSolved(false);
                return;
            }

            bool solved;
            if (hasButton && hasLever)
            {
                solved = _requireAll
                    ? (_button.IsPressed && _lever.IsActivated)
                    : (_button.IsPressed || _lever.IsActivated);
            }
            else if (hasButton)
            {
                solved = _button.IsPressed;
            }
            else
            {
                solved = _lever.IsActivated;
            }

            SetSolved(solved);
        }

        private void SetSolved(bool solved, bool force = false)
        {
            if (solved != IsSolved || force)
            {
                IsSolved = solved;

                if (_door != null)
                {
                    _door.SetOpen(IsSolved);
                }

                if (_platform != null)
                {
                    _platform.SetState(IsSolved);
                }

                OnPuzzleStateChanged?.Invoke(IsSolved);
                if (IsSolved)
                {
                    OnPuzzleSolved?.Invoke();
                }
            }
        }

        /// <summary>
        /// Recovers the puzzle from an impossible state (e.g. crate pushed into a pit or wedged in a corner).
        /// Restores boxes to their initial spawn and resets activation switches.
        /// </summary>
        public void ResetPuzzle()
        {
            if (_box != null)
            {
                _box.ResetToSpawn();
            }

            if (_lever != null)
            {
                _lever.ResetState();
            }

            if (_resetSwitch != null)
            {
                _resetSwitch.ResetState();
            }

            EvaluateState();
            OnPuzzleReset?.Invoke();
        }
    }
}
