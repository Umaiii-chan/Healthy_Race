using System.Collections.Generic;
using HealthyRace.Level;
using HealthyRace.Player;
using HealthyRace.UI;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class MatchCompletionTests
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

        [Test]
        public void MatchCompletion_FirstFinisher_FreezesPlayerAndStartsFinalCountdown()
        {
            var setup = CreateMatchSetup();

            setup.FinishLine.RecordFinish(1);

            Assert.That(
                setup.Controller.State,
                Is.EqualTo(MatchCompletionState.FinalCountdown),
                "Controller should be in FinalCountdown state."
            );
            Assert.That(
                setup.Controller.FirstFinisherIndex,
                Is.EqualTo(1),
                "First finisher should be Player 1."
            );
            Assert.That(
                setup.Controller.FinalWindowTimeRemaining,
                Is.EqualTo(20f).Within(0.01f),
                "Final countdown window should start at 20 seconds."
            );
            Assert.That(
                setup.P1Movement.IsMovementEnabled,
                Is.False,
                "First finisher's movement should be disabled."
            );
        }

        [Test]
        public void MatchCompletion_SecondFinisherInTime_CompletesMatchWithoutDnf()
        {
            var setup = CreateMatchSetup();

            setup.FinishLine.RecordFinish(1);
            setup.FinishLine.RecordFinish(2);

            Assert.That(
                setup.Controller.State,
                Is.EqualTo(MatchCompletionState.Concluded),
                "Match should be concluded."
            );

            var results = setup.Controller.FinalResults;
            Assert.That(results.WinnerPlayerIndex, Is.EqualTo(1), "Player 1 should be the winner.");
            Assert.That(results.Player1.Placement, Is.EqualTo(1));
            Assert.That(results.Player1.IsDnf, Is.False);
            Assert.That(results.Player2.Placement, Is.EqualTo(2));
            Assert.That(
                results.Player2.IsDnf,
                Is.False,
                "Player 2 finished in time and should not have DNF."
            );
            Assert.That(results.HasDnf, Is.False);
        }

        [Test]
        public void MatchCompletion_Timeout_AppliesDnfPenaltyAndConcludes()
        {
            var setup = CreateMatchSetup();
            var p2InitialWb = setup.P2Wellbeing.CurrentValue;

            setup.FinishLine.RecordFinish(1);
            setup.Controller.HandleWindowExpired();

            Assert.That(
                setup.Controller.State,
                Is.EqualTo(MatchCompletionState.Concluded),
                "Match should conclude on timeout."
            );

            var results = setup.Controller.FinalResults;
            Assert.That(results.Player2.IsDnf, Is.True, "Player 2 should be marked DNF.");
            Assert.That(
                setup.P2Wellbeing.CurrentValue,
                Is.EqualTo(p2InitialWb - setup.Controller.DnfPenalty),
                "Player 2 should receive DNF wellbeing penalty."
            );
            Assert.That(results.WinnerPlayerIndex, Is.EqualTo(1));
            Assert.That(results.HasDnf, Is.True);
        }

        [Test]
        public void MatchCompletion_CompileResults_HandlesP2Winner()
        {
            var setup = CreateMatchSetup();

            // P2 finishes first
            setup.FinishLine.RecordFinish(2);
            setup.FinishLine.RecordFinish(1);

            var results = setup.Controller.FinalResults;
            Assert.That(results.WinnerPlayerIndex, Is.EqualTo(2));
            Assert.That(results.Player2.Placement, Is.EqualTo(1));
            Assert.That(results.Player1.Placement, Is.EqualTo(2));
        }

        private MatchSetup CreateMatchSetup()
        {
            var flObj = new GameObject("FinishLine");
            var flCol = flObj.AddComponent<BoxCollider2D>();
            flCol.isTrigger = true;
            var finishLine = flObj.AddComponent<FinishLine>();
            _createdObjects.Add(flObj);

            var p1 = CreatePlayerObject("P1");
            var p2 = CreatePlayerObject("P2");

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
                resultsPanel: null,
                finalWindowDuration: 20f,
                dnfPenalty: 20
            );

            return new MatchSetup
            {
                FinishLine = finishLine,
                Controller = controller,
                P1Movement = p1.Movement,
                P2Movement = p2.Movement,
                P1Wellbeing = p1.Wellbeing,
                P2Wellbeing = p2.Wellbeing,
            };
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

        private struct MatchSetup
        {
            public FinishLine FinishLine;
            public MatchCompletionController Controller;
            public PlayerMovement P1Movement;
            public PlayerMovement P2Movement;
            public WellbeingComponent P1Wellbeing;
            public WellbeingComponent P2Wellbeing;
        }
    }
}
