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

        [Test]
        public void PushableBox_CachesSpawnPosition_AndResetsToSpawn()
        {
            var spawnPos = new Vector2(5f, 2f);
            _box.transform.position = spawnPos;
            _box.SetSpawnPosition(spawnPos);

            Assert.That(_box.InitialSpawnPosition, Is.EqualTo(spawnPos));

            // Move box away
            _box.transform.position = new Vector2(25f, -8f);
            _box.RigidbodyComponent.linearVelocity = new Vector2(10f, -5f);

            var resetCalled = false;
            _box.OnResetToSpawn += () => resetCalled = true;

            _box.ResetToSpawn();

            Assert.That(_box.transform.position, Is.EqualTo((Vector3)spawnPos));
            Assert.That(_box.RigidbodyComponent.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(resetCalled, Is.True);
        }

        [Test]
        public void CooperativePuzzleController_SolvesAndRecovers_FromImpossibleState()
        {
            var controllerObj = new GameObject("CoopController");
            var leverObj = new GameObject("CoopLever", typeof(BoxCollider2D));
            var doorObj = new GameObject("CoopDoor", typeof(BoxCollider2D));
            var platObj = new GameObject("CoopPlat", typeof(BoxCollider2D), typeof(Rigidbody2D));
            var resetObj = new GameObject("ResetSwitch", typeof(BoxCollider2D));

            try
            {
                var lever = leverObj.AddComponent<Lever>();
                var door = doorObj.AddComponent<PuzzleDoor>();
                door.Configure(new Vector2(0f, 3f), 4f, instantMotion: true);
                var plat = platObj.AddComponent<ActivatedPlatform>();
                var resetSwitch = resetObj.AddComponent<Lever>();

                var controller = controllerObj.AddComponent<CooperativePuzzleController>();
                controller.Bind(_button, lever, _box, door, plat, resetSwitch, requireAll: true);

                Assert.That(controller.IsSolved, Is.False);
                Assert.That(door.IsOpen, Is.False);
                Assert.That(plat.IsActive, Is.False);

                // Press button only (requireAll is true -> not solved yet)
                _button.ForceSetPressed(true);
                Assert.That(controller.IsSolved, Is.False);

                // Activate lever (both active -> solved!)
                lever.SetState(true);
                Assert.That(controller.IsSolved, Is.True);
                Assert.That(door.IsOpen, Is.True);
                Assert.That(plat.IsActive, Is.True);

                // Simulate impossible state: box pushed away, player activates reset switch
                _box.transform.position = new Vector2(999f, -999f);
                var resetFired = false;
                controller.OnPuzzleReset += () => resetFired = true;

                controller.ResetPuzzle();

                Assert.That(resetFired, Is.True);
                Assert.That(
                    _box.transform.position,
                    Is.EqualTo((Vector3)_box.InitialSpawnPosition)
                );
                Assert.That(lever.IsActivated, Is.False);
                Assert.That(controller.IsSolved, Is.False);
                Assert.That(door.IsOpen, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(controllerObj);
                Object.DestroyImmediate(leverObj);
                Object.DestroyImmediate(doorObj);
                Object.DestroyImmediate(platObj);
                Object.DestroyImmediate(resetObj);
            }
        }
    }
}
