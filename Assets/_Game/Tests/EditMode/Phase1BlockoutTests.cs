using System.Linq;
using System.Reflection;
using HealthyRace.Collectibles;
using HealthyRace.Core;
using HealthyRace.Level;
using HealthyRace.Player;
using HealthyRace.UI;
using HealthyRace.Visuals;
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

                Assert.That(
                    playerProp.objectReferenceValue,
                    Is.Not.Null,
                    "_playerPrefab must be assigned."
                );
                Assert.That(
                    collectibleProp.objectReferenceValue,
                    Is.Not.Null,
                    "_collectiblePrefab must be assigned."
                );
                Assert.That(
                    boxProp.objectReferenceValue,
                    Is.Not.Null,
                    "_pushableBoxPrefab must be assigned."
                );
                Assert.That(
                    foodCatalogProp,
                    Is.Not.Null,
                    "_foodCatalog array property must exist."
                );
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
                var rootObjects = scene.GetRootGameObjects();
                var bootstrapObject = rootObjects.FirstOrDefault(go =>
                    go.name == "Phase1_CorridaDoEquilibrio"
                );
                Assert.That(bootstrapObject, Is.Not.Null);

                var bootstrap = bootstrapObject.GetComponent<Phase1Bootstrap>();
                Assert.That(bootstrap, Is.Not.Null);

                var serialized = new SerializedObject(bootstrap);
                var visualCatalogProp = serialized.FindProperty("_visualCatalog");
                var p1VisualProp = serialized.FindProperty("_playerOneVisualPrefab");
                var p2VisualProp = serialized.FindProperty("_playerTwoVisualPrefab");

                Assert.That(
                    visualCatalogProp.objectReferenceValue,
                    Is.Not.Null,
                    "_visualCatalog must be assigned in scene."
                );
                Assert.That(
                    p1VisualProp.objectReferenceValue,
                    Is.Not.Null,
                    "_playerOneVisualPrefab must be assigned in scene."
                );
                Assert.That(
                    p2VisualProp.objectReferenceValue,
                    Is.Not.Null,
                    "_playerTwoVisualPrefab must be assigned in scene."
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

                Assert.That(cp.Index, Is.EqualTo(1));
                Assert.That(cp.ActivatedPlayerCount, Is.EqualTo(0));
                Assert.That(cp.IsActivatedByPlayer(1), Is.False);
                Assert.That(cp.IsActivatedByPlayer(2), Is.False);

                var reachedP1 = false;
                cp.CheckpointReached += (p) =>
                {
                    if (p == 1)
                    {
                        reachedP1 = true;
                    }
                };

                var firstActivate = cp.ActivateForPlayer(1);
                Assert.That(
                    firstActivate,
                    Is.True,
                    "First activation for Player 1 should succeed."
                );
                Assert.That(reachedP1, Is.True);
                Assert.That(cp.IsActivatedByPlayer(1), Is.True);
                Assert.That(cp.IsActivatedByPlayer(2), Is.False, "Player 2 must remain isolated.");

                var repeatActivate = cp.ActivateForPlayer(1);
                Assert.That(
                    repeatActivate,
                    Is.False,
                    "Repeat activation for same player must be ignored."
                );

                var p2Activate = cp.ActivateForPlayer(2);
                Assert.That(
                    p2Activate,
                    Is.True,
                    "Player 2 activation should succeed independently."
                );
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
                Assert.That(fl.FinishedPlayerCount, Is.EqualTo(0));

                var finishP1 = fl.RecordFinish(1);
                Assert.That(finishP1, Is.True);
                Assert.That(fl.HasPlayerFinished(1), Is.True);
                Assert.That(
                    fl.HasPlayerFinished(2),
                    Is.False,
                    "Player 2 should not be marked as finished."
                );
                Assert.That(fl.HasAnyPlayerFinished, Is.True);
                Assert.That(fl.FinishedPlayerCount, Is.EqualTo(1));

                var repeatP1 = fl.RecordFinish(1);
                Assert.That(
                    repeatP1,
                    Is.False,
                    "Repeat finish record for same player must return false."
                );

                var finishP2 = fl.RecordFinish(2);
                Assert.That(finishP2, Is.True);
                Assert.That(fl.FinishedPlayerCount, Is.EqualTo(2));

                fl.ResetState();
                Assert.That(fl.FinishedPlayerCount, Is.EqualTo(0));
                Assert.That(fl.HasAnyPlayerFinished, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Phase1Bootstrap_BuildsAllSixZonesAndRequiredActors()
        {
            var go = new GameObject("BootstrapHolder");
            try
            {
                var bootstrap = go.AddComponent<Phase1Bootstrap>();

                var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                var collectiblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    CollectiblePrefabPath
                );
                var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PushableBoxPrefabPath);

                var serialized = new SerializedObject(bootstrap);
                serialized.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab;
                serialized.FindProperty("_collectiblePrefab").objectReferenceValue =
                    collectiblePrefab;
                serialized.FindProperty("_pushableBoxPrefab").objectReferenceValue = boxPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // Trigger Awake via reflection
                var awakeMethod = typeof(Phase1Bootstrap).GetMethod(
                    "Awake",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                awakeMethod.Invoke(bootstrap, null);

                Assert.That(bootstrap.PlayerOne, Is.Not.Null, "Player One must be spawned.");
                Assert.That(bootstrap.PlayerTwo, Is.Not.Null, "Player Two must be spawned.");
                Assert.That(bootstrap.PlayerOneHud, Is.Not.Null, "Player One HUD must exist.");
                Assert.That(bootstrap.PlayerTwoHud, Is.Not.Null, "Player Two HUD must exist.");
                Assert.That(bootstrap.MatchHud, Is.Not.Null, "Match HUD must exist.");
                Assert.That(
                    bootstrap.VisualCatalog,
                    Is.Not.Null,
                    "VisualCatalog must be auto-resolved."
                );
                Assert.That(
                    bootstrap.PlayerOne.transform.Find("VisualPresentation"),
                    Is.Not.Null,
                    "P1 must have VisualPresentation child."
                );
                Assert.That(
                    bootstrap.PlayerTwo.transform.Find("VisualPresentation"),
                    Is.Not.Null,
                    "P2 must have VisualPresentation child."
                );

                // Validate 6 zones
                var levelGeometry = go.transform.Find("LevelGeometry");
                Assert.That(levelGeometry, Is.Not.Null, "LevelGeometry container must exist.");
                Assert.That(levelGeometry.Find("Z1_Spawn"), Is.Not.Null, "Z1_Spawn must exist.");
                Assert.That(levelGeometry.Find("Z2_Caixa"), Is.Not.Null, "Z2_Caixa must exist.");
                Assert.That(levelGeometry.Find("Z3_Rotas"), Is.Not.Null, "Z3_Rotas must exist.");
                Assert.That(
                    levelGeometry.Find("Z4_Reencontro"),
                    Is.Not.Null,
                    "Z4_Reencontro must exist."
                );
                Assert.That(
                    levelGeometry.Find("Z5_PuzzleFinal"),
                    Is.Not.Null,
                    "Z5_PuzzleFinal must exist."
                );
                Assert.That(
                    levelGeometry.Find("Z6_Chegada"),
                    Is.Not.Null,
                    "Z6_Chegada must exist."
                );

                // Validate 2 checkpoints
                Assert.That(
                    bootstrap.Checkpoints.Count,
                    Is.EqualTo(2),
                    "Must have exactly 2 checkpoints."
                );
                Assert.That(bootstrap.Checkpoints[0].Index, Is.EqualTo(1));
                Assert.That(bootstrap.Checkpoints[1].Index, Is.EqualTo(2));

                // Validate Finish Line
                Assert.That(
                    bootstrap.FinishLine,
                    Is.Not.Null,
                    "FinishLine must exist in bootstrap."
                );
                Assert.That(bootstrap.FinishLine.transform.position.x, Is.GreaterThan(200f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Phase1Scene_AllPlacedFoodsResolveTheirCatalogData()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var bootstrap = scene
                    .GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Phase1Bootstrap>())
                    .Single();
                typeof(Phase1Bootstrap)
                    .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(bootstrap, null);

                var foods = bootstrap.GetComponentsInChildren<FoodCollectible>();
                Assert.That(foods, Is.Not.Empty);
                foreach (var food in foods)
                {
                    Assert.That(
                        food.FoodData,
                        Is.Not.Null,
                        $"{food.name} has no collectible data."
                    );
                    Assert.That(
                        food.name,
                        Is.EqualTo($"Food_{food.FoodData.Id}").IgnoreCase,
                        "The placement must resolve its own food, not a fallback."
                    );
                    Assert.That(
                        food.GetComponent<SpriteRenderer>().sprite,
                        Is.SameAs(food.FoodData.Sprite)
                    );
                }
                Assert.That(foods.Any(food => food.FoodData.Category == FoodCategory.Healthy));
                Assert.That(
                    foods.Any(food => food.FoodData.Category == FoodCategory.UltraProcessed)
                );
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void Phase1Redesign_HasThreeRoutesVerticalConnectionsAndLandmarks()
        {
            var bootstrap = BuildPhase1ForInspection(out var holder);
            try
            {
                var geometry = holder.transform.Find("LevelGeometry");
                var routes = geometry.Find("Z3_Rotas");
                Assert.That(routes, Is.Not.Null);

                var blocks = routes.GetComponentsInChildren<BoxCollider2D>();
                Assert.That(
                    blocks.Count(c => c.name.StartsWith("Upper_")),
                    Is.GreaterThanOrEqualTo(7)
                );
                Assert.That(
                    blocks.Count(c => c.name.StartsWith("Central_")),
                    Is.GreaterThanOrEqualTo(6)
                );
                Assert.That(
                    blocks.Count(c => c.name.StartsWith("Lower_")),
                    Is.GreaterThanOrEqualTo(7)
                );
                Assert.That(
                    blocks.Count(c => c.name.StartsWith("Z3_Connector")),
                    Is.GreaterThanOrEqualTo(4)
                );

                var minY = blocks.Min(c => c.bounds.min.y);
                var maxY = blocks.Max(c => c.bounds.max.y);
                Assert.That(maxY - minY, Is.GreaterThan(7f), "The route room must use height.");

                var landmarks = geometry
                    .GetComponentsInChildren<Transform>()
                    .Count(t => t.name.StartsWith("Landmark_") || t.name.Contains("Landmark_"));
                Assert.That(landmarks, Is.GreaterThanOrEqualTo(5));
                Assert.That(bootstrap.Checkpoints.Count, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void Phase1Redesign_FinalPuzzleRequiresBothSignalsAndHasNoGateBypass()
        {
            var bootstrap = BuildPhase1ForInspection(out var holder);
            try
            {
                Assert.That(bootstrap.FinalPuzzle, Is.Not.Null);
                Assert.That(bootstrap.FinalPuzzle.RequireAll, Is.True);
                Assert.That(bootstrap.FinalPuzzle.Button, Is.Not.Null);
                Assert.That(bootstrap.FinalPuzzle.Lever, Is.Not.Null);
                Assert.That(bootstrap.FinalPuzzle.Box, Is.Not.Null);
                Assert.That(bootstrap.FinalPuzzle.Door, Is.Not.Null);
                Assert.That(bootstrap.FinalPuzzle.ResetSwitch, Is.Not.Null);

                var puzzle = holder.transform.Find("LevelGeometry/Z5_PuzzleFinal");
                Assert.That(puzzle.Find("Z5_CooperationLift"), Is.Not.Null);
                Assert.That(puzzle.Find("Z5_RecoveryStepA"), Is.Not.Null);
                Assert.That(puzzle.Find("Z5_RecoveryStepB"), Is.Not.Null);
                Assert.That(puzzle.Find("Z5_Step1"), Is.Null, "The old free bypass must be gone.");
                Assert.That(puzzle.Find("Z5_Step2"), Is.Null, "The old free bypass must be gone.");
                Assert.That(puzzle.Find("Z5_Step3"), Is.Null, "The old free bypass must be gone.");

                var button = bootstrap.FinalPuzzle.Button;
                var lever = bootstrap.FinalPuzzle.Lever;
                var door = bootstrap.FinalPuzzle.Door;
                button.ForceSetPressed(true);
                Assert.That(door.IsOpen, Is.False, "The button alone cannot open the final gate.");
                button.ForceSetPressed(false);
                lever.SetState(true);
                Assert.That(door.IsOpen, Is.False, "The lever alone cannot open the final gate.");
                button.ForceSetPressed(true);
                Assert.That(door.IsOpen, Is.True, "Both signals must open the final gate.");

                var doorCollider = door.GetComponent<BoxCollider2D>();
                var leftTower = puzzle.Find("Landmark_BalanceTower/TowerLeft");
                var rightTower = puzzle.Find("Landmark_BalanceTower/TowerRight");
                Assert.That(leftTower, Is.Not.Null);
                Assert.That(rightTower, Is.Not.Null);
                var leftGap =
                    doorCollider.bounds.min.x
                    - leftTower.GetComponent<BoxCollider2D>().bounds.max.x;
                var rightGap =
                    rightTower.GetComponent<BoxCollider2D>().bounds.min.x
                    - doorCollider.bounds.max.x;
                Assert.That(leftGap, Is.GreaterThan(1.5f), "The left gate passage is too narrow.");
                Assert.That(
                    rightGap,
                    Is.GreaterThan(1.5f),
                    "The right gate passage is too narrow."
                );
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void Phase1Redesign_ThreeRoutesRespectThePlayerJumpEnvelope()
        {
            BuildPhase1ForInspection(out var holder);
            try
            {
                var routes = holder.transform.Find("LevelGeometry/Z3_Rotas");

                // Limites vindos da física real, não de literais.
                var budget = Phase1Bootstrap.DesignBudget();
                var maxGap = budget.Reach;
                var maxRise = budget.Apex;

                AssertSurfaceChain(
                    routes,
                    collider => collider.name.StartsWith("Upper_") && !collider.isTrigger,
                    maxGap,
                    maxRise,
                    "upper canopy"
                );
                AssertSurfaceChain(
                    routes,
                    collider => collider.name.StartsWith("Central_Bridge"),
                    maxGap,
                    maxRise,
                    "central bridge"
                );
                AssertSurfaceChain(
                    routes,
                    collider => collider.name.StartsWith("Lower_CreekBank"),
                    maxGap,
                    maxRise,
                    "lower creek"
                );
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void Phase1Redesign_GardenLiftIsAccessibleAndConnectsToUpperGarden()
        {
            BuildPhase1ForInspection(out var holder);
            try
            {
                var zone = holder.transform.Find("LevelGeometry/Z2_Caixa");
                var lift = zone.Find("Z2_GardenLift").GetComponent<BoxCollider2D>();
                var wall = zone.Find("Z2_GardenWall").GetComponent<BoxCollider2D>();
                var garden = zone.Find("Z2_UpperGarden").GetComponent<BoxCollider2D>();

                Assert.That(
                    lift.bounds.max.x,
                    Is.LessThanOrEqualTo(wall.bounds.min.x + 0.01f),
                    "The lift must start on the accessible side of the garden wall."
                );

                var raisedTop = lift.bounds.max.y + 2.7f;
                Assert.That(
                    Mathf.Abs(raisedTop - garden.bounds.max.y),
                    Is.LessThanOrEqualTo(0.15f),
                    "The raised lift must align with the upper garden."
                );
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void Phase1Redesign_RouteConnectorsRemainInsideSharedCameraEnvelope()
        {
            var bootstrap = BuildPhase1ForInspection(out var holder);
            try
            {
                var camera = holder.GetComponentInChildren<SharedCameraController>();
                Assert.That(camera, Is.Not.Null);
                var pairs = new[]
                {
                    (new Vector2(66f, -2.7f), new Vector2(86f, 4f)),
                    (new Vector2(86f, -3.4f), new Vector2(111f, 4.2f)),
                    (new Vector2(111f, -3.4f), new Vector2(138f, 2f)),
                };

                foreach (var pair in pairs)
                {
                    bootstrap.PlayerOne.transform.position = pair.Item1;
                    bootstrap.PlayerTwo.transform.position = pair.Item2;
                    Assert.That(
                        camera.CalculateDesiredOrthographicSize(),
                        Is.LessThanOrEqualTo(camera.MaximumOrthographicSize),
                        $"Players at {pair.Item1} and {pair.Item2} exceed the shared camera envelope."
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MatchResultsPanel_ExposesAWorkingReplayAction()
        {
            var canvas = new GameObject("Canvas", typeof(Canvas));
            var panelObject = new GameObject("Results", typeof(RectTransform));
            try
            {
                var panel = panelObject.AddComponent<MatchResultsPanel>();
                panel.Initialize(canvas.transform);
                var requested = false;
                panel.ReplayRequested += () => requested = true;

                Assert.That(panel.ReplayButton, Is.Not.Null);
                panel.ReplayButton.onClick.Invoke();
                Assert.That(requested, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void Phase1Redesign_BlockVisualsMatchTheirColliderDimensions()
        {
            BuildPhase1ForInspection(out var holder);
            try
            {
                var geometry = holder.transform.Find("LevelGeometry");
                var colliders = geometry.GetComponentsInChildren<BoxCollider2D>();
                var checkedCount = 0;
                foreach (var collider in colliders)
                {
                    var renderer = collider.GetComponent<SpriteRenderer>();
                    if (renderer == null || renderer.drawMode == SpriteDrawMode.Simple)
                        continue;

                    Assert.That(
                        renderer.size.x,
                        Is.EqualTo(collider.size.x).Within(0.001f),
                        $"{collider.name} visual width differs from collision."
                    );
                    Assert.That(
                        renderer.size.y,
                        Is.EqualTo(collider.size.y).Within(0.001f),
                        $"{collider.name} visual height differs from collision."
                    );
                    checkedCount++;
                }
                Assert.That(checkedCount, Is.GreaterThan(35));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void PlatformingMetrics_AreDerivedFromPhysics_NotFromLiterals()
        {
            var budget = Phase1Bootstrap.DesignBudget();

            // O gravityScale é derivado do ápice declarado, então o ápice previsto
            // pelo orçamento é exatamente o alvo de projeto.
            Assert.That(budget.Apex, Is.EqualTo(Phase1Bootstrap.TargetJumpApex).Within(0.01f));

            // A fórmula contínua v0^2/(2g) superestima o ápice em ~v0*dt/2, porque a
            // Unity integra em passos. O orçamento usa o valor do integrador discreto,
            // que é o que o jogador realmente alcança.
            Assert.That(budget.IdealApex, Is.GreaterThan(budget.Apex));
            Assert.That(
                budget.IdealApex - budget.Apex,
                Is.EqualTo(budget.JumpVelocity * Phase1Bootstrap.FixedDeltaTime * 0.5f)
                    .Within(0.02f)
            );

            // Alcance e folgas são consequência, nunca números escolhidos.
            Assert.That(budget.Reach, Is.EqualTo(budget.MoveSpeed * budget.AirTime).Within(0.001f));
            Assert.That(budget.SafeStep, Is.LessThan(budget.Apex));
            Assert.That(budget.SafeGap, Is.LessThan(budget.Reach));

            // O orçamento acompanha a física: mais gravidade, menos ápice.
            var heavier = JumpBudget.FromPhysics(
                Phase1Bootstrap.PhaseGravityY,
                budget.Gravity / Mathf.Abs(Phase1Bootstrap.PhaseGravityY) * 2f,
                budget.JumpVelocity,
                budget.MoveSpeed,
                Phase1Bootstrap.FixedDeltaTime
            );
            Assert.That(heavier.Apex, Is.LessThan(budget.Apex * 0.55f));
        }

        private static void AssertSurfaceChain(
            Transform routeRoot,
            System.Func<BoxCollider2D, bool> predicate,
            float maxGap,
            float maxRise,
            string routeName
        )
        {
            var surfaces = routeRoot
                .GetComponentsInChildren<BoxCollider2D>()
                .Where(predicate)
                .OrderBy(collider => collider.bounds.center.x)
                .ToArray();
            Assert.That(surfaces.Length, Is.GreaterThanOrEqualTo(5));

            for (var i = 1; i < surfaces.Length; i++)
            {
                var previous = surfaces[i - 1].bounds;
                var current = surfaces[i].bounds;
                var horizontalGap = Mathf.Max(0f, current.min.x - previous.max.x);
                var upwardRise = Mathf.Max(0f, current.max.y - previous.max.y);
                Assert.That(
                    horizontalGap,
                    Is.LessThanOrEqualTo(maxGap),
                    $"{routeName}: gap from {surfaces[i - 1].name} to {surfaces[i].name} is {horizontalGap:0.00}m."
                );
                Assert.That(
                    upwardRise,
                    Is.LessThanOrEqualTo(maxRise),
                    $"{routeName}: rise from {surfaces[i - 1].name} to {surfaces[i].name} is {upwardRise:0.00}m."
                );
            }
        }

        private static Phase1Bootstrap BuildPhase1ForInspection(out GameObject holder)
        {
            holder = new GameObject("Phase1Inspection");
            var bootstrap = holder.AddComponent<Phase1Bootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_playerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            serialized.FindProperty("_collectiblePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(CollectiblePrefabPath);
            serialized.FindProperty("_pushableBoxPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(PushableBoxPrefabPath);
            serialized.FindProperty("_visualCatalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VisualCatalog>(
                    "Assets/_Game/Art/VisualAtlas/SO_VisualCatalog.asset"
                );

            var foodAssets = AssetDatabase
                .FindAssets("t:FoodData", new[] { "Assets/_Game/ScriptableObjects/Food" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<FoodData>)
                .ToArray();
            var foodCatalog = serialized.FindProperty("_foodCatalog");
            foodCatalog.arraySize = foodAssets.Length;
            for (var i = 0; i < foodAssets.Length; i++)
                foodCatalog.GetArrayElementAtIndex(i).objectReferenceValue = foodAssets[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            typeof(Phase1Bootstrap)
                .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(bootstrap, null);
            return bootstrap;
        }
    }
}
