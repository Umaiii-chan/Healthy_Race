using System;
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
using UnityEngine.UI;

namespace HealthyRace.Level
{
    /// <summary>
    /// Superfície horizontal onde um jogador pode ficar em pé. Usada pelo teste de
    /// percorribilidade para conferir desníveis e vãos contra o orçamento de pulo.
    /// </summary>
    public readonly struct ArenaSurface
    {
        public readonly string Name;
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float TopY;

        public ArenaSurface(string name, float minX, float maxX, float topY)
        {
            Name = name;
            MinX = minX;
            MaxX = maxX;
            TopY = topY;
        }
    }

    /// <summary>
    /// Ligação entre duas superfícies que só existe por causa de um mecanismo
    /// cooperativo (botão, alavanca, caixa, plataforma acionada).
    /// </summary>
    public readonly struct ArenaLink
    {
        public readonly string From;
        public readonly string To;
        public readonly string Mechanism;

        public ArenaLink(string from, string to, string mechanism)
        {
            From = from;
            To = to;
            Mechanism = mechanism;
        }
    }

    /// <summary>
    /// Bootstrap da Fase 1: Corrida do Equilíbrio.
    /// Constrói a arena de tela única (32 x 18) com cinco patamares empilhados,
    /// três portais cooperativos, perigos, coletáveis e duas portas de saída.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Phase1Bootstrap : MonoBehaviour
    {
        private const int GroundLayer = 6;

        // ---------------------------------------------------------------------
        // Enquadramento da arena (16:9, orthographicSize = 9)
        // ---------------------------------------------------------------------
        public const float ArenaMinX = 0f;
        public const float ArenaMaxX = 32f;
        public const float ArenaMinY = 0f;
        public const float ArenaMaxY = 18f;
        public const float CameraOrthographicSize = 9f;
        public static readonly Vector3 CameraPosition = new(16f, 9f, -10f);

        // ---------------------------------------------------------------------
        // Física da arena e intenção de projeto.
        //
        // A arena declara o ápice que quer (2.0 m) e deriva o gravityScale do
        // jogador a partir dele e da gravidade em vigor. Nenhum valor de
        // orçamento é literal: geometria, HUD e testes leem JumpBudget, que por
        // sua vez lê a física viva. Ver Assets/_Game/Scripts/Player/JumpBudget.cs.
        // ---------------------------------------------------------------------
        public const float PhaseGravityY = -18f;
        public const float TargetJumpApex = 2.0f;

        /// <summary>
        /// Passo de física assumido pelo orçamento em EditMode, onde
        /// <see cref="Time.fixedDeltaTime"/> pode não refletir o runtime.
        /// </summary>
        public const float FixedDeltaTime = 0.02f;
        public const float BoxHeight = 1.22f;

        // ---------------------------------------------------------------------
        // Patamares (topo da superfície)
        // ---------------------------------------------------------------------
        public const float Tier0Top = 1.0f;
        public const float Tier1Top = 4.4f;
        public const float Tier2Top = 7.8f;
        public const float Tier3Top = 11.2f;
        public const float Tier4Top = 14.6f;
        public static readonly float[] TierTops =
        {
            Tier0Top,
            Tier1Top,
            Tier2Top,
            Tier3Top,
            Tier4Top,
        };

        /// <summary>Marcos de progresso: T1, T2, T3, T4 e porta ocupada.</summary>
        public const int ProgressMilestones = 5;

        private const float LedgeThickness = 0.6f;
        private const float PlayerHalfHeight = 0.625f;
        private static readonly Vector2 PlayerOneSpawn = new(3.5f, Tier0Top + 0.7f);
        private static readonly Vector2 PlayerTwoSpawn = new(5.0f, Tier0Top + 0.7f);
        private static readonly Vector2 DefaultRespawn = new(4.25f, Tier0Top + 0.7f);

        private static readonly Color GroundColor = new(0.12f, 0.48f, 0.36f);
        private static readonly Color PlatformColor = new(0.16f, 0.63f, 0.47f);
        private static readonly Color WallColor = new(0.08f, 0.32f, 0.25f);
        private static readonly Color LabelColor = new(0.85f, 0.95f, 1f);
        private static readonly Color HintColor = new(0.95f, 0.95f, 0.7f);

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

        // Os ScriptableObject guardam o id em minúsculas ("apple", "hotdog") enquanto
        // o nível pede "Apple" e "HotDog". Sem o comparador que ignora caixa, todos os
        // 18 alimentos resolviam para nulo e nasciam sem sprite e sem efeito.
        private readonly Dictionary<string, FoodData> _foodLookup = new(
            StringComparer.OrdinalIgnoreCase
        );
        private readonly List<string> _unresolvedFoodIds = new();
        private readonly List<Checkpoint> _checkpoints = new();
        private readonly List<SpikeHazard> _spikes = new();
        private readonly List<MudHazard> _mudHazards = new();
        private readonly List<FallingPlatform> _fallingPlatforms = new();
        private readonly List<ArenaSurface> _surfaces = new();
        private readonly List<ArenaLink> _cooperativeLinks = new();
        private readonly List<CooperativePuzzleController> _gates = new();
        private readonly int[] _milestoneReached = new int[3];

        private FinishLine _playerOneDoor;
        private FinishLine _playerTwoDoor;
        private GameObject _playerOne;
        private GameObject _playerTwo;
        private PlayerMovement _playerOneMovement;
        private PlayerMovement _playerTwoMovement;
        private WellbeingComponent _playerOneWellbeing;
        private WellbeingComponent _playerTwoWellbeing;
        private PlayerHUD _playerOneHud;
        private PlayerHUD _playerTwoHud;
        private MatchHUD _matchHud;
        private SharedCameraController _cameraController;
        private CooperativePuzzleController _gateOne;
        private CooperativePuzzleController _gateOneReturn;
        private CooperativePuzzleController _gateTwo;
        private CooperativePuzzleController _gateThree;
        private ActivatedPlatform _gateOnePlatform;
        private PushableBox _gateThreeBox;
        private MatchCompletionController _matchController;
        private MatchResultsPanel _resultsPanel;
        private Transform _levelParent;

        public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints;
        public IReadOnlyList<SpikeHazard> Spikes => _spikes;
        public IReadOnlyList<MudHazard> MudHazards => _mudHazards;
        public IReadOnlyList<FallingPlatform> FallingPlatforms => _fallingPlatforms;

        /// <summary>Ids pedidos por <c>SpawnFood</c> que não existem no catálogo.</summary>
        public IReadOnlyList<string> UnresolvedFoodIds => _unresolvedFoodIds;
        public IReadOnlyList<ArenaSurface> Surfaces => _surfaces;
        public IReadOnlyList<ArenaLink> CooperativeLinks => _cooperativeLinks;
        public IReadOnlyList<CooperativePuzzleController> Gates => _gates;

        /// <summary>Porta de saída do P1 (mantido por compatibilidade com testes antigos).</summary>
        public FinishLine FinishLine => _playerOneDoor;
        public FinishLine PlayerOneDoor => _playerOneDoor;
        public FinishLine PlayerTwoDoor => _playerTwoDoor;
        public GameObject PlayerOne => _playerOne;
        public GameObject PlayerTwo => _playerTwo;
        public PlayerHUD PlayerOneHud => _playerOneHud;
        public PlayerHUD PlayerTwoHud => _playerTwoHud;
        public MatchHUD MatchHud => _matchHud;
        public SharedCameraController CameraController => _cameraController;
        public CooperativePuzzleController GateOne => _gateOne;
        public CooperativePuzzleController GateOneReturn => _gateOneReturn;
        public CooperativePuzzleController GateTwo => _gateTwo;
        public CooperativePuzzleController GateThree => _gateThree;
        public PushableBox GateThreeBox => _gateThreeBox;

        /// <summary>Portal persistente usado na pontuação (G2 fica resolvido após a alavanca).</summary>
        public CooperativePuzzleController FinalPuzzle => _gateTwo;
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
            Physics2D.gravity = new Vector2(0f, PhaseGravityY);

            CreateBackground();
            CreateZones();

            _playerOne = CreatePlayer("P1", PlayerOneSpawn, 1);
            _playerTwo = CreatePlayer("P2", PlayerTwoSpawn, 2);
            _playerOneMovement = _playerOne.GetComponent<PlayerMovement>();
            _playerTwoMovement = _playerTwo.GetComponent<PlayerMovement>();

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

            UpdateProgress(1, _playerOne, _playerOneMovement, _playerOneHud, _playerOneDoor);
            UpdateProgress(2, _playerTwo, _playerTwoMovement, _playerTwoHud, _playerTwoDoor);
            MonitorGateThreeBox();
        }

