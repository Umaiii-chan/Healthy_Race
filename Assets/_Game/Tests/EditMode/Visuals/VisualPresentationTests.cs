using System.Linq;
using HealthyRace.Visuals;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Tests.Visuals
{
    public sealed class VisualPresentationTests
    {
        private const string Root = "Assets/_Game/";

        [TestCase(100, WellbeingAppearance.Healthy)]
        [TestCase(76, WellbeingAppearance.Healthy)]
        [TestCase(75, WellbeingAppearance.Stable)]
        [TestCase(51, WellbeingAppearance.Stable)]
        [TestCase(50, WellbeingAppearance.Tired)]
        [TestCase(26, WellbeingAppearance.Tired)]
        [TestCase(25, WellbeingAppearance.Critical)]
        [TestCase(1, WellbeingAppearance.Critical)]
        [TestCase(0, WellbeingAppearance.Salo)]
        public void SnapshotAppearance_UsesDocumentedVisualBands(
            int value,
            WellbeingAppearance expected
        )
        {
            Assert.That(VisualCatalog.AppearanceFor(value), Is.EqualTo(expected));
        }

        [TestCase(0, "00:00")]
        [TestCase(65.9f, "01:05")]
        [TestCase(-5, "00:00")]
        [TestCase(float.NaN, "--:--")]
        [TestCase(float.PositiveInfinity, "--:--")]
        public void Clock_HandlesBoundaryAndUnavailableValues(float value, string expected)
        {
            Assert.That(VisualUiFormat.Clock(value), Is.EqualTo(expected));
        }

        [Test]
        public void FinalWindow_RoundsUpToAvoidPrematureZero()
        {
            Assert.That(VisualUiFormat.FinalWindow(0.1f), Is.EqualTo("JANELA FINAL  01s"));
        }

        [Test]
        public void Result_UsesProvidedScoreAndMarksUnfinished()
        {
            var text = VisualUiFormat.Result(
                new PlayerResultData
                {
                    Player = new PlayerHudData { Score = -1234 },
                    Finished = false,
                }
            );
            Assert.That(text, Does.Contain("-1.234"));
            Assert.That(text, Does.Contain("Não concluiu"));
        }

        [Test]
        public void Catalog_HasAllUniqueFoodSpritesAndFiveDistinctForms()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VisualCatalog>(
                Root + "Art/VisualAtlas/SO_VisualCatalog.asset"
            );
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Foods.Length, Is.EqualTo(18));
            Assert.That(catalog.Foods.All(s => s != null), Is.True);
            Assert.That(catalog.Foods.Distinct().Count(), Is.EqualTo(18));
            Assert.That(catalog.Characters.Distinct().Count(), Is.EqualTo(5));
            Assert.That(catalog.IdentityColor(1), Is.Not.EqualTo(catalog.IdentityColor(2)));
            Assert.That(catalog.Food("missing"), Is.Null);
            foreach (
                var sprite in new[]
                {
                    catalog.Particle,
                    catalog.Heart,
                    catalog.Droplet,
                    catalog.Ring,
                }
            )
            {
                Assert.That(sprite, Is.Not.Null);
                Assert.That(
                    sprite.texture,
                    Is.Not.Null,
                    "Procedural sprite texture must survive serialization."
                );
            }
            var badge = AssetDatabase.LoadAssetAtPath<Sprite>(
                Root + "Art/UI/Visual/SP_Badge.asset"
            );
            Assert.That(badge.texture, Is.Not.Null);
        }

        [Test]
        public void Animator_ContainsEveryActionAndNoGameplayEvents()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                Root + "Animations/Visual/AC_VIS_Player.controller"
            );
            Assert.That(controller, Is.Not.Null);
            var states = controller.layers[0].stateMachine.states;
            Assert.That(states.Length, Is.EqualTo(12));
            foreach (var state in states)
            {
                var clip = state.state.motion as AnimationClip;
                Assert.That(clip, Is.Not.Null);
                Assert.That(clip.length, Is.GreaterThan(0));
                Assert.That(AnimationUtility.GetAnimationEvents(clip), Is.Empty);
            }
        }

        [Test]
        public void VisualScene_HasNoMissingScriptsOrPhysicsAndHasAllGalleryContent()
        {
            var scene = EditorSceneManager.OpenPreviewScene(Root + "Scenes/Test/VisualTest.unity");
            try
            {
                var roots = scene.GetRootGameObjects();
                var objects = roots
                    .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .ToArray();
                Assert.That(
                    objects.All(t =>
                        GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0
                    ),
                    Is.True
                );
                Assert.That(
                    roots
                        .SelectMany(r => r.GetComponentsInChildren<FoodPresentation>(true))
                        .Count(),
                    Is.EqualTo(18)
                );
                Assert.That(
                    roots
                        .SelectMany(r => r.GetComponentsInChildren<PlayerPresentation>(true))
                        .Count(),
                    Is.EqualTo(10)
                );
                Assert.That(
                    roots.SelectMany(r => r.GetComponentsInChildren<Rigidbody2D>(true)),
                    Is.Empty
                );
                Assert.That(
                    roots.SelectMany(r => r.GetComponentsInChildren<Collider2D>(true)),
                    Is.Empty
                );
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void ScreenView_DisplaysOnlyRequestedScreenAndEmitsIntentOnce()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                Root + "Prefabs/Visual/PF_VIS_Screens.prefab"
            );
            var go = Object.Instantiate(prefab);
            try
            {
                var screen = go.GetComponent<RaceScreenView>();
                var calls = 0;
                screen.CommandRequested += command =>
                {
                    Assert.That(command, Is.EqualTo(VisualCommand.Play));
                    calls++;
                };
                screen.Request(VisualCommand.Play);
                Assert.That(calls, Is.EqualTo(1));
                screen.Show(VisualScreen.Pause);
                Assert.That(
                    go.transform.Cast<Transform>().Count(t => t.gameObject.activeSelf),
                    Is.EqualTo(1)
                );
                Assert.That(go.transform.Find("Pause").gameObject.activeSelf, Is.True);
                screen.RenderRace(65, 0.4f, null);
                Assert.That(
                    go.GetComponentsInChildren<Text>(true).Any(t => t.text == "01:05"),
                    Is.True
                );
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
