using System.Collections.Generic;
using UnityEngine;

namespace HealthyRace.Level
{
    /// <summary>
    /// Progresso de um jogador ao longo da rota da largada até a própria porta, de 0 a 1.
    /// A rota é a de <see cref="LevelRoutes"/> pelo caminho longo; quem está no corredor
    /// curto é projetado no trecho de rota mais próximo, que corre logo acima dele. Serve
    /// para dizer quem está na frente — a altura sozinha não diz, porque os andares vão
    /// e voltam.
    /// </summary>
    public sealed class RaceProgress
    {
        private readonly List<Vector2> _points = new();
        private readonly float[] _distance;
        private readonly float _length;

        public RaceProgress(LevelGrid grid, char spawn, char door)
        {
            var start = default(Vector2Int);
            var found = false;
            foreach (var cell in grid.Cells(spawn))
            {
                start = cell;
                found = true;
                break;
            }

            if (found)
            {
                // pelo caminho longo, para a projeção ter um trecho em cada andar
                var route = LevelRoutes.Find(
                    grid,
                    start,
                    c => grid[c.x, c.y] == door,
                    c => grid.IsShortCorridor(c.x, c.y)
                );
                if (route.Count == 0)
                {
                    route = LevelRoutes.Find(grid, start, c => grid[c.x, c.y] == door);
                }

                foreach (var step in route)
                {
                    _points.Add(new Vector2(step.Cell.x + 0.5f, step.Cell.y + 0.5f));
                }
            }

            _distance = new float[_points.Count];
            for (var i = 1; i < _points.Count; i++)
            {
                _distance[i] = _distance[i - 1] + Vector2.Distance(_points[i - 1], _points[i]);
            }

            _length = _points.Count > 0 ? _distance[_points.Count - 1] : 0f;
        }

        public int PointCount => _points.Count;

        public float Length => _length;

        /// <summary>
        /// Fração da rota já percorrida no ponto do mundo mais próximo de
        /// <paramref name="position"/>. Sem rota, 0.
        /// </summary>
        public float Fraction(Vector2 position)
        {
            if (_points.Count < 2 || _length <= 0f)
            {
                return 0f;
            }

            var best = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < _points.Count; i++)
            {
                var d = (_points[i] - position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }

            return Mathf.Clamp01(_distance[best] / _length);
        }
    }
}
