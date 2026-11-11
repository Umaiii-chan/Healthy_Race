using HealthyRace.Collectibles;
using HealthyRace.Player;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    /// <summary>
    /// O lanche rápido dá um pico e cobra com uma queda (decisão D2); fruta e água
    /// encerram a queda.
    /// </summary>
    public sealed class SnackRushTests
    {
        private GameObject _player;
        private PlayerMovement _movement;

        private static SnackRush NewSnack() => new(1.3f, 2f, 0.6f, 4f, 2f, 8f);

        [SetUp]
        public void SetUp()
        {
            _player = new GameObject("Player");
            _player.AddComponent<Rigidbody2D>();
            _player.AddComponent<BoxCollider2D>();
            _player.AddComponent<PlayerGroundCheck>();
            _movement = _player.AddComponent<PlayerMovement>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
        }

        [Test]
        public void Eat_StartsTheRush_ThenTheCrash_ThenNothing()
        {
            var snack = NewSnack();
            Assert.That(snack.State, Is.EqualTo(SnackState.None));
            Assert.That(snack.Multiplier, Is.EqualTo(1f));

            snack.Eat();
            Assert.That(snack.State, Is.EqualTo(SnackState.Rush));
            Assert.That(snack.Multiplier, Is.EqualTo(1.3f));

            snack.Tick(1.9f);
            Assert.That(snack.State, Is.EqualTo(SnackState.Rush));
            snack.Tick(0.2f);
            Assert.That(snack.State, Is.EqualTo(SnackState.Crash));
            Assert.That(snack.Multiplier, Is.EqualTo(0.6f));
            // o que passou do pico já conta na queda
            Assert.That(snack.Remaining, Is.EqualTo(3.9f).Within(0.001f));

            snack.Tick(3.8f);
            Assert.That(snack.State, Is.EqualTo(SnackState.Crash));
            snack.Tick(0.2f);
            Assert.That(snack.State, Is.EqualTo(SnackState.None));
            Assert.That(snack.Multiplier, Is.EqualTo(1f));
        }

        [Test]
        public void EatingAgainDuringTheRush_RenewsIt_AndLengthensTheCrash()
        {
            var snack = NewSnack();
            snack.Eat();
            snack.Tick(1f);
            snack.Eat();
            Assert.That(snack.Remaining, Is.EqualTo(2f).Within(0.001f), "o pico renova");
            Assert.That(snack.PendingCrashSeconds, Is.EqualTo(6f).Within(0.001f));

            snack.Tick(0.5f);
            snack.Eat();
            snack.Tick(0.5f);
            snack.Eat();
            Assert.That(snack.PendingCrashSeconds, Is.EqualTo(8f).Within(0.001f), "tem teto");
        }

        [Test]
        public void EatingDuringTheCrash_StartsANewRush_WithTheBaseCrash()
        {
            var snack = NewSnack();
            snack.Eat();
            snack.Tick(2.5f);
            Assert.That(snack.State, Is.EqualTo(SnackState.Crash));

            snack.Eat();
            Assert.That(snack.State, Is.EqualTo(SnackState.Rush));
            Assert.That(snack.PendingCrashSeconds, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void Cure_EndsTheCrash_ButNotTheRush()
        {
            var snack = NewSnack();
            snack.Eat();
            Assert.That(snack.Cure(), Is.False, "no pico a fruta não muda nada");
            Assert.That(snack.State, Is.EqualTo(SnackState.Rush));

            snack.Tick(2.5f);
            Assert.That(snack.Cure(), Is.True);
            Assert.That(snack.State, Is.EqualTo(SnackState.None));
            Assert.That(snack.Cure(), Is.False);
        }

        [Test]
        public void StateChanged_FiresOncePerTransition()
        {
            var snack = NewSnack();
            var seen = new System.Collections.Generic.List<SnackState>();
            snack.StateChanged += seen.Add;
            snack.Eat();
            snack.Eat();
            snack.Tick(2.1f);
            snack.Tick(10f);
            Assert.That(
                seen,
                Is.EqualTo(new[] { SnackState.Rush, SnackState.Crash, SnackState.None })
            );
        }

        [Test]
        public void Food_SnackStartsTheRush_HealthyEndsTheCrash()
        {
            var burger = FoodData.Create(
                "burger",
                "Hambúrguer",
                FoodCategory.UltraProcessed,
                0,
                -18
            );
            var apple = FoodData.Create("apple", "Maçã", FoodCategory.Healthy, 100, 10);
            try
            {
                Assert.That(burger.StartsRush, Is.True);
                Assert.That(burger.EndsCrash, Is.False);
                Assert.That(apple.StartsRush, Is.False);
                Assert.That(apple.EndsCrash, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(burger);
                Object.DestroyImmediate(apple);
            }
        }

        [Test]
        public void Movement_SnackScalesTheSpeed_SeparateFromWellbeingAndTerrain()
        {
            _movement.SetMovementMultipliers(0.7f, 0.8f);
            _movement.SetTerrainMultiplier(0.7f);
            var before = _movement.EffectiveMoveSpeed;

            _movement.EatSnack();
            Assert.That(_movement.IsRushing, Is.True);
            Assert.That(
                _movement.EffectiveMoveSpeed,
                Is.EqualTo(before * _movement.Snack.Multiplier).Within(0.001f)
            );
            Assert.That(_movement.EffectiveMoveSpeed, Is.GreaterThan(before));
            Assert.That(_movement.MovementMultiplier, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(_movement.TerrainMultiplier, Is.EqualTo(0.7f).Within(0.001f));

            _movement.ClearSnack();
            Assert.That(_movement.EffectiveMoveSpeed, Is.EqualTo(before).Within(0.001f));
        }
    }
}
