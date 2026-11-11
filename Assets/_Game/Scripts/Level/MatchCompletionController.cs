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

        [Tooltip("Duração máxima da partida em segundos. 0 = sem limite.")]
        [SerializeField, Min(0f)]
        private float _timeLimitSeconds;

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

        private const float TimeLimitWarningSeconds = 30f;

        private MatchCompletionState _state = MatchCompletionState.Running;
        private float _elapsed;
        private float _finalWindowTimeRemaining;
        private int _firstFinisherIndex;
        private GameObject _playerOne;
        private GameObject _playerTwo;
        private WellbeingComponent _playerOneWellbeing;
        private WellbeingComponent _playerTwoWellbeing;
        private PlayerHUD _playerOneHud;
        private PlayerHUD _playerTwoHud;
        private MatchResultData _finalResults;

        public MatchCompletionState State => _state;
        public float FinalWindowDuration => _finalWindowDuration;
        public float FinalWindowTimeRemaining => _finalWindowTimeRemaining;
        public int DnfPenalty => _dnfPenalty;
        public float TimeLimitSeconds => _timeLimitSeconds;

        /// <summary>Tempo de partida decorrido (para de contar ao concluir).</summary>
        public float Elapsed => _elapsed;
        public int FirstFinisherIndex => _firstFinisherIndex;
        public MatchResultData FinalResults => _finalResults;
        public ScoreConfig ScoreConfig => _scoreConfig;

        private readonly int[] _puzzlesSolved = new int[3];

        /// <summary>Mecanismos resolvidos por jogador (alavancas puxadas, nas corridas).</summary>
        public int PuzzlesSolvedBy(int playerIndex) =>
            playerIndex is 1 or 2 ? _puzzlesSolved[playerIndex] : 0;

        /// <summary>Credita um mecanismo resolvido (vale <see cref="Scoring.ScoreConfig.PointsPerPuzzle"/>).</summary>
        public void RegisterPuzzleSolved(int playerIndex)
        {
            if (playerIndex is 1 or 2 && _state != MatchCompletionState.Concluded)
            {
                _puzzlesSolved[playerIndex]++;
            }
        }

        public bool RequireBothAtDoors => _requireBothAtDoors;
        public FinishLine PlayerOneDoor => _finishLine;
        public FinishLine PlayerTwoDoor =>
            _secondFinishLine != null ? _secondFinishLine : _finishLine;

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
            ScoreConfig scoreConfig = null,
            FinishLine secondFinishLine = null,
            bool requireBothAtDoors = false,
            float timeLimitSeconds = 0f
        )
        {
            UnbindFinishLine();
            _timeLimitSeconds = Mathf.Max(0f, timeLimitSeconds);

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

        /// <summary>
        /// Porta de cada jogador. Com duas portas, a segunda é sempre do P2 — exija
        /// ou não os dois juntos nela.
        /// </summary>
        private FinishLine GetDoor(int playerIndex)
        {
            if (playerIndex == 2 && _secondFinishLine != null)
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
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Avança o relógio da partida. Público para que os testes possam simular a
        /// passagem do tempo sem esperar em tempo real.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_state == MatchCompletionState.Concluded)
            {
                return;
            }

            _elapsed += deltaTime;
            if (_timeLimitSeconds > 0f && _elapsed >= _timeLimitSeconds)
            {
                ConcludeWithPenalties();
                return;
            }

            if (_state == MatchCompletionState.WaitingForPartner)
            {
                if (AreBothPlayersAtDoors())
                {
                    ConcludeMatch();
                    return;
                }

                // Sem prazo, um parceiro que não consegue subir prenderia a partida
                // para sempre. O primeiro segue livre para ajudar enquanto o prazo corre.
                _finalWindowTimeRemaining -= deltaTime;
                FinalWindowTicked?.Invoke(_finalWindowTimeRemaining);
                if (_matchHud != null)
                {
                    var partner = _firstFinisherIndex == 1 ? 2 : 1;
                    _matchHud.ShowStatusMessage(
                        $"P{_firstFinisherIndex} NA PORTA - P{partner} TEM "
                            + $"{Mathf.CeilToInt(Mathf.Max(0f, _finalWindowTimeRemaining))}s"
                    );
                }

                if (_finalWindowTimeRemaining <= 0f)
                {
                    ConcludeWithPenalties();
                }

                return;
            }

            if (_state == MatchCompletionState.Running)
            {
                var remaining = _timeLimitSeconds - _elapsed;
                if (
                    _timeLimitSeconds > 0f
                    && remaining <= TimeLimitWarningSeconds
                    && _matchHud != null
                )
                {
                    _matchHud.ShowStatusMessage($"TEMPO ACABANDO: {Mathf.CeilToInt(remaining)}s");
                }

                return;
            }

            if (_state != MatchCompletionState.FinalCountdown)
            {
                return;
            }

            _finalWindowTimeRemaining -= deltaTime;
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
        /// congelado (o primeiro pode sair para ajudar). A partida encerra quando os
        /// dois estão em suas portas ao mesmo tempo ou quando a janela aberta pela
        /// primeira chegada expira; nesse caso quem não chegou leva DNF.
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
                _finalWindowTimeRemaining = _finalWindowDuration;
                FinalWindowStarted?.Invoke(_finalWindowDuration);

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

        /// <summary>
        /// Encerra por tempo (janela das portas ou limite da partida): quem não
        /// registrou chegada perde Bem-Estar e sai como DNF. Vale a chegada
        /// registrada, não a presença na porta no instante do prazo: quem chegou e
        /// saiu para ajudar o parceiro não é punido por ter ajudado.
        /// </summary>
        public void ConcludeWithPenalties()
        {
            if (_state == MatchCompletionState.Concluded)
            {
                return;
            }

            for (var playerIndex = 1; playerIndex <= 2; playerIndex++)
            {
                var door = GetDoor(playerIndex);
                if (door != null && door.HasPlayerFinished(playerIndex))
                {
                    continue;
                }

                var wellbeing = GetPlayerWellbeing(playerIndex);
                if (wellbeing != null)
                {
                    wellbeing.ApplyDelta(-_dnfPenalty);
                }
            }

            ConcludeMatch();
        }

        public void ConcludeMatch()
        {
            _state = MatchCompletionState.Concluded;

            // A fadiga em curso reativaria o movimento e somaria Bem-Estar depois
            // do resultado já calculado.
            HaltFatigue(_playerOne);
            HaltFatigue(_playerTwo);

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

            int p1Puzzles = _puzzlesSolved[1];
            int p2Puzzles = _puzzlesSolved[2];

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
                // tempo de partida, não a última chegada: encerrar por prazo também conta
                TotalDuration = Mathf.Max(_elapsed, Mathf.Max(p1Time, p2Time)),
                HasDnf = r1.IsDnf || r2.IsDnf,
                ScoreResult = scoreResult,
            };
        }

        private static void ResumeFatigue(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            var fatigue = player.GetComponent<FatigueComponent>();
            if (fatigue != null)
            {
                fatigue.Resume();
            }
        }

        private static void HaltFatigue(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            var fatigue = player.GetComponent<FatigueComponent>();
            if (fatigue != null)
            {
                fatigue.Halt();
            }
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
            _elapsed = 0f;
            ResumeFatigue(_playerOne);
            ResumeFatigue(_playerTwo);
            _finalWindowTimeRemaining = 0f;
            _firstFinisherIndex = 0;
            _finalResults = default;
            _puzzlesSolved[1] = _puzzlesSolved[2] = 0;

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
