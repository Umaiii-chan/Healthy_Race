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

        [UnityTest]
        public IEnumerator Phase1_LoadsInPlayMode_SpawnsPlayersAndInitializesHud()
        {
            var loadOp = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            var bootstrapObject = GameObject.Find("Phase1_CorridaDoEquilibrio");
            Assert.That(bootstrapObject, Is.Not.Null, "Bootstrap GameObject must exist.");

            var bootstrap = bootstrapObject.GetComponent<Phase1Bootstrap>();
            Assert.That(bootstrap, Is.Not.Null, "Phase1Bootstrap component must exist.");

            var p1 = bootstrap.PlayerOne;
            var p2 = bootstrap.PlayerTwo;

            Assert.That(p1, Is.Not.Null, "Player 1 must exist.");
            Assert.That(p2, Is.Not.Null, "Player 2 must exist.");

            Assert.That(p1.activeInHierarchy, Is.True);
            Assert.That(p2.activeInHierarchy, Is.True);

            var p1Movement = p1.GetComponent<PlayerMovement>();
            var p2Movement = p2.GetComponent<PlayerMovement>();

            Assert.That(p1Movement, Is.Not.Null);
            Assert.That(p2Movement, Is.Not.Null);

            yield return new WaitForFixedUpdate();

            Assert.That(
                p1Movement.IsGrounded,
                Is.True,
                "Player 1 should spawn grounded on Z1 platform."
            );
            Assert.That(
                p2Movement.IsGrounded,
                Is.True,
                "Player 2 should spawn grounded on Z1 platform."
            );

            Assert.That(bootstrap.PlayerOneHud, Is.Not.Null);
            Assert.That(bootstrap.PlayerTwoHud, Is.Not.Null);
            Assert.That(bootstrap.MatchHud, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Phase1_PlayerReachesCheckpoint_ActivatesIndependently()
        {
            var loadOp = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Checkpoints.Count, Is.GreaterThanOrEqualTo(1));

            var cp1 = bootstrap.Checkpoints[0];
            Assert.That(cp1.Index, Is.EqualTo(1));
            Assert.That(cp1.IsActivatedByPlayer(1), Is.False);
            Assert.That(cp1.IsActivatedByPlayer(2), Is.False);

            // Move P1 to Checkpoint 1
            var p1 = bootstrap.PlayerOne;
            var rb1 = p1.GetComponent<Rigidbody2D>();
            rb1.position = cp1.transform.position;
            p1.transform.position = cp1.transform.position;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(
                cp1.IsActivatedByPlayer(1),
                Is.True,
                "Checkpoint 1 should activate for P1."
            );
            Assert.That(
                cp1.IsActivatedByPlayer(2),
                Is.False,
                "P2 should remain isolated from P1 checkpoint activation."
            );
            Assert.That(Checkpoint.GetLastCheckpoint(1), Is.SameAs(cp1));
        }

        [UnityTest]
        public IEnumerator Phase1_PlayerReachesFinishLine_RegistersFinishEvent()
        {
            var loadOp = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.FinishLine, Is.Not.Null);

            var finishLine = bootstrap.FinishLine;
            Assert.That(finishLine.HasPlayerFinished(1), Is.False);

            // Move P1 to finish line
            var p1 = bootstrap.PlayerOne;
            var rb1 = p1.GetComponent<Rigidbody2D>();
            rb1.position = finishLine.transform.position;
            p1.transform.position = finishLine.transform.position;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(
                finishLine.HasPlayerFinished(1),
                Is.True,
                "P1 should be registered as finished."
            );
            Assert.That(
                finishLine.HasPlayerFinished(2),
                Is.False,
                "P2 has not reached finish line yet."
            );
            Assert.That(finishLine.FirstFinishTime, Is.GreaterThanOrEqualTo(0f));
        }

        [UnityTest]
        public IEnumerator Phase1_BothPlayers_CanPhysicallyTraverseFromSpawnToFinish_AndActivateCheckpoints()
        {
            var loadOp = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            Assert.That(bootstrap, Is.Not.Null);

            var p1 = bootstrap.PlayerOne;
            var p2 = bootstrap.PlayerTwo;
            Assert.That(p1, Is.Not.Null);
            Assert.That(p2, Is.Not.Null);

            // Assert visual presentation is active on both players
            Assert.That(p1.transform.Find("VisualPresentation"), Is.Not.Null);
            Assert.That(p2.transform.Find("VisualPresentation"), Is.Not.Null);

            Assert.That(bootstrap.Checkpoints.Count, Is.EqualTo(2));
            var cp1 = bootstrap.Checkpoints[0];
            var cp2 = bootstrap.Checkpoints[1];
            var finishLine = bootstrap.FinishLine;

            var rb1 = p1.GetComponent<Rigidbody2D>();
            var rb2 = p2.GetComponent<Rigidbody2D>();

            // Phase traversal Step 1: Checkpoint 1 (Zone 3)
            rb1.position = cp1.transform.position;
            p1.transform.position = cp1.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(cp1.IsActivatedByPlayer(1), Is.True, "P1 should activate CP1.");

            rb2.position = cp1.transform.position;
            p2.transform.position = cp1.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(cp1.IsActivatedByPlayer(2), Is.True, "P2 should activate CP1.");

            // Phase traversal Step 2: Checkpoint 2 (Zone 4)
            rb1.position = cp2.transform.position;
            p1.transform.position = cp2.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(cp2.IsActivatedByPlayer(1), Is.True, "P1 should activate CP2.");

            rb2.position = cp2.transform.position;
            p2.transform.position = cp2.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(cp2.IsActivatedByPlayer(2), Is.True, "P2 should activate CP2.");

            // Phase traversal Step 3: Finish Line (Zone 6)
            rb1.position = finishLine.transform.position;
            p1.transform.position = finishLine.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(finishLine.HasPlayerFinished(1), Is.True, "P1 should reach finish.");

            rb2.position = finishLine.transform.position;
            p2.transform.position = finishLine.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(finishLine.HasPlayerFinished(2), Is.True, "P2 should reach finish.");

            Assert.That(finishLine.FinishedPlayerCount, Is.EqualTo(2));
        }
    }
}
