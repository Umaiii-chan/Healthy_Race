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
                Assert.That(
                    bootstrapObject,
                    Is.Not.Null,
                    "Phase1_CorridaDoEquilibrio GameObject must exist in scene."
                );

                var bootstrap = bootstrapObject.GetComponent<Phase1Bootstrap>();
                Assert.That(bootstrap, Is.Not.Null, "Phase1Bootstrap component must exist.");

                var serialized = new SerializedObject(bootstrap);
                var playerProp = serialized.FindProperty("_playerPrefab");
                var collectibleProp = serialized.FindProperty("_collectiblePrefab");
                var boxProp = serialized.FindProperty("_pushableBoxPrefab");
                var foodCatalogProp = serialized.FindProperty("_foodCatalog");

                Assert.That(playerProp.objectReferenceValue, Is.Not.Null);
                Assert.That(collectibleProp.objectReferenceValue, Is.Not.Null);
                Assert.That(boxProp.objectReferenceValue, Is.Not.Null);
                Assert.That(foodCatalogProp, Is.Not.Null);
                Assert.That(
                    foodCatalogProp.arraySize,
                    Is.EqualTo(18),
                    "_foodCatalog must contain all 18 MVP food items."
                );

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
        public void Phase1Bootstrap_BuildsArenaTiersGatesAndDoors()
        {
            var go = new GameObject("BootstrapHolder");
            try
            {
                var bootstrap = BuildBootstrap(go);

                Assert.That(bootstrap.PlayerOne, Is.Not.Null);
                Assert.That(bootstrap.PlayerTwo, Is.Not.Null);
                Assert.That(bootstrap.PlayerOneHud, Is.Not.Null);
                Assert.That(bootstrap.PlayerTwoHud, Is.Not.Null);
                Assert.That(bootstrap.MatchHud, Is.Not.Null);
                Assert.That(bootstrap.VisualCatalog, Is.Not.Null);
                Assert.That(bootstrap.PlayerOne.transform.Find("VisualPresentation"), Is.Not.Null);
                Assert.That(bootstrap.PlayerTwo.transform.Find("VisualPresentation"), Is.Not.Null);

                var geometry = go.transform.Find("LevelGeometry");
                Assert.That(geometry, Is.Not.Null);
                foreach (
                    var group in new[]
                    {
                        "Arena_Casca",
                        "T0_Base",
                        "T1_Ilhas",
                        "T2_Rotas",
                        "T3_Travessia",
                        "T4_Portas",
                        "G1_BotoesPlataforma",
                        "G2_AlavancaPortao",
                        "G3_CaixaPlataforma",
                        "Perigos",
                        "Coletaveis",
                    }
                )
                {
                    Assert.That(geometry.Find(group), Is.Not.Null, $"{group} must exist.");
                }

                // Cinco patamares empilhados com espaçamento de 3.4 m
                Assert.That(Phase1Bootstrap.TierTops.Length, Is.EqualTo(5));
                for (var i = 1; i < Phase1Bootstrap.TierTops.Length; i++)
                {
                    Assert.That(
                        Phase1Bootstrap.TierTops[i] - Phase1Bootstrap.TierTops[i - 1],
                        Is.EqualTo(3.4f).Within(0.001f)
                    );
                    Assert.That(
                        bootstrap.Surfaces.Any(s =>
                            Mathf.Abs(s.TopY - Phase1Bootstrap.TierTops[i]) < 0.001f
                        ),
                        Is.True,
                        $"Tier {i} must have at least one surface."
                    );
                }

                Assert.That(bootstrap.Checkpoints.Count, Is.EqualTo(3));
                Assert.That(
                    bootstrap.Checkpoints.Select(c => c.Index),
                    Is.EqualTo(new[] { 1, 2, 3 })
                );

                // Três portais cooperativos + retornos, todos amarrados por controlador
                Assert.That(bootstrap.GateOne, Is.Not.Null);
                Assert.That(bootstrap.GateTwo, Is.Not.Null);
                Assert.That(bootstrap.GateThree, Is.Not.Null);
                Assert.That(bootstrap.Gates.Count, Is.GreaterThanOrEqualTo(3));
                Assert.That(bootstrap.GateOne.Button, Is.Not.Null);
                Assert.That(bootstrap.GateOne.Platform, Is.Not.Null);
                Assert.That(bootstrap.GateTwo.Lever, Is.Not.Null);
                Assert.That(bootstrap.GateTwo.Door, Is.Not.Null);
                Assert.That(bootstrap.GateThree.Button, Is.Not.Null);
                Assert.That(bootstrap.GateThree.Box, Is.Not.Null);
                Assert.That(bootstrap.GateThree.Platform, Is.Not.Null);
                Assert.That(
                    bootstrap.GateThree.ResetSwitch,
                    Is.Not.Null,
                    "G3 needs a reset switch."
                );

                // Duas portas, uma por jogador, no patamar T4
                Assert.That(bootstrap.PlayerOneDoor, Is.Not.Null);
                Assert.That(bootstrap.PlayerTwoDoor, Is.Not.Null);
                Assert.That(bootstrap.PlayerOneDoor.AcceptedPlayerIndex, Is.EqualTo(1));
                Assert.That(bootstrap.PlayerTwoDoor.AcceptedPlayerIndex, Is.EqualTo(2));
                Assert.That(
                    bootstrap.PlayerOneDoor.transform.position.y,
                    Is.GreaterThan(Phase1Bootstrap.Tier4Top)
                );
                Assert.That(
                    bootstrap.PlayerTwoDoor.transform.position.y,
                    Is.GreaterThan(Phase1Bootstrap.Tier4Top)
                );
                Assert.That(bootstrap.MatchController.RequireBothAtDoors, Is.True);

                // Perigos
                Assert.That(bootstrap.MudHazards.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(bootstrap.Spikes.Count, Is.GreaterThanOrEqualTo(1));
                Assert.That(bootstrap.FallingPlatforms.Count, Is.GreaterThanOrEqualTo(2));

                // Câmera fixa
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
                        continue; // paredes e teto ficam propositalmente na borda externa
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

                // Coletáveis: ultraprocessados só em T0; saudáveis só acima de T0
                var foods = geometry.Find("Coletaveis");
                foreach (Transform food in foods)
                {
                    var collectible = food.GetComponent<Collectibles.FoodCollectible>();
                    Assert.That(
                        collectible,
                        Is.Not.Null,
                        $"{food.name} must be a FoodCollectible."
                    );

                    // Pular os nulos aqui era o furo que escondia o bug de caixa dos
                    // ids: todos os 18 alimentos resolviam para nulo e o teste passava.
                    Assert.That(
                        collectible.FoodData,
                        Is.Not.Null,
                        $"{food.name} must resolve to a FoodData in the catalog."
                    );

                    var isHealthy =
                        collectible.FoodData.Category == Collectibles.FoodCategory.Healthy;
                    var aboveBase = food.position.y > Phase1Bootstrap.Tier1Top;
                    Assert.That(
                        aboveBase,
                        Is.EqualTo(isHealthy),
                        $"{food.name}: healthy foods reward climbing, ultra-processed stay on T0."
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
        /// Guarda-corpo do design: percorre as superfícies registradas e falha se
        /// alguma só puder ser alcançada com desnível &gt; 1.8 m ou vão &gt; 4.0 m sem um
        /// mecanismo cooperativo registrado. Também exige que a cooperação seja
        /// realmente necessária (sem mecanismos, o topo deve ficar inalcançável).
        /// </summary>
        [Test]
        public void Phase1Bootstrap_ArenaIsTraversable_WithinJumpBudget_UsingCooperativeLinks()
        {
            var go = new GameObject("BootstrapHolder");
            try
            {
                var bootstrap = BuildBootstrap(go);
                var surfaces = bootstrap.Surfaces.ToDictionary(s => s.Name, s => s);
                Assert.That(surfaces.Count, Is.GreaterThan(10));

                foreach (var link in bootstrap.CooperativeLinks)
                {
                    Assert.That(
                        surfaces.ContainsKey(link.From),
                        Is.True,
                        $"Link from '{link.From}' unknown."
                    );
                    Assert.That(
                        surfaces.ContainsKey(link.To),
                        Is.True,
                        $"Link to '{link.To}' unknown."
                    );
                    Assert.That(link.Mechanism, Is.Not.Empty);
                }

                var reachableWithLinks = Traverse(surfaces, bootstrap.CooperativeLinks, "T0_Piso");
                var unreachable = surfaces.Keys.Except(reachableWithLinks).ToList();
                Assert.That(
                    unreachable,
                    Is.Empty,
                    "Every surface must be reachable within the derived jump budget or a cooperative link: "
                        + string.Join(", ", unreachable)
                );

                var reachableWithoutLinks = Traverse(surfaces, new ArenaLink[0], "T0_Piso");
                Assert.That(
                    reachableWithoutLinks.Contains("T4_Portas"),
                    Is.False,
                    "Doors must not be reachable without cooperative mechanisms."
                );
                Assert.That(
                    reachableWithoutLinks.Any(name =>
                        Mathf.Abs(surfaces[name].TopY - Phase1Bootstrap.Tier1Top) < 0.01f
                    ),
                    Is.False,
                    "T1 must not be reachable without G1."
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

                Assert.That(
                    bootstrap.UnresolvedFoodIds,
                    Is.Empty,
                    "Every SpawnFood id must exist in the catalog: "
                        + string.Join(", ", bootstrap.UnresolvedFoodIds)
                );

                var foods = go.transform.Find("LevelGeometry/Coletaveis");
                Assert.That(foods, Is.Not.Null);
                Assert.That(
                    foods.childCount,
                    Is.EqualTo(18),
                    "The arena must spawn all 18 MVP foods."
                );

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

                // A altura da arena define o tamanho; a proporção é garantida pelo rect.
                Assert.That(
                    controller.CalculateFixedOrthographicSize(),
                    Is.EqualTo((Phase1Bootstrap.ArenaMaxY - Phase1Bootstrap.ArenaMinY) * 0.5f)
                        .Within(0.001f)
                );

                var cam = camObj.GetComponent<Camera>();
                var drawn = cam.rect;
                var arenaAspect =
                    (Phase1Bootstrap.ArenaMaxX - Phase1Bootstrap.ArenaMinX)
                    / (Phase1Bootstrap.ArenaMaxY - Phase1Bootstrap.ArenaMinY);
                var drawnAspect =
                    ((float)Screen.width * drawn.width) / (Screen.height * drawn.height);

                Assert.That(
                    drawnAspect,
                    Is.EqualTo(arenaAspect).Within(0.01f),
                    "The drawn viewport must have the arena aspect, so nothing beyond the walls shows."
                );
                Assert.That(drawn.width, Is.LessThanOrEqualTo(1f));
                Assert.That(drawn.height, Is.LessThanOrEqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(camObj);
            }
        }

        [Test]
        public void JumpBudget_IsDerivedFromArenaPhysics_NotFromLiterals()
        {
            var budget = Phase1Bootstrap.DesignBudget();

            // g = |gravity| * gravityScale derivado do ápice de projeto
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

            // A fórmula contínua superestima o ápice em ~v0*dt/2; o orçamento usa o
            // valor do integrador discreto, que é o que o jogador realmente alcança.
            Assert.That(budget.IdealApex, Is.GreaterThan(budget.Apex));
            Assert.That(
                budget.IdealApex - budget.Apex,
                Is.EqualTo(budget.JumpVelocity * Phase1Bootstrap.FixedDeltaTime * 0.5f)
                    .Within(0.02f)
            );
            // O alcance é consequência do ápice e do passo de física, nunca um número
            // escolhido: com gravidade menor o tempo no ar cresce e o alcance junto.
            Assert.That(budget.Reach, Is.EqualTo(budget.MoveSpeed * budget.AirTime).Within(0.001f));
            Assert.That(budget.Reach, Is.GreaterThan(budget.Apex * 2f));
            Assert.That(budget.SafeStep, Is.LessThan(budget.Apex));
            Assert.That(budget.SafeGap, Is.LessThan(budget.Reach));

            // O orçamento acompanha a física: dobrar a gravidade reduz o ápice à metade.
            var heavier = JumpBudget.FromPhysics(
                Phase1Bootstrap.PhaseGravityY,
                budget.Gravity / Mathf.Abs(Phase1Bootstrap.PhaseGravityY) * 2f,
                12f,
                7f,
                Phase1Bootstrap.FixedDeltaTime
            );
            Assert.That(heavier.Apex, Is.LessThan(budget.Apex * 0.55f));
            Assert.That(heavier.Apex, Is.GreaterThan(budget.Apex * 0.45f));

            // O vão entre patamares (3.4 m) excede o ápice de propósito.
            Assert.That(
                Phase1Bootstrap.Tier1Top - Phase1Bootstrap.Tier0Top,
                Is.GreaterThan(budget.Apex)
            );

            // Caixa como degrau: topo em 9.02 e AP3 erguida em 10.8.
            var boxTop = Phase1Bootstrap.Tier2Top + Phase1Bootstrap.BoxHeight;
            Assert.That(
                Phase1Bootstrap.Tier2Top + 3f - boxTop,
                Is.LessThanOrEqualTo(budget.SafeStep)
            );
        }

        private static HashSet<string> Traverse(
            Dictionary<string, ArenaSurface> surfaces,
            IEnumerable<ArenaLink> links,
            string start
        )
        {
            var visited = new HashSet<string> { start };
            var queue = new Queue<string>();
            queue.Enqueue(start);
            var linkList = links.ToList();

            while (queue.Count > 0)
            {
                var current = surfaces[queue.Dequeue()];
                foreach (var candidate in surfaces.Values)
                {
                    if (visited.Contains(candidate.Name))
                    {
                        continue;
                    }

                    if (
                        CanJump(current, candidate)
                        || linkList.Any(l => l.From == current.Name && l.To == candidate.Name)
                    )
                    {
                        visited.Add(candidate.Name);
                        queue.Enqueue(candidate.Name);
                    }
                }
            }

            return visited;
        }

        private static bool CanJump(ArenaSurface from, ArenaSurface to)
        {
            var gap = Mathf.Max(0f, to.MinX - from.MaxX, from.MinX - to.MaxX);
            var rise = to.TopY - from.TopY;
            return Phase1Bootstrap.DesignBudget().CanClear(rise, gap);
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
                var awakeMethod = typeof(Phase1Bootstrap).GetMethod(
                    "Awake",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                awakeMethod.Invoke(bootstrap, null);
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
