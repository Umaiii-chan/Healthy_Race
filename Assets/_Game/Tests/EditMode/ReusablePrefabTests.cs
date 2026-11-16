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
    public sealed class ReusablePrefabTests
    {
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player.prefab";
        private const string CollectiblePrefabPath =
            "Assets/_Game/Prefabs/Collectibles/PF_FoodCollectible.prefab";
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
        }
    }
}
