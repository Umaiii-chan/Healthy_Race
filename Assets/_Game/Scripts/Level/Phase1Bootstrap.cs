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
        public const float TargetJumpApex = 2.0f;

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

        /// <summary>Lado dos ladrilhos de rocha usados no visual.</summary>
        public const float RockTileSize = 1f;

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

        /// <summary>
        /// Preenche um retângulo com ladrilhos uniformes do sprite, em vez de esticar
        /// um único sprite. Só o ladrilho da borda é encolhido para caber no resto.
        /// </summary>
        private static Transform TileSprite(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            float minX,
            float maxX,
            float minY,
            float maxY,
            float tileSize,
            int sortingOrder
        )
        {
            var holder = new GameObject(name).transform;
            holder.SetParent(parent, false);

            var natural = sprite.bounds.size;
            var naturalX = natural.x > 0.0001f ? natural.x : 1f;
            var naturalY = natural.y > 0.0001f ? natural.y : 1f;
            var width = maxX - minX;
            var height = maxY - minY;
            var cols = Mathf.Max(1, Mathf.CeilToInt(width / tileSize - 0.001f));
            var rows = Mathf.Max(1, Mathf.CeilToInt(height / tileSize - 0.001f));

            for (var r = 0; r < rows; r++)
            {
                var y0 = minY + r * tileSize;
                var h = Mathf.Min(tileSize, maxY - y0);
                for (var c = 0; c < cols; c++)
                {
                    var x0 = minX + c * tileSize;
                    var w = Mathf.Min(tileSize, maxX - x0);

                    var tile = new GameObject($"Tile_{r}_{c}");
                    tile.transform.SetParent(holder, false);
                    tile.transform.position = new Vector3(x0 + w * 0.5f, y0 + h * 0.5f, 0f);
                    tile.transform.localScale = new Vector3(w / naturalX, h / naturalY, 1f);

                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.color = color;
                    sr.sortingOrder = sortingOrder;
                }
            }

            return holder;
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
        private void CreateMazeRock()
        {
            var rock = CreateGroup("Labirinto_Rocha");

            for (var row = 0; row < MazeRowCount; row++)
            {
                var column = 0;
                while (column < MazeColumns)
                {
                    if (!IsRock(column, row))
                    {
                        column++;
                        continue;
                    }

                    var start = column;
                    while (column < MazeColumns && IsRock(column, row))
                    {
                        column++;
                    }

                    CreateRock(
                        $"Rocha_L{row}_C{start}-{column - 1}",
                        rock,
                        start * TileSize,
                        column * TileSize,
                        row * TileSize,
                        (row + 1) * TileSize
                    );
                }
            }
        }

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

            var sprite = GetEnvironmentSprite(StaticBlockKind.Platform);
            var tint = Color.white;
            if (sprite == null)
            {
                sprite = PlaceholderSpriteFactory.Square;
                tint = PlatformColor;
            }

            var visual = TileSprite(
                "Visual",
                root.transform,
                sprite,
                tint,
                minX,
                maxX,
                bottomY - 0.4f,
                bottomY,
                TileSize,
                1
            );

            var platform = root.AddComponent<ActivatedPlatform>();
            platform.Configure(new Vector2(0f, topY - bottomY), 3f);

            var button = CreatePressureButtonObject(
                $"{name}_Botao",
                root.transform,
                new Vector2(centerX, bottomY + 0.15f)
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
            bool instantMotion = false,
            Vector2? openOffset = null
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
            door.Configure(openOffset ?? new Vector2(0f, size.y + 0.5f), 4f, instantMotion);

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

            // O colisor tem exatamente o tamanho de projeto.
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = size;

            // O visual é ladrilhado, nunca esticado: um piso de 25 m recebe 30
            // ladrilhos iguais em vez de um sprite deformado 30 vezes.
            var sprite = GetEnvironmentSprite(kind);
            var tint = Color.white;
            if (sprite == null)
            {
                sprite = PlaceholderSpriteFactory.Square;
                tint = color;
            }

            var half = size * 0.5f;
            var tiles = TileSprite(
                "Visual",
                block.transform,
                sprite,
                tint,
                position.x - half.x,
                position.x + half.x,
                position.y - half.y,
                position.y + half.y,
                RockTileSize,
                0
            );

            if (_visualCatalog != null && _visualCatalog.SpriteMaterial != null)
            {
                foreach (var sr in tiles.GetComponentsInChildren<SpriteRenderer>())
                {
                    sr.sharedMaterial = _visualCatalog.SpriteMaterial;
                }
            }
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
