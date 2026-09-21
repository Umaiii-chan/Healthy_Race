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
    /// <summary>Elevador de uso individual entre dois corredores, em colunas e linhas do mapa.</summary>
    public readonly struct MazeElevator
    {
        public readonly int MinColumn;
        public readonly int MaxColumn;
        public readonly int BottomRow;
        public readonly int TopRow;

        public MazeElevator(int minColumn, int maxColumn, int bottomRow, int topRow)
        {
            MinColumn = minColumn;
            MaxColumn = maxColumn;
            BottomRow = bottomRow;
            TopRow = topRow;
        }
    }

    /// <summary>Portão que bloqueia uma coluna do mapa entre duas linhas até a alavanca ser acionada.</summary>
    public readonly struct MazeDoor
    {
        public readonly int Column;
        public readonly int MinRow;
        public readonly int MaxRow;

        public MazeDoor(int column, int minRow, int maxRow)
        {
            Column = column;
            MinRow = minRow;
            MaxRow = maxRow;
        }
    }

    /// <summary>Subida que só existe com a caixa encostada no muro: da célula de origem à do topo.</summary>
    public readonly struct MazeBoxClimb
    {
        public readonly Vector2Int From;
        public readonly Vector2Int To;

        public MazeBoxClimb(Vector2Int from, Vector2Int to)
        {
            From = from;
            To = to;
        }
    }

    /// <summary>Alimento colocado numa célula do mapa.</summary>
    public readonly struct MazeFood
    {
        public readonly string Id;
        public readonly Vector2Int Cell;

        public MazeFood(string id, Vector2Int cell)
        {
            Id = id;
            Cell = cell;
        }
    }

    /// <summary>
    /// Bootstrap da Fase 1: Corrida do Equilíbrio.
    /// Constrói um labirinto competitivo de tela única (32 x 18 ladrilhos): duas
    /// metades espelhadas escavadas na rocha, uma largada por jogador nos cantos
    /// de baixo e uma saída única no centro do topo. Quem chega primeiro vence.
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

        /// <summary>
        /// Ápice de pulo. Os corredores têm 3 m livres e o jogador 1.25 m: com 1.65 m
        /// a cabeça passa 10 cm abaixo do teto e nenhum salto bate. Os degraus são de
        /// 1 m e os vãos de até 3 ladrilhos, ambos dentro deste orçamento.
        /// </summary>
        public const float TargetJumpApex = 1.65f;

        /// <summary>
        /// Passo de física assumido pelo orçamento em EditMode, onde
        /// <see cref="Time.fixedDeltaTime"/> pode não refletir o runtime.
        /// </summary>
        public const float FixedDeltaTime = 0.02f;
        public const float BoxHeight = 1.22f;

        // ---------------------------------------------------------------------
        // Mapa do labirinto. Ladrilhos de 1 m; '#' é rocha e '.' é túnel. A linha
        // de cima é o topo da arena. A metade direita é o espelho da esquerda, para
        // as duas rotas terem a mesma dificuldade.
        //
        // Corredores (piso): A y=1, B y=5, C y=9, D y=13. O espaçamento de 4 m
        // excede o ápice de 2 m: subir depende de escada, elevador ou caixa.
        // ---------------------------------------------------------------------
        public const int MazeColumns = 32;
        public const int MazeRowCount = 18;
        public const float TileSize = 1f;

        public static readonly string[] MazeRows =
        {
            "################################",
            "###########.########.###########",
            "####........................####",
            "####........................####",
            "####........................####",
            "####..#..####.####.####..#..####",
            "####...........##...........####",
            "####....#......##......#....####",
            "####....#......##......#....####",
            "#############..##..#############",
            "#########......##......#########",
            "########.......##.......########",
            "#######....#...##...#....#######",
            "##.#.#...##############...#.#.##",
            "#.......################.......#",
            "#......##################......#",
            "##....####################....##",
            "################################",
        };

        /// <summary>Piso de cada corredor, de baixo para cima. Também são os marcos de progresso.</summary>
        public const float Tier0Top = 1f;
        public const float Tier1Top = 5f;
        public const float Tier2Top = 9f;
        public const float Tier3Top = 13f;
        public static readonly float[] TierTops = { Tier0Top, Tier1Top, Tier2Top, Tier3Top };

        /// <summary>Altura livre dos corredores em ladrilhos. O jogador tem 1.25 m.</summary>
        public const float CorridorClearance = 3f;

        /// <summary>Marcos de progresso: corredores B, C e D alcançados, e saída cruzada.</summary>
        public const int ProgressMilestones = 4;

        /// <summary>X da saída única, no centro do topo.</summary>
        public const float ExitX = 16f;

        public static readonly Vector2Int PlayerOneSpawnCell = new(2, 1);
        public static readonly Vector2Int PlayerTwoSpawnCell = new(29, 1);
        public static readonly Vector2Int[] ExitCells = { new(15, 13), new(16, 13) };

        private const float PlayerHalfHeight = 0.625f;
        private static readonly Vector2 PlayerOneSpawn = new(
            (PlayerOneSpawnCell.x + 0.5f) * TileSize,
            PlayerOneSpawnCell.y * TileSize + 0.75f
        );
        private static readonly Vector2 PlayerTwoSpawn = new(
            (PlayerTwoSpawnCell.x + 0.5f) * TileSize,
            PlayerTwoSpawnCell.y * TileSize + 0.75f
        );

        private static readonly Color GroundColor = new(0.12f, 0.48f, 0.36f);
        private static readonly Color PlatformColor = new(0.16f, 0.63f, 0.47f);
        private static readonly Color WallColor = new(0.08f, 0.32f, 0.25f);
        private static readonly Color TunnelColor = new(0.05f, 0.08f, 0.12f);

        private static PhysicsMaterial2D _playerPhysicsMaterial;

        /// <summary>Sem atrito e sem quique: o jogador desliza em paredes em vez de grudar.</summary>
        private static PhysicsMaterial2D PlayerPhysicsMaterial =>
            _playerPhysicsMaterial ??= new PhysicsMaterial2D("Jogador")
            {
                friction = 0f,
                bounciness = 0f,
            };
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
        private readonly List<CooperativePuzzleController> _gates = new();
        private readonly List<MazeElevator> _elevators = new();
        private readonly List<MazeDoor> _doors = new();
        private readonly List<MazeBoxClimb> _boxClimbs = new();
        private readonly List<MazeFood> _foodPlacements = new();
        private readonly List<PushableBox> _boxes = new();
        private readonly int[] _milestoneReached = new int[3];

        private FinishLine _finishLine;
        private Checkpoint _playerOneSpawnCheckpoint;
        private Checkpoint _playerTwoSpawnCheckpoint;
        private CooperativePuzzleController _leftDoorGate;
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
        private MatchCompletionController _matchController;
        private MatchResultsPanel _resultsPanel;
        private Transform _levelParent;

        public IReadOnlyList<Checkpoint> Checkpoints => _checkpoints;
        public IReadOnlyList<SpikeHazard> Spikes => _spikes;
        public IReadOnlyList<MudHazard> MudHazards => _mudHazards;
        public IReadOnlyList<FallingPlatform> FallingPlatforms => _fallingPlatforms;

        /// <summary>Ids pedidos por <c>SpawnFood</c> que não existem no catálogo.</summary>
        public IReadOnlyList<string> UnresolvedFoodIds => _unresolvedFoodIds;
        public IReadOnlyList<CooperativePuzzleController> Gates => _gates;
        public IReadOnlyList<MazeElevator> Elevators => _elevators;
        public IReadOnlyList<MazeDoor> Doors => _doors;
        public IReadOnlyList<MazeBoxClimb> BoxClimbs => _boxClimbs;
        public IReadOnlyList<MazeFood> FoodPlacements => _foodPlacements;
        public IReadOnlyList<PushableBox> Boxes => _boxes;

        /// <summary>Saída única da corrida, aceita os dois jogadores.</summary>
        public FinishLine FinishLine => _finishLine;

        /// <summary>Mantidos por compatibilidade: na corrida os dois jogadores usam a mesma saída.</summary>
        public FinishLine PlayerOneDoor => _finishLine;
        public FinishLine PlayerTwoDoor => _finishLine;
        public Checkpoint PlayerOneSpawnCheckpoint => _playerOneSpawnCheckpoint;
        public Checkpoint PlayerTwoSpawnCheckpoint => _playerTwoSpawnCheckpoint;
        public GameObject PlayerOne => _playerOne;
        public GameObject PlayerTwo => _playerTwo;
        public PlayerHUD PlayerOneHud => _playerOneHud;
        public PlayerHUD PlayerTwoHud => _playerTwoHud;
        public MatchHUD MatchHud => _matchHud;
        public SharedCameraController CameraController => _cameraController;

        /// <summary>Puzzle usado na pontuação: o portão da metade esquerda fica resolvido após a alavanca.</summary>
        public CooperativePuzzleController FinalPuzzle => _leftDoorGate;
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

            // Cada jogador nasce com o próprio ponto de retorno: morrer nos espinhos
            // antes de qualquer checkpoint devolve cada um à sua largada.
            _playerOneSpawnCheckpoint?.ActivateForPlayer(1);
            _playerTwoSpawnCheckpoint?.ActivateForPlayer(2);

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

            UpdateProgress(1, _playerOne, _playerOneMovement, _playerOneHud, _finishLine);
            UpdateProgress(2, _playerTwo, _playerTwoMovement, _playerTwoHud, _finishLine);
            MonitorBoxes();
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

        /// <summary>Progresso em etapas: 0/25/50/75/100%.</summary>
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

        /// <summary>
        /// Não há céu: o labirinto é rocha escavada. Os túneis são a cor de fundo da
        /// câmera, e a rocha vem dos blocos do mapa com visual ladrilhado.
        /// </summary>
        private void CreateBackground()
        {
            var bgParent = new GameObject("Background").transform;
            bgParent.SetParent(transform, false);
        }

        // =====================================================================
        // COMPOSIÇÃO DO LABIRINTO
        // =====================================================================
        private void CreateZones()
        {
            _gates.Clear();
            _elevators.Clear();
            _doors.Clear();
            _boxClimbs.Clear();
            _foodPlacements.Clear();
            _boxes.Clear();

            _levelParent = new GameObject("LevelGeometry").transform;
            _levelParent.SetParent(transform, false);

            CreateArenaShell();
            CreateMazeRock();
            CreateMechanisms();
            CreateCheckpoints();
            CreateExit();
            CreateHazards();
            CreateCollectibles();
            CreateLabels();
        }

        private Transform CreateGroup(string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(_levelParent, false);
            return group;
        }

        /// <summary>Coluna espelhada: a metade direita é o reflexo da esquerda.</summary>
        public static int MirrorColumn(int column)
        {
            return MazeColumns - 1 - column;
        }

        /// <summary>Coordenada X espelhada em unidades de mundo.</summary>
        public static float MirrorX(float x)
        {
            return ArenaMaxX - x;
        }

        /// <summary>Verdadeiro se o ladrilho (coluna, linha) é rocha. Linha 0 é a base.</summary>
        public static bool IsRock(int column, int row)
        {
            if (column < 0 || column >= MazeColumns || row < 0 || row >= MazeRowCount)
            {
                return true;
            }

            return MazeRows[MazeRowCount - 1 - row][column] == '#';
        }

        /// <summary>
        /// Bloco de rocha maciça definido por extremos, não por centro e tamanho.
        /// Colisor exato e visual ladrilhado.
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

        // ---------------------------------------------------------------------
        // Casca: paredes externas além da arena. A rocha da base e do topo já
        // está no mapa (linhas 0, 16 e 17).
        // ---------------------------------------------------------------------
        private void CreateArenaShell()
        {
            var shell = CreateGroup("Arena_Casca");
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
        }

        // ---------------------------------------------------------------------
        // A rocha do labirinto vem do mapa de ladrilhos. Cada sequência horizontal
        // de '#' numa linha vira um bloco com colisor exato e visual ladrilhado.
        // Tudo que não é rocha é túnel: o labirinto é o espaço negativo.
        // ---------------------------------------------------------------------
        // ---------------------------------------------------------------------
        // Mecanismos, um conjunto por metade. Numa corrida nenhum mecanismo pode
        // depender do outro jogador: elevadores com botão na própria plataforma,
        // portão aberto por alavanca no caminho e caixa para vencer um muro.
        // ---------------------------------------------------------------------
        private void CreateMechanisms()
        {
            CreateHalfMechanisms("Esq", mirrored: false);
            CreateHalfMechanisms("Dir", mirrored: true);
        }

        /// <summary>
        /// Constrói os mecanismos de uma metade. As colunas autoradas são da metade
        /// esquerda; a direita recebe as mesmas colunas espelhadas.
        /// </summary>
        private void CreateHalfMechanisms(string suffix, bool mirrored)
        {
            var group = CreateGroup($"Mecanismos_{suffix}");

            // Elevador 1: do corredor B (y=5) ao corredor C (y=9), colunas 13-14.
            CreateElevator($"Elevador1_{suffix}", group, 13, 14, 5, 9, mirrored);

            // Elevador 2: do corredor C (y=9) ao corredor D (y=13), colunas 4-5.
            CreateElevator($"Elevador2_{suffix}", group, 4, 5, 9, 13, mirrored);

            // Portão no corredor C (coluna 12), aberto pela alavanca do degrau de B
            // (coluna 11). Quem passa reto pela alavanca encontra o portão fechado e
            // volta: é o custo de navegação do labirinto.
            var leverX = ColumnCenter(11, mirrored);
            var lever = CreateLeverObject(
                $"Alavanca_{suffix}",
                group,
                new Vector2(leverX, 6f + 0.75f),
                "ALAVANCA",
                false
            );
            var doorColumn = mirrored ? MirrorColumn(12) : 12;
            var door = CreatePuzzleDoorObject(
                $"Portao_{suffix}",
                group,
                new Vector2(ColumnCenter(12, mirrored), 9f + 1.3f),
                new Vector2(0.8f, 2.6f),
                instantMotion: true,
                openOffset: Vector2.zero
            );
            _doors.Add(new MazeDoor(doorColumn, 9, 11));
            var doorGate = CreateGateController(
                $"Portao_{suffix}_Controlador",
                group,
                null,
                lever,
                null,
                door,
                null,
                null
            );
            if (!mirrored)
            {
                _leftDoorGate = doorGate;
            }

            // Caixa no corredor C: empurrada até o muro de 2 m da coluna 8, vira degrau
            // (1.22 m) para alcançar o topo do muro (0.78 m).
            var boxX = ColumnCenter(11, mirrored);
            var box = CreatePushableBox(
                $"Caixa_{suffix}",
                group,
                new Vector2(boxX, 9f + BoxHeight * 0.5f)
            );
            if (box != null)
            {
                box.SetSpawnPosition(new Vector2(boxX, 9f + BoxHeight * 0.5f));
                _boxes.Add(box);
            }

            var fromColumn = mirrored ? MirrorColumn(9) : 9;
            var toColumn = mirrored ? MirrorColumn(8) : 8;
            _boxClimbs.Add(
                new MazeBoxClimb(new Vector2Int(fromColumn, 9), new Vector2Int(toColumn, 11))
            );
        }

        private static float ColumnCenter(int column, bool mirrored)
        {
            var c = mirrored ? MirrorColumn(column) : column;
            return (c + 0.5f) * TileSize;
        }

        /// <summary>
        /// Elevador de uso individual: a plataforma sobe enquanto o próprio passageiro
        /// pisa no botão montado nela, e desce sozinha quando ele sai no topo. Assim
        /// cada jogador usa o elevador sem depender do outro, e quem cair de volta
        /// encontra a plataforma embaixo de novo.
        /// </summary>
        private void CreateElevator(
            string name,
            Transform parent,
            int minColumn,
            int maxColumn,
            int bottomRow,
            int topRow,
            bool mirrored
        )
        {
            var c0 = mirrored ? MirrorColumn(maxColumn) : minColumn;
            var c1 = mirrored ? MirrorColumn(minColumn) : maxColumn;
            var minX = c0 * TileSize;
            var maxX = (c1 + 1) * TileSize;
            var bottomY = bottomRow * TileSize;
            var topY = topRow * TileSize;
            var width = maxX - minX;
            var centerX = (minX + maxX) * 0.5f;

            // Raiz sem escala: o botão filho não pode herdar deformação.
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(centerX, bottomY - 0.2f, 0f);
            root.layer = GroundLayer;

            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width, 0.4f);

            var sprite = SpriteOrPlaceholder(1, out var tint, PlatformColor);
            CreateTiledStrip(
                "Visual",
                root.transform,
                sprite,
                tint,
                minX,
                maxX,
                bottomY - 0.4f,
                bottomY,
                1
            );

            var platform = root.AddComponent<ActivatedPlatform>();
            platform.Configure(new Vector2(0f, topY - bottomY), 3f);

            var button = CreatePressureButtonObject(
                $"{name}_Botao",
                root.transform,
                new Vector2(centerX, bottomY + 0.15f),
                width
            );
            CreateGateController(
                $"{name}_Controlador",
                parent,
                button,
                null,
                null,
                null,
                platform,
                null
            );

            _elevators.Add(new MazeElevator(c0, c1, bottomRow, topRow));
            CreateWorldLabel("ELEVADOR", root.transform, new Vector2(0f, 0.9f), HintColor);
        }

        // ---------------------------------------------------------------------
        // Checkpoints por metade: largada (ativado de saída para o dono da metade),
        // topo da escada, corredor C e corredor D.
        // ---------------------------------------------------------------------
        private void CreateCheckpoints()
        {
            var group = CreateGroup("Checkpoints");

            _playerOneSpawnCheckpoint = CreateCheckpointObject(
                "Checkpoint_Largada_P1",
                group,
                1,
                new Vector2(2.5f, 2.3f)
            );
            _checkpoints.Add(_playerOneSpawnCheckpoint);
            _checkpoints.Add(
                CreateCheckpointObject("Checkpoint_Esq_B", group, 2, new Vector2(9.5f, 6.2f))
            );
            _checkpoints.Add(
                CreateCheckpointObject("Checkpoint_Esq_C", group, 3, new Vector2(10.5f, 10.2f))
            );
            _checkpoints.Add(
                CreateCheckpointObject("Checkpoint_Esq_D", group, 4, new Vector2(6.5f, 14.2f))
            );

            _playerTwoSpawnCheckpoint = CreateCheckpointObject(
                "Checkpoint_Largada_P2",
                group,
                5,
                new Vector2(MirrorX(2.5f), 2.3f)
            );
            _checkpoints.Add(_playerTwoSpawnCheckpoint);
            _checkpoints.Add(
                CreateCheckpointObject(
                    "Checkpoint_Dir_B",
                    group,
                    6,
                    new Vector2(MirrorX(9.5f), 6.2f)
                )
            );
            _checkpoints.Add(
                CreateCheckpointObject(
                    "Checkpoint_Dir_C",
                    group,
                    7,
                    new Vector2(MirrorX(10.5f), 10.2f)
                )
            );
            _checkpoints.Add(
                CreateCheckpointObject(
                    "Checkpoint_Dir_D",
                    group,
                    8,
                    new Vector2(MirrorX(6.5f), 14.2f)
                )
            );
        }

        // ---------------------------------------------------------------------
        // Saída única no centro do topo: quem chega primeiro vence.
        // ---------------------------------------------------------------------
        private void CreateExit()
        {
            var group = CreateGroup("Saida");
            _finishLine = CreateFinishLineObject(
                "Saida",
                group,
                new Vector2(ExitX, 13f + 1.3f),
                0,
                "SAÍDA",
                new Color(1f, 0.84f, 0f)
            );
        }

        // ---------------------------------------------------------------------
        // Perigos na rota direta, espelhados: lama em A e D, espinhos em B e D.
        // ---------------------------------------------------------------------
        private void CreateHazards()
        {
            var hazards = CreateGroup("Perigos");

            foreach (var mirrored in new[] { false, true })
            {
                var suffix = mirrored ? "Dir" : "Esq";
                var respawn = mirrored ? PlayerTwoSpawn : PlayerOneSpawn;

                _mudHazards.Add(
                    CreateMudHazardObject(
                        $"Lama_A_{suffix}",
                        hazards,
                        new Vector2(RangeCenter(3, 4, mirrored), 1f + 0.2f),
                        new Vector2(2f, 0.4f),
                        0.7f
                    )
                );
                _mudHazards.Add(
                    CreateMudHazardObject(
                        $"Lama_D_{suffix}",
                        hazards,
                        new Vector2(RangeCenter(9, 10, mirrored), 13f + 0.2f),
                        new Vector2(2f, 0.4f),
                        0.7f
                    )
                );

                _spikes.Add(
                    CreateSpikeHazardObject(
                        $"Espinhos_B_{suffix}",
                        hazards,
                        new Vector2(ColumnCenter(12, mirrored), 5f + 0.2f),
                        new Vector2(1f, 0.4f),
                        5,
                        respawn
                    )
                );
                _spikes.Add(
                    CreateSpikeHazardObject(
                        $"Espinhos_D_{suffix}",
                        hazards,
                        new Vector2(ColumnCenter(12, mirrored), 13f + 0.2f),
                        new Vector2(1f, 0.4f),
                        5,
                        respawn
                    )
                );
            }
        }

        private static float RangeCenter(int minColumn, int maxColumn, bool mirrored)
        {
            var a = ColumnCenter(minColumn, mirrored);
            var b = ColumnCenter(maxColumn, mirrored);
            return (a + b) * 0.5f;
        }

        // ---------------------------------------------------------------------
        // Coletáveis. Ultraprocessados no piso do caminho, saudáveis em nichos de
        // salto no teto e num degrau sem saída. As duas metades somam os mesmos
        // pontos de alimento saudável (590) para a corrida ser justa.
        // ---------------------------------------------------------------------
        private void CreateCollectibles()
        {
            var foods = CreateGroup("Coletaveis");

            // Metade esquerda (P1)
            PlaceFood("Burger", 3, 1, 0.7f, foods, false);
            PlaceFood("Fries", 5, 1, 0.7f, foods, false);
            PlaceFood("Donut", 10, 5, 0.7f, foods, false);
            PlaceFood("Candy", 10, 9, 0.7f, foods, false);
            PlaceFood("Water", 1, 2, 0.7f, foods, false);
            PlaceFood("Banana", 2, 4, 0.4f, foods, false);
            PlaceFood("Carrot", 4, 4, 0.4f, foods, false);
            PlaceFood("Strawberry", 13, 12, 0.4f, foods, false);
            PlaceFood("Fish", 11, 16, 0.3f, foods, false);

            // Metade direita (P2), posições espelhadas
            PlaceFood("Pizza", 3, 1, 0.7f, foods, true);
            PlaceFood("HotDog", 5, 1, 0.7f, foods, true);
            PlaceFood("Soda", 10, 5, 0.7f, foods, true);
            PlaceFood("Chips", 10, 9, 0.7f, foods, true);
            PlaceFood("Broccoli", 1, 2, 0.7f, foods, true);
            PlaceFood("Apple", 2, 4, 0.4f, foods, true);
            PlaceFood("Avocado", 4, 4, 0.4f, foods, true);
            PlaceFood("Orange", 13, 12, 0.4f, foods, true);
            PlaceFood("Watermelon", 11, 16, 0.3f, foods, true);
        }

        private void PlaceFood(
            string id,
            int column,
            int row,
            float heightInCell,
            Transform parent,
            bool mirrored
        )
        {
            var c = mirrored ? MirrorColumn(column) : column;
            SpawnFood(
                id,
                new Vector2((c + 0.5f) * TileSize, row * TileSize + heightInCell),
                parent
            );
            _foodPlacements.Add(new MazeFood(id, new Vector2Int(c, row)));
        }

        private void CreateLabels()
        {
            var labels = CreateGroup("Rotulos");
            CreateWorldLabel(
                "LARGADA P1",
                labels,
                new Vector2(2.5f, 3.55f),
                new Color(0.15f, 0.67f, 1f)
            );
            CreateWorldLabel(
                "LARGADA P2",
                labels,
                new Vector2(MirrorX(2.5f), 3.55f),
                new Color(0.35f, 1f, 0.55f)
            );
            CreateWorldLabel(
                "quem chegar primeiro vence",
                labels,
                new Vector2(ExitX, 15.35f),
                HintColor
            );
        }

        // ---------------------------------------------------------------------
        // Anti-deadlock da caixa: se cair do corredor C (poço do elevador aberto),
        // volta ao spawn sozinha.
        // ---------------------------------------------------------------------
        private void MonitorBoxes()
        {
            foreach (var box in _boxes)
            {
                if (box != null && box.transform.position.y < 9f - 1f)
                {
                    box.ResetToSpawn();
                }
            }
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

            // Colisor com cantos arredondados e sem atrito: não engancha nas emendas
            // da rocha nem gruda na parede em pleno salto.
            var playerCollider = player.GetComponent<BoxCollider2D>();
            if (playerCollider != null)
            {
                const float rounding = 0.06f;
                playerCollider.size = new Vector2(1f - 2f * rounding, 1f - 2f * rounding);
                playerCollider.edgeRadius = rounding;
                playerCollider.sharedMaterial = PlayerPhysicsMaterial;
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
            cam.backgroundColor = TunnelColor;
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
            // Corrida: a primeira chegada abre a janela final de 20 s; a segunda
            // encerra a partida ou vira DNF. É o fluxo do GOAL-01.
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
                _leftDoorGate
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

        // =====================================================================
        // VISUAL SEM ESTICAR
        //
        // Nenhum sprite do atlas é quadrado (rocha 1.65 x 1.34, mecanismos
        // ~1.63 x 1.56). Escalar um sprite para o tamanho do colisor deforma a
        // textura. Aqui cada visual é uma faixa em modo Tiled: o sprite repete no
        // tamanho natural, com escala uniforme, e a borda é recortada, nunca
        // esticada. Os sprites do atlas são FullRect, o que o modo Tiled exige.
        // =====================================================================

        /// <summary>
        /// Faixa de sprites repetidos cobrindo exatamente o retângulo dado. A escala
        /// é uniforme e definida pela altura: a faixa tem sempre um sprite de
        /// altura, repetido na horizontal quantas vezes couber.
        /// </summary>
        private SpriteRenderer CreateTiledStrip(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            float minX,
            float maxX,
            float minY,
            float maxY,
            int sortingOrder
        )
        {
            var strip = new GameObject(name);
            strip.transform.SetParent(parent, false);
            strip.transform.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);

            var natural = sprite.bounds.size;
            var naturalY = natural.y > 0.0001f ? natural.y : 1f;
            var scale = (maxY - minY) / naturalY;
            strip.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = strip.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2((maxX - minX) / scale, naturalY);
            if (_visualCatalog != null && _visualCatalog.SpriteMaterial != null)
            {
                sr.sharedMaterial = _visualCatalog.SpriteMaterial;
            }

            return sr;
        }

        /// <summary>
        /// Sprite único com escala uniforme que cabe dentro da caixa dada, alinhado
        /// pela base. Para elementos que são uma imagem, não uma textura: bandeira,
        /// portal, alavanca.
        /// </summary>
        private SpriteRenderer CreateFittedSprite(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector2 bottomCenter,
            Vector2 maxSize,
            int sortingOrder
        )
        {
            var natural = sprite.bounds.size;
            var naturalX = natural.x > 0.0001f ? natural.x : 1f;
            var naturalY = natural.y > 0.0001f ? natural.y : 1f;
            var scale = Mathf.Min(maxSize.x / naturalX, maxSize.y / naturalY);

            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(
                bottomCenter.x,
                bottomCenter.y + naturalY * scale * 0.5f,
                0f
            );
            obj.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            if (_visualCatalog != null && _visualCatalog.SpriteMaterial != null)
            {
                sr.sharedMaterial = _visualCatalog.SpriteMaterial;
            }

            return sr;
        }

        private Sprite SpriteOrPlaceholder(int catalogIndex, out Color tint, Color fallbackTint)
        {
            if (
                _visualCatalog != null
                && _visualCatalog.Environment != null
                && _visualCatalog.Environment.Length > catalogIndex
                && _visualCatalog.Environment[catalogIndex] != null
            )
            {
                tint = Color.white;
                return _visualCatalog.Environment[catalogIndex];
            }

            tint = fallbackTint;
            return PlaceholderSpriteFactory.Square;
        }

        /// <summary>
        /// Bloco estático com colisor exato. Sem visual: a rocha do labirinto é
        /// desenhada por linha em <see cref="CreateMazeRock"/>, e as paredes externas
        /// ficam fora do enquadramento.
        /// </summary>
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

            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        // ---------------------------------------------------------------------
        // A rocha do labirinto vem do mapa de ladrilhos. Sequências de rocha
        // idênticas em linhas vizinhas são fundidas num único retângulo de colisor,
        // para o jogador não enganchar nas emendas. O visual é uma faixa por linha,
        // com a textura de chão (topo com musgo) onde a face de cima dá para um
        // túnel e a textura de rocha no resto.
        // ---------------------------------------------------------------------
        private void CreateMazeRock()
        {
            var rock = CreateGroup("Labirinto_Rocha");
            var ground = SpriteOrPlaceholder(0, out var groundTint, GroundColor);
            var wall = SpriteOrPlaceholder(2, out var wallTint, WallColor);

            foreach (var (minColumn, maxColumn, minRow, maxRow) in MergeRockRectangles())
            {
                var name = $"Rocha_C{minColumn}-{maxColumn}_L{minRow}-{maxRow}";
                var minX = minColumn * TileSize;
                var maxX = (maxColumn + 1) * TileSize;

                CreateStaticBlock(
                    name,
                    rock,
                    new Vector2((minX + maxX) * 0.5f, (minRow + maxRow + 1) * 0.5f * TileSize),
                    new Vector2(maxX - minX, (maxRow - minRow + 1) * TileSize),
                    WallColor,
                    StaticBlockKind.Wall
                );

                for (var row = minRow; row <= maxRow; row++)
                {
                    var facesTunnel = false;
                    for (var c = minColumn; c <= maxColumn && !facesTunnel; c++)
                    {
                        facesTunnel = !IsRock(c, row + 1);
                    }

                    var sprite = facesTunnel ? ground : wall;
                    var tint = facesTunnel ? groundTint : wallTint;
                    CreateTiledStrip(
                        $"{name}_Visual_L{row}",
                        rock,
                        sprite,
                        tint,
                        minX,
                        maxX,
                        row * TileSize,
                        (row + 1) * TileSize,
                        0
                    );
                }
            }
        }

        /// <summary>
        /// Funde sequências horizontais de rocha iguais em linhas consecutivas num
        /// retângulo só. Menos colisores, menos emendas.
        /// </summary>
        public static List<(
            int MinColumn,
            int MaxColumn,
            int MinRow,
            int MaxRow
        )> MergeRockRectangles()
        {
            var rectangles = new List<(int, int, int, int)>();
            var claimed = new bool[MazeColumns, MazeRowCount];

            for (var row = 0; row < MazeRowCount; row++)
            {
                var column = 0;
                while (column < MazeColumns)
                {
                    if (!IsRock(column, row) || claimed[column, row])
                    {
                        column++;
                        continue;
                    }

                    var start = column;
                    while (column < MazeColumns && IsRock(column, row) && !claimed[column, row])
                    {
                        column++;
                    }
                    var end = column - 1;

                    var top = row;
                    while (top + 1 < MazeRowCount && RunMatches(start, end, top + 1, claimed))
                    {
                        top++;
                    }

                    for (var r = row; r <= top; r++)
                    {
                        for (var c = start; c <= end; c++)
                        {
                            claimed[c, r] = true;
                        }
                    }

                    rectangles.Add((start, end, row, top));
                }
            }

            return rectangles;
        }

        private static bool RunMatches(int start, int end, int row, bool[,] claimed)
        {
            if (start > 0 && IsRock(start - 1, row) && !claimed[start - 1, row])
            {
                return false;
            }

            if (end + 1 < MazeColumns && IsRock(end + 1, row) && !claimed[end + 1, row])
            {
                return false;
            }

            for (var c = start; c <= end; c++)
            {
                if (!IsRock(c, row) || claimed[c, row])
                {
                    return false;
                }
            }

            return true;
        }

        private PushableBox CreatePushableBox(string name, Transform parent, Vector2 position)
        {
            if (_pushableBoxPrefab == null)
            {
                return null;
            }

            var box = Instantiate(_pushableBoxPrefab, position, Quaternion.identity, parent);
            box.name = name;

            // O sprite da caixa mede 1.65 x 1.56; o colisor assume o tamanho do
            // sprite e a escala uniforme dá a altura de projeto. Antes o sprite era
            // 65% maior que o colisor e a caixa parecia atravessar a rocha.
            var sprite = SpriteOrPlaceholder(7, out var tint, new Color(0.72f, 0.39f, 0.13f));
            var natural = sprite.bounds.size;
            var scale = BoxHeight / Mathf.Max(0.0001f, natural.y);
            box.transform.localScale = new Vector3(scale, scale, 1f);

            var collider = box.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.size = natural;
                collider.offset = Vector2.zero;
            }

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
                sr.sprite = sprite;
                sr.color = tint;
                sr.sortingOrder = 1;
            }

            CreateWorldLabel(
                "CAIXA",
                parent,
                new Vector2(position.x, position.y + BoxHeight * 0.5f + 0.35f),
                Color.white
            );
            return box.GetComponent<PushableBox>();
        }

        /// <summary>
        /// Botão de pressão com gatilho da largura pedida e visual em faixa de
        /// pinos. Raiz sem escala, para o gatilho não deformar.
        /// </summary>
        private PressureButton CreatePressureButtonObject(
            string name,
            Transform parent,
            Vector2 position,
            float width = 1.6f
        )
        {
            var btn = new GameObject(name);
            btn.transform.SetParent(parent, false);
            btn.transform.position = position;

            var col = btn.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 0.3f);
            col.isTrigger = true;

            var sprite = SpriteOrPlaceholder(8, out var tint, new Color(0.9f, 0.25f, 0.25f));
            var visual = CreateTiledStrip(
                "Visual",
                btn.transform,
                sprite,
                tint,
                position.x - width * 0.5f,
                position.x + width * 0.5f,
                position.y - 0.15f,
                position.y + 0.2f,
                1
            );

            var button = btn.AddComponent<PressureButton>();
            button.SetVisualRenderer(visual);
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
            handle.transform.localScale = new Vector3(0.12f, 0.7f, 1f);

            var handleRenderer = handle.AddComponent<SpriteRenderer>();
            handleRenderer.sprite = PlaceholderSpriteFactory.Square;
            handleRenderer.color = Color.white;
            handleRenderer.sortingOrder = 3;

            var baseSprite = SpriteOrPlaceholder(9, out var baseTint, new Color(0.3f, 0.3f, 0.35f));
            var baseRenderer = CreateFittedSprite(
                "Base",
                leverObj.transform,
                baseSprite,
                baseTint,
                new Vector2(position.x, position.y - 0.75f),
                new Vector2(0.9f, 0.7f),
                2
            );

            var lever = leverObj.AddComponent<Lever>();
            lever.Configure(handle.transform, baseRenderer, isToggleable);

            CreateWorldLabel(
                label,
                parent,
                new Vector2(position.x, position.y + 1.05f),
                Color.white
            );
            return lever;
        }

        /// <summary>
        /// Portão de coluna inteira: sprite repetido na vertical com escala uniforme,
        /// colisor exato. O <see cref="PuzzleDoor"/> encontra o renderer na raiz e
        /// muda a cor ao abrir.
        /// </summary>
        private PuzzleDoor CreatePuzzleDoorObject(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            bool instantMotion = false,
            Vector2? openOffset = null
        )
        {
            var sprite = SpriteOrPlaceholder(10, out var tint, new Color(0.2f, 0.45f, 0.6f));
            var natural = sprite.bounds.size;
            var scale = size.x / Mathf.Max(0.0001f, natural.x);

            var doorObj = new GameObject(name);
            doorObj.transform.SetParent(parent, false);
            doorObj.transform.position = position;
            doorObj.transform.localScale = new Vector3(scale, scale, 1f);
            doorObj.layer = GroundLayer;

            var col = doorObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size.x / scale, size.y / scale);

            var sr = doorObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = 1;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(size.x / scale, size.y / scale);

            var door = doorObj.AddComponent<PuzzleDoor>();
            door.Configure(openOffset ?? new Vector2(0f, size.y + 0.5f), 4f, instantMotion);

            CreateWorldLabel(
                "PORTÃO",
                parent,
                new Vector2(position.x, position.y + size.y * 0.5f + 0.35f),
                Color.cyan
            );
            return door;
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

            // Visual mais alto que o gatilho, para os espinhos serem legíveis; o
            // gatilho continua rente ao chão.
            var sprite = SpriteOrPlaceholder(11, out var tint, new Color(0.85f, 0.2f, 0.2f));
            var bottom = position.y - size.y * 0.5f;
            CreateTiledStrip(
                "Visual",
                spikeObj.transform,
                sprite,
                tint,
                position.x - size.x * 0.5f,
                position.x + size.x * 0.5f,
                bottom,
                bottom + 0.55f,
                2
            );

            var spike = spikeObj.AddComponent<SpikeHazard>();
            spike.Configure(penalty, 1.0f, defaultRespawn);
            CreateWorldLabel(
                "ESPINHOS",
                parent,
                new Vector2(position.x, bottom + 0.95f),
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
            col.size = size;
            col.isTrigger = true;

            var sprite = SpriteOrPlaceholder(
                12,
                out var tint,
                new Color(0.42f, 0.28f, 0.15f, 0.75f)
            );
            if (sprite != PlaceholderSpriteFactory.Square)
            {
                tint = new Color(1f, 1f, 1f, 0.9f);
            }

            CreateTiledStrip(
                "Visual",
                mudObj.transform,
                sprite,
                tint,
                position.x - size.x * 0.5f,
                position.x + size.x * 0.5f,
                position.y - size.y * 0.5f,
                position.y + size.y * 0.5f,
                2
            );

            var mud = mudObj.AddComponent<MudHazard>();
            mud.Configure(speedMultiplier);
            CreateWorldLabel(
                "LAMA (-30%)",
                parent,
                new Vector2(position.x, position.y + 0.65f),
                new Color(0.85f, 0.7f, 0.5f)
            );
            return mud;
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

            // A bandeira cabe numa caixa de 1.2 x 1.8 com escala uniforme e apoia no
            // piso, que fica 1.2 abaixo do centro do gatilho.
            var sprite = SpriteOrPlaceholder(5, out _, new Color(0.55f, 0.55f, 0.6f));
            var flag = CreateFittedSprite(
                "Flag",
                cp.transform,
                sprite,
                new Color(0.55f, 0.55f, 0.6f),
                new Vector2(position.x, position.y - 1.2f),
                new Vector2(1.2f, 1.8f),
                2
            );

            var checkpoint = cp.AddComponent<Checkpoint>();
            checkpoint.Configure(index, new Vector2(0f, 0.5f));
            checkpoint.SetVisualElements(
                flag,
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

            CreateWorldLabel($"CP {index}", cp.transform, new Vector2(0f, 1.1f), Color.yellow);
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
            col.size = new Vector2(1.5f, 2.4f);
            col.isTrigger = true;

            // O portal cabe em 1.6 x 2.4 com escala uniforme, apoiado no piso do
            // corredor, que fica 1.3 abaixo do centro do gatilho.
            var sprite = SpriteOrPlaceholder(
                6,
                out var tint,
                bannerColor ?? new Color(1f, 0.84f, 0f)
            );
            CreateFittedSprite(
                "Portal",
                fl.transform,
                sprite,
                sprite == PlaceholderSpriteFactory.Square ? tint : (bannerColor ?? Color.white),
                new Vector2(position.x, position.y - 1.3f),
                new Vector2(1.6f, 2.4f),
                2
            );

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

            CreateWorldLabel(label, fl.transform, new Vector2(0f, 1.45f), Color.white);
            return finishLine;
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
