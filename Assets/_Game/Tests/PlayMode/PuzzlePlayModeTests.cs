using System.Collections;
using System.Collections.Generic;
using HealthyRace.Obstacles;
using HealthyRace.Player;
using HealthyRace.Puzzles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class PuzzlePlayModeTests
    {
        private const int GroundLayer = 6;
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
        public IEnumerator Player_ContactWithLever_TogglesState()
        {
            CreateGround(new Vector2(0f, -1f), new Vector2(20f, 1f));
            var lever = CreateLever(new Vector2(1f, 0f));
            var player = CreatePlayer(new Vector2(-0.5f, 0f));

            Assert.That(lever.IsActivated, Is.False, "Lever should start deactivated.");

            // Walk player into the lever
            player.SetHorizontalInput(1f);

            for (var frame = 0; frame < 60; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (lever.IsActivated)
                {
                    break;
                }
            }

            Assert.That(
                lever.IsActivated,
                Is.True,
                "Lever should activate when touched by player."
            );
        }

        [UnityTest]
        public IEnumerator PressureButton_PressedByBox_OpensPuzzleDoor()
        {
            CreateGround(new Vector2(0f, -1f), new Vector2(20f, 1f));
            var button = CreatePressureButton(new Vector2(0f, -0.45f));
            var door = CreateDoor(new Vector2(5f, 0f), new Vector2(0f, 3f), instant: true);
            var box = CreateBox(new Vector2(0f, 1.5f));

            var puzzleObj = new GameObject("PuzzleController");
            var puzzle = puzzleObj.AddComponent<CooperativePuzzleController>();
            _createdObjects.Add(puzzleObj);

            puzzle.Bind(
                button: button,
                lever: null,
                box: box,
                door: door,
                platform: null,
                resetSwitch: null,
                requireAll: false
            );

            Assert.That(door.IsOpen, Is.False, "Door should start closed.");

            // Wait for box to land on button
            for (var frame = 0; frame < 60; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(button.IsPressed, Is.True, "Button should be pressed by falling box.");
            Assert.That(door.IsOpen, Is.True, "Door should open when button is pressed.");
        }

        [UnityTest]
        public IEnumerator ActivatedPlatform_MovesTowardsTarget_WhenActivated()
        {
            var platform = CreatePlatform(new Vector2(0f, 0f), new Vector2(0f, 2f), 4f);

            Assert.That(platform.IsActive, Is.False);
            platform.Activate();
            Assert.That(platform.IsActive, Is.True);

            for (var frame = 0; frame < 30; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(
                platform.transform.position.y,
                Is.GreaterThan(0.5f),
                "Platform should have moved vertically towards target."
            );

            platform.Deactivate();

            for (var frame = 0; frame < 40; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(
                platform.transform.position.y,
                Is.LessThan(0.2f),
                "Platform should have returned towards origin."
            );
        }

        [UnityTest]
        public IEnumerator CooperativePuzzle_ResetRecoversCratePosition()
        {
            CreateGround(new Vector2(0f, -1f), new Vector2(20f, 1f));
            var box = CreateBox(new Vector2(2f, 0f));
            var lever = CreateLever(new Vector2(0f, 0f));
            var resetSwitch = CreateLever(new Vector2(-3f, 0f));
            var door = CreateDoor(new Vector2(5f, 0f), new Vector2(0f, 3f), instant: true);

            var puzzleObj = new GameObject("PuzzleController");
            var puzzle = puzzleObj.AddComponent<CooperativePuzzleController>();
            _createdObjects.Add(puzzleObj);

            puzzle.Bind(
                button: null,
                lever: lever,
                box: box,
                door: door,
                platform: null,
                resetSwitch: resetSwitch,
                requireAll: true
            );

            // Move box away (simulate fallen into pit)
            box.transform.position = new Vector2(50f, -20f);

            // Trigger reset
            puzzle.ResetPuzzle();

            Assert.That(
                box.transform.position.x,
                Is.EqualTo(2f).Within(0.01f),
                "Box position X should be restored to initial spawn."
            );
            Assert.That(
                box.transform.position.y,
                Is.EqualTo(0f).Within(0.01f),
                "Box position Y should be restored to initial spawn."
            );
            Assert.That(door.IsOpen, Is.False, "Door should remain closed after reset.");

            yield return null;
        }

        private GameObject CreateGround(Vector2 position, Vector2 size)
        {
            var ground = new GameObject("Ground");
            ground.layer = GroundLayer;
            ground.transform.position = position;

            var col = ground.AddComponent<BoxCollider2D>();
            col.size = size;

            _createdObjects.Add(ground);
            return ground;
        }

        private PushableBox CreateBox(Vector2 position)
        {
            var boxObj = new GameObject("PushableBox");
            boxObj.transform.position = position;

            var col = boxObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            var box = boxObj.AddComponent<PushableBox>();
            _createdObjects.Add(boxObj);
            return box;
        }

        private Lever CreateLever(Vector2 position)
        {
            var leverObj = new GameObject("Lever");
            leverObj.transform.position = position;

            var col = leverObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);

            var lever = leverObj.AddComponent<Lever>();
            _createdObjects.Add(leverObj);
            return lever;
        }

        private PuzzleDoor CreateDoor(Vector2 position, Vector2 openOffset, bool instant = false)
        {
            var doorObj = new GameObject("PuzzleDoor");
            doorObj.transform.position = position;

            var col = doorObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 3f);

            var door = doorObj.AddComponent<PuzzleDoor>();
            door.Configure(openOffset, 4f, instant);
            _createdObjects.Add(doorObj);
            return door;
        }

        private ActivatedPlatform CreatePlatform(
            Vector2 position,
            Vector2 targetOffset,
            float moveSpeed
        )
        {
            var platObj = new GameObject("ActivatedPlatform");
            platObj.transform.position = position;

            var rb = platObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = platObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);

            var platform = platObj.AddComponent<ActivatedPlatform>();
            platform.Configure(targetOffset, moveSpeed, returnWhenDeactivated: true);
            _createdObjects.Add(platObj);
            return platform;
        }

        private PressureButton CreatePressureButton(Vector2 position)
        {
            var buttonObj = new GameObject("PressureButton");
            buttonObj.transform.position = position;

            var col = buttonObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.5f, 0.4f);

            var button = buttonObj.AddComponent<PressureButton>();
            _createdObjects.Add(buttonObj);
            return button;
        }

        private PlayerMovement CreatePlayer(Vector2 position)
        {
            var player = new GameObject("TestPlayer");
            player.transform.position = position;
            player.SetActive(false);

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = player.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            player.AddComponent<PlayerGroundCheck>();
            var movement = player.AddComponent<PlayerMovement>();
            player.SetActive(true);

            _createdObjects.Add(player);
            return movement;
        }
    }
}
