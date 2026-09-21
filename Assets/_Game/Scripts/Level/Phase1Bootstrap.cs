using System.Collections.Generic;
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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HealthyRace.Level
{
    /// <summary>
    /// Level bootstrap and orchestrator for Phase 1: Corrida do Equilíbrio.
    /// Builds the 6-zone blockout, spawns players, camera, HUD, checkpoints,
    /// puzzles, and educational food distribution.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class Phase1Bootstrap : MonoBehaviour
    {
        private const int GroundLayer = 6;
        public const float StartX = 0f;
        public const float FinishX = 215f;

        [Header("Required Prefabs")]
        [SerializeField]
        private GameObject _playerPrefab;

        [SerializeField]
        private GameObject _collectiblePrefab;

        [SerializeField]
        private GameObject _pushableBoxPrefab;

        [Header("Food Catalog (MVP 18)")]
        [SerializeField]
        private FoodData[] _foodCatalog;

        [Header("Visual Integration")]
        [SerializeField]
        private VisualCatalog _visualCatalog;

        [SerializeField]
        private GameObject _playerOneVisualPrefab;

        [SerializeField]
        private GameObject _playerTwoVisualPrefab;

        private readonly Dictionary<string, FoodData> _foodLookup = new(
            System.StringComparer.OrdinalIgnoreCase
        );
        private readonly List<Checkpoint> _checkpoints = new();
        private readonly List<SpikeHazard> _spikes = new();
        private readonly List<MudHazard> _mudHazards = new();
        private readonly List<FallingPlatform> _fallingPlatforms = new();
        private FinishLine _finishLine;
        private GameObject _playerOne;
        private GameObject _playerTwo;
        private WellbeingComponent _playerOneWellbeing;
        private WellbeingComponent _playerTwoWellbeing;
        private PlayerHUD _playerOneHud;
        private PlayerHUD _playerTwoHud;
        private MatchHUD _matchHud;
        private SharedCameraController _cameraController;
        private CooperativePuzzleController _finalPuzzle;
        private MatchCompletionController _matchController;
        private MatchResultsPanel _resultsPanel;

        public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints;
        public IReadOnlyList<SpikeHazard> Spikes => _spikes;
        public IReadOnlyList<MudHazard> MudHazards => _mudHazards;
        public IReadOnlyList<FallingPlatform> FallingPlatforms => _fallingPlatforms;
        public FinishLine FinishLine => _finishLine;
        public GameObject PlayerOne => _playerOne;
        public GameObject PlayerTwo => _playerTwo;
        public PlayerHUD PlayerOneHud => _playerOneHud;
        public PlayerHUD PlayerTwoHud => _playerTwoHud;
        public MatchHUD MatchHud => _matchHud;
        public CooperativePuzzleController FinalPuzzle => _finalPuzzle;
        public MatchCompletionController MatchController => _matchController;
        public MatchResultsPanel ResultsPanel => _resultsPanel;
        public VisualCatalog VisualCatalog => _visualCatalog;
        public GameObject PlayerOneVisualPrefab => _playerOneVisualPrefab;
        public GameObject PlayerTwoVisualPrefab => _playerTwoVisualPrefab;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                return;
            }

#if UNITY_EDITOR
            if (_visualCatalog == null)
            {
                _visualCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualCatalog>(
                    "Assets/_Game/Art/VisualAtlas/SO_VisualCatalog.asset"
                );
            }

            if (_playerOneVisualPrefab == null)
            {
                _playerOneVisualPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Game/Prefabs/Visual/PF_VIS_Player_P1.prefab"
                );
            }

            if (_playerTwoVisualPrefab == null)
            {
                _playerTwoVisualPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Game/Prefabs/Visual/PF_VIS_Player_P2.prefab"
                );
            }
