using System.Linq;
using HealthyRace.Input;
using HealthyRace.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class MovementSceneConfigurationTests
    {
        private const int GroundLayer = 6;
        private const string MovementTestScenePath = "Assets/_Game/Scenes/MovementTest.unity";

        [Test]
        public void MovementTest_HasTwoConfiguredPlayersAndNoMissingScripts()
        {
            var scene = EditorSceneManager.OpenPreviewScene(MovementTestScenePath);

            try
            {
                var rootObjects = scene.GetRootGameObjects();
                var playerOne = rootObjects.Single(gameObject => gameObject.name == "Player1");
                var playerTwo = rootObjects.Single(gameObject => gameObject.name == "Player2");

                AssertPlayerConfiguration(playerOne, 1, PlayerInputId.Player1);
                AssertPlayerConfiguration(playerTwo, 2, PlayerInputId.Player2);
                Assert.That(
                    rootObjects.Single(gameObject => gameObject.name == "Ground").layer,
                    Is.EqualTo(GroundLayer)
                );
                Assert.That(
                    rootObjects.Where(gameObject => gameObject.name.StartsWith("Platform")).Count(),
                    Is.EqualTo(3)
                );

                var components = rootObjects.SelectMany(gameObject =>
                    gameObject.GetComponentsInChildren<MonoBehaviour>(true)
                );
                Assert.That(
                    components,
                    Has.None.Null,
                    "MovementTest must not contain missing scripts."
                );
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void AssertPlayerConfiguration(
            GameObject player,
            int playerIndex,
            PlayerInputId inputId
        )
        {
            Assert.That(
                player.GetComponent<PlayerInputReader>().PlayerInputId,
                Is.EqualTo(inputId)
            );
            Assert.That(
                player.GetComponent<PlayerController>().PlayerIndex,
                Is.EqualTo(playerIndex)
            );
            Assert.That(player.GetComponent<PlayerGroundCheck>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerMovement>(), Is.Not.Null);
            Assert.That(player.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(player.GetComponent<Collider2D>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerMovementDebug>(), Is.Not.Null);
        }
    }
}
