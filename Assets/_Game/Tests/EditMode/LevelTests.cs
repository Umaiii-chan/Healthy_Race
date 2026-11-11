using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HealthyRace.Core;
using HealthyRace.Level;
using HealthyRace.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    /// <summary>
    /// Fase 1 montada a partir do mapa em texto: cena, contagens, geometria e rotas.
    /// Também os componentes de checkpoint, porta, fim de partida e câmera.
    /// </summary>
    public sealed class LevelTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity";
        private const string MapPath = "Assets/_Game/Levels/Fase01.txt";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player.prefab";
        private const string CollectiblePrefabPath =
            "Assets/_Game/Prefabs/Collectibles/PF_FoodCollectible.prefab";
        private const string CatalogPath = "Assets/_Game/Art/VisualAtlas/SO_VisualCatalog.asset";

        /// <summary>Os mapas das 10 fases.</summary>
        public static IEnumerable<string> Maps =>
            Enumerable.Range(1, 10).Select(n => $"Assets/_Game/Levels/Fase{n:00}.txt");

        private static LevelGrid LoadGrid() => LoadGrid(MapPath);

        private static LevelGrid LoadGrid(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Assert.That(asset, Is.Not.Null, $"mapa ausente: {path}");
            return LevelGrid.Parse(asset.text);
        }

        [Test]
        public void Scene_OpensWithoutMissingScripts_AndHasConfiguredBootstrap()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var rootObjects = scene.GetRootGameObjects();
                var bootstrap = rootObjects
                    .Select(go => go.GetComponent<LevelBootstrap>())
                    .FirstOrDefault(b => b != null);
                Assert.That(bootstrap, Is.Not.Null, "A cena precisa do LevelBootstrap.");

                var serialized = new SerializedObject(bootstrap);
                foreach (
                    var field in new[]
                    {
                        "_map",
                        "_playerPrefab",
                        "_collectiblePrefab",
                        "_visualCatalog",
                        "_playerOneVisualPrefab",
                        "_playerTwoVisualPrefab",
                        "_screensPrefab",
                        "_inputActions",
                    }
                )
                {
                    Assert.That(
                        serialized.FindProperty(field).objectReferenceValue,
                        Is.Not.Null,
                        $"Campo {field} não atribuído."
                    );
                }

                var foodCatalog = serialized.FindProperty("_foodCatalog");
                Assert.That(foodCatalog.arraySize, Is.EqualTo(18), "Os 18 alimentos do MVP.");
                for (var i = 0; i < foodCatalog.arraySize; i++)
                {
                    Assert.That(
                        foodCatalog.GetArrayElementAtIndex(i).objectReferenceValue,
                        Is.Not.Null
                    );
                }

                var components = rootObjects.SelectMany(go =>
                    go.GetComponentsInChildren<MonoBehaviour>(true)
                );
                Assert.That(components, Has.None.Null, "A cena não pode ter script ausente.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void Catalog_HasTheGreenhouseArt()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HealthyRace.Visuals.VisualCatalog>(
                CatalogPath
            );
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Greenhouse.IsComplete, Is.True, "Rode 'Build greenhouse levels'.");
            Assert.That(
                catalog.Greenhouse.Brick.bounds.size.x,
                Is.EqualTo(1f).Within(0.01f),
                "Um tijolo por tile: a arte é ladrilhada, não esticada."
            );
        }

        // =====================================================================
        // Mapa
        // =====================================================================
        [Test]
        public void Map_IsAClosedRectangleWithBothSpawnsAndDoors()
        {
            var grid = LoadGrid();
            Assert.That(grid.Width, Is.EqualTo(64));
            Assert.That(grid.Height, Is.EqualTo(38));
            for (var x = 0; x < grid.Width; x++)
            {
                Assert.That(grid[x, 0], Is.EqualTo(LevelGrid.Solid), $"chão aberto em x={x}");
                Assert.That(grid[x, grid.Height - 1], Is.EqualTo(LevelGrid.Solid));
            }

            for (var y = 0; y < grid.Height; y++)
            {
                Assert.That(grid[0, y], Is.EqualTo(LevelGrid.Solid), $"parede aberta em y={y}");
                Assert.That(grid[grid.Width - 1, y], Is.EqualTo(LevelGrid.Solid));
            }

            Assert.That(grid.Count(LevelGrid.SpawnOne), Is.EqualTo(1));
            Assert.That(grid.Count(LevelGrid.SpawnTwo), Is.EqualTo(1));
            Assert.That(grid.Count(LevelGrid.DoorOne), Is.EqualTo(2), "porta de 2 tiles");
            Assert.That(grid.Count(LevelGrid.DoorTwo), Is.EqualTo(2), "porta de 2 tiles");
            Assert.That(grid.Signs.Count, Is.GreaterThanOrEqualTo(12), "placa em cada bifurcação");
        }

        [TestCaseSource(nameof(Maps))]
        public void Map_EveryStandingSpotHasHeadroom(string map)
        {
            // corpo de 1,25; com 1,5 de espaço livre ninguém fica preso (só não pula)
            var grid = LoadGrid(map);
            var problems = new List<string>();
            for (var x = 1; x < grid.Width - 1; x++)
            for (var y = 1; y < grid.Height - 1; y++)
            {
                if (!LevelRoutes.IsStandable(grid, x, y) || grid[x, y] == LevelGrid.Lift)
                {
                    continue;
                }

                var floor = grid.SurfaceTop(x, y - 1);
                var room = grid.CeilingAbove(x, floor) - floor;
                if (room < 1.5f)
                {
                    problems.Add($"({x},{y}) livre {room:0.##}");
                }
            }

            Assert.That(problems, Is.Empty, string.Join(", ", problems));
        }

        [TestCaseSource(nameof(Maps))]
        public void Map_AnyLevel_IsClosed_HasSpawnsDoorsAndATheme(string map)
        {
            var grid = LoadGrid(map);
            for (var x = 0; x < grid.Width; x++)
            {
                Assert.That(grid[x, 0], Is.EqualTo(LevelGrid.Solid));
                Assert.That(grid[x, grid.Height - 1], Is.EqualTo(LevelGrid.Solid));
            }

            for (var y = 0; y < grid.Height; y++)
            {
                Assert.That(grid[0, y], Is.EqualTo(LevelGrid.Solid));
                Assert.That(grid[grid.Width - 1, y], Is.EqualTo(LevelGrid.Solid));
            }

            Assert.That(grid.Count(LevelGrid.SpawnOne), Is.EqualTo(1));
            Assert.That(grid.Count(LevelGrid.SpawnTwo), Is.EqualTo(1));
            Assert.That(grid.Count(LevelGrid.DoorOne), Is.EqualTo(2));
            Assert.That(grid.Count(LevelGrid.DoorTwo), Is.EqualTo(2));
            Assert.That(grid.Count(LevelGrid.Fast), Is.GreaterThan(0));
            Assert.That(grid.Count(LevelGrid.Healthy), Is.GreaterThan(0));
            Assert.That(grid.LevelNumber, Is.InRange(1, 10));
            Assert.That(grid.LevelName, Is.Not.Empty);
        }

        [Test]
        public void Maps_AreTenDistinctLevels()
        {
            var grids = Maps.Select(LoadGrid).ToList();
            Assert.That(grids.Select(g => g.LevelNumber), Is.EqualTo(Enumerable.Range(1, 10)));
            Assert.That(grids.Select(g => g.LevelName).Distinct().Count(), Is.EqualTo(10));
            var layouts = grids.Select(g =>
                string.Join(
                    "",
                    Enumerable
                        .Range(0, g.Height)
                        .Select(y => new string(
                            Enumerable.Range(0, g.Width).Select(x => g[x, y]).ToArray()
                        ))
                )
            );
            Assert.That(layouts.Distinct().Count(), Is.EqualTo(10), "nenhum mapa repetido");
        }

        [Test]
        public void Map_HasThePlannedContent()
        {
            var grid = LoadGrid();
            Assert.That(grid.Count(LevelGrid.Healthy), Is.EqualTo(32));
            Assert.That(grid.Count(LevelGrid.Fast), Is.EqualTo(24));
            Assert.That(grid.Count(LevelGrid.Checkpoint), Is.EqualTo(11));
            Assert.That(grid.Count(LevelGrid.Lever), Is.EqualTo(4));
            Assert.That(grid.Runs(LevelGrid.Lift).Count(), Is.EqualTo(5), "um elevador por virada");
        }

        [TestCaseSource(nameof(Maps))]
        public void Map_FastFoodOnlyInTheShortCorridor_HealthyOnlyOutsideIt(string map)
        {
            var grid = LoadGrid(map);
            foreach (var cell in grid.Cells(LevelGrid.Fast))
            {
                Assert.That(grid.IsShortCorridor(cell.x, cell.y), Is.True, $"fast-food em {cell}");
            }

            foreach (var cell in grid.Cells(LevelGrid.Healthy))
            {
                Assert.That(grid.IsShortCorridor(cell.x, cell.y), Is.False, $"saudável em {cell}");
            }
        }

        // =====================================================================
        // Rotas: cada jogador termina sozinho, pelo longo ou pelo curto
        // =====================================================================
        [Test]
        public void Routes_WorstWellbeingBudget_CoversTheGridMoves()
        {
            // Salo: velocidade × 0,7 e pulo × 0,8. As rotas da grade usam degraus e
            // vãos de 1 tile; o orçamento real tem de cobrir isso com folga.
            var worst = LevelBootstrap.DesignBudget(12f * 0.8f, 7f * 0.7f);
            Assert.That(
                worst.CanClear(LevelRoutes.MaxRise, LevelRoutes.MaxGap),
                Is.True,
                worst.ToString()
            );
        }

        [TestCaseSource(nameof(Maps))]
        public void Routes_EachPlayerReachesTheOwnDoorAlone(string map)
        {
            var grid = LoadGrid(map);
            foreach (
                var (spawn, door) in new[]
                {
                    (LevelGrid.SpawnOne, LevelGrid.DoorOne),
                    (LevelGrid.SpawnTwo, LevelGrid.DoorTwo),
                }
            )
            {
                var start = grid.Cells(spawn).Single();
                var route = LevelRoutes.Find(grid, start, c => grid[c.x, c.y] == door);
                Assert.That(route, Is.Not.Empty, $"sem rota de {spawn} até {door}");
            }
        }

        [TestCaseSource(nameof(Maps))]
        public void Routes_BothPlayersRunTheSameDistance(string map)
        {
            // Largadas lado a lado e portas lado a lado: quem larga um tile à frente tem
            // de ter a porta um tile mais longe, senão a corrida começa decidida.
            var grid = LoadGrid(map);
            var one = Horizontal(grid, LevelGrid.SpawnOne, LevelGrid.DoorOne);
            var two = Horizontal(grid, LevelGrid.SpawnTwo, LevelGrid.DoorTwo);
            Assert.That(two, Is.EqualTo(one), $"P1 corre {one} tiles e P2 corre {two}");
        }

        private static int Horizontal(LevelGrid grid, char spawn, char door)
        {
            var route = LevelRoutes.Find(grid, grid.Cells(spawn).Single(), c =>
                grid[c.x, c.y] == door
            );
            Assert.That(route, Is.Not.Empty, $"sem rota de {spawn} até {door}");
            var distance = 0;
            for (var i = 1; i < route.Count; i++)
            {
                distance += Mathf.Abs(route[i].Cell.x - route[i - 1].Cell.x);
            }
            return distance;
        }

        [TestCaseSource(nameof(Maps))]
        public void Routes_LongPath_ReachesTheDoorWithoutEnteringTheShortCorridor(string map)
        {
            var grid = LoadGrid(map);
            var start = grid.Cells(LevelGrid.SpawnOne).Single();
            var route = LevelRoutes.Find(
                grid,
                start,
                c => grid[c.x, c.y] == LevelGrid.DoorOne,
                c => grid.IsShortCorridor(c.x, c.y)
            );
            Assert.That(route, Is.Not.Empty, "o caminho longo precisa existir inteiro");
        }

        [TestCaseSource(nameof(Maps))]
        public void Routes_ShortPath_EachCorridorLeadsToTheDoor(string map)
        {
            var grid = LoadGrid(map);
            var corridors = grid.Cells(LevelGrid.Fast).ToList();
            Assert.That(corridors, Is.Not.Empty);
            foreach (var cell in corridors)
            {
                var route = LevelRoutes.Find(grid, cell, c => grid[c.x, c.y] == LevelGrid.DoorOne);
                Assert.That(route, Is.Not.Empty, $"do corredor em {cell} não se chega à porta");
            }

            // e o corredor se alcança da largada caindo no buraco da bifurcação
            var start = grid.Cells(LevelGrid.SpawnOne).Single();
            var toCorridor = LevelRoutes.Find(grid, start, c => grid.IsShortCorridor(c.x, c.y));
            Assert.That(toCorridor, Is.Not.Empty);
            Assert.That(toCorridor.Any(s => s.Move == RouteMove.Fall), Is.True);
        }

        [TestCaseSource(nameof(Maps))]
        public void Routes_TheShortCorridorCannotBeJumped(string map)
        {
            // teto baixo: no corredor não há pulo, então o fast-food é inevitável
            var grid = LoadGrid(map);
            foreach (var cell in grid.Cells(LevelGrid.Fast))
            {
                var jumps = LevelRoutes
                    .Neighbours(grid, cell)
                    .Where(n => n.Move == RouteMove.Jump && n.Cell.y >= cell.y);
                Assert.That(jumps, Is.Empty, $"dá para pular no corredor em {cell}");
            }
        }

        // =====================================================================
        // Montagem pelo LevelBootstrap
        // =====================================================================
        [Test]
        public void Bootstrap_BuildsEverythingTheMapDescribes()
        {
            var holder = new GameObject("LevelHolder");
            try
            {
                var bootstrap = BuildBootstrap(holder);
                var grid = bootstrap.Grid;
                Assert.That(bootstrap.PlayerOne, Is.Not.Null);
                Assert.That(bootstrap.PlayerTwo, Is.Not.Null);
                Assert.That(
                    bootstrap.Foods.Count,
                    Is.EqualTo(grid.Count(LevelGrid.Healthy) + grid.Count(LevelGrid.Fast))
                );
                Assert.That(
                    bootstrap.Checkpoints.Count,
                    Is.EqualTo(grid.Count(LevelGrid.Checkpoint))
                );
                Assert.That(bootstrap.Spikes.Count, Is.EqualTo(grid.Count(LevelGrid.Spikes)));
                Assert.That(
                    bootstrap.MudHazards.Count,
                    Is.EqualTo(grid.Runs(LevelGrid.Mud).Count())
                );
                Assert.That(
                    bootstrap.FallingPlatforms.Count,
                    Is.EqualTo(grid.Count(LevelGrid.Falling))
                );
                Assert.That(bootstrap.Lifts.Count, Is.EqualTo(grid.Runs(LevelGrid.Lift).Count()));
                Assert.That(bootstrap.Gates.Count, Is.EqualTo(grid.Count(LevelGrid.Lever)));

                Assert.That(bootstrap.PlayerOneDoor.AcceptsPlayer(1), Is.True);
                Assert.That(bootstrap.PlayerOneDoor.AcceptsPlayer(2), Is.False);
                Assert.That(bootstrap.PlayerTwoDoor.AcceptsPlayer(2), Is.True);
                Assert.That(
                    bootstrap.MatchController.RequireBothAtDoors,
                    Is.False,
                    "Corrida: cada um termina sozinho."
                );
                Assert.That(
                    bootstrap.MatchController.PlayerTwoDoor,
                    Is.SameAs(bootstrap.PlayerTwoDoor)
                );
                Assert.That(bootstrap.DebugLabelsVisible, Is.False, "Rótulos só com F1.");
                var foliage = holder.GetComponentInChildren<HealthyRace.Visuals.ParallaxLayer>();
                Assert.That(foliage, Is.Not.Null, "fundo com parallax");
                Assert.That(
                    foliage.GetComponent<SpriteRenderer>().drawMode,
                    Is.EqualTo(SpriteDrawMode.Tiled),
                    "folhagem ladrilhada, não esticada"
                );
                Assert.That(bootstrap.DebugLabelCount, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void Bootstrap_EverythingStaysInsideTheLevelRectangle()
        {
            var holder = new GameObject("LevelHolder");
            try
            {
                var bootstrap = BuildBootstrap(holder);
                var bounds = new Rect(0f, 0f, bootstrap.Grid.Width, bootstrap.Grid.Height);
                foreach (var collider in holder.GetComponentsInChildren<Collider2D>(true))
                {
                    var b = collider.bounds;
                    if (b.size == Vector3.zero)
                    {
                        // o composto só ganha limites quando a física roda; as peças
                        // que ele junta são conferidas uma a uma
                        continue;
                    }

                    Assert.That(
                        bounds.Contains(b.min) && bounds.Contains(b.max - Vector3.one * 0.001f),
                        Is.True,
                        $"{collider.name} sai da fase: {b}"
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void Bootstrap_TerrainIsOneCompositeOnTheGroundLayer()
        {
            var holder = new GameObject("LevelHolder");
            try
            {
                BuildBootstrap(holder);
                var composites = holder.GetComponentsInChildren<CompositeCollider2D>(true);
                Assert.That(composites.Length, Is.EqualTo(1), "terreno sem emendas");
                Assert.That(composites[0].gameObject.layer, Is.EqualTo(LevelBootstrap.GroundLayer));
                Assert.That(composites[0].pathCount, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void Bootstrap_SignsAreReadableAt1080p()
        {
            // TextMesh: altura da letra ≈ fontSize × characterSize × 0,1 unidade. A
            // câmera mostra no mínimo 18 unidades de altura em 1016 px (1080 menos o HUD).
            var pixelsPerUnit =
                (1080f - LevelBootstrap.HudBandPixels) / LevelBootstrap.MinimumViewHeight;
            var letterHeight = 64 * LevelBootstrap.SignCharacterSize * 0.1f * pixelsPerUnit;
            Assert.That(letterHeight, Is.GreaterThanOrEqualTo(24f), $"{letterHeight:0.#} px");
        }

        [Test]
        public void Checkpoint_Component_TracksActivationAndPreservesPlayerIsolation()
        {
            var go = new GameObject("TestCheckpoint", typeof(BoxCollider2D));
            try
            {
                var cp = go.AddComponent<Checkpoint>();
                cp.Configure(1, new Vector2(0f, 1f));

                Assert.That(cp.ActivateForPlayer(1), Is.True);
                Assert.That(cp.IsActivatedByPlayer(1), Is.True);
                Assert.That(cp.IsActivatedByPlayer(2), Is.False, "Player 2 must remain isolated.");
                Assert.That(cp.ActivateForPlayer(1), Is.False);
                Assert.That(cp.ActivateForPlayer(2), Is.True);
                Assert.That(cp.ActivatedPlayerCount, Is.EqualTo(2));
                Assert.That(Checkpoint.GetLastCheckpoint(1), Is.SameAs(cp));
                Assert.That(Checkpoint.GetLastCheckpoint(2), Is.SameAs(cp));
            }
            finally
            {
                Checkpoint.ResetRegistry();
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FinishLine_Component_TracksArrivalsAndCompletionOrder()
        {
            var go = new GameObject("TestFinishLine", typeof(BoxCollider2D));
            try
            {
                var fl = go.AddComponent<FinishLine>();

                Assert.That(fl.HasAnyPlayerFinished, Is.False);
                Assert.That(fl.RecordFinish(1), Is.True);
                Assert.That(fl.HasPlayerFinished(1), Is.True);
                Assert.That(fl.HasPlayerFinished(2), Is.False);
                Assert.That(fl.RecordFinish(1), Is.False);
                Assert.That(fl.RecordFinish(2), Is.True);
                Assert.That(fl.FinishedPlayerCount, Is.EqualTo(2));

                fl.ResetState();
                Assert.That(fl.FinishedPlayerCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FinishLine_DoorForPlayer_RejectsOtherPlayerAndTracksOccupancy()
        {
            var go = new GameObject("DoorP2", typeof(BoxCollider2D));
            try
            {
                var door = go.AddComponent<FinishLine>();
                door.ConfigureAcceptedPlayer(2);

                Assert.That(door.AcceptsPlayer(1), Is.False);
                Assert.That(door.AcceptsPlayer(2), Is.True);
                Assert.That(door.RecordFinish(1), Is.False, "P1 must not finish at P2's door.");
                Assert.That(door.SetOccupied(1, true), Is.False);
                Assert.That(door.IsOccupied, Is.False);

                Assert.That(door.SetOccupied(2, true), Is.True);
                Assert.That(door.IsOccupiedBy(2), Is.True);
                Assert.That(door.RecordFinish(2), Is.True);

                door.SetOccupied(2, false);
                Assert.That(door.IsOccupied, Is.False, "Leaving the door clears occupancy.");
                Assert.That(door.HasPlayerFinished(2), Is.True, "Arrival record is kept.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MatchCompletion_DualDoors_ConcludesOnlyWhenBothPlayersAreAtTheirDoors()
        {
            var created = new List<GameObject>();
            try
            {
                var doorOne = CreateDoor(1, created);
                var doorTwo = CreateDoor(2, created);
                var p1 = new GameObject("P1");
                var p2 = new GameObject("P2");
                created.Add(p1);
                created.Add(p2);

                var ctrlObj = new GameObject("MatchController");
                created.Add(ctrlObj);
                var controller = ctrlObj.AddComponent<MatchCompletionController>();
                controller.Configure(
                    doorOne,
                    null,
                    p1,
                    p2,
                    null,
                    null,
                    secondFinishLine: doorTwo,
                    requireBothAtDoors: true
                );

                Assert.That(controller.RequireBothAtDoors, Is.True);

                doorOne.SetOccupied(1, true);
                doorOne.RecordFinish(1);
                Assert.That(
                    controller.State,
                    Is.EqualTo(MatchCompletionState.WaitingForPartner),
                    "First arrival must wait for the partner instead of starting a countdown."
                );
                Assert.That(controller.FirstFinisherIndex, Is.EqualTo(1));

                doorTwo.SetOccupied(2, true);
                doorTwo.RecordFinish(2);
                Assert.That(controller.State, Is.EqualTo(MatchCompletionState.Concluded));
                Assert.That(controller.FinalResults.HasDnf, Is.False);
                Assert.That(controller.FinalResults.Player1.Placement, Is.EqualTo(1));
                Assert.That(controller.FinalResults.Player2.Placement, Is.EqualTo(2));
            }
            finally
            {
                foreach (var go in created)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void MatchCompletion_DualDoors_DoesNotConcludeIfFirstPlayerLeavesTheDoor()
        {
            var created = new List<GameObject>();
            try
            {
                var doorOne = CreateDoor(1, created);
                var doorTwo = CreateDoor(2, created);
                var p1 = new GameObject("P1");
                var p2 = new GameObject("P2");
                created.Add(p1);
                created.Add(p2);

                var ctrlObj = new GameObject("MatchController");
                created.Add(ctrlObj);
                var controller = ctrlObj.AddComponent<MatchCompletionController>();
                controller.Configure(
                    doorOne,
                    null,
                    p1,
                    p2,
                    null,
                    null,
                    secondFinishLine: doorTwo,
                    requireBothAtDoors: true
                );

                doorOne.SetOccupied(1, true);
                doorOne.RecordFinish(1);
                doorOne.SetOccupied(1, false);

                doorTwo.SetOccupied(2, true);
                doorTwo.RecordFinish(2);

                Assert.That(
                    controller.State,
                    Is.EqualTo(MatchCompletionState.WaitingForPartner),
                    "Match must not conclude while P1 is away from the door."
                );
                Assert.That(controller.AreBothPlayersAtDoors(), Is.False);

                doorOne.SetOccupied(1, true);
                Assert.That(controller.AreBothPlayersAtDoors(), Is.True);
            }
            finally
            {
                foreach (var go in created)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void SharedCamera_FixedFraming_IgnoresTargetsAndKeepsSize()
        {
            var camObj = new GameObject("Cam", typeof(Camera));
            var t1 = new GameObject("T1");
            var t2 = new GameObject("T2");
            try
            {
                var controller = camObj.AddComponent<SharedCameraController>();
                controller.Configure(t1.transform, t2.transform);
                controller.SetFixedFraming(new Vector3(16f, 9f, -10f), 9f, 16f);

                t1.transform.position = new Vector3(1f, 1f, 0f);
                t2.transform.position = new Vector3(31f, 16f, 0f);
                controller.ExecuteCameraUpdate(0.016f);

                Assert.That(controller.UseFixedFraming, Is.True);
                Assert.That(camObj.transform.position, Is.EqualTo(new Vector3(16f, 9f, -10f)));
                Assert.That(
                    controller.CalculateDesiredPosition(),
                    Is.EqualTo(new Vector3(16f, 9f, -10f))
                );
                Assert.That(
                    controller.CalculateDesiredOrthographicSize(),
                    Is.EqualTo(camObj.GetComponent<Camera>().orthographicSize).Within(0.001f)
                );
                Assert.That(
                    camObj.GetComponent<Camera>().orthographicSize,
                    Is.GreaterThanOrEqualTo(9f)
                );
            }
            finally
            {
                Object.DestroyImmediate(camObj);
                Object.DestroyImmediate(t1);
                Object.DestroyImmediate(t2);
            }
        }

        [Test]
        public void JumpBudget_IsDerivedFromArenaPhysics_NotFromLiterals()
        {
            var budget = LevelBootstrap.DesignBudget();

            // g = |gravity| * gravityScale derivado do ápice de projeto
            var expectedGravityScale = JumpBudget.GravityScaleForApex(
                LevelBootstrap.PhaseGravityY,
                12f,
                LevelBootstrap.TargetJumpApex,
                LevelBootstrap.FixedDeltaTime
            );
            Assert.That(
                budget.Gravity,
                Is.EqualTo(Mathf.Abs(LevelBootstrap.PhaseGravityY) * expectedGravityScale)
                    .Within(0.001f)
            );
            Assert.That(budget.Apex, Is.EqualTo(LevelBootstrap.TargetJumpApex).Within(0.01f));

            // A fórmula contínua superestima o ápice em ~v0*dt/2; o orçamento usa o
            // valor do integrador discreto, que é o que o jogador realmente alcança.
            Assert.That(budget.IdealApex, Is.GreaterThan(budget.Apex));
            Assert.That(
                budget.IdealApex - budget.Apex,
                Is.EqualTo(budget.JumpVelocity * LevelBootstrap.FixedDeltaTime * 0.5f).Within(0.02f)
            );
            Assert.That(budget.Reach, Is.EqualTo(budget.MoveSpeed * budget.AirTime).Within(0.001f));
            Assert.That(budget.Reach, Is.GreaterThan(budget.Apex * 2f));
            Assert.That(budget.SafeStep, Is.LessThan(budget.Apex));
            Assert.That(budget.SafeGap, Is.LessThan(budget.Reach));

            // O orçamento acompanha a física: dobrar a gravidade reduz o ápice à metade.
            var heavier = JumpBudget.FromPhysics(
                LevelBootstrap.PhaseGravityY,
                budget.Gravity / Mathf.Abs(LevelBootstrap.PhaseGravityY) * 2f,
                12f,
                7f,
                LevelBootstrap.FixedDeltaTime
            );
            Assert.That(heavier.Apex, Is.LessThan(budget.Apex * 0.55f));
            Assert.That(heavier.Apex, Is.GreaterThan(budget.Apex * 0.45f));
        }

        private static FinishLine CreateDoor(int playerIndex, List<GameObject> created)
        {
            var go = new GameObject($"Door{playerIndex}", typeof(BoxCollider2D));
            created.Add(go);
            var door = go.AddComponent<FinishLine>();
            door.ConfigureAcceptedPlayer(playerIndex);
            return door;
        }

        private static LevelBootstrap BuildBootstrap(GameObject holder)
        {
            var bootstrap = holder.AddComponent<LevelBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_map").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>(MapPath);
            serialized.FindProperty("_playerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            serialized.FindProperty("_collectiblePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(CollectiblePrefabPath);
            serialized.FindProperty("_visualCatalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<HealthyRace.Visuals.VisualCatalog>(CatalogPath);

            var foodGuids = AssetDatabase.FindAssets(
                "t:FoodData",
                new[] { "Assets/_Game/ScriptableObjects/Food" }
            );
            var foodProp = serialized.FindProperty("_foodCatalog");
            foodProp.arraySize = foodGuids.Length;
            for (var i = 0; i < foodGuids.Length; i++)
            {
                foodProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Collectibles.FoodData>(
                        AssetDatabase.GUIDToAssetPath(foodGuids[i])
                    );
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Awake altera Physics2D.gravity; em EditMode isso persistiria em
            // ProjectSettings, então o valor original é restaurado após a construção.
            var previousGravity = Physics2D.gravity;
            var previousFrameRate = Application.targetFrameRate;
            try
            {
                typeof(LevelBootstrap)
                    .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(bootstrap, null);
            }
            finally
            {
                Physics2D.gravity = previousGravity;
                Application.targetFrameRate = previousFrameRate;
            }

            return bootstrap;
        }
    }
}
