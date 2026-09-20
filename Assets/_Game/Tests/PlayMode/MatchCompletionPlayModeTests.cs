using System.Collections;
using System.Collections.Generic;
using HealthyRace.Level;
using HealthyRace.Player;
using HealthyRace.UI;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class MatchCompletionPlayModeTests
    {
        private const int GroundLayer = 6;
        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator Player_CrossingFinishLine_StopsLocomotion_InPlayMode()
        {
            CreateGround(new Vector2(0f, -1f), new Vector2(30f, 1f));

            var flObj = new GameObject("FinishLine");
            flObj.transform.position = new Vector2(4f, 0f);
            var col = flObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 3f);
            col.isTrigger = true;
            var finishLine = flObj.AddComponent<FinishLine>();
            _createdObjects.Add(flObj);

            var p1 = CreatePlayer(new Vector2(0f, 0f), 1);
            var p2 = CreatePlayer(new Vector2(-5f, 0f), 2);

            var ctrlObj = new GameObject("MatchController");
            var controller = ctrlObj.AddComponent<MatchCompletionController>();
            controller.Configure(
                finishLine: finishLine,
                matchHud: null,
                playerOne: p1,
                playerTwo: p2,
                p1Wb: p1.GetComponent<WellbeingComponent>(),
                p2Wb: p2.GetComponent<WellbeingComponent>(),
                resultsPanel: null,
                finalWindowDuration: 10f,
                dnfPenalty: 20
            );
            _createdObjects.Add(ctrlObj);

            var movement = p1.GetComponent<PlayerMovement>();
            Assert.That(movement.IsMovementEnabled, Is.True);

            // Settle on ground
            for (var frame = 0; frame < 20; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            // Walk player into finish line
            movement.SetHorizontalInput(1f);

            for (var frame = 0; frame < 80; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (finishLine.HasPlayerFinished(1))
                {
                    break;
                }
            }

            Assert.That(
                finishLine.HasPlayerFinished(1),
                Is.True,
                "Player 1 should have crossed finish line."
            );
            Assert.That(
                movement.IsMovementEnabled,
                Is.False,
                "Player 1 movement should be frozen upon finishing."
            );
        }

        [UnityTest]
        public IEnumerator FirstFinisher_TriggersFinalCountdownInMatchHUD_InPlayMode()
        {
            var canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _createdObjects.Add(canvasObj);

            var hudObj = new GameObject("MatchHUD");
            var matchHud = hudObj.AddComponent<MatchHUD>();
            matchHud.Initialize(canvasObj.transform, Vector2.zero);
            _createdObjects.Add(hudObj);

            var flObj = new GameObject("FinishLine");
            var col = flObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var finishLine = flObj.AddComponent<FinishLine>();
            _createdObjects.Add(flObj);

            var p1 = CreatePlayer(new Vector2(0f, 0f), 1);
            var p2 = CreatePlayer(new Vector2(0f, 0f), 2);

            var ctrlObj = new GameObject("MatchController");
            var controller = ctrlObj.AddComponent<MatchCompletionController>();
            controller.Configure(
                finishLine: finishLine,
                matchHud: matchHud,
                playerOne: p1,
                playerTwo: p2,
                p1Wb: p1.GetComponent<WellbeingComponent>(),
                p2Wb: p2.GetComponent<WellbeingComponent>(),
                resultsPanel: null,
                finalWindowDuration: 5f,
                dnfPenalty: 20
            );
            _createdObjects.Add(ctrlObj);

            Assert.That(matchHud.IsFinalWindowActive, Is.False);

            // Trigger P1 finish
            finishLine.RecordFinish(1);

            yield return null;

            Assert.That(
                matchHud.IsFinalWindowActive,
                Is.True,
                "Match HUD final window should be active."
            );
            Assert.That(
                matchHud.FinalWindowTimeRemaining,
                Is.GreaterThan(0f),
                "Final window should have time remaining."
            );
        }

        [UnityTest]
        public IEnumerator MatchCompletion_CountdownExpiration_MarksDnfAndAppliesPenalty_InPlayMode()
        {
            var flObj = new GameObject("FinishLine");
            var col = flObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var finishLine = flObj.AddComponent<FinishLine>();
            _createdObjects.Add(flObj);

            var p1 = CreatePlayer(new Vector2(0f, 0f), 1);
            var p2 = CreatePlayer(new Vector2(0f, 0f), 2);
            var p2Wb = p2.GetComponent<WellbeingComponent>();
            var initialP2Wb = p2Wb.CurrentValue;

            var ctrlObj = new GameObject("MatchController");
            var controller = ctrlObj.AddComponent<MatchCompletionController>();
            controller.Configure(
                finishLine: finishLine,
                matchHud: null,
                playerOne: p1,
                playerTwo: p2,
                p1Wb: p1.GetComponent<WellbeingComponent>(),
                p2Wb: p2Wb,
                resultsPanel: null,
                finalWindowDuration: 0.15f,
                dnfPenalty: 20
            );
            _createdObjects.Add(ctrlObj);

            // P1 finishes
            finishLine.RecordFinish(1);
            Assert.That(controller.State, Is.EqualTo(MatchCompletionState.FinalCountdown));

            // Wait for 0.15s window to expire
            yield return new WaitForSeconds(0.3f);
            for (
                var frame = 0;
                frame < 30 && controller.State != MatchCompletionState.Concluded;
                frame++
            )
            {
                yield return null;
            }

            Assert.That(
                controller.State,
                Is.EqualTo(MatchCompletionState.Concluded),
                "Match should be concluded after window expiry."
            );
            Assert.That(
                controller.FinalResults.Player2.IsDnf,
                Is.True,
                "Player 2 should be marked DNF."
            );
            Assert.That(
                p2Wb.CurrentValue,
                Is.EqualTo(initialP2Wb - 20),
                "Player 2 should have received DNF penalty."
            );
        }

        private GameObject CreateGround(Vector2 position, Vector2 size)
        {
            var ground = new GameObject("Ground");
            ground.layer = GroundLayer;
            ground.transform.position = position;

            var col = ground.AddComponent<BoxCollider2D>();
            col.size = size;

            _createdObjects.Add(ground);
            return ground;
        }

        private GameObject CreatePlayer(Vector2 position, int playerIndex)
        {
            var player = new GameObject($"Player_{playerIndex}");
            player.transform.position = position;
            player.SetActive(false);

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = player.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            player.AddComponent<PlayerGroundCheck>();
            player.AddComponent<PlayerMovement>();
            var wb = player.AddComponent<WellbeingComponent>();
            wb.Initialize(100);

            player.SetActive(true);
            _createdObjects.Add(player);
            return player;
        }
    }
}
