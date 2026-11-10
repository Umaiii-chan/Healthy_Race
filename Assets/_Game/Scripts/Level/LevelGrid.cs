using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace HealthyRace.Level
{
    /// <summary>Placa de instrução escrita no arquivo da fase.</summary>
    public readonly struct LevelSign
    {
        public readonly Vector2 Position;
        public readonly string Text;

        public LevelSign(Vector2 position, string text)
        {
            Position = position;
            Text = text;
        }
    }

    /// <summary>Enfeite sem colisão escrito no arquivo da fase.</summary>
    public readonly struct LevelDecor
    {
        public readonly Vector2Int Cell;
        public readonly string Kind;

        public LevelDecor(Vector2Int cell, string kind)
        {
            Cell = cell;
            Kind = kind;
        }
    }

    /// <summary>Trecho horizontal contínuo de um mesmo símbolo.</summary>
    public readonly struct LevelRun
    {
        public readonly int X0;
        public readonly int X1;
        public readonly int Y;

        public LevelRun(int x0, int x1, int y)
        {
            X0 = x0;
            X1 = x1;
            Y = y;
        }

        public int Width => X1 - X0 + 1;
    }

    /// <summary>
    /// Fase descrita em texto (gerada por <c>tools/levels</c>). Seções:
    /// <c>[mapa]</c> com a grade (linha de cima primeiro, 1 caractere = 1 tile),
    /// <c>[placas]</c> com <c>x y texto</c> (<c>|</c> quebra linha) e
    /// <c>[enfeites]</c> com <c>x y tipo</c>. Linhas iniciadas por <c>;</c> são
    /// comentário. Em memória, <c>y</c> cresce para cima, como no jogo.
    /// </summary>
    public sealed class LevelGrid
    {
        public const char Solid = '#';
        public const char Plank = '=';
        public const char RampUp = '/';
        public const char RampDown = '\\';
        public const char Mud = '~';
        public const char Spikes = '^';
        public const char Falling = 'w';
        public const char Lift = 'E';
        public const char LiftPath = ':';
        public const char Lever = 'L';
        public const char Gate = 'G';
        public const char Checkpoint = 'C';
        public const char Healthy = 'h';
        public const char Fast = 'f';
        public const char SpawnOne = '1';
        public const char SpawnTwo = '2';
        public const char DoorOne = 'P';
        public const char DoorTwo = 'Q';
        public const char Empty = '.';

        private readonly char[,] _cells;
        private readonly List<LevelSign> _signs;
        private readonly List<LevelDecor> _decor;

        private LevelGrid(char[,] cells, List<LevelSign> signs, List<LevelDecor> decor)
        {
            _cells = cells;
            _signs = signs;
            _decor = decor;
        }

        public int Width => _cells.GetLength(0);

        public int Height => _cells.GetLength(1);

        public IReadOnlyList<LevelSign> Signs => _signs;

        public IReadOnlyList<LevelDecor> Decor => _decor;

        /// <summary>Fora da grade conta como sólido: a moldura continua para fora.</summary>
        public char this[int x, int y] => InBounds(x, y) ? _cells[x, y] : Solid;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        /// <summary>Bloco cheio ou rampa: o que forma a estrutura.</summary>
        public static bool IsSolidShape(char c) => c == Solid || c == RampUp || c == RampDown;

        /// <summary>Tábua ou algo apoiado nela (lama e espinhos têm base de tábua).</summary>
        public static bool IsPlankLike(char c) => c == Plank || c == Mud || c == Spikes;

        /// <summary>Qualquer coisa em que se pisa, exceto as plataformas que caem.</summary>
        public bool IsFloor(int x, int y) => IsSolidShape(this[x, y]) || IsPlankLike(this[x, y]);

        public static LevelGrid Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("Arquivo de fase vazio.");
            }

            var rows = new List<string>();
            var signs = new List<LevelSign>();
            var decor = new List<LevelDecor>();
            var section = string.Empty;
            foreach (var raw in text.Replace("\r", string.Empty).Split('\n'))
            {
                var line = raw.TrimEnd();
                if (line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.StartsWith("[", StringComparison.Ordinal))
                {
                    section = line.Trim('[', ']').ToLowerInvariant();
                    continue;
                }

                switch (section)
                {
                    case "mapa":
                        rows.Add(line);
                        break;
                    case "placas":
                        signs.Add(ParseSign(line));
                        break;
                    case "enfeites":
                        decor.Add(ParseDecor(line));
                        break;
                }
            }

            if (rows.Count == 0)
            {
                throw new FormatException("A seção [mapa] não tem linhas.");
            }

            var width = rows[0].Length;
            var height = rows.Count;
            var cells = new char[width, height];
            for (var r = 0; r < height; r++)
            {
                if (rows[r].Length != width)
                {
                    throw new FormatException(
                        $"Linha {r + 1} do mapa tem {rows[r].Length} colunas; esperado {width}."
                    );
                }

                for (var x = 0; x < width; x++)
                {
                    cells[x, height - 1 - r] = rows[r][x];
                }
            }

            return new LevelGrid(cells, signs, decor);
        }

        private static LevelSign ParseSign(string line)
        {
            var parts = line.Split(new[] { ' ' }, 3);
            if (parts.Length < 3)
            {
                throw new FormatException($"Placa mal formada: '{line}'.");
            }

            return new LevelSign(
                new Vector2(ParseFloat(parts[0]), ParseFloat(parts[1])),
                parts[2].Replace('|', '\n')
            );
        }

        private static LevelDecor ParseDecor(string line)
        {
            var parts = line.Split(' ');
            if (parts.Length < 3)
            {
                throw new FormatException($"Enfeite mal formado: '{line}'.");
            }

            return new LevelDecor(
                new Vector2Int(
                    Mathf.RoundToInt(ParseFloat(parts[0])),
                    Mathf.RoundToInt(ParseFloat(parts[1]))
                ),
                parts[2]
            );
        }

        private static float ParseFloat(string value) =>
            float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

        public IEnumerable<Vector2Int> Cells(char symbol)
        {
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                if (_cells[x, y] == symbol)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }

        public int Count(char symbol)
        {
            var count = 0;
            foreach (var _ in Cells(symbol))
            {
                count++;
            }

            return count;
        }

        /// <summary>Trechos horizontais contínuos de células que satisfazem o filtro.</summary>
        public IEnumerable<LevelRun> Runs(Func<char, bool> match)
        {
            for (var y = 0; y < Height; y++)
            {
                var x = 0;
                while (x < Width)
                {
                    if (!match(_cells[x, y]))
                    {
                        x++;
                        continue;
                    }

                    var start = x;
                    while (x < Width && match(_cells[x, y]))
                    {
                        x++;
                    }

                    yield return new LevelRun(start, x - 1, y);
                }
            }
        }

        public IEnumerable<LevelRun> Runs(char symbol) => Runs(c => c == symbol);

        /// <summary>Altura do piso em que se fica de pé numa coluna, logo acima de <paramref name="y"/>.</summary>
        public float SurfaceTop(int x, int y)
        {
            var c = this[x, y];
            return IsPlankLike(c) || c == Falling ? y + 1f
                : IsSolidShape(c) ? y + 1f
                : y;
        }

        /// <summary>
        /// Base do primeiro teto acima de <paramref name="from"/> na coluna x (tábuas
        /// ocupam a metade de cima da célula).
        /// </summary>
        public float CeilingAbove(int x, float from)
        {
            for (var y = Mathf.FloorToInt(from); y < Height; y++)
            {
                var c = this[x, y];
                var bottom =
                    IsSolidShape(c) ? y
                    : IsPlankLike(c) || c == Falling ? y + 0.5f
                    : float.MaxValue;
                if (bottom >= from && bottom < float.MaxValue)
                {
                    return bottom;
                }
            }

            return Height;
        }

        /// <summary>
        /// Célula de corredor curto: o corredor baixo debaixo do caminho longo. O piso
        /// é uma tábua (ou o chão da moldura) e o teto é uma tábua logo acima, ou o vão
        /// das plataformas que caem. O topo de uma lombada embaixo de tábua não conta:
        /// a lombada é do caminho longo.
        /// </summary>
        public bool IsShortCorridor(int x, int y)
        {
            if (IsFloor(x, y) || this[x, y] == Falling || this[x, y] == Lift)
            {
                return false;
            }

            var below = this[x, y - 1];
            var corridorFloor = below == Plank || (y - 1 == 0 && below == Solid);
            return corridorFloor && IsCorridorCeiling(x, y + 1);
        }

        /// <summary>Tábua, plataforma que cai, ou o vão entre plataformas que caem.</summary>
        private bool IsCorridorCeiling(int x, int y)
        {
            var c = this[x, y];
            if (IsPlankLike(c) || c == Falling)
            {
                return true;
            }

            return c == Empty && (this[x - 1, y] == Falling || this[x + 1, y] == Falling);
        }
    }
}
