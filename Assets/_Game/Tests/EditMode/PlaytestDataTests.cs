using System.IO;
using System.Linq;
using HealthyRace.Core;
using HealthyRace.Level;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    /// <summary>Progresso pela rota, registro de partida e progresso salvo.</summary>
    public sealed class PlaytestDataTests
    {
        private string _temp;

        [SetUp]
        public void SetUp()
        {
            _temp = Path.Combine(
                Path.GetTempPath(),
                "healthy-race-tests-" + Path.GetRandomFileName()
            );
            Directory.CreateDirectory(_temp);
        }

        [TearDown]
        public void TearDown()
        {
            MatchRecorder.ForceEnabled = false;
            MatchRecorder.OutputDirectory = null;
            SaveGame.FilePath = null;
            SaveGame.Reload();
            if (Directory.Exists(_temp))
            {
                Directory.Delete(_temp, true);
            }
        }

        private static LevelGrid Grid(int level) =>
            LevelGrid.Parse(
                AssetDatabase
                    .LoadAssetAtPath<TextAsset>($"Assets/_Game/Levels/Fase{level:00}.txt")
                    .text
            );

        [TestCase(1)]
        [TestCase(10)]
        public void RaceProgress_GoesFromSpawnToDoor(int level)
        {
            var grid = Grid(level);
            var progress = new RaceProgress(grid, LevelGrid.SpawnOne, LevelGrid.DoorOne);
            Assert.That(progress.PointCount, Is.GreaterThan(10));

            var spawn = grid.Cells(LevelGrid.SpawnOne).Single();
            var door = grid.Cells(LevelGrid.DoorOne).First();
            Assert.That(
                progress.Fraction((Vector2)spawn + new Vector2(0.5f, 0.5f)),
                Is.LessThan(0.02f)
            );
            Assert.That(
                progress.Fraction((Vector2)door + new Vector2(0.5f, 0.5f)),
                Is.GreaterThan(0.98f)
            );
        }

        [Test]
        public void RaceProgress_TellsWhoIsAheadOnTheSameFloor()
        {
            // no primeiro andar a corrida vai para a direita: mais à direita = mais à frente
            var grid = Grid(1);
            var progress = new RaceProgress(grid, LevelGrid.SpawnOne, LevelGrid.DoorOne);
            var spawn = grid.Cells(LevelGrid.SpawnOne).Single();
            var behind = progress.Fraction((Vector2)spawn + new Vector2(5.5f, 0.5f));
            var ahead = progress.Fraction((Vector2)spawn + new Vector2(20.5f, 0.5f));
            Assert.That(ahead, Is.GreaterThan(behind));
        }

        [Test]
        public void MatchRecorder_WritesOneJsonLinePerEvent()
        {
            MatchRecorder.ForceEnabled = true;
            MatchRecorder.OutputDirectory = _temp;
            var grid = Grid(1);
            var host = new GameObject("Recorder");
            var p1 = new GameObject("P1").transform;
            var p2 = new GameObject("P2").transform;
            try
            {
                var spawn = grid.Cells(LevelGrid.SpawnOne).Single();
                p1.position = new Vector3(spawn.x + 0.5f, spawn.y + 0.5f);
                p2.position = new Vector3(spawn.x + 10.5f, spawn.y + 0.5f);
                var recorder = host.AddComponent<MatchRecorder>();
                recorder.Begin(
                    grid,
                    130f,
                    () => 12.5f,
                    p1,
                    p2,
                    _ => 80,
                    i =>
                        i == 2
                            ? HealthyRace.Player.SnackState.Crash
                            : HealthyRace.Player.SnackState.None
                );
                Assert.That(recorder.IsRecording, Is.True);

                recorder.Food(1, "apple", true, 90);
                recorder.Lever(2);
                recorder.Result(1, true, 120f, 1, 5000, 90, 10, 0, 0);
                recorder.Result(2, true, 125f, 2, 3000, 40, 2, 4, 1);
                recorder.End(1);

                var lines = File.ReadAllLines(recorder.FilePath);
                Assert.That(lines.First(), Does.Contain("\"evento\":\"largada\""));
                Assert.That(lines.First(), Does.Contain("\"fase\":1"));
                Assert.That(lines, Has.Some.Contains("\"evento\":\"coleta\""));
                Assert.That(lines, Has.Some.Contains("\"alimento\":\"apple\""));
                Assert.That(lines, Has.Some.Contains("\"evento\":\"alavanca\""));
                Assert.That(lines, Has.Some.Contains("\"evento\":\"amostra\""));
                Assert.That(lines, Has.Some.Contains("\"lider\":2"));
                Assert.That(lines.Last(), Does.Contain("\"evento\":\"fim\""));
                Assert.That(lines.Last(), Does.Contain("\"vencedor\":1"));
                Assert.That(lines.All(l => l.StartsWith("{") && l.EndsWith("}")), Is.True);
                Assert.That(lines.All(l => l.Contains("\"t\":12.5")), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(p1.gameObject);
                Object.DestroyImmediate(p2.gameObject);
            }
        }

        [Test]
        public void MatchRecorder_IsOffInBatchModeUnlessForced()
        {
            Assert.That(MatchRecorder.Enabled, Is.EqualTo(!Application.isBatchMode));
            MatchRecorder.ForceEnabled = true;
            Assert.That(MatchRecorder.Enabled, Is.True);
        }

        [Test]
        public void SaveGame_RecordsBestTimeAndUnlocksTheNextLevel()
        {
            SaveGame.FilePath = Path.Combine(_temp, SaveGame.FileName);
            SaveGame.Reload();
            Assert.That(SaveGame.HighestUnlocked(10), Is.EqualTo(1));

            Assert.That(SaveGame.RecordFinish(1, 130f), Is.True);
            Assert.That(SaveGame.RecordFinish(1, 140f), Is.False);
            Assert.That(SaveGame.RecordFinish(1, 120f), Is.True);
            Assert.That(SaveGame.RecordFinish(2, 100f), Is.True);

            SaveGame.Reload();
            Assert.That(SaveGame.IsCompleted(1), Is.True);
            Assert.That(SaveGame.BestTime(1), Is.EqualTo(120f));
            Assert.That(SaveGame.BestTime(3), Is.Null);
            Assert.That(SaveGame.HighestUnlocked(10), Is.EqualTo(3));
            Assert.That(SaveGame.HighestUnlocked(2), Is.EqualTo(2));
        }

        [Test]
        public void SaveGame_UnreadableFile_StartsEmpty()
        {
            SaveGame.FilePath = Path.Combine(_temp, SaveGame.FileName);
            File.WriteAllText(SaveGame.FilePath, "{ isto não é json");
            SaveGame.Reload();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Assert.That(SaveGame.HighestUnlocked(10), Is.EqualTo(1));
            Assert.That(SaveGame.IsCompleted(1), Is.False);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
        }
    }
}