#endif

            BuildFoodLookup();

            Application.targetFrameRate = 60;
            Physics2D.gravity = new Vector2(0f, -18f);

            CreateBackground();
            CreateZones();

            _playerOne = CreatePlayer("P1", new Vector2(-1.5f, -1.8f), 1);
            _playerTwo = CreatePlayer("P2", new Vector2(1.5f, -1.8f), 2);

            _playerOneWellbeing = _playerOne.GetComponent<WellbeingComponent>();
            _playerTwoWellbeing = _playerTwo.GetComponent<WellbeingComponent>();
            var p1Fatigue = _playerOne.GetComponent<FatigueComponent>();
            var p2Fatigue = _playerTwo.GetComponent<FatigueComponent>();

            CreateSharedCamera(_playerOne.transform, _playerTwo.transform);
            CreateHud(_playerOneWellbeing, p1Fatigue, _playerTwoWellbeing, p2Fatigue);
        }

        private void Start()
        {
            AudioService.Instance.PlayMusic(AudioClipId.Music_Level01);
        }

        private void Update()
        {
            if (_matchHud != null)
            {
                _matchHud.UpdateTimer(Time.timeSinceLevelLoad);
            }

            if (_playerOne != null && _playerOneHud != null)
            {
                var progress1 = Mathf.Clamp01(
                    (_playerOne.transform.position.x - StartX) / (FinishX - StartX)
                );
                _playerOneHud.UpdateProgress(progress1);
            }

            if (_playerTwo != null && _playerTwoHud != null)
            {
                var progress2 = Mathf.Clamp01(
                    (_playerTwo.transform.position.x - StartX) / (FinishX - StartX)
                );
                _playerTwoHud.UpdateProgress(progress2);
            }
        }

        private bool HasRequiredReferences()
        {
            return _playerPrefab != null && _collectiblePrefab != null;
        }

        private void BuildFoodLookup()
        {
            _foodLookup.Clear();
            if (_foodCatalog == null)
            {
                return;
            }

            foreach (var food in _foodCatalog)
            {
                if (food != null && !string.IsNullOrEmpty(food.Id))
                {
                    _foodLookup[food.Id] = food;
                }
            }
        }

        private FoodData GetFood(string id)
        {
            _foodLookup.TryGetValue(id, out var data);
            return data;
        }

        private void CreateBackground()
        {
            var bgParent = new GameObject("Background").transform;
            bgParent.SetParent(transform, false);

            var backgroundSprite = _visualCatalog != null ? _visualCatalog.Background : null;
            var tileIndex = 0;
            for (var x = 0f; x <= 224f; x += 32f, tileIndex++)
            {
                var bg = new GameObject($"Landscape_{x:000}");
                bg.transform.SetParent(bgParent, false);
                var renderer = bg.AddComponent<SpriteRenderer>();
                renderer.sprite = backgroundSprite ?? PlaceholderSpriteFactory.Square;
                renderer.color =
                    backgroundSprite != null ? Color.white : new Color(0.55f, 0.82f, 0.94f);
                renderer.sortingOrder = -50;
                renderer.flipX = backgroundSprite != null && tileIndex % 2 == 1;

                var bounds = renderer.sprite.bounds;
                var scale = new Vector3(32f / bounds.size.x, 18f / bounds.size.y, 1f);
                bg.transform.localScale = scale;
                var scaledCenter = Vector3.Scale(bounds.center, scale);
                bg.transform.position = new Vector3(x, 1.5f, 5f) - scaledCenter;
            }
        }

        private void CreateZones()
        {
            var levelParent = new GameObject("LevelGeometry").transform;
            levelParent.SetParent(transform, false);

            CreateZone1_Spawn(levelParent);
            CreateZone2_Caixa(levelParent);
            CreateZone3_Rotas(levelParent);
            CreateZone4_Reencontro(levelParent);
            CreateZone5_PuzzleFinal(levelParent);
            CreateZone6_Chegada(levelParent);
        }

        // =========================================================================
        // ZONE 1: SPAWN (X: -5 to 32)
        // =========================================================================
        private void CreateZone1_Spawn_Legacy(Transform parent)
        {
            var zone = new GameObject("Z1_Spawn").transform;
            zone.SetParent(parent, false);

            // Ground & left boundary wall
            CreateStaticBlock(
                "Z1_Ground",
                zone,
                new Vector2(13.5f, -3.4f),
                new Vector2(37f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f),
                StaticBlockKind.Ground
            );
            CreateStaticBlock(
                "Z1_LeftBoundary",
                zone,
                new Vector2(-5.5f, 0f),
                new Vector2(1f, 8f),
                new Color(0.08f, 0.32f, 0.25f),
                StaticBlockKind.Wall
            );

            // Starting line marker
            CreateStaticBlock(
                "Z1_StartMarker",
                zone,
                new Vector2(0f, -2.6f),
                new Vector2(0.3f, 0.8f),
                Color.white,
                StaticBlockKind.Wall
            );
            CreateWorldLabel("LARGADA", zone, new Vector2(0f, -1.8f), Color.white);
            CreateWorldLabel(
                "Z1 — SPAWN",
                zone,
                new Vector2(13.5f, -1.8f),
                new Color(0.85f, 0.95f, 1f)
            );

            // Scenery props
            CreateSceneryProp(
                "Z1_Tree1",
                zone,
                new Vector2(-2f, -1.2f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z1_Tree2",
                zone,
                new Vector2(6f, -1.2f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z1_Tree3",
                zone,
                new Vector2(22f, -1.2f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z1_Shrub1",
                zone,
                new Vector2(2f, -2.4f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );
            CreateSceneryProp(
                "Z1_Shrub2",
                zone,
                new Vector2(14f, -2.4f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );

            // Introductory low step platforms (jump training: heights 1.2m, gaps 3-4m)
            CreateStaticBlock(
                "Z1_Platform1",
                zone,
                new Vector2(10f, -2.2f),
                new Vector2(4f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f),
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z1_Platform2",
                zone,
                new Vector2(18f, -1.4f),
                new Vector2(4f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f),
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z1_Platform3",
                zone,
                new Vector2(25f, -2.2f),
                new Vector2(4f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f),
                StaticBlockKind.Platform
            );

            // Introductory healthy collectibles
            SpawnFood("Apple", new Vector2(10f, -1.4f), zone);
            SpawnFood("Banana", new Vector2(18f, -0.6f), zone);
            SpawnFood("Water", new Vector2(25f, -1.4f), zone);
        }

        // =========================================================================
        // ZONE 2: CAIXA (X: 32 to 65)
        // =========================================================================
        private void CreateZone2_Caixa_Legacy(Transform parent)
        {
            var zone = new GameObject("Z2_Caixa").transform;
            zone.SetParent(parent, false);

            // Ground before cliff
            CreateStaticBlock(
                "Z2_GroundLower",
                zone,
                new Vector2(41f, -3.4f),
                new Vector2(18f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f),
                StaticBlockKind.Ground
            );

            // Elevated cliff ledge: height -0.6 (2.8m above lower ground, impassable by single jump)
            CreateStaticBlock(
                "Z2_CliffLedge",
                zone,
                new Vector2(57.5f, -0.6f),
                new Vector2(15f, 0.6f),
                new Color(0.16f, 0.63f, 0.47f),
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z2_CliffWall",
                zone,
                new Vector2(50f, -2.0f),
                new Vector2(1f, 2.2f),
                new Color(0.09f, 0.34f, 0.31f),
                StaticBlockKind.Wall
            );

            CreateWorldLabel(
                "Z2 — CAIXA",
                zone,
                new Vector2(45f, -1.8f),
                new Color(0.85f, 0.95f, 1f)
            );
            CreateWorldLabel(
                "Empurre a caixa para subir!",
                zone,
                new Vector2(45f, -2.2f),
                new Color(0.95f, 0.95f, 0.7f)
            );

            // Scenery props
            CreateSceneryProp(
                "Z2_Tree1",
                zone,
                new Vector2(36f, -1.2f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z2_Tree2",
                zone,
                new Vector2(62f, 1.6f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z2_Shrub1",
                zone,
                new Vector2(40f, -2.4f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );

            // Pushable crate
            CreatePushableBox("Z2_PushableBox", zone, new Vector2(42f, -2.25f));

            // Elevated perch with reward foods
            CreateStaticBlock(
                "Z2_Perch",
                zone,
                new Vector2(58f, 1.2f),
                new Vector2(4f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f),
                StaticBlockKind.Platform
            );
            SpawnFood("Carrot", new Vector2(58f, 1.9f), zone);
            SpawnFood("Orange", new Vector2(54f, 0.1f), zone);
        }

        // =========================================================================
        // ZONE 3: ROTAS (X: 65 to 125) - Split routes & Checkpoint 1
        // =========================================================================
        private void CreateZone3_Rotas_Legacy(Transform parent)
        {
            var zone = new GameObject("Z3_Rotas").transform;
            zone.SetParent(parent, false);

            CreateWorldLabel(
                "Z3 — ROTAS",
                zone,
                new Vector2(70f, 0.8f),
                new Color(0.85f, 0.95f, 1f)
            );
            CreateWorldLabel(
                "↑ Rota Saudável   |   ↓ Rota Rápida",
                zone,
                new Vector2(70f, 0.3f),
                new Color(0.95f, 0.95f, 0.7f)
            );

            // Scenery props
            CreateSceneryProp(
                "Z3_Tree1",
                zone,
                new Vector2(68f, 2.0f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z3_Tree2",
                zone,
                new Vector2(90f, -1.2f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z3_Tree3",
                zone,
                new Vector2(112f, -1.2f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z3_Shrub1",
                zone,
                new Vector2(78f, -2.4f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );
            CreateSceneryProp(
                "Z3_Shrub2",
                zone,
                new Vector2(100f, -2.4f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );

            // Checkpoint 1 at entrance of Zone 3
            var cp1 = CreateCheckpointObject("Checkpoint_1", zone, 1, new Vector2(66f, 0.1f));
            _checkpoints.Add(cp1);

            // -------------------------------------------------------------
            // Upper Route: Healthy & Agility (Elevated platforms with healthy foods)
            // -------------------------------------------------------------
            var upper = new GameObject("UpperRoute_Healthy").transform;
            upper.SetParent(zone, false);

            CreateStaticBlock(
                "Upper_P1",
                upper,
                new Vector2(74f, 1.6f),
                new Vector2(5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f),
                StaticBlockKind.Platform
            );
            SpawnFood("Broccoli", new Vector2(74f, 2.3f), upper);

            CreateStaticBlock(
                "Upper_P2",
                upper,
                new Vector2(83f, 2.4f),
                new Vector2(4.5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f),
                StaticBlockKind.Platform
            );
            SpawnFood("Strawberry", new Vector2(83f, 3.1f), upper);

            var falling1 = CreateFallingPlatformObject(
                "Upper_FallingPlatform",
                upper,
                new Vector2(87f, 2.7f),
                new Vector2(3.5f, 0.4f),
                0.5f,
                3.0f
            );
            _fallingPlatforms.Add(falling1);

            CreateStaticBlock(
                "Upper_P3",
                upper,
                new Vector2(91f, 3.0f),
                new Vector2(4.5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f),
                StaticBlockKind.Platform
            );
            SpawnFood("Watermelon", new Vector2(91f, 3.7f), upper);

            CreateStaticBlock(
                "Upper_P4",
                upper,
                new Vector2(99f, 2.4f),
                new Vector2(4.5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f),
                StaticBlockKind.Platform
            );
            SpawnFood("Avocado", new Vector2(99f, 3.1f), upper);

            CreateStaticBlock(
                "Upper_P5",
                upper,
                new Vector2(107f, 1.6f),
                new Vector2(5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f),
                StaticBlockKind.Platform
            );
            SpawnFood("Fish", new Vector2(107f, 2.3f), upper);

            CreateStaticBlock(
                "Upper_P6",
                upper,
                new Vector2(116f, 0.6f),
                new Vector2(5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f),
                StaticBlockKind.Platform
            );
            SpawnFood("Banana", new Vector2(116f, 1.3f), upper);

            // -------------------------------------------------------------
            // Lower Route: Flat & Temptation (Continuous ground with ultra-processed foods)
            // -------------------------------------------------------------
            var lower = new GameObject("LowerRoute_Temptation").transform;
            lower.SetParent(zone, false);

            CreateStaticBlock(
                "Lower_Ground",
                lower,
                new Vector2(95f, -3.4f),
                new Vector2(60f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f),
                StaticBlockKind.Ground
            );

            var mud1 = CreateMudHazardObject(
                "Lower_Mud1",
                lower,
                new Vector2(84.5f, -2.85f),
                new Vector2(6f, 0.4f),
                0.7f
            );
            _mudHazards.Add(mud1);

            var mud2 = CreateMudHazardObject(
                "Lower_Mud2",
                lower,
                new Vector2(105.5f, -2.85f),
                new Vector2(6f, 0.4f),
                0.7f
            );
            _mudHazards.Add(mud2);

            SpawnFood("Burger", new Vector2(74f, -2.4f), lower);
            SpawnFood("Soda", new Vector2(81f, -2.4f), lower);
            SpawnFood("Fries", new Vector2(88f, -2.4f), lower);
            SpawnFood("Donut", new Vector2(95f, -2.4f), lower);
            SpawnFood("Pizza", new Vector2(102f, -2.4f), lower);
            SpawnFood("HotDog", new Vector2(109f, -2.4f), lower);
            SpawnFood("Chips", new Vector2(116f, -2.4f), lower);
            SpawnFood("Candy", new Vector2(122f, -2.4f), lower);
        }

        // =========================================================================
        // ZONE 4: REENCONTRO (X: 125 to 160) - Reunion & Checkpoint 2
        // =========================================================================
        private void CreateZone4_Reencontro_Legacy(Transform parent)
        {
            var zone = new GameObject("Z4_Reencontro").transform;
            zone.SetParent(parent, false);

            // Reconnecting transition ramp from lower ground
            CreateStaticBlock(
                "Z4_TransitionStep",
                zone,
                new Vector2(123.5f, -2.6f),
                new Vector2(5f, 0.8f),
                new Color(0.14f, 0.55f, 0.42f),
                StaticBlockKind.Platform
            );

            // Main reunion ground plateau at Y = -1.8
            CreateStaticBlock(
                "Z4_Ground",
                zone,
                new Vector2(142.5f, -1.8f),
                new Vector2(35f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f),
                StaticBlockKind.Ground
            );

            CreateWorldLabel(
                "Z4 — REENCONTRO",
                zone,
                new Vector2(142.5f, -0.2f),
                new Color(0.85f, 0.95f, 1f)
            );

            // Scenery props
            CreateSceneryProp(
                "Z4_Tree1",
                zone,
                new Vector2(128f, 0.4f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z4_Tree2",
                zone,
                new Vector2(144f, 0.4f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z4_Shrub1",
                zone,
                new Vector2(136f, -0.8f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );

            // Checkpoint 2 at start of reunion area
            var cp2 = CreateCheckpointObject("Checkpoint_2", zone, 2, new Vector2(130f, -0.8f));
            _checkpoints.Add(cp2);

            // Spike obstacle on plateau
            var spike = CreateSpikeHazardObject(
                "Z4_Spikes",
                zone,
                new Vector2(150f, -1.25f),
                new Vector2(3f, 0.4f),
                5,
                new Vector2(130f, -0.3f)
            );
            _spikes.Add(spike);

            // Hydrating and restorative fruit
            SpawnFood("Water", new Vector2(138f, -1.0f), zone);
            SpawnFood("Apple", new Vector2(146f, -1.0f), zone);
            SpawnFood("Orange", new Vector2(154f, -1.0f), zone);
        }

        // =========================================================================
        // ZONE 5: PUZZLE FINAL (X: 160 to 195) - Cooperative Challenge & Anti-Deadlock
        // =========================================================================
        private void CreateZone5_PuzzleFinal_Legacy(Transform parent)
        {
            var zone = new GameObject("Z5_PuzzleFinal").transform;
            zone.SetParent(parent, false);

            // Ground before gate
            CreateStaticBlock(
                "Z5_Ground1",
                zone,
                new Vector2(167f, -1.8f),
                new Vector2(14f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f),
                StaticBlockKind.Ground
            );

            // Interactive puzzle door at X = 175
            var door = CreatePuzzleDoorObject(
                "Z5_Door",
                zone,
                new Vector2(175f, -0.3f),
                new Vector2(1.2f, 2.2f)
            );

            // Ground past the barrier
            CreateStaticBlock(
                "Z5_Ground2",
                zone,
                new Vector2(185f, -1.8f),
                new Vector2(20f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f),
                StaticBlockKind.Ground
            );

            CreateWorldLabel(
                "Z5 — PUZZLE FINAL",
                zone,
                new Vector2(175f, 2.8f),
                new Color(0.85f, 0.95f, 1f)
            );

            // Scenery props
            CreateSceneryProp(
                "Z5_Tree1",
                zone,
                new Vector2(163f, 0.4f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z5_Tree2",
                zone,
                new Vector2(188f, 0.4f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z5_Shrub1",
                zone,
                new Vector2(171f, -0.8f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );

            // Interactive puzzle elements
            var button = CreatePressureButtonObject(
                "Z5_PressureButton",
                zone,
                new Vector2(169f, -1.32f)
            );
            var box = CreatePushableBox("Z5_PushableBox", zone, new Vector2(164f, -1.0f));
            var lever = CreateLeverObject("Z5_Lever", zone, new Vector2(166f, -1.0f), "ALAVANCA");
            var resetSwitch = CreateLeverObject(
                "Z5_ResetSwitch",
                zone,
                new Vector2(161f, -1.0f),
                "RESET",
                false
            );
            var platform = CreateActivatedPlatformObject(
                "Z5_MovingPlatform",
                zone,
                new Vector2(180f, -0.8f),
                new Vector2(3.5f, 0.4f),
                new Vector2(0f, 2.5f)
            );

            // Cooperative puzzle controller orchestrates door and moving platform
            var puzzleControllerObj = new GameObject("CooperativePuzzleController");
            puzzleControllerObj.transform.SetParent(zone, false);
            _finalPuzzle = puzzleControllerObj.AddComponent<CooperativePuzzleController>();
            _finalPuzzle.Bind(button, lever, box, door, platform, resetSwitch, requireAll: false);

            if (button != null)
            {
                button.OnPressedStateChanged += (pressed) =>
                {
                    if (pressed)
                    {
                        AudioService.Instance.PlaySfx(AudioClipId.Sfx_Button);
                    }
                };
            }
            if (lever != null)
            {
                lever.OnToggled += (active) => AudioService.Instance.PlaySfx(AudioClipId.Sfx_Lever);
            }
            if (door != null)
            {
                door.OnOpened += () => AudioService.Instance.PlaySfx(AudioClipId.Sfx_Door);
            }
            if (_finalPuzzle != null)
            {
                _finalPuzzle.OnPuzzleSolved += () =>
                    AudioService.Instance.PlaySfx(AudioClipId.Sfx_PuzzleSolved);
            }

            // Anti-deadlock bypass stepping stones (allows jump-clearing even if puzzle is skipped)
            CreateStaticBlock(
                "Z5_Step1",
                zone,
                new Vector2(167f, 0.2f),
                new Vector2(3f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f),
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z5_Step2",
                zone,
                new Vector2(175f, 1.2f),
                new Vector2(3f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f),
                StaticBlockKind.Platform
            );
            CreateStaticBlock(
                "Z5_Step3",
                zone,
                new Vector2(182f, 0.2f),
                new Vector2(3f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f),
                StaticBlockKind.Platform
            );

            SpawnFood("Carrot", new Vector2(175f, 1.9f), zone);
            SpawnFood("Avocado", new Vector2(188f, -1.0f), zone);
        }

        // =========================================================================
        // ZONE 6: CHEGADA (X: 195 to 230) - Finish Line & Podium
        // =========================================================================
        private void CreateZone6_Chegada_Legacy(Transform parent)
        {
            var zone = new GameObject("Z6_Chegada").transform;
            zone.SetParent(parent, false);

            // Ground & right containment wall
            CreateStaticBlock(
                "Z6_Ground",
                zone,
                new Vector2(212.5f, -1.8f),
                new Vector2(35f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f),
                StaticBlockKind.Ground
            );
            CreateStaticBlock(
                "Z6_RightBoundary",
                zone,
                new Vector2(228f, 0f),
                new Vector2(1f, 8f),
                new Color(0.08f, 0.32f, 0.25f),
                StaticBlockKind.Wall
            );

            CreateWorldLabel(
                "Z6 — CHEGADA",
                zone,
                new Vector2(215f, 2.8f),
                new Color(0.85f, 0.95f, 1f)
            );
            CreateWorldLabel("LINHA DE CHEGADA", zone, new Vector2(215f, -0.6f), Color.white);

            // Scenery props
            CreateSceneryProp(
                "Z6_Tree1",
                zone,
                new Vector2(200f, 0.4f),
                new Vector2(2.2f, 3.5f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z6_Tree2",
                zone,
                new Vector2(210f, 0.4f),
                new Vector2(2.2f, 3.5f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z6_Tree3",
                zone,
                new Vector2(225f, 0.4f),
                new Vector2(2.2f, 3.5f),
                GetScenerySprite(SceneryKind.Tree)
            );
            CreateSceneryProp(
                "Z6_Shrub1",
                zone,
                new Vector2(205f, -0.8f),
                new Vector2(1.4f, 1.1f),
                GetScenerySprite(SceneryKind.Shrub)
            );

            // Finish Line trigger
            _finishLine = CreateFinishLineObject("FinishLine", zone, new Vector2(FinishX, -0.5f));

            // Celebratory podium
            CreateStaticBlock(
                "Z6_Podium",
                zone,
                new Vector2(221f, -1.2f),
                new Vector2(4f, 0.4f),
                new Color(0.85f, 0.7f, 0.2f),
                StaticBlockKind.Platform
            );
            CreateWorldLabel("PÓDIO", zone, new Vector2(221f, -0.6f), new Color(1f, 0.9f, 0.3f));
        }

        // =========================================================================
        // HELPERS: Players, Camera, HUD, Collectibles, Checkpoints
        // =========================================================================
        private GameObject CreatePlayer(string playerName, Vector2 position, int playerIndex)
        {
            var player = Instantiate(_playerPrefab, position, Quaternion.identity, transform);
            player.name = playerName;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 2f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 2;
            }

            player.transform.localScale = new Vector3(0.85f, 1.25f, 1f);

            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.Configure(playerIndex);
            }

            var visualPrefab = playerIndex == 1 ? _playerOneVisualPrefab : _playerTwoVisualPrefab;
            if (visualPrefab != null)
            {
                var visualObj = Instantiate(visualPrefab, player.transform, false);
                visualObj.name = "VisualPresentation";
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localScale = new Vector3(1f / 0.85f, 1f / 1.25f, 1f);

                var presentation = visualObj.GetComponent<PlayerPresentation>();
                if (presentation != null)
                {
                    var wellbeing = player.GetComponent<WellbeingComponent>();
                    var fatigue = player.GetComponent<FatigueComponent>();
                    presentation.Bind(wellbeing, fatigue, controller);
                }

                if (sr != null)
                {
                    sr.enabled = false;
                }
            }
            else
            {
                CreateWorldLabel(playerName, player.transform, new Vector2(0f, 0.86f), Color.white);
            }

            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.Jumped += () => AudioService.Instance.PlaySfx(AudioClipId.Sfx_Jump);
            }

            var playerFatigue = player.GetComponent<FatigueComponent>();
            if (playerFatigue != null)
            {
                playerFatigue.StateChanged += (state) =>
                {
                    if (state == FatigueState.Fatigued)
                    {
                        AudioService.Instance.PlaySfx(AudioClipId.Sfx_Fatigue);
                    }
                };
            }

            var playerWellbeing = player.GetComponent<WellbeingComponent>();
            if (playerWellbeing != null)
            {
                playerWellbeing.WellbeingChanged += (prev, current) =>
                {
                    if (current > prev)
                    {
                        AudioService.Instance.PlaySfx(AudioClipId.Sfx_WellbeingUp);
                    }
                    else if (current < prev)
                    {
                        AudioService.Instance.PlaySfx(AudioClipId.Sfx_WellbeingDown);
                    }
                };
            }

            return player;
        }

        private void CreateSharedCamera(Transform p1, Transform p2)
        {
            var cameraObject = new GameObject(
                "Shared Camera",
                typeof(Camera),
                typeof(AudioListener)
            );
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.tag = "MainCamera";

            var cam = cameraObject.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.backgroundColor = new Color(0.07f, 0.17f, 0.29f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            _cameraController = cameraObject.AddComponent<SharedCameraController>();
            _cameraController.Configure(p1, p2);
            _cameraController.SetPlayableBounds(
                new Bounds(new Vector3(110f, 1f, 0f), new Vector3(250f, 18f, 20f)),
                true
            );
        }

        private void CreateHud(
            WellbeingComponent p1Wb,
            FatigueComponent p1Fat,
            WellbeingComponent p2Wb,
            FatigueComponent p2Fat
        )
        {
            var canvasObject = new GameObject(
                "HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            CreateCanvasText(
                "PhaseTitle",
                canvasObject.transform,
                "CORRIDA DO EQUILÍBRIO — FASE 1",
                24,
                FontStyle.Bold,
                Color.white,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -18f),
                new Vector2(560f, 32f),
                TextAnchor.UpperCenter
            );

            _matchHud = new GameObject("Match HUD", typeof(RectTransform)).AddComponent<MatchHUD>();
            _matchHud.Initialize(canvasObject.transform, new Vector2(0f, -54f));

            _playerOneHud = new GameObject(
                "P1 HUD",
                typeof(RectTransform)
            ).AddComponent<PlayerHUD>();
            _playerOneHud.Initialize(
                canvasObject.transform,
                "P1",
                new Color(0.15f, 0.67f, 1f),
                p1Wb,
                p1Fat,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(22f, -20f)
            );

            _playerTwoHud = new GameObject(
                "P2 HUD",
                typeof(RectTransform)
            ).AddComponent<PlayerHUD>();
            _playerTwoHud.Initialize(
                canvasObject.transform,
                "P2",
                new Color(0.35f, 1f, 0.55f),
                p2Wb,
                p2Fat,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-22f, -20f)
            );

            var resultsPanelObj = new GameObject("Match Results Panel", typeof(RectTransform));
            _resultsPanel = resultsPanelObj.AddComponent<MatchResultsPanel>();
            _resultsPanel.Initialize(canvasObject.transform);
            _resultsPanel.ReplayRequested += ReloadCurrentScene;

            var matchControllerObj = new GameObject("MatchCompletionController");
            matchControllerObj.transform.SetParent(transform, false);
            _matchController = matchControllerObj.AddComponent<MatchCompletionController>();
            _matchController.Configure(
                _finishLine,
                _matchHud,
                _playerOne,
                _playerTwo,
                p1Wb,
                p2Wb,
                _resultsPanel,
                20f,
                20,
                _playerOneHud,
                _playerTwoHud,
                _finalPuzzle
            );
            _matchController.MatchConcluded += (results) =>
            {
                if (results.WinnerPlayerIndex == 0)
                {
                    AudioService.Instance.PlaySfx(AudioClipId.Sfx_Tie);
                }
                else
                {
                    AudioService.Instance.PlaySfx(AudioClipId.Sfx_Victory);
                }
            };
        }

        private void OnDestroy()
        {
            if (_resultsPanel != null)
            {
                _resultsPanel.ReplayRequested -= ReloadCurrentScene;
            }
            Checkpoint.ResetRegistry();
        }

        private static void ReloadCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                SceneManager.LoadScene(scene.buildIndex);
            }
        }

        private void HandleFoodCollected(FoodCollectible collectible, WellbeingComponent collector)
        {
            if (collectible == null || collectible.FoodData == null)
            {
                return;
            }

            if (collectible.FoodData.Category == FoodCategory.Healthy)
            {
                AudioService.Instance.PlaySfx(AudioClipId.Sfx_PickupHealthy);
            }
            else
            {
                AudioService.Instance.PlaySfx(AudioClipId.Sfx_PickupUltra);
            }

            if (collector == _playerOneWellbeing && _playerOneHud != null)
            {
                _playerOneHud.RegisterFoodCollected(collectible.FoodData);
                _playerOneHud.AddScore(collectible.FoodData.BasePoints);
            }
            else if (collector == _playerTwoWellbeing && _playerTwoHud != null)
            {
                _playerTwoHud.RegisterFoodCollected(collectible.FoodData);
                _playerTwoHud.AddScore(collectible.FoodData.BasePoints);
            }

            if (_visualCatalog != null)
            {
                var kind =
                    collectible.FoodData.Category == FoodCategory.Healthy
                        ? FeedbackKind.HealthyPickup
                        : FeedbackKind.UltraProcessedPickup;
                VisualFeedback.Emit(_visualCatalog, kind, collectible.transform.position);
            }
        }

        private PushableBox CreatePushableBox(string name, Transform parent, Vector2 position)
        {
            if (_pushableBoxPrefab == null)
            {
                return null;
            }

            var box = Instantiate(_pushableBoxPrefab, position, Quaternion.identity, parent);
            box.name = name;
            box.transform.localScale = new Vector3(1.22f, 1.22f, 1f);

            var rb = box.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.mass = 1.2f;
                rb.gravityScale = 2.4f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            var sr = box.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (
                    _visualCatalog != null
                    && _visualCatalog.Environment != null
                    && _visualCatalog.Environment.Length > 7
                    && _visualCatalog.Environment[7] != null
                )
                {
                    sr.sprite = _visualCatalog.Environment[7];
                    sr.color = Color.white;
                }
                else
                {
                    sr.sprite = PlaceholderSpriteFactory.Square;
                    sr.color = new Color(0.72f, 0.39f, 0.13f);
                }
                sr.sortingOrder = 1;
            }

            return box.GetComponent<PushableBox>();
        }

        private PressureButton CreatePressureButtonObject(
            string name,
            Transform parent,
            Vector2 position
        )
        {
            var btn = new GameObject(name);
            btn.transform.SetParent(parent, false);
            btn.transform.position = position;

            var col = btn.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.6f, 0.3f);
            col.isTrigger = true;

            var sr = btn.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 8
                && _visualCatalog.Environment[8] != null
            )
            {
                sr.sprite = _visualCatalog.Environment[8];
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = new Color(0.9f, 0.25f, 0.25f);
            }
            sr.sortingOrder = 1;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(1.6f, 0.3f);

            var button = btn.AddComponent<PressureButton>();
            return button;
        }

        private Lever CreateLeverObject(
            string name,
            Transform parent,
            Vector2 position,
            string label = "ALAVANCA",
            bool isToggleable = true
        )
        {
            var leverObj = new GameObject(name);
            leverObj.transform.SetParent(parent, false);
            leverObj.transform.position = position;

            var col = leverObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.5f);
            col.isTrigger = true;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(leverObj.transform, false);
            handle.transform.localPosition = new Vector2(0f, 0.4f);
            handle.transform.localScale = new Vector3(0.25f, 0.9f, 1f);

            var handleRenderer = handle.AddComponent<SpriteRenderer>();
            handleRenderer.sprite = PlaceholderSpriteFactory.Square;
            handleRenderer.color = Color.white;
            handleRenderer.sortingOrder = 3;

            var baseObj = new GameObject("Base");
            baseObj.transform.SetParent(leverObj.transform, false);
            baseObj.transform.localPosition = new Vector2(0f, -0.2f);
            baseObj.transform.localScale = new Vector3(0.8f, 0.4f, 1f);

            var baseRenderer = baseObj.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 9
                && _visualCatalog.Environment[9] != null
            )
            {
                baseRenderer.sprite = _visualCatalog.Environment[9];
                baseRenderer.color = Color.white;
            }
            else
            {
                baseRenderer.sprite = PlaceholderSpriteFactory.Square;
                baseRenderer.color = new Color(0.3f, 0.3f, 0.35f);
            }
            baseRenderer.sortingOrder = 2;

            var lever = leverObj.AddComponent<Lever>();
            lever.Configure(handle.transform, baseRenderer, isToggleable);

            CreateWorldLabel(label, leverObj.transform, new Vector2(0f, 1.2f), Color.white);
            return lever;
        }

        private PuzzleDoor CreatePuzzleDoorObject(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size
        )
        {
            var doorObj = new GameObject(name);
            doorObj.transform.SetParent(parent, false);
            doorObj.transform.position = position;
            doorObj.layer = GroundLayer;

            var col = doorObj.AddComponent<BoxCollider2D>();
            col.size = size;

            var sr = doorObj.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 10
                && _visualCatalog.Environment[10] != null
            )
            {
                sr.sprite = _visualCatalog.Environment[10];
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = new Color(0.2f, 0.45f, 0.6f);
            }
            sr.sortingOrder = 1;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;

            var door = doorObj.AddComponent<PuzzleDoor>();
            door.Configure(new Vector2(0f, size.y + 0.5f), 4f);

            return door;
        }

        private ActivatedPlatform CreateActivatedPlatformObject(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Vector2 targetOffset
        )
        {
            var platObj = new GameObject(name);
            platObj.transform.SetParent(parent, false);
            platObj.transform.position = position;
            platObj.layer = GroundLayer;

            var col = platObj.AddComponent<BoxCollider2D>();
            col.size = size;

            var sr = platObj.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 1
                && _visualCatalog.Environment[1] != null
            )
            {
                sr.sprite = _visualCatalog.Environment[1];
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = new Color(0.18f, 0.72f, 0.52f);
            }
            sr.sortingOrder = 1;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;

            var platform = platObj.AddComponent<ActivatedPlatform>();
            platform.Configure(targetOffset, 3f);

            return platform;
        }

        private SpikeHazard CreateSpikeHazardObject(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            int penalty = 5,
            Vector2 defaultRespawn = default
        )
        {
            var spikeObj = new GameObject(name);
            spikeObj.transform.SetParent(parent, false);
            spikeObj.transform.position = position;

            var col = spikeObj.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            var sr = spikeObj.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 11
                && _visualCatalog.Environment[11] != null
            )
            {
                sr.sprite = _visualCatalog.Environment[11];
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = new Color(0.85f, 0.2f, 0.2f);
            }
            sr.sortingOrder = 2;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;

            var spike = spikeObj.AddComponent<SpikeHazard>();
            spike.Configure(penalty, 1.0f, defaultRespawn);

            return spike;
        }

        private MudHazard CreateMudHazardObject(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            float speedMultiplier = 0.7f
        )
        {
            var mudObj = new GameObject(name);
            mudObj.transform.SetParent(parent, false);
            mudObj.transform.position = position;

            var col = mudObj.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            var sr = mudObj.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 12
                && _visualCatalog.Environment[12] != null
            )
            {
                sr.sprite = _visualCatalog.Environment[12];
                sr.color = new Color(1f, 1f, 1f, 0.9f);
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = new Color(0.42f, 0.28f, 0.15f, 0.75f);
            }
            sr.sortingOrder = 2;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;

            var mud = mudObj.AddComponent<MudHazard>();
            mud.Configure(speedMultiplier);

            return mud;
        }

        private FallingPlatform CreateFallingPlatformObject(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            float anticipation = 0.5f,
            float respawnDelay = 3.0f
        )
        {
            var platObj = new GameObject(name);
            platObj.transform.SetParent(parent, false);
            platObj.transform.position = position;
            platObj.layer = GroundLayer;

            var col = platObj.AddComponent<BoxCollider2D>();
            col.size = size;

            var sr = platObj.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 1
                && _visualCatalog.Environment[1] != null
            )
            {
                sr.sprite = _visualCatalog.Environment[1];
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = new Color(0.75f, 0.55f, 0.35f);
            }
            sr.sortingOrder = 1;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;

            var platform = platObj.AddComponent<FallingPlatform>();
            platform.Configure(anticipation, respawnDelay);

            return platform;
        }

        private Checkpoint CreateCheckpointObject(
            string name,
            Transform parent,
            int index,
            Vector2 position
        )
        {
            var cp = new GameObject(name);
            cp.transform.SetParent(parent, false);
            cp.transform.position = position;

            var col = cp.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 3.5f);
            col.isTrigger = true;

            var flag = new GameObject("Flag");
            flag.transform.SetParent(cp.transform, false);
            flag.transform.localPosition = new Vector2(0f, 0.8f);
            flag.transform.localScale = new Vector3(0.8f, 1.8f, 1f);

            var sr = flag.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 5
                && _visualCatalog.Environment[5] != null
            )
            {
                sr.sprite = _visualCatalog.Environment[5];
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
            }
            sr.color = new Color(0.55f, 0.55f, 0.6f);
            sr.sortingOrder = 2;

            var checkpoint = cp.AddComponent<Checkpoint>();
            checkpoint.Configure(index, new Vector2(0f, 0.5f));
            checkpoint.SetVisualElements(
                sr,
                new Color(0.55f, 0.55f, 0.6f),
                new Color(0.25f, 0.85f, 0.35f)
            );

            checkpoint.CheckpointReached += (playerIndex) =>
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

            CreateWorldLabel($"CP {index}", cp.transform, new Vector2(0f, 2.0f), Color.yellow);
            return checkpoint;
        }

        private FinishLine CreateFinishLineObject(string name, Transform parent, Vector2 position)
        {
            var fl = new GameObject(name);
            fl.transform.SetParent(parent, false);
            fl.transform.position = position;

            var col = fl.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 5.0f);
            col.isTrigger = true;

            // Visual arch banners
            var banner = new GameObject("ArchBanner");
            banner.transform.SetParent(fl.transform, false);
            banner.transform.localPosition = new Vector2(0f, 2.2f);
            banner.transform.localScale = new Vector3(1.2f, 4.5f, 1f);

            var sr = banner.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 6
                && _visualCatalog.Environment[6] != null
            )
            {
                sr.sprite = _visualCatalog.Environment[6];
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = new Color(1f, 0.84f, 0f);
            }
            sr.sortingOrder = 2;

            var finishLine = fl.AddComponent<FinishLine>();
            finishLine.PlayerCrossedFinish += (playerIndex, finishTime) =>
            {
                AudioService.Instance.PlaySfx(AudioClipId.Sfx_Finish);
                if (_visualCatalog != null)
                {
                    VisualFeedback.Emit(_visualCatalog, FeedbackKind.Finish, fl.transform.position);
                }
            };

            return finishLine;
        }

        private void SpawnFood(string foodId, Vector2 position, Transform parent)
        {
            if (_collectiblePrefab == null)
            {
                return;
            }

            var foodData = GetFood(foodId);
            var item = Instantiate(_collectiblePrefab, position, Quaternion.identity, parent);
            item.name = $"Food_{foodId}";

            var collectible = item.GetComponent<FoodCollectible>();
            if (collectible != null)
            {
                if (foodData != null)
                {
                    collectible.Configure(foodData);
                }

                collectible.Collected += HandleFoodCollected;
            }
        }

        public enum StaticBlockKind
        {
            Ground,
            Platform,
            Wall,
        }

        public enum SceneryKind
        {
            Tree,
            Shrub,
        }

        private void CreateStaticBlock(
            string blockName,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Color color,
            StaticBlockKind kind = StaticBlockKind.Ground
        )
        {
            var block = new GameObject(blockName);
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.layer = GroundLayer;

            var sr = block.AddComponent<SpriteRenderer>();
            var sprite = GetEnvironmentSprite(kind);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
                if (_visualCatalog != null && _visualCatalog.SpriteMaterial != null)
                {
                    sr.sharedMaterial = _visualCatalog.SpriteMaterial;
                }
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = color;
            }

            sr.sortingOrder = 0;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Adaptive;
            sr.size = size;
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private Sprite GetEnvironmentSprite(StaticBlockKind kind)
        {
            if (_visualCatalog == null || _visualCatalog.Environment == null)
            {
                return null;
            }

            var index = kind switch
            {
                StaticBlockKind.Ground => 0,
                StaticBlockKind.Platform => 1,
                StaticBlockKind.Wall => 2,
                _ => 0,
            };

            return index < _visualCatalog.Environment.Length
                ? _visualCatalog.Environment[index]
                : null;
        }

        private Sprite GetScenerySprite(SceneryKind kind)
        {
            if (_visualCatalog == null || _visualCatalog.Environment == null)
            {
                return null;
            }

            var index = kind switch
            {
                SceneryKind.Tree => 3,
                SceneryKind.Shrub => 4,
                _ => 3,
            };

            return index < _visualCatalog.Environment.Length
                ? _visualCatalog.Environment[index]
                : null;
        }

        private void CreateSceneryProp(
            string propName,
            Transform parent,
            Vector2 position,
            Vector2 scale,
            Sprite sprite,
            int sortingOrder = -2
        )
        {
            if (sprite == null)
            {
                return;
            }

            var prop = new GameObject(propName);
            prop.transform.SetParent(parent, false);
            prop.transform.position = position;
            prop.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var sr = prop.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = Color.white;
            if (_visualCatalog != null && _visualCatalog.SpriteMaterial != null)
            {
                sr.sharedMaterial = _visualCatalog.SpriteMaterial;
            }
        }

        private static Text CreateCanvasText(
            string objectName,
            Transform parent,
            string value,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            TextAnchor alignment
        )
        {
            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var rectTransform = (RectTransform)textObject.transform;
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        private static void CreateWorldLabel(
            string text,
            Transform parent,
            Vector2 localPosition,
            Color color
        )
        {
            var labelObject = new GameObject("WorldLabel", typeof(MeshRenderer));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            labelObject.transform.localScale = new Vector3(0.08f, 0.08f, 1f);

            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.text = text;
            textMesh.fontSize = 28;
            textMesh.color = color;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;

            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.material = textMesh.font.material;
            renderer.sortingOrder = 5;
        }
    }
}
