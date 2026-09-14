using System.Collections;
using System.Collections.Generic;
using HealthyRace.Input;
using HealthyRace.Player;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class FatiguePlayModeTests
    {
        private const int GroundLayer = 6;
        private const int MaximumGroundingFrames = 180;

        private readonly List<GameObject> _createdObjects = new();
        private Vector2 _originalGravity;

        [SetUp]
        public void SetUp()
        {
            _originalGravity = Physics2D.gravity;
            Physics2D.gravity = new Vector2(0f, -12f);
        }

        [TearDown]
        public void TearDown()
        {
            Physics2D.gravity = _originalGravity;

            foreach (var createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();
        }

        [UnityTest]
        public IEnumerator Fatigue_LocksMovementForDuration_AndAppliesAutoRecovery_InPlayMode()
        {
            CreateGround();
            var player = CreatePlayer(
                "Fatigue Test Player",
                new Vector2(0f, 0.5f),
                out var controller,
                out var movement,
                out var wellbeing,
                out var fatigue
            );

            // Use short duration for test speed
            fatigue.Configure(0.3f, 15, 20, 25);
            wellbeing.SetValue(100);

            yield return WaitForGrounded(movement);

            Assert.That(controller.IsMovementEnabled, Is.True);
            Assert.That(fatigue.State, Is.EqualTo(FatigueState.Normal));

            // Cross threshold: 100 -> 20
            wellbeing.SetValue(20);

            Assert.That(fatigue.State, Is.EqualTo(FatigueState.Fatigued));
            Assert.That(
                controller.IsMovementEnabled,
                Is.False,
                "Movement must be locked upon fatigue"
            );
            Assert.That(fatigue.FatigueEpisodeCount, Is.EqualTo(1));

            // Attempt movement while fatigued
            movement.SetHorizontalInput(1f);
            yield return new WaitForFixedUpdate();
            Assert.That(
                movement.Velocity.x,
                Is.EqualTo(0f),
                "Velocity must stay zero while fatigued"
            );

            // Wait for fatigue duration to expire
            yield return new WaitForSeconds(0.4f);

            Assert.That(fatigue.State, Is.EqualTo(FatigueState.Normal));
            Assert.That(
                controller.IsMovementEnabled,
                Is.True,
                "Movement must be restored after fatigue"
            );
            Assert.That(
                wellbeing.CurrentValue,
                Is.EqualTo(35),
                "Auto recovery must add +15 (20 + 15 = 35)"
            );
            Assert.That(
                fatigue.Readiness,
                Is.EqualTo(FatigueReadiness.Armed),
                "35 > 25, so fatigue must rearm"
            );
        }

        [UnityTest]
        public IEnumerator Fatigue_CrossingThresholdInAir_DelaysUntilGrounded_InPlayMode()
        {
            CreateGround();
            var player = CreatePlayer(
                "Airborne Player",
                new Vector2(0f, 3f),
                out var controller,
                out var movement,
                out var wellbeing,
                out var fatigue
            );

            fatigue.Configure(0.3f, 15, 20, 25);
            wellbeing.SetValue(100);

            // In air
            Assert.That(movement.IsGrounded, Is.False);

            // Cross threshold while in mid-air
            wellbeing.SetValue(20);

            Assert.That(
                fatigue.State,
                Is.EqualTo(FatigueState.FatiguePending),
                "Must enter FatiguePending while in air"
            );
            Assert.That(fatigue.FatigueEpisodeCount, Is.EqualTo(1));

            // Fall and touch ground
            yield return WaitForGrounded(movement);

            // Give a frame for Update to detect grounding
            yield return null;

            Assert.That(
                fatigue.State,
                Is.EqualTo(FatigueState.Fatigued),
                "Must transition to Fatigued upon landing"
            );
            Assert.That(controller.IsMovementEnabled, Is.False);

            // Wait for recovery
            yield return new WaitForSeconds(0.4f);
            Assert.That(fatigue.State, Is.EqualTo(FatigueState.Normal));
            Assert.That(wellbeing.CurrentValue, Is.EqualTo(35));
        }

        [UnityTest]
        public IEnumerator TwoPlayers_FatigueEpisodeOnPlayerOne_LeavesPlayerTwoCompletelyUnaffected_InPlayMode()
        {
            CreateGround();
            var p1 = CreatePlayer(
                "Player 1",
                new Vector2(-3f, 0.5f),
                out var p1Controller,
                out var p1Movement,
                out var p1Wellbeing,
                out var p1Fatigue
            );

            var p2 = CreatePlayer(
                "Player 2",
                new Vector2(3f, 0.5f),
                out var p2Controller,
                out var p2Movement,
                out var p2Wellbeing,
                out var p2Fatigue
            );

            p1Fatigue.Configure(0.4f, 15, 20, 25);
            p2Fatigue.Configure(0.4f, 15, 20, 25);

            p1Wellbeing.SetValue(100);
            p2Wellbeing.SetValue(100);

            yield return WaitForGrounded(p1Movement);
            yield return WaitForGrounded(p2Movement);

            // P1 enters fatigue
            p1Wellbeing.SetValue(20);

            Assert.That(p1Fatigue.State, Is.EqualTo(FatigueState.Fatigued));
            Assert.That(p1Controller.IsMovementEnabled, Is.False);
            Assert.That(p1Fatigue.FatigueEpisodeCount, Is.EqualTo(1));

            // P2 remains completely unaffected
            Assert.That(p2Fatigue.State, Is.EqualTo(FatigueState.Normal));
            Assert.That(p2Controller.IsMovementEnabled, Is.True);
            Assert.That(p2Fatigue.FatigueEpisodeCount, Is.EqualTo(0));
            Assert.That(p2Wellbeing.CurrentValue, Is.EqualTo(100));

            // P2 can move freely while P1 is locked
            p2Controller.enabled = false;
            var p2StartX = p2.transform.position.x;
            p2Movement.SetHorizontalInput(1f);

            for (var frame = 0; frame < 10; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(p2.transform.position.x, Is.GreaterThan(p2StartX));
        }

        private GameObject CreateGround()
        {
            var ground = new GameObject("Ground");
            ground.layer = GroundLayer;
            ground.transform.position = new Vector2(0f, -1f);

            var collider = ground.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(40f, 1f);
            _createdObjects.Add(ground);
            return ground;
        }

        private GameObject CreatePlayer(
            string name,
            Vector2 position,
            out PlayerController controller,
            out PlayerMovement movement,
            out WellbeingComponent wellbeing,
            out FatigueComponent fatigue
        )
        {
            var player = new GameObject(name);
            player.transform.position = position;
            player.SetActive(false);

            var rigidbody = player.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 2f;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            player.AddComponent<PlayerGroundCheck>();
            var inputReader = player.AddComponent<PlayerInputReader>();
            var inputActions =
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                    "Assets/_Game/Input/HealthyRaceInputActions.inputactions"
                );
            if (inputActions != null)
            {
                inputReader.Configure(inputActions, PlayerInputId.Player1);
            }

            movement = player.AddComponent<PlayerMovement>();
            controller = player.AddComponent<PlayerController>();
            wellbeing = player.AddComponent<WellbeingComponent>();
            fatigue = player.AddComponent<FatigueComponent>();

            player.SetActive(true);
            _createdObjects.Add(player);
            return player;
        }

        private static IEnumerator WaitForGrounded(PlayerMovement player)
        {
            for (var frame = 0; frame < MaximumGroundingFrames; frame++)
            {
                yield return new WaitForFixedUpdate();

                if (player.IsGrounded)
                {
                    yield break;
                }
            }

            Assert.Fail("Player did not recognize the ground within the expected physics frames.");
        }
    }
}
