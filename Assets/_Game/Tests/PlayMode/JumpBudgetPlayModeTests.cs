using System.Collections;
using System.Collections.Generic;
using HealthyRace.Level;
using HealthyRace.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    /// <summary>
    /// Guarda-corpo da física: mede ápice e alcance reais em runtime e confronta
    /// com o <see cref="JumpBudget"/> declarado. Um teste que compara constante com
    /// constante não detecta divergência entre documentação e física; este mede.
    /// </summary>
    public sealed class JumpBudgetPlayModeTests
    {
        private const int GroundLayer = 6;
        private const int MaximumFrames = 600;
        private const float Tolerance = 0.05f;

        private readonly List<GameObject> _createdObjects = new();
        private Vector2 _originalGravity;

        [SetUp]
        public void SetUp()
        {
            _originalGravity = Physics2D.gravity;
            Physics2D.gravity = new Vector2(0f, Phase1Bootstrap.PhaseGravityY);
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
        public IEnumerator JumpBudget_MatchesMeasuredPhysics_ForPhasePlayer()
        {
            CreateGround();
            var player = CreatePhasePlayer(new Vector2(0f, 0.5f));

            yield return WaitForGrounded(player);
            Assert.That(player.IsGrounded, Is.True, "Player must settle on the ground first.");

            var budget = JumpBudget.For(player);
            Assert.That(
                budget.Apex,
                Is.EqualTo(Phase1Bootstrap.TargetJumpApex).Within(0.01f),
                "Declared budget must match the phase design target."
            );

            var measuredApex = 0f;
            yield return MeasureJump(player, 0f, apex => measuredApex = apex, _ => { });

            Assert.That(
                measuredApex,
                Is.EqualTo(budget.Apex).Within(budget.Apex * Tolerance),
                $"Measured apex {measuredApex:F3} m must match the declared budget ({budget})."
            );
        }

        [UnityTest]
        public IEnumerator JumpBudget_MatchesMeasuredHorizontalReach()
        {
            CreateGround();
            var player = CreatePhasePlayer(new Vector2(-6f, 0.5f));

            yield return WaitForGrounded(player);

            var budget = JumpBudget.For(player);
            var measuredReach = 0f;
            yield return MeasureJump(player, 1f, _ => { }, reach => measuredReach = reach);

            Assert.That(
                measuredReach,
                Is.EqualTo(budget.Reach).Within(budget.Reach * Tolerance),
                $"Measured reach {measuredReach:F3} m must match the declared budget ({budget})."
            );
        }

        [UnityTest]
        public IEnumerator JumpBudget_MeasuredApexStaysBelowTheDeclaredSafeStepPlusMargin()
        {
            CreateGround();
            var player = CreatePhasePlayer(new Vector2(0f, 0.5f));

            yield return WaitForGrounded(player);

            var budget = JumpBudget.For(player);
            var measuredApex = 0f;
            yield return MeasureJump(player, 0f, apex => measuredApex = apex, _ => { });

            // O degrau seguro precisa ser efetivamente vencível, e um degrau acima do
            // ápice precisa ser efetivamente intransponível. É isso que garante que a
            // geometria calibrada contra o orçamento se comporte como o projeto diz.
            Assert.That(
                measuredApex,
                Is.GreaterThan(budget.SafeStep),
                "A safe step must be clearable by a real jump."
            );
            Assert.That(
                measuredApex,
                Is.LessThan(budget.Apex + 0.05f),
                "A jump must not exceed the declared apex."
            );
        }

        /// <summary>
        /// Executa um pulo completo e devolve o ápice e o alcance horizontal medidos
        /// entre a decolagem e o retorno à altura inicial.
        /// </summary>
        private static IEnumerator MeasureJump(
            PlayerMovement player,
            float horizontalInput,
            System.Action<float> onApex,
            System.Action<float> onReach
        )
        {
            var transform = player.transform;
            var startY = transform.position.y;
            var startX = transform.position.x;

            player.SetHorizontalInput(horizontalInput);
            player.RequestJump();

            var peakY = startY;
            var hasLeftGround = false;
            var reach = 0f;

            for (var frame = 0; frame < MaximumFrames; frame++)
            {
                yield return new WaitForFixedUpdate();

                var position = transform.position;
                peakY = Mathf.Max(peakY, position.y);

                if (!hasLeftGround)
                {
                    hasLeftGround = position.y > startY + 0.05f;
                    continue;
                }

                // Retornou à altura de partida: fim do arco.
                if (position.y <= startY + 0.01f)
                {
                    reach = Mathf.Abs(position.x - startX);
                    break;
                }
            }

            player.SetHorizontalInput(0f);
            onApex(peakY - startY);
            onReach(reach);
        }

        private void CreateGround()
        {
            var ground = new GameObject("Ground");
            ground.layer = GroundLayer;
            ground.transform.position = new Vector2(0f, -1f);

            var collider = ground.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(60f, 1f);
            _createdObjects.Add(ground);
        }

        /// <summary>Jogador com a mesma física que <see cref="Phase1Bootstrap"/> aplica.</summary>
        private PlayerMovement CreatePhasePlayer(Vector2 position)
        {
            var player = new GameObject("Jump Budget Test Player");
            player.transform.position = position;
            player.SetActive(false);

            var rigidbody = player.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = JumpBudget.GravityScaleForApex(
                Physics2D.gravity.y,
                12f,
                Phase1Bootstrap.TargetJumpApex,
                Time.fixedDeltaTime
            );
            rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = player.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            player.AddComponent<PlayerGroundCheck>();
            var movement = player.AddComponent<PlayerMovement>();
            player.SetActive(true);
            _createdObjects.Add(player);
            return movement;
        }

        private static IEnumerator WaitForGrounded(PlayerMovement player)
        {
            for (var frame = 0; frame < MaximumFrames; frame++)
            {
                yield return new WaitForFixedUpdate();

                if (player.IsGrounded)
                {
                    yield break;
                }
            }
        }
    }
}
