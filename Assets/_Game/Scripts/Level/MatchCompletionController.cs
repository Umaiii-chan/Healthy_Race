using System;
using HealthyRace.Player;
using HealthyRace.Puzzles;
using HealthyRace.Scoring;
using HealthyRace.UI;
using HealthyRace.WellBeing;
using UnityEngine;

namespace HealthyRace.Level
{
    public enum MatchCompletionState
    {
        Running,
        FinalCountdown,
        WaitingForPartner,
        Concluded,
    }

    [Serializable]
    public struct PlayerMatchResult
    {
        public int PlayerIndex;
        public float FinishTime;
        public bool IsDnf;
        public int FinalWellbeing;
        public int Placement;
        public PlayerScoreResult Score;
    }

    [Serializable]
    public struct MatchResultData
    {
        public PlayerMatchResult Player1;
        public PlayerMatchResult Player2;
        public int WinnerPlayerIndex; // 0 = tie, 1 = P1, 2 = P2
        public float TotalDuration;
        public bool HasDnf;
        public MatchScoreResult ScoreResult;
    }

    /// <summary>
    /// Coordinates the finish line sequence, final 20-second countdown window,
    /// DNF penalties, and match result compilation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchCompletionController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField, Min(1f)]
        private float _finalWindowDuration = 20f;

        [SerializeField, Min(0)]
        private int _dnfPenalty = 20;

        [Header("Scoring")]
        [SerializeField]
        private ScoreConfig _scoreConfig;

        [Header("Scene References")]
        [SerializeField]
        private FinishLine _finishLine;

        [Header("Portas duplas (arena cooperativa)")]
        [SerializeField]
        private FinishLine _secondFinishLine;

        [SerializeField]
        private bool _requireBothAtDoors;

        [SerializeField]
        private MatchHUD _matchHud;

        [SerializeField]
        private MatchResultsPanel _resultsPanel;

        private MatchCompletionState _state = MatchCompletionState.Running;
        private float _finalWindowTimeRemaining;
        private int _firstFinisherIndex;
        private GameObject _playerOne;
        private GameObject _playerTwo;
        private WellbeingComponent _playerOneWellbeing;
        private WellbeingComponent _playerTwoWellbeing;
        private PlayerHUD _playerOneHud;
        private PlayerHUD _playerTwoHud;
        private CooperativePuzzleController _finalPuzzle;
        private MatchResultData _finalResults;

        public MatchCompletionState State => _state;
        public float FinalWindowDuration => _finalWindowDuration;
        public float FinalWindowTimeRemaining => _finalWindowTimeRemaining;
        public int DnfPenalty => _dnfPenalty;
        public int FirstFinisherIndex => _firstFinisherIndex;
        public MatchResultData FinalResults => _finalResults;
        public ScoreConfig ScoreConfig => _scoreConfig;
        public bool RequireBothAtDoors => _requireBothAtDoors;
        public FinishLine PlayerOneDoor => _finishLine;
        public FinishLine PlayerTwoDoor => _requireBothAtDoors ? _secondFinishLine : _finishLine;

        public event Action<int, float> PlayerFinished;
        public event Action<float> FinalWindowStarted;
        public event Action<float> FinalWindowTicked;
        public event Action<MatchResultData> MatchConcluded;

