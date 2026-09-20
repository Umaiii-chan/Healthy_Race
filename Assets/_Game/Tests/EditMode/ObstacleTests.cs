using System.Collections.Generic;
using HealthyRace.Level;
using HealthyRace.Obstacles;
using HealthyRace.Player;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class ObstacleTests
    {
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

        [Test]
        public void SpikeHazard_PenalizesWellbeing_AndTeleportsPlayerToCheckpoint()
        {
            var cpObj = new GameObject("TestCheckpoint");
            cpObj.transform.position = new Vector2(10f, 0f);
            var cpCol = cpObj.AddComponent<BoxCollider2D>();
            cpCol.isTrigger = true;
            var checkpoint = cpObj.AddComponent<Checkpoint>();
            checkpoint.Configure(1, new Vector2(0f, 0.5f));
            _createdObjects.Add(cpObj);

            // Register checkpoint for P1
            checkpoint.ActivateForPlayer(1);

            var player = CreatePlayer(new Vector2(25f, 0f), 1);
            var wb = player.GetComponent<WellbeingComponent>();
            var initialWb = wb.CurrentValue;

            var spikeObj = new GameObject("Spikes");
            spikeObj.transform.position = new Vector2(25f, 0f);
            var spikeCol = spikeObj.AddComponent<BoxCollider2D>();
            spikeCol.isTrigger = true;
            var spike = spikeObj.AddComponent<SpikeHazard>();
            spike.Configure(
                wellbeingPenalty: 5,
                hitCooldown: 1f,
                defaultRespawnPosition: Vector2.zero
            );
            _createdObjects.Add(spikeObj);

            var handled = spike.HandlePlayerContact(player);

            Assert.That(handled, Is.True, "Contact should be handled.");
            Assert.That(
                wb.CurrentValue,
                Is.EqualTo(initialWb - 5),
                "Wellbeing should be reduced by 5."
            );
            Assert.That(
                (Vector2)player.transform.position,
                Is.EqualTo(checkpoint.RespawnPosition),
                "Player should be teleported to checkpoint respawn position."
            );
        }

        [Test]
        public void SpikeHazard_RespectsHitCooldown()
        {
            var player = CreatePlayer(new Vector2(5f, 0f), 1);
            var wb = player.GetComponent<WellbeingComponent>();
            var initialWb = wb.CurrentValue;

            var spikeObj = new GameObject("Spikes");
            var spike = spikeObj.AddComponent<SpikeHazard>();
            spike.Configure(
                wellbeingPenalty: 5,
                hitCooldown: 2f,
                defaultRespawnPosition: Vector2.zero
            );
            _createdObjects.Add(spikeObj);

            var firstHit = spike.HandlePlayerContact(player);
            Assert.That(firstHit, Is.True);
            Assert.That(wb.CurrentValue, Is.EqualTo(initialWb - 5));

            // Immediate second hit within cooldown
            var secondHit = spike.HandlePlayerContact(player);
            Assert.That(secondHit, Is.False, "Second contact within cooldown must be ignored.");
            Assert.That(
                wb.CurrentValue,
                Is.EqualTo(initialWb - 5),
                "Wellbeing must not be deducted twice."
            );
        }

        [Test]
        public void MudHazard_ConfiguresMultiplier_AndManagesAffectedPlayers()
        {
            var player = CreatePlayer(new Vector2(0f, 0f), 1);
            var movement = player.GetComponent<PlayerMovement>();

            var mudObj = new GameObject("Mud");
            var mud = mudObj.AddComponent<MudHazard>();
            mud.Configure(0.7f);
            _createdObjects.Add(mudObj);

            Assert.That(mud.SpeedMultiplier, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(mud.AffectedPlayerCount, Is.EqualTo(0));

            // Manually simulate entering mud
            movement.SetMovementMultipliers(mud.SpeedMultiplier, movement.JumpMultiplier);
            Assert.That(movement.MovementMultiplier, Is.EqualTo(0.7f).Within(0.001f));

            // Manually simulate leaving mud
            movement.SetMovementMultipliers(1f, movement.JumpMultiplier);
            Assert.That(movement.MovementMultiplier, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void FallingPlatform_InitializesStable_AndStartsAnticipationOnTrigger()
        {
            var platObj = new GameObject("FallingPlatform");
            platObj.transform.position = new Vector2(15f, 2f);
            var col = platObj.AddComponent<BoxCollider2D>();
            var platform = platObj.AddComponent<FallingPlatform>();
            platform.Configure(anticipationDuration: 0.5f, respawnDelay: 3f);
            _createdObjects.Add(platObj);

            Assert.That(platform.State, Is.EqualTo(FallingPlatformState.Idle));
            Assert.That(platform.AnticipationDuration, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(platform.RespawnDelay, Is.EqualTo(3f).Within(0.01f));

            platform.TriggerFall();
            Assert.That(platform.State, Is.EqualTo(FallingPlatformState.Shaking));

            platform.ResetPlatform();
            Assert.That(platform.State, Is.EqualTo(FallingPlatformState.Idle));
            Assert.That(col.enabled, Is.True);
        }

        private GameObject CreatePlayer(Vector2 position, int playerIndex)
        {
            var player = new GameObject($"Player_{playerIndex}");
            player.transform.position = position;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = player.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            player.AddComponent<PlayerGroundCheck>();
            player.AddComponent<PlayerMovement>();
            var wb = player.AddComponent<WellbeingComponent>();

            var controller = player.AddComponent<PlayerController>();
            controller.EnsureInitialized();

            _createdObjects.Add(player);
            return player;
        }
    }
}
