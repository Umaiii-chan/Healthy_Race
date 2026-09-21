using System.Collections;
using HealthyRace.Level;
using HealthyRace.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class Phase1PlayModeTests
    {
        private const string SceneName = "Phase1_CorridaDoEquilibrio";

        private Vector2 _originalGravity;

        [SetUp]
        public void SetUp()
        {
            _originalGravity = Physics2D.gravity;
        }

        /// <summary>
        /// A arena ocupa a região da origem; se ficasse carregada, os testes de
        /// física seguintes (que montam cenários em torno de (0, 0)) colidiriam com
        /// ela. Descarrega a cena e restaura a gravidade do projeto.
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var arena = SceneManager.GetSceneByName(SceneName);
            if (arena.IsValid() && arena.isLoaded)
            {
                var empty = SceneManager.CreateScene("Phase1PlayModeTests_Empty");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(arena);
            }

            Checkpoint.ResetRegistry();
            Physics2D.gravity = _originalGravity;
        }

        private static IEnumerator LoadArena()
        {
            var loadOp = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }

        private static void Teleport(GameObject player, Vector2 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            rb.position = position;
            player.transform.position = position;
        }

        [UnityTest]
        public IEnumerator Phase1_LoadsInPlayMode_SpawnsPlayersInsideArenaAndGrounded()
        {
            yield return LoadArena();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            Assert.That(bootstrap, Is.Not.Null, "Phase1Bootstrap must exist.");

            var p1 = bootstrap.PlayerOne;
            var p2 = bootstrap.PlayerTwo;
            Assert.That(p1, Is.Not.Null);
            Assert.That(p2, Is.Not.Null);
            Assert.That(p1.activeInHierarchy, Is.True);
            Assert.That(p2.activeInHierarchy, Is.True);

            foreach (var player in new[] { p1, p2 })
            {
                var pos = player.transform.position;
                Assert.That(
                    pos.x,
                    Is.InRange(Phase1Bootstrap.ArenaMinX, Phase1Bootstrap.ArenaMaxX)
                );
                Assert.That(
                    pos.y,
                    Is.InRange(Phase1Bootstrap.ArenaMinY, Phase1Bootstrap.ArenaMaxY)
                );
            }

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(p1.GetComponent<PlayerMovement>().IsGrounded, Is.True, "P1 spawns on T0.");
            Assert.That(p2.GetComponent<PlayerMovement>().IsGrounded, Is.True, "P2 spawns on T0.");

            Assert.That(bootstrap.PlayerOneHud, Is.Not.Null);
            Assert.That(bootstrap.PlayerTwoHud, Is.Not.Null);
            Assert.That(bootstrap.MatchHud, Is.Not.Null);
            Assert.That(bootstrap.PlayerOneHud.Progress, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Phase1_PlayerReachesCheckpoint_ActivatesIndependently()
        {
            yield return LoadArena();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            Assert.That(bootstrap.Checkpoints.Count, Is.EqualTo(3));

            var cp1 = bootstrap.Checkpoints[0];
            Assert.That(cp1.Index, Is.EqualTo(1));

            Teleport(bootstrap.PlayerOne, cp1.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(
                cp1.IsActivatedByPlayer(1),
                Is.True,
                "Checkpoint 1 should activate for P1."
            );
            Assert.That(cp1.IsActivatedByPlayer(2), Is.False, "P2 must remain isolated.");
            Assert.That(Checkpoint.GetLastCheckpoint(1), Is.SameAs(cp1));
            Assert.That(Checkpoint.GetLastCheckpoint(2), Is.Null);
        }

        [UnityTest]
        public IEnumerator Phase1_ProgressAdvancesByTier_NotByHorizontalPosition()
        {
            yield return LoadArena();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var p1 = bootstrap.PlayerOne;

            // Correr até o fim do piso não gera progresso: o marco é o patamar.
            Teleport(p1, new Vector2(26f, Phase1Bootstrap.Tier0Top + 0.7f));
            for (var i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            yield return null;
            Assert.That(bootstrap.PlayerOneHud.Progress, Is.EqualTo(0f).Within(0.001f));

            // Sobre a plataforma T2 esquerda: marco 2 -> 40%
            Teleport(p1, new Vector2(5f, Phase1Bootstrap.Tier2Top + 0.7f));
            for (var i = 0; i < 6; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            yield return null;
            Assert.That(bootstrap.GetMilestone(1), Is.EqualTo(2));
            Assert.That(bootstrap.PlayerOneHud.Progress, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(bootstrap.PlayerTwoHud.Progress, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Phase1_MatchConcludesOnlyWhenBothPlayersOccupyTheirOwnDoors()
        {
            yield return LoadArena();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var doorOne = bootstrap.PlayerOneDoor;
            var doorTwo = bootstrap.PlayerTwoDoor;
            var controller = bootstrap.MatchController;
            Assert.That(controller.RequireBothAtDoors, Is.True);

            // P2 na porta do P1: nada acontece.
            Teleport(bootstrap.PlayerTwo, doorOne.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(doorOne.HasPlayerFinished(2), Is.False, "P1 door rejects P2.");
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.Running));

            // P1 na própria porta: chegada registrada, partida aguarda o parceiro.
            Teleport(bootstrap.PlayerOne, doorOne.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(doorOne.HasPlayerFinished(1), Is.True);
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.WaitingForPartner));
            Assert.That(
                bootstrap.PlayerOne.GetComponent<PlayerMovement>().IsMovementEnabled,
                Is.True,
                "First arrival must stay free to help the partner."
            );

            // P2 na própria porta com P1 ainda na dele: partida encerra.
            Teleport(bootstrap.PlayerTwo, doorTwo.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(doorTwo.HasPlayerFinished(2), Is.True);
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.Concluded));
            Assert.That(controller.FinalResults.HasDnf, Is.False);
            Assert.That(bootstrap.GetMilestone(1), Is.EqualTo(Phase1Bootstrap.ProgressMilestones));
        }

        [UnityTest]
        public IEnumerator Phase1_CameraStaysFixed_WhilePlayersMoveAcrossTheArena()
        {
            yield return LoadArena();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var cam = bootstrap.CameraController.CameraComponent;
            var initialPosition = cam.transform.position;
            var initialSize = cam.orthographicSize;

            Assert.That(initialPosition, Is.EqualTo(Phase1Bootstrap.CameraPosition));
            Assert.That(
                initialSize,
                Is.GreaterThanOrEqualTo(Phase1Bootstrap.CameraOrthographicSize)
            );

            var positions = new[]
            {
                new Vector2(1f, Phase1Bootstrap.Tier0Top + 0.7f),
                new Vector2(30f, Phase1Bootstrap.Tier4Top + 0.7f),
                new Vector2(16f, Phase1Bootstrap.Tier2Top + 0.7f),
            };

            foreach (var target in positions)
            {
                Teleport(bootstrap.PlayerOne, target);
                Teleport(bootstrap.PlayerTwo, new Vector2(32f - target.x, target.y));
                for (var frame = 0; frame < 5; frame++)
                {
                    yield return null;
                    Assert.That(cam.transform.position, Is.EqualTo(initialPosition));
                    Assert.That(cam.orthographicSize, Is.EqualTo(initialSize).Within(0.0001f));
                }
            }

            // Os dois jogadores continuam visíveis em qualquer canto da arena.
            foreach (var player in new[] { bootstrap.PlayerOne, bootstrap.PlayerTwo })
            {
                var viewport = cam.WorldToViewportPoint(player.transform.position);
                Assert.That(viewport.x, Is.InRange(0f, 1f));
                Assert.That(viewport.y, Is.InRange(0f, 1f));
            }
        }
    }
}
