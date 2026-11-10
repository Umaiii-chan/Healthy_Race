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

        private static IEnumerator LoadLevel()
        {
            var loadOp = SceneManager.LoadSceneAsync(
                GameFlow.SceneForLevel(1),
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
            Assert.That(gate.IsOpen, Is.False, "o portão demora para abrir");

            yield return new WaitForSeconds(LevelBootstrap.GateOpenDelay + 0.2f);
            Assert.That(gate.IsOpen, Is.True);
        }

        // =====================================================================
        // Corrida inteira
        // =====================================================================
        [UnityTest]
        public IEnumerator Race_LongAndShortRoutes_BothFinishAlone_AtFullWellbeing()
        {
            yield return Race(startingWellbeing: -1, label: "bem-estar cheio");
        }

        [UnityTest]
        public IEnumerator Race_LongAndShortRoutes_BothFinishAlone_FromZeroWellbeing()
        {
            yield return Race(startingWellbeing: 0, label: "bem-estar zero");
        }

        /// <summary>
        /// P1 pelo caminho longo, P2 pelo curto (comendo todo o fast-food do corredor).
        /// O tempo de jogo avança em passos fixos, então o teste roda mais rápido que
        /// o relógio e os tempos medidos são os do jogo.
        /// </summary>
        private IEnumerator Race(int startingWellbeing, string label)
        {
            yield return LoadLevel();
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
                new RouteBot(bootstrap.PlayerOne, longRoute, "P1 longo"),
                new RouteBot(bootstrap.PlayerTwo, shortRoute, "P2 curto"),
            };
            var doors = new[] { bootstrap.PlayerOneDoor, bootstrap.PlayerTwoDoor };
            var times = new float[2];
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
            var p1 = bootstrap.PlayerOne.GetComponent<WellbeingComponent>().CurrentValue;
            var p2 = bootstrap.PlayerTwo.GetComponent<WellbeingComponent>().CurrentValue;
            Debug.Log(
                $"CORRIDA ({label}): longo {times[0]:0.0} s (Bem-Estar {p1}), "
                    + $"curto {times[1]:0.0} s (Bem-Estar {p2})"
            );
            Assert.That(times[0], Is.GreaterThan(0f), $"P1 não terminou: {bots[0].Status}");
            Assert.That(times[1], Is.GreaterThan(0f), $"P2 não terminou: {bots[1].Status}");
        }

        /// <summary>
        /// Rota do caminho curto: passa por todos os fast-foods do corredor, na ordem
        /// em que a corrida os encontra (andares pares para a direita, ímpares para a
        /// esquerda), e termina na porta do P2.
        /// </summary>
        private static List<RouteStep> ShortRoute(LevelGrid grid, Vector2Int start)
        {
            var waypoints = grid.Cells(LevelGrid.Fast)
                .OrderBy(c => c.y)
                .ThenBy(c => ((c.y - 1) / 6) % 2 == 0 ? c.x : -c.x)
                .ToList();
            var route = new List<RouteStep> { new(start, RouteMove.Start) };
            var from = start;
            foreach (var waypoint in waypoints)
            {
                var leg = LevelRoutes.Find(grid, from, c => c == waypoint);
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

            private readonly GameObject _player;
            private readonly PlayerMovement _movement;
            private readonly List<RouteStep> _route;
            private readonly string _name;
            private int _index = 1;
            private bool _jumped;
            private bool _leftGround;
            private float _jumpWait;
            private float _stepTime;

            public RouteBot(GameObject player, List<RouteStep> route, string name)
            {
                _player = player;
                _movement = TakeControl(player);
                _route = route;
                _name = name;
            }

            public string Failure { get; private set; }

            private bool IsLift(Vector2Int cell) =>
                Object.FindFirstObjectByType<LevelBootstrap>().Grid[cell.x, cell.y]
                == LevelGrid.Lift;

            public string Status =>
                _index < _route.Count
                    ? $"{_name} no passo {_index}/{_route.Count} ({_route[_index]}), "
                        + $"em {(Vector2)_player.transform.position}, chão {_movement.IsGrounded}, "
                        + $"vel {_movement.Velocity}, rampa {_movement.IsOnSlope}, "
                        + $"pulou {_jumped}/{_leftGround}, mov {_movement.IsMovementEnabled}"
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

                if (_stepTime > StuckSeconds)
                {
                    Failure = "preso: " + Status;
                }
            }
        }
    }
}