        // =====================================================================
        // PROGRESSO POR MARCOS (patamar alcançado + porta ocupada)
        // =====================================================================
        private void UpdateProgress(
            int playerIndex,
            GameObject player,
            PlayerMovement movement,
            PlayerHUD hud,
            FinishLine door
        )
        {
            if (player == null || hud == null)
            {
                return;
            }

            var reached = _milestoneReached[playerIndex];
            if (door != null && door.HasPlayerFinished(playerIndex))
            {
                reached = ProgressMilestones;
            }
            else if (IsStandingOnGround(player, movement))
            {
                // O marco só conta com o jogador apoiado: pular no lugar não avança
                // o progresso, por mais alto que seja o ápice.
                var feetY = player.transform.position.y - PlayerHalfHeight;
                reached = Mathf.Max(reached, GetTierIndex(feetY));
            }

            _milestoneReached[playerIndex] = reached;
            hud.UpdateProgress(GetProgress(playerIndex));
        }

        /// <summary>
        /// Verdadeiro apenas quando o jogador está efetivamente apoiado, conforme o
        /// <see cref="PlayerGroundCheck"/>. Sem componente de apoio não há marco.
        /// </summary>
        private static bool IsStandingOnGround(GameObject player, PlayerMovement movement)
        {
            var groundCheck = player.GetComponent<PlayerGroundCheck>();
            if (groundCheck != null)
            {
                return groundCheck.IsGrounded;
            }

            return movement != null && movement.IsGrounded;
        }

        /// <summary>Índice do patamar (0..4) cujo topo é o mais alto abaixo dos pés do jogador.</summary>
        public static int GetTierIndex(float feetY)
        {
            var tier = 0;
            for (var i = 0; i < TierTops.Length; i++)
            {
                if (feetY >= TierTops[i] - 0.1f)
                {
                    tier = i;
                }
            }

            return tier;
        }

        /// <summary>Progresso em etapas: 0/20/40/60/80/100%.</summary>
        public float GetProgress(int playerIndex)
        {
            if (playerIndex < 1 || playerIndex > 2)
            {
                return 0f;
            }

            return Mathf.Clamp01(_milestoneReached[playerIndex] / (float)ProgressMilestones);
        }

        public int GetMilestone(int playerIndex)
        {
            return playerIndex >= 1 && playerIndex <= 2 ? _milestoneReached[playerIndex] : 0;
        }

        /// <summary>Orçamento de pulo medido no jogador vivo.</summary>
        public JumpBudget BudgetFor(int playerIndex)
        {
            var movement = playerIndex == 1 ? _playerOneMovement : _playerTwoMovement;
            return JumpBudget.For(movement);
        }

