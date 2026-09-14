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
    public sealed class PhysicsInteractionPlayModeTests
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
        public IEnumerator Box_FallsAndLandsOnGround_WithoutTunneling()
        {
            var ground = CreateGround(new Vector2(0f, -1f), new Vector2(10f, 1f));
            var box = CreateBox(new Vector2(0f, 2f));

            // Wait for box to drop and settle on ground
            for (var frame = 0; frame < 60; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            // Ground is at y = -1, thickness 1 => top surface is at y = -0.5
            // Box height 1 => center should be around y = 0.0
            Assert.That(
                box.transform.position.y,
                Is.GreaterThan(-0.5f),
                "Box must not fall through the ground."
            );
            Assert.That(
                Mathf.Abs(box.RigidbodyComponent.linearVelocity.y),
                Is.LessThan(0.1f),
                "Box should be at rest vertically."
            );
        }

        [UnityTest]
        public IEnumerator Player_CanPushBoxHorizontally()
        {
            CreateGround(new Vector2(0f, -1f), new Vector2(20f, 1f));
            var player = CreatePlayer(new Vector2(-1.5f, 0f));
            var box = CreateBox(new Vector2(0f, 0f));

            // Let both settle on ground
            for (var frame = 0; frame < 20; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            var initialBoxX = box.transform.position.x;

            // Player walks to the right pushing the box
            player.SetHorizontalInput(1f);

            for (var frame = 0; frame < 40; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(
                box.transform.position.x,
                Is.GreaterThan(initialBoxX + 0.1f),
                "Box should have been pushed to the right."
            );
        }

        [UnityTest]
        public IEnumerator Box_TriggersPressureButton_WhenPlacedOnIt()
        {
            CreateGround(new Vector2(0f, -1f), new Vector2(10f, 1f));
            var button = CreatePressureButton(new Vector2(0f, -0.45f));
            var box = CreateBox(new Vector2(0f, 1.5f));

            Assert.That(button.IsPressed, Is.False, "Button should start unpressed.");

            for (var frame = 0; frame < 60; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(
                button.IsPressed,
                Is.True,
                "Button should be pressed when box rests on it."
            );
        }

        [UnityTest]
        public IEnumerator Player_TriggersPressureButton_WhenSteppingOnIt()
        {
            CreateGround(new Vector2(0f, -1f), new Vector2(10f, 1f));
            var button = CreatePressureButton(new Vector2(0f, -0.45f));
            var player = CreatePlayer(new Vector2(0f, 1.5f));

            Assert.That(button.IsPressed, Is.False, "Button should start unpressed.");

            for (var frame = 0; frame < 60; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(
                button.IsPressed,
                Is.True,
                "Button should be pressed when player stands on it."
            );
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
    }
}
