using System.Collections;
using System.Collections.Generic;
using HealthyRace.Collectibles;
using HealthyRace.UI;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class PlayerHudPlayModeTests
    {
        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }

            _createdObjects.Clear();
        }

        private GameObject CreateCanvas(Vector2 referenceResolution)
        {
            var canvasGo = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;

            _createdObjects.Add(canvasGo);
            return canvasGo;
        }

        private GameObject CreatePlayer(
            string name,
            int initialWellbeing,
            out WellbeingComponent wb,
            out FatigueComponent fat
        )
        {
            var go = new GameObject(name);
            wb = go.AddComponent<WellbeingComponent>();
            wb.Initialize(initialWellbeing);
            fat = go.AddComponent<FatigueComponent>();
            fat.EnsureInitialized();

            _createdObjects.Add(go);
            return go;
        }

        [UnityTest]
        public IEnumerator TwoPlayers_PlayerHUD_IsolatesP1AndP2_InRealTime()
        {
            var canvasGo = CreateCanvas(new Vector2(1280f, 720f));

            CreatePlayer("Player1", 80, out var p1Wb, out var p1Fat);
            CreatePlayer("Player2", 90, out var p2Wb, out var p2Fat);

            var p1HudGo = new GameObject("P1 HUD", typeof(RectTransform));
            var p1Hud = p1HudGo.AddComponent<PlayerHUD>();
            p1Hud.Initialize(
                canvasGo.transform,
                "P1",
                Color.cyan,
                p1Wb,
                p1Fat,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -20f)
            );
            _createdObjects.Add(p1HudGo);

            var p2HudGo = new GameObject("P2 HUD", typeof(RectTransform));
            var p2Hud = p2HudGo.AddComponent<PlayerHUD>();
            p2Hud.Initialize(
                canvasGo.transform,
                "P2",
                Color.magenta,
                p2Wb,
                p2Fat,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-20f, -20f)
            );
            _createdObjects.Add(p2HudGo);

            yield return null;

            var p1ValueText = System.Array.Find(
                p1HudGo.GetComponentsInChildren<Text>(true),
                t => t.name == "WellbeingValue"
            );
            var p2ValueText = System.Array.Find(
                p2HudGo.GetComponentsInChildren<Text>(true),
                t => t.name == "WellbeingValue"
            );

            Assert.That(p1ValueText.text, Does.Contain("80 / 100"));
            Assert.That(p2ValueText.text, Does.Contain("90 / 100"));

            // Alter P1 wellbeing only
            p1Wb.ApplyDelta(-30);
            yield return null;

            Assert.That(
                p1ValueText.text,
                Does.Contain("50 / 100"),
                "P1 HUD should reflect P1's delta."
            );
            Assert.That(
                p2ValueText.text,
                Does.Contain("90 / 100"),
                "P2 HUD must remain unaffected by P1's delta."
            );

            // Add score to P2 only
            p2Hud.AddScore(150);
            yield return null;

            Assert.That(p1Hud.Score, Is.EqualTo(0), "P1 score must remain 0.");
            Assert.That(p2Hud.Score, Is.EqualTo(150), "P2 score must be updated to 150.");

            // Trigger fatigue on P1
            p1Wb.SetValue(5);
            yield return null;

            var p1StateText = System.Array.Find(
                p1HudGo.GetComponentsInChildren<Text>(true),
                t => t.name == "FatigueState"
            );
            var p2StateText = System.Array.Find(
                p2HudGo.GetComponentsInChildren<Text>(true),
                t => t.name == "FatigueState"
            );

            Assert.That(
                p1StateText.text,
                Does.Contain("EM FADIGA!"),
                "P1 should be in fatigue state."
            );
            Assert.That(
                p2StateText.text,
                Does.Contain("Normal"),
                "P2 must remain in Normal fatigue state."
            );
        }

        [UnityTest]
        public IEnumerator HUD_Layout_At1280x720_HasNoOverlapBetweenP1AndP2()
        {
            var canvasGo = CreateCanvas(new Vector2(1280f, 720f));

            var p1HudGo = new GameObject("P1 HUD", typeof(RectTransform));
            var p1Hud = p1HudGo.AddComponent<PlayerHUD>();
            p1Hud.Initialize(
                canvasGo.transform,
                "P1",
                Color.cyan,
                null,
                null,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(20f, -20f)
            );
            _createdObjects.Add(p1HudGo);

            var matchHudGo = new GameObject("Match HUD", typeof(RectTransform));
            var matchHud = matchHudGo.AddComponent<MatchHUD>();
            matchHud.Initialize(canvasGo.transform, new Vector2(0f, -20f));
            _createdObjects.Add(matchHudGo);

            var p2HudGo = new GameObject("P2 HUD", typeof(RectTransform));
            var p2Hud = p2HudGo.AddComponent<PlayerHUD>();
            p2Hud.Initialize(
                canvasGo.transform,
                "P2",
                Color.magenta,
                null,
                null,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-20f, -20f)
            );
            _createdObjects.Add(p2HudGo);

            yield return null;

            var p1Rect = (RectTransform)p1HudGo.transform;
            var matchRect = (RectTransform)matchHudGo.transform;
            var p2Rect = (RectTransform)p2HudGo.transform;

            // In 1280x720 reference space:
            // P1: anchored (0, 1), pos (20, -20), size (280, 175) -> x spans 20 to 300
            // Match: anchored (0.5, 1), pos (0, -20), size (280, 64) -> x spans (640-140)=500 to (640+140)=780
            // P2: anchored (1, 1), pos (-20, -20), size (280, 175) -> x spans (1280-20-280)=980 to 1260

            float p1Right = p1Rect.anchoredPosition.x + p1Rect.rect.width;
            float matchLeft =
                640f + matchRect.anchoredPosition.x - (matchRect.rect.width * matchRect.pivot.x);
            float matchRight = matchLeft + matchRect.rect.width;
            float p2Left =
                1280f + p2Rect.anchoredPosition.x - (p2Rect.rect.width * (1f - p2Rect.pivot.x));

            Assert.That(p1Right, Is.LessThan(matchLeft), "P1 HUD must not overlap with Match HUD.");
            Assert.That(matchRight, Is.LessThan(p2Left), "Match HUD must not overlap with P2 HUD.");
            Assert.That(p1Right, Is.LessThan(p2Left), "P1 HUD must not overlap with P2 HUD.");

            float gapP1ToMatch = matchLeft - p1Right;
            float gapMatchToP2 = p2Left - matchRight;

            Assert.That(
                gapP1ToMatch,
                Is.GreaterThanOrEqualTo(150f),
                "Substantial margin between P1 and Match HUD."
            );
            Assert.That(
                gapMatchToP2,
                Is.GreaterThanOrEqualTo(150f),
                "Substantial margin between Match HUD and P2."
            );
        }
    }
}
