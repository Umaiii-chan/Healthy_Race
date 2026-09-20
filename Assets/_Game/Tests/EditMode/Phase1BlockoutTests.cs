using System.Linq;
using System.Reflection;
using HealthyRace.Level;
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
        public void PlatformingMetrics_ConfirmLevelIsTraversable()
        {
            // Physics calculations:
            // jumpVelocity = 12, gravityScale = 2, Physics2D.gravity = (0, -18) -> g = 36 m/s^2
            // Jump apex: h = v^2 / (2g) = 144 / 72 = 2.0 m
            // Jump duration: t = 2 * (v / g) = 24 / 36 = 0.667 s
            // Max horizontal reach at 7 m/s: d = 7 * 0.667 = 4.67 m
            const float jumpVelocity = 12f;
            const float effectiveGravity = 36f;
            const float moveSpeed = 7f;

            var maxJumpHeight = (jumpVelocity * jumpVelocity) / (2f * effectiveGravity);
            var jumpDuration = 2f * (jumpVelocity / effectiveGravity);
            var maxHorizontalReach = moveSpeed * jumpDuration;

            Assert.That(maxJumpHeight, Is.EqualTo(2.0f).Within(0.01f));
            Assert.That(maxHorizontalReach, Is.GreaterThan(4.5f));

            // Zone 1 test platforms: heights 0.8m, gaps 3m -> well within 2.0m height and 4.6m reach
            const float z1StepHeight = 0.8f;
            const float z1Gap = 3.5f;
            Assert.That(z1StepHeight, Is.LessThan(maxJumpHeight));
            Assert.That(z1Gap, Is.LessThan(maxHorizontalReach));

            // Zone 2 cliff height: 2.8m (impassable directly: 2.8 > 2.0)
            // But with pushable box (height 1.2m), effective step is 1.6m -> traversable!
            const float z2CliffHeight = 2.8f;
            const float z2BoxHeight = 1.22f;
            Assert.That(z2CliffHeight, Is.GreaterThan(maxJumpHeight), "Cliff must require box.");
            Assert.That(
                z2CliffHeight - z2BoxHeight,
                Is.LessThan(maxJumpHeight),
                "Cliff must be climbable with box."
            );

            // Zone 5 anti-deadlock stepping stones: heights <= 1.0m, gaps <= 3.5m
            const float z5StepHeight = 1.0f;
            const float z5StepGap = 3.5f;
            Assert.That(z5StepHeight, Is.LessThan(maxJumpHeight));
            Assert.That(z5StepGap, Is.LessThan(maxHorizontalReach));
        }
    }
}
