using HealthyRace.Obstacles;
using HealthyRace.Player;
using HealthyRace.Puzzles;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class PhysicsPuzzleTests
    {
        private GameObject _boxObject;
        private PushableBox _box;
        private GameObject _buttonObject;
        private PressureButton _button;

        [SetUp]
        public void SetUp()
        {
            _boxObject = new GameObject("TestBox");
            _boxObject.AddComponent<BoxCollider2D>();
            _box = _boxObject.AddComponent<PushableBox>();

            _buttonObject = new GameObject("TestButton");
            _buttonObject.AddComponent<BoxCollider2D>();
            _button = _buttonObject.AddComponent<PressureButton>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_boxObject);
            Object.DestroyImmediate(_buttonObject);
        }

        [Test]
        public void PushableBox_InitializesPhysicsPropertiesCorrectly()
        {
            var rb = _box.RigidbodyComponent;

            Assert.That(rb.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
            Assert.That(rb.mass, Is.EqualTo(1.5f));
            Assert.That(rb.linearDamping, Is.EqualTo(1.5f));
            Assert.That(rb.gravityScale, Is.EqualTo(2.4f));
            Assert.That(rb.constraints, Is.EqualTo(RigidbodyConstraints2D.FreezeRotation));
            Assert.That(rb.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
            Assert.That(rb.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
        }

        [Test]
        public void PushableBox_SetMassAndDamping_UpdatesRigidbody()
        {
            _box.SetMass(3.0f);
            _box.SetLinearDamping(2.0f);

            Assert.That(_box.RigidbodyComponent.mass, Is.EqualTo(3.0f));
            Assert.That(_box.RigidbodyComponent.linearDamping, Is.EqualTo(2.0f));
        }

        [Test]
        public void PressureButton_StartsUnpressed()
        {
            Assert.That(_button.IsPressed, Is.False);
            Assert.That(_button.ActivatorCount, Is.EqualTo(0));
        }

        [Test]
        public void PressureButton_IdentifiesBoxAsValidActivator()
        {
            var col = _boxObject.GetComponent<Collider2D>();
            Assert.That(_button.IsValidActivator(col), Is.True);
        }

        [Test]
        public void PressureButton_IdentifiesPlayerAsValidActivator()
        {
            var playerObj = new GameObject("TestPlayer");
            try
            {
                var col = playerObj.AddComponent<BoxCollider2D>();
                playerObj.AddComponent<Rigidbody2D>();
                playerObj.AddComponent<PlayerGroundCheck>();
                playerObj.AddComponent<PlayerMovement>();
                Assert.That(_button.IsValidActivator(col), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(playerObj);
            }
        }

        [Test]
        public void PressureButton_RejectsUnrelatedCollider()
        {
            var randomObj = new GameObject("RandomProp");
            try
            {
                var col = randomObj.AddComponent<BoxCollider2D>();
                Assert.That(_button.IsValidActivator(col), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(randomObj);
            }
        }

        [Test]
        public void PressureButton_EmitsEventsOnStateChange()
        {
            var pressedCount = 0;
            var releasedCount = 0;
            bool? lastState = null;

            _button.OnPressed += () => pressedCount++;
            _button.OnReleased += () => releasedCount++;
            _button.OnPressedStateChanged += state => lastState = state;

            _button.ForceSetPressed(true);

            Assert.That(_button.IsPressed, Is.True);
            Assert.That(pressedCount, Is.EqualTo(1));
            Assert.That(releasedCount, Is.EqualTo(0));
            Assert.That(lastState, Is.True);

            _button.ForceSetPressed(false);

            Assert.That(_button.IsPressed, Is.False);
            Assert.That(pressedCount, Is.EqualTo(1));
            Assert.That(releasedCount, Is.EqualTo(1));
            Assert.That(lastState, Is.False);
        }
    }
}
