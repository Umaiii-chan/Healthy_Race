using System;
using System.Globalization;
using System.IO;
using System.Text;
using HealthyRace.Player;
using UnityEngine;

namespace HealthyRace.Level
{
    /// <summary>
    /// Registro local de uma partida para playtest: um arquivo JSON Lines por partida em
    /// <c>persistentDataPath/partidas</c>. Grava largada, amostras a cada segundo
    /// (posição, progresso, Bem-Estar, quem lidera), entrada e saída do corredor curto,
    /// coletas, fadigas, espinhos, alavancas, chegadas e o resultado. Não grava nenhum
    /// dado pessoal. Desligado em modo batch (testes) e quando o jogador desliga.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchRecorder : MonoBehaviour
    {
        public const string PrefEnabled = "HealthyRace_MatchLog";
        public const float SampleInterval = 1f;

        private readonly StringBuilder _line = new();
        private StreamWriter _writer;
        private Func<float> _clock;
        private LevelGrid _grid;
        private Transform[] _players;
        private Func<int, int> _wellbeing;
        private Func<int, SnackState> _snack;
        private RaceProgress[] _progress;
        private bool[] _inCorridor;
        private float _nextSample;
        private int _leader;
        private int _leadChanges;

        /// <summary>Diretório de saída; testes podem trocar por um temporário.</summary>
        public static string OutputDirectory { get; set; }

        /// <summary>Liga a gravação mesmo em modo batch (testes).</summary>
        public static bool ForceEnabled { get; set; }

        public static bool Enabled
        {
            get
            {
                if (ForceEnabled)
                {
                    return true;
                }

                return !Application.isBatchMode && PlayerPrefs.GetInt(PrefEnabled, 1) == 1;
            }
        }

        public static string DefaultDirectory =>
            Path.Combine(Application.persistentDataPath, "partidas");

        public string FilePath { get; private set; }

        public int LeadChanges => _leadChanges;

        public bool IsRecording => _writer != null;

        /// <summary>
        /// Abre o arquivo e grava a largada. <paramref name="clock"/> é o tempo de
        /// partida; os jogadores vêm na ordem P1, P2.
        /// </summary>
        public void Begin(
            LevelGrid grid,
            float targetTimeSeconds,
            Func<float> clock,
            Transform playerOne,
            Transform playerTwo,
            Func<int, int> wellbeing,
            Func<int, SnackState> snack
        )
        {
            if (!Enabled || _writer != null)
            {
                return;
            }

            _grid = grid;
            _clock = clock;
            _players = new[] { null, playerOne, playerTwo };
            _wellbeing = wellbeing;
            _snack = snack;
            _inCorridor = new bool[3];
            _progress = new[]
            {
                null,
                new RaceProgress(grid, LevelGrid.SpawnOne, LevelGrid.DoorOne),
                new RaceProgress(grid, LevelGrid.SpawnTwo, LevelGrid.DoorTwo),
            };

            try
            {
                var directory = string.IsNullOrEmpty(OutputDirectory)
                    ? DefaultDirectory
                    : OutputDirectory;
                Directory.CreateDirectory(directory);
                var stamp = DateTime.Now.ToString(
                    "yyyy-MM-dd_HHmmss",
                    CultureInfo.InvariantCulture
                );
                FilePath = Path.Combine(directory, $"{stamp}_fase{grid.LevelNumber:00}.jsonl");
                _writer = new StreamWriter(FilePath, false, new UTF8Encoding(false));
            }
            catch (Exception exception)
            {
                // o registro é auxiliar: falhar aqui nunca pode parar a partida
                Debug.LogWarning($"MatchRecorder: registro desligado ({exception.Message}).");
                _writer = null;
                return;
            }

            Record("largada")
                .Field("fase", grid.LevelNumber)
                .Field("nome", grid.LevelName)
                .Field("tempo_alvo", targetTimeSeconds)
                .Field("versao", Application.version)
                .Field("data", DateTime.Now.ToString("s", CultureInfo.InvariantCulture))
                .Write();
            _nextSample = 0f;
        }

        public void Food(int player, string foodId, bool healthy, int wellbeingAfter)
        {
            Record("coleta")
                .Field("jogador", player)
                .Field("alimento", foodId)
                .Field("saudavel", healthy)
                .Field("bem_estar", wellbeingAfter)
                .Write();
        }

        public void Fatigue(int player) => Record("fadiga").Field("jogador", player).Write();

        public void Spike(int player) => Record("espinho").Field("jogador", player).Write();

        public void Lever(int player) => Record("alavanca").Field("jogador", player).Write();

        public void Finish(int player, float seconds) =>
            Record("chegada").Field("jogador", player).Field("tempo", seconds).Write();

        /// <summary>Resultado de um jogador; chame uma vez por jogador no fim.</summary>
        public void Result(
            int player,
            bool finished,
            float seconds,
            int placement,
            int score,
            int wellbeing,
            int healthyCount,
            int ultraCount,
            int fatigueEpisodes,
            int stars = 0
        )
        {
            Record("resultado")
                .Field("jogador", player)
                .Field("concluiu", finished)
                .Field("tempo", seconds)
                .Field("colocacao", placement)
                .Field("pontos", score)
                .Field("bem_estar", wellbeing)
                .Field("saudaveis", healthyCount)
                .Field("ultra", ultraCount)
                .Field("fadigas", fatigueEpisodes)
                .Field("estrelas", stars)
                .Write();
        }

        /// <summary>Grava o fim (vencedor 0 = empate) e fecha o arquivo.</summary>
        public void End(int winner)
        {
            if (_writer == null)
            {
                return;
            }

            Sample();
            Record("fim")
                .Field("vencedor", winner)
                .Field("trocas_de_lideranca", _leadChanges)
                .Write();
            Close();
        }

        private void Update()
        {
            if (_writer == null || _players == null)
            {
                return;
            }

            for (var i = 1; i <= 2; i++)
            {
                if (_players[i] == null)
                {
                    continue;
                }

                var p = _players[i].position;
                var x = Mathf.FloorToInt(p.x);
                var y = Mathf.FloorToInt(p.y + 0.1f);
                var inside =
                    x >= 0
                    && y >= 0
                    && x < _grid.Width
                    && y < _grid.Height
                    && _grid.IsShortCorridor(x, y);
                if (inside != _inCorridor[i])
                {
                    _inCorridor[i] = inside;
                    Record(inside ? "corredor_entrada" : "corredor_saida")
                        .Field("jogador", i)
                        .Field("x", x)
                        .Field("y", y)
                        .Write();
                }
            }

            if (Now() >= _nextSample)
            {
                _nextSample = Now() + SampleInterval;
                Sample();
            }
        }

        private void Sample()
        {
            if (_writer == null)
            {
                return;
            }

            var p1 = Progress(1);
            var p2 = Progress(2);
            var leader = Mathf.Abs(p1 - p2) < 0.005f ? _leader : (p1 > p2 ? 1 : 2);
            if (_leader != 0 && leader != _leader)
            {
                _leadChanges++;
            }

            _leader = leader;
            var entry = Record("amostra").Field("lider", leader);
            for (var i = 1; i <= 2; i++)
            {
                var pos = _players[i] != null ? (Vector2)_players[i].position : Vector2.zero;
                entry
                    .Field($"p{i}_x", pos.x)
                    .Field($"p{i}_y", pos.y)
                    .Field($"p{i}_progresso", i == 1 ? p1 : p2)
                    .Field($"p{i}_bem_estar", _wellbeing != null ? _wellbeing(i) : 0)
                    .Field($"p{i}_lanche", SnackName(_snack != null ? _snack(i) : SnackState.None));
            }

            entry.Write();
        }

        private static string SnackName(SnackState state) =>
            state switch
            {
                SnackState.Rush => "pico",
                SnackState.Crash => "queda",
                _ => "nenhum",
            };

        private float Progress(int player) =>
            _players[player] != null ? _progress[player].Fraction(_players[player].position) : 0f;

        private float Now() => _clock != null ? _clock() : Time.time;

        private Entry Record(string kind)
        {
            _line.Clear();
            _line
                .Append("{\"t\":")
                .Append(Number(Now()))
                .Append(",\"evento\":\"")
                .Append(kind)
                .Append('"');
            return new Entry(this);
        }

        private void Close()
        {
            try
            {
                _writer?.Flush();
                _writer?.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MatchRecorder: falha ao fechar ({exception.Message}).");
            }

            _writer = null;
        }

        private void OnDestroy()
        {
            // partida abandonada (menu, reinício): fecha o que já foi gravado
            if (_writer != null)
            {
                Record("abandono").Write();
                Close();
            }
        }

        private static string Number(float value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);

        private readonly struct Entry
        {
            private readonly MatchRecorder _owner;

            public Entry(MatchRecorder owner) => _owner = owner;

            public Entry Field(string name, string value)
            {
                var b = _owner._line.Append(",\"").Append(name).Append("\":\"");
                foreach (var c in value ?? string.Empty)
                {
                    if (c == '"' || c == '\\')
                    {
                        b.Append('\\');
                    }

                    b.Append(c);
                }

                b.Append('"');
                return this;
            }

            public Entry Field(string name, float value)
            {
                _owner._line.Append(",\"").Append(name).Append("\":").Append(Number(value));
                return this;
            }

            public Entry Field(string name, int value)
            {
                _owner
                    ._line.Append(",\"")
                    .Append(name)
                    .Append("\":")
                    .Append(value.ToString(CultureInfo.InvariantCulture));
                return this;
            }

            public Entry Field(string name, bool value)
            {
                _owner
                    ._line.Append(",\"")
                    .Append(name)
                    .Append("\":")
                    .Append(value ? "true" : "false");
                return this;
            }

            public void Write()
            {
                if (_owner._writer == null)
                {
                    return;
                }

                _owner._line.Append('}');
                try
                {
                    _owner._writer.WriteLine(_owner._line.ToString());
                    // grava no instante: partida interrompida conserva o que aconteceu
                    _owner._writer.Flush();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"MatchRecorder: falha ao gravar ({exception.Message}).");
                }
            }
        }
    }
}
