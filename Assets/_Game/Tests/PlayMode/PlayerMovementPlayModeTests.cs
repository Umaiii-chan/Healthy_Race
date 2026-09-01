using System.Collections;
using System.Collections.Generic;
using HealthyRace.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class PlayerMovementPlayModeTests
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
        public IEnumerator PlayerFallsAndRecognizesGround()
        {
            CreateGround();
            var player = CreatePlayer(new Vector2(0f, 2f));

            yield return WaitForGrounded(player);

            Assert.That(player.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator PlayerJumpsOnlyWhileGrounded()
        {
            CreateGround();
            var player = CreatePlayer(new Vector2(0f, 0.5f));

            yield return WaitForGrounded(player);

            player.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.That(player.Velocity.y, Is.GreaterThan(0f));

            yield return new WaitForFixedUpdate();
            Assert.That(player.IsGrounded, Is.False);

            var velocityBeforeAirJumpRequest = player.Velocity.y;
            player.RequestJump();
            yield return new WaitForFixedUpdate();

            Assert.That(player.Velocity.y, Is.LessThan(velocityBeforeAirJumpRequest));
        }

        [UnityTest]
        public IEnumerator PlayerInstancesMoveIndependently()
        {
            CreateGround();
            var playerOne = CreatePlayer(new Vector2(-3f, 0.5f));
            var playerTwo = CreatePlayer(new Vector2(3f, 0.5f));

            yield return WaitForGrounded(playerOne);
            yield return WaitForGrounded(playerTwo);

            var playerOneStart = playerOne.transform.position.x;
            var playerTwoStart = playerTwo.transform.position.x;
            playerOne.SetHorizontalInput(-1f);
            playerTwo.SetHorizontalInput(1f);

            for (var frame = 0; frame < 12; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(playerOne.transform.position.x, Is.LessThan(playerOneStart));
            Assert.That(playerTwo.transform.position.x, Is.GreaterThan(playerTwoStart));
        }

        private GameObject CreateGround()
        {
            var ground = new GameObject("Ground");
            ground.layer = GroundLayer;
            ground.transform.position = new Vector2(0f, -1f);

            var collider = ground.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(20f, 1f);
            _createdObjects.Add(ground);
            return ground;
        }

        private PlayerMovement CreatePlayer(Vector2 position)
        {
            var player = new GameObject("Movement Test Player");
            player.transform.position = position;
            player.SetActive(false);

            var rigidbody = player.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 2f;
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            player.AddComponent<PlayerGroundCheck>();
            var playerMovement = player.AddComponent<PlayerMovement>();
            player.SetActive(true);
            _createdObjects.Add(player);
            return playerMovement;
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

            Assert.Fail(
                "Player did not recognize the ground within the expected number of physics frames."
            );
        }
    }
}
