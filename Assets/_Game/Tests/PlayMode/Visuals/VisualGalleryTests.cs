#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using HealthyRace.Visuals;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HealthyRace.Tests.Visuals
{
    public sealed class VisualGalleryTests
    {
        private Scene _gallery;

        [UnityTest]
        public IEnumerator Gallery_AnimatesCollectsNavigatesAndCleansFeedback()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Game/Scenes/Test/VisualTest.unity",
                new LoadSceneParameters(LoadSceneMode.Additive)
            );
            _gallery = SceneManager.GetSceneByName("VisualTest");
            yield return null;
            var roots = _gallery.GetRootGameObjects();
            var players = roots
                .SelectMany(r => r.GetComponentsInChildren<PlayerPresentation>(true))
                .ToArray();
            Assert.That(players.Length, Is.EqualTo(10));
            Assert.That(players.Count(p => p.PlayerIndex == 1), Is.EqualTo(5));
            var player = players[0];
            player.Play(VisualAction.Move);
            yield return new WaitForSeconds(0.2f);
            Assert.That(
                player.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Move"),
                Is.True
            );
            var food = roots.SelectMany(r => r.GetComponentsInChildren<FoodPresentation>()).First();
            food.Pickup();
            food.Pickup();
            yield return new WaitForSeconds(0.25f);
            Assert.That(food.GetComponentInChildren<SpriteRenderer>().enabled, Is.False);
            food.ResetPresentation();
            Assert.That(food.GetComponentInChildren<SpriteRenderer>().enabled, Is.True);
            var screen = roots
                .SelectMany(r => r.GetComponentsInChildren<RaceScreenView>())
                .Single();
            screen.Show(VisualScreen.MainMenu);
            var play = screen.GetComponentsInChildren<Button>().Single(b => b.name == "Jogar");
            play.onClick.Invoke();
            Assert.That(screen.Screen, Is.EqualTo(VisualScreen.Gameplay));
            screen.Show(VisualScreen.Audio);
            screen.GetComponentInChildren<Toggle>().isOn = true;
            Assert.That(players.All(p => p.ReduceFlashes), Is.True);
            yield return new WaitForSeconds(1f);
            Assert.That(
                Object.FindObjectsByType<VisualFeedback>(FindObjectsSortMode.None),
                Is.Empty
            );
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_gallery.IsValid() && _gallery.isLoaded)
                yield return SceneManager.UnloadSceneAsync(_gallery);
        }
    }
}
#endif
