using System;
using System.Collections.Generic;
using HealthyRace.Core;
using HealthyRace.Player;
using UnityEngine;

namespace HealthyRace.Level
{
    /// <summary>
    /// Level checkpoint trigger supporting independent activation by Player 1 and Player 2.
    /// Tracks the last valid respawn coordinates per player and provides visual activation feedback.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class Checkpoint : MonoBehaviour
    {
        private static readonly Dictionary<int, Checkpoint> LastCheckpoints = new();

        [Header("Configuration")]
        [SerializeField]
        private int _index = 1;

        [SerializeField]
        private Vector2 _respawnOffset = new(0f, 0.5f);

        [Header("Visual Feedback")]
        [SerializeField]
        private SpriteRenderer _flagRenderer;

        [SerializeField]
        private Color _inactiveColor = new(0.55f, 0.55f, 0.6f);

        [SerializeField]
        private Color _activeColor = new(0.25f, 0.85f, 0.35f);

        private readonly HashSet<int> _activatedPlayers = new();
        private BoxCollider2D _collider;

        public int Index => _index;

        public Vector2 RespawnPosition => (Vector2)transform.position + _respawnOffset;

        public int ActivatedPlayerCount => _activatedPlayers.Count;

        public static event Action<Checkpoint, int> AnyCheckpointReached;

        public event Action<int> CheckpointReached;

        public static Checkpoint GetLastCheckpoint(int playerIndex)
        {
            LastCheckpoints.TryGetValue(playerIndex, out var checkpoint);
            return checkpoint;
        }

        public static void ResetRegistry()
        {
            LastCheckpoints.Clear();
        }

        public bool IsActivatedByPlayer(int playerIndex)
        {
            return _activatedPlayers.Contains(playerIndex);
        }

        public void Configure(int index, Vector2 respawnOffset)
        {
            _index = index;
            _respawnOffset = respawnOffset;
        }

        public void SetVisualElements(
            SpriteRenderer flagRenderer,
            Color inactiveColor,
            Color activeColor
        )
        {
            _flagRenderer = flagRenderer;
            _inactiveColor = inactiveColor;
            _activeColor = activeColor;
            UpdateVisual();
        }

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            if (_flagRenderer == null)
            {
                _flagRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            UpdateVisual();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var controller = other.GetComponentInParent<PlayerController>();
            if (controller == null)
            {
                return;
            }

            ActivateForPlayer(controller.PlayerIndex);
        }

        public bool ActivateForPlayer(int playerIndex)
        {
            if (!_activatedPlayers.Add(playerIndex))
            {
                return false;
            }

            LastCheckpoints[playerIndex] = this;
            UpdateVisual();

            CheckpointReached?.Invoke(playerIndex);
            AnyCheckpointReached?.Invoke(this, playerIndex);
            return true;
        }

        private void UpdateVisual()
        {
            if (_flagRenderer == null)
            {
                return;
            }

            _flagRenderer.color = _activatedPlayers.Count > 0 ? _activeColor : _inactiveColor;
        }
    }
}