        /// <summary>
        /// Orçamento de projeto da arena, disponível sem jogador instanciado
        /// (usado pelos testes de geometria em EditMode).
        /// </summary>
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

        private void MonitorGateThreeBox()
        {
            // Anti-deadlock: a caixa de G3 vive em T2. Se cair em qualquer buraco
            // (vão da AP3 aberta, borda do patamar), volta sozinha ao spawn.
            if (_gateThreeBox != null && _gateThreeBox.transform.position.y < Tier2Top - 1f)
            {
                _gateThreeBox.ResetToSpawn();
            }
        }

        private bool HasRequiredReferences()
        {
            return _playerPrefab != null && _collectiblePrefab != null;
        }

        private void BuildFoodLookup()
        {
            _foodLookup.Clear();
            _unresolvedFoodIds.Clear();
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

            var bg = new GameObject("Sky");
            bg.transform.SetParent(bgParent, false);
            bg.transform.position = new Vector3(
                (ArenaMinX + ArenaMaxX) * 0.5f,
                (ArenaMinY + ArenaMaxY) * 0.5f,
                5f
            );
            bg.transform.localScale = new Vector3(
                ArenaMaxX - ArenaMinX + 2f,
                ArenaMaxY - ArenaMinY + 2f,
                1f
            );

            var renderer = bg.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderSpriteFactory.Square;
            renderer.color = new Color(0.08f, 0.16f, 0.28f);
            renderer.sortingOrder = -10;
        }

        // =====================================================================
        // COMPOSIÇÃO DA ARENA
        // =====================================================================
        private void CreateZones()
        {
            _surfaces.Clear();
            _cooperativeLinks.Clear();
            _gates.Clear();

            _levelParent = new GameObject("LevelGeometry").transform;
            _levelParent.SetParent(transform, false);

            CreateArenaShell();
            CreateTier0_Base();
            CreateTier1();
            CreateTier2();
            CreateTier3();
            CreateTier4_Portas();
            CreateCoopGates();
            CreateHazards();
            CreateCollectibles();
        }

        private Transform CreateGroup(string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(_levelParent, false);
            return group;
        }

        /// <summary>
        /// Cria um bloco estático a partir de X mínimo/máximo e topo da superfície,
        /// registrando-o como superfície percorrível.
        /// </summary>
        private void CreateLedge(
            string name,
            Transform parent,
            float minX,
            float maxX,
            float topY,
            float thickness,
            Color color,
            StaticBlockKind kind
        )
        {
            var center = new Vector2((minX + maxX) * 0.5f, topY - thickness * 0.5f);
            var size = new Vector2(maxX - minX, thickness);
            CreateStaticBlock(name, parent, center, size, color, kind);
            RegisterSurface(name, minX, maxX, topY);
        }

        private void RegisterSurface(string name, float minX, float maxX, float topY)
        {
            _surfaces.Add(new ArenaSurface(name, minX, maxX, topY));
        }

        private void RegisterLink(string from, string to, string mechanism)
        {
            _cooperativeLinks.Add(new ArenaLink(from, to, mechanism));
        }

        // ---------------------------------------------------------------------
        // Casca: paredes, piso e teto. Nada existe fora de X [0, 32] x Y [0, 18].
        // ---------------------------------------------------------------------
        private void CreateArenaShell()
        {
            var shell = CreateGroup("Arena_Casca");

            CreateLedge(
                "T0_Piso",
                shell,
                ArenaMinX,
                ArenaMaxX,
                Tier0Top,
                1f,
                GroundColor,
                StaticBlockKind.Ground
            );

            CreateStaticBlock(
                "Arena_ParedeEsquerda",
                shell,
                new Vector2(-0.5f, ArenaMaxY * 0.5f),
                new Vector2(1f, ArenaMaxY + 2f),
                WallColor,
                StaticBlockKind.Wall
            );
            CreateStaticBlock(
                "Arena_ParedeDireita",
                shell,
                new Vector2(ArenaMaxX + 0.5f, ArenaMaxY * 0.5f),
                new Vector2(1f, ArenaMaxY + 2f),
                WallColor,
                StaticBlockKind.Wall
            );
            CreateStaticBlock(
                "Arena_Teto",
                shell,
                new Vector2((ArenaMinX + ArenaMaxX) * 0.5f, ArenaMaxY + 0.5f),
                new Vector2(ArenaMaxX - ArenaMinX + 2f, 1f),
                WallColor,
                StaticBlockKind.Wall
            );

            // O título já aparece no HUD; um rótulo de mundo no topo colidia com ele.
        }

