using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealthyRace.Level
{
    /// <summary>Como se chega a uma célula a partir da anterior na rota.</summary>
    public enum RouteMove
    {
        Start,
        Walk,
        Fall,
        Jump,
        Lift,
    }

    /// <summary>Passo de uma rota: a célula onde ficam os pés e como se chegou nela.</summary>
    public readonly struct RouteStep
    {
        public readonly Vector2Int Cell;
        public readonly RouteMove Move;

        public RouteStep(Vector2Int cell, RouteMove move)
        {
            Cell = cell;
            Move = move;
        }

        public override string ToString() => $"{Move}{Cell}";
    }

    /// <summary>
    /// Busca de rotas na grade da fase para um jogador sozinho, com o orçamento de
    /// pulo do pior Bem-Estar: sobe no máximo <see cref="MaxRise"/> tile e cruza no
    /// máximo <see cref="MaxGap"/> tile vazio. Serve para provar que cada jogador
    /// termina sem depender do outro, pelo caminho longo e pelo curto.
    /// </summary>
    public static class LevelRoutes
    {
        public const int MaxRise = 1;
        public const int MaxGap = 1;

        /// <summary>
        /// Célula onde o jogador pode ficar de pé: livre, com piso embaixo (ou o próprio
        /// elevador) e sem bloco na célula de cima. Espinhos não contam como piso.
        /// </summary>
        public static bool IsStandable(LevelGrid grid, int x, int y)
        {
            if (!grid.InBounds(x, y))
            {
                return false;
            }

            var c = grid[x, y];
            if (c == LevelGrid.Lift)
            {
                return true;
            }

            if (!IsPassable(c))
            {
                return false;
            }

            var below = grid[x, y - 1];
            var hasFloor =
                LevelGrid.IsSolidShape(below)
                || below == LevelGrid.Plank
                || below == LevelGrid.Mud
                || below == LevelGrid.Falling;
            return hasFloor && !LevelGrid.IsSolidShape(grid[x, y + 1]);
        }

        /// <summary>Célula que o corpo atravessa (portão conta como aberto: a alavanca vem antes).</summary>
        public static bool IsPassable(char c) =>
            !LevelGrid.IsSolidShape(c) && !LevelGrid.IsPlankLike(c) && c != LevelGrid.Falling;

        /// <summary>
        /// Rota mais curta (em passos) de <paramref name="start"/> até uma célula que
        /// satisfaça <paramref name="goal"/>; vazia se não houver. Células em que
        /// <paramref name="avoid"/> é verdadeiro ficam proibidas.
        /// </summary>
        public static List<RouteStep> Find(
            LevelGrid grid,
            Vector2Int start,
            Func<Vector2Int, bool> goal,
            Func<Vector2Int, bool> avoid = null
        )
        {
            var previous = new Dictionary<Vector2Int, RouteStep>();
            var queue = new Queue<Vector2Int>();
            previous[start] = new RouteStep(start, RouteMove.Start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (goal(current))
                {
                    return Unwind(previous, start, current);
                }

                foreach (var next in Neighbours(grid, current))
                {
                    if (previous.ContainsKey(next.Cell) || (avoid != null && avoid(next.Cell)))
                    {
                        continue;
                    }

                    previous[next.Cell] = new RouteStep(current, next.Move);
                    queue.Enqueue(next.Cell);
                }
            }

            return new List<RouteStep>();
        }

        private static List<RouteStep> Unwind(
            Dictionary<Vector2Int, RouteStep> previous,
            Vector2Int start,
            Vector2Int end
        )
        {
            var route = new List<RouteStep>();
            var cell = end;
            while (cell != start)
            {
                var link = previous[cell];
                route.Add(new RouteStep(cell, link.Move));
                cell = link.Cell;
            }

            route.Add(new RouteStep(start, RouteMove.Start));
            route.Reverse();
            return route;
        }

        public static IEnumerable<RouteStep> Neighbours(LevelGrid grid, Vector2Int from)
        {
            var x = from.x;
            var y = from.y;

            // elevador: de pé nele, sobe até o fim do curso
            if (grid[x, y] == LevelGrid.Lift)
            {
                var top = y;
                while (grid[x, top + 1] == LevelGrid.LiftPath)
                {
                    top++;
                }

                // no alto do curso o próprio elevador é o piso
                if (top > y && IsPassable(grid[x, top + 1]))
                {
                    yield return new RouteStep(new Vector2Int(x, top + 1), RouteMove.Lift);
                }
            }

            foreach (var dir in new[] { -1, 1 })
            {
                // andar (inclui subir ou descer uma rampa de um tile)
                if (IsStandable(grid, x + dir, y))
                {
                    yield return new RouteStep(new Vector2Int(x + dir, y), RouteMove.Walk);
                }
                else if (IsRampUp(grid, x + dir, y, dir) && IsStandable(grid, x + dir, y + 1))
                {
                    yield return new RouteStep(new Vector2Int(x + dir, y + 1), RouteMove.Walk);
                }
                else if (IsRampUp(grid, x + dir, y - 1, -dir) && IsStandable(grid, x + dir, y - 1))
                {
                    yield return new RouteStep(new Vector2Int(x + dir, y - 1), RouteMove.Walk);
                }
                else if (IsPassable(grid[x + dir, y]) && !IsStandable(grid, x + dir, y))
                {
                    // cair: desce pela coluna vizinha até achar piso
                    var fy = y - 1;
                    while (
                        fy > 0 && IsPassable(grid[x + dir, fy]) && !IsStandable(grid, x + dir, fy)
                    )
                    {
                        fy--;
                    }

                    if (IsStandable(grid, x + dir, fy))
                    {
                        yield return new RouteStep(new Vector2Int(x + dir, fy), RouteMove.Fall);
                    }
                }

                // pular: até MaxGap células vazias no meio e no máximo MaxRise para cima
                for (var dx = 1; dx <= MaxGap + 1; dx++)
                {
                    for (var dy = -2; dy <= MaxRise; dy++)
                    {
                        var tx = x + dir * dx;
                        var ty = y + dy;
                        if ((dx == 1 && dy <= 0) || !IsStandable(grid, tx, ty))
                        {
                            continue;
                        }

                        if (JumpIsClear(grid, x, y, dir, dx, ty))
                        {
                            yield return new RouteStep(new Vector2Int(tx, ty), RouteMove.Jump);
                        }
                    }
                }
            }
        }

        /// <summary>A rampa na célula sobe no sentido <paramref name="dir"/>.</summary>
        private static bool IsRampUp(LevelGrid grid, int x, int y, int dir) =>
            grid[x, y] == (dir > 0 ? LevelGrid.RampUp : LevelGrid.RampDown);

        /// <summary>
        /// O arco do pulo precisa de duas células livres acima da saída (o corpo tem
        /// 1,25 de altura e sobe até ~1,3 no pior Bem-Estar) e de passagem livre na
        /// altura mais alta do arco em cada coluna até o destino.
        /// </summary>
        private static bool JumpIsClear(LevelGrid grid, int x, int y, int dir, int dx, int ty)
        {
            var top = Mathf.Max(y, ty) + 1;
            if (!IsPassable(grid[x, y + 1]) || !IsPassable(grid[x, top]))
            {
                return false;
            }

            // pulo com vão precisa do arco inteiro: mais uma célula livre acima, senão
            // o teto achata o pulo e ele não chega ao outro lado
            if (dx >= 2)
            {
                for (var i = 0; i < dx; i++)
                {
                    if (!IsPassable(grid[x + dir * i, top + 1]))
                    {
                        return false;
                    }
                }
            }

            for (var i = 1; i <= dx; i++)
            {
                var cx = x + dir * i;
                for (var cy = Mathf.Min(y, ty); cy <= top; cy++)
                {
                    if (cx == x + dir * dx && cy < ty)
                    {
                        continue;
                    }

                    if (!IsPassable(grid[cx, cy]) && grid[cx, cy] != LevelGrid.Lift)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
