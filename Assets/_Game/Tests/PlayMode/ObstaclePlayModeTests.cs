using System.Collections;
using System.Collections.Generic;
using HealthyRace.Level;
using HealthyRace.Obstacles;
using HealthyRace.Player;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class ObstaclePlayModeTests
    {
        private const int GroundLayer = 6;
        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            Checkpoint.ResetRegistry();

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
        public IEnumerator Player_StepsOnSpikeHazard_TakesPenaltyAndRespawns()
        {
            CreateGround(new Vector2(0f, -1f), new Vector2(40f, 1f));

            var cpObj = new GameObject("Checkpoint");
            cpObj.transform.position = new Vector2(2f, 0f);
            var cpCol = cpObj.AddComponent<BoxCollider2D>();
            cpCol.isTrigger = true;
            var checkpoint = cpObj.AddComponent<Checkpoint>();
            checkpoint.Configure(1, new Vector2(0f, 0.5f));
            _createdObjects.Add(cpObj);

            var player = CreatePlayer(new Vector2(0f, 0.5f), 1);
            var wb = player.GetComponent<WellbeingComponent>();
            var initialWb = wb.CurrentValue;

            // Step on checkpoint to register it
            checkpoint.ActivateForPlayer(1);

            // Create spike hazard at x = 6
            var spikeObj = new GameObject("Spikes");
            spikeObj.transform.position = new Vector2(6f, 0f);
            var spikeCol = spikeObj.AddComponent<BoxCollider2D>();
            spikeCol.isTrigger = true;
            spikeCol.size = new Vector2(2f, 1f);
            var spike = spikeObj.AddComponent<SpikeHazard>();
            spike.Configure(5, 1f, new Vector2(2f, 0.5f));
            _createdObjects.Add(spikeObj);

            // Walk player into spikes
            player.GetComponent<PlayerMovement>().SetHorizontalInput(1f);

            for (var frame = 0; frame < 60; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (wb.CurrentValue < initialWb)
                {
                    break;
                }
            }

            Assert.That(
                wb.CurrentValue,
                Is.EqualTo(initialWb - 5),
                "Player should have lost 5 wellbeing points."
            );
            Assert.That(
                player.transform.position.x,
                Is.EqualTo(checkpoint.RespawnPosition.x).Within(0.5f),
                "Player should have respawned at checkpoint X."
            );
        }

        [UnityTest]
        public IEnumerator Player_EntersMudHazard_ReducesSpeed_AndRestoresOnExit()
        {
            CreateGround(new Vector2(0f, -1f), new Vector2(40f, 1f));

            var mudObj = new GameObject("MudHazard");
            mudObj.transform.position = new Vector2(3f, 0f);
            var mudCol = mudObj.AddComponent<BoxCollider2D>();
            mudCol.isTrigger = true;
            mudCol.size = new Vector2(2f, 1f);
            var mud = mudObj.AddComponent<MudHazard>();
            mud.Configure(0.7f);
            _createdObjects.Add(mudObj);

            var player = CreatePlayer(new Vector2(0f, 0.5f), 1);
            var movement = player.GetComponent<PlayerMovement>();

            Assert.That(movement.MovementMultiplier, Is.EqualTo(1f).Within(0.01f));

            // Walk into mud
            movement.SetHorizontalInput(1f);

            for (var frame = 0; frame < 40; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (mud.IsPlayerAffected(movement))
                {
                    break;
                }
            }

            Assert.That(
                mud.IsPlayerAffected(movement),
                Is.True,
                "Player should be recognized as affected by mud."
            );
            Assert.That(
                movement.MovementMultiplier,
                Is.EqualTo(0.7f).Within(0.01f),
                "Speed multiplier should be 0.7 while in mud."
            );

            // Continue walking to exit mud
            for (var frame = 0; frame < 60; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (!mud.IsPlayerAffected(movement))
                {
                    break;
                }
            }

            Assert.That(
                mud.IsPlayerAffected(movement),
                Is.False,
                "Player should no longer be in mud."
            );
            Assert.That(
                movement.MovementMultiplier,
                Is.EqualTo(1f).Within(0.01f),
                "Speed multiplier should be restored to 1.0 after exiting mud."
            );
        }

        [UnityTest]
        public IEnumerator Player_LandsOnFallingPlatform_TriggersCollapseAndRespawn()
        {
            var platObj = new GameObject("FallingPlatform");
            platObj.transform.position = new Vector2(0f, 0f);
            platObj.layer = GroundLayer;
            var platCol = platObj.AddComponent<BoxCollider2D>();
            platCol.size = new Vector2(4f, 0.5f);
            var platform = platObj.AddComponent<FallingPlatform>();
            platform.Configure(
                anticipationDuration: 0.2f,
                respawnDelay: 0.4f,
                shakeIntensity: 0.05f
            );
            _createdObjects.Add(platObj);

            Assert.That(platform.State, Is.EqualTo(FallingPlatformState.Idle));

            var player = CreatePlayer(new Vector2(0f, 1.5f), 1);

            // Wait for player to land on platform
            for (var frame = 0; frame < 40; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (platform.State == FallingPlatformState.Shaking)
                {
                    break;
                }
            }

            Assert.That(
                platform.State,
                Is.EqualTo(FallingPlatformState.Shaking),
                "Platform should shake after player lands on it."
            );

            // Wait for anticipation to elapse and platform to fall
            yield return new WaitForSeconds(0.25f);

            Assert.That(
                platform.State,
                Is.EqualTo(FallingPlatformState.Fallen),
                "Platform should enter Fallen state."
            );
            Assert.That(platCol.enabled, Is.False, "Collider should be disabled while fallen.");

            // Wait for respawn delay to elapse
            yield return new WaitForSeconds(0.45f);

            Assert.That(
                platform.State,
                Is.EqualTo(FallingPlatformState.Idle),
                "Platform should return to Idle state after respawn."
            );
            Assert.That(platCol.enabled, Is.True, "Collider should be re-enabled on respawn.");
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
            player.AddComponent<WellbeingComponent>();

            player.SetActive(true);
            _createdObjects.Add(player);
            return player;
        }
    }
}