        public void Configure(
            FinishLine finishLine,
            MatchHUD matchHud,
            GameObject playerOne,
            GameObject playerTwo,
            WellbeingComponent p1Wb,
            WellbeingComponent p2Wb,
            MatchResultsPanel resultsPanel = null,
            float finalWindowDuration = 20f,
            int dnfPenalty = 20,
            PlayerHUD p1Hud = null,
            PlayerHUD p2Hud = null,
            CooperativePuzzleController finalPuzzle = null,
            ScoreConfig scoreConfig = null,
            FinishLine secondFinishLine = null,
            bool requireBothAtDoors = false
        )
        {
            UnbindFinishLine();

            _finishLine = finishLine;
            _secondFinishLine = secondFinishLine;
            _requireBothAtDoors = requireBothAtDoors && secondFinishLine != null;
            _matchHud = matchHud;
            _playerOne = playerOne;
            _playerTwo = playerTwo;
            _playerOneWellbeing = p1Wb;
            _playerTwoWellbeing = p2Wb;
            _resultsPanel = resultsPanel;
            _finalWindowDuration = Mathf.Max(0.05f, finalWindowDuration);
            _dnfPenalty = Mathf.Max(0, dnfPenalty);
            _playerOneHud = p1Hud;
            _playerTwoHud = p2Hud;
            _finalPuzzle = finalPuzzle;
            _scoreConfig = scoreConfig;

            BindFinishLine();
        }

        private void Awake()
        {
            BindFinishLine();
        }

        private void OnDestroy()
        {
            UnbindFinishLine();
        }

        private void BindFinishLine()
        {
            if (_finishLine != null)
            {
                _finishLine.PlayerCrossedFinish -= HandlePlayerCrossedFinish;
                _finishLine.PlayerCrossedFinish += HandlePlayerCrossedFinish;
            }

            if (_secondFinishLine != null && _secondFinishLine != _finishLine)
            {
                _secondFinishLine.PlayerCrossedFinish -= HandlePlayerCrossedFinish;
                _secondFinishLine.PlayerCrossedFinish += HandlePlayerCrossedFinish;
            }
        }

        private void UnbindFinishLine()
        {
            if (_finishLine != null)
            {
                _finishLine.PlayerCrossedFinish -= HandlePlayerCrossedFinish;
            }

            if (_secondFinishLine != null)
            {
                _secondFinishLine.PlayerCrossedFinish -= HandlePlayerCrossedFinish;
            }
        }

        private FinishLine GetDoor(int playerIndex)
        {
            if (_requireBothAtDoors && playerIndex == 2)
            {
                return _secondFinishLine;
            }

            return _finishLine;
        }

        /// <summary>Verdadeiro quando P1 e P2 estão simultaneamente em suas portas.</summary>
        public bool AreBothPlayersAtDoors()
        {
            var doorOne = GetDoor(1);
            var doorTwo = GetDoor(2);
            return doorOne != null
                && doorTwo != null
                && doorOne.IsOccupiedBy(1)
                && doorTwo.IsOccupiedBy(2);
        }

        private void Update()
        {
            if (_state == MatchCompletionState.WaitingForPartner)
            {
                if (AreBothPlayersAtDoors())
                {
                    ConcludeMatch();
                }
                return;
            }

            if (_state != MatchCompletionState.FinalCountdown)
            {
                return;
            }

            _finalWindowTimeRemaining -= Time.deltaTime;
            FinalWindowTicked?.Invoke(_finalWindowTimeRemaining);

            if (_matchHud != null)
            {
                _matchHud.SetFinalWindow(true, _finalWindowTimeRemaining);
            }

            if (_finalWindowTimeRemaining <= 0f)
            {
                HandleWindowExpired();
            }
        }

        public void HandlePlayerCrossedFinish(int playerIndex, float finishTime)
        {
            if (_requireBothAtDoors)
            {
                HandleDoorArrival(playerIndex, finishTime);
                return;
            }

            FreezePlayer(playerIndex);

            if (_state == MatchCompletionState.Running)
            {
                _firstFinisherIndex = playerIndex;
                _state = MatchCompletionState.FinalCountdown;
                _finalWindowTimeRemaining = _finalWindowDuration;

                if (_matchHud != null)
                {
                    _matchHud.SetFinalWindow(true, _finalWindowDuration);
                }

                PlayerFinished?.Invoke(playerIndex, finishTime);
                FinalWindowStarted?.Invoke(_finalWindowDuration);
            }
            else if (_state == MatchCompletionState.FinalCountdown)
            {
                PlayerFinished?.Invoke(playerIndex, finishTime);
                ConcludeMatch();
            }
        }

