using System.Collections;
using System.Linq;
using HealthyRace.Core;
using HealthyRace.Level;
using HealthyRace.Player;
using HealthyRace.Visuals;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    /// <summary>O personagem articulado reagindo ao jogo de verdade, na Fase 1.</summary>
    public sealed class CharacterRigPlayModeTests
    {
        private Vector2 _originalGravity;

        [SetUp]
        public void SetUp()
        {
            _originalGravity = Physics2D.gravity;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameFlow.SetPaused(false);
            var active = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("CharacterRigTests_Empty_" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (active.IsValid() && active.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(active);
            }

            Checkpoint.ResetRegistry();
            Physics2D.gravity = _originalGravity;
        }

        private static IEnumerator LoadArena()
        {
            var loadOp = SceneManager.LoadSceneAsync(
                GameFlow.SceneForLevel(1),
                LoadSceneMode.Single
            );
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>
        /// Sem o PlayerController o teste comanda o PlayerMovement direto (o
        /// controlador reescreveria a entrada a cada quadro com o teclado parado).
        /// </summary>
        private static PlayerMovement TakeControl(GameObject player)
        {
            player.GetComponent<PlayerController>().enabled = false;
            return player.GetComponent<PlayerMovement>();
        }

        [UnityTest]
        public IEnumerator Presentation_DetectsPushingAgainstAWall()
        {
            yield return LoadArena();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var player = bootstrap.PlayerOne;
            var presentation = player.GetComponentInChildren<PlayerPresentation>();
            var movement = TakeControl(player);
            Assert.That(presentation.IsBlocked, Is.False);

            // encosta o jogador na moldura esquerda, no piso da largada
            var body = player.GetComponent<Rigidbody2D>();
            var start = new Vector2(1.5f, bootstrap.SpawnOne.y);
            body.linearVelocity = Vector2.zero;
            body.position = start;
            player.transform.position = start;
            yield return new WaitForSeconds(0.3f);
            Assert.That(movement.IsGrounded, Is.True, "O jogador precisa estar apoiado.");

            movement.SetHorizontalInput(-1f);
            yield return new WaitForSeconds(0.4f);

            Assert.That(presentation.IsBlocked, Is.True, "Andando contra a parede: empurrando.");
        }

        [UnityTest]
        public IEnumerator Presentation_JumpEmitsDustAtTheFeet()
        {
            yield return LoadArena();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var presentation = bootstrap.PlayerOne.GetComponentInChildren<PlayerPresentation>();

            presentation.Play(VisualAction.Jump);
            yield return null;

            var dust = Object
                .FindObjectsByType<VisualFeedback>(FindObjectsSortMode.None)
                .Where(effect => effect.name == "VFX_" + FeedbackKind.Dust)
                .ToArray();
            Assert.That(dust, Is.Not.Empty);
            Assert.That(
                dust[0].transform.position.y,
                Is.LessThan(bootstrap.PlayerOne.transform.position.y),
                "A poeira sai dos pés, não do centro do corpo."
            );
        }
    }
}
