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
    /// <summary>Zuri e Tom reagindo ao jogo de verdade, na Fase 1.</summary>
    public sealed class RunnerRigPlayModeTests
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
            var empty = SceneManager.CreateScene("RunnerRigTests_Empty_" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (active.IsValid() && active.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(active);
            }

            Checkpoint.ResetRegistry();
            Physics2D.gravity = _originalGravity;
        }

        private static IEnumerator LoadLevel()
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
        public IEnumerator Players_AreZuriAndTom_AndFaceTheMovementDirection()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var zuri = bootstrap.PlayerOne.GetComponentInChildren<RunnerRig>();
            var tom = bootstrap.PlayerTwo.GetComponentInChildren<RunnerRig>();
            Assert.That(zuri, Is.Not.Null, "o jogador 1 é a Zuri");
            Assert.That(tom, Is.Not.Null, "o jogador 2 é o Tom");
            Assert.That(zuri.FaceSprite.name, Does.StartWith("Runner_Zuri_"));
            Assert.That(tom.FaceSprite.name, Does.StartWith("Runner_Tom_"));
            Assert.That(
                bootstrap.PlayerOne.GetComponentInChildren<CharacterRig>(),
                Is.Null,
                "o broto saiu das fases"
            );

            var movement = TakeControl(bootstrap.PlayerOne);
            movement.SetHorizontalInput(-1f);
            yield return null;
            yield return null;
            Assert.That(zuri.Facing, Is.EqualTo(-1));

            movement.SetHorizontalInput(1f);
            yield return null;
            yield return null;
            Assert.That(zuri.Facing, Is.EqualTo(1));

            movement.SetHorizontalInput(0f);
            yield return null;
            yield return null;
            Assert.That(zuri.Facing, Is.EqualTo(1), "parado, mantém o último lado");
        }

        [UnityTest]
        public IEnumerator Limbs_AreDrawn_AndFollowTheRunAnimation()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var player = bootstrap.PlayerOne;
            var rig = player.GetComponentInChildren<RunnerRig>();
            var presentation = player.GetComponentInChildren<PlayerPresentation>();
            var animator = presentation.GetComponent<Animator>();

            Assert.That(rig.Limbs.Count, Is.EqualTo(4));
            foreach (var limb in rig.Limbs)
            {
                Assert.That(limb.Mesh, Is.Not.Null, limb.name + " sem malha");
                Assert.That(limb.Mesh.vertexCount, Is.EqualTo(HoseLimb.VerticesPerStrip * 2));
                // a malha liga a âncora ao alvo, em espaço de mundo
                var bounds = limb.GetComponent<MeshRenderer>().bounds;
                Assert.That(
                    bounds.SqrDistance(limb.Anchor.position),
                    Is.LessThan(0.01f),
                    limb.name
                );
                Assert.That(
                    bounds.SqrDistance(limb.Target.position),
                    Is.LessThan(0.01f),
                    limb.name
                );
            }

            // Aqui o PlayerController fica ligado: é ele que roda a máquina de estados que a
            // apresentação lê. Como ele zera a entrada a cada Update (teclado parado), o teste
            // a repõe a cada quadro, depois do Update e antes do LateUpdate.
            var movement = player.GetComponent<PlayerMovement>();
            var backFoot = rig.Targets[1];
            var frontFoot = rig.Targets[2];
            var restHeight = Mathf.Max(backFoot.localPosition.y, frontFoot.localPosition.y);
            var highest = restHeight;
            var running = 0f;
            var elapsed = 0f;
            while (elapsed < 2f && running < 0.7f)
            {
                yield return null;
                // vaivém curto: fica no chão da largada, sem chegar à bifurcação da fase
                movement.SetHorizontalInput(Mathf.FloorToInt(elapsed / 0.25f) % 2 == 0 ? 1f : -1f);
                elapsed += Time.deltaTime;
                if (presentation.Action != VisualAction.Move)
                {
                    continue;
                }

                running += Time.deltaTime;
                Assert.That(animator.GetInteger("Action"), Is.EqualTo((int)VisualAction.Move));
                highest = Mathf.Max(highest, backFoot.localPosition.y, frontFoot.localPosition.y);
            }

            Assert.That(running, Is.GreaterThan(0.5f), "o jogador correu");
            Assert.That(highest, Is.GreaterThan(restHeight + 0.1f), "na corrida o pé sai do chão");
        }

        [UnityTest]
        public IEnumerator Wellbeing_MovesTheHeadband_NotTheBodyOrTheSkin()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var player = bootstrap.PlayerOne;
            var rig = player.GetComponentInChildren<RunnerRig>();
            var head = rig
                .transform.Find("Rig/Body/Torso/Head/HeadSprite")
                .GetComponent<SpriteRenderer>();
            var skin = head.sprite;
            var color = head.color;
            float TipHeight() => (rig.TailA.TransformPoint(Vector3.left) - rig.TailA.position).y;

            var full = TipHeight();
            Assert.That(
                full,
                Is.GreaterThan(0.1f),
                "com Bem-Estar cheio a ponta da faixa aponta para cima"
            );

            player.GetComponent<WellbeingComponent>().SetValue(0);
            yield return null;
            Assert.That(TipHeight(), Is.LessThan(-0.1f), "com Bem-Estar zero a ponta cai");
            Assert.That(head.sprite, Is.SameAs(skin), "a pele não muda com o estado");
            Assert.That(head.color, Is.EqualTo(color));

            player.GetComponent<WellbeingComponent>().SetValue(50);
            yield return null;
            Assert.That(TipHeight(), Is.InRange(-0.6f, full - 0.05f));
        }

        [UnityTest]
        public IEnumerator Snack_ShowsTheRush_ThenTheCrash_OnTheRunner()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var player = bootstrap.PlayerOne;
            var rig = player.GetComponentInChildren<RunnerRig>();
            var presentation = player.GetComponentInChildren<PlayerPresentation>();
            var animator = presentation.GetComponent<Animator>();
            var movement = player.GetComponent<PlayerMovement>();

            // o lanche entra direto (a coleta em si é coberta pelos testes de comida): o que
            // se confere aqui é o corredor mostrando o pico e depois a queda
            movement.EatSnack(1f);
            Assert.That(movement.IsRushing, Is.True);

            yield return RunUntilMoving(movement, presentation, 1f);
            Assert.That(movement.IsRushing, Is.True);
            Assert.That(presentation.Action, Is.EqualTo(VisualAction.Move));
            Assert.That(rig.Face, Is.EqualTo(RunnerFace.Rush), "no pico, olho arregalado");
            Assert.That(animator.GetInteger("Action"), Is.EqualTo(RunnerRig.RushAction));

            // parado até o pico acabar; depois volta pelo chão por onde veio
            yield return new WaitForSeconds(movement.Snack.Remaining + 0.3f);
            Assert.That(movement.IsCrashing, Is.True);
            Assert.That(rig.Face, Is.EqualTo(RunnerFace.Sleepy), "na queda, sonolento");

            yield return RunUntilMoving(movement, presentation, -1f);
            Assert.That(movement.IsCrashing, Is.True);
            Assert.That(presentation.Action, Is.EqualTo(VisualAction.Move));
            Assert.That(rig.Face, Is.EqualTo(RunnerFace.Sleepy));
            Assert.That(animator.GetInteger("Action"), Is.EqualTo(RunnerRig.CrashAction));
        }

        /// <summary>
        /// Corre com o PlayerController ligado até a apresentação mostrar a corrida. A entrada
        /// é reposta a cada quadro porque o controlador a zera no Update com o teclado parado.
        /// </summary>
        private static IEnumerator RunUntilMoving(
            PlayerMovement movement,
            PlayerPresentation presentation,
            float input
        )
        {
            var elapsed = 0f;
            var moving = 0;
            // dois quadros seguidos em corrida: o segundo já passou pelo SyncRunner do rig
            while (elapsed < 1f && moving < 2)
            {
                yield return null;
                movement.SetHorizontalInput(input);
                elapsed += Time.deltaTime;
                moving = presentation.Action == VisualAction.Move ? moving + 1 : 0;
            }
        }

        [UnityTest]
        public IEnumerator Blink_ClosesTheEyes_AndReduceMotionKeepsThemOpen()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var presentation = bootstrap.PlayerOne.GetComponentInChildren<PlayerPresentation>();
            var rig = presentation.Runner;
            rig.SetFace(RunnerFace.Neutral);
            var open = rig.FaceSprite;

            rig.Blink();
            Assert.That(rig.IsBlinking, Is.True);
            Assert.That(rig.FaceSprite, Is.Not.SameAs(open), "olhos fechados na piscada");
            Assert.That(rig.Face, Is.EqualTo(RunnerFace.Neutral), "piscar não muda o rosto pedido");

            presentation.ReduceFlashes = true;
            yield return null;
            yield return null;
            Assert.That(rig.ReduceMotion, Is.True);
            Assert.That(
                rig.FaceSprite,
                Is.SameAs(open),
                "com movimento reduzido o personagem não pode ficar de olhos fechados"
            );
        }

        [UnityTest]
        public IEnumerator LooseRunner_WithoutAPlayer_StillHonoursReduceMotion()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();

            // rig solto, como numa galeria ou no menu: sem jogador nem movimento ligados
            var loose = Object.Instantiate(bootstrap.PlayerOneVisualPrefab);
            var presentation = loose.GetComponent<PlayerPresentation>();
            presentation.ReduceFlashes = true;
            yield return null;
            yield return null;

            Assert.That(presentation.Runner.ReduceMotion, Is.True);
            Assert.That(presentation.Runner.Limbs.All(l => l.Mesh != null), Is.True);
            Object.Destroy(loose);
        }
    }
}
