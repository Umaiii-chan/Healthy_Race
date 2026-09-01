using HealthyRace.Input;
using UnityEngine;

namespace HealthyRace.Player
{
    /// <summary>
    /// Provides visual feedback in InputTest only. This is deliberately separate
    /// from the production player controller and is not gameplay movement.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerInputDebug : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _debugMoveSpeed = 3f;

        private int _interactPressCount;
        private int _jumpPressCount;
        private PlayerInputReader _playerInputReader;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _playerInputReader = GetComponent<PlayerInputReader>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _spriteRenderer.sprite = CreateDebugSprite();
            _spriteRenderer.color =
                _playerInputReader.PlayerInputId == PlayerInputId.Player1
                    ? new Color(0.15f, 0.67f, 1f)
                    : new Color(1f, 0.35f, 0.55f);
        }

        private void OnDisable()
        {
            if (_playerInputReader == null)
            {
                return;
            }

            _playerInputReader.JumpPerformed -= HandleJump;
            _playerInputReader.InteractPerformed -= HandleInteract;
        }

        private void OnEnable()
        {
            if (_playerInputReader == null)
            {
                _playerInputReader = GetComponent<PlayerInputReader>();
            }

            _playerInputReader.JumpPerformed += HandleJump;
            _playerInputReader.InteractPerformed += HandleInteract;
        }

        private void OnGUI()
        {
            if (_playerInputReader == null)
            {
                return;
            }

            var isPlayerOne = _playerInputReader.PlayerInputId == PlayerInputId.Player1;
            var x = isPlayerOne ? 16f : Screen.width - 276f;
            var controls = isPlayerOne
                ? "A / D | W or Space | E"
                : "Left / Right | Up | Right Ctrl";

            GUI.Box(new Rect(x, 84f, 260f, 118f), _playerInputReader.PlayerInputId.ToString());
            GUI.Label(
                new Rect(x + 12f, 110f, 236f, 20f),
                $"Move X: {_playerInputReader.MoveInput.x:0.0}"
            );
            GUI.Label(new Rect(x + 12f, 132f, 236f, 20f), $"Jump pressed: {_jumpPressCount}");
            GUI.Label(
                new Rect(x + 12f, 154f, 236f, 20f),
                $"Interact pressed: {_interactPressCount}"
            );
            GUI.Label(new Rect(x + 12f, 176f, 236f, 20f), controls);
        }

        private void Update()
        {
            var movement = _playerInputReader.MoveInput.x * _debugMoveSpeed * Time.deltaTime;
            transform.position += Vector3.right * movement;
        }

        private static Sprite CreateDebugSprite()
        {
            return Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f
            );
        }

        private void HandleInteract()
        {
            _interactPressCount++;
        }

        private void HandleJump()
        {
            _jumpPressCount++;
        }
    }
}
