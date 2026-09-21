using System.Collections;
using System.Collections.Generic;
using HealthyRace.Collectibles;
using HealthyRace.Level;
using HealthyRace.Player;
using HealthyRace.Scoring;
using HealthyRace.UI;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class ScorePlayModeTests
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

        [UnityTest]
        public IEnumerator MatchCompletion_WithHudsAndPuzzles_CalculatesScoreAndDisplaysResultsPanel()
        {
            var canvasObj = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler)
            );
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _createdObjects.Add(canvasObj);

            var hudObj1 = new GameObject("Player1HUD", typeof(RectTransform));
            var p1Hud = hudObj1.AddComponent<PlayerHUD>();
            _createdObjects.Add(hudObj1);

            var hudObj2 = new GameObject("Player2HUD", typeof(RectTransform));
            var p2Hud = hudObj2.AddComponent<PlayerHUD>();
            _createdObjects.Add(hudObj2);

            var resultsPanelObj = new GameObject("ResultsPanel", typeof(RectTransform));
            var resultsPanel = resultsPanelObj.AddComponent<MatchResultsPanel>();
            resultsPanel.Initialize(canvas.transform);
            _createdObjects.Add(resultsPanelObj);

            var p1 = CreatePlayerObject("P1");
            var p2 = CreatePlayerObject("P2");

            p1Hud.Initialize(
                canvas.transform,
                "P1",
                Color.blue,
                p1.Wellbeing,
                null,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero
            );
            p2Hud.Initialize(
                canvas.transform,
                "P2",
                Color.green,
                p2.Wellbeing,
                null,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero
            );

            var foodData = FoodData.Create("apple", "Apple", FoodCategory.Healthy, 100, 10);
            p1Hud.RegisterFoodCollected(foodData);
            p1Hud.AddScore(foodData.BasePoints);

            var flObj = new GameObject("FinishLine");
            var flCol = flObj.AddComponent<BoxCollider2D>();
            flCol.isTrigger = true;
            var finishLine = flObj.AddComponent<FinishLine>();
            _createdObjects.Add(flObj);

            var ctrlObj = new GameObject("MatchCompletionController");
            var controller = ctrlObj.AddComponent<MatchCompletionController>();
            _createdObjects.Add(ctrlObj);

            controller.Configure(
                finishLine: finishLine,
                matchHud: null,
                playerOne: p1.GameObject,
                playerTwo: p2.GameObject,
                p1Wb: p1.Wellbeing,
                p2Wb: p2.Wellbeing,
                resultsPanel: resultsPanel,
                finalWindowDuration: 20f,
                dnfPenalty: 20,
                p1Hud: p1Hud,
                p2Hud: p2Hud,
                finalPuzzle: null,
                scoreConfig: null
            );

            yield return null;

            finishLine.RecordFinish(1);
            finishLine.RecordFinish(2);

            yield return null;

            Assert.That(
                controller.State,
                Is.EqualTo(MatchCompletionState.Concluded),
                "Match should be concluded."
            );
            Assert.That(
                controller.FinalResults.ScoreResult.Player1.FinalScore,
                Is.GreaterThan(0),
                "Player 1 should have positive score."
            );
            Assert.That(
                controller.FinalResults.ScoreResult.Player1.HealthyPoints,
                Is.EqualTo(100),
                "Player 1 should have 100 healthy points from apple."
            );
            Assert.That(resultsPanel.IsVisible, Is.True, "Results panel should be visible.");
        }

        [UnityTest]
        public IEnumerator MatchCompletion_Timeout_CalculatesDnfPenaltyInPlayMode()
        {
            var canvasObj = new GameObject("Canvas", typeof(Canvas));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _createdObjects.Add(canvasObj);

            var resultsPanelObj = new GameObject("ResultsPanel", typeof(RectTransform));
            var resultsPanel = resultsPanelObj.AddComponent<MatchResultsPanel>();
            resultsPanel.Initialize(canvas.transform);
            _createdObjects.Add(resultsPanelObj);

            var p1 = CreatePlayerObject("P1");
            var p2 = CreatePlayerObject("P2");

            var flObj = new GameObject("FinishLine");
            var flCol = flObj.AddComponent<BoxCollider2D>();
            flCol.isTrigger = true;
            var finishLine = flObj.AddComponent<FinishLine>();
            _createdObjects.Add(flObj);

            var ctrlObj = new GameObject("MatchCompletionController");
            var controller = ctrlObj.AddComponent<MatchCompletionController>();
            _createdObjects.Add(ctrlObj);

            controller.Configure(
                finishLine: finishLine,
                matchHud: null,
                playerOne: p1.GameObject,
                playerTwo: p2.GameObject,
                p1Wb: p1.Wellbeing,
                p2Wb: p2.Wellbeing,
                resultsPanel: resultsPanel,
                finalWindowDuration: 0.1f,
                dnfPenalty: 20
            );

            yield return null;

            finishLine.RecordFinish(1);
            controller.HandleWindowExpired();

            yield return null;

            Assert.That(
                controller.FinalResults.Player2.IsDnf,
                Is.True,
                "Player 2 should be marked DNF."
            );
            Assert.That(
                controller.FinalResults.ScoreResult.Player2.DnfPenalty,
                Is.EqualTo(500),
                "Player 2 should receive 500 points DNF penalty."
            );
            Assert.That(
                controller.FinalResults.ScoreResult.WinnerPlayerIndex,
                Is.EqualTo(1),
                "Player 1 should be the winner."
            );
        }

        private PlayerSetup CreatePlayerObject(string name)
        {
            var obj = new GameObject(name);
            obj.AddComponent<Rigidbody2D>();
            obj.AddComponent<BoxCollider2D>();
            obj.AddComponent<PlayerGroundCheck>();
            var mov = obj.AddComponent<PlayerMovement>();
            var wb = obj.AddComponent<WellbeingComponent>();
            wb.Initialize(100);

            _createdObjects.Add(obj);
            return new PlayerSetup
            {
                GameObject = obj,
                Movement = mov,
                Wellbeing = wb,
            };
        }

        private struct PlayerSetup
        {
            public GameObject GameObject;
            public PlayerMovement Movement;
            public WellbeingComponent Wellbeing;
        }
    }
}
