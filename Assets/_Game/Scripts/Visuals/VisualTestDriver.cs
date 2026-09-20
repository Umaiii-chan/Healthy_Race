using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Visuals
{
    /// <summary>Owned gallery only. Demo snapshots never alter gameplay components.</summary>
    public sealed class VisualTestDriver : MonoBehaviour
    {
        [SerializeField]
        private VisualCatalog _catalog;

        [SerializeField]
        private RaceScreenView _screen;

        [SerializeField]
        private PlayerPresentation[] _players;

        [SerializeField]
        private FoodPresentation[] _foods;

        [SerializeField]
        private PuzzlePresentation[] _puzzles;

        [SerializeField]
        private HazardPresentation _hazard;
        private int _animation;
        private int _effect;
        private int _puzzle;
        private bool _collected;
        private bool _reduced;
        private VisualScreen _returnScreen = VisualScreen.MainMenu;
        private Text _animationLabel;
        private Text _effectLabel;

        public void Configure(
            VisualCatalog catalog,
            RaceScreenView screen,
            PlayerPresentation[] players,
            FoodPresentation[] foods,
            PuzzlePresentation[] puzzles,
            HazardPresentation hazard
        )
        {
            _catalog = catalog;
            _screen = screen;
            _players = players;
            _foods = foods;
            _puzzles = puzzles;
            _hazard = hazard;
        }

        private void OnEnable()
        {
            if (_screen == null)
                return;
            _screen.CommandRequested += HandleCommand;
            _screen.ReduceFlashesRequested += ReduceFlashes;
        }

        private void OnDisable()
        {
            if (_screen == null)
                return;
            _screen.CommandRequested -= HandleCommand;
            _screen.ReduceFlashesRequested -= ReduceFlashes;
        }

        private void Start()
        {
            ApplyDemoData(_screen);
            CreateToolbar();
        }

        public static void ApplyDemoData(RaceScreenView screen)
        {
            var p1 = new PlayerHudData
            {
                Player = 1,
                Wellbeing = 88,
                Score = 2140,
                HealthyFoods = 7,
                UltraProcessedFoods = 1,
                FatigueEpisodes = 0,
                Progress = 0.76f,
            };
            var p2 = new PlayerHudData
            {
                Player = 2,
                Wellbeing = 35,
                Score = 1055,
                HealthyFoods = 4,
                UltraProcessedFoods = 3,
                FatigueEpisodes = 1,
                Progress = 0.92f,
            };
            screen.Players[0].Render(p1);
            screen.Players[1].Render(p2);
            screen.RenderRace(163, 0.84f, 20);
            screen.RenderResults(
                "P1 VENCEU!",
                new PlayerResultData
                {
                    Player = p1,
                    Seconds = 179,
                    Finished = true,
                    HealthyPoints = 800,
                    PuzzlePoints = 300,
                    TimeBonus = 600,
                    ArrivalBonus = 100,
                    WellbeingBonus = 440,
                    UltraPenalty = 100,
                },
                new PlayerResultData
                {
                    Player = p2,
                    Seconds = 163,
                    Finished = true,
                    HealthyPoints = 430,
                    PuzzlePoints = 150,
                    TimeBonus = 600,
                    ArrivalBonus = 200,
                    WellbeingBonus = 175,
                    UltraPenalty = 300,
                    FatiguePenalty = 200,
                }
            );
        }

        private void HandleCommand(VisualCommand command)
        {
            switch (command)
            {
                case VisualCommand.Controls:
                    _returnScreen = _screen.Screen;
                    _screen.Show(VisualScreen.Controls);
                    break;
                case VisualCommand.Audio:
                    _returnScreen = _screen.Screen;
                    _screen.Show(VisualScreen.Audio);
                    break;
                case VisualCommand.Back:
                    _screen.Show(_returnScreen);
                    break;
                case VisualCommand.Menu:
                    _screen.Show(VisualScreen.MainMenu);
                    break;
                case VisualCommand.Quit:
                    _screen.Show(VisualScreen.Boot);
                    break;
                default:
                    _screen.Show(VisualScreen.Gameplay);
                    break;
            }
        }

        private void ReduceFlashes(bool reduced)
        {
            _reduced = reduced;
            foreach (var player in _players)
                player.ReduceFlashes = reduced;
            foreach (var food in _foods)
                food.ReduceFlashes = reduced;
            foreach (var puzzle in _puzzles)
                puzzle.ReduceFlashes = reduced;
            _hazard.ReduceFlashes = reduced;
        }

        private void CreateToolbar()
        {
            var go = new GameObject(
                "Gallery controls",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            go.transform.SetParent(transform, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.GetComponent<Canvas>().sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            AddButton(go.transform, "Menu", 20, () => _screen.Show(VisualScreen.MainMenu));
            AddButton(go.transform, "Galeria", 240, () => _screen.Show(VisualScreen.Gameplay));
            AddButton(go.transform, "Pausa", 460, () => _screen.Show(VisualScreen.Pause));
            AddButton(go.transform, "Resultados", 680, () => _screen.Show(VisualScreen.Results));
            _animationLabel = AddButton(
                go.transform,
                "Anim: Idle",
                900,
                () =>
                {
                    _animation = (_animation + 1) % 12;
                    foreach (var player in _players)
                        player.Play((VisualAction)_animation);
                    _animationLabel.text = "Anim: " + (VisualAction)_animation;
                }
            );
            AddButton(
                go.transform,
                "Coleta / reset",
                1120,
                () =>
                {
                    foreach (var food in _foods)
                    {
                        if (_collected)
                            food.ResetPresentation();
                        else
                            food.Pickup();
                    }
                    _collected = !_collected;
                }
            );
            AddButton(
                go.transform,
                "Puzzle / perigo",
                1340,
                () =>
                {
                    _puzzle = (_puzzle + 1) % 4;
                    foreach (var puzzle in _puzzles)
                        puzzle.Present((PuzzleAppearance)_puzzle);
                    _hazard.PresentWarning(_puzzle / 3f);
                }
            );
            _effectLabel = AddButton(
                go.transform,
                "VFX: coleta",
                1560,
                () =>
                {
                    VisualFeedback.Emit(
                        _catalog,
                        (FeedbackKind)_effect,
                        new Vector3(10.5f, 3.25f, 0),
                        _reduced
                    );
                    _effectLabel.text = "VFX " + (_effect + 1) + "/11";
                    _effect = (_effect + 1) % 11;
                }
            );
        }

        private static Text AddButton(
            Transform parent,
            string label,
            float x,
            UnityEngine.Events.UnityAction action
        )
        {
            var root = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -228);
            rect.sizeDelta = new Vector2(204, 44);
            root.GetComponent<Image>().color = new Color32(30, 53, 44, 255);
            root.GetComponent<Button>().onClick.AddListener(action);
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 21;
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }
}
