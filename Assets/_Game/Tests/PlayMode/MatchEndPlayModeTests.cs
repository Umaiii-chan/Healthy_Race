using System.Collections;
using HealthyRace.Core;
using HealthyRace.Level;
using HealthyRace.Player;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    /// <summary>
    /// A partida sempre termina, e depois do resultado nada mais mexe nos jogadores.
    /// </summary>
    public sealed class MatchEndPlayModeTests
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
            var empty = SceneManager.CreateScene("MatchEndPlayModeTests_Empty_" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (active.IsValid() && active.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(active);
            }

            Checkpoint.ResetRegistry();
            Physics2D.gravity = _originalGravity;
        }

        private static IEnumerator LoadArena()
        {
            var loadOp = SceneManager.LoadSceneAsync(
                GameFlow.SceneForLevel(1),
                LoadSceneMode.Single
            );
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);
        }

        private static void Teleport(GameObject player, Vector2 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            rb.position = position;
            player.transform.position = position;
        }

        [UnityTest]
        public IEnumerator PartnerWindow_Expires_ConcludesWithDnfForTheMissingPlayer()
        {
            yield return LoadArena();
            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var controller = bootstrap.MatchController;
            var p2Wellbeing = bootstrap.PlayerTwo.GetComponent<WellbeingComponent>();

            Teleport(bootstrap.PlayerOne, bootstrap.PlayerOneDoor.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.WaitingForPartner));
            Assert.That(
                controller.FinalWindowTimeRemaining,
                Is.GreaterThan(0f),
                "A primeira chegada deve abrir um prazo para o parceiro."
            );

            Assert.That(controller.FinalWindowDuration, Is.EqualTo(45f), "Prazo prometido: 45 s.");

            controller.Tick(44f);
            Assert.That(
                controller.State,
                Is.EqualTo(MatchCompletionState.WaitingForPartner),
                "Antes dos 45 s a partida não pode encerrar."
            );

            controller.Tick(2f);
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.Concluded));
            Assert.That(
                controller.FinalResults.TotalDuration,
                Is.GreaterThanOrEqualTo(46f),
                "A duração inclui a espera pelo parceiro."
            );
            Assert.That(controller.FinalResults.Player1.IsDnf, Is.False);
            Assert.That(controller.FinalResults.Player2.IsDnf, Is.True);
            Assert.That(
                p2Wellbeing.CurrentValue,
                Is.EqualTo(WellbeingComponent.MaximumValue - controller.DnfPenalty)
            );
            Assert.That(bootstrap.Screens.IsMatchConcluded, Is.True);
        }

        [UnityTest]
        public IEnumerator TimeLimit_WithNobodyAtTheDoors_ConcludesTheMatch()
        {
            yield return LoadArena();
            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var controller = bootstrap.MatchController;
            Assert.That(controller.TimeLimitSeconds, Is.EqualTo(300f), "Limite prometido: 5 min.");

            controller.Tick(299f);
            Assert.That(
                controller.State,
                Is.EqualTo(MatchCompletionState.Running),
                "Antes dos 5 min a partida não pode encerrar."
            );

            controller.Tick(2f);
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.Concluded));
            Assert.That(
                controller.FinalResults.TotalDuration,
                Is.GreaterThanOrEqualTo(300f),
                "Sem nenhuma chegada a duração é o tempo de partida, não -1."
            );
            Assert.That(controller.FinalResults.Player1.IsDnf, Is.True);
            Assert.That(controller.FinalResults.Player2.IsDnf, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BothArrivedSeparately_WindowExpires_NobodyIsPenalized()
        {
            yield return LoadArena();
            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var controller = bootstrap.MatchController;

            // P1 chega, sai para ajudar; só então P2 chega. Nunca estão juntos.
            Teleport(bootstrap.PlayerOne, bootstrap.PlayerOneDoor.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Teleport(bootstrap.PlayerOne, new Vector2(8f, Phase1Bootstrap.Tier4Top + 0.7f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Teleport(bootstrap.PlayerTwo, bootstrap.PlayerTwoDoor.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.WaitingForPartner));

            controller.Tick(Phase1Bootstrap.PartnerWindowSeconds + 1f);

            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.Concluded));
            Assert.That(
                controller.FinalResults.HasDnf,
                Is.False,
                "Quem já registrou chegada não é punido por ter saído da porta."
            );
        }

        [UnityTest]
        public IEnumerator ResetMatch_RearmsFatigue()
        {
            yield return LoadArena();
            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var wellbeing = bootstrap.PlayerOne.GetComponent<WellbeingComponent>();
            var fatigue = bootstrap.PlayerOne.GetComponent<FatigueComponent>();

            bootstrap.MatchController.ConcludeMatch();
            Assert.That(fatigue.IsHalted, Is.True);

            bootstrap.MatchController.ResetMatch();
            Assert.That(fatigue.IsHalted, Is.False, "Partida nova: a fadiga volta a agir.");

            wellbeing.SetValue(fatigue.TriggerThreshold + 1);
            wellbeing.ApplyDelta(-5);
            Assert.That(
                fatigue.FatigueEpisodeCount,
                Is.EqualTo(1),
                "Cruzar o limiar depois do reinício dispara um episódio."
            );
        }

        [UnityTest]
        public IEnumerator Fatigue_InProgressWhenMatchConcludes_NeverUnfreezesThePlayer()
        {
            yield return LoadArena();
            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var player = bootstrap.PlayerOne;
            var wellbeing = player.GetComponent<WellbeingComponent>();
            var fatigue = player.GetComponent<FatigueComponent>();
            var movement = player.GetComponent<PlayerMovement>();

            wellbeing.SetValue(fatigue.TriggerThreshold + 1);
            wellbeing.ApplyDelta(-5);
            for (var i = 0; i < 30 && fatigue.State != FatigueState.Fatigued; i++)
            {
                yield return null;
            }

            Assert.That(fatigue.State, Is.EqualTo(FatigueState.Fatigued));

            bootstrap.MatchController.ConcludeMatch();
            var wellbeingAtResult = wellbeing.CurrentValue;
            Assert.That(
                bootstrap.MatchController.FinalResults.Player1.FinalWellbeing,
                Is.EqualTo(wellbeingAtResult)
            );

            yield return new WaitForSeconds(fatigue.FatigueDurationSeconds + 0.6f);

            Assert.That(fatigue.IsHalted, Is.True);
            Assert.That(
                movement.IsMovementEnabled,
                Is.False,
                "O fim da fadiga não pode devolver o movimento depois do resultado."
            );
            Assert.That(
                wellbeing.CurrentValue,
                Is.EqualTo(wellbeingAtResult),
                "A recuperação da fadiga não pode alterar o Bem-Estar já pontuado."
            );
        }
    }
}
