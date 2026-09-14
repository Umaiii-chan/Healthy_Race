using HealthyRace.Input;
using UnityEngine;

namespace HealthyRace.Player
{
    /// <summary>
    /// Visualizes movement state in MovementTest without affecting gameplay physics.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerInputReader), typeof(PlayerMovement))]
    public sealed class PlayerMovementDebug : MonoBehaviour
    {
        private PlayerController _playerController;
        private PlayerInputReader _playerInputReader;
        private PlayerMovement _playerMovement;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _playerInputReader = GetComponent<PlayerInputReader>();
            _playerMovement = GetComponent<PlayerMovement>();
        }

        private void OnGUI()
        {
            if (_playerController == null || _playerInputReader == null || _playerMovement == null)
            {
                return;
            }

            var isPlayerOne = _playerInputReader.PlayerInputId == PlayerInputId.Player1;
            var x = isPlayerOne ? 16f : Screen.width - 286f;
            var controls = isPlayerOne ? "A / D + W or Space" : "Left / Right + Up";

            GUI.Box(new Rect(x, 18f, 270f, 126f), _playerInputReader.PlayerInputId.ToString());
            GUI.Label(
                new Rect(x + 12f, 44f, 246f, 20f),
                $"State: {_playerController.CurrentState}"
            );
            GUI.Label(
                new Rect(x + 12f, 66f, 246f, 20f),
                $"Grounded: {_playerController.IsGrounded}"
            );
            GUI.Label(
                new Rect(x + 12f, 88f, 246f, 20f),
                $"Velocity: {_playerMovement.Velocity.x:0.0}, {_playerMovement.Velocity.y:0.0}"
            );
            GUI.Label(new Rect(x + 12f, 110f, 246f, 20f), controls);
        }
    }
}
