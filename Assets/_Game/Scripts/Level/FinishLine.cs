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

        [Header("Porta por jogador (0 = aceita qualquer jogador)")]
        [SerializeField, Min(0)]
        private int _acceptedPlayerIndex;

        private readonly HashSet<int> _finishedPlayers = new();
        private readonly HashSet<int> _occupants = new();
        private readonly Dictionary<int, float> _playerFinishTimes = new();
        private BoxCollider2D _collider;

        public bool HasAnyPlayerFinished => _finishedPlayers.Count > 0;

        public int FinishedPlayerCount => _finishedPlayers.Count;

        public float FirstFinishTime { get; private set; } = -1f;

        public int AcceptedPlayerIndex => _acceptedPlayerIndex;

        /// <summary>Verdadeiro enquanto um jogador aceito está dentro da porta.</summary>
        public bool IsOccupied => _occupants.Count > 0;

        public int OccupantCount => _occupants.Count;

        public event Action<int> PlayerEnteredDoor;

        public event Action<int> PlayerLeftDoor;

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

        public bool IsOccupiedBy(int playerIndex)
        {
            return _occupants.Contains(playerIndex);
        }

        public bool AcceptsPlayer(int playerIndex)
        {
            return _acceptedPlayerIndex <= 0 || playerIndex == _acceptedPlayerIndex;
        }

        public void ConfigureAcceptedPlayer(int playerIndex)
        {
            _acceptedPlayerIndex = Mathf.Max(0, playerIndex);
        }

        /// <summary>Marca presença de um jogador na porta (também usado pelos testes).</summary>
        public bool SetOccupied(int playerIndex, bool occupied)
        {
            if (!AcceptsPlayer(playerIndex))
            {
                return false;
            }

            if (occupied)
            {
                if (_occupants.Add(playerIndex))
                {
                    PlayerEnteredDoor?.Invoke(playerIndex);
                }
                return true;
            }

            if (_occupants.Remove(playerIndex))
            {
                PlayerLeftDoor?.Invoke(playerIndex);
            }
            return true;
        }

        public void ResetState()
        {
            _finishedPlayers.Clear();
            _playerFinishTimes.Clear();
            _occupants.Clear();
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
            if (!TryResolvePlayerIndex(other, out var playerIndex) || !AcceptsPlayer(playerIndex))
            {
                return;
            }

            SetOccupied(playerIndex, true);
            RecordFinish(playerIndex);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!TryResolvePlayerIndex(other, out var playerIndex))
            {
                return;
            }

            SetOccupied(playerIndex, false);
        }

        private static bool TryResolvePlayerIndex(Collider2D other, out int playerIndex)
        {
            playerIndex = 0;
            var controller = other.GetComponentInParent<PlayerController>();
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (controller == null && movement == null)
            {
                return false;
            }

            playerIndex = controller != null ? controller.PlayerIndex : 1;
            return true;
        }

        public bool RecordFinish(int playerIndex)
        {
            if (!AcceptsPlayer(playerIndex) || !_finishedPlayers.Add(playerIndex))
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
