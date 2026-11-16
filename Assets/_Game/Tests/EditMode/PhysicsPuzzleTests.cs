using HealthyRace.Obstacles;
using HealthyRace.Player;
using HealthyRace.Puzzles;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class PhysicsPuzzleTests
    {
        [Test]
        public void Lever_StartsDeactivated_AndTogglesState()
        {
            var leverObj = new GameObject("TestLever", typeof(BoxCollider2D));
            try
            {
                var lever = leverObj.AddComponent<Lever>();
                Assert.That(lever.IsActivated, Is.False);

                var toggledStates = new System.Collections.Generic.List<bool>();
                lever.OnToggled += state => toggledStates.Add(state);

                lever.Toggle();
                Assert.That(lever.IsActivated, Is.True);
                Assert.That(toggledStates, Has.Count.EqualTo(1));
                Assert.That(toggledStates[0], Is.True);

                lever.Toggle();
                Assert.That(lever.IsActivated, Is.False);
                Assert.That(toggledStates, Has.Count.EqualTo(2));
                Assert.That(toggledStates[1], Is.False);

                lever.ResetState();
                Assert.That(lever.IsActivated, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(leverObj);
            }
        }

        [Test]
        public void PuzzleDoor_OpensAndCloses_UpdatingStateAndEvents()
        {
            var doorObj = new GameObject("TestDoor", typeof(BoxCollider2D));
            try
            {
                var door = doorObj.AddComponent<PuzzleDoor>();
                door.Configure(new Vector2(0f, 3f), 4f, instantMotion: true);

                Assert.That(door.IsOpen, Is.False);
                var openedCount = 0;
                var closedCount = 0;
                door.OnOpened += () => openedCount++;
                door.OnClosed += () => closedCount++;

                door.Open();
                Assert.That(door.IsOpen, Is.True);
                Assert.That(openedCount, Is.EqualTo(1));
                Assert.That(door.transform.position.y, Is.EqualTo(3f).Within(0.01f));

                door.Close();
                Assert.That(door.IsOpen, Is.False);
                Assert.That(closedCount, Is.EqualTo(1));
                Assert.That(door.transform.position.y, Is.EqualTo(0f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(doorObj);
            }
        }

        [Test]
        public void ActivatedPlatform_ConfiguresTargetAndTogglesActivation()
        {
            var platObj = new GameObject(
                "TestPlatform",
                typeof(BoxCollider2D),
                typeof(Rigidbody2D)
            );
            try
            {
                var plat = platObj.AddComponent<ActivatedPlatform>();
                plat.Configure(new Vector2(0f, 4f), 3f);

                Assert.That(plat.IsActive, Is.False);
                Assert.That(plat.TargetPosition.y, Is.EqualTo(4f).Within(0.01f));

                plat.Activate();
                Assert.That(plat.IsActive, Is.True);

                plat.Deactivate();
                Assert.That(plat.IsActive, Is.False);

                plat.ResetPlatform();
                Assert.That(plat.IsActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(platObj);
            }
        }
    }
}
