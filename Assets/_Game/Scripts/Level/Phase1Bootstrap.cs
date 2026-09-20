using System.Collections.Generic;
using HealthyRace.Collectibles;
using HealthyRace.Core;
using HealthyRace.Obstacles;
using HealthyRace.Player;
using HealthyRace.Puzzles;
using HealthyRace.UI;
using HealthyRace.WellBeing;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Level
{
    /// <summary>
    /// Level bootstrap and orchestrator for Phase 1: Corrida do Equilíbrio.
    /// Builds the 6-zone blockout, spawns players, camera, HUD, checkpoints,
    /// puzzles, and educational food distribution.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Phase1Bootstrap : MonoBehaviour
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

        private readonly Dictionary<string, FoodData> _foodLookup = new();
        private readonly List<Checkpoint> _checkpoints = new();
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

        public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints;
        public FinishLine FinishLine => _finishLine;
        public GameObject PlayerOne => _playerOne;
        public GameObject PlayerTwo => _playerTwo;
        public PlayerHUD PlayerOneHud => _playerOneHud;
        public PlayerHUD PlayerTwoHud => _playerTwoHud;
        public MatchHUD MatchHud => _matchHud;
        public CooperativePuzzleController FinalPuzzle => _finalPuzzle;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                return;
            }

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

            for (var x = -10f; x <= 240f; x += 30f)
            {
                var bg = new GameObject($"SkySegment_{x}");
                bg.transform.SetParent(bgParent, false);
                bg.transform.position = new Vector3(x, 2f, 5f);
                bg.transform.localScale = new Vector3(32f, 22f, 1f);

                var renderer = bg.AddComponent<SpriteRenderer>();
                renderer.sprite = PlaceholderSpriteFactory.Square;
                renderer.color = new Color(0.08f, 0.16f, 0.28f);
                renderer.sortingOrder = -10;
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
        private void CreateZone1_Spawn(Transform parent)
        {
            var zone = new GameObject("Z1_Spawn").transform;
            zone.SetParent(parent, false);

            // Ground & left boundary wall
            CreateStaticBlock(
                "Z1_Ground",
                zone,
                new Vector2(13.5f, -3.4f),
                new Vector2(37f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f)
            );
            CreateStaticBlock(
                "Z1_LeftBoundary",
                zone,
                new Vector2(-5.5f, 0f),
                new Vector2(1f, 8f),
                new Color(0.08f, 0.32f, 0.25f)
            );

            // Starting line marker
            CreateStaticBlock(
                "Z1_StartMarker",
                zone,
                new Vector2(0f, -2.6f),
                new Vector2(0.3f, 0.8f),
                Color.white
            );
            CreateWorldLabel("LARGADA", zone, new Vector2(0f, -1.8f), Color.white);
            CreateWorldLabel(
                "Z1 — SPAWN",
                zone,
                new Vector2(13.5f, -1.8f),
                new Color(0.85f, 0.95f, 1f)
            );

            // Introductory low step platforms (jump training: heights 1.2m, gaps 3-4m)
            CreateStaticBlock(
                "Z1_Platform1",
                zone,
                new Vector2(10f, -2.2f),
                new Vector2(4f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f)
            );
            CreateStaticBlock(
                "Z1_Platform2",
                zone,
                new Vector2(18f, -1.4f),
                new Vector2(4f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f)
            );
            CreateStaticBlock(
                "Z1_Platform3",
                zone,
                new Vector2(25f, -2.2f),
                new Vector2(4f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f)
            );

            // Introductory healthy collectibles
            SpawnFood("Apple", new Vector2(10f, -1.4f), zone);
            SpawnFood("Banana", new Vector2(18f, -0.6f), zone);
            SpawnFood("Water", new Vector2(25f, -1.4f), zone);
        }

        // =========================================================================
        // ZONE 2: CAIXA (X: 32 to 65)
        // =========================================================================
        private void CreateZone2_Caixa(Transform parent)
        {
            var zone = new GameObject("Z2_Caixa").transform;
            zone.SetParent(parent, false);

            // Ground before cliff
            CreateStaticBlock(
                "Z2_GroundLower",
                zone,
                new Vector2(41f, -3.4f),
                new Vector2(18f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f)
            );

            // Elevated cliff ledge: height -0.6 (2.8m above lower ground, impassable by single jump)
            CreateStaticBlock(
                "Z2_CliffLedge",
                zone,
                new Vector2(57.5f, -0.6f),
                new Vector2(15f, 0.6f),
                new Color(0.16f, 0.63f, 0.47f)
            );
            CreateStaticBlock(
                "Z2_CliffWall",
                zone,
                new Vector2(50f, -2.0f),
                new Vector2(1f, 2.2f),
                new Color(0.09f, 0.34f, 0.31f)
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

            // Pushable crate
            CreatePushableBox("Z2_PushableBox", zone, new Vector2(42f, -2.25f));

            // Elevated perch with reward foods
            CreateStaticBlock(
                "Z2_Perch",
                zone,
                new Vector2(58f, 1.2f),
                new Vector2(4f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f)
            );
            SpawnFood("Carrot", new Vector2(58f, 1.9f), zone);
            SpawnFood("Orange", new Vector2(54f, 0.1f), zone);
        }

        // =========================================================================
        // ZONE 3: ROTAS (X: 65 to 125) - Split routes & Checkpoint 1
        // =========================================================================
        private void CreateZone3_Rotas(Transform parent)
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
                new Color(0.2f, 0.7f, 0.5f)
            );
            SpawnFood("Broccoli", new Vector2(74f, 2.3f), upper);

            CreateStaticBlock(
                "Upper_P2",
                upper,
                new Vector2(83f, 2.4f),
                new Vector2(4.5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f)
            );
            SpawnFood("Strawberry", new Vector2(83f, 3.1f), upper);

            CreateStaticBlock(
                "Upper_P3",
                upper,
                new Vector2(91f, 3.0f),
                new Vector2(4.5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f)
            );
            SpawnFood("Watermelon", new Vector2(91f, 3.7f), upper);

            CreateStaticBlock(
                "Upper_P4",
                upper,
                new Vector2(99f, 2.4f),
                new Vector2(4.5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f)
            );
            SpawnFood("Avocado", new Vector2(99f, 3.1f), upper);

            CreateStaticBlock(
                "Upper_P5",
                upper,
                new Vector2(107f, 1.6f),
                new Vector2(5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f)
            );
            SpawnFood("Fish", new Vector2(107f, 2.3f), upper);

            CreateStaticBlock(
                "Upper_P6",
                upper,
                new Vector2(116f, 0.6f),
                new Vector2(5f, 0.4f),
                new Color(0.2f, 0.7f, 0.5f)
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
                new Color(0.12f, 0.48f, 0.36f)
            );

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
        private void CreateZone4_Reencontro(Transform parent)
        {
            var zone = new GameObject("Z4_Reencontro").transform;
            zone.SetParent(parent, false);

            // Reconnecting transition ramp from lower ground
            CreateStaticBlock(
                "Z4_TransitionStep",
                zone,
                new Vector2(123.5f, -2.6f),
                new Vector2(5f, 0.8f),
                new Color(0.14f, 0.55f, 0.42f)
            );

            // Main reunion ground plateau at Y = -1.8
            CreateStaticBlock(
                "Z4_Ground",
                zone,
                new Vector2(142.5f, -1.8f),
                new Vector2(35f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f)
            );

            CreateWorldLabel(
                "Z4 — REENCONTRO",
                zone,
                new Vector2(142.5f, -0.2f),
                new Color(0.85f, 0.95f, 1f)
            );

            // Checkpoint 2 at start of reunion area
            var cp2 = CreateCheckpointObject("Checkpoint_2", zone, 2, new Vector2(130f, -0.8f));
            _checkpoints.Add(cp2);

            // Hydrating and restorative fruit
            SpawnFood("Water", new Vector2(138f, -1.0f), zone);
            SpawnFood("Apple", new Vector2(146f, -1.0f), zone);
            SpawnFood("Orange", new Vector2(154f, -1.0f), zone);
        }

        // =========================================================================
        // ZONE 5: PUZZLE FINAL (X: 160 to 195) - Cooperative Challenge & Anti-Deadlock
        // =========================================================================
        private void CreateZone5_PuzzleFinal(Transform parent)
        {
            var zone = new GameObject("Z5_PuzzleFinal").transform;
            zone.SetParent(parent, false);

            // Ground before gate
            CreateStaticBlock(
                "Z5_Ground1",
                zone,
                new Vector2(167f, -1.8f),
                new Vector2(14f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f)
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
                new Color(0.12f, 0.48f, 0.36f)
            );

            CreateWorldLabel(
                "Z5 — PUZZLE FINAL",
                zone,
                new Vector2(175f, 2.8f),
                new Color(0.85f, 0.95f, 1f)
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

            // Anti-deadlock bypass stepping stones (allows jump-clearing even if puzzle is skipped)
            CreateStaticBlock(
                "Z5_Step1",
                zone,
                new Vector2(167f, 0.2f),
                new Vector2(3f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f)
            );
            CreateStaticBlock(
                "Z5_Step2",
                zone,
                new Vector2(175f, 1.2f),
                new Vector2(3f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f)
            );
            CreateStaticBlock(
                "Z5_Step3",
                zone,
                new Vector2(182f, 0.2f),
                new Vector2(3f, 0.4f),
                new Color(0.16f, 0.63f, 0.47f)
            );

            SpawnFood("Carrot", new Vector2(175f, 1.9f), zone);
            SpawnFood("Avocado", new Vector2(188f, -1.0f), zone);
        }

        // =========================================================================
        // ZONE 6: CHEGADA (X: 195 to 230) - Finish Line & Podium
        // =========================================================================
        private void CreateZone6_Chegada(Transform parent)
        {
            var zone = new GameObject("Z6_Chegada").transform;
            zone.SetParent(parent, false);

            // Ground & right containment wall
            CreateStaticBlock(
                "Z6_Ground",
                zone,
                new Vector2(212.5f, -1.8f),
                new Vector2(35f, 0.8f),
                new Color(0.12f, 0.48f, 0.36f)
            );
            CreateStaticBlock(
                "Z6_RightBoundary",
                zone,
                new Vector2(228f, 0f),
                new Vector2(1f, 8f),
                new Color(0.08f, 0.32f, 0.25f)
            );

            CreateWorldLabel(
                "Z6 — CHEGADA",
                zone,
                new Vector2(215f, 2.8f),
                new Color(0.85f, 0.95f, 1f)
            );
            CreateWorldLabel("LINHA DE CHEGADA", zone, new Vector2(215f, -0.6f), Color.white);

            // Finish Line trigger
            _finishLine = CreateFinishLineObject("FinishLine", zone, new Vector2(FinishX, -0.5f));

            // Celebratory podium
            CreateStaticBlock(
                "Z6_Podium",
                zone,
                new Vector2(221f, -1.2f),
                new Vector2(4f, 0.4f),
                new Color(0.85f, 0.7f, 0.2f)
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

            CreateWorldLabel(playerName, player.transform, new Vector2(0f, 0.86f), Color.white);
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
        }

        private void OnDestroy()
        {
            Checkpoint.ResetRegistry();
        }

        private void HandleFoodCollected(FoodCollectible collectible, WellbeingComponent collector)
        {
            if (collectible == null || collectible.FoodData == null)
            {
                return;
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
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = new Color(0.72f, 0.39f, 0.13f);
                sr.sortingOrder = 1;
            }

            CreateWorldLabel("CAIXA", box.transform, new Vector2(0f, 0.72f), Color.white);
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
            sr.sprite = PlaceholderSpriteFactory.Square;
            sr.color = new Color(0.9f, 0.25f, 0.25f);
            sr.sortingOrder = 1;
            btn.transform.localScale = new Vector3(1.6f, 0.25f, 1f);

            var button = btn.AddComponent<PressureButton>();
            CreateWorldLabel("BOTÃO", btn.transform, new Vector2(0f, 0.5f), Color.white);
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
            baseRenderer.sprite = PlaceholderSpriteFactory.Square;
            baseRenderer.color = new Color(0.3f, 0.3f, 0.35f);
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
            doorObj.transform.localScale = new Vector3(size.x, size.y, 1f);
            doorObj.layer = GroundLayer;

            var col = doorObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var sr = doorObj.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSpriteFactory.Square;
            sr.color = new Color(0.2f, 0.45f, 0.6f);
            sr.sortingOrder = 1;

            var door = doorObj.AddComponent<PuzzleDoor>();
            door.Configure(new Vector2(0f, size.y + 0.5f), 4f);

            CreateWorldLabel("PORTÃO", doorObj.transform, new Vector2(0f, 0f), Color.cyan);
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
            platObj.transform.localScale = new Vector3(size.x, size.y, 1f);
            platObj.layer = GroundLayer;

            var col = platObj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var sr = platObj.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSpriteFactory.Square;
            sr.color = new Color(0.18f, 0.72f, 0.52f);
            sr.sortingOrder = 1;

            var platform = platObj.AddComponent<ActivatedPlatform>();
            platform.Configure(targetOffset, 3f);

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
            flag.transform.localScale = new Vector3(0.4f, 1.8f, 1f);

            var sr = flag.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSpriteFactory.Square;
            sr.color = new Color(0.55f, 0.55f, 0.6f);
            sr.sortingOrder = 2;

            var checkpoint = cp.AddComponent<Checkpoint>();
            checkpoint.Configure(index, new Vector2(0f, 0.5f));
            checkpoint.SetVisualElements(
                sr,
                new Color(0.55f, 0.55f, 0.6f),
                new Color(0.25f, 0.85f, 0.35f)
            );

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
            banner.transform.localScale = new Vector3(0.6f, 4.5f, 1f);

            var sr = banner.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSpriteFactory.Square;
            sr.color = new Color(1f, 0.84f, 0f);
            sr.sortingOrder = 2;

            return fl.AddComponent<FinishLine>();
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

        private static void CreateStaticBlock(
            string blockName,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Color color
        )
        {
            var block = new GameObject(blockName);
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);
            block.layer = GroundLayer;

            var sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSpriteFactory.Square;
            sr.color = color;
            sr.sortingOrder = 0;

            block.AddComponent<BoxCollider2D>();
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
