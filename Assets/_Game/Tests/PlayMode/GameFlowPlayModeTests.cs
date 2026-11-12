using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HealthyRace.Audio;
using HealthyRace.Core;
using HealthyRace.Level;
using HealthyRace.Player;
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

            // o menu é a estufa jogável: a montagem das fases em modo menu
            var menu = Object.FindFirstObjectByType<LevelBootstrap>();
            Assert.That(menu, Is.Not.Null, "A cena de menu precisa da estufa jogável.");
            Assert.That(menu.IsMenu, Is.True);
            Assert.That(menu.Screens, Is.Not.Null, "O prefab de telas deve estar atribuído.");
            Assert.That(menu.Screens.View.IsHidden, Is.False);
            Assert.That(menu.Screens.View.Screen, Is.EqualTo(VisualScreen.MainMenu));
            Assert.That(menu.GreenhouseHud, Is.Null, "o menu não tem HUD de corrida");
            Assert.That(menu.Foods, Is.Empty);

            var stage = menu.MenuStage;
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.State, Is.EqualTo(MenuStage.Mode.Choosing));
            Assert.That(
                stage.Signs.Select(sign => sign.GetComponentInChildren<Text>().text),
                Is.EqualTo(new[] { "Jogar", "Como jogar", "Áudio", "Sair" })
            );
            var events = Object.FindFirstObjectByType<EventSystem>();
            Assert.That(events, Is.Not.Null, "Sem EventSystem as placas não recebem clique.");
            Assert.That(
                events.currentSelectedGameObject,
                Is.SameAs(stage.Signs[0].gameObject),
                "o teclado começa na placa Jogar"
            );
        }

        [UnityTest]
        public IEnumerator MainMenu_JumpingIntoASign_ChoosesIt()
        {
            yield return Load(GameFlow.MenuScene);
            var menu = Object.FindFirstObjectByType<LevelBootstrap>();
            var stage = menu.MenuStage;
            var player = menu.PlayerOne;
            var movement = player.GetComponent<PlayerMovement>();

            // parado embaixo da placa nada acontece: só o pulo alcança
            var under = stage.SignPosition(1);
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(under.x, body.position.y);
            player.transform.position = new Vector3(under.x, player.transform.position.y, 0f);
            yield return new WaitForSeconds(0.4f);
            Assert.That(menu.Screens.View.Screen, Is.EqualTo(VisualScreen.MainMenu));

            movement.RequestJump();
            yield return new WaitForSeconds(0.6f);
            Assert.That(
                menu.Screens.View.Screen,
                Is.EqualTo(VisualScreen.Controls),
                "pular na placa Como jogar abre a tela de controles"
            );
        }

        [UnityTest]
        public IEnumerator MainMenu_Play_OpensReady_AndBothOnTheirPads_StartTheLevel()
        {
            yield return Load(GameFlow.MenuScene);
            var menu = Object.FindFirstObjectByType<LevelBootstrap>();
            var stage = menu.MenuStage;

            // clica na placa de verdade: uma placa sem ouvinte tem de quebrar este teste
            stage.Signs[0].onClick.Invoke();
            Assert.That(stage.State, Is.EqualTo(MenuStage.Mode.Ready));
            Assert.That(menu.Screens.View.IsHidden, Is.True, "o letreiro sai no Prontos?");
            yield return new WaitForSeconds(0.3f);
            Assert.That(stage.IsReady(1), Is.False);
            Assert.That(stage.IsReady(2), Is.False);

            // só um no vaso não começa a corrida
            PutOnPad(menu.PlayerOne, stage.PadPosition(1));
            yield return new WaitForSeconds(MenuStage.ReadyHoldSeconds + 0.5f);
            Assert.That(stage.IsReady(1), Is.True);
            Assert.That(stage.State, Is.EqualTo(MenuStage.Mode.Ready));

            var level = stage.SelectedLevel;
            PutOnPad(menu.PlayerTwo, stage.PadPosition(2));
            yield return WaitForScene(GameFlow.SceneForLevel(level));

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(GameFlow.SceneForLevel(level))
            );
            var loaded = Object.FindFirstObjectByType<LevelBootstrap>();
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.IsMenu, Is.False);
            Assert.That(GameFlow.CurrentLevel, Is.EqualTo(level));
        }

        private static void PutOnPad(GameObject player, Vector2 pad)
        {
            var body = player.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.position = new Vector2(pad.x, body.position.y);
            player.transform.position = new Vector3(pad.x, player.transform.position.y, 0f);
        }

        [UnityTest]
        public IEnumerator EveryButton_EmitsItsSerializedCommand()
        {
            yield return Load(GameFlow.MenuScene);
            var menu = Object.FindFirstObjectByType<LevelBootstrap>();

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
