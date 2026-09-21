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
        /// O labirinto ocupa a região da origem; se ficasse carregado, os testes de
        /// física seguintes (que montam cenários em torno de (0, 0)) colidiriam com
        /// ele. Descarrega a cena e restaura a gravidade do projeto.
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

        private static IEnumerator LoadMaze()
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

        private static IEnumerator Settle(int fixedFrames = 6)
        {
            for (var i = 0; i < fixedFrames; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Phase1_LoadsInPlayMode_SpawnsPlayersOnSeparateStarts()
        {
            yield return LoadMaze();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            Assert.That(bootstrap, Is.Not.Null, "Phase1Bootstrap must exist.");

            var p1 = bootstrap.PlayerOne;
            var p2 = bootstrap.PlayerTwo;
            Assert.That(p1, Is.Not.Null);
            Assert.That(p2, Is.Not.Null);

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

            // Largadas separadas, espelhadas, uma em cada metade.
            Assert.That(p1.transform.position.x, Is.LessThan(Phase1Bootstrap.ExitX));
            Assert.That(p2.transform.position.x, Is.GreaterThan(Phase1Bootstrap.ExitX));
            Assert.That(
                p2.transform.position.x,
                Is.EqualTo(Phase1Bootstrap.MirrorX(p1.transform.position.x)).Within(0.01f)
            );

            yield return Settle();

            Assert.That(
                p1.GetComponent<PlayerMovement>().IsGrounded,
                Is.True,
                "P1 spawns on corridor A."
            );
            Assert.That(
                p2.GetComponent<PlayerMovement>().IsGrounded,
                Is.True,
                "P2 spawns on corridor A."
            );

            // Cada jogador já nasce com o próprio ponto de retorno.
            Assert.That(
                Checkpoint.GetLastCheckpoint(1),
                Is.SameAs(bootstrap.PlayerOneSpawnCheckpoint)
            );
            Assert.That(
                Checkpoint.GetLastCheckpoint(2),
                Is.SameAs(bootstrap.PlayerTwoSpawnCheckpoint)
            );

            Assert.That(bootstrap.PlayerOneHud, Is.Not.Null);
            Assert.That(bootstrap.PlayerTwoHud, Is.Not.Null);
            Assert.That(bootstrap.MatchHud, Is.Not.Null);
            Assert.That(bootstrap.PlayerOneHud.Progress, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Phase1_PlayerReachesCheckpoint_ActivatesIndependently()
        {
            yield return LoadMaze();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            Assert.That(bootstrap.Checkpoints.Count, Is.EqualTo(8));

            // Segundo checkpoint da metade esquerda: topo da escada, corredor B.
            var cp = bootstrap.Checkpoints[1];
            Assert.That(cp.Index, Is.EqualTo(2));

            Teleport(bootstrap.PlayerOne, cp.transform.position);
            yield return Settle(2);

            Assert.That(cp.IsActivatedByPlayer(1), Is.True, "Checkpoint should activate for P1.");
            Assert.That(cp.IsActivatedByPlayer(2), Is.False, "P2 must remain isolated.");
            Assert.That(Checkpoint.GetLastCheckpoint(1), Is.SameAs(cp));
            Assert.That(
                Checkpoint.GetLastCheckpoint(2),
                Is.SameAs(bootstrap.PlayerTwoSpawnCheckpoint),
                "P2 keeps its own start as respawn."
            );
        }

        [UnityTest]
        public IEnumerator Phase1_Spikes_RespawnEachPlayerAtTheirOwnStart()
        {
            yield return LoadMaze();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var p1 = bootstrap.PlayerOne;
            var p2 = bootstrap.PlayerTwo;
            var p2Start = p2.transform.position;

            // P1 cai nos espinhos do corredor B da própria metade.
            var spikes = bootstrap.Spikes[0];
            Teleport(p1, spikes.transform.position + Vector3.up * 0.6f);
            yield return Settle(4);

            var expected = bootstrap.PlayerOneSpawnCheckpoint.RespawnPosition;
            Assert.That(
                Vector2.Distance(p1.transform.position, expected),
                Is.LessThan(1.5f),
                $"P1 must respawn at its own start, got {p1.transform.position} expected {expected}."
            );
            Assert.That(
                p1.transform.position.x,
                Is.LessThan(Phase1Bootstrap.ExitX),
                "P1 must not respawn on P2's side."
            );
            Assert.That(
                Vector2.Distance(p2.transform.position, p2Start),
                Is.LessThan(0.5f),
                "P2 is unaffected."
            );
        }

        [UnityTest]
        public IEnumerator Phase1_ProgressAdvancesByCorridor_NotByHorizontalPosition()
        {
            yield return LoadMaze();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var p1 = bootstrap.PlayerOne;

            // Correr até a outra ponta do corredor de baixo não gera progresso.
            Teleport(p1, new Vector2(26f, Phase1Bootstrap.Tier0Top + 0.75f));
            yield return Settle();
            Assert.That(bootstrap.PlayerOneHud.Progress, Is.EqualTo(0f).Within(0.001f));

            // No piso do corredor C: marco 2 de 4 -> 50%
            Teleport(p1, new Vector2(10.5f, Phase1Bootstrap.Tier2Top + 0.75f));
            yield return Settle();
            Assert.That(bootstrap.GetMilestone(1), Is.EqualTo(2));
            Assert.That(bootstrap.PlayerOneHud.Progress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(bootstrap.PlayerTwoHud.Progress, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Phase1_JumpingInPlace_DoesNotAdvanceProgress()
        {
            yield return LoadMaze();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var p1 = bootstrap.PlayerOne;
            var movement = p1.GetComponent<PlayerMovement>();

            yield return Settle(10);
            Assert.That(movement.IsGrounded, Is.True);
            var startProgress = bootstrap.PlayerOneHud.Progress;

            for (var jump = 0; jump < 3; jump++)
            {
                movement.RequestJump();
                for (var frame = 0; frame < 40; frame++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    Assert.That(
                        bootstrap.PlayerOneHud.Progress,
                        Is.EqualTo(startProgress).Within(0.001f),
                        "Jumping in place must not advance the milestone."
                    );
                }
            }

            Assert.That(bootstrap.GetMilestone(1), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Phase1_Race_FirstToTheExitStartsTheFinalWindow_SecondConcludes()
        {
            yield return LoadMaze();

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            var exit = bootstrap.FinishLine;
            var controller = bootstrap.MatchController;
            Assert.That(controller.RequireBothAtDoors, Is.False, "Phase 1 is a race.");
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.Running));

            // P2 chega primeiro: abre a janela final e é congelado na saída.
            Teleport(bootstrap.PlayerTwo, exit.transform.position);
            yield return Settle(2);

            Assert.That(exit.HasPlayerFinished(2), Is.True);
            Assert.That(exit.HasPlayerFinished(1), Is.False);
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.FinalCountdown));
            Assert.That(controller.FirstFinisherIndex, Is.EqualTo(2));
            Assert.That(
                bootstrap.PlayerTwo.GetComponent<PlayerMovement>().IsMovementEnabled,
                Is.False
            );
            Assert.That(
                bootstrap.PlayerOne.GetComponent<PlayerMovement>().IsMovementEnabled,
                Is.True
            );
            Assert.That(bootstrap.GetMilestone(2), Is.EqualTo(Phase1Bootstrap.ProgressMilestones));

            // P1 chega dentro da janela: partida encerra sem DNF, P2 em primeiro.
            Teleport(bootstrap.PlayerOne, exit.transform.position);
            yield return Settle(2);

            Assert.That(exit.HasPlayerFinished(1), Is.True);
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.Concluded));
            Assert.That(controller.FinalResults.HasDnf, Is.False);
            Assert.That(controller.FinalResults.Player2.Placement, Is.EqualTo(1));
            Assert.That(controller.FinalResults.Player1.Placement, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Phase1_CameraStaysFixed_WhilePlayersMoveAcrossTheMaze()
        {
            yield return LoadMaze();

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
                new Vector2(3.5f, Phase1Bootstrap.Tier0Top + 0.75f),
                new Vector2(Phase1Bootstrap.ExitX, Phase1Bootstrap.Tier3Top + 0.75f),
                new Vector2(10.5f, Phase1Bootstrap.Tier2Top + 0.75f),
            };

            foreach (var target in positions)
            {
                Teleport(bootstrap.PlayerOne, target);
                Teleport(
                    bootstrap.PlayerTwo,
                    new Vector2(Phase1Bootstrap.MirrorX(target.x), target.y)
                );
                for (var frame = 0; frame < 5; frame++)
                {
                    yield return null;
                    Assert.That(cam.transform.position, Is.EqualTo(initialPosition));
                    Assert.That(cam.orthographicSize, Is.EqualTo(initialSize).Within(0.0001f));
                }
            }

            foreach (var player in new[] { bootstrap.PlayerOne, bootstrap.PlayerTwo })
            {
                var viewport = cam.WorldToViewportPoint(player.transform.position);
                Assert.That(viewport.x, Is.InRange(0f, 1f));
                Assert.That(viewport.y, Is.InRange(0f, 1f));
            }
        }
    }
}
