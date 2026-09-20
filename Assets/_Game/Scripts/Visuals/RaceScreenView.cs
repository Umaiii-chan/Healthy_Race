using System;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Visuals
{
    /// <summary>Displays supplied snapshots and emits intent. The match flow remains external.</summary>
    public sealed class RaceScreenView : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _screens;

        [SerializeField]
        private Text _clock;

        [SerializeField]
        private Text _window;

        [SerializeField]
        private Image _levelProgress;

        [SerializeField]
        private Text _winner;

        [SerializeField]
        private Text[] _results;

        [SerializeField]
        private PlayerHudView[] _players;

        [SerializeField]
        private Button[] _buttons;

        [SerializeField]
        private VisualCommand[] _commands;

        [SerializeField]
        private Slider _music;

        [SerializeField]
        private Slider _effects;

        [SerializeField]
        private Toggle _reduceFlashes;
        public event Action<VisualCommand> CommandRequested;
        public event Action<float, float> AudioRequested;
        public event Action<bool> ReduceFlashesRequested;
        public VisualScreen Screen { get; private set; }
        public PlayerHudView[] Players => _players;

        public void Configure(
            GameObject[] screens,
            Text clock,
            Text window,
            Image levelProgress,
            Text winner,
            Text[] results,
            PlayerHudView[] players,
            Button[] buttons,
            VisualCommand[] commands,
            Slider music,
            Slider effects,
            Toggle reduceFlashes
        )
        {
            _screens = screens;
            _clock = clock;
            _window = window;
            _levelProgress = levelProgress;
            _winner = winner;
            _results = results;
            _players = players;
            _buttons = buttons;
            _commands = commands;
            _music = music;
            _effects = effects;
            _reduceFlashes = reduceFlashes;
        }

        private void Awake()
        {
            for (var i = 0; i < _buttons.Length; i++)
            {
                var command = _commands[i];
                _buttons[i].onClick.AddListener(() => Request(command));
            }
            _music.onValueChanged.AddListener(_ =>
                AudioRequested?.Invoke(_music.value, _effects.value)
            );
            _effects.onValueChanged.AddListener(_ =>
                AudioRequested?.Invoke(_music.value, _effects.value)
            );
            _reduceFlashes.onValueChanged.AddListener(value =>
                ReduceFlashesRequested?.Invoke(value)
            );
        }

        public void Request(VisualCommand command) => CommandRequested?.Invoke(command);

        public void Show(VisualScreen screen)
        {
            Screen = screen;
            for (var i = 0; i < _screens.Length; i++)
                _screens[i].SetActive(i == (int)screen);
        }

        public void RenderRace(float elapsed, float progress, float? finalWindowRemaining)
        {
            _clock.text = VisualUiFormat.Clock(elapsed);
            _levelProgress.fillAmount = float.IsNaN(progress) ? 0f : Mathf.Clamp01(progress);
            _window.gameObject.SetActive(finalWindowRemaining.HasValue);
            _window.text = finalWindowRemaining.HasValue
                ? VisualUiFormat.FinalWindow(finalWindowRemaining.Value)
                : string.Empty;
        }

        public void RenderResults(string winner, PlayerResultData p1, PlayerResultData p2)
        {
            _winner.text = winner;
            _results[0].text = VisualUiFormat.Result(p1);
            _results[1].text = VisualUiFormat.Result(p2);
        }
    }
}
