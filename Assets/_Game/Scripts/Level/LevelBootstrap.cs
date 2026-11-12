using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthyRace.Audio;
using HealthyRace.Collectibles;
using HealthyRace.Core;
using HealthyRace.Obstacles;
using HealthyRace.Player;
using HealthyRace.Puzzles;
using HealthyRace.UI;
using HealthyRace.Visuals;
using HealthyRace.WellBeing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HealthyRace.Level
{
    /// <summary>
    /// Monta uma fase de corrida a partir do mapa em texto (<see cref="LevelGrid"/>):
    /// terreno com colisor único, perigos, elevadores, alavancas, alimentos,
    /// checkpoints, portas, placas e enfeites; depois os jogadores, a câmera
    /// compartilhada, o HUD e a partida. Cada jogador termina sozinho na própria porta.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelBootstrap : MonoBehaviour
    {
        public const int GroundLayer = 6;

        // Física da fase e intenção de projeto (ver JumpBudget).
        public const float PhaseGravityY = -18f;
        public const float TargetJumpApex = 2.0f;
        public const float FixedDeltaTime = 0.02f;

        /// <summary>Prazo do segundo jogador depois que o primeiro chega à porta.</summary>
        public static float PartnerWindowSeconds => BalanceConfig.Current.PartnerWindowSeconds;

        /// <summary>Duração máxima de uma partida.</summary>
        public static float MatchTimeLimitSeconds => BalanceConfig.Current.MatchTimeLimitSeconds;

        public const float LiftSpeed = 5f;
        public const float GateOpenDelay = 1.2f;

        /// <summary>Altura da faixa do HUD, em pixels de referência (1080p).</summary>
        public const float HudBandPixels = GreenhouseHud.BandPixels;

        /// <summary>A câmera mostra no mínimo esta altura de fase (uma tela).</summary>
        public const float MinimumViewHeight = 18f;

        public const float SignCharacterSize = 0.075f;

        private const float PlayerHalfHeight = 0.625f;
        private const float PlankThickness = 0.5f;
        private const float LiftThickness = 0.3f;

        [Header("Fase")]
        [SerializeField]
        private TextAsset _map;

        [Header("Prefabs")]
        [SerializeField]
        private GameObject _playerPrefab;

        [SerializeField]
        private GameObject _collectiblePrefab;

        [SerializeField]
        private FoodData[] _foodCatalog;

        [Header("Visual")]
        [SerializeField]
        private VisualCatalog _visualCatalog;

        [SerializeField]
        private GameObject _playerOneVisualPrefab;

        [SerializeField]
        private GameObject _playerTwoVisualPrefab;

        [Header("Fluxo")]
        [SerializeField]
        private GameObject _screensPrefab;

        [SerializeField]
        private InputActionAsset _inputActions;

        private readonly List<Checkpoint> _checkpoints = new();
        private readonly List<SpikeHazard> _spikes = new();
        private readonly List<MudHazard> _mudHazards = new();
        private readonly List<FallingPlatform> _fallingPlatforms = new();
        private readonly List<ActivatedPlatform> _lifts = new();
        private readonly List<(Lever Lever, PuzzleDoor Gate)> _gates = new();
        private readonly List<FoodCollectible> _foods = new();
        private readonly List<GameObject> _debugLabels = new();
        private readonly List<FoodData> _healthyFoods = new();
        private readonly List<FoodData> _fastFoods = new();
        private readonly float[] _bestHeight = new float[3];

        // A arte da estufa é neutra (cinza claro) e ganha a cor do [tema] da fase. Sem
        // tema, estas cores reproduzem a Fase 1 (ver build_greenhouse.py).
        public static readonly Color DefaultBrickTint = new Color32(69, 105, 79, 255);
        public static readonly Color DefaultWallTint = new Color32(226, 240, 222, 255);
        public static readonly Color DefaultFoliageTint = new Color32(124, 180, 124, 255);

        public Color BrickTint => _grid.ThemeColor("tijolo", DefaultBrickTint);

        /// <summary>Faixa do topo dos blocos: o tijolo clareado.</summary>
        public Color BrickTopTint => Color.Lerp(BrickTint, Color.white, 0.3f);

        public Color WallTint => _grid.ThemeColor("parede", DefaultWallTint);

        public Color FoliageTint => _grid.ThemeColor("folhagem", DefaultFoliageTint);

        /// <summary>Quanto a folhagem de fundo acompanha a câmera (0 = presa à fase).</summary>
        public const float FoliageFollowFactor = 0.35f;

        private ParallaxLayer _foliage;

        private LevelGrid _grid;
        private Transform _levelParent;
        private Vector2 _spawnOne;
        private Vector2 _spawnTwo;
        private float _finishHeight;
        private bool _debugLabelsVisible;
        private GameObject _playerOne;
        private GameObject _playerTwo;
        private FinishLine _playerOneDoor;
        private FinishLine _playerTwoDoor;
        private PlayerHUD _playerOneHud;
        private PlayerHUD _playerTwoHud;
        private MatchHUD _matchHud;
        private GreenhouseHud _greenhouseHud;
        private RaceResultBoard _resultBoard;
        private readonly Scoring.RaceTrace _trace = new();
        private SharedCameraController _cameraController;
        private MatchCompletionController _matchController;
        private MatchRecorder _recorder;
        private RaceCountdown _countdown;
        private MatchResultsPanel _resultsPanel;
        private GameScreens _screens;

        public LevelGrid Grid => _grid;
        public GameObject PlayerOne => _playerOne;
        public GameObject PlayerTwo => _playerTwo;
        public FinishLine PlayerOneDoor => _playerOneDoor;
        public FinishLine PlayerTwoDoor => _playerTwoDoor;
        public PlayerHUD PlayerOneHud => _playerOneHud;
        public PlayerHUD PlayerTwoHud => _playerTwoHud;
        public MatchHUD MatchHud => _matchHud;

        /// <summary>HUD da estufa (placas, folhas, relógio e trepadeira); nulo sem as peças.</summary>
        public GreenhouseHud GreenhouseHud => _greenhouseHud;

        /// <summary>Liderança registrada ao longo da corrida.</summary>
        public Scoring.RaceTrace Trace => _trace;

        /// <summary>Quadro da tela de resultado; nulo até a corrida acabar.</summary>
        public RaceResultBoard ResultBoard => _resultBoard;

        private static bool IsRushing(GameObject player)
        {
            var movement = player != null ? player.GetComponent<PlayerMovement>() : null;
            return movement != null && movement.IsRushing;
        }

        public SharedCameraController CameraController => _cameraController;
        public MatchCompletionController MatchController => _matchController;
        public RaceCountdown Countdown => _countdown;
        public MatchResultsPanel ResultsPanel => _resultsPanel;
        public GameScreens Screens => _screens;
        public VisualCatalog VisualCatalog => _visualCatalog;
        public GameObject PlayerOneVisualPrefab => _playerOneVisualPrefab;
        public GameObject PlayerTwoVisualPrefab => _playerTwoVisualPrefab;
        public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints;
        public IReadOnlyList<SpikeHazard> Spikes => _spikes;
        public IReadOnlyList<MudHazard> MudHazards => _mudHazards;
        public IReadOnlyList<FallingPlatform> FallingPlatforms => _fallingPlatforms;
        public IReadOnlyList<ActivatedPlatform> Lifts => _lifts;
        public IReadOnlyList<(Lever Lever, PuzzleDoor Gate)> Gates => _gates;
        public IReadOnlyList<FoodCollectible> Foods => _foods;
        public Vector2 SpawnOne => _spawnOne;
        public Vector2 SpawnTwo => _spawnTwo;
        public bool DebugLabelsVisible => _debugLabelsVisible;
        public int DebugLabelCount => _debugLabels.Count;

        /// <summary>Orçamento de pulo de projeto, sem jogador instanciado.</summary>
        public static JumpBudget DesignBudget(float jumpVelocity = 12f, float moveSpeed = 7f)
        {
            var gravityScale = JumpBudget.GravityScaleForApex(
                PhaseGravityY,
                jumpVelocity,
                TargetJumpApex,
                FixedDeltaTime
            );
            return JumpBudget.FromPhysics(
                PhaseGravityY,
                gravityScale,
                jumpVelocity,
                moveSpeed,
                FixedDeltaTime
            );
        }

        private void Awake()
        {
            if (_map == null || _playerPrefab == null || _collectiblePrefab == null)
            {
                Debug.LogError("LevelBootstrap: mapa ou prefabs não atribuídos.", this);
                return;
            }

            _grid = LevelGrid.Parse(_map.text);
            Application.targetFrameRate = 60;
            Physics2D.gravity = new Vector2(0f, PhaseGravityY);
            _levelParent = Group("Fase", transform);
            SplitFoodCatalog();
            // antes dos perigos: os espinhos usam a largada como renascimento padrão
            _spawnOne = SpawnPosition(LevelGrid.SpawnOne, new Vector2(2.5f, 1.7f));
            _spawnTwo = SpawnPosition(LevelGrid.SpawnTwo, _spawnOne + Vector2.right);

            CreateBackground();
            CreateTerrain();
            CreateCorridors();
            CreateHazards();
            CreateLifts();
            CreateLeverGates();
            if (!IsMenu)
            {
                CreateFoods();
                CreateCheckpoints();
                CreateDoorsAndSpawns();
            }

            CreateSigns();
            CreateDecor();

            _playerOne = CreatePlayer("P1", _spawnOne, 1);
            _playerTwo = CreatePlayer("P2", _spawnTwo, 2);
            _bestHeight[1] = _spawnOne.y;
            _bestHeight[2] = _spawnTwo.y;

            CreateCamera();
            if (IsMenu)
            {
                CreateMenuStage();
            }
            else
            {
                CreateHud();
            }

            CollectDebugLabels();
        }

        private void Start()
        {
            AudioService.Instance.PlayMusic(
                IsMenu ? AudioClipId.Music_Menu : AudioClipId.Music_Level01
            );
        }

        private void OnDestroy()
        {
            Checkpoint.ResetRegistry();
        }

        private void Update()
        {
            if (_grid == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                SetDebugLabelsVisible(!_debugLabelsVisible);
            }

            if (_matchHud != null && _matchController != null)
            {
                _matchHud.UpdateTimer(_matchController.Elapsed);
            }

            UpdateProgress(1, _playerOne, _playerOneHud, _playerOneDoor);
            UpdateProgress(2, _playerTwo, _playerTwoHud, _playerTwoDoor);
            if (_matchController != null)
            {
                // quem está na frente, para o gráfico da tela de resultado
                _trace.Record(
                    _matchController.Elapsed,
                    GetProgress(1),
                    GetProgress(2),
                    IsRushing(_playerOne),
                    IsRushing(_playerTwo)
                );
            }
        }

        // =====================================================================
        // Progresso: maior altura alcançada em relação à porta
        // =====================================================================
        private void UpdateProgress(int index, GameObject player, PlayerHUD hud, FinishLine door)
        {
            if (player == null || hud == null)
            {
                return;
            }

            _bestHeight[index] = Mathf.Max(_bestHeight[index], player.transform.position.y);
            hud.UpdateProgress(GetProgress(index));
        }

        /// <summary>0 na largada, 1 ao entrar na própria porta.</summary>
        public float GetProgress(int playerIndex)
        {
            if (playerIndex < 1 || playerIndex > 2)
            {
                return 0f;
            }

            var door = playerIndex == 1 ? _playerOneDoor : _playerTwoDoor;
            if (door != null && door.HasPlayerFinished(playerIndex))
            {
                return 1f;
            }

            var start = playerIndex == 1 ? _spawnOne.y : _spawnTwo.y;
            var span = Mathf.Max(0.01f, _finishHeight - start);
            // a porta só vale 100%: até lá o máximo é 95%
            return Mathf.Clamp((_bestHeight[playerIndex] - start) / span, 0f, 0.95f);
        }

        public void SetDebugLabelsVisible(bool visible)
        {
            _debugLabelsVisible = visible;
            foreach (var label in _debugLabels)
            {
                if (label != null)
                {
                    label.SetActive(visible);
                }
            }
        }

        private void CollectDebugLabels()
        {
            _debugLabels.Clear();
            foreach (var label in GetComponentsInChildren<DebugLabel>(true))
            {
                _debugLabels.Add(label.gameObject);
            }

            SetDebugLabelsVisible(false);
        }

        // =====================================================================
        // Fundo, terreno e corredores
        // =====================================================================
        private GreenhouseArt Art => _visualCatalog != null ? _visualCatalog.Greenhouse : default;

        private void CreateBackground()
        {
            var parent = Group("Fundo", _levelParent);
            var wall = Art.Wall;
            var renderer = SpriteObject(
                "ParedeDaEstufa",
                parent,
                wall,
                new Vector2(_grid.Width, _grid.Height) * 0.5f,
                -40
            );
            if (wall != null)
            {
                renderer.color = WallTint;
                var size = wall.bounds.size;
                renderer.transform.localScale = new Vector3(
                    _grid.Width / size.x,
                    _grid.Height / size.y,
                    1f
                );
            }
            else
            {
                renderer.color = new Color(0.86f, 0.93f, 0.86f);
                renderer.transform.localScale = new Vector3(_grid.Width, _grid.Height, 1f);
            }

            // Folhagem entre a parede e a fase, ladrilhada (nunca esticada). Anda com
            // a câmera mais devagar que a fase: é o que dá profundidade. A margem cobre
            // o quanto ela se desloca até a câmera chegar na borda.
            if (Art.Leaves != null)
            {
                var margin = new Vector2(_grid.Width, _grid.Height) * FoliageFollowFactor;
                var foliage = TiledObject(
                    "Folhagem",
                    parent,
                    Art.Leaves,
                    new Vector2(_grid.Width, _grid.Height) * 0.5f,
                    new Vector2(_grid.Width, _grid.Height) + margin,
                    -35,
                    Color.clear
                );
                foliage.color = FoliageTint;
                _foliage = foliage.gameObject.AddComponent<ParallaxLayer>();
            }

            CreateGreenhouseDepth(parent);
        }

        /// <summary>Peças do Cenário 3; nulo sem o kit, e aí vale a arte anterior.</summary>
        private static SceneryKit Scenery
        {
            get
            {
                var kit = SceneryKit.Load();
                return kit != null && kit.IsComplete ? kit : null;
            }
        }

        /// <summary>
        /// Profundidade da estufa (Cenário 3): um marco em silhueta por fase, uma faixa de
        /// luz pintada que aponta para a saída e vasos pendurados no teto. Tudo no valor do
        /// fundo, para não competir com o que se joga.
        /// </summary>
        private void CreateGreenhouseDepth(Transform parent)
        {
            var kit = Scenery;
            if (kit == null)
            {
                return;
            }

            var size = new Vector2(_grid.Width, _grid.Height);
            var landmark = kit.LandmarkFor(_grid.LevelNumber);
            if (landmark != null)
            {
                var mark = SpriteObject(
                    "Marco",
                    parent,
                    landmark,
                    new Vector2(size.x * 0.6f, size.y * 0.48f),
                    -39
                );
                var scale = size.y * 0.66f / landmark.bounds.size.y;
                mark.transform.localScale = new Vector3(scale, scale, 1f);
                mark.color = new Color(0.56f, 0.74f, 0.66f, 0.5f);
            }

            var exit = _grid.Cells(LevelGrid.DoorOne).Concat(_grid.Cells(LevelGrid.DoorTwo));
            if (exit.Any())
            {
                // a faixa desce do alto, do lado de dentro da fase, até a porta
                var door = exit.First();
                var fromRight = door.x < size.x * 0.5f;
                var beam = SpriteObject(
                    "LuzPintada",
                    parent,
                    kit.Light,
                    new Vector2(
                        door.x + 0.5f + (fromRight ? 1f : -1f) * size.y * 0.14f,
                        size.y * 0.5f
                    ),
                    -38
                );
                Fit(beam, new Vector2(size.y * 0.5f, size.y));
                beam.flipX = !fromRight;
                beam.color = new Color(1f, 1f, 1f, 0.26f);
            }

            for (var x = 7; x < _grid.Width - 6; x += 11)
            {
                var top = _grid.Height - 1;
                while (top > 0 && _grid[x, top] == LevelGrid.Solid)
                {
                    top--;
                }

                // só onde há vão livre embaixo do teto para o vaso pender
                if (_grid[x, top] != LevelGrid.Empty || _grid[x, top - 1] != LevelGrid.Empty)
                {
                    continue;
                }

                var pot = SpriteObject(
                    "VasoPendurado",
                    parent,
                    kit.HangingPot,
                    new Vector2(x + 0.5f, top + 1f - kit.HangingPot.bounds.size.y * 0.45f),
                    -34
                );
                pot.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            }
        }

        /// <summary>
        /// Placa sem texto (Cenário 3): na bifurcação, seta e alimento de cada caminho; na
        /// alavanca, o desenho do puxão. Devolve falso para qualquer outro texto.
        /// </summary>
        private bool TryCreatePictureSign(Transform parent, Vector2 basePosition, string text)
        {
            var kit = Scenery;
            if (kit == null)
            {
                return false;
            }

            Sprite art;
            if (text.Contains("CURTO"))
            {
                art = text.Contains("←") ? kit.ForkLeft : kit.ForkRight;
            }
            else if (text.Contains("ALAVANCA"))
            {
                art = kit.LeverSign;
            }
            else
            {
                return false;
            }

            // a base fica onde ficava a da placa de texto
            SpriteObject(
                "Placa",
                parent,
                art,
                basePosition + Vector2.up * art.bounds.size.y * 0.5f,
                -12
            );
            return true;
        }

        /// <summary>Cabo tracejado amarelo da alavanca ao portão: liga a causa ao efeito.</summary>
        private void CreateCable(Transform parent, Vector2 from, Vector2 to)
        {
            if (Scenery == null)
            {
                return;
            }

            var delta = to - from;
            var length = delta.magnitude;
            if (length < 0.6f)
            {
                return;
            }

            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            for (var distance = 0.5f; distance < length - 0.25f; distance += 0.34f)
            {
                var dash = SpriteObject(
                    "Cabo",
                    parent,
                    PlaceholderSpriteFactory.Square,
                    from + delta / length * distance,
                    -32
                );
                dash.color = new Color32(0xFF, 0xC8, 0x3D, 255);
                dash.transform.localScale = new Vector3(0.18f, 0.055f, 1f);
                dash.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void CreateTerrain()
        {
            var terrain = new GameObject("Terreno") { layer = GroundLayer };
            terrain.transform.SetParent(_levelParent, false);
            var body = terrain.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = terrain.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            composite.generationType = CompositeCollider2D.GenerationType.Manual;

            foreach (var run in _grid.Runs(LevelGrid.Solid))
            {
                AddBox(terrain, new Rect(run.X0, run.Y, run.Width, 1f));
                var brick = TiledObject(
                    "Bloco",
                    terrain.transform,
                    Art.Brick,
                    new Vector2(run.X0 + run.Width * 0.5f, run.Y + 0.5f),
                    new Vector2(run.Width, 1f),
                    -20,
                    new Color(0.23f, 0.35f, 0.26f)
                );
                if (Art.Brick != null)
                {
                    brick.color = BrickTint;
                }

                brick.name = $"Bloco {run.X0}-{run.X1},{run.Y}";
            }

            // faixa clara no topo dos blocos expostos (não sob o corredor curto)
            foreach (var run in _grid.Runs(c => c == LevelGrid.Solid))
            {
                var x = run.X0;
                while (x <= run.X1)
                {
                    if (!IsExposedTop(x, run.Y))
                    {
                        x++;
                        continue;
                    }

                    var start = x;
                    while (x <= run.X1 && IsExposedTop(x, run.Y))
                    {
                        x++;
                    }

                    var width = x - start;
                    var corridor = LooksLikeCorridor(start, run.Y + 1);
                    var top = TiledObject(
                        "Topo",
                        terrain.transform,
                        corridor ? Art.FastFloor : Art.BrickTop,
                        new Vector2(start + width * 0.5f, run.Y + (corridor ? 0.875f : 0.905f)),
                        new Vector2(width, corridor ? 0.25f : 0.19f),
                        -19,
                        new Color(0.38f, 0.53f, 0.4f)
                    );
                    if (!corridor && Art.BrickTop != null)
                    {
                        top.color = BrickTopTint;
                    }
                }
            }

            foreach (
                var cell in _grid.Cells(LevelGrid.RampUp).Concat(_grid.Cells(LevelGrid.RampDown))
            )
            {
                var up = _grid[cell.x, cell.y] == LevelGrid.RampUp;
                var polygon = terrain.AddComponent<PolygonCollider2D>();
                polygon.compositeOperation = Collider2D.CompositeOperation.Merge;
                polygon.points = up
                    ? new[]
                    {
                        new Vector2(cell.x, cell.y),
                        new Vector2(cell.x + 1, cell.y),
                        new Vector2(cell.x + 1, cell.y + 1),
                    }
                    : new[]
                    {
                        new Vector2(cell.x, cell.y),
                        new Vector2(cell.x + 1, cell.y),
                        new Vector2(cell.x, cell.y + 1),
                    };
                var renderer = SpriteObject(
                    "Rampa",
                    terrain.transform,
                    Art.Ramp,
                    new Vector2(cell.x + 0.5f, cell.y + 0.5f),
                    -20
                );
                Fit(renderer, Vector2.one);
                renderer.flipX = !up;
                if (Art.Ramp != null)
                {
                    renderer.color = BrickTint;
                }
            }

            // tábuas (e a base da lama e dos espinhos) entram no mesmo colisor
            foreach (var run in _grid.Runs(LevelGrid.IsPlankLike))
            {
                AddBox(
                    terrain,
                    new Rect(run.X0, run.Y + PlankThickness, run.Width, PlankThickness)
                );
            }

            foreach (var run in _grid.Runs(LevelGrid.Plank))
            {
                CreatePlankVisuals(terrain.transform, run);
            }

            composite.GenerateGeometry();
        }

        /// <summary>
        /// Visual do corredor curto: as células do corredor e as brechas de até dois
        /// tiles entre elas (debaixo dos buracos da bifurcação), para o piso de
        /// lanchonete não virar retalho.
        /// </summary>
        private bool LooksLikeCorridor(int x, int y)
        {
            if (_grid.IsShortCorridor(x, y))
            {
                return true;
            }

            if (_grid.IsFloor(x, y) || !_grid.IsFloor(x, y - 1))
            {
                return false;
            }

            var left = false;
            var right = false;
            for (var d = 1; d <= 2; d++)
            {
                left |= _grid.IsShortCorridor(x - d, y);
                right |= _grid.IsShortCorridor(x + d, y);
            }

            return left && right;
        }

        private bool IsExposedTop(int x, int y)
        {
            var above = _grid[x, y + 1];
            return !LevelGrid.IsSolidShape(above) && !LevelGrid.IsPlankLike(above);
        }

        /// <summary>Tábua de madeira no caminho longo; ladrilho de lanchonete no corredor curto.</summary>
        private void CreatePlankVisuals(Transform parent, LevelRun run)
        {
            var x = run.X0;
            while (x <= run.X1)
            {
                var corridor = LooksLikeCorridor(x, run.Y + 1);
                var start = x;
                while (x <= run.X1 && LooksLikeCorridor(x, run.Y + 1) == corridor)
                {
                    x++;
                }

                var width = x - start;
                var center = new Vector2(start + width * 0.5f, run.Y + 0.75f);
                if (corridor)
                {
                    TiledObject(
                        "PisoLanchonete",
                        parent,
                        Art.FastFloor,
                        center,
                        new Vector2(width, PlankThickness),
                        -10,
                        new Color(0.84f, 0.25f, 0.2f)
                    );
                }
                else
                {
                    SlicedObject(
                        "Tabua",
                        parent,
                        _visualCatalog != null ? _visualCatalog.Arena.Plank : null,
                        center,
                        new Vector2(width, PlankThickness),
                        -10,
                        new Color(0.75f, 0.5f, 0.26f)
                    );
                }
            }
        }

        private void AddBox(GameObject terrain, Rect rect)
        {
            var box = terrain.AddComponent<BoxCollider2D>();
            box.compositeOperation = Collider2D.CompositeOperation.Merge;
            box.offset = rect.center;
            box.size = rect.size;
        }

        /// <summary>O corredor curto tem cara de lanchonete: fundo quente e letreiros "FAST".</summary>
        private void CreateCorridors()
        {
            var parent = Group("Corredores", _levelParent);
            foreach (var run in _grid.Runs(c => true).Where(r => r.Y > 0 && r.Y < _grid.Height - 1))
            {
                var x = run.X0;
                while (x <= run.X1)
                {
                    if (!LooksLikeCorridor(x, run.Y))
                    {
                        x++;
                        continue;
                    }

                    var start = x;
                    while (x <= run.X1 && LooksLikeCorridor(x, run.Y))
                    {
                        x++;
                    }

                    var width = x - start;
                    var back = SpriteObject(
                        "FundoLanchonete",
                        parent,
                        PlaceholderSpriteFactory.Square,
                        new Vector2(start + width * 0.5f, run.Y + 0.75f),
                        -30
                    );
                    back.color = new Color(0.97f, 0.8f, 0.74f, 0.6f);
                    back.transform.localScale = new Vector3(width, 1.5f, 1f);

                    for (var sx = start + 4; sx < x - 2; sx += 10)
                    {
                        var sign = SpriteObject(
                            "Letreiro",
                            parent,
                            Art.FastSign,
                            new Vector2(sx + 0.5f, run.Y + 1.15f),
                            -29
                        );
                        Fit(sign, new Vector2(1.1f, 0.42f));
                        if (Scenery != null)
                        {
                            // sem texto: o selo do lanche com o raio do impulso
                            sign.sprite = Scenery.FastBadge;
                            Fit(sign, new Vector2(0.8f, 0.8f));
                        }
                    }
                }
            }
        }

        // =====================================================================
        // Perigos e mecanismos
        // =====================================================================
        private void CreateHazards()
        {
            var parent = Group("Perigos", _levelParent);
            foreach (var cell in _grid.Cells(LevelGrid.Spikes))
            {
                var spikes = new GameObject("Espinhos");
                spikes.transform.SetParent(parent, false);
                // gatilho do tamanho das pontas desenhadas: quem pula da beira não resvala
                spikes.transform.position = new Vector3(cell.x + 0.5f, cell.y + 1.2f, 0f);
                var collider = spikes.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(0.7f, 0.36f);
                var hazard = spikes.AddComponent<SpikeHazard>();
                hazard.Configure(5, 1f, _spawnOne);
                _spikes.Add(hazard);

                var art = SpriteObject(
                    "Arte",
                    spikes.transform,
                    _visualCatalog != null ? _visualCatalog.Arena.Spikes : null,
                    new Vector2(cell.x + 0.5f, cell.y + 1.25f),
                    -8
                );
                Fit(art, new Vector2(0.9f, 0.5f));
                art.drawMode = SpriteDrawMode.Simple;
                var base0 = SpriteObject(
                    "Base",
                    spikes.transform,
                    PlaceholderSpriteFactory.Square,
                    new Vector2(cell.x + 0.5f, cell.y + 0.75f),
                    -9
                );
                base0.color = new Color(0.27f, 0.29f, 0.33f);
                base0.transform.localScale = new Vector3(1f, PlankThickness, 1f);
                if (Scenery != null)
                {
                    // pontas claras sobre a faixa de perigo, vermelha e branca
                    art.sprite = Scenery.Spikes;
                    Fit(art, new Vector2(0.96f, 0.5f));
                    base0.sprite = Scenery.Hazard;
                    base0.color = Color.white;
                    Fit(base0, new Vector2(1f, PlankThickness));
                }
                CreateWorldLabel("ESPINHOS", spikes.transform, new Vector2(0f, 0.6f), Color.red);
            }

            foreach (var run in _grid.Runs(LevelGrid.Mud))
            {
                var mud = new GameObject("Lama");
                mud.transform.SetParent(parent, false);
                mud.transform.position = new Vector3(run.X0 + run.Width * 0.5f, run.Y + 1.3f, 0f);
                var collider = mud.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(run.Width, 0.6f);
                var hazard = mud.AddComponent<MudHazard>();
                hazard.Configure(0.7f);
                _mudHazards.Add(hazard);
                TiledObject(
                    "Arte",
                    mud.transform,
                    _visualCatalog != null ? _visualCatalog.Arena.Mud : null,
                    new Vector2(run.X0 + run.Width * 0.5f, run.Y + 0.75f),
                    new Vector2(run.Width, PlankThickness),
                    -9,
                    new Color(0.47f, 0.3f, 0.16f)
                );
                if (_visualCatalog != null && _visualCatalog.Ring != null)
                {
                    mud.AddComponent<MudBubbles>().Configure(_visualCatalog.Ring, run.Width, -0.3f);
                }
            }

            foreach (var cell in _grid.Cells(LevelGrid.Falling))
            {
                var platform = new GameObject("PlataformaQueCai") { layer = GroundLayer };
                platform.transform.SetParent(parent, false);
                platform.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.75f, 0f);
                var collider = platform.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1.2f, PlankThickness);
                var renderer = platform.AddComponent<SpriteRenderer>();
                renderer.sprite =
                    _visualCatalog != null
                        ? _visualCatalog.Arena.Plank
                        : PlaceholderSpriteFactory.Square;
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = new Vector2(1.2f, PlankThickness);
                renderer.color = new Color(1f, 0.86f, 0.7f);
                renderer.sortingOrder = -7;
                var falling = platform.AddComponent<FallingPlatform>();
                falling.Configure(0.45f, 2.5f);
                _fallingPlatforms.Add(falling);
            }
        }

        /// <summary>
        /// Elevador de autoatendimento (<see cref="LiftRiderSensor"/>): sobe com quem
        /// está em cima e desce sozinho quando fica vazio. Embaixo há piso fixo.
        /// </summary>
        private void CreateLifts()
        {
            var parent = Group("Elevadores", _levelParent);
            foreach (var run in _grid.Runs(LevelGrid.Lift))
            {
                var top = run.Y;
                while (_grid[run.X0, top + 1] == LevelGrid.LiftPath)
                {
                    top++;
                }

                // em repouso o topo fica rente ao piso: entra-se andando, sem degrau
                var restTop = (float)run.Y;
                var rise = top + 1 - restTop;
                var width = run.Width - 0.1f;

                var lift = new GameObject("Elevador") { layer = GroundLayer };
                lift.transform.SetParent(parent, false);
                lift.transform.position = new Vector3(
                    run.X0 + run.Width * 0.5f,
                    restTop - LiftThickness * 0.5f,
                    0f
                );
                var collider = lift.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(width, LiftThickness);
                // mão única: segura quem está em cima e, ao descer, passa por quem
                // espera embaixo em vez de esmagá-lo contra o piso fixo
                collider.usedByEffector = true;
                var effector = lift.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 170f;
                var renderer = lift.AddComponent<SpriteRenderer>();
                renderer.sprite = Art.Lift != null ? Art.Lift : PlaceholderSpriteFactory.Square;
                renderer.drawMode =
                    Art.Lift != null ? SpriteDrawMode.Sliced : SpriteDrawMode.Simple;
                if (Scenery != null)
                {
                    renderer.sprite = Scenery.Lift;
                    renderer.drawMode = SpriteDrawMode.Sliced;
                }

                renderer.size = new Vector2(width, LiftThickness + 0.05f);
                renderer.sortingOrder = -6;
                var platform = lift.AddComponent<ActivatedPlatform>();
                platform.Configure(new Vector2(0f, rise), LiftSpeed);

                // sobe com quem está com os pés em cima; não com quem ela atravessa
                lift.AddComponent<LiftRiderSensor>().Configure(collider, platform);
                _lifts.Add(platform);

                // trilhos do curso, só visuais
                for (var side = 0; side < 2; side++)
                {
                    var rx = side == 0 ? run.X0 + 0.06f : run.X1 + 0.94f;
                    var rail = SpriteObject(
                        "Trilho",
                        parent,
                        PlaceholderSpriteFactory.Square,
                        new Vector2(rx, restTop + (rise - LiftThickness) * 0.5f),
                        -31
                    );
                    rail.color = new Color(0.5f, 0.56f, 0.64f, 0.8f);
                    rail.transform.localScale = new Vector3(0.08f, rise + LiftThickness, 1f);
                }

                CreateWorldLabel(
                    $"ELEVADOR +{rise:0.#}",
                    lift.transform,
                    new Vector2(0f, 0.8f),
                    Color.white
                );
            }
        }

        private void CreateLeverGates()
        {
            var parent = Group("Alavancas", _levelParent);
            var gateCells = _grid.Cells(LevelGrid.Gate).ToList();
            foreach (var cell in _grid.Cells(LevelGrid.Lever))
            {
                // portão: a coluna de 'G' mais próxima que começa na mesma altura
                var bottoms = gateCells.Where(g => g.y == cell.y).ToList();
                if (bottoms.Count == 0)
                {
                    Debug.LogError($"Alavanca em {cell} sem portão na mesma altura.", this);
                    continue;
                }

                var gx = bottoms.OrderBy(g => Mathf.Abs(g.x - cell.x)).First().x;
                var height = 0;
                while (_grid[gx, cell.y + height] == LevelGrid.Gate)
                {
                    height++;
                }

                var gate = new GameObject("Portao");
                gate.transform.SetParent(parent, false);
                gate.transform.position = new Vector3(gx + 0.5f, cell.y + height * 0.5f, 0f);
                var collider = gate.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.45f, height);
                var renderer = gate.AddComponent<SpriteRenderer>();
                renderer.sprite = Art.Gate != null ? Art.Gate : PlaceholderSpriteFactory.Square;
                if (Scenery != null)
                {
                    renderer.sprite = Scenery.Gate;
                }

                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = new Vector2(0.5f, height);
                renderer.sortingOrder = -3;
                if (Scenery != null)
                {
                    // a capa listrada sobe e desce com a grade
                    var cap = SpriteObject(
                        "Capa",
                        gate.transform,
                        Scenery.GateCap,
                        new Vector2(gx + 0.5f, cell.y + height - 0.12f),
                        -2
                    );
                    Fit(cap, new Vector2(0.8f, 0.3f));
                }
                var door = gate.AddComponent<PuzzleDoor>();
                door.Configure(Vector2.zero, 4f, instantMotion: true);
                CreateWorldLabel(
                    "PORTÃO",
                    gate.transform,
                    new Vector2(0f, height * 0.5f + 0.3f),
                    Color.cyan
                );

                var lever = CreateLever(parent, new Vector2(cell.x + 0.5f, cell.y + 0.75f));
                CreateCable(
                    parent,
                    new Vector2(cell.x + 0.5f, cell.y + 0.4f),
                    new Vector2(gx + 0.5f, cell.y + height - 0.12f)
                );
                lever.OnActivated += () =>
                {
                    AudioService.Instance.PlaySfx(AudioClipId.Sfx_Lever);
                    // quem puxou (o mais perto da alavanca) ganha os pontos do mecanismo
                    var puller = ClosestPlayer(lever.transform.position);
                    _matchController?.RegisterPuzzleSolved(puller);
                    _recorder?.Lever(puller);
                    StartCoroutine(OpenGateAfterDelay(door));
                };
                _gates.Add((lever, door));
            }
        }

        private int ClosestPlayer(Vector2 position)
        {
            if (_playerOne == null || _playerTwo == null)
            {
                return _playerOne != null ? 1 : 0;
            }

            var d1 = ((Vector2)_playerOne.transform.position - position).sqrMagnitude;
            var d2 = ((Vector2)_playerTwo.transform.position - position).sqrMagnitude;
            return d1 <= d2 ? 1 : 2;
        }

        /// <summary>Tempo-alvo do bônus de tempo (<c>tempo-alvo</c> no [tema]; 180 s sem ele).</summary>
        public float TargetTimeSeconds =>
            _grid != null
            && _grid.Theme.TryGetValue("tempo-alvo", out var value)
            && float.TryParse(
                value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var seconds
            )
                ? seconds
                : 180f;

        private IEnumerator OpenGateAfterDelay(PuzzleDoor door)
        {
            // o portão treme enquanto abre: a espera é parte do custo do caminho longo
            var origin = door.transform.position;
            var elapsed = 0f;
            while (elapsed < GateOpenDelay)
            {
                elapsed += Time.deltaTime;
                door.transform.position =
                    origin + new Vector3(Mathf.Sin(elapsed * 60f) * 0.03f, 0f, 0f);
                yield return null;
            }

            door.transform.position = origin;
            door.SetOpen(true);
            AudioService.Instance.PlaySfx(AudioClipId.Sfx_Door);
        }

        private Lever CreateLever(Transform parent, Vector2 position)
        {
            var leverObject = new GameObject("Alavanca");
            leverObject.transform.SetParent(parent, false);
            leverObject.transform.position = position;
            var collider = leverObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 1.5f);
            collider.isTrigger = true;

            var handle = new GameObject("Haste");
            handle.transform.SetParent(leverObject.transform, false);
            handle.transform.localPosition = new Vector2(0f, 0.15f);
            handle.transform.localScale = new Vector3(0.16f, 0.8f, 1f);
            var handleRenderer = handle.AddComponent<SpriteRenderer>();
            handleRenderer.sprite = PlaceholderSpriteFactory.Square;
            handleRenderer.color = new Color(0.14f, 0.69f, 0.63f);
            handleRenderer.sortingOrder = -2;

            var baseObject = new GameObject("Base");
            baseObject.transform.SetParent(leverObject.transform, false);
            baseObject.transform.localPosition = new Vector2(0f, -0.55f);
            var baseRenderer = baseObject.AddComponent<SpriteRenderer>();
            baseRenderer.sprite =
                _visualCatalog != null && _visualCatalog.Environment.Length > 9
                    ? _visualCatalog.Environment[9]
                    : PlaceholderSpriteFactory.Square;
            baseRenderer.sortingOrder = -1;
            Fit(baseRenderer, new Vector2(0.8f, 0.5f));
            if (Scenery != null)
            {
                // mecanismo amarelo, pomo vermelho: o que se aciona tem sempre a mesma cor
                baseRenderer.sprite = Scenery.LeverBase;
                Fit(baseRenderer, new Vector2(0.8f, 0.5f));
                handleRenderer.color = new Color32(0x1E, 0x2A, 0x22, 255);
                var knob = new GameObject("Pomo");
                knob.transform.SetParent(handle.transform, false);
                knob.transform.localPosition = new Vector2(0f, 0.5f);
                var knobRenderer = knob.AddComponent<SpriteRenderer>();
                knobRenderer.sprite = Scenery.Knob;
                knobRenderer.sortingOrder = -1;
                // a haste é um quadrado esticado: o pomo desfaz a escala dela
                var knobSize = Scenery.Knob.bounds.size;
                var stretch = handle.transform.localScale;
                knob.transform.localScale = new Vector3(
                    0.36f / (knobSize.x * stretch.x),
                    0.36f / (knobSize.y * stretch.y),
                    1f
                );
            }

            var lever = leverObject.AddComponent<Lever>();
            lever.Configure(handle.transform, baseRenderer, isToggleable: false);
            CreateWorldLabel("ALAVANCA", leverObject.transform, new Vector2(0f, 1.1f), Color.white);
            return lever;
        }

        // =====================================================================
        // Alimentos, checkpoints, portas e largada
        // =====================================================================
        private void SplitFoodCatalog()
        {
            _healthyFoods.Clear();
            _fastFoods.Clear();
            if (_foodCatalog == null)
            {
                return;
            }

            foreach (var food in _foodCatalog.Where(f => f != null).OrderBy(f => f.Id))
            {
                (food.Category == FoodCategory.Healthy ? _healthyFoods : _fastFoods).Add(food);
            }
        }

        private void CreateFoods()
        {
            var parent = Group("Alimentos", _levelParent);
            var healthy = 0;
            var fast = 0;
            // ordem de leitura estável: de baixo para cima, da esquerda para a direita
            foreach (
                var cell in _grid
                    .Cells(LevelGrid.Healthy)
                    .Concat(_grid.Cells(LevelGrid.Fast))
                    .OrderBy(c => c.y)
                    .ThenBy(c => c.x)
            )
            {
                var isHealthy = _grid[cell.x, cell.y] == LevelGrid.Healthy;
                var list = isHealthy ? _healthyFoods : _fastFoods;
                var data =
                    list.Count > 0 ? list[(isHealthy ? healthy++ : fast++) % list.Count] : null;
                var item = Instantiate(
                    _collectiblePrefab,
                    new Vector3(cell.x + 0.5f, cell.y + 0.45f, 0f),
                    Quaternion.identity,
                    parent
                );
                item.name = isHealthy ? "Saudavel" : "FastFood";
                var collectible = item.GetComponent<FoodCollectible>();
                if (collectible == null)
                {
                    continue;
                }

                if (data != null)
                {
                    collectible.Configure(data);
                }

                // cada jogador tem a sua vez; o item espera, na cor de quem falta
                collectible.SetPerPlayer(2, WaitingTint);
                collectible.Collected += HandleFoodCollected;
                _foods.Add(collectible);
            }
        }

        /// <summary>Cor de quem ainda não pegou o item (o outro jogador).</summary>
        private Color WaitingTint(WellbeingComponent lastCollector)
        {
            var lastWasOne =
                _playerOne != null
                && lastCollector == _playerOne.GetComponent<WellbeingComponent>();
            return PlayerColor(lastWasOne ? 2 : 1);
        }

        private void HandleFoodCollected(FoodCollectible collectible, WellbeingComponent collector)
        {
            if (collectible == null || collectible.FoodData == null)
            {
                return;
            }

            var healthy = collectible.FoodData.Category == FoodCategory.Healthy;
            AudioService.Instance.PlaySfx(
                healthy ? AudioClipId.Sfx_PickupHealthy : AudioClipId.Sfx_PickupUltra
            );
            var hud =
                collector == _playerOne?.GetComponent<WellbeingComponent>() ? _playerOneHud
                : collector == _playerTwo?.GetComponent<WellbeingComponent>() ? _playerTwoHud
                : null;
            if (hud != null)
            {
                hud.RegisterFoodCollected(collectible.FoodData);
                hud.AddScore(collectible.FoodData.BasePoints);
            }

            if (collector != null)
            {
                var data = collectible.FoodData;
                var effect =
                    data.StartsRush ? "PICO!"
                    : collectible.LastCollectionCuredCrash ? "ACORDOU!"
                    : string.Empty;
                PickupPopup.Spawn(
                    _levelParent,
                    collector.transform.position + Vector3.up * 1.5f,
                    PickupPopup.Compose(data.DisplayName, data.WellbeingDelta, effect),
                    healthy ? PickupPopup.HealthyColor : PickupPopup.SnackColor
                );
            }

            if (_recorder != null && collector != null)
            {
                _recorder.Food(
                    hud == _playerOneHud ? 1 : 2,
                    collectible.FoodData.Id,
                    healthy,
                    collector.CurrentValue
                );
            }

            if (_visualCatalog != null)
            {
                VisualFeedback.Emit(
                    _visualCatalog,
                    healthy ? FeedbackKind.HealthyPickup : FeedbackKind.UltraProcessedPickup,
                    collectible.transform.position
                );
            }
        }

        private void CreateCheckpoints()
        {
            var parent = Group("Checkpoints", _levelParent);
            var index = 0;
            foreach (
                var cell in _grid.Cells(LevelGrid.Checkpoint).OrderBy(c => c.y).ThenBy(c => c.x)
            )
            {
                index++;
                var cp = new GameObject($"Checkpoint {index}");
                cp.transform.SetParent(parent, false);
                cp.transform.position = new Vector3(cell.x + 0.5f, cell.y + 1.2f, 0f);
                var collider = cp.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1.2f, 2.4f);
                collider.isTrigger = true;

                var flag = new GameObject("Bandeira");
                flag.transform.SetParent(cp.transform, false);
                var renderer = flag.AddComponent<SpriteRenderer>();
                renderer.sprite =
                    _visualCatalog != null && _visualCatalog.Environment.Length > 5
                        ? _visualCatalog.Environment[5]
                        : PlaceholderSpriteFactory.Square;
                renderer.color = new Color(0.55f, 0.55f, 0.6f);
                renderer.sortingOrder = -2;
                StandOn(flag.transform, renderer.sprite, -1.2f, 1.45f);

                var checkpoint = cp.AddComponent<Checkpoint>();
                checkpoint.Configure(index, new Vector2(0f, PlayerHalfHeight + 0.05f - 1.2f));
                checkpoint.SetVisualElements(
                    renderer,
                    new Color(0.55f, 0.55f, 0.6f),
                    new Color(0.25f, 0.85f, 0.35f)
                );
                checkpoint.CheckpointReached += _ =>
                {
                    AudioService.Instance.PlaySfx(AudioClipId.Sfx_Checkpoint);
                    if (_visualCatalog != null)
                    {
                        VisualFeedback.Emit(
                            _visualCatalog,
                            FeedbackKind.Checkpoint,
                            cp.transform.position
                        );
                    }
                };
                CreateWorldLabel($"CP {index}", cp.transform, new Vector2(0f, 1.6f), Color.yellow);
                _checkpoints.Add(checkpoint);
            }
        }

        private void CreateDoorsAndSpawns()
        {
            var parent = Group("Portas", _levelParent);
            foreach (
                var (symbol, index) in new[] { (LevelGrid.DoorOne, 1), (LevelGrid.DoorTwo, 2) }
            )
            {
                var cells = _grid.Cells(symbol).ToList();
                if (cells.Count == 0)
                {
                    Debug.LogError($"Mapa sem a porta do P{index}.", this);
                    continue;
                }

                var bottom = cells.OrderBy(c => c.y).First();
                var height = cells.Count(c => c.x == bottom.x);
                var door = new GameObject($"Porta P{index}");
                door.transform.SetParent(parent, false);
                door.transform.position = new Vector3(
                    bottom.x + 0.5f,
                    bottom.y + height * 0.5f,
                    0f
                );
                var collider = door.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1f, height);
                collider.isTrigger = true;
                var art = new GameObject("Arte");
                art.transform.SetParent(door.transform, false);
                var renderer = art.AddComponent<SpriteRenderer>();
                renderer.sprite =
                    _visualCatalog != null && _visualCatalog.Environment.Length > 6
                        ? _visualCatalog.Environment[6]
                        : PlaceholderSpriteFactory.Square;
                renderer.color = PlayerColor(index);
                renderer.sortingOrder = -2;
                StandOn(art.transform, renderer.sprite, -height * 0.5f, height * 0.95f);
                var finish = door.AddComponent<FinishLine>();
                finish.ConfigureAcceptedPlayer(index);
                finish.PlayerCrossedFinish += (_, _) =>
                {
                    AudioService.Instance.PlaySfx(AudioClipId.Sfx_Finish);
                    if (_visualCatalog != null)
                    {
                        VisualFeedback.Emit(
                            _visualCatalog,
                            FeedbackKind.Finish,
                            door.transform.position
                        );
                    }
                };
                CreateWorldLabel(
                    $"P{index}",
                    door.transform,
                    new Vector2(0f, height * 0.5f + 0.3f),
                    PlayerColor(index)
                );
                if (index == 1)
                {
                    _playerOneDoor = finish;
                }
                else
                {
                    _playerTwoDoor = finish;
                }

                _finishHeight = Mathf.Max(_finishHeight, bottom.y + PlayerHalfHeight);
            }
        }

        private Vector2 SpawnPosition(char symbol, Vector2 fallback)
        {
            foreach (var cell in _grid.Cells(symbol))
            {
                return new Vector2(cell.x + 0.5f, cell.y + PlayerHalfHeight + 0.05f);
            }

            return fallback;
        }

        // =====================================================================
        // Placas e enfeites
        // =====================================================================
        private void CreateSigns()
        {
            var parent = Group("Placas", _levelParent);
            foreach (var sign in _grid.Signs)
            {
                CreateSign(parent, sign.Position, sign.Text);
            }
        }

        private void CreateSign(Transform parent, Vector2 basePosition, string text)
        {
            if (TryCreatePictureSign(parent, basePosition, text))
            {
                return;
            }

            const float lineHeight = 0.5f;
            const float characterWidth = 0.27f;
            var lines = text.Split('\n');
            var longest = lines.Max(l => l.Length);
            var size = new Vector2(
                longest * characterWidth + 0.7f,
                lines.Length * lineHeight + 0.45f
            );
            var center = basePosition + Vector2.up * size.y * 0.5f;

            var board = SlicedObject(
                "Placa",
                parent,
                _visualCatalog != null ? _visualCatalog.Arena.Sign : null,
                center,
                size,
                -12,
                new Color(0.29f, 0.2f, 0.14f)
            );
            var label = new GameObject("Texto", typeof(MeshRenderer));
            label.transform.SetParent(board.transform, false);
            label.transform.localPosition = Vector3.zero;
            var textMesh = label.AddComponent<TextMesh>();
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.text = text;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.fontSize = 64;
            textMesh.characterSize = SignCharacterSize;
            textMesh.color = new Color(0.98f, 0.96f, 0.86f);
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            var renderer = label.GetComponent<MeshRenderer>();
            renderer.material = textMesh.font.material;
            renderer.sortingOrder = -11;
        }

        private void CreateDecor()
        {
            var parent = Group("Enfeites", _levelParent);
            foreach (var item in _grid.Decor)
            {
                switch (item.Kind)
                {
                    case "vaso":
                        var pot = SpriteObject(
                            "Vaso",
                            parent,
                            Art.Pot,
                            new Vector2(item.Cell.x + 0.5f, 0f),
                            -15
                        );
                        StandOn(pot.transform, pot.sprite, item.Cell.y, 0.95f);
                        break;
                    case "trepadeira":
                        var vine = SpriteObject(
                            "Trepadeira",
                            parent,
                            Art.Vine,
                            new Vector2(item.Cell.x + 0.5f, item.Cell.y + 0.5f - 0.55f),
                            -15
                        );
                        Fit(vine, new Vector2(0.55f, 1.1f));
                        break;
                }
            }
        }

        // =====================================================================
        // Jogadores, câmera, HUD e partida
        // =====================================================================
        private GameObject CreatePlayer(string playerName, Vector2 position, int playerIndex)
        {
            var player = Instantiate(_playerPrefab, position, Quaternion.identity, transform);
            player.name = playerName;
            var movement = player.GetComponent<PlayerMovement>();
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.gravityScale = JumpBudget.GravityScaleForApex(
                    Physics2D.gravity.y,
                    movement != null ? movement.JumpVelocity : 12f,
                    TargetJumpApex
                );
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            player.transform.localScale = new Vector3(0.85f, 1.25f, 1f);
            var controller = player.GetComponent<PlayerController>();
            controller?.Configure(playerIndex);

            var visualPrefab = playerIndex == 1 ? _playerOneVisualPrefab : _playerTwoVisualPrefab;
            var spriteRenderer = player.GetComponent<SpriteRenderer>();
            if (visualPrefab != null)
            {
                var visual = Instantiate(visualPrefab, player.transform, false);
                visual.name = "VisualPresentation";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = new Vector3(1f / 0.85f, 1f / 1.25f, 1f);
                var presentation = visual.GetComponent<PlayerPresentation>();
                presentation?.Bind(
                    player.GetComponent<WellbeingComponent>(),
                    player.GetComponent<FatigueComponent>(),
                    controller
                );
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = false;
                }

                if (presentation != null && presentation.Runner != null)
                {
                    // corredor humano: sem halo nem folha na cabeça
                    player.GetComponent<PlayerVisualController>()?.HidePlaceholders();
                }
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 2;
            }

            if (movement != null)
            {
                movement.Jumped += () => AudioService.Instance.PlaySfx(AudioClipId.Sfx_Jump);
            }

            var fatigue = player.GetComponent<FatigueComponent>();
            if (fatigue != null)
            {
                var balance = BalanceConfig.Current;
                fatigue.Configure(
                    balance.FatigueDurationSeconds,
                    balance.FatigueRecoveryWellbeing,
                    balance.FatigueTriggerThreshold,
                    balance.FatigueRearmThreshold
                );
                fatigue.WarningSeconds = balance.FatigueWarningSeconds;
                fatigue.StateChanged += state =>
                {
                    if (state == FatigueState.Fatigued)
                    {
                        AudioService.Instance.PlaySfx(AudioClipId.Sfx_Fatigue);
                    }
                };
            }

            var wellbeing = player.GetComponent<WellbeingComponent>();
            if (wellbeing != null)
            {
                wellbeing.WellbeingChanged += (previous, current) =>
                {
                    if (current != previous)
                    {
                        AudioService.Instance.PlaySfx(
                            current > previous
                                ? AudioClipId.Sfx_WellbeingUp
                                : AudioClipId.Sfx_WellbeingDown
                        );
                    }
                };
            }

            return player;
        }

        /// <summary>
        /// Mapa de menu (tema "modo menu"): a estufa jogável, sem corrida. Tem piso,
        /// jogadores e câmera como uma fase, mas no lugar do HUD entram as placas.
        /// </summary>
        public bool IsMenu =>
            _grid != null && _grid.Theme.TryGetValue("modo", out var mode) && mode == "menu";

        /// <summary>Palco do menu jogável; nulo numa fase de corrida.</summary>
        public MenuStage MenuStage => _menuStage;

        private MenuStage _menuStage;

        private void CreateMenuStage()
        {
            // sem HUD não há faixa reservada: a câmera usa a tela inteira
            var view = Camera.main;
            if (view != null)
            {
                view.rect = new Rect(0f, 0f, 1f, 1f);
            }

            _screens = GameScreens.Create(_screensPrefab, _inputActions, false, transform);
            _menuStage = MenuStage.Create(
                transform,
                _screens,
                _playerOne,
                _playerTwo,
                new Vector2(_grid.Width * 0.5f, Mathf.Floor(_spawnOne.y)),
                PlayerColor(1),
                PlayerColor(2)
            );
        }

        private void CreateCamera()
        {
            _cameraController = LevelCameraSetup.Create(
                transform,
                _grid,
                _spawnOne,
                _spawnTwo,
                _playerOne.transform,
                _playerTwo.transform,
                _foliage
            );
        }

        private void CreateHud()
        {
            var canvasObject = LevelHudSetup.CreateCanvas(transform);

            _matchHud = new GameObject("Match HUD", typeof(RectTransform)).AddComponent<MatchHUD>();
            _matchHud.Initialize(canvasObject.transform, Vector2.zero);
            _matchHud.SetLevelName(
                _grid.LevelNumber > 0
                    ? $"Fase {_grid.LevelNumber} · {_grid.LevelName}"
                    : _grid.LevelName
            );

            var p1Wellbeing = _playerOne.GetComponent<WellbeingComponent>();
            var p2Wellbeing = _playerTwo.GetComponent<WellbeingComponent>();
            var p1Fatigue = _playerOne.GetComponent<FatigueComponent>();
            var p2Fatigue = _playerTwo.GetComponent<FatigueComponent>();

            _playerOneHud = LevelHudSetup.CreatePlayerHud(
                canvasObject.transform,
                1,
                PlayerColor(1),
                p1Wellbeing,
                p1Fatigue
            );
            _playerTwoHud = LevelHudSetup.CreatePlayerHud(
                canvasObject.transform,
                2,
                PlayerColor(2),
                p2Wellbeing,
                p2Fatigue
            );

            _greenhouseHud = GreenhouseHud.Create(
                canvasObject.transform,
                _matchHud,
                _playerOneHud,
                _playerTwoHud,
                PlayerColor(1),
                PlayerColor(2)
            );

            _screens = GameScreens.Create(_screensPrefab, _inputActions, true, transform);
            if (_screens == null)
            {
                GameScreens.EnsureEventSystem();
                _resultsPanel = new GameObject(
                    "Match Results Panel",
                    typeof(RectTransform)
                ).AddComponent<MatchResultsPanel>();
                _resultsPanel.Initialize(canvasObject.transform);
                _resultsPanel.ReplayRequested += () => GameFlow.RestartLevel();
            }

            var controllerObject = new GameObject("MatchCompletionController");
            controllerObject.transform.SetParent(transform, false);
            _matchController = controllerObject.AddComponent<MatchCompletionController>();
            _matchController.Configure(
                _playerOneDoor,
                _matchHud,
                _playerOne,
                _playerTwo,
                p1Wellbeing,
                p2Wellbeing,
                _resultsPanel,
                PartnerWindowSeconds,
                20,
                _playerOneHud,
                _playerTwoHud,
                scoreConfig: Scoring.ScoreConfig.CreateForRace(TargetTimeSeconds),
                secondFinishLine: _playerTwoDoor,
                requireBothAtDoors: false,
                timeLimitSeconds: MatchTimeLimitSeconds
            );
            StartRecording(p1Wellbeing, p2Wellbeing, p1Fatigue, p2Fatigue);
            _countdown = gameObject.AddComponent<RaceCountdown>();
            _countdown.Begin(
                canvasObject.transform,
                _matchController,
                _playerOne.GetComponent<PlayerController>(),
                _playerTwo.GetComponent<PlayerController>()
            );
            _matchController.MatchConcluded += results =>
            {
                RecordResults(results, p1Fatigue, p2Fatigue);
                foreach (var result in new[] { results.Player1, results.Player2 })
                {
                    if (!result.IsDnf)
                    {
                        SaveGame.RecordFinish(_grid.LevelNumber, result.FinishTime);
                    }
                }

                AudioService.Instance.PlaySfx(
                    results.WinnerPlayerIndex == 0 ? AudioClipId.Sfx_Tie : AudioClipId.Sfx_Victory
                );
                _screens?.ShowResults(
                    results.WinnerPlayerIndex == 0
                        ? "EMPATE!"
                        : $"P{results.WinnerPlayerIndex} VENCEU!",
                    ToResultData(results.Player1, _playerOneHud, p1Fatigue),
                    ToResultData(results.Player2, _playerTwoHud, p2Fatigue),
                    BalanceConfig.Current.VictoryByArrival
                        ? Scoring.RaceOutcome.Describe(results.Race)
                        : null
                );

                if (_screens != null)
                {
                    var race = results.Race;
                    var floors = Mathf.Clamp(
                        _grid
                            .Signs.Where(sign => sign.Text.Contains("CURTO"))
                            .Select(sign => Mathf.RoundToInt(sign.Position.y))
                            .Distinct()
                            .Count(),
                        3,
                        8
                    );
                    var turned = _trace.TryLastLeadChange(out var turn, out _, out var withRush);
                    _resultBoard = RaceResultBoard.Show(
                        _screens.View.ScreenRoot(VisualScreen.Results),
                        new RaceResultBoard.Data
                        {
                            Winner = results.WinnerPlayerIndex,
                            Outcome = Scoring.RaceOutcome.Describe(race),
                            Story = _trace.Describe(results.WinnerPlayerIndex, floors),
                            StarsOne = ToResultData(
                                results.Player1,
                                _playerOneHud,
                                p1Fatigue
                            ).Stars,
                            StarsTwo = ToResultData(
                                results.Player2,
                                _playerTwoHud,
                                p2Fatigue
                            ).Stars,
                            Leads = _trace.Leads(24),
                            HasTurn = turned,
                            Turn = turn,
                            TurnWithRush = withRush,
                            Floors = floors,
                            ColorOne = PlayerColor(1),
                            ColorTwo = PlayerColor(2),
                            WinnerPose =
                                _visualCatalog != null && results.WinnerPlayerIndex != 0
                                    ? _visualCatalog.RunnerPose(results.WinnerPlayerIndex)
                                    : null,
                        }
                    );
                }
            };
        }

        // =====================================================================
        // Registro de partida (playtest)
        // =====================================================================
        private void StartRecording(
            WellbeingComponent p1Wellbeing,
            WellbeingComponent p2Wellbeing,
            FatigueComponent p1Fatigue,
            FatigueComponent p2Fatigue
        )
        {
            if (!MatchRecorder.Enabled)
            {
                return;
            }

            _recorder = gameObject.AddComponent<MatchRecorder>();
            var p1Movement = _playerOne.GetComponent<PlayerMovement>();
            var p2Movement = _playerTwo.GetComponent<PlayerMovement>();
            _recorder.Begin(
                _grid,
                TargetTimeSeconds,
                () => _matchController != null ? _matchController.Elapsed : 0f,
                _playerOne.transform,
                _playerTwo.transform,
                i => (i == 1 ? p1Wellbeing : p2Wellbeing)?.CurrentValue ?? 0,
                i => (i == 1 ? p1Movement : p2Movement)?.SnackState ?? SnackState.None
            );
            if (p1Fatigue != null)
            {
                p1Fatigue.StateChanged += state =>
                {
                    if (state == FatigueState.Fatigued)
                        _recorder?.Fatigue(1);
                };
            }

            if (p2Fatigue != null)
            {
                p2Fatigue.StateChanged += state =>
                {
                    if (state == FatigueState.Fatigued)
                        _recorder?.Fatigue(2);
                };
            }

            foreach (var spike in _spikes)
            {
                spike.PlayerPenalized += (player, _) => _recorder?.Spike(player);
            }

            _matchController.PlayerFinished += (player, seconds) =>
                _recorder?.Finish(player, seconds);
        }

        private void RecordResults(
            MatchResultData results,
            FatigueComponent p1Fatigue,
            FatigueComponent p2Fatigue
        )
        {
            if (_recorder == null)
            {
                return;
            }

            foreach (
                var (result, hud, fatigue) in new[]
                {
                    (results.Player1, _playerOneHud, p1Fatigue),
                    (results.Player2, _playerTwoHud, p2Fatigue),
                }
            )
            {
                _recorder.Result(
                    result.PlayerIndex,
                    !result.IsDnf,
                    result.FinishTime,
                    result.Placement,
                    result.Score.FinalScore,
                    result.FinalWellbeing,
                    hud != null ? hud.HealthyFoodCount : 0,
                    hud != null ? hud.UltraProcessedFoodCount : 0,
                    fatigue != null ? fatigue.FatigueEpisodeCount : 0,
                    result.Stars
                );
            }

            _recorder.End(results.WinnerPlayerIndex);
        }

        private static PlayerResultData ToResultData(
            PlayerMatchResult result,
            PlayerHUD hud,
            FatigueComponent fatigue
        )
        {
            var hudData = new PlayerHudData
            {
                Player = result.PlayerIndex,
                Wellbeing = result.FinalWellbeing,
                Score = result.Score.FinalScore,
                HealthyFoods = hud != null ? hud.HealthyFoodCount : 0,
                UltraProcessedFoods = hud != null ? hud.UltraProcessedFoodCount : 0,
                FatigueEpisodes = fatigue != null ? fatigue.FatigueEpisodeCount : 0,
                Progress = result.IsDnf ? 0f : 1f,
            };
            var data = result.Score.ToVisualResultData(hudData, result.FinishTime, !result.IsDnf);
            data.Stars = result.Stars;
            return data;
        }

        // =====================================================================
        // Auxiliares
        // =====================================================================
        private Color PlayerColor(int playerIndex)
        {
            if (_visualCatalog != null)
            {
                return _visualCatalog.IdentityColor(playerIndex);
            }

            return playerIndex == 2
                ? new Color32(0xF2, 0x99, 0x4A, 255)
                : new Color(0.19f, 0.56f, 0.94f);
        }

        private static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private SpriteRenderer SpriteObject(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 position,
            int order
        )
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : PlaceholderSpriteFactory.Square;
            renderer.sortingOrder = order;
            if (_visualCatalog != null && _visualCatalog.SpriteMaterial != null && sprite != null)
            {
                renderer.sharedMaterial = _visualCatalog.SpriteMaterial;
            }

            return renderer;
        }

        /// <summary>Repete o sprite na área pedida; sem arte, um bloco da cor de reserva.</summary>
        private SpriteRenderer TiledObject(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 center,
            Vector2 size,
            int order,
            Color fallback
        )
        {
            var renderer = SpriteObject(name, parent, sprite, center, order);
            if (sprite != null)
            {
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = size;
            }
            else
            {
                renderer.color = fallback;
                renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            return renderer;
        }

        private SpriteRenderer SlicedObject(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 center,
            Vector2 size,
            int order,
            Color fallback
        )
        {
            var renderer = SpriteObject(name, parent, sprite, center, order);
            if (sprite != null)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = size;
            }
            else
            {
                renderer.color = fallback;
                renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            return renderer;
        }

        /// <summary>Escala o sprite para caber exatamente em <paramref name="size"/>.</summary>
        private static void Fit(SpriteRenderer renderer, Vector2 size)
        {
            if (renderer.sprite == null)
            {
                return;
            }

            var bounds = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(
                size.x / Mathf.Max(0.0001f, bounds.x),
                size.y / Mathf.Max(0.0001f, bounds.y),
                1f
            );
        }

        /// <summary>
        /// Coloca um desenho de pé sobre um piso, com escala uniforme para a altura
        /// pedida. Os desenhos do atlas têm folga transparente embaixo.
        /// </summary>
        private static void StandOn(
            Transform target,
            Sprite sprite,
            float surfaceLocalY,
            float height
        )
        {
            const float bottomPadding = 0.07f;
            var spriteHeight = sprite != null ? sprite.bounds.size.y : 1f;
            var scale = height / Mathf.Max(0.0001f, spriteHeight);
            target.localScale = new Vector3(scale, scale, 1f);
            var position = target.localPosition;
            position.y = surfaceLocalY + height * 0.5f - height * bottomPadding;
            target.localPosition = position;
        }

        /// <summary>Rótulo de depuração (F1). Instrução para o jogador é placa.</summary>
        private static void CreateWorldLabel(
            string text,
            Transform parent,
            Vector2 localPosition,
            Color color
        )
        {
            var labelObject = new GameObject(
                "WorldLabel",
                typeof(MeshRenderer),
                typeof(DebugLabel)
            );
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = parent.position + (Vector3)localPosition;
            labelObject.transform.localScale = Vector3.one;
            var lossy = labelObject.transform.lossyScale;
            labelObject.transform.localScale = new Vector3(0.08f / lossy.x, 0.08f / lossy.y, 1f);
            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.text = text;
            textMesh.fontSize = 28;
            textMesh.color = color;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.material = textMesh.font.material;
            renderer.sortingOrder = 30;
        }
    }
}
