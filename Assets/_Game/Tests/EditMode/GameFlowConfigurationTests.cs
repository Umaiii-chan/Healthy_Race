using System;
using System.Linq;
using HealthyRace.Core;
using HealthyRace.Level;
using HealthyRace.UI;
using HealthyRace.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    /// <summary>
    /// Garante que o que o fluxo do jogo precisa está serializado: cenas na build,
    /// prefab de telas com todos os comandos e referências de fluxo na fase.
    /// </summary>
    public sealed class GameFlowConfigurationTests
    {
        private const string ScreensPrefabPath =
            "Assets/_Game/Prefabs/Visual/PF_VIS_Screens.prefab";
        private const string MenuScenePath = "Assets/_Game/Scenes/MainMenu.unity";

        private static string[] BuildSceneNames()
        {
            return EditorBuildSettings
                .scenes.Where(scene => scene.enabled)
                .Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path))
                .ToArray();
        }

        [Test]
        public void BuildSettings_StartOnMainMenu()
        {
            var scenes = BuildSceneNames();
            Assert.That(scenes, Is.Not.Empty);
            Assert.That(
                scenes[0],
                Is.EqualTo(GameFlow.MenuScene),
                "O executável deve abrir no menu principal."
            );
        }

        [Test]
        public void BuildSettings_ContainEveryLevelScene()
        {
            var scenes = BuildSceneNames();
            for (var level = 1; level <= GameFlow.LevelCount; level++)
            {
                Assert.That(
                    scenes,
                    Does.Contain(GameFlow.SceneForLevel(level)),
                    $"A cena da fase {level} não está na Build Settings."
                );
            }
        }

        [Test]
        public void ScreensPrefab_HasAButtonForEveryCommand()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreensPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<RaceScreenView>();
            Assert.That(view, Is.Not.Null);

            var commands = new SerializedObject(view).FindProperty("_commands");
            var present = Enumerable
                .Range(0, commands.arraySize)
                .Select(i => (VisualCommand)commands.GetArrayElementAtIndex(i).enumValueIndex)
                .ToArray();

            foreach (VisualCommand command in Enum.GetValues(typeof(VisualCommand)))
            {
                Assert.That(present, Does.Contain(command), $"Nenhum botão emite {command}.");
            }
        }

        [Test]
        public void MainMenuScene_ReferencesScreensPrefab()
        {
            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);

            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuBootstrap>();
            Assert.That(menu, Is.Not.Null);
            var prefab = new SerializedObject(menu).FindProperty("_screensPrefab");
            Assert.That(prefab.objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void LevelScene_ReferencesScreensPrefabAndInputActions()
        {
            var path = EditorBuildSettings
                .scenes.First(scene =>
                    System.IO.Path.GetFileNameWithoutExtension(scene.path)
                    == GameFlow.SceneForLevel(1)
                )
                .path;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var bootstrap = UnityEngine.Object.FindFirstObjectByType<Phase1Bootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            var serialized = new SerializedObject(bootstrap);
            Assert.That(
                serialized.FindProperty("_screensPrefab").objectReferenceValue,
                Is.Not.Null,
                "Sem o prefab de telas a fase fica sem pausa e sem resultados."
            );
            Assert.That(
                serialized.FindProperty("_inputActions").objectReferenceValue,
                Is.Not.Null,
                "Sem Input Actions não há ação UI/Pause."
            );
        }

        [Test]
        public void SetPaused_TogglesTimeScale()
        {
            try
            {
                GameFlow.SetPaused(true);
                Assert.That(GameFlow.IsPaused, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(0f));

                GameFlow.SetPaused(false);
                Assert.That(GameFlow.IsPaused, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                GameFlow.SetPaused(false);
            }
        }
    }
}