        /// <summary>
        /// Modo portas duplas: a primeira chegada define a ordem, mas ninguém é
        /// congelado (o primeiro pode sair para ajudar). A partida só encerra quando
        /// os dois estão em suas portas ao mesmo tempo. Não há janela final nem DNF.
        /// </summary>
        private void HandleDoorArrival(int playerIndex, float finishTime)
        {
            if (_state == MatchCompletionState.Concluded)
            {
                return;
            }

            if (_state == MatchCompletionState.Running)
            {
                _firstFinisherIndex = playerIndex;
                _state = MatchCompletionState.WaitingForPartner;
                _finalWindowTimeRemaining = 0f;

                if (_matchHud != null)
                {
                    var partner = playerIndex == 1 ? 2 : 1;
                    _matchHud.ShowStatusMessage($"P{playerIndex} NA PORTA - AGUARDANDO P{partner}");
                }
            }

            PlayerFinished?.Invoke(playerIndex, finishTime);

            if (AreBothPlayersAtDoors())
            {
                ConcludeMatch();
            }
        }

        public void HandleWindowExpired()
        {
            if (_state != MatchCompletionState.FinalCountdown)
            {
                return;
            }

            var missingPlayer = _firstFinisherIndex == 1 ? 2 : 1;
            FreezePlayer(missingPlayer);

            var missingWellbeing = GetPlayerWellbeing(missingPlayer);
            if (missingWellbeing != null)
            {
                missingWellbeing.ApplyDelta(-_dnfPenalty);
            }

            ConcludeMatch();
        }

        public void ConcludeMatch()
        {
            _state = MatchCompletionState.Concluded;

            if (_matchHud != null)
            {
                _matchHud.SetFinalWindow(false);
            }

            FreezePlayer(1);
            FreezePlayer(2);

            _finalResults = CompileResults();

            if (_resultsPanel != null)
            {
                _resultsPanel.Show(_finalResults);
            }

            MatchConcluded?.Invoke(_finalResults);
        }

