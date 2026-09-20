using System;
using System.Collections.Generic;
using HealthyRace.Player;
using UnityEngine;

namespace HealthyRace.Level
{
    /// <summary>
    /// Level finish trigger that tracks player arrival times and completion order.
    /// Emits decoupled events to prepare for the end-game sequence (GOAL-01).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class FinishLine : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField]
        private SpriteRenderer _bannerRenderer;

        private readonly HashSet<int> _finishedPlayers = new();
        private readonly Dictionary<int, float> _playerFinishTimes = new();
        private BoxCollider2D _collider;

        public bool HasAnyPlayerFinished => _finishedPlayers.Count > 0;

        public int FinishedPlayerCount => _finishedPlayers.Count;

        public float FirstFinishTime { get; private set; } = -1f;

        public static event Action<int, float> AnyPlayerFinished;

        public event Action<int, float> PlayerCrossedFinish;

        public bool HasPlayerFinished(int playerIndex)
        {
            return _finishedPlayers.Contains(playerIndex);
        }

        public float GetPlayerFinishTime(int playerIndex)
        {
            return _playerFinishTimes.TryGetValue(playerIndex, out var time) ? time : -1f;
        }

        public void ResetState()
        {
            _finishedPlayers.Clear();
            _playerFinishTimes.Clear();
            FirstFinishTime = -1f;
        }

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var controller = other.GetComponentInParent<PlayerController>();
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (controller == null && movement == null)
            {
                return;
            }

            var playerIndex = controller != null ? controller.PlayerIndex : 1;
            RecordFinish(playerIndex);
        }

        public bool RecordFinish(int playerIndex)
        {
            if (!_finishedPlayers.Add(playerIndex))
            {
                return false;
            }

            var time = Time.timeSinceLevelLoad;
            _playerFinishTimes[playerIndex] = time;

            if (FirstFinishTime < 0f)
            {
                FirstFinishTime = time;
            }

            PlayerCrossedFinish?.Invoke(playerIndex, time);
            AnyPlayerFinished?.Invoke(playerIndex, time);
            return true;
        }
    }
}
