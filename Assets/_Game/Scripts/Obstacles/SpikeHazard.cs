using System;
using System.Collections.Generic;
using HealthyRace.Level;
using HealthyRace.Player;
using HealthyRace.WellBeing;
using UnityEngine;

namespace HealthyRace.Obstacles
{
    /// <summary>
    /// Lethal spike hazard. On player contact, applies a wellbeing penalty, resets
    /// velocity, and respawns the player at their last recorded checkpoint.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class SpikeHazard : MonoBehaviour
    {
        [Header("Hazard Settings")]
        [SerializeField, Min(0)]
        private int _wellbeingPenalty = 5;

        [SerializeField, Min(0.1f)]
        private float _hitCooldown = 1f;

        [SerializeField]
        private Vector2 _defaultRespawnPosition = Vector2.zero;

        [Header("Visual Elements")]
        [SerializeField]
        private SpriteRenderer _visualRenderer;

        private readonly Dictionary<int, float> _lastHitTimes = new();
        private BoxCollider2D _collider;

        public int WellbeingPenalty => _wellbeingPenalty;
        public float HitCooldown => _hitCooldown;
        public Vector2 DefaultRespawnPosition => _defaultRespawnPosition;

        public event Action<int, Vector2> PlayerPenalized;
        public event Action OnAnyPlayerHit;

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
            HandlePlayerContact(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandlePlayerContact(other.gameObject);
        }

        public bool HandlePlayerContact(GameObject contactObject)
        {
            if (contactObject == null)
            {
                return false;
            }

            var playerController = contactObject.GetComponentInParent<PlayerController>();
            var playerMovement = contactObject.GetComponentInParent<PlayerMovement>();

            if (playerController == null && playerMovement == null)
            {
                return false;
            }

            var playerIndex = playerController != null ? playerController.PlayerIndex : 1;
            var currentTime = Time.time;

            if (
                _lastHitTimes.TryGetValue(playerIndex, out var lastHit)
                && currentTime < lastHit + _hitCooldown
            )
            {
                return false;
            }

            _lastHitTimes[playerIndex] = currentTime;

            var targetTransform =
                playerController != null ? playerController.transform : playerMovement.transform;

            // Apply wellbeing penalty
            var wellbeing = targetTransform.GetComponent<WellbeingComponent>();
            if (wellbeing != null)
            {
                wellbeing.ApplyDelta(-_wellbeingPenalty);
            }

            // Determine respawn point
            var checkpoint = Checkpoint.GetLastCheckpoint(playerIndex);
            var respawnPosition =
                checkpoint != null ? checkpoint.RespawnPosition : _defaultRespawnPosition;

            // Teleport and halt momentum
            targetTransform.position = respawnPosition;
            var rb = targetTransform.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            PlayerPenalized?.Invoke(playerIndex, respawnPosition);
            OnAnyPlayerHit?.Invoke();
            return true;
        }

        public void Configure(
            int wellbeingPenalty,
            float hitCooldown,
            Vector2 defaultRespawnPosition
        )
        {
            _wellbeingPenalty = Mathf.Max(0, wellbeingPenalty);
            _hitCooldown = Mathf.Max(0.05f, hitCooldown);
            _defaultRespawnPosition = defaultRespawnPosition;
        }

        public void ResetCooldowns()
        {
            _lastHitTimes.Clear();
        }
    }
}
