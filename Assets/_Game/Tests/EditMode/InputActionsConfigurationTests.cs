using System.Collections.Generic;
using System.Linq;
using HealthyRace.Input;
using HealthyRace.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HealthyRace.Tests.EditMode
{
    public sealed class InputActionsConfigurationTests
    {
        private const string InputActionsPath =
            "Assets/_Game/Input/HealthyRaceInputActions.inputactions";
        private const string InputTestScenePath = "Assets/_Game/Scenes/InputTest.unity";

        private InputActionAsset _inputActions;

        [SetUp]
        public void SetUp()
        {
            _inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.That(_inputActions, Is.Not.Null, "The Input Actions asset must be importable.");
        }

        [Test]
        public void PlayerOneMap_ContainsExpectedActionsAndBindings()
        {
            AssertPlayerMap(
                "Player1",
                new[] { "<Keyboard>/a", "<Keyboard>/d" },
                new[] { "<Keyboard>/w", "<Keyboard>/space" },
                "<Keyboard>/e"
            );
        }

        [Test]
        public void PlayerTwoMap_ContainsExpectedActionsAndBindings()
        {
            AssertPlayerMap(
                "Player2",
                new[] { "<Keyboard>/leftArrow", "<Keyboard>/rightArrow" },
                new[] { "<Keyboard>/upArrow" },
                "<Keyboard>/rightCtrl"
            );
        }

        [Test]
        public void PlayerBindings_AreIsolatedAndPauseUsesEscape()
        {
            var playerOneBindings = GetKeyboardPaths(_inputActions.FindActionMap("Player1", true));
            var playerTwoBindings = GetKeyboardPaths(_inputActions.FindActionMap("Player2", true));
            var pause = _inputActions.FindActionMap("UI", true).FindAction("Pause", true);

            Assert.That(playerOneBindings.Intersect(playerTwoBindings), Is.Empty);
            Assert.That(GetKeyboardPaths(pause), Is.EquivalentTo(new[] { "<Keyboard>/escape" }));
        }

        [Test]
        public void InputTestScene_HasConfiguredReadersAndNoMissingScripts()
        {
            var scene = EditorSceneManager.OpenPreviewScene(InputTestScenePath);

            try
            {
                var rootObjects = scene.GetRootGameObjects();
                var playerOne = rootObjects.Single(gameObject => gameObject.name == "Player1Debug");
                var playerTwo = rootObjects.Single(gameObject => gameObject.name == "Player2Debug");

                Assert.That(playerOne.GetComponent<PlayerInputDebug>(), Is.Not.Null);
                Assert.That(playerTwo.GetComponent<PlayerInputDebug>(), Is.Not.Null);
                Assert.That(
                    playerOne.GetComponent<PlayerInputReader>().PlayerInputId,
                    Is.EqualTo(PlayerInputId.Player1)
                );
                Assert.That(
                    playerTwo.GetComponent<PlayerInputReader>().PlayerInputId,
                    Is.EqualTo(PlayerInputId.Player2)
                );

                var components = rootObjects.SelectMany(gameObject =>
                    gameObject.GetComponentsInChildren<MonoBehaviour>(true)
                );
                Assert.That(
                    components,
                    Has.None.Null,
                    "InputTest must not contain missing scripts."
                );
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void AssertPlayerMap(
            string mapName,
            IReadOnlyCollection<string> moveBindings,
            IReadOnlyCollection<string> jumpBindings,
            string interactBinding
        )
        {
            var map = AssetDatabase
                .LoadAssetAtPath<InputActionAsset>(InputActionsPath)
                .FindActionMap(mapName, true);
            var move = map.FindAction("Move", true);
            var jump = map.FindAction("Jump", true);
            var interact = map.FindAction("Interact", true);

            Assert.That(move.type, Is.EqualTo(InputActionType.Value));
            Assert.That(move.expectedControlType, Is.EqualTo("Vector2"));
            Assert.That(
                move.bindings.Any(binding => binding.isComposite && binding.path == "2DVector"),
                Is.True
            );
            Assert.That(GetKeyboardPaths(move), Is.EquivalentTo(moveBindings));
            Assert.That(GetKeyboardPaths(jump), Is.EquivalentTo(jumpBindings));
            Assert.That(GetKeyboardPaths(interact), Is.EquivalentTo(new[] { interactBinding }));
        }

        private static IEnumerable<string> GetKeyboardPaths(InputAction action)
        {
            return action
                .bindings.Where(binding => binding.path.StartsWith("<Keyboard>"))
                .Select(binding => binding.path);
        }

        private static IEnumerable<string> GetKeyboardPaths(InputActionMap actionMap)
        {
            return actionMap.actions.SelectMany(GetKeyboardPaths);
        }
    }
}