        // ---------------------------------------------------------------------
        // T0 (topo 1.0): rota da tentação. Largada à esquerda, botão B1 e
        // plataforma AP1 à direita. Nicho da alavanca L2 em X [0, 2], fechado por
        // uma parede de 3 m (só se entra por cima, vindo de T1).
        // ---------------------------------------------------------------------
        private void CreateTier0_Base()
        {
            var tier = CreateGroup("T0_Base");

            // Nicho da alavanca L2 (G2). Parede de 3 m: intransponível por pulo.
            CreateStaticBlock(
                "T0_ParedeNicho",
                tier,
                new Vector2(2.25f, Tier0Top + 1.5f),
                new Vector2(0.5f, 3f),
                WallColor,
                StaticBlockKind.Wall
            );
            RegisterSurface("T0_Nicho", 0f, 2f, Tier0Top);

            CreateStaticBlock(
                "T0_MarcadorLargada",
                tier,
                new Vector2(4.25f, Tier0Top + 0.15f),
                new Vector2(2.4f, 0.1f),
                Color.white,
                StaticBlockKind.Wall
            );
            CreateWorldLabel("LARGADA", tier, new Vector2(4.25f, Tier0Top + 2.6f), Color.white);
            CreateWorldLabel(
                "Rota rápida: fast-food e lama",
                tier,
                new Vector2(16f, Tier0Top + 2.4f),
                HintColor
            );

            CreateSceneryProp(
                "T0_Arbusto1",
                tier,
                new Vector2(8f, Tier0Top + 0.5f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );
            CreateSceneryProp(
                "T0_Arbusto2",
                tier,
                new Vector2(21f, Tier0Top + 0.5f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );
        }

        // ---------------------------------------------------------------------
        // T1 (topo 4.4): ilha direita (chegada da AP1, CP1, botão B2), pedra,
        // ilha esquerda (botão de resgate B2r) e degrau S2 rumo a T2.
        // ---------------------------------------------------------------------
        private void CreateTier1()
        {
            var tier = CreateGroup("T1_Ilhas");

            CreateLedge(
                "T1_IlhaDireita",
                tier,
                19f,
                28f,
                Tier1Top,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );
            CreateLedge(
                "T1_Pedra",
                tier,
                13.5f,
                16.5f,
                Tier1Top,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );
            CreateLedge(
                "T1_IlhaEsquerda",
                tier,
                4f,
                9.5f,
                Tier1Top,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );
            // Degrau intermediário T1 -> T2: 1.6 m acima de T1, 1.8 m abaixo de T2.
            CreateLedge(
                "T1_DegrauS2",
                tier,
                10.5f,
                12.5f,
                6.0f,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );

            var cp1 = CreateCheckpointObject(
                "Checkpoint_1",
                tier,
                1,
                new Vector2(26f, Tier1Top + 1.2f)
            );
            _checkpoints.Add(cp1);

            CreateWorldLabel("T1", tier, new Vector2(23.5f, Tier1Top + 2.4f), LabelColor);
            CreateSceneryProp(
                "T1_Arbusto",
                tier,
                new Vector2(6f, Tier1Top + 0.5f),
                new Vector2(1.2f, 1f),
                GetScenerySprite(SceneryKind.Shrub)
            );
        }

        // ---------------------------------------------------------------------
        // T2 (topo 7.8): plataforma esquerda (CP2), pedra, portão D2 e o trecho
        // direito com a caixa, o botão B3 e a plataforma AP3 (G3).
        // ---------------------------------------------------------------------
        private void CreateTier2()
        {
            var tier = CreateGroup("T2_Rotas");

            CreateLedge(
                "T2_PlataformaEsquerda",
                tier,
                0f,
                10f,
                Tier2Top,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );
            CreateLedge(
                "T2_Pedra",
                tier,
                13.5f,
                15.5f,
                Tier2Top,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );
            CreateLedge(
                "T2_PlataformaDireitaA",
                tier,
                17f,
                23f,
                Tier2Top,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );
            CreateLedge(
                "T2_PlataformaDireitaB",
                tier,
                24.5f,
                32f,
                Tier2Top,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );

            var cp2 = CreateCheckpointObject(
                "Checkpoint_2",
                tier,
                2,
                new Vector2(5f, Tier2Top + 1.2f)
            );
            _checkpoints.Add(cp2);

            CreateWorldLabel("T2", tier, new Vector2(5.25f, Tier2Top + 2.4f), LabelColor);
            CreateWorldLabel(
                "Empurre a caixa até o botão",
                tier,
                new Vector2(28f, Tier2Top + 2.2f),
                HintColor
            );
        }

        // ---------------------------------------------------------------------
        // T3 (topo 11.2): plataforma direita (chegada da AP3, CP3), duas
        // plataformas que caem e plataforma esquerda com degrau S4 rumo a T4.
        // ---------------------------------------------------------------------
        private void CreateTier3()
        {
            var tier = CreateGroup("T3_Travessia");

            CreateLedge(
                "T3_PlataformaDireita",
                tier,
                24.5f,
                28f,
                Tier3Top,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );
            CreateLedge(
                "T3_PlataformaEsquerda",
                tier,
                4f,
                12f,
                Tier3Top,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );
            // Degrau intermediário T3 -> T4: 1.8 m acima de T3, 1.6 m abaixo de T4.
            CreateLedge(
                "T3_DegrauS4",
                tier,
                1f,
                3f,
                13.0f,
                LedgeThickness,
                PlatformColor,
                StaticBlockKind.Platform
            );

            var cp3 = CreateCheckpointObject(
                "Checkpoint_3",
                tier,
                3,
                new Vector2(26.25f, Tier3Top + 1.2f)
            );
            _checkpoints.Add(cp3);

            CreateWorldLabel("T3", tier, new Vector2(8f, Tier3Top + 2.4f), LabelColor);
        }

        // ---------------------------------------------------------------------
        // T4 (topo 14.6): patamar das portas. P1 e P2 têm portas próprias.
        // ---------------------------------------------------------------------
        private void CreateTier4_Portas()
        {
            var tier = CreateGroup("T4_Portas");

            CreateLedge(
                "T4_Portas",
                tier,
                5f,
                32f,
                Tier4Top,
                LedgeThickness,
                GroundColor,
                StaticBlockKind.Ground
            );

            _playerOneDoor = CreateFinishLineObject(
                "Porta_P1",
                tier,
                new Vector2(26.5f, Tier4Top + 1.6f),
                1,
                "PORTA P1",
                new Color(0.15f, 0.67f, 1f)
            );
            _playerTwoDoor = CreateFinishLineObject(
                "Porta_P2",
                tier,
                new Vector2(30f, Tier4Top + 1.6f),
                2,
                "PORTA P2",
                new Color(0.35f, 1f, 0.55f)
            );

            // Abaixo do cronômetro do HUD, que ocupa o topo central da tela.
            CreateWorldLabel(
                "CHEGADA — os dois nas portas",
                tier,
                new Vector2(14f, Tier4Top + 0.9f),
                Color.white
            );
            CreateSceneryProp(
                "T4_Arvore",
                tier,
                new Vector2(8f, Tier4Top + 1.6f),
                new Vector2(2f, 3.2f),
                GetScenerySprite(SceneryKind.Tree)
            );
        }

        // ---------------------------------------------------------------------
        // Portais cooperativos
        // ---------------------------------------------------------------------
        private void CreateCoopGates()
        {
            CreateGateOne();
            CreateGateTwo();
            CreateGateThree();
        }

        /// <summary>
        /// G1 (T0 -> T1): B1 no piso ergue a AP1 (1.6 -> 4.4) enquanto pressionado.
        /// B2 na ilha direita de T1 devolve o favor: quem já subiu segura B2 e a
        /// AP1 ergue o parceiro que embarcou nela.
        /// Saída de emergência: ambos os botões são momentâneos, e qualquer jogador
        /// pode voltar a T0 se deixando cair. Não há estado irreversível.
        /// </summary>
        private void CreateGateOne()
        {
            var gate = CreateGroup("G1_BotoesPlataforma");

            var b1 = CreatePressureButtonObject(
                "G1_BotaoB1",
                gate,
                new Vector2(27f, Tier0Top + 0.15f)
            );
            var b2 = CreatePressureButtonObject(
                "G1_BotaoB2",
                gate,
                new Vector2(20f, Tier1Top + 0.15f)
            );
            _gateOnePlatform = CreateActivatedPlatformObject(
                "G1_PlataformaAP1",
                gate,
                new Vector2(30f, Tier0Top + 0.4f),
                new Vector2(3f, 0.4f),
                new Vector2(0f, Tier1Top - (Tier0Top + 0.6f))
            );
            RegisterSurface("G1_AP1_Baixa", 28.5f, 31.5f, Tier0Top + 0.6f);
            RegisterSurface("G1_AP1_Alta", 28.5f, 31.5f, Tier1Top);
            RegisterLink(
                "G1_AP1_Baixa",
                "G1_AP1_Alta",
                "G1: parceiro segura B1 (T0) ou B2 (T1) e a AP1 sobe"
            );

            _gateOne = CreateGateController(
                "G1_Controlador",
                gate,
                b1,
                null,
                null,
                null,
                _gateOnePlatform,
                null
            );
            _gateOneReturn = CreateGateController(
                "G1_ControladorRetorno",
                gate,
                b2,
                null,
                null,
                null,
                _gateOnePlatform,
                null
            );

            // A plataforma obedece ao OU dos dois botões (os controladores rodam antes).
            void SyncPlatform(bool _)
            {
                _gateOnePlatform.SetState(_gateOne.IsSolved || _gateOneReturn.IsSolved);
            }

            _gateOne.OnPuzzleStateChanged += SyncPlatform;
            _gateOneReturn.OnPuzzleStateChanged += SyncPlatform;

            CreateWorldLabel(
                "G1: um segura, o outro sobe",
                gate,
                new Vector2(27f, Tier0Top + 1.6f),
                HintColor
            );
        }

        /// <summary>
        /// G2 (T1 -> T2): a alavanca L2 fica num nicho de T0 acessível só por cima
        /// (queda a partir da ilha esquerda de T1). Ao ser acionada abre o portão D2,
        /// que bloqueia a entrada do trecho direito de T2. Quem acionou não sobe
        /// sozinho: o parceiro segura o botão de resgate B2r na ilha esquerda e a
        /// AP2 do nicho o leva de volta a T1.
        /// Saída de emergência: espinhos no canto do nicho devolvem o jogador ao
        /// último checkpoint (custo de 5 de bem-estar). A alavanca não é
        /// alternável, então o portão nunca volta a fechar.
        /// </summary>
        private void CreateGateTwo()
        {
            var gate = CreateGroup("G2_AlavancaPortao");

            var lever = CreateLeverObject(
                "G2_AlavancaL2",
                gate,
                new Vector2(1.3f, Tier0Top + 1.3f),
                "ALAVANCA",
                false
            );
            var door = CreatePuzzleDoorObject(
                "G2_PortaoD2",
                gate,
                new Vector2(17.6f, Tier2Top + 1.1f),
                new Vector2(1.2f, 2.2f),
                instantMotion: true
            );
            RegisterLink(
                "T2_Pedra",
                "T2_PlataformaDireitaA",
                "G2: alavanca L2 (nicho em T0) abre o portão D2"
            );

            var rescueButton = CreatePressureButtonObject(
                "G2_BotaoResgateB2r",
                gate,
                new Vector2(7.5f, Tier1Top + 0.15f)
            );
            var rescuePlatform = CreateActivatedPlatformObject(
                "G2_PlataformaAP2",
                gate,
                new Vector2(1.3f, Tier0Top + 0.4f),
                new Vector2(1.2f, 0.4f),
                new Vector2(0f, Tier1Top - (Tier0Top + 0.6f))
            );
            RegisterSurface("G2_AP2_Baixa", 0.7f, 1.9f, Tier0Top + 0.6f);
            RegisterSurface("G2_AP2_Alta", 0.7f, 1.9f, Tier1Top);
            RegisterLink(
                "G2_AP2_Baixa",
                "G2_AP2_Alta",
                "G2: parceiro segura B2r (T1) e a AP2 resgata quem acionou a alavanca"
            );

            _gateTwo = CreateGateController(
                "G2_Controlador",
                gate,
                null,
                lever,
                null,
                door,
                null,
                null
            );
            var rescue = CreateGateController(
                "G2_ControladorResgate",
                gate,
                rescueButton,
                null,
                null,
                null,
                rescuePlatform,
                null
            );
            _gates.Add(rescue);

            CreateWorldLabel(
                "G2: alavanca no nicho",
                gate,
                new Vector2(1.3f, Tier0Top + 2.6f),
                HintColor
            );
        }

        /// <summary>
        /// G3 (T2 -> T3): a caixa nasce no trecho direito de T2 e é empurrada até o
        /// botão B3, ao lado do vão da AP3. Com B3 pressionado (por jogador ou pela
        /// caixa) a AP3 sobe de 7.8 para 10.8; de lá o degrau até T3 é de 0.4 m.
        /// Rota cooperativa: um segura B3 e o outro sobe. Rota anti-deadlock: a
        /// caixa sobre B3 trava a plataforma e serve de degrau (topo 9.02) para
        /// alcançar a AP3 erguida (desnível 1.78 m).
        /// Saída de emergência: alavanca de reset R3 devolve a caixa ao spawn e o
        /// bootstrap também a devolve se ela cair abaixo de T2.
        /// </summary>
        private void CreateGateThree()
        {
            var gate = CreateGroup("G3_CaixaPlataforma");

            var b3 = CreatePressureButtonObject(
                "G3_BotaoB3",
                gate,
                new Vector2(22f, Tier2Top + 0.15f)
            );
            var platform = CreateActivatedPlatformObject(
                "G3_PlataformaAP3",
                gate,
                new Vector2(23.75f, Tier2Top - LedgeThickness * 0.5f),
                new Vector2(1.5f, LedgeThickness),
                new Vector2(0f, 3f)
            );
            RegisterSurface("G3_AP3_Baixa", 23f, 24.5f, Tier2Top);
            RegisterSurface("G3_AP3_Alta", 23f, 24.5f, Tier2Top + 3f);
            RegisterLink(
                "G3_AP3_Baixa",
                "G3_AP3_Alta",
                "G3: parceiro segura B3 ou a caixa trava B3 e a AP3 sobe"
            );

            _gateThreeBox = CreatePushableBox(
                "G3_Caixa",
                gate,
                new Vector2(20f, Tier2Top + BoxHeight * 0.5f)
            );
            if (_gateThreeBox != null)
            {
                _gateThreeBox.SetSpawnPosition(new Vector2(20f, Tier2Top + BoxHeight * 0.5f));
                RegisterSurface("G3_CaixaSobreB3", 21.39f, 22.61f, Tier2Top + BoxHeight);
                RegisterLink(
                    "T2_PlataformaDireitaA",
                    "G3_CaixaSobreB3",
                    "G3: caixa empurrada sobre B3 vira degrau"
                );
            }

            var resetSwitch = CreateLeverObject(
                "G3_ResetR3",
                gate,
                new Vector2(30.5f, Tier2Top + 0.8f),
                "RESET",
                false
            );
            _gateThree = CreateGateController(
                "G3_Controlador",
                gate,
                b3,
                null,
                _gateThreeBox,
                null,
                platform,
                resetSwitch
            );

            CreateWorldLabel(
                "G3: caixa sobre o botão trava a plataforma",
                gate,
                new Vector2(20f, Tier2Top + 3.0f),
                HintColor
            );
        }

        private CooperativePuzzleController CreateGateController(
            string name,
            Transform parent,
            PressureButton button,
            Lever lever,
            PushableBox box,
            PuzzleDoor door,
            ActivatedPlatform platform,
            Lever resetSwitch
        )
        {
            var controllerObj = new GameObject(name);
            controllerObj.transform.SetParent(parent, false);
            var controller = controllerObj.AddComponent<CooperativePuzzleController>();
            controller.Bind(button, lever, box, door, platform, resetSwitch, requireAll: false);
            _gates.Add(controller);

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
                lever.OnToggled += (_) => AudioService.Instance.PlaySfx(AudioClipId.Sfx_Lever);
            }

            if (resetSwitch != null)
            {
                resetSwitch.OnToggled += (_) =>
                    AudioService.Instance.PlaySfx(AudioClipId.Sfx_Lever);
            }

            if (door != null)
            {
                door.OnOpened += () => AudioService.Instance.PlaySfx(AudioClipId.Sfx_Door);
            }

            controller.OnPuzzleSolved += () =>
                AudioService.Instance.PlaySfx(AudioClipId.Sfx_PuzzleSolved);
            return controller;
        }

        // ---------------------------------------------------------------------
        // Perigos: lama e espinhos na rota preguiçosa (T0), espinhos de emergência
        // no nicho e plataformas que caem na travessia de T3.
        // ---------------------------------------------------------------------
        private void CreateHazards()
        {
            var hazards = CreateGroup("Perigos");

            _mudHazards.Add(
                CreateMudHazardObject(
                    "T0_Lama1",
                    hazards,
                    new Vector2(17f, Tier0Top + 0.2f),
                    new Vector2(4f, 0.4f),
                    0.7f
                )
            );
            _mudHazards.Add(
                CreateMudHazardObject(
                    "T0_Lama2",
                    hazards,
                    new Vector2(24f, Tier0Top + 0.2f),
                    new Vector2(4f, 0.4f),
                    0.7f
                )
            );

            // Sob o vão entre as ilhas de T1: pune o pulo mal calculado.
            _spikes.Add(
                CreateSpikeHazardObject(
                    "T0_Espinhos",
                    hazards,
                    new Vector2(11.5f, Tier0Top + 0.2f),
                    new Vector2(3f, 0.4f),
                    5,
                    DefaultRespawn
                )
            );
            // Saída de emergência do nicho de G2.
            _spikes.Add(
                CreateSpikeHazardObject(
                    "T0_EspinhosNicho",
                    hazards,
                    new Vector2(0.25f, Tier0Top + 0.2f),
                    new Vector2(0.5f, 0.4f),
                    5,
                    DefaultRespawn
                )
            );

            _fallingPlatforms.Add(
                CreateFallingPlatformObject(
                    "T3_Queda1",
                    hazards,
                    new Vector2(19.5f, Tier3Top - LedgeThickness * 0.5f),
                    new Vector2(3f, LedgeThickness),
                    0.5f,
                    3f
                )
            );
            _fallingPlatforms.Add(
                CreateFallingPlatformObject(
                    "T3_Queda2",
                    hazards,
                    new Vector2(15f, Tier3Top - LedgeThickness * 0.5f),
                    new Vector2(3f, LedgeThickness),
                    0.5f,
                    3f
                )
            );
            RegisterSurface("T3_Queda1", 18f, 21f, Tier3Top);
            RegisterSurface("T3_Queda2", 13.5f, 16.5f, Tier3Top);
        }

        // ---------------------------------------------------------------------
        // Coletáveis: ultraprocessados enfileirados em T0; saudáveis em pontos
        // que exigem subir, desviar ou cooperar.
        // ---------------------------------------------------------------------
        private void CreateCollectibles()
        {
            var foods = CreateGroup("Coletaveis");
            var lowY = Tier0Top + 0.7f;

            SpawnFood("Burger", new Vector2(7f, lowY), foods);
            SpawnFood("Soda", new Vector2(9f, lowY), foods);
            SpawnFood("Fries", new Vector2(14.5f, lowY), foods);
            SpawnFood("Donut", new Vector2(17f, lowY), foods);
            SpawnFood("Pizza", new Vector2(19.5f, lowY), foods);
            SpawnFood("HotDog", new Vector2(22f, lowY), foods);
            SpawnFood("Chips", new Vector2(24f, lowY), foods);
            SpawnFood("Candy", new Vector2(26f, lowY), foods);

            SpawnFood("Apple", new Vector2(5.5f, Tier1Top + 0.7f), foods);
            SpawnFood("Carrot", new Vector2(8.8f, Tier1Top + 0.7f), foods);
            SpawnFood("Orange", new Vector2(15f, Tier1Top + 0.7f), foods);
            SpawnFood("Broccoli", new Vector2(2.5f, Tier2Top + 0.7f), foods);
            SpawnFood("Avocado", new Vector2(8f, Tier2Top + 0.7f), foods);
            SpawnFood("Water", new Vector2(14.5f, Tier2Top + 0.7f), foods);
            SpawnFood("Fish", new Vector2(8f, Tier3Top + 0.7f), foods);
            SpawnFood("Strawberry", new Vector2(15f, Tier3Top + 0.7f), foods);
            SpawnFood("Watermelon", new Vector2(10f, Tier4Top + 0.7f), foods);
            SpawnFood("Banana", new Vector2(18f, Tier4Top + 0.7f), foods);
        }

        // =========================================================================
        // HELPERS: Players, Camera, HUD, Collectibles, Checkpoints
        // =========================================================================
        private GameObject CreatePlayer(string playerName, Vector2 position, int playerIndex)
        {
            var player = Instantiate(_playerPrefab, position, Quaternion.identity, transform);
            player.name = playerName;

            var movement = player.GetComponent<PlayerMovement>();
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Deriva a gravidade do jogador do ápice de projeto em vez de fixar
                // um número: se a gravidade global ou a velocidade de pulo mudar, o
                // ápice continua sendo o declarado.
                var jumpVelocity = movement != null ? movement.JumpVelocity : 12f;
                rb.gravityScale = JumpBudget.GravityScaleForApex(
                    Physics2D.gravity.y,
                    jumpVelocity,
                    TargetJumpApex
                );
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
            cameraObject.transform.position = CameraPosition;
            cameraObject.tag = "MainCamera";

            var cam = cameraObject.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraOrthographicSize;
            cam.backgroundColor = new Color(0.07f, 0.17f, 0.29f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // Enquadramento fixo: a arena inteira cabe na tela, sem scroll nem zoom.
            _cameraController = cameraObject.AddComponent<SharedCameraController>();
            _cameraController.Configure(p1, p2);
            _cameraController.SetPlayableBounds(
                new Bounds(
                    new Vector3((ArenaMinX + ArenaMaxX) * 0.5f, (ArenaMinY + ArenaMaxY) * 0.5f, 0f),
                    new Vector3(ArenaMaxX - ArenaMinX, ArenaMaxY - ArenaMinY, 20f)
                ),
                true
            );
            _cameraController.SetLetterboxedFraming(
                CameraPosition,
                ArenaMaxX - ArenaMinX,
                ArenaMaxY - ArenaMinY
            );

            // A câmera com rect reduzido não limpa a tela inteira, então uma câmera de
            // fundo pinta as barras de preto e evita lixo gráfico nas bordas.
            var letterboxObject = new GameObject("Letterbox Camera", typeof(Camera));
            letterboxObject.transform.SetParent(transform, false);
            letterboxObject.transform.position = CameraPosition;

            var letterboxCamera = letterboxObject.GetComponent<Camera>();
            letterboxCamera.orthographic = true;
            letterboxCamera.orthographicSize = CameraOrthographicSize;
            letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
            letterboxCamera.backgroundColor = Color.black;
            letterboxCamera.cullingMask = 0;
            letterboxCamera.depth = cam.depth - 1;
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

            var matchControllerObj = new GameObject("MatchCompletionController");
            matchControllerObj.transform.SetParent(transform, false);
            _matchController = matchControllerObj.AddComponent<MatchCompletionController>();
            _matchController.Configure(
                _playerOneDoor,
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
                _gateTwo,
                secondFinishLine: _playerTwoDoor,
                requireBothAtDoors: true
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
            Checkpoint.ResetRegistry();
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
            Vector2 size,
            bool instantMotion = false
        )
        {
            var doorObj = new GameObject(name);
            doorObj.transform.SetParent(parent, false);
            doorObj.transform.position = position;
            doorObj.layer = GroundLayer;

            var col = doorObj.AddComponent<BoxCollider2D>();

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
            FitSpriteToSize(doorObj.transform, sr, col, size);

            var door = doorObj.AddComponent<PuzzleDoor>();
            door.Configure(new Vector2(0f, size.y + 0.5f), 4f, instantMotion);

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
            platObj.layer = GroundLayer;

            var col = platObj.AddComponent<BoxCollider2D>();

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
            FitSpriteToSize(platObj.transform, sr, col, size);

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
            FitSpriteToSize(spikeObj.transform, sr, col, size);

            var spike = spikeObj.AddComponent<SpikeHazard>();
            spike.Configure(penalty, 1.0f, defaultRespawn);
            CreateWorldLabel(
                "ESPINHOS",
                spikeObj.transform,
                new Vector2(0f, 0.6f / Mathf.Max(0.1f, size.y)),
                new Color(1f, 0.4f, 0.4f)
            );

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
            FitSpriteToSize(mudObj.transform, sr, col, size);

            var mud = mudObj.AddComponent<MudHazard>();
            mud.Configure(speedMultiplier);
            CreateWorldLabel(
                "LAMA (-30%)",
                mudObj.transform,
                new Vector2(0f, 0.6f / Mathf.Max(0.1f, size.y)),
                new Color(0.85f, 0.7f, 0.5f)
            );

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
            FitSpriteToSize(platObj.transform, sr, col, size);

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

        private FinishLine CreateFinishLineObject(
            string name,
            Transform parent,
            Vector2 position,
            int acceptedPlayerIndex = 0,
            string label = "LINHA DE CHEGADA",
            Color? bannerColor = null
        )
        {
            var fl = new GameObject(name);
            fl.transform.SetParent(parent, false);
            fl.transform.position = position;

            var col = fl.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 3.0f);
            col.isTrigger = true;

            // Portal visual
            var banner = new GameObject("ArchBanner");
            banner.transform.SetParent(fl.transform, false);
            banner.transform.localPosition = new Vector2(0f, 0.2f);
            banner.transform.localScale = new Vector3(1.4f, 3.0f, 1f);

            var sr = banner.AddComponent<SpriteRenderer>();
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > 6
                && _visualCatalog.Environment[6] != null
            )
            {
                sr.sprite = _visualCatalog.Environment[6];
                sr.color = bannerColor ?? Color.white;
            }
            else
            {
                sr.sprite = PlaceholderSpriteFactory.Square;
                sr.color = bannerColor ?? new Color(1f, 0.84f, 0f);
            }
            sr.sortingOrder = 2;

            var finishLine = fl.AddComponent<FinishLine>();
            finishLine.ConfigureAcceptedPlayer(acceptedPlayerIndex);
            finishLine.PlayerCrossedFinish += (playerIndex, finishTime) =>
            {
                AudioService.Instance.PlaySfx(AudioClipId.Sfx_Finish);
                if (_visualCatalog != null)
                {
                    VisualFeedback.Emit(_visualCatalog, FeedbackKind.Finish, fl.transform.position);
                }
            };

            CreateWorldLabel(label, fl.transform, new Vector2(0f, 2.0f), Color.white);
            return finishLine;
        }

        private void SpawnFood(string foodId, Vector2 position, Transform parent)
        {
            if (_collectiblePrefab == null)
            {
                return;
            }

            var foodData = GetFood(foodId);
            if (foodData == null)
            {
                // Falhar em silêncio aqui foi o que escondeu o bug de caixa dos ids.
                _unresolvedFoodIds.Add(foodId);
                Debug.LogError($"[Phase1Bootstrap] Alimento '{foodId}' não existe no catálogo.");
            }

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
            var collider = block.AddComponent<BoxCollider2D>();
            FitSpriteToSize(block.transform, sr, collider, size);
        }

        /// <summary>
        /// Ajusta escala e colisor para que o visual e a física tenham exatamente
        /// <paramref name="size"/> em unidades de mundo, independentemente do
        /// tamanho do sprite do atlas. Sem isso o BoxCollider2D auto-ajustado ao
        /// sprite ficaria maior que a geometria desenhada no projeto.
        /// </summary>
        private static void FitSpriteToSize(
            Transform target,
            SpriteRenderer renderer,
            BoxCollider2D collider,
            Vector2 size
        )
        {
            var spriteSize = Vector2.one;
            if (renderer != null && renderer.sprite != null)
            {
                var bounds = renderer.sprite.bounds.size;
                if (bounds.x > 0.0001f && bounds.y > 0.0001f)
                {
                    spriteSize = new Vector2(bounds.x, bounds.y);
                }
            }

            target.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
            if (collider != null)
            {
                collider.offset = Vector2.zero;
                collider.size = spriteSize;
            }
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
