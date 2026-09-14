using System.Collections;
using System.Collections.Generic;
using HealthyRace.Player;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class WellbeingPlayModeTests
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
        public IEnumerator TwoPlayers_WellbeingLocomotionMultipliers_AffectSpeedIndependently_InPlayMode()
        {
            CreateGround();
            var p1 = CreatePlayer(
                "Player 1",
                new Vector2(-4f, 0.5f),
                out var p1Wellbeing,
                out var p1Movement
            );
            var p2 = CreatePlayer(
                "Player 2",
                new Vector2(0f, 0.5f),
                out var p2Wellbeing,
                out var p2Movement
            );

            var bands = CreateDefaultBands();
            p1Wellbeing.ConfigureBands(bands);
            p2Wellbeing.ConfigureBands(bands);

            // P1 at Salo (0) -> 0.7x speed; P2 at Healthy (100) -> 1.0x speed
            p1Wellbeing.SetValue(0);
            p2Wellbeing.SetValue(100);

            yield return WaitForGrounded(p1Movement);
            yield return WaitForGrounded(p2Movement);

            Assert.That(p1Movement.MovementMultiplier, Is.EqualTo(0.7f).Within(0.01f));
            Assert.That(p2Movement.MovementMultiplier, Is.EqualTo(1.0f).Within(0.01f));

            var p1StartX = p1.transform.position.x;
            var p2StartX = p2.transform.position.x;

            p1Movement.SetHorizontalInput(1f);
            p2Movement.SetHorizontalInput(1f);

            for (var frame = 0; frame < 20; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            var p1Distance = p1.transform.position.x - p1StartX;
            var p2Distance = p2.transform.position.x - p2StartX;

            Assert.That(p1Distance, Is.GreaterThan(0.5f), "P1 should have moved forward");
            Assert.That(p2Distance, Is.GreaterThan(0.5f), "P2 should have moved forward");
            Assert.That(
                p2Distance,
                Is.GreaterThan(p1Distance),
                "P2 (1.0x speed) should cover more distance than P1 (0.7x speed)"
            );
        }

        [UnityTest]
        public IEnumerator Player_WellbeingApplyDelta_UpdatesSpeedImmediately_InPlayMode()
        {
            CreateGround();
            var player = CreatePlayer(
                "Player",
                new Vector2(0f, 0.5f),
                out var wellbeing,
                out var movement
            );
            var bands = CreateDefaultBands();
            wellbeing.ConfigureBands(bands);
            wellbeing.SetValue(100);

            yield return WaitForGrounded(movement);

            Assert.That(movement.MovementMultiplier, Is.EqualTo(1.0f).Within(0.01f));

            // Reduce wellbeing to Salo (0) via delta
            wellbeing.ApplyDelta(-100);
            Assert.That(wellbeing.CurrentValue, Is.EqualTo(0));
            Assert.That(movement.MovementMultiplier, Is.EqualTo(0.7f).Within(0.01f));

            // Recover to Healthy (100) via delta
            wellbeing.ApplyDelta(100);
            Assert.That(wellbeing.CurrentValue, Is.EqualTo(100));
            Assert.That(movement.MovementMultiplier, Is.EqualTo(1.0f).Within(0.01f));
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
            out WellbeingComponent wellbeing,
            out PlayerMovement movement
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
            movement = player.AddComponent<PlayerMovement>();
            wellbeing = player.AddComponent<WellbeingComponent>();

            var localMovement = movement;
            wellbeing.WellbeingBandChanged += band =>
            {
                if (localMovement != null && band != null)
                {
                    localMovement.SetMovementMultipliers(
                        band.MovementMultiplier,
                        band.JumpMultiplier
                    );
                }
            };

            player.SetActive(true);
            _createdObjects.Add(player);
            return player;
        }

        private static WellbeingBandData[] CreateDefaultBands()
        {
            return new[]
            {
                WellbeingBandData.Create("healthy", "VITA saud�vel", 76, 100, 1.0f, 1.0f),
                WellbeingBandData.Create("stable", "VITA est�vel", 51, 75, 0.95f, 1.0f),
                WellbeingBandData.Create("tired", "VITA cansado", 26, 50, 0.9f, 0.95f),
                WellbeingBandData.Create("critical", "Estado cr�tico", 1, 25, 0.8f, 0.9f),
                WellbeingBandData.Create("salo", "SALO", 0, 0, 0.7f, 0.8f),
            };
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
