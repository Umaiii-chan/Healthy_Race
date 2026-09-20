using System;
using System.Collections.Generic;
using HealthyRace.Player;
using UnityEngine;

namespace HealthyRace.Obstacles
{
    /// <summary>
    /// Mud / sludge hazard area that slows down traversing players by a configurable multiplier (default -30%).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class MudHazard : MonoBehaviour
    {
        [Header("Mud Settings")]
        [SerializeField, Range(0.1f, 1f)]
        private float _speedMultiplier = 0.7f;

        [Header("Visual Feedback")]
        [SerializeField]
        private SpriteRenderer _visualRenderer;

        private readonly HashSet<PlayerMovement> _affectedPlayers = new();
        private BoxCollider2D _collider;

        public float SpeedMultiplier => _speedMultiplier;
        public int AffectedPlayerCount => _affectedPlayers.Count;

        public event Action<PlayerMovement> PlayerEntered;
        public event Action<PlayerMovement> PlayerExited;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (movement == null)
            {
                return;
            }

            if (_affectedPlayers.Add(movement))
            {
                movement.SetMovementMultipliers(_speedMultiplier, movement.JumpMultiplier);
                PlayerEntered?.Invoke(movement);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (movement == null)
            {
                return;
            }

            if (_affectedPlayers.Remove(movement))
            {
                movement.SetMovementMultipliers(1f, movement.JumpMultiplier);
                PlayerExited?.Invoke(movement);
            }
        }

        private void OnDisable()
        {
            ResetAffectedPlayers();
        }

        private void OnDestroy()
        {
            ResetAffectedPlayers();
        }

        public void Configure(float speedMultiplier)
        {
            _speedMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
        }

        public bool IsPlayerAffected(PlayerMovement movement)
        {
            return movement != null && _affectedPlayers.Contains(movement);
        }

        public void ResetAffectedPlayers()
        {
            foreach (var movement in _affectedPlayers)
            {
                if (movement != null)
                {
                    movement.SetMovementMultipliers(1f, movement.JumpMultiplier);
                }
            }
            _affectedPlayers.Clear();
        }
    }
}
