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
        // Piso de cada corredor escavado. O espaçamento de 3.2 m excede o ápice de
        // 2.0 m de propósito: nenhum corredor alcança o de cima por pulo.
        public const float Tier0Top = 1.0f;
        public const float Tier1Top = 4.2f;
        public const float Tier2Top = 7.4f;
        public const float Tier3Top = 10.6f;
        public const float Tier4Top = 13.8f;

        /// <summary>Altura livre de cada corredor. O jogador tem 1.25 m.</summary>
        public const float CorridorClearance = 2.6f;

        /// <summary>Espessura das lajes de rocha entre corredores.</summary>
        public const float SlabThickness = 0.6f;

        /// <summary>Altura dos pilares internos, dentro do degrau seguro.</summary>
        public const float PillarHeight = 1.6f;

        /// <summary>Passagem livre sob os arcos pendurados no teto.</summary>
        public const float ArchOpening = 2.0f;

        // Poços verticais, escalonados para obrigar a percorrer cada corredor de
        // ponta a ponta. É o que dá o traçado de labirinto.
        public const float ShaftAMinX = 25.5f;
        public const float ShaftAMaxX = 28.5f;
        public const float ShaftBMinX = 3f;
        public const float ShaftBMaxX = 6f;
        public const float ShaftCMinX = 19.5f;
        public const float ShaftCMaxX = 22.5f;
        public const float ShaftDMinX = 8f;
        public const float ShaftDMaxX = 11f;

        /// <summary>
        /// Vão aberto em cada laje, na ordem das lajes 1 a 4. É por aqui, e só por
        /// aqui, que se passa de um corredor para o de cima: a rocha fecha o resto.
        /// </summary>
        public static readonly (float MinX, float MaxX)[] Shafts =
        {
            (ShaftAMinX, ShaftAMaxX),
            (ShaftBMinX, ShaftBMaxX),
            (ShaftCMinX, ShaftCMaxX),
            (ShaftDMinX, ShaftDMaxX),
        };

        /// <summary>Vão da travessia de plataformas que caem, no corredor C3.</summary>
        public const float FallGapMinX = 12f;
        public const float FallGapMaxX = 17f;
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
            CreateCorridorSlabs();
            CreateMazeDetail();
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
        /// Bloco de rocha maciça definido por extremos, não por centro e tamanho.
        /// É a peça básica do labirinto: tudo que não é corredor é rocha.
        /// </summary>
        private void CreateRock(
            string name,
            Transform parent,
            float minX,
            float maxX,
            float minY,
            float maxY
        )
        {
            var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            var size = new Vector2(maxX - minX, maxY - minY);
            CreateStaticBlock(name, parent, center, size, WallColor, StaticBlockKind.Wall);
        }

        /// <summary>Piso de corredor: rocha com a face superior em <paramref name="topY"/>.</summary>
        private void CreateCorridorFloor(
            string name,
            Transform parent,
            float minX,
            float maxX,
            float topY
        )
        {
            var center = new Vector2((minX + maxX) * 0.5f, topY - SlabThickness * 0.5f);
            var size = new Vector2(maxX - minX, SlabThickness);
            CreateStaticBlock(name, parent, center, size, GroundColor, StaticBlockKind.Ground);
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
        // Casca: paredes externas, rocha da base e do topo.
        // ---------------------------------------------------------------------
        private void CreateArenaShell()
        {
            var shell = CreateGroup("Arena_Casca");

            // Rocha da base, cuja face superior é o piso do corredor C0.
            CreateCorridorFloor("C0_Piso", shell, ArenaMinX, ArenaMaxX, Tier0Top);
            CreateRock(
                "Rocha_Base",
                shell,
                ArenaMinX,
                ArenaMaxX,
                ArenaMinY - 1f,
                Tier0Top - SlabThickness
            );

            // Rocha do topo, que fecha o teto do corredor das portas.
            CreateRock(
                "Rocha_Topo",
                shell,
                ArenaMinX,
                ArenaMaxX,
                Tier4Top + CorridorClearance,
                ArenaMaxY + 1f
            );

            CreateRock(
                "Arena_ParedeEsquerda",
                shell,
                ArenaMinX - 1f,
                ArenaMinX,
                ArenaMinY - 1f,
                ArenaMaxY + 1f
            );
            CreateRock(
                "Arena_ParedeDireita",
                shell,
                ArenaMaxX,
                ArenaMaxX + 1f,
                ArenaMinY - 1f,
                ArenaMaxY + 1f
            );

            CreateWorldLabel("LARGADA", shell, new Vector2(4.25f, Tier0Top + 2.1f), Color.white);
        }

        /// <summary>
        /// As lajes entre corredores. Cada laje é rocha de ponta a ponta, menos o
        /// poço vertical que a atravessa. Os poços ficam escalonados, o que obriga a
        /// percorrer cada corredor de uma ponta à outra e dá o traçado de labirinto.
        /// </summary>
        private void CreateCorridorSlabs()
        {
            var slabs = CreateGroup("Corredores_Lajes");

            CreateSlab(slabs, 1, Tier1Top, ShaftAMinX, ShaftAMaxX);
            CreateSlab(slabs, 2, Tier2Top, ShaftBMinX, ShaftBMaxX);
            // A laje 3 tem um segundo corte: o vão de 5.0 m que só as plataformas
            // que caem atravessam, já que excede o alcance de pulo.
            CreateSlab(slabs, 3, Tier3Top, ShaftCMinX, ShaftCMaxX, FallGapMinX, FallGapMaxX);
            CreateSlab(slabs, 4, Tier4Top, ShaftDMinX, ShaftDMaxX);

            CreateWorldLabel("C1", slabs, new Vector2(15f, Tier1Top + 1.9f), LabelColor);
            CreateWorldLabel("C2", slabs, new Vector2(15f, Tier2Top + 1.9f), LabelColor);
            CreateWorldLabel("C3", slabs, new Vector2(26f, Tier3Top + 1.9f), LabelColor);
            CreateWorldLabel("C4", slabs, new Vector2(16f, Tier4Top + 1.9f), LabelColor);
        }

        /// <summary>
        /// Uma laje: rocha dos dois lados do poço. A face superior de cada pedaço é
        /// piso do corredor de cima; a face inferior é o teto do corredor de baixo.
        /// </summary>
        private void CreateSlab(
            Transform parent,
            int index,
            float topY,
            float shaftMinX,
            float shaftMaxX,
            float extraGapMinX = 0f,
            float extraGapMaxX = 0f
        )
        {
            var bottomY = topY - SlabThickness;

            if (extraGapMaxX > extraGapMinX)
            {
                CreateCorridorFloor($"C{index}_PisoEsqA", parent, ArenaMinX, extraGapMinX, topY);
                CreateCorridorFloor($"C{index}_PisoEsqB", parent, extraGapMaxX, shaftMinX, topY);
            }
            else
            {
                CreateCorridorFloor($"C{index}_PisoEsq", parent, ArenaMinX, shaftMinX, topY);
            }

            CreateCorridorFloor($"C{index}_PisoDir", parent, shaftMaxX, ArenaMaxX, topY);

            // Ombreiras do poço, para o vão ficar legível como passagem escavada.
            CreateRock(
                $"Poco{index}_OmbreiraEsq",
                parent,
                shaftMinX - 0.3f,
                shaftMinX,
                bottomY - 0.5f,
                bottomY
            );
            CreateRock(
                $"Poco{index}_OmbreiraDir",
                parent,
                shaftMaxX,
                shaftMaxX + 0.3f,
                bottomY - 0.5f,
                bottomY
            );
        }

        /// <summary>
        /// O que transforma cinco corredores retos em labirinto: pilares baixos que
        /// se vencem com um pulo e arcos pendurados no teto que obrigam a passar por
        /// baixo. Nenhum deles fecha um corredor, então a travessia é sempre possível.
        /// </summary>
        private void CreateMazeDetail()
        {
            var detail = CreateGroup("Labirinto_Detalhe");

            // Pilares: 1.6 m de altura, dentro do degrau seguro.
            CreatePillar(detail, "C0_Pilar1", 8f, Tier0Top);
            CreatePillar(detail, "C0_Pilar2", 17.4f, Tier0Top);
            CreatePillar(detail, "C1_Pilar1", 10.6f, Tier1Top);
            CreatePillar(detail, "C1_Pilar2", 21f, Tier1Top);
            CreatePillar(detail, "C2_Pilar1", 25.4f, Tier2Top);
            CreatePillar(detail, "C3_Pilar1", 5.2f, Tier3Top);
            CreatePillar(detail, "C4_Pilar1", 20.4f, Tier4Top);

            // Arcos: rocha presa ao teto com 2.0 m de passagem livre embaixo.
            CreateArch(detail, "C0_Arco", 12.4f, 13.6f, Tier0Top);
            CreateArch(detail, "C1_Arco", 17.8f, 19f, Tier1Top);
            CreateArch(detail, "C2_Arco", 9.2f, 10.4f, Tier2Top);
            CreateArch(detail, "C3_Arco", 24.6f, 25.8f, Tier3Top);
        }

        private void CreatePillar(Transform parent, string name, float minX, float floorY)
        {
            CreateRock(name, parent, minX, minX + 0.8f, floorY, floorY + PillarHeight);
            RegisterSurface(name, minX, minX + 0.8f, floorY + PillarHeight);
        }

        private void CreateArch(Transform parent, string name, float minX, float maxX, float floorY)
        {
            CreateRock(name, parent, minX, maxX, floorY + ArchOpening, floorY + CorridorClearance);
        }

        // ---------------------------------------------------------------------
        // Portais cooperativos, um por poço, exceto o último.
        // ---------------------------------------------------------------------
        private void CreateCoopGates()
        {
            CreateGateOne();
            CreateGateTwo();
            CreateGateThree();
            CreateFinalClimb();
        }

        /// <summary>
        /// G1 (C0 -> C1), poço A. O botão B1 fica longe do poço, no meio do corredor
        /// C0: enquanto um jogador o mantém pressionado, a plataforma AP1 sobe com o
        /// outro. O botão B2, já em C1, devolve o favor.
        /// Saída de emergência: os dois botões são momentâneos e o poço fica aberto,
        /// então qualquer um pode se deixar cair de volta a C0. Nenhum estado trava.
        /// </summary>
        private void CreateGateOne()
        {
            var gate = CreateGroup("G1_BotoesPlataforma");

            var b1 = CreatePressureButtonObject(
                "G1_BotaoB1",
                gate,
                new Vector2(19f, Tier0Top + 0.15f)
            );
            var b2 = CreatePressureButtonObject(
                "G1_BotaoB2",
                gate,
                new Vector2(23f, Tier1Top + 0.15f)
            );

            _gateOnePlatform = CreateActivatedPlatformObject(
                "G1_PlataformaAP1",
                gate,
                new Vector2((ShaftAMinX + ShaftAMaxX) * 0.5f, Tier0Top + 0.3f),
                new Vector2(2.4f, 0.4f),
                new Vector2(0f, Tier1Top - (Tier0Top + 0.6f))
            );
            RegisterSurface("G1_AP1_Baixo", ShaftAMinX + 0.3f, ShaftAMaxX - 0.3f, Tier0Top + 0.6f);
            RegisterSurface("G1_AP1_Alto", ShaftAMinX + 0.3f, ShaftAMaxX - 0.3f, Tier1Top);
            RegisterLink(
                "G1_AP1_Baixo",
                "G1_AP1_Alto",
                "G1: parceiro segura B1 (C0) ou B2 (C1) e a AP1 sobe"
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

            void SyncPlatform(bool _)
            {
                _gateOnePlatform.SetState(_gateOne.IsSolved || _gateOneReturn.IsSolved);
            }

            _gateOne.OnPuzzleStateChanged += SyncPlatform;
            _gateOneReturn.OnPuzzleStateChanged += SyncPlatform;

            var cp1 = CreateCheckpointObject(
                "Checkpoint_1",
                gate,
                1,
                new Vector2(ShaftAMaxX + 1.5f, Tier1Top + 1f)
            );
            _checkpoints.Add(cp1);

            CreateWorldLabel(
                "G1: um segura, o outro sobe",
                gate,
                new Vector2(19f, Tier0Top + 1.6f),
                HintColor
            );
        }

        /// <summary>
        /// G2 (C1 -> C2), poço B. A alavanca L2 fica na ponta oposta do corredor C1 e
        /// ergue a plataforma AP2 de forma permanente. Quem puxa precisa atravessar o
        /// corredor inteiro, então os dois avançam juntos em vez de um esperar.
        /// Saída de emergência: a alavanca não é alternável, então a plataforma nunca
        /// volta a descer e o caminho fica aberto para sempre.
        /// </summary>
        private void CreateGateTwo()
        {
            var gate = CreateGroup("G2_AlavancaPlataforma");

            var lever = CreateLeverObject(
                "G2_AlavancaL2",
                gate,
                new Vector2(29f, Tier1Top + 0.8f),
                "ALAVANCA",
                false
            );

            var platform = CreateActivatedPlatformObject(
                "G2_PlataformaAP2",
                gate,
                new Vector2((ShaftBMinX + ShaftBMaxX) * 0.5f, Tier1Top + 0.3f),
                new Vector2(2.4f, 0.4f),
                new Vector2(0f, Tier2Top - (Tier1Top + 0.6f))
            );
            RegisterSurface("G2_AP2_Baixo", ShaftBMinX + 0.3f, ShaftBMaxX - 0.3f, Tier1Top + 0.6f);
            RegisterSurface("G2_AP2_Alto", ShaftBMinX + 0.3f, ShaftBMaxX - 0.3f, Tier2Top);
            RegisterLink(
                "G2_AP2_Baixo",
                "G2_AP2_Alto",
                "G2: alavanca L2 na outra ponta de C1 ergue a AP2"
            );

            _gateTwo = CreateGateController(
                "G2_Controlador",
                gate,
                null,
                lever,
                null,
                null,
                platform,
                null
            );

            var cp2 = CreateCheckpointObject(
                "Checkpoint_2",
                gate,
                2,
                new Vector2(ShaftBMaxX + 1.5f, Tier2Top + 1f)
            );
            _checkpoints.Add(cp2);

            CreateWorldLabel(
                "G2: alavanca no fim do corredor",
                gate,
                new Vector2(29f, Tier1Top + 1.9f),
                HintColor
            );
        }

        /// <summary>
        /// G3 (C2 -> C3), poço C. O botão B3 ergue a AP3 enquanto pressionado.
        /// Rota cooperativa: um segura B3 e o outro sobe. Rota anti-deadlock: a caixa
        /// empurrada sobre B3 trava a plataforma erguida e os dois sobem sem depender
        /// de sincronia. A alavanca R3 devolve a caixa ao spawn, e o bootstrap também
        /// a devolve se ela cair do corredor.
        /// </summary>
        private void CreateGateThree()
        {
            var gate = CreateGroup("G3_CaixaPlataforma");

            var b3 = CreatePressureButtonObject(
                "G3_BotaoB3",
                gate,
                new Vector2(15.5f, Tier2Top + 0.15f)
            );

            var platform = CreateActivatedPlatformObject(
                "G3_PlataformaAP3",
                gate,
                new Vector2((ShaftCMinX + ShaftCMaxX) * 0.5f, Tier2Top + 0.3f),
                new Vector2(2.4f, 0.4f),
                new Vector2(0f, Tier3Top - (Tier2Top + 0.6f))
            );
            RegisterSurface("G3_AP3_Baixo", ShaftCMinX + 0.3f, ShaftCMaxX - 0.3f, Tier2Top + 0.6f);
            RegisterSurface("G3_AP3_Alto", ShaftCMinX + 0.3f, ShaftCMaxX - 0.3f, Tier3Top);
            RegisterLink(
                "G3_AP3_Baixo",
                "G3_AP3_Alto",
                "G3: parceiro segura B3 ou a caixa trava B3 e a AP3 sobe"
            );

            _gateThreeBox = CreatePushableBox(
                "G3_Caixa",
                gate,
                new Vector2(12.5f, Tier2Top + BoxHeight * 0.5f)
            );
            if (_gateThreeBox != null)
            {
                _gateThreeBox.SetSpawnPosition(new Vector2(12.5f, Tier2Top + BoxHeight * 0.5f));
            }

            var resetSwitch = CreateLeverObject(
                "G3_ResetR3",
                gate,
                new Vector2(30f, Tier2Top + 0.8f),
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

            var cp3 = CreateCheckpointObject(
                "Checkpoint_3",
                gate,
                3,
                new Vector2(ShaftCMaxX + 1.5f, Tier3Top + 1f)
            );
            _checkpoints.Add(cp3);

            CreateWorldLabel(
                "G3: caixa sobre o botão trava a plataforma",
                gate,
                new Vector2(15.5f, Tier2Top + 1.9f),
                HintColor
            );
        }

        /// <summary>
        /// Subida final (C3 -> C4), poço D. Escada de rocha livre, sem mecanismo: os
        /// dois convergem e chegam juntos ao corredor das portas.
        /// </summary>
        private void CreateFinalClimb()
        {
            var climb = CreateGroup("C4_Portas");

            var stepMinX = ShaftDMinX + 0.6f;
            var stepMaxX = ShaftDMaxX - 0.6f;
            var stepTop = Tier3Top + 1.6f;
            CreateRock("PocoD_Degrau", climb, stepMinX, stepMaxX, stepTop - SlabThickness, stepTop);
            RegisterSurface("PocoD_Degrau", stepMinX, stepMaxX, stepTop);

            _playerOneDoor = CreateFinishLineObject(
                "Porta_P1",
                climb,
                new Vector2(26.5f, Tier4Top + 1.3f),
                1,
                "PORTA P1",
                new Color(0.15f, 0.67f, 1f)
            );
            _playerTwoDoor = CreateFinishLineObject(
                "Porta_P2",
                climb,
                new Vector2(30f, Tier4Top + 1.3f),
                2,
                "PORTA P2",
                new Color(0.35f, 1f, 0.55f)
            );

            var cp4 = CreateCheckpointObject(
                "Checkpoint_4",
                climb,
                4,
                new Vector2(ShaftDMaxX + 1.5f, Tier4Top + 1f)
            );
            _checkpoints.Add(cp4);

            CreateWorldLabel(
                "CHEGADA — os dois nas portas",
                climb,
                new Vector2(23f, Tier4Top + 0.4f),
                Color.white
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
        // Perigos: lama e espinhos na rota direta dos corredores baixos, e a
        // travessia de plataformas que caem no corredor C3.
        // ---------------------------------------------------------------------
        private void CreateHazards()
        {
            var hazards = CreateGroup("Perigos");

            _mudHazards.Add(
                CreateMudHazardObject(
                    "C0_Lama1",
                    hazards,
                    new Vector2(15f, Tier0Top + 0.2f),
                    new Vector2(3.6f, 0.4f),
                    0.7f
                )
            );
            _mudHazards.Add(
                CreateMudHazardObject(
                    "C0_Lama2",
                    hazards,
                    new Vector2(22f, Tier0Top + 0.2f),
                    new Vector2(3.6f, 0.4f),
                    0.7f
                )
            );
            _mudHazards.Add(
                CreateMudHazardObject(
                    "C1_Lama1",
                    hazards,
                    new Vector2(14f, Tier1Top + 0.2f),
                    new Vector2(3.2f, 0.4f),
                    0.7f
                )
            );

            // Entre os dois pilares de C0: é preciso calcular o pulo.
            _spikes.Add(
                CreateSpikeHazardObject(
                    "C0_Espinhos",
                    hazards,
                    new Vector2(10.4f, Tier0Top + 0.2f),
                    new Vector2(2.4f, 0.4f),
                    5,
                    DefaultRespawn
                )
            );

            // Travessia de C3: o vão de 5.0 m excede o alcance, então as duas
            // plataformas que caem são obrigatórias. Quem cair volta para C2 e sobe
            // de novo pela AP3, que continua travada pela caixa.
            _fallingPlatforms.Add(
                CreateFallingPlatformObject(
                    "C3_Queda1",
                    hazards,
                    new Vector2(13.35f, Tier3Top - SlabThickness * 0.5f),
                    new Vector2(1.7f, SlabThickness),
                    0.5f,
                    3f
                )
            );
            _fallingPlatforms.Add(
                CreateFallingPlatformObject(
                    "C3_Queda2",
                    hazards,
                    new Vector2(15.65f, Tier3Top - SlabThickness * 0.5f),
                    new Vector2(1.7f, SlabThickness),
                    0.5f,
                    3f
                )
            );
            RegisterSurface("C3_Queda1", 12.5f, 14.2f, Tier3Top);
            RegisterSurface("C3_Queda2", 14.8f, 16.5f, Tier3Top);
        }

        // ---------------------------------------------------------------------
        // Coletáveis: ultraprocessados na linha reta dos corredores baixos, onde
        // estão a lama e os espinhos; saudáveis em cima dos pilares e nas pontas
        // que exigem desviar do caminho.
        // ---------------------------------------------------------------------
        private void CreateCollectibles()
        {
            var foods = CreateGroup("Coletaveis");

            SpawnFood("Burger", new Vector2(6f, Tier0Top + 0.7f), foods);
            SpawnFood("Soda", new Vector2(12.9f, Tier0Top + 0.7f), foods);
            SpawnFood("Fries", new Vector2(15f, Tier0Top + 0.7f), foods);
            SpawnFood("Donut", new Vector2(22f, Tier0Top + 0.7f), foods);
            SpawnFood("Pizza", new Vector2(25f, Tier0Top + 0.7f), foods);
            SpawnFood("HotDog", new Vector2(14f, Tier1Top + 0.7f), foods);
            SpawnFood("Chips", new Vector2(18.4f, Tier1Top + 0.7f), foods);
            SpawnFood("Candy", new Vector2(27f, Tier1Top + 0.7f), foods);

            // Em cima dos pilares: exige um pulo extra.
            SpawnFood("Apple", new Vector2(8.4f, Tier0Top + PillarHeight + 0.6f), foods);
            SpawnFood("Carrot", new Vector2(11f, Tier1Top + PillarHeight + 0.6f), foods);
            SpawnFood("Orange", new Vector2(25.8f, Tier2Top + PillarHeight + 0.6f), foods);
            SpawnFood("Broccoli", new Vector2(5.6f, Tier3Top + PillarHeight + 0.6f), foods);

            // Nas pontas opostas ao caminho: exige atravessar o corredor inteiro.
            SpawnFood("Water", new Vector2(1.5f, Tier1Top + 0.7f), foods);
            SpawnFood("Avocado", new Vector2(30.5f, Tier2Top + 0.7f), foods);
            SpawnFood("Fish", new Vector2(1.5f, Tier2Top + 0.7f), foods);
            SpawnFood("Strawberry", new Vector2(30.5f, Tier3Top + 0.7f), foods);
            SpawnFood("Watermelon", new Vector2(2f, Tier3Top + 0.7f), foods);
            SpawnFood("Banana", new Vector2(3f, Tier4Top + 0.7f), foods);
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
