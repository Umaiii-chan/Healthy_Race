using System.Collections;
using HealthyRace.Collectibles;
using HealthyRace.Core;
using HealthyRace.Obstacles;
using HealthyRace.Player;
using HealthyRace.UI;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class VerticalSlicePlayModeTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/VerticalSlice_Core.unity";

        [UnityTest]
        public IEnumerator VerticalSlice_LoadsAndBootstrapsCoreElementsSuccessfully()
        {
            var loadOp = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            Assert.That(
                loadOp,
                Is.Not.Null,
                "LoadSceneAsync should return a valid AsyncOperation."
            );

            while (!loadOp.isDone)
            {
                yield return null;
            }

            // Allow initial frames for Awake/Start and physics setup
            for (var i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            // 1. Verify Players
            var playerOne = GameObject.Find("P1");
            Assert.That(playerOne, Is.Not.Null, "Player P1 should be instantiated.");
            Assert.That(playerOne.GetComponent<PlayerController>(), Is.Not.Null);
            Assert.That(playerOne.GetComponent<PlayerMovement>(), Is.Not.Null);
            Assert.That(playerOne.GetComponent<WellbeingComponent>(), Is.Not.Null);
            Assert.That(playerOne.GetComponent<FatigueComponent>(), Is.Not.Null);

            var playerTwo = GameObject.Find("P2");
            Assert.That(playerTwo, Is.Not.Null, "Player P2 should be instantiated.");
            Assert.That(playerTwo.GetComponent<PlayerController>(), Is.Not.Null);
            Assert.That(playerTwo.GetComponent<PlayerMovement>(), Is.Not.Null);
            Assert.That(playerTwo.GetComponent<WellbeingComponent>(), Is.Not.Null);
            Assert.That(playerTwo.GetComponent<FatigueComponent>(), Is.Not.Null);

            // 2. Verify Pushable Box
            var box = GameObject.Find("Pushable Box");
            Assert.That(box, Is.Not.Null, "Pushable Box should be instantiated.");
            Assert.That(box.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(box.GetComponent<BoxCollider2D>(), Is.Not.Null);

            // 3. Verify Food Collectibles
            var apple = GameObject.Find("Apple (+10)");
            Assert.That(apple, Is.Not.Null, "Apple collectible should be instantiated.");
            Assert.That(apple.GetComponent<FoodCollectible>(), Is.Not.Null);

            var burger = GameObject.Find("Burger (-18)");
            Assert.That(burger, Is.Not.Null, "Burger collectible should be instantiated.");
            Assert.That(burger.GetComponent<FoodCollectible>(), Is.Not.Null);

            // 4. Verify Camera and HUD
            var camera = GameObject.Find("Shared Camera");
            Assert.That(camera, Is.Not.Null, "Shared Camera should be instantiated.");
            Assert.That(camera.GetComponent<SharedCameraController>(), Is.Not.Null);

            var hud = GameObject.Find("HUD");
            Assert.That(hud, Is.Not.Null, "HUD should be instantiated.");
            var huds = Object.FindObjectsByType<PlayerHUD>(FindObjectsSortMode.None);
            Assert.That(huds.Length, Is.EqualTo(2), "Both P1 and P2 HUDs should be created.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var emptyScene = SceneManager.CreateScene(
                "EmptyTestScene_" + System.Guid.NewGuid().ToString("N")
            );
            SceneManager.SetActiveScene(emptyScene);
            var unloadOp = SceneManager.UnloadSceneAsync(ScenePath);
            if (unloadOp != null)
            {
                while (!unloadOp.isDone)
                {
                    yield return null;
                }
            }
        }
    }
}
