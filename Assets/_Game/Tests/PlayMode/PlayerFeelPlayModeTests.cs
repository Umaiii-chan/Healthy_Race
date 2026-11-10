using System.Collections;
using System.Collections.Generic;
using HealthyRace.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    /// <summary>
    /// Sensação do controle: aceleração no chão, tolerância ao sair de uma borda
    /// e pulo apertado um pouco antes de tocar o chão.
    /// </summary>
    public sealed class PlayerFeelPlayModeTests
    {
        private const int GroundLayer = 6;
        private const float GroundTop = -0.5f;

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

        private void CreateGround(float width)
        {
            var ground = new GameObject("Ground") { layer = GroundLayer };
            ground.transform.position = new Vector2(0f, GroundTop - 0.5f);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(width, 1f);
            _createdObjects.Add(ground);
        }

        private PlayerMovement CreatePlayer(Vector2 position)
        {
            var player = new GameObject("Feel Test Player");
            player.transform.position = position;
            player.SetActive(false);
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 2f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            // igual ao prefab do jogador: sem atrito (ver PM_Player)
            collider.sharedMaterial = new PhysicsMaterial2D("Test") { friction = 0f };
            player.AddComponent<PlayerGroundCheck>();
            var movement = player.AddComponent<PlayerMovement>();
            player.SetActive(true);
            _createdObjects.Add(player);
            return movement;
        }

        private static IEnumerator WaitForGrounded(PlayerMovement player)
        {
            for (var frame = 0; frame < 180 && !player.IsGrounded; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.IsGrounded, Is.True, "O jogador não chegou ao chão.");

            // o sensor enxerga o chão um pouco antes do contato: espera assentar
            for (var frame = 0; frame < 5; frame++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private static IEnumerator WalkOffTheLedge(PlayerMovement player)
        {
            player.SetHorizontalInput(1f);
            for (var frame = 0; frame < 120 && player.IsGrounded; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.IsGrounded, Is.False, "O jogador não saiu da borda.");
        }

        [UnityTest]
        public IEnumerator GroundAcceleration_RampsUpInsteadOfSnapping()
        {
            CreateGround(40f);
            var player = CreatePlayer(new Vector2(0f, 0.5f));
            yield return WaitForGrounded(player);

            player.SetHorizontalInput(1f);
            yield return new WaitForFixedUpdate();
            Assert.That(player.Velocity.x, Is.GreaterThan(0f));
            Assert.That(
                player.Velocity.x,
                Is.LessThan(player.MoveSpeed),
                "No primeiro passo a velocidade ainda está subindo."
            );

            for (var frame = 0; frame < 12; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.Velocity.x, Is.EqualTo(player.MoveSpeed).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator CoyoteTime_AllowsJumpRightAfterLeavingTheLedge()
        {
            CreateGround(4f);
            var player = CreatePlayer(new Vector2(1f, 0.5f));
            yield return WaitForGrounded(player);
            yield return WalkOffTheLedge(player);

            player.RequestJump();
            yield return new WaitForFixedUpdate();

            Assert.That(
                player.Velocity.y,
                Is.GreaterThan(0f),
                "Logo depois de sair da borda o pulo ainda deve ser aceito."
            );
        }

        [UnityTest]
        public IEnumerator CoyoteTime_ExpiresAfterTheWindow()
        {
            CreateGround(4f);
            var player = CreatePlayer(new Vector2(1f, 0.5f));
            yield return WaitForGrounded(player);
            yield return WalkOffTheLedge(player);

            yield return new WaitForSeconds(player.CoyoteTime + 0.1f);
            player.RequestJump();
            yield return new WaitForFixedUpdate();

            Assert.That(
                player.Velocity.y,
                Is.LessThan(0f),
                "Passada a tolerância, pular no ar não pode funcionar."
            );
        }

        [UnityTest]
        public IEnumerator JumpBuffer_JumpPressedJustBeforeLandingStillJumps()
        {
            CreateGround(40f);
            var player = CreatePlayer(new Vector2(0f, 2.5f));

            var requested = false;
            var jumped = false;
            for (var frame = 0; frame < 240 && !jumped; frame++)
            {
                yield return new WaitForFixedUpdate();
                var feet = player.transform.position.y - 0.5f;
                if (!requested && player.Velocity.y < 0f && feet - GroundTop < 0.3f)
                {
                    Assert.That(player.IsGrounded, Is.False, "O pedido tem de ser feito no ar.");
                    player.RequestJump();
                    requested = true;
                }
                else if (requested && player.Velocity.y > 1f)
                {
                    jumped = true;
                }
            }

            Assert.That(requested, Is.True);
            Assert.That(jumped, Is.True, "O pulo guardado deveria sair ao tocar o chão.");
        }

        [UnityTest]
        public IEnumerator RepeatedJumpRequests_NeverProduceADoubleJump()
        {
            CreateGround(40f);
            var single = CreatePlayer(new Vector2(-5f, 0.5f));
            var spammer = CreatePlayer(new Vector2(5f, 0.5f));
            yield return WaitForGrounded(single);
            yield return WaitForGrounded(spammer);

            single.RequestJump();
            var singleApex = float.MinValue;
            var spamApex = float.MinValue;
            for (var frame = 0; frame < 90; frame++)
            {
                // o segundo jogador aperta pulo em todo passo de física
                spammer.RequestJump();
                yield return new WaitForFixedUpdate();
                singleApex = Mathf.Max(singleApex, single.transform.position.y);
                if (frame < 25)
                {
                    spamApex = Mathf.Max(spamApex, spammer.transform.position.y);
                }
            }

            Assert.That(
                spamApex,
                Is.EqualTo(singleApex).Within(0.05f),
                "Apertar pulo sem parar não pode render altura extra."
            );
        }
    }
}
