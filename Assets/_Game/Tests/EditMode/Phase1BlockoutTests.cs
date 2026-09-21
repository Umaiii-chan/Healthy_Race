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
    public sealed class Phase1BlockoutTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player.prefab";
        private const string CollectiblePrefabPath =
            "Assets/_Game/Prefabs/Collectibles/PF_FoodCollectible.prefab";
        private const string PushableBoxPrefabPath =
            "Assets/_Game/Prefabs/Obstacles/PF_PushableBox.prefab";

        [Test]
        public void Scene_OpensWithoutMissingScripts_AndHasConfiguredBootstrap()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var rootObjects = scene.GetRootGameObjects();
                var bootstrapObject = rootObjects.FirstOrDefault(go =>
                    go.name == "Phase1_CorridaDoEquilibrio"
                );
                Assert.That(bootstrapObject, Is.Not.Null);

                var bootstrap = bootstrapObject.GetComponent<Phase1Bootstrap>();
                Assert.That(bootstrap, Is.Not.Null);

                var serialized = new SerializedObject(bootstrap);
                Assert.That(
                    serialized.FindProperty("_playerPrefab").objectReferenceValue,
                    Is.Not.Null
                );
                Assert.That(
                    serialized.FindProperty("_collectiblePrefab").objectReferenceValue,
                    Is.Not.Null
                );
                Assert.That(
                    serialized.FindProperty("_pushableBoxPrefab").objectReferenceValue,
                    Is.Not.Null
                );

                var foodCatalogProp = serialized.FindProperty("_foodCatalog");
                Assert.That(foodCatalogProp.arraySize, Is.EqualTo(18));
                for (var i = 0; i < foodCatalogProp.arraySize; i++)
                {
                    Assert.That(
                        foodCatalogProp.GetArrayElementAtIndex(i).objectReferenceValue,
                        Is.Not.Null,
                        $"Food catalog element {i} must not be null."
                    );
                }

                var components = rootObjects.SelectMany(go =>
                    go.GetComponentsInChildren<MonoBehaviour>(true)
                );
                Assert.That(components, Has.None.Null, "Scene must not contain missing scripts.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void Scene_HasVisualIntegrationConfigured()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var bootstrapObject = scene
                    .GetRootGameObjects()
                    .FirstOrDefault(go => go.name == "Phase1_CorridaDoEquilibrio");
                Assert.That(bootstrapObject, Is.Not.Null);

                var serialized = new SerializedObject(
                    bootstrapObject.GetComponent<Phase1Bootstrap>()
                );
                Assert.That(
                    serialized.FindProperty("_visualCatalog").objectReferenceValue,
                    Is.Not.Null
                );
                Assert.That(
                    serialized.FindProperty("_playerOneVisualPrefab").objectReferenceValue,
                    Is.Not.Null
                );
                Assert.That(
                    serialized.FindProperty("_playerTwoVisualPrefab").objectReferenceValue,
                    Is.Not.Null
                );
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
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
                Assert.That(door.RecordFinish(1), Is.False);
                Assert.That(door.SetOccupied(1, true), Is.False);
                Assert.That(door.SetOccupied(2, true), Is.True);
                Assert.That(door.IsOccupiedBy(2), Is.True);
                Assert.That(door.RecordFinish(2), Is.True);

                door.SetOccupied(2, false);
                Assert.That(door.IsOccupied, Is.False);
                Assert.That(door.HasPlayerFinished(2), Is.True);
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

                doorOne.SetOccupied(1, true);
                doorOne.RecordFinish(1);
                Assert.That(controller.State, Is.EqualTo(MatchCompletionState.WaitingForPartner));

                doorTwo.SetOccupied(2, true);
                doorTwo.RecordFinish(2);
                Assert.That(controller.State, Is.EqualTo(MatchCompletionState.Concluded));
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
        public void SharedCamera_Letterbox_MatchesTheArenaAspect()
        {
            var camObj = new GameObject("Cam", typeof(Camera));
            try
            {
                var controller = camObj.AddComponent<SharedCameraController>();
                controller.SetLetterboxedFraming(
                    Phase1Bootstrap.CameraPosition,
                    Phase1Bootstrap.ArenaMaxX - Phase1Bootstrap.ArenaMinX,
                    Phase1Bootstrap.ArenaMaxY - Phase1Bootstrap.ArenaMinY
                );

                Assert.That(controller.UsesLetterbox, Is.True);
                Assert.That(
                    controller.CalculateFixedOrthographicSize(),
                    Is.EqualTo((Phase1Bootstrap.ArenaMaxY - Phase1Bootstrap.ArenaMinY) * 0.5f)
                        .Within(0.001f)
                );

                var drawn = camObj.GetComponent<Camera>().rect;
                var arenaAspect =
                    (Phase1Bootstrap.ArenaMaxX - Phase1Bootstrap.ArenaMinX)
                    / (Phase1Bootstrap.ArenaMaxY - Phase1Bootstrap.ArenaMinY);
                var drawnAspect =
                    ((float)Screen.width * drawn.width) / (Screen.height * drawn.height);
                Assert.That(drawnAspect, Is.EqualTo(arenaAspect).Within(0.01f));
                Assert.That(drawn.width, Is.LessThanOrEqualTo(1f));
                Assert.That(drawn.height, Is.LessThanOrEqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(camObj);
            }
        }

        // -----------------------------------------------------------------
        // Labirinto competitivo
        // -----------------------------------------------------------------

        [Test]
        public void Maze_MapIsWellFormed_AndMirrored()
        {
            Assert.That(Phase1Bootstrap.MazeRows.Length, Is.EqualTo(Phase1Bootstrap.MazeRowCount));
            foreach (var row in Phase1Bootstrap.MazeRows)
            {
                Assert.That(row.Length, Is.EqualTo(Phase1Bootstrap.MazeColumns));
                Assert.That(
                    row.All(ch => ch == '#' || ch == '.'),
                    Is.True,
                    $"Unexpected char in '{row}'."
                );
            }

            // Borda de rocha: base, topo e laterais fechadas.
            for (var c = 0; c < Phase1Bootstrap.MazeColumns; c++)
            {
                Assert.That(
                    Phase1Bootstrap.IsRock(c, 0),
                    Is.True,
                    $"bottom row must be rock at column {c}"
                );
                Assert.That(
                    Phase1Bootstrap.IsRock(c, Phase1Bootstrap.MazeRowCount - 1),
                    Is.True,
                    $"top row must be rock at column {c}"
                );
            }
            for (var r = 0; r < Phase1Bootstrap.MazeRowCount; r++)
            {
                Assert.That(
                    Phase1Bootstrap.IsRock(0, r),
                    Is.True,
                    $"left column must be rock at row {r}"
                );
                Assert.That(
                    Phase1Bootstrap.IsRock(Phase1Bootstrap.MazeColumns - 1, r),
                    Is.True,
                    $"right column must be rock at row {r}"
                );
            }

            // Espelho: as duas metades têm a mesma dificuldade.
            for (var r = 0; r < Phase1Bootstrap.MazeRowCount; r++)
            {
                for (var c = 0; c < Phase1Bootstrap.MazeColumns / 2; c++)
                {
                    Assert.That(
                        Phase1Bootstrap.IsRock(c, r),
                        Is.EqualTo(Phase1Bootstrap.IsRock(Phase1Bootstrap.MirrorColumn(c), r)),
                        $"Map must be mirrored at ({c},{r})."
                    );
                }
            }

            var world = new MazeSim(null, mechanismsEnabled: true);
            foreach (
                var spawn in new[]
                {
                    Phase1Bootstrap.PlayerOneSpawnCell,
                    Phase1Bootstrap.PlayerTwoSpawnCell,
                }
            )
            {
                Assert.That(
                    world.CanStand(spawn.x, spawn.y),
                    Is.True,
                    $"spawn {spawn} must be a standing cell"
                );
            }
            foreach (var exit in Phase1Bootstrap.ExitCells)
            {
                Assert.That(Phase1Bootstrap.IsRock(exit.x, exit.y), Is.False);
            }
        }

        [Test]
        public void Phase1Bootstrap_BuildsMirroredMaze_WithSeparateSpawnsAndSharedExit()
        {
            var go = new GameObject("BootstrapHolder");
            try
            {
                var bootstrap = BuildBootstrap(go);

                Assert.That(bootstrap.PlayerOne, Is.Not.Null);
                Assert.That(bootstrap.PlayerTwo, Is.Not.Null);
                Assert.That(bootstrap.PlayerOne.transform.Find("VisualPresentation"), Is.Not.Null);
                Assert.That(bootstrap.PlayerTwo.transform.Find("VisualPresentation"), Is.Not.Null);

                var geometry = go.transform.Find("LevelGeometry");
                Assert.That(geometry, Is.Not.Null);
                foreach (
                    var group in new[]
                    {
                        "Arena_Casca",
                        "Labirinto_Rocha",
                        "Mecanismos_Esq",
                        "Mecanismos_Dir",
                        "Checkpoints",
                        "Saida",
                        "Perigos",
                        "Coletaveis",
                    }
                )
                {
                    Assert.That(geometry.Find(group), Is.Not.Null, $"{group} must exist.");
                }

                // Largadas separadas, uma em cada canto de baixo.
                var p1 = bootstrap.PlayerOne.transform.position;
                var p2 = bootstrap.PlayerTwo.transform.position;
                Assert.That(p1.x, Is.LessThan(Phase1Bootstrap.ArenaMaxX * 0.5f));
                Assert.That(p2.x, Is.GreaterThan(Phase1Bootstrap.ArenaMaxX * 0.5f));
                Assert.That(p2.x, Is.EqualTo(Phase1Bootstrap.MirrorX(p1.x)).Within(0.001f));
                Assert.That(p1.y, Is.EqualTo(p2.y).Within(0.001f));

                // Cada jogador nasce com o próprio checkpoint de retorno.
                Assert.That(bootstrap.Checkpoints.Count, Is.EqualTo(8));
                Assert.That(
                    bootstrap.Checkpoints.Select(c => c.Index),
                    Is.EqualTo(Enumerable.Range(1, 8))
                );
                Assert.That(
                    Checkpoint.GetLastCheckpoint(1),
                    Is.SameAs(bootstrap.PlayerOneSpawnCheckpoint)
                );
                Assert.That(
                    Checkpoint.GetLastCheckpoint(2),
                    Is.SameAs(bootstrap.PlayerTwoSpawnCheckpoint)
                );
                Assert.That(bootstrap.PlayerOneSpawnCheckpoint.IsActivatedByPlayer(2), Is.False);
                Assert.That(bootstrap.PlayerTwoSpawnCheckpoint.IsActivatedByPlayer(1), Is.False);

                // Saída única, aceita os dois, no centro do topo: modo corrida.
                Assert.That(bootstrap.FinishLine, Is.Not.Null);
                Assert.That(bootstrap.FinishLine.AcceptedPlayerIndex, Is.EqualTo(0));
                Assert.That(
                    bootstrap.FinishLine.transform.position.x,
                    Is.EqualTo(Phase1Bootstrap.ExitX).Within(0.001f)
                );
                Assert.That(bootstrap.MatchController.RequireBothAtDoors, Is.False);

                // Mecanismos espelhados: dois elevadores, um portão e uma caixa por metade.
                Assert.That(bootstrap.Elevators.Count, Is.EqualTo(4));
                Assert.That(bootstrap.Doors.Count, Is.EqualTo(2));
                Assert.That(bootstrap.BoxClimbs.Count, Is.EqualTo(2));
                Assert.That(bootstrap.Boxes.Count, Is.EqualTo(2));
                Assert.That(bootstrap.Gates.Count, Is.EqualTo(6));
                foreach (var gate in bootstrap.Gates)
                {
                    Assert.That(
                        gate.Button != null || gate.Lever != null,
                        Is.True,
                        $"{gate.name} needs an input"
                    );
                    Assert.That(
                        gate.Platform != null || gate.Door != null,
                        Is.True,
                        $"{gate.name} needs an output"
                    );
                }

                Assert.That(bootstrap.MudHazards.Count, Is.EqualTo(4));
                Assert.That(bootstrap.Spikes.Count, Is.EqualTo(4));
                Assert.That(bootstrap.CameraController.UseFixedFraming, Is.True);
                Assert.That(
                    bootstrap.CameraController.transform.position,
                    Is.EqualTo(Phase1Bootstrap.CameraPosition)
                );
            }
            finally
            {
                Checkpoint.ResetRegistry();
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Phase1Bootstrap_AllGeometryAndActorsStayInsideArenaRectangle()
        {
            var go = new GameObject("BootstrapHolder");
            try
            {
                var bootstrap = BuildBootstrap(go);
                var geometry = go.transform.Find("LevelGeometry");

                foreach (var collider in geometry.GetComponentsInChildren<Collider2D>(true))
                {
                    if (collider.name.StartsWith("Arena_"))
                    {
                        continue; // paredes externas ficam propositalmente além da borda
                    }

                    // Em EditMode a física não sincroniza bounds; calcula pelo transform.
                    var box = collider as BoxCollider2D;
                    var scale = collider.transform.lossyScale;
                    var size =
                        box != null
                            ? new Vector2(
                                box.size.x * Mathf.Abs(scale.x),
                                box.size.y * Mathf.Abs(scale.y)
                            )
                            : new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                    var center =
                        (Vector2)collider.transform.position
                        + (box != null ? box.offset : Vector2.zero);
                    var min = center - size * 0.5f;
                    var max = center + size * 0.5f;
                    Assert.That(
                        min.x,
                        Is.GreaterThanOrEqualTo(Phase1Bootstrap.ArenaMinX - 0.01f),
                        collider.name
                    );
                    Assert.That(
                        max.x,
                        Is.LessThanOrEqualTo(Phase1Bootstrap.ArenaMaxX + 0.01f),
                        collider.name
                    );
                    Assert.That(
                        min.y,
                        Is.GreaterThanOrEqualTo(Phase1Bootstrap.ArenaMinY - 0.01f),
                        collider.name
                    );
                    Assert.That(
                        max.y,
                        Is.LessThanOrEqualTo(Phase1Bootstrap.ArenaMaxY + 0.01f),
                        collider.name
                    );
                }

                foreach (var player in new[] { bootstrap.PlayerOne, bootstrap.PlayerTwo })
                {
                    var p = player.transform.position;
                    Assert.That(
                        p.x,
                        Is.InRange(Phase1Bootstrap.ArenaMinX, Phase1Bootstrap.ArenaMaxX)
                    );
                    Assert.That(
                        p.y,
                        Is.InRange(Phase1Bootstrap.ArenaMinY, Phase1Bootstrap.ArenaMaxY)
                    );
                }
            }
            finally
            {
                Checkpoint.ResetRegistry();
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Guarda-corpo do labirinto: simula o movimento do jogador célula a célula,
        /// com as regras do orçamento de pulo (degrau de 1, vão de até 3, ou 4 sob
        /// teto de 4), e confere que cada jogador chega à saída pelo próprio lado
        /// usando os mecanismos, que nenhum chega sem eles, que não há bolsão
        /// inalcançável e que os dois caminhos têm o mesmo comprimento.
        /// </summary>
        [Test]
        public void Phase1Bootstrap_MazeIsTraversable_ForEachPlayer_OnlyWithMechanisms()
        {
            var go = new GameObject("BootstrapHolder");
            try
            {
                var bootstrap = BuildBootstrap(go);
                var withMechanisms = new MazeSim(bootstrap, mechanismsEnabled: true);
                var withoutMechanisms = new MazeSim(bootstrap, mechanismsEnabled: false);

                var distances = new List<int>();
                foreach (
                    var (spawn, isLeft, label) in new[]
                    {
                        (Phase1Bootstrap.PlayerOneSpawnCell, true, "P1"),
                        (Phase1Bootstrap.PlayerTwoSpawnCell, false, "P2"),
                    }
                )
                {
                    var reached = withMechanisms.Explore(spawn);
                    var exits = Phase1Bootstrap.ExitCells.Where(reached.ContainsKey).ToList();
                    Assert.That(
                        exits,
                        Is.Not.Empty,
                        $"{label} must reach the exit with mechanisms."
                    );
                    distances.Add(exits.Min(e => reached[e]));

                    var blocked = withoutMechanisms.Explore(spawn);
                    Assert.That(
                        Phase1Bootstrap.ExitCells.Any(blocked.ContainsKey),
                        Is.False,
                        $"{label} must not reach the exit without elevators, box and door."
                    );

                    // Nenhuma célula em pé da própria metade fica órfã.
                    var orphans = new List<Vector2Int>();
                    for (var r = 0; r < Phase1Bootstrap.MazeRowCount; r++)
                    {
                        for (var c = 0; c < Phase1Bootstrap.MazeColumns; c++)
                        {
                            var inHalf = isLeft
                                ? c < Phase1Bootstrap.MazeColumns / 2
                                : c >= Phase1Bootstrap.MazeColumns / 2;
                            var cell = new Vector2Int(c, r);
                            if (
                                inHalf
                                && withMechanisms.CanStand(c, r)
                                && !reached.ContainsKey(cell)
                            )
                            {
                                orphans.Add(cell);
                            }
                        }
                    }
                    Assert.That(
                        orphans,
                        Is.Empty,
                        $"{label}: unreachable standing cells {string.Join(", ", orphans)}"
                    );
                }

                Assert.That(
                    distances[0],
                    Is.EqualTo(distances[1]),
                    "Both routes must be equally long."
                );
            }
            finally
            {
                Checkpoint.ResetRegistry();
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Regra pedagógica do labirinto: ultraprocessado fica no piso por onde se
        /// passa; saudável fica onde exige desvio, num nicho de salto ou num degrau
        /// sem saída. As metades somam os mesmos pontos, para a corrida ser justa.
        /// </summary>
        [Test]
        public void Phase1Bootstrap_FoodsFollowTheDetourRule_AndHalvesAreBalanced()
        {
            var go = new GameObject("BootstrapHolder");
            try
            {
                var bootstrap = BuildBootstrap(go);
                Assert.That(
                    bootstrap.UnresolvedFoodIds,
                    Is.Empty,
                    string.Join(", ", bootstrap.UnresolvedFoodIds)
                );
                Assert.That(bootstrap.FoodPlacements.Count, Is.EqualTo(18));

                var sim = new MazeSim(bootstrap, mechanismsEnabled: true);
                var reachable = new HashSet<Vector2Int>();
                foreach (
                    var spawn in new[]
                    {
                        Phase1Bootstrap.PlayerOneSpawnCell,
                        Phase1Bootstrap.PlayerTwoSpawnCell,
                    }
                )
                {
                    reachable.UnionWith(sim.Explore(spawn).Keys);
                }

                var foods = go.transform.Find("LevelGeometry/Coletaveis");
                var healthyPoints = new[] { 0, 0 };
                var ultraWellbeing = new[] { 0, 0 };

                foreach (var placement in bootstrap.FoodPlacements)
                {
                    var collectible = foods
                        .Find($"Food_{placement.Id}")
                        ?.GetComponent<Collectibles.FoodCollectible>();
                    Assert.That(collectible, Is.Not.Null, $"{placement.Id} must be spawned.");
                    Assert.That(collectible.FoodData, Is.Not.Null, $"{placement.Id} must resolve.");

                    var half = placement.Cell.x < Phase1Bootstrap.MazeColumns / 2 ? 0 : 1;
                    var isHealthy =
                        collectible.FoodData.Category == Collectibles.FoodCategory.Healthy;
                    var cell = placement.Cell;

                    if (isHealthy)
                    {
                        healthyPoints[half] += collectible.FoodData.BasePoints;
                        var isNotch = !sim.CanStand(cell.x, cell.y);
                        var isDeadEnd =
                            sim.CanStand(cell.x, cell.y)
                            && sim.Degree(cell) <= 1
                            && reachable.Contains(cell);
                        Assert.That(
                            isNotch || isDeadEnd,
                            Is.True,
                            $"{placement.Id} at {cell} must need a detour."
                        );

                        if (isNotch)
                        {
                            var grabbable = Enumerable
                                .Range(0, 4)
                                .Any(dy => reachable.Contains(new Vector2Int(cell.x, cell.y - dy)));
                            Assert.That(
                                grabbable,
                                Is.True,
                                $"{placement.Id} at {cell} must be grabbable from a reachable cell below."
                            );
                        }
                    }
                    else
                    {
                        ultraWellbeing[half] += collectible.FoodData.WellbeingDelta;
                        Assert.That(
                            sim.CanStand(cell.x, cell.y),
                            Is.True,
                            $"{placement.Id} at {cell} must sit on a corridor floor."
                        );
                        Assert.That(
                            reachable.Contains(cell),
                            Is.True,
                            $"{placement.Id} at {cell} must be reachable."
                        );
                        Assert.That(
                            sim.Degree(cell),
                            Is.GreaterThanOrEqualTo(2),
                            $"{placement.Id} at {cell} must be on a through path."
                        );
                    }
                }

                Assert.That(
                    healthyPoints[0],
                    Is.EqualTo(healthyPoints[1]),
                    "Healthy points must be equal on both halves."
                );
                Assert.That(
                    Mathf.Abs(ultraWellbeing[0] - ultraWellbeing[1]),
                    Is.LessThanOrEqualTo(2),
                    "Ultra-processed penalties must be balanced."
                );
            }
            finally
            {
                Checkpoint.ResetRegistry();
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Phase1Bootstrap_SpawnsEveryFood_ResolvedFromTheCatalog()
        {
            var go = new GameObject("BootstrapHolder");
            try
            {
                var bootstrap = BuildBootstrap(go);
                Assert.That(bootstrap.UnresolvedFoodIds, Is.Empty);

                var foods = go.transform.Find("LevelGeometry/Coletaveis");
                Assert.That(foods.childCount, Is.EqualTo(18));

                var healthy = 0;
                var ultraProcessed = 0;
                foreach (Transform food in foods)
                {
                    var data = food.GetComponent<Collectibles.FoodCollectible>().FoodData;
                    Assert.That(data, Is.Not.Null, $"{food.name} must have FoodData.");
                    if (data.Category == Collectibles.FoodCategory.Healthy)
                    {
                        healthy++;
                    }
                    else
                    {
                        ultraProcessed++;
                    }
                }

                Assert.That(healthy, Is.EqualTo(10));
                Assert.That(ultraProcessed, Is.EqualTo(8));
            }
            finally
            {
                Checkpoint.ResetRegistry();
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void JumpBudget_IsDerivedFromArenaPhysics_NotFromLiterals()
        {
            var budget = Phase1Bootstrap.DesignBudget();

            var expectedGravityScale = JumpBudget.GravityScaleForApex(
                Phase1Bootstrap.PhaseGravityY,
                12f,
                Phase1Bootstrap.TargetJumpApex,
                Phase1Bootstrap.FixedDeltaTime
            );
            Assert.That(
                budget.Gravity,
                Is.EqualTo(Mathf.Abs(Phase1Bootstrap.PhaseGravityY) * expectedGravityScale)
                    .Within(0.001f)
            );
            Assert.That(budget.Apex, Is.EqualTo(Phase1Bootstrap.TargetJumpApex).Within(0.01f));
            Assert.That(budget.IdealApex, Is.GreaterThan(budget.Apex));
            Assert.That(
                budget.IdealApex - budget.Apex,
                Is.EqualTo(budget.JumpVelocity * Phase1Bootstrap.FixedDeltaTime * 0.5f)
                    .Within(0.02f)
            );
            Assert.That(budget.Reach, Is.EqualTo(budget.MoveSpeed * budget.AirTime).Within(0.001f));
            Assert.That(budget.SafeStep, Is.LessThan(budget.Apex));
            Assert.That(budget.SafeGap, Is.LessThan(budget.Reach));

            // As regras do simulador do labirinto cabem no orçamento real: degrau de
            // 1 ladrilho, vão de 3 sob teto de 3 e vão de 4 sob teto de 4.
            Assert.That(Phase1Bootstrap.TileSize, Is.LessThanOrEqualTo(budget.SafeStep));
            Assert.That(
                3f * Phase1Bootstrap.TileSize - 0.3f,
                Is.LessThanOrEqualTo(budget.Reach * 0.8f)
            );
            Assert.That(4f * Phase1Bootstrap.TileSize - 0.3f, Is.LessThanOrEqualTo(budget.SafeGap));
            Assert.That(
                Phase1Bootstrap.CorridorClearance,
                Is.GreaterThan(1.25f + Phase1Bootstrap.TileSize)
            );

            // O espaçamento entre corredores excede o ápice de propósito.
            Assert.That(
                Phase1Bootstrap.Tier1Top - Phase1Bootstrap.Tier0Top,
                Is.GreaterThan(budget.Apex)
            );
        }

        // -----------------------------------------------------------------
        // Simulador de travessia sobre o mapa de ladrilhos
        // -----------------------------------------------------------------

        /// <summary>
        /// Reproduz o alcance do jogador no mapa: andar, subir 1 ladrilho, cair,
        /// saltar vãos de até 3 ladrilhos (4 sob teto de 4) e usar os mecanismos
        /// registrados pelo bootstrap. Portões contam como rocha sem mecanismos.
        /// </summary>
        private sealed class MazeSim
        {
            private readonly HashSet<Vector2Int> _floors = new();
            private readonly HashSet<Vector2Int> _blocked = new();
            private readonly Dictionary<Vector2Int, List<Vector2Int>> _links = new();

            public MazeSim(Phase1Bootstrap bootstrap, bool mechanismsEnabled)
            {
                if (bootstrap == null)
                {
                    return;
                }

                foreach (var elevator in bootstrap.Elevators)
                {
                    for (var c = elevator.MinColumn; c <= elevator.MaxColumn; c++)
                    {
                        _floors.Add(new Vector2Int(c, elevator.BottomRow));
                        if (mechanismsEnabled)
                        {
                            _floors.Add(new Vector2Int(c, elevator.TopRow));
                            AddLink(
                                new Vector2Int(c, elevator.BottomRow),
                                new Vector2Int(c, elevator.TopRow)
                            );
                        }
                    }
                }

                if (!mechanismsEnabled)
                {
                    foreach (var door in bootstrap.Doors)
                    {
                        for (var r = door.MinRow; r <= door.MaxRow; r++)
                        {
                            _blocked.Add(new Vector2Int(door.Column, r));
                        }
                    }
                }
                else
                {
                    foreach (var climb in bootstrap.BoxClimbs)
                    {
                        AddLink(climb.From, climb.To);
                    }
                }
            }

            private void AddLink(Vector2Int from, Vector2Int to)
            {
                if (!_links.TryGetValue(from, out var list))
                {
                    list = new List<Vector2Int>();
                    _links[from] = list;
                }
                list.Add(to);
            }

            public bool Open(int c, int r)
            {
                return !Phase1Bootstrap.IsRock(c, r) && !_blocked.Contains(new Vector2Int(c, r));
            }

            public bool HasFloor(int c, int r)
            {
                return _floors.Contains(new Vector2Int(c, r)) || Phase1Bootstrap.IsRock(c, r - 1);
            }

            public bool CanStand(int c, int r)
            {
                return Open(c, r) && Open(c, r + 1) && HasFloor(c, r);
            }

            private Vector2Int? Land(int c, int r)
            {
                while (r > 0 && !HasFloor(c, r))
                {
                    if (!Open(c, r - 1))
                    {
                        return null;
                    }
                    r--;
                }
                return CanStand(c, r) ? new Vector2Int(c, r) : null;
            }

            /// <summary>Vizinhos alcançáveis a partir de uma célula em pé.</summary>
            public IEnumerable<Vector2Int> Neighbors(Vector2Int cur, bool includeJumpsAndLinks)
            {
                var c = cur.x;
                var r = cur.y;
                foreach (var d in new[] { -1, 1 })
                {
                    var nc = c + d;
                    if (Open(nc, r) && Open(nc, r + 1))
                    {
                        if (HasFloor(nc, r))
                        {
                            yield return new Vector2Int(nc, r);
                        }
                        else if (Land(nc, r) is Vector2Int landed)
                        {
                            yield return landed;
                        }
                    }

                    if (Open(c, r + 2) && Open(nc, r + 1) && Open(nc, r + 2) && HasFloor(nc, r + 1))
                    {
                        yield return new Vector2Int(nc, r + 1);
                    }

                    if (!includeJumpsAndLinks)
                    {
                        continue;
                    }

                    for (var k = 2; k <= 4; k++)
                    {
                        var needRows = k == 4 ? 3 : 2;
                        var headroom = true;
                        for (var h = 2; h <= needRows; h++)
                        {
                            headroom &= Open(c, r + h);
                        }
                        if (!headroom)
                        {
                            break;
                        }

                        var arcOk = true;
                        for (var i = 1; i < k && arcOk; i++)
                        {
                            for (var h = 0; h <= needRows; h++)
                            {
                                arcOk &= Open(c + d * i, r + h);
                            }
                        }
                        if (!arcOk)
                        {
                            break;
                        }

                        var lc = c + d * k;
                        var landedOnLedge = false;
                        foreach (var lr in new[] { r + 1, r })
                        {
                            if (lr == r + 1 && !Open(lc, r + 2))
                            {
                                continue;
                            }
                            if (CanStand(lc, lr) && Open(lc, lr + 1))
                            {
                                yield return new Vector2Int(lc, lr);
                                landedOnLedge = true;
                                break;
                            }
                        }
                        if (
                            !landedOnLedge
                            && Open(lc, r)
                            && Open(lc, r + 1)
                            && Land(lc, r) is Vector2Int fell
                        )
                        {
                            yield return fell;
                        }
                    }
                }

                if (includeJumpsAndLinks && _links.TryGetValue(cur, out var targets))
                {
                    foreach (var to in targets)
                    {
                        if (CanStand(to.x, to.y))
                        {
                            yield return to;
                        }
                    }
                }
            }

            /// <summary>Busca em largura; devolve a distância em movimentos até cada célula alcançada.</summary>
            public Dictionary<Vector2Int, int> Explore(Vector2Int start)
            {
                var dist = new Dictionary<Vector2Int, int> { [start] = 0 };
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    foreach (var next in Neighbors(cur, includeJumpsAndLinks: true))
                    {
                        if (!dist.ContainsKey(next))
                        {
                            dist[next] = dist[cur] + 1;
                            queue.Enqueue(next);
                        }
                    }
                }
                return dist;
            }

            /// <summary>Quantas células em pé distintas se alcançam andando, subindo 1 ou caindo, sem saltos.</summary>
            public int Degree(Vector2Int cell)
            {
                return Neighbors(cell, includeJumpsAndLinks: false).Distinct().Count();
            }
        }

        private static FinishLine CreateDoor(int playerIndex, List<GameObject> created)
        {
            var go = new GameObject($"Door{playerIndex}", typeof(BoxCollider2D));
            created.Add(go);
            var door = go.AddComponent<FinishLine>();
            door.ConfigureAcceptedPlayer(playerIndex);
            return door;
        }

        private static Phase1Bootstrap BuildBootstrap(GameObject holder)
        {
            Checkpoint.ResetRegistry();
            var bootstrap = holder.AddComponent<Phase1Bootstrap>();

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_playerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            serialized.FindProperty("_collectiblePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(CollectiblePrefabPath);
            serialized.FindProperty("_pushableBoxPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(PushableBoxPrefabPath);

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
                typeof(Phase1Bootstrap)
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