        public MatchResultData CompileResults()
        {
            var doorOne = GetDoor(1);
            var doorTwo = GetDoor(2);
            var p1Finished = doorOne != null && doorOne.HasPlayerFinished(1);
            var p2Finished = doorTwo != null && doorTwo.HasPlayerFinished(2);

            var p1Time = doorOne != null ? doorOne.GetPlayerFinishTime(1) : -1f;
            var p2Time = doorTwo != null ? doorTwo.GetPlayerFinishTime(2) : -1f;

            var p1Wb = _playerOneWellbeing != null ? _playerOneWellbeing.CurrentValue : 0;
            var p2Wb = _playerTwoWellbeing != null ? _playerTwoWellbeing.CurrentValue : 0;

            var r1 = new PlayerMatchResult
            {
                PlayerIndex = 1,
                FinishTime = p1Time,
                IsDnf = !p1Finished,
                FinalWellbeing = p1Wb,
            };

            var r2 = new PlayerMatchResult
            {
                PlayerIndex = 2,
                FinishTime = p2Time,
                IsDnf = !p2Finished,
            };

            // Calculate placements based on finish line crossing
            if (p1Finished && !p2Finished)
            {
                r1.Placement = 1;
                r2.Placement = 2;
            }
            else if (p2Finished && !p1Finished)
            {
                r1.Placement = 2;
                r2.Placement = 1;
            }
            else if (p1Finished && p2Finished)
            {
                if (_firstFinisherIndex == 1)
                {
                    r1.Placement = 1;
                    r2.Placement = 2;
                }
                else if (_firstFinisherIndex == 2)
                {
                    r1.Placement = 2;
                    r2.Placement = 1;
                }
                else if (p1Time < p2Time)
                {
                    r1.Placement = 1;
                    r2.Placement = 2;
                }
                else if (p2Time < p1Time)
                {
                    r1.Placement = 2;
                    r2.Placement = 1;
                }
                else
                {
                    r1.Placement = 1;
                    r2.Placement = 1;
                }
            }
            else
            {
                // Neither finished
                r1.Placement = 1;
                r2.Placement = 1;
            }

            r1.FinalWellbeing = p1Wb;
            r2.FinalWellbeing = p2Wb;

            // Collect scoring inputs from HUDs, puzzles, and fatigue components
            var p1Fatigue = _playerOne != null ? _playerOne.GetComponent<FatigueComponent>() : null;
            var p2Fatigue = _playerTwo != null ? _playerTwo.GetComponent<FatigueComponent>() : null;

            int p1Puzzles = (_finalPuzzle != null && _finalPuzzle.IsSolved) ? 1 : 0;
            int p2Puzzles = (_finalPuzzle != null && _finalPuzzle.IsSolved) ? 1 : 0;

            var p1Input = new ScoreInput
            {
                PlayerIndex = 1,
                FinishTime = p1Time,
                Finished = p1Finished,
                ArrivalPlacement = p1Finished ? r1.Placement : 0,
                FinalWellbeing = p1Wb,
                HealthyPointsEarned = _playerOneHud != null ? _playerOneHud.Score : 0,
                HealthyFoodsCount = _playerOneHud != null ? _playerOneHud.HealthyFoodCount : 0,
                UltraProcessedFoodsCount =
                    _playerOneHud != null ? _playerOneHud.UltraProcessedFoodCount : 0,
                PuzzlesSolvedCount = p1Puzzles,
                FatigueEpisodesCount = p1Fatigue != null ? p1Fatigue.FatigueEpisodeCount : 0,
            };

            var p2Input = new ScoreInput
            {
                PlayerIndex = 2,
                FinishTime = p2Time,
                Finished = p2Finished,
                ArrivalPlacement = p2Finished ? r2.Placement : 0,
                FinalWellbeing = p2Wb,
                HealthyPointsEarned = _playerTwoHud != null ? _playerTwoHud.Score : 0,
                HealthyFoodsCount = _playerTwoHud != null ? _playerTwoHud.HealthyFoodCount : 0,
                UltraProcessedFoodsCount =
                    _playerTwoHud != null ? _playerTwoHud.UltraProcessedFoodCount : 0,
                PuzzlesSolvedCount = p2Puzzles,
                FatigueEpisodesCount = p2Fatigue != null ? p2Fatigue.FatigueEpisodeCount : 0,
            };

            var scoreResult = ScoreService.EvaluateMatch(p1Input, p2Input, _scoreConfig);
            r1.Score = scoreResult.Player1;
            r2.Score = scoreResult.Player2;

            int winner = scoreResult.WinnerPlayerIndex;

            return new MatchResultData
            {
                Player1 = r1,
                Player2 = r2,
                WinnerPlayerIndex = winner,
                TotalDuration = Mathf.Max(p1Time, p2Time),
                HasDnf = r1.IsDnf || r2.IsDnf,
                ScoreResult = scoreResult,
            };
        }

        public void FreezePlayer(int playerIndex)
        {
            var playerObj = playerIndex == 1 ? _playerOne : _playerTwo;
            if (playerObj == null)
            {
                return;
            }

            var controller = playerObj.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetMovementEnabled(false);
            }

            var movement = playerObj.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.SetMovementEnabled(false);
            }

            var rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        public WellbeingComponent GetPlayerWellbeing(int playerIndex)
        {
            return playerIndex == 1 ? _playerOneWellbeing : _playerTwoWellbeing;
        }

        public void ResetMatch()
        {
            _state = MatchCompletionState.Running;
            _finalWindowTimeRemaining = 0f;
            _firstFinisherIndex = 0;
            _finalResults = default;

            if (_finishLine != null)
            {
                _finishLine.ResetState();
            }

            if (_secondFinishLine != null)
            {
                _secondFinishLine.ResetState();
            }

            if (_matchHud != null)
            {
                _matchHud.SetFinalWindow(false);
            }

            if (_resultsPanel != null)
            {
                _resultsPanel.Hide();
            }
        }
    }
}
