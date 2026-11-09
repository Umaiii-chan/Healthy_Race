using HealthyRace.Audio;
using HealthyRace.Core;
using HealthyRace.Visuals;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HealthyRace.UI
{
    /// <summary>
    /// Liga as telas de <see cref="RaceScreenView"/> (menu, pausa, resultados,
    /// controles, áudio) ao fluxo real do jogo. A view só desenha e emite intenção;
    /// aqui a intenção vira navegação, pausa e ajuste de volume.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameScreens : MonoBehaviour
    {
        public const string PrefReduceFlashes = "HealthyRace_ReduceFlashes";

        private const int CanvasSortingOrder = 50;

        private bool _inMatch;
        private bool _matchConcluded;
        private InputAction _pauseAction;
        private VisualScreen _returnScreen;
        private InputActionAsset _runtimeActions;
        private RaceScreenView _view;

        public RaceScreenView View => _view;

        public bool IsMatchConcluded => _matchConcluded;

        /// <summary>
        /// Instancia as telas. <paramref name="inMatch"/> começa com tudo escondido e
        /// habilita a pausa; fora de partida abre no menu principal.
        /// </summary>
        public static GameScreens Create(
            GameObject screensPrefab,
            InputActionAsset inputActions,
            bool inMatch,
            Transform parent = null
        )
        {
            if (screensPrefab == null)
            {
                return null;
            }

            var host = new GameObject("Game Screens");
            host.transform.SetParent(parent, false);
            var screens = host.AddComponent<GameScreens>();
            screens.Initialize(screensPrefab, inputActions, inMatch);
            return screens;
        }

        /// <summary>Sem EventSystem nenhum botão de UI recebe clique.</summary>
        public static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule)
            );
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public void Pause()
        {
            if (!_inMatch || _matchConcluded || GameFlow.IsPaused)
            {
                return;
            }

            GameFlow.SetPaused(true);
            ShowScreen(VisualScreen.Pause);
        }

        public void Resume()
        {
            if (!GameFlow.IsPaused)
            {
                return;
            }

            GameFlow.SetPaused(false);
            _view.Hide();
        }

        public void ShowResults(string winner, PlayerResultData p1, PlayerResultData p2)
        {
            _matchConcluded = true;
            _view.RenderResults(winner, p1, p2);
            _view.SetCommandVisible(VisualCommand.Next, GameFlow.HasNextLevel);
            ShowScreen(VisualScreen.Results);
        }

        private void Initialize(
            GameObject screensPrefab,
            InputActionAsset inputActions,
            bool inMatch
        )
        {
            _inMatch = inMatch;
            EnsureEventSystem();

            var instance = Instantiate(screensPrefab, transform);
            instance.name = "Screens";
            _view = instance.GetComponent<RaceScreenView>();
            var canvas = instance.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = CanvasSortingOrder;
            }

            _view.CommandRequested += HandleCommand;
            _view.AudioRequested += HandleAudio;
            _view.ReduceFlashesRequested += HandleReduceFlashes;

            var audio = AudioService.Instance;
            _view.SetAudio(audio.MusicVolume, audio.SfxVolume, ReduceFlashesPreference());

            if (_inMatch)
            {
                _view.Hide();
                BindPause(inputActions);
            }
            else
            {
                ShowScreen(VisualScreen.MainMenu);
            }
        }

        private void Start()
        {
            // No Start a fase já montou jogadores, alimentos e mecanismos.
            ApplyReduceFlashes(ReduceFlashesPreference());
        }

        private void OnDestroy()
        {
            if (_pauseAction != null)
            {
                _pauseAction.performed -= HandlePausePerformed;
                _pauseAction.Disable();
            }

            if (_runtimeActions != null)
            {
                Destroy(_runtimeActions);
            }

            if (_view != null)
            {
                _view.CommandRequested -= HandleCommand;
                _view.AudioRequested -= HandleAudio;
                _view.ReduceFlashesRequested -= HandleReduceFlashes;
            }
        }

        private void BindPause(InputActionAsset inputActions)
        {
            if (inputActions == null)
            {
                Debug.LogError(
                    "GameScreens: sem Input Actions não há ação UI/Pause; a pausa fica indisponível.",
                    this
                );
                return;
            }

            // Cópia própria, como o PlayerInputReader: habilitar o mapa UI não deve
            // tocar nos mapas dos jogadores.
            _runtimeActions = Instantiate(inputActions);
            _pauseAction = _runtimeActions.FindActionMap("UI", false)?.FindAction("Pause", false);
            if (_pauseAction == null)
            {
                Debug.LogError("GameScreens: ação UI/Pause não encontrada.", this);
                return;
            }

            _pauseAction.performed += HandlePausePerformed;
            _pauseAction.Enable();
        }

        private void HandlePausePerformed(InputAction.CallbackContext context)
        {
            if (_matchConcluded)
            {
                return;
            }

            if (!GameFlow.IsPaused)
            {
                Pause();
            }
            else if (_view.Screen == VisualScreen.Pause)
            {
                Resume();
            }
            else
            {
                // Esc dentro de Controles/Áudio volta para a pausa.
                ShowScreen(_returnScreen);
            }
        }

        private void HandleCommand(VisualCommand command)
        {
            AudioService.Instance.PlaySfx(AudioClipId.Sfx_Button);

            switch (command)
            {
                case VisualCommand.Play:
                    GameFlow.LoadLevel(1);
                    break;
                case VisualCommand.Continue:
                    Resume();
                    break;
                case VisualCommand.Restart:
                    GameFlow.RestartLevel();
                    break;
                case VisualCommand.Next:
                    GameFlow.LoadNextLevel();
                    break;
                case VisualCommand.Menu:
                    GameFlow.LoadMenu();
                    break;
                case VisualCommand.Quit:
                    GameFlow.Quit();
                    break;
                case VisualCommand.Controls:
                    _returnScreen = _view.Screen;
                    ShowScreen(VisualScreen.Controls);
                    break;
                case VisualCommand.Audio:
                    _returnScreen = _view.Screen;
                    ShowScreen(VisualScreen.Audio);
                    break;
                case VisualCommand.Back:
                    ShowScreen(_returnScreen);
                    break;
            }
        }

        private static void HandleAudio(float music, float effects)
        {
            AudioService.Instance.SetVolumes(music, effects);
        }

        private static void HandleReduceFlashes(bool reduce)
        {
            PlayerPrefs.SetInt(PrefReduceFlashes, reduce ? 1 : 0);
            PlayerPrefs.Save();
            ApplyReduceFlashes(reduce);
        }

        private static bool ReduceFlashesPreference()
        {
            return PlayerPrefs.GetInt(PrefReduceFlashes, 0) == 1;
        }

        private static void ApplyReduceFlashes(bool reduce)
        {
            UiFloat.ReduceMotion = reduce;
            ParallaxLayer.ReduceMotion = reduce;
            MudBubbles.ReduceMotion = reduce;
            foreach (var p in FindObjectsByType<PlayerPresentation>(FindObjectsSortMode.None))
            {
                p.ReduceFlashes = reduce;
            }

            foreach (var f in FindObjectsByType<FoodPresentation>(FindObjectsSortMode.None))
            {
                f.ReduceFlashes = reduce;
            }

            foreach (var z in FindObjectsByType<PuzzlePresentation>(FindObjectsSortMode.None))
            {
                z.ReduceFlashes = reduce;
            }

            foreach (var h in FindObjectsByType<HazardPresentation>(FindObjectsSortMode.None))
            {
                h.ReduceFlashes = reduce;
            }
        }

        private void ShowScreen(VisualScreen screen)
        {
            _view.Show(screen);

            // Seleção inicial: o menu inteiro é navegável só no teclado.
            var root = _view.ScreenRoot(screen);
            var first = root != null ? root.GetComponentInChildren<Button>() : null;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : null);
            }
        }
    }
}
