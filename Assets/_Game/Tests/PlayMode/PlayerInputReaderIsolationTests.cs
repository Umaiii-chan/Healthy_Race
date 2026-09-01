using System.IO;
using HealthyRace.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class PlayerInputReaderIsolationTests : InputTestFixture
    {
        private const string InputActionsRelativePath =
            "_Game/Input/HealthyRaceInputActions.inputactions";

        private GameObject _playerOneObject;
        private GameObject _playerTwoObject;
        private PlayerInputReader _playerOneReader;
        private PlayerInputReader _playerTwoReader;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            var inputActionsPath = Path.Combine(Application.dataPath, InputActionsRelativePath);
            var inputActions = InputActionAsset.FromJson(File.ReadAllText(inputActionsPath));

            _playerOneReader = CreateReader(
                inputActions,
                PlayerInputId.Player1,
                out _playerOneObject
            );
            _playerTwoReader = CreateReader(
                inputActions,
                PlayerInputId.Player2,
                out _playerTwoObject
            );
        }

        [TearDown]
        public override void TearDown()
        {
            if (_playerOneObject != null)
            {
                Object.DestroyImmediate(_playerOneObject);
            }

            if (_playerTwoObject != null)
            {
                Object.DestroyImmediate(_playerTwoObject);
            }

            base.TearDown();
        }

        [Test]
        public void PlayerOneKeyboardBindings_DoNotAffectPlayerTwo()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var playerOneJumpCount = 0;
            var playerTwoJumpCount = 0;
            _playerOneReader.JumpPerformed += () => playerOneJumpCount++;
            _playerTwoReader.JumpPerformed += () => playerTwoJumpCount++;

            Press(keyboard.aKey);
            Assert.That(_playerOneReader.MoveInput.x, Is.EqualTo(-1f));
            Assert.That(_playerTwoReader.MoveInput, Is.EqualTo(Vector2.zero));
            Release(keyboard.aKey);

            Press(keyboard.dKey);
            Assert.That(_playerOneReader.MoveInput.x, Is.EqualTo(1f));
            Assert.That(_playerTwoReader.MoveInput, Is.EqualTo(Vector2.zero));
            Release(keyboard.dKey);

            Press(keyboard.wKey);
            Assert.That(playerOneJumpCount, Is.EqualTo(1));
            Assert.That(playerTwoJumpCount, Is.Zero);
            Release(keyboard.wKey);

            Press(keyboard.spaceKey);
            Assert.That(playerOneJumpCount, Is.EqualTo(2));
            Assert.That(playerTwoJumpCount, Is.Zero);
            Release(keyboard.spaceKey);

            Press(keyboard.eKey);
            Assert.That(_playerOneReader.InteractPressed, Is.True);
            Assert.That(_playerTwoReader.InteractPressed, Is.False);
        }

        [Test]
        public void PlayerTwoKeyboardBindings_DoNotAffectPlayerOne()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.leftArrowKey);
            Assert.That(_playerTwoReader.MoveInput.x, Is.EqualTo(-1f));
            Assert.That(_playerOneReader.MoveInput, Is.EqualTo(Vector2.zero));
            Release(keyboard.leftArrowKey);

            Press(keyboard.rightArrowKey);
            Assert.That(_playerTwoReader.MoveInput.x, Is.EqualTo(1f));
            Assert.That(_playerOneReader.MoveInput, Is.EqualTo(Vector2.zero));
            Release(keyboard.rightArrowKey);

            Press(keyboard.upArrowKey);
            Assert.That(_playerTwoReader.JumpPressed, Is.True);
            Assert.That(_playerOneReader.JumpPressed, Is.False);
            Release(keyboard.upArrowKey);

            Press(keyboard.rightCtrlKey);
            Assert.That(_playerTwoReader.InteractPressed, Is.True);
            Assert.That(_playerOneReader.InteractPressed, Is.False);
        }

        private static PlayerInputReader CreateReader(
            InputActionAsset inputActions,
            PlayerInputId playerInputId,
            out GameObject playerObject
        )
        {
            playerObject = new GameObject(playerInputId.ToString());
            playerObject.SetActive(false);

            var inputReader = playerObject.AddComponent<PlayerInputReader>();
            inputReader.Configure(inputActions, playerInputId);
            playerObject.SetActive(true);
            return inputReader;
        }
    }
}
