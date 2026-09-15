using System.Linq;
using HealthyRace.Collectibles;
using HealthyRace.Core;
using HealthyRace.Obstacles;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class VerticalSliceSceneTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/VerticalSlice_Core.unity";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player.prefab";
        private const string CollectiblePrefabPath =
            "Assets/_Game/Prefabs/Collectibles/PF_FoodCollectible.prefab";
        private const string PushableBoxPrefabPath =
            "Assets/_Game/Prefabs/Obstacles/PF_PushableBox.prefab";
        private const string AppleDataPath =
            "Assets/_Game/ScriptableObjects/Food/SO_Food_Apple.asset";
        private const string BurgerDataPath =
            "Assets/_Game/ScriptableObjects/Food/SO_Food_Burger.asset";

        [Test]
        public void ReusablePrefabs_LoadSuccessfullyAndAreValid()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(
                playerPrefab,
                Is.Not.Null,
                "PF_Player.prefab should load as a valid GameObject."
            );

            var collectiblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CollectiblePrefabPath
            );
            Assert.That(
                collectiblePrefab,
                Is.Not.Null,
                "PF_FoodCollectible.prefab should load as a valid GameObject."
            );
            Assert.That(
                collectiblePrefab.GetComponent<FoodCollectible>(),
                Is.Not.Null,
                "PF_FoodCollectible should contain a FoodCollectible component."
            );

            var pushableBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PushableBoxPrefabPath
            );
            Assert.That(
                pushableBoxPrefab,
                Is.Not.Null,
                "PF_PushableBox.prefab should load as a valid GameObject."
            );
            Assert.That(
                pushableBoxPrefab.GetComponent<PushableBox>(),
                Is.Not.Null,
                "PF_PushableBox should contain a PushableBox component."
            );
        }

        [Test]
        public void VerticalSlice_SceneOpens_HasBootstrapWithValidReferencesAndNoMissingScripts()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var rootObjects = scene.GetRootGameObjects();
                var bootstrapObject = rootObjects.FirstOrDefault(go =>
                    go.name == "VerticalSlice_Core"
                );
                Assert.That(
                    bootstrapObject,
                    Is.Not.Null,
                    "VerticalSlice_Core GameObject must exist."
                );

                var bootstrap = bootstrapObject.GetComponent<VerticalSliceBootstrap>();
                Assert.That(bootstrap, Is.Not.Null, "VerticalSliceBootstrap component must exist.");

                var serializedObject = new SerializedObject(bootstrap);
                var collectibleProp = serializedObject.FindProperty("_collectiblePrefab");
                var playerProp = serializedObject.FindProperty("_playerPrefab");
                var boxProp = serializedObject.FindProperty("_pushableBoxPrefab");
                var appleProp = serializedObject.FindProperty("_appleData");
                var burgerProp = serializedObject.FindProperty("_burgerData");

                Assert.That(
                    collectibleProp.objectReferenceValue,
                    Is.Not.Null,
                    "_collectiblePrefab must not be null in VerticalSliceBootstrap."
                );
                Assert.That(
                    playerProp.objectReferenceValue,
                    Is.Not.Null,
                    "_playerPrefab must not be null in VerticalSliceBootstrap."
                );
                Assert.That(
                    boxProp.objectReferenceValue,
                    Is.Not.Null,
                    "_pushableBoxPrefab must not be null in VerticalSliceBootstrap."
                );
                Assert.That(
                    appleProp.objectReferenceValue,
                    Is.Not.Null,
                    "_appleData must not be null in VerticalSliceBootstrap."
                );
                Assert.That(
                    burgerProp.objectReferenceValue,
                    Is.Not.Null,
                    "_burgerData must not be null in VerticalSliceBootstrap."
                );

                var components = rootObjects.SelectMany(go =>
                    go.GetComponentsInChildren<MonoBehaviour>(true)
                );
                Assert.That(
                    components,
                    Has.None.Null,
                    "VerticalSlice_Core must not contain missing scripts."
                );
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
