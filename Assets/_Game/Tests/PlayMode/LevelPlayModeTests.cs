using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthyRace.Core;
using HealthyRace.Level;
using HealthyRace.Player;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    /// <summary>
    /// Fase 1 com física de verdade: rampas, elevador, alavanca e a corrida inteira
    /// feita por um robô que segue as rotas da grade, um jogador pelo caminho longo e
    /// o outro pelo curto. Os tempos medidos vão para o log do teste.
    /// </summary>
    public sealed class LevelPlayModeTests
    {
        private const float Step = 0.02f;

        private Vector2 _originalGravity;
        private readonly float[] _times = new float[2];

        [SetUp]
        public void SetUp()
        {
            _originalGravity = Physics2D.gravity;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureDeltaTime = 0f;
            GameFlow.SetPaused(false);
            var active = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("LevelPlayModeTests_Empty_" + Time.frameCount);
            SceneManager.SetActiveScene(empty);
            if (active.IsValid() && active.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(active);
            }

            Checkpoint.ResetRegistry();
            Physics2D.gravity = _originalGravity;
        }

        private static IEnumerator LoadLevel(int level = 1)
        {
            var loadOp = SceneManager.LoadSceneAsync(
                GameFlow.SceneForLevel(level),
                LoadSceneMode.Single
            );
            while (!loadOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);
        }

        private static PlayerMovement TakeControl(GameObject player)
        {
            player.GetComponent<PlayerController>().enabled = false;
            return player.GetComponent<PlayerMovement>();
        }

        private static void Teleport(GameObject player, Vector2 position)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            rb.position = position;
            player.transform.position = position;
        }

        private static float Feet(GameObject player) =>
            player.GetComponent<Collider2D>().bounds.min.y;

        // =====================================================================
        // Mecanismos isolados
        // =====================================================================
        [UnityTest]
        public IEnumerator Spawn_BothPlayersStartGroundedInsideTheLevel()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            foreach (var player in new[] { bootstrap.PlayerOne, bootstrap.PlayerTwo })
            {
                Assert.That(player.GetComponent<PlayerMovement>().IsGrounded, Is.True);
                var p = player.transform.position;
                Assert.That(p.x, Is.InRange(1f, bootstrap.Grid.Width - 1f));
                Assert.That(p.y, Is.InRange(1f, bootstrap.Grid.Height - 1f));
            }

            var camera = bootstrap.CameraController.GetComponent<Camera>();
            Assert.That(camera.rect.height, Is.LessThan(1f), "o HUD tem faixa própria");
        }

        [UnityTest]
        public IEnumerator Ramp_IsClimbedByWalkingAlone()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var grid = bootstrap.Grid;
            // a rampa '/' do trecho juntado do primeiro andar
            var ramp = grid.Cells(LevelGrid.RampUp).Where(c => c.y == 1).OrderBy(c => c.x).First();
            var player = bootstrap.PlayerOne;
            var movement = TakeControl(player);
            Teleport(player, new Vector2(ramp.x - 1.5f, 1f + 0.7f));
            yield return new WaitForSeconds(0.3f);

            movement.SetHorizontalInput(1f);
            var top = 0f;
            for (var t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                Assert.That(movement.Velocity.y, Is.LessThan(9f), "subir a rampa não é pular");
                top = Mathf.Max(top, Feet(player));
                yield return null;
            }

            Assert.That(top, Is.GreaterThan(2.9f), "sobe os dois degraus da rampa andando");
            Assert.That(movement.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator Lift_CarriesThePlayerToTheFloorAbove()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var grid = bootstrap.Grid;
            var run = grid.Runs(LevelGrid.Lift).OrderBy(r => r.Y).First();
            var top = run.Y;
            while (grid[run.X0, top + 1] == LevelGrid.LiftPath)
            {
                top++;
            }

            var player = bootstrap.PlayerOne;
            var movement = TakeControl(player);
            // chega andando pelo corredor: o elevador em repouso fica rente ao piso
            Teleport(player, new Vector2(run.X0 - 1.5f, run.Y + 0.7f));
            yield return new WaitForSeconds(0.2f);
            movement.SetHorizontalInput(1f);
            for (
                var t = 0f;
                t < 2f && player.transform.position.x < run.X0 + 0.9f;
                t += Time.deltaTime
            )
            {
                yield return null;
            }

            movement.SetHorizontalInput(0f);
            Assert.That(
                player.transform.position.x,
                Is.GreaterThan(run.X0 + 0.5f),
                "entra andando"
            );
            for (var t = 0f; t < 6f && Feet(player) < top + 0.9f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.That(Feet(player), Is.GreaterThan(top + 0.9f), "chega ao piso de cima");
        }

        [UnityTest]
        public IEnumerator Lever_OpensItsGateAfterTheDelay()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var (lever, gate) = bootstrap.Gates[0];
            Assert.That(gate.IsOpen, Is.False);

            var player = bootstrap.PlayerOne;
            TakeControl(player);
            Teleport(player, lever.transform.position);
            yield return new WaitForSeconds(0.3f);
            Assert.That(lever.IsActivated, Is.True);
            Assert.That(
                bootstrap.MatchController.PuzzlesSolvedBy(1),
                Is.EqualTo(1),
                "quem puxa a alavanca ganha os pontos do mecanismo"
            );
            Assert.That(gate.IsOpen, Is.False, "o portão demora para abrir");

            yield return new WaitForSeconds(LevelBootstrap.GateOpenDelay + 0.2f);
            Assert.That(gate.IsOpen, Is.True);
        }

        // =====================================================================
        // Corrida inteira
        // =====================================================================
        [UnityTest]
        public IEnumerator Race_LongAndShortRoutes_BothFinishAlone_AtFullWellbeing(
            [Values(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)] int level
        )
        {
            yield return Race(
                level,
                startingWellbeing: -1,
                label: $"fase {level}, bem-estar cheio"
            );
            // Com o lanche "pico e queda" o atalho pode ganhar ou perder: os dois têm de
            // terminar (Race já exige); os tempos saem no log CORRIDA para o balanceamento.
        }

        [UnityTest]
        public IEnumerator Snack_GivesARush_ThenACrash_ThatPasses()
        {
            yield return LoadLevel();
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var food = bootstrap.Foods.First(f =>
                f.FoodData.Category == HealthyRace.Collectibles.FoodCategory.UltraProcessed
            );
            var player = bootstrap.PlayerOne;
            var movement = TakeControl(player);
            var normal = movement.EffectiveMoveSpeed;
            Teleport(player, food.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            var balance = HealthyRace.Core.BalanceConfig.Current;
            Assert.That(movement.IsRushing, Is.True, "o lanche dá o pico");
            Assert.That(movement.EffectiveMoveSpeed, Is.GreaterThan(normal * 1.1f));
            yield return new WaitForSeconds(balance.RushSeconds + 0.2f);
            Assert.That(movement.IsCrashing, Is.True, "depois do pico vem a queda");
            Assert.That(movement.EffectiveMoveSpeed, Is.LessThan(normal * 0.8f));
            yield return new WaitForSeconds(balance.CrashSeconds + 0.2f);
            Assert.That(movement.SnackState, Is.EqualTo(SnackState.None), "a queda passa");
        }

        [UnityTest]
        public IEnumerator Race_LongAndShortRoutes_BothFinishAlone_FromZeroWellbeing()
        {
            yield return Race(1, startingWellbeing: 0, label: "fase 1, bem-estar zero");
        }

        /// <summary>
        /// P1 pelo caminho longo, P2 pelo curto (comendo todo o fast-food do corredor).
        /// O tempo de jogo avança em passos fixos, então o teste roda mais rápido que
        /// o relógio e os tempos medidos são os do jogo.
        /// </summary>
        private IEnumerator Race(int level, int startingWellbeing, string label)
        {
            yield return LoadLevel(level);
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            var grid = bootstrap.Grid;
            var longRoute = LevelRoutes.Find(
                grid,
                grid.Cells(LevelGrid.SpawnOne).Single(),
                c => grid[c.x, c.y] == LevelGrid.DoorOne,
                c => grid.IsShortCorridor(c.x, c.y)
            );
            var shortRoute = ShortRoute(grid, grid.Cells(LevelGrid.SpawnTwo).Single());
            Assert.That(longRoute, Is.Not.Empty);
            Assert.That(shortRoute, Is.Not.Empty);

            if (startingWellbeing >= 0)
            {
                bootstrap.PlayerOne.GetComponent<WellbeingComponent>().SetValue(startingWellbeing);
                bootstrap.PlayerTwo.GetComponent<WellbeingComponent>().SetValue(startingWellbeing);
            }

            var bots = new[]
            {
                new RouteBot(bootstrap.PlayerOne, longRoute, "P1 longo", c =>
                    LevelRoutes.Find(grid, c, d => grid[d.x, d.y] == LevelGrid.DoorOne)
                ),
                new RouteBot(
                    bootstrap.PlayerTwo,
                    shortRoute,
                    "P2 curto",
                    // desviado, continua comendo o fast-food que falta: é a rota do curto
                    c =>
                        ShortRoute(
                            grid,
                            c,
                            RemainingFastFood(bootstrap).Where(f => (f.y - 1) / 6 >= (c.y - 1) / 6),
                            strict: false
                        )
                ),
            };
            var doors = new[] { bootstrap.PlayerOneDoor, bootstrap.PlayerTwoDoor };
            var times = _times;
            times[0] = times[1] = 0f;
            var start = Time.time;

            Time.captureDeltaTime = Step;
            var limit = LevelBootstrap.MatchTimeLimitSeconds - 5f;
            while (Time.time - start < limit)
            {
                for (var i = 0; i < 2; i++)
                {
                    if (times[i] <= 0f && doors[i].HasPlayerFinished(i + 1))
                    {
                        times[i] = Time.time - start;
                    }

                    if (times[i] <= 0f)
                    {
                        bots[i].Tick();
                        Assert.That(bots[i].Failure, Is.Null, bots[i].Failure);
                    }
                }

                if (times.All(t => t > 0f))
                {
                    break;
                }

                yield return new WaitForFixedUpdate();
            }

            Time.captureDeltaTime = 0f;
            yield return null;
            var results = bootstrap.MatchController.FinalResults;
            Debug.Log(
                $"PONTOS ({label}): longo {results.Player1.Score.FinalScore} "
                    + $"(saudáveis {results.Player1.Score.HealthyPoints}, mecanismos {results.Player1.Score.PuzzlePoints}, "
                    + $"tempo {results.Player1.Score.TimeBonus}, chegada {results.Player1.Score.ArrivalBonus}, "
                    + $"bem-estar {results.Player1.Score.WellbeingBonus}, ultra -{results.Player1.Score.UltraPenalty}, "
                    + $"fadiga -{results.Player1.Score.FatiguePenalty}) | curto {results.Player2.Score.FinalScore} "
                    + $"(saudáveis {results.Player2.Score.HealthyPoints}, tempo {results.Player2.Score.TimeBonus}, "
                    + $"chegada {results.Player2.Score.ArrivalBonus}, bem-estar {results.Player2.Score.WellbeingBonus}, "
                    + $"ultra -{results.Player2.Score.UltraPenalty}, fadiga -{results.Player2.Score.FatiguePenalty})"
            );
            var p1 = bootstrap.PlayerOne.GetComponent<WellbeingComponent>().CurrentValue;
            var p2 = bootstrap.PlayerTwo.GetComponent<WellbeingComponent>().CurrentValue;
            Debug.Log(
                $"CORRIDA ({label}): longo {times[0]:0.0} s (Bem-Estar {p1}, "
                    + $"{bots[0].Replans} desvios), curto {times[1]:0.0} s (Bem-Estar {p2}, "
                    + $"{bots[1].Replans} desvios) | desvios P1: {string.Join("; ", bots[0].Detours)}"
                    + $" | desvios P2: {string.Join("; ", bots[1].Detours)}"
            );
            Assert.That(times[0], Is.GreaterThan(0f), $"P1 não terminou: {bots[0].Status}");
            Assert.That(times[1], Is.GreaterThan(0f), $"P2 não terminou: {bots[1].Status}");
        }

        /// <summary>
        /// Rota do caminho curto: passa por todos os fast-foods do corredor, na ordem
        /// em que a corrida os encontra (andares pares para a direita, ímpares para a
        /// esquerda), e termina na porta do P2.
        /// </summary>
        private static List<RouteStep> ShortRoute(LevelGrid grid, Vector2Int start) =>
            ShortRoute(grid, start, grid.Cells(LevelGrid.Fast));

        /// <summary>Fast-food ainda não comido, em células da grade.</summary>
        private static IEnumerable<Vector2Int> RemainingFastFood(LevelBootstrap bootstrap) =>
            bootstrap
                .Foods.Where(f =>
                    f != null
                    && f.gameObject.activeSelf
                    && f.FoodData.Category == HealthyRace.Collectibles.FoodCategory.UltraProcessed
                )
                .Select(f => new Vector2Int(
                    Mathf.FloorToInt(f.transform.position.x),
                    Mathf.FloorToInt(f.transform.position.y)
                ));

        private static List<RouteStep> ShortRoute(
            LevelGrid grid,
            Vector2Int start,
            IEnumerable<Vector2Int> fastFood,
            bool strict = true
        )
        {
            var waypoints = fastFood
                .OrderBy(c => c.y)
                .ThenBy(c => ((c.y - 1) / 6) % 2 == 0 ? c.x : -c.x)
                .ToList();
            var route = new List<RouteStep> { new(start, RouteMove.Start) };
            var from = start;
            foreach (var waypoint in waypoints)
            {
                var leg = LevelRoutes.Find(grid, from, c => c == waypoint);
                if (!strict && leg.Count == 0)
                {
                    continue;
                }

                Assert.That(leg, Is.Not.Empty, $"sem trecho de {from} até {waypoint}");
                route.AddRange(leg.Skip(1));
                from = waypoint;
            }

            var last = LevelRoutes.Find(grid, from, c => grid[c.x, c.y] == LevelGrid.DoorTwo);
            Assert.That(last, Is.Not.Empty);
            route.AddRange(last.Skip(1));
            return route;
        }

        /// <summary>
        /// Segue uma rota da grade com os mesmos comandos que um jogador dá: andar,
        /// pular e esperar o elevador. Dirige no ar para pousar no tile certo.
        /// </summary>
        private sealed class RouteBot
        {
            private const float StuckSeconds = 8f;
            private const float RetrySeconds = 3f;

            private readonly GameObject _player;
            private readonly PlayerMovement _movement;
            private readonly System.Func<Vector2Int, List<RouteStep>> _replan;
            private List<RouteStep> _route;
            private readonly string _name;
            private int _index = 1;
            private bool _jumped;
            private bool _leftGround;
            private float _jumpWait;
            private bool _retried;
            private float _stepTime;

            public RouteBot(
                GameObject player,
                List<RouteStep> route,
                string name,
                System.Func<Vector2Int, List<RouteStep>> replan
            )
            {
                _player = player;
                _movement = TakeControl(player);
                _route = route;
                _name = name;
                _replan = replan;
            }

            /// <summary>Quantas vezes caiu fora da rota (plataforma, espinho) e refez o caminho.</summary>
            public int Replans { get; private set; }

            public List<string> Detours { get; } = new();

            public string Failure { get; private set; }

            private static string LiftsStatus()
            {
                var text = "elevadores:";
                foreach (
                    var sensor in Object.FindObjectsByType<HealthyRace.Puzzles.LiftRiderSensor>(
                        FindObjectsSortMode.None
                    )
                )
                {
                    var platform = sensor.GetComponent<HealthyRace.Puzzles.ActivatedPlatform>();
                    text +=
                        $" [{(Vector2)sensor.transform.position} ativo {platform.IsActive} "
                        + $"passageiro {sensor.HasRider}]";
                }

                return text;
            }

            private bool IsLift(Vector2Int cell) =>
                Object.FindFirstObjectByType<LevelBootstrap>().Grid[cell.x, cell.y]
                == LevelGrid.Lift;

            public string Status =>
                _index < _route.Count
                    ? $"{_name} no passo {_index}/{_route.Count} ({_route[_index]}), "
                        + $"em {(Vector2)_player.transform.position}, chão {_movement.IsGrounded}, "
                        + $"vel {_movement.Velocity}, rampa {_movement.IsOnSlope}, "
                        + $"pulou {_jumped}/{_leftGround}, mov {_movement.IsMovementEnabled}, "
                        + LiftsStatus()
                    : $"{_name} terminou a rota";

            public void Tick()
            {
                if (_index >= _route.Count)
                {
                    // fim da rota: fica dentro da porta
                    _movement.SetHorizontalInput(0f);
                    return;
                }

                var step = _route[_index];
                var previous = _route[_index - 1].Cell;
                var x = _player.transform.position.x;
                var feet = Feet(_player);
                var tx = step.Cell.x + 0.5f;
                var ty = step.Cell.y;
                var steer = Mathf.Clamp((tx - x) * 2.5f, -1f, 1f);
                var grounded = _movement.IsGrounded;

                // caiu da plataforma ou voltou ao checkpoint: refaz a rota dali
                if (
                    grounded
                    && step.Move != RouteMove.Lift
                    && Mathf.Min(Mathf.Abs(feet - ty), Mathf.Abs(feet - previous.y)) > 1.5f
                )
                {
                    var here = new Vector2Int(Mathf.FloorToInt(x), Mathf.RoundToInt(feet));
                    var fresh = _replan(here);
                    if (fresh.Count > 1)
                    {
                        _route = fresh;
                        _index = 1;
                        _jumped = false;
                        _leftGround = false;
                        _stepTime = 0f;
                        Replans++;
                        Detours.Add($"{step} em {here}");
                        return;
                    }
                }

                var atTarget = grounded && Mathf.Abs(feet - ty) < 0.6f && Mathf.Abs(tx - x) < 0.5f;
                if (step.Move != RouteMove.Lift && IsLift(step.Cell))
                {
                    // o elevador parte assim que se pisa nele: basta estar em cima
                    atTarget = Mathf.Abs(tx - x) < 1f && feet > ty - 0.6f && grounded;
                }

                var arrived = false;

                switch (step.Move)
                {
                    case RouteMove.Walk:
                        _movement.SetHorizontalInput(Mathf.Sign(tx - x));
                        arrived = Mathf.Abs(tx - x) < 0.35f;
                        break;
                    case RouteMove.Fall:
                        _movement.SetHorizontalInput(steer);
                        arrived = atTarget;
                        break;
                    case RouteMove.Jump:
                        // um "pulo" que dá para andar (sem vão de verdade) também chega
                        arrived = atTarget && (_leftGround || Mathf.Abs(feet - ty) < 0.3f);
                        if (arrived)
                        {
                            break;
                        }

                        if (!_jumped)
                        {
                            // vão de verdade: decola da metade de trás da célula
                            var dir = Mathf.Sign(step.Cell.x - previous.x);
                            var gap = Mathf.Abs(step.Cell.x - previous.x) >= 2;
                            var px = previous.x + 0.5f - (gap ? dir * 0.25f : 0f);
                            _movement.SetHorizontalInput(Mathf.Clamp((px - x) * 2.5f, -1f, 1f));
                            if (grounded && Mathf.Abs(px - x) < 0.4f)
                            {
                                _movement.RequestJump();
                                _jumped = true;
                                _jumpWait = 0f;
                            }
                        }
                        else
                        {
                            _movement.SetHorizontalInput(steer);
                            _leftGround |= !grounded;
                            _jumpWait += Step;
                            if (!_leftGround && _jumpWait > 0.4f)
                            {
                                // o pedido se perdeu (fadiga, por exemplo): tenta de novo
                                _jumped = false;
                            }
                        }

                        break;
                    case RouteMove.Lift:
                        _movement.SetHorizontalInput(Mathf.Clamp((tx - x) * 1.5f, -0.3f, 0.3f));
                        arrived = feet > ty - 0.1f;
                        break;
                }

                if (arrived)
                {
                    _retried = false;
                    _index++;
                    _jumped = false;
                    _leftGround = false;
                    _stepTime = 0f;
                    return;
                }

                if (_movement.IsMovementEnabled)
                {
                    _stepTime += Step;
                }

                // parado sem progresso: como um jogador, tenta outro caminho dali
                if (_stepTime > RetrySeconds && !_retried)
                {
                    var here = new Vector2Int(Mathf.FloorToInt(x), Mathf.RoundToInt(feet));
                    var fresh = _replan(here);
                    _retried = true;
                    if (fresh.Count > 1)
                    {
                        _route = fresh;
                        _index = 1;
                        _jumped = false;
                        _leftGround = false;
                        Replans++;
                        _stepTime = 0f;
                        Detours.Add($"parado em {here}");
                        return;
                    }
                }

                if (_stepTime > StuckSeconds)
                {
                    Failure = "preso: " + Status;
                }
            }
        }
    }
}
