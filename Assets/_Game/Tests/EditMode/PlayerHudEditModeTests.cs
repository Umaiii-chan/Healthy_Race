using HealthyRace.Collectibles;
using HealthyRace.UI;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Tests.EditMode
{
    public sealed class PlayerHudEditModeTests
    {
        private GameObject _canvasObject;
        private GameObject _playerObject;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _playerObject = new GameObject("Player");
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }

            if (_canvasObject != null)
            {
                Object.DestroyImmediate(_canvasObject);
            }
        }

        [Test]
        public void PlayerHUD_Initialize_ConfiguresLabelsAndBindings()
        {
            var wb = _playerObject.AddComponent<WellbeingComponent>();
            wb.Initialize(80);
            var fat = _playerObject.AddComponent<FatigueComponent>();

            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(_canvasObject.transform, false);
            var hud = hudGo.AddComponent<PlayerHUD>();

            hud.Initialize(
                _canvasObject.transform,
                "P1",
                Color.cyan,
                wb,
                fat,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -20f)
            );

            Assert.That(hud.PlayerLabel, Is.EqualTo("P1"));
            Assert.That(hud.Score, Is.EqualTo(0));
            Assert.That(hud.HealthyFoodCount, Is.EqualTo(0));
            Assert.That(hud.UltraProcessedFoodCount, Is.EqualTo(0));
            Assert.That(hud.Wellbeing, Is.SameAs(wb));
            Assert.That(hud.Fatigue, Is.SameAs(fat));

            var texts = hudGo.GetComponentsInChildren<Text>(true);
            var labelText = System.Array.Find(texts, t => t.name == "PlayerLabel");
            var valueText = System.Array.Find(texts, t => t.name == "WellbeingValue");
            var scoreText = System.Array.Find(texts, t => t.name == "ScoreText");

            Assert.That(labelText, Is.Not.Null);
            Assert.That(labelText.text, Is.EqualTo("P1"));
            Assert.That(valueText, Is.Not.Null);
            Assert.That(valueText.text, Does.Contain("80 / 100"));
            Assert.That(scoreText, Is.Not.Null);
            Assert.That(scoreText.text, Does.Contain("0"));
        }

        [Test]
        public void PlayerHUD_WellbeingChanged_UpdatesValueAndFillAmount()
        {
            var wb = _playerObject.AddComponent<WellbeingComponent>();
            wb.Initialize(100);

            var hudGo = new GameObject("HUD", typeof(RectTransform));
            var hud = hudGo.AddComponent<PlayerHUD>();
            hud.Initialize(
                _canvasObject.transform,
                "P1",
                Color.cyan,
                wb,
                null,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero
            );

            wb.SetValue(65);

            var texts = hudGo.GetComponentsInChildren<Text>(true);
            var valueText = System.Array.Find(texts, t => t.name == "WellbeingValue");
            Assert.That(valueText.text, Does.Contain("65 / 100"));

            var images = hudGo.GetComponentsInChildren<Image>(true);
            var fillImage = System.Array.Find(images, i => i.name == "WellbeingFill");
            Assert.That(fillImage, Is.Not.Null);
            Assert.That(fillImage.fillAmount, Is.EqualTo(0.65f).Within(0.01f));
        }

        [Test]
        public void PlayerHUD_WellbeingBandChanged_UpdatesBandTextAndColor()
        {
            var wb = _playerObject.AddComponent<WellbeingComponent>();
            var tiredBand = WellbeingBandData.Create(
                "tired",
                "VITA cansado",
                26,
                50,
                characterColor: Color.yellow
            );
            var healthyBand = WellbeingBandData.Create(
                "healthy",
                "VITA saudável",
                51,
                100,
                characterColor: Color.green
            );

            try
            {
                wb.Initialize(100, new[] { tiredBand, healthyBand });

                var hudGo = new GameObject("HUD", typeof(RectTransform));
                var hud = hudGo.AddComponent<PlayerHUD>();
                hud.Initialize(
                    _canvasObject.transform,
                    "P1",
                    Color.cyan,
                    wb,
                    null,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero
                );

                wb.SetValue(40);

                var texts = hudGo.GetComponentsInChildren<Text>(true);
                var bandText = System.Array.Find(texts, t => t.name == "WellbeingBand");
                Assert.That(bandText.text, Does.Contain("VITA cansado"));

                var images = hudGo.GetComponentsInChildren<Image>(true);
                var fillImage = System.Array.Find(images, i => i.name == "WellbeingFill");
                Assert.That(fillImage.color, Is.EqualTo(Color.yellow));
            }
            finally
            {
                Object.DestroyImmediate(tiredBand);
                Object.DestroyImmediate(healthyBand);
            }
        }

        [Test]
        public void PlayerHUD_FatigueChanges_UpdateStateAndEpisodes()
        {
            var wb = _playerObject.AddComponent<WellbeingComponent>();
            wb.Initialize(100);
            var fat = _playerObject.AddComponent<FatigueComponent>();
            fat.EnsureInitialized();

            var hudGo = new GameObject("HUD", typeof(RectTransform));
            var hud = hudGo.AddComponent<PlayerHUD>();
            hud.Initialize(
                _canvasObject.transform,
                "P1",
                Color.cyan,
                wb,
                fat,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero
            );

            var texts = hudGo.GetComponentsInChildren<Text>(true);
            var stateText = System.Array.Find(texts, t => t.name == "FatigueState");
            var episodeText = System.Array.Find(texts, t => t.name == "FatigueEpisodes");

            Assert.That(stateText.text, Does.Contain("Normal"));
            Assert.That(episodeText.text, Does.Contain("0"));

            // Trigger fatigue via wellbeing drop below threshold
            wb.SetValue(10);

            Assert.That(stateText.text, Does.Contain("EM FADIGA!"));
            Assert.That(episodeText.text, Does.Contain("1"));
        }

        [Test]
        public void PlayerHUD_ScoreFoodAndProgress_UpdateCorrectly()
        {
            var hudGo = new GameObject("HUD", typeof(RectTransform));
            var hud = hudGo.AddComponent<PlayerHUD>();
            hud.Initialize(
                _canvasObject.transform,
                "P1",
                Color.cyan,
                null,
                null,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero
            );

            hud.UpdateScore(150);
            Assert.That(hud.Score, Is.EqualTo(150));

            hud.AddScore(50);
            Assert.That(hud.Score, Is.EqualTo(200));

            var healthy = FoodData.Create("apple", "Apple", FoodCategory.Healthy, 100, 10);
            var ultra = FoodData.Create("burger", "Burger", FoodCategory.UltraProcessed, 0, -18);

            try
            {
                hud.RegisterFoodCollected(healthy);
                hud.RegisterFoodCollected(ultra);

                Assert.That(hud.HealthyFoodCount, Is.EqualTo(1));
                Assert.That(hud.UltraProcessedFoodCount, Is.EqualTo(1));
                Assert.That(hud.TotalFoodCount, Is.EqualTo(2));

                hud.UpdateProgress(0.65f);
                Assert.That(hud.Progress, Is.EqualTo(0.65f).Within(0.001f));

                var texts = hudGo.GetComponentsInChildren<Text>(true);
                var scoreText = System.Array.Find(texts, t => t.name == "ScoreText");
                var foodText = System.Array.Find(texts, t => t.name == "FoodCount");
                var progressText = System.Array.Find(texts, t => t.name == "ProgressText");

                Assert.That(scoreText.text, Does.Contain("200"));
                Assert.That(foodText.text, Does.Contain("2"));
                Assert.That(progressText.text, Does.Contain("65%"));
            }
            finally
            {
                Object.DestroyImmediate(healthy);
                Object.DestroyImmediate(ultra);
            }
        }

        [Test]
        public void PlayerHUD_Unbind_ClearsSubscriptionsCleanly()
        {
            var wb = _playerObject.AddComponent<WellbeingComponent>();
            wb.Initialize(100);

            var hudGo = new GameObject("HUD", typeof(RectTransform));
            var hud = hudGo.AddComponent<PlayerHUD>();
            hud.Initialize(
                _canvasObject.transform,
                "P1",
                Color.cyan,
                wb,
                null,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero
            );

            hud.Unbind();
            Assert.That(hud.Wellbeing, Is.Null);

            // Setting value after unbind should not throw
            Assert.DoesNotThrow(() => wb.SetValue(50));

            var texts = hudGo.GetComponentsInChildren<Text>(true);
            var valueText = System.Array.Find(texts, t => t.name == "WellbeingValue");
            // Value text should remain at 100 because it was unbound
            Assert.That(valueText.text, Does.Contain("100 / 100"));
        }

        [Test]
        public void MatchHUD_TimerAndFinalWindow_UpdatesDisplayCorrectly()
        {
            var matchGo = new GameObject("MatchHUD", typeof(RectTransform));
            var matchHud = matchGo.AddComponent<MatchHUD>();
            matchHud.Initialize(_canvasObject.transform, Vector2.zero);

            matchHud.UpdateTimer(135f);
            Assert.That(matchHud.CurrentTime, Is.EqualTo(135f));
            Assert.That(matchHud.FormattedTime, Is.EqualTo("02:15"));

            var texts = matchGo.GetComponentsInChildren<Text>(true);
            var timerText = System.Array.Find(texts, t => t.name == "TimerText");
            Assert.That(timerText.text, Does.Contain("02:15"));

            var finalWindowText = System.Array.Find(texts, t => t.name == "FinalWindowText");
            Assert.That(finalWindowText.gameObject.activeSelf, Is.False);

            matchHud.SetFinalWindow(true, 18.5f);
            Assert.That(matchHud.IsFinalWindowActive, Is.True);
            Assert.That(finalWindowText.gameObject.activeSelf, Is.True);
            Assert.That(finalWindowText.text, Does.Contain("19s"));

            matchHud.SetFinalWindow(false);
            Assert.That(matchHud.IsFinalWindowActive, Is.False);
            Assert.That(finalWindowText.gameObject.activeSelf, Is.False);
        }
    }
}
