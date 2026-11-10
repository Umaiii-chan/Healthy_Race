using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HealthyRace.Audio;
using HealthyRace.Core;
using HealthyRace.Level;
using HealthyRace.UI;
using HealthyRace.Visuals;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HealthyRace.Tests.PlayMode
{
    /// <summary>
    /// Fluxo de ponta a ponta: menu -> fase -> pausa -> resultados -> reinício.
    /// </summary>
    public sealed class GameFlowPlayModeTests
    {
        private Vector2 _originalGravity;

        [SetUp]
        public void SetUp()
        {
            _originalGravity = Physics2D.gravity;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameFlow.SetPaused(false);

            var active = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("GameFlowPlayModeTests_Empty_" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (active.IsValid() && active.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(active);
            }

            Checkpoint.ResetRegistry();
            Physics2D.gravity = _originalGravity;
        }

        private static IEnumerator Load(string sceneName)
        {
            var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            for (var i = 0; i < 120 && SceneManager.GetActiveScene().name != sceneName; i++)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);
        }

        [UnityTest]
        public IEnumerator MainMenu_Loads_ShowsMenuWithClickableUi()
        {
            yield return Load(GameFlow.MenuScene);

            var menu = Object.FindFirstObjectByType<MainMenuBootstrap>();
            Assert.That(menu, Is.Not.Null, "A cena de menu precisa do MainMenuBootstrap.");
            Assert.That(menu.Screens, Is.Not.Null, "O prefab de telas deve estar atribuído.");
            Assert.That(menu.Screens.View.IsHidden, Is.False);
            Assert.That(menu.Screens.View.Screen, Is.EqualTo(VisualScreen.MainMenu));
            Assert.That(
                Object.FindFirstObjectByType<EventSystem>(),
                Is.Not.Null,
                "Sem EventSystem os botões não recebem clique."
            );
        }

        [UnityTest]
        public IEnumerator MainMenu_Play_LoadsFirstLevel()
        {
            yield return Load(GameFlow.MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuBootstrap>();

            // clica no botão de verdade: um botão sem ouvinte tem de quebrar este teste
            var play = menu
                .Screens.View.ScreenRoot(VisualScreen.MainMenu)
                .GetComponentsInChildren<Button>()
                .Single(button => button.name == "JOGAR");
            play.onClick.Invoke();
            yield return WaitForScene(GameFlow.SceneForLevel(1));

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(GameFlow.SceneForLevel(1)));
            Assert.That(Object.FindFirstObjectByType<LevelBootstrap>(), Is.Not.Null);
            Assert.That(GameFlow.CurrentLevel, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EveryButton_EmitsItsSerializedCommand()
        {
            yield return Load(GameFlow.MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuBootstrap>();

            // Cópia das telas: tem os próprios ouvintes, mas ninguém do fluxo a
            // escuta, então clicar em "Sair" ou "Jogar" aqui não troca de cena.
            var copy = Object.Instantiate(menu.Screens.View.gameObject);
            var view = copy.GetComponent<RaceScreenView>();
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var buttons = (Button[])
                typeof(RaceScreenView).GetField("_buttons", flags).GetValue(view);
            var commands = (VisualCommand[])
                typeof(RaceScreenView).GetField("_commands", flags).GetValue(view);
            Assert.That(buttons, Is.Not.Empty);
            Assert.That(commands.Length, Is.EqualTo(buttons.Length));

            VisualCommand? received = null;
            view.CommandRequested += command => received = command;
            for (var i = 0; i < buttons.Length; i++)
            {
                Assert.That(buttons[i], Is.Not.Null, $"Botão {i} sem referência.");
                received = null;
                buttons[i].onClick.Invoke();
                Assert.That(
                    received,
                    Is.EqualTo(commands[i]),
                    $"O botão '{buttons[i].name}' deveria emitir {commands[i]}."
                );
            }

            Object.Destroy(copy);
        }

        [UnityTest]
        public IEnumerator SceneChange_KeepsTheAudioServiceAndSwitchesMusic()
        {
            yield return Load(GameFlow.MenuScene);
            var service = AudioService.Instance;
            Assert.That(
                service.CurrentMusicClip,
                Is.SameAs(service.GetClip(AudioClipId.Music_Menu))
            );

            Assert.That(GameFlow.LoadLevel(1), Is.True);
            yield return WaitForScene(GameFlow.SceneForLevel(1));

            Assert.That(
                AudioService.Instance,
                Is.SameAs(service),
                "O serviço de áudio não pode ser destruído com a cena."
            );
            Assert.That(
                service.CurrentMusicClip,
                Is.SameAs(service.GetClip(AudioClipId.Music_Level01))
            );
        }

        [UnityTest]
        public IEnumerator LoadingAMissingScene_ChangesNothing()
        {
            yield return Load(GameFlow.SceneForLevel(1));
            GameFlow.LoadLevel(1);
            yield return WaitForScene(GameFlow.SceneForLevel(1));
            var screens = Object.FindFirstObjectByType<LevelBootstrap>().Screens;
            screens.Pause();
            var level = GameFlow.CurrentLevel;

            LogAssert.Expect(LogType.Error, new Regex("não está na build"));
            Assert.That(GameFlow.TryLoad("Cena_Que_Nao_Existe"), Is.False);
            yield return null;

            Assert.That(GameFlow.CurrentLevel, Is.EqualTo(level));
            Assert.That(GameFlow.IsPaused, Is.True, "Carga recusada não mexe na pausa.");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(GameFlow.SceneForLevel(1)));
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTime_AndContinueRestoresIt()
        {
            yield return Load(GameFlow.SceneForLevel(1));
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var screens = bootstrap.Screens;
            Assert.That(screens, Is.Not.Null, "A fase deve ter as telas de pausa/resultados.");
            Assert.That(
                screens.View.IsHidden,
                Is.True,
                "Durante a partida nenhuma tela cobre o jogo."
            );

            screens.Pause();
            yield return null;

            Assert.That(GameFlow.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(screens.View.Screen, Is.EqualTo(VisualScreen.Pause));
            Assert.That(screens.View.IsHidden, Is.False);

            screens.View.Request(VisualCommand.Continue);
            yield return null;

            Assert.That(GameFlow.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(screens.View.IsHidden, Is.True);
        }

        [UnityTest]
        public IEnumerator Pause_ControlsScreen_ReturnsToPause()
        {
            yield return Load(GameFlow.SceneForLevel(1));
            var screens = Object.FindFirstObjectByType<LevelBootstrap>().Screens;

            screens.Pause();
            screens.View.Request(VisualCommand.Controls);
            Assert.That(screens.View.Screen, Is.EqualTo(VisualScreen.Controls));

            screens.View.Request(VisualCommand.Back);
            Assert.That(screens.View.Screen, Is.EqualTo(VisualScreen.Pause));
            Assert.That(GameFlow.IsPaused, Is.True, "Abrir Controles não pode despausar.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MatchConcluded_ShowsResults_AndIgnoresPause()
        {
            yield return Load(GameFlow.SceneForLevel(1));
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();

            bootstrap.MatchController.ConcludeMatch();
            yield return null;

            var screens = bootstrap.Screens;
            Assert.That(screens.IsMatchConcluded, Is.True);
            Assert.That(screens.View.Screen, Is.EqualTo(VisualScreen.Results));
            Assert.That(screens.View.IsHidden, Is.False);

            screens.Pause();
            Assert.That(GameFlow.IsPaused, Is.False, "Depois do resultado não há o que pausar.");
        }

        [UnityTest]
        public IEnumerator Restart_FromResults_RestoresInitialState()
        {
            yield return Load(GameFlow.SceneForLevel(1));
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var wellbeing = bootstrap.PlayerOne.GetComponent<WellbeingComponent>();
            wellbeing.ApplyDelta(-30);
            Assert.That(wellbeing.CurrentValue, Is.EqualTo(70));
            bootstrap.MatchController.ConcludeMatch();
            yield return null;
            var firstInstance = bootstrap.GetInstanceID();

            bootstrap.Screens.View.Request(VisualCommand.Restart);
            yield return null;
            yield return WaitForScene(GameFlow.SceneForLevel(1));

            var reloaded = Object.FindFirstObjectByType<LevelBootstrap>();
            Assert.That(reloaded, Is.Not.Null);
            Assert.That(
                reloaded.GetInstanceID(),
                Is.Not.EqualTo(firstInstance),
                "Reiniciar deve recarregar a cena, não reaproveitar a partida anterior."
            );
            Assert.That(
                reloaded.PlayerOne.GetComponent<WellbeingComponent>().CurrentValue,
                Is.EqualTo(WellbeingComponent.MaximumValue)
            );
            Assert.That(reloaded.MatchController.State, Is.EqualTo(MatchCompletionState.Running));
            Assert.That(reloaded.Screens.View.IsHidden, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Menu_FromPause_ReturnsToMainMenuUnpaused()
        {
            yield return Load(GameFlow.SceneForLevel(1));
            var screens = Object.FindFirstObjectByType<LevelBootstrap>().Screens;
            screens.Pause();

            screens.View.Request(VisualCommand.Menu);
            yield return null;
            yield return WaitForScene(GameFlow.MenuScene);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(GameFlow.MenuScene));
            Assert.That(Time.timeScale, Is.EqualTo(1f), "O menu não pode nascer congelado.");
        }
    }
}
