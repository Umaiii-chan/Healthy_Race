using HealthyRace.Collectibles;
using HealthyRace.Player;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    /// <summary>
    /// O atalho cobra no tempo: ultraprocessado deixa o jogador lento por alguns
    /// segundos; saudável não.
    /// </summary>
    public sealed class SluggishnessTests
    {
        private GameObject _player;
        private PlayerMovement _movement;

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
        public void UltraProcessed_Slows_Healthy_DoesNot()
        {
            var burger = FoodData.Create("burger", "Burger", FoodCategory.UltraProcessed, 0, -18);
            var apple = FoodData.Create("apple", "Apple", FoodCategory.Healthy, 100, 10);
            try
            {
                Assert.That(burger.SlowMultiplier, Is.LessThan(1f));
                Assert.That(burger.SlowSeconds, Is.GreaterThan(0f));
                Assert.That(apple.SlowMultiplier, Is.EqualTo(1f));
                Assert.That(apple.SlowSeconds, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(burger);
                Object.DestroyImmediate(apple);
            }
        }

        [Test]
        public void Sluggishness_ScalesTheEffectiveSpeed_AndClears()
        {
            var normal = _movement.EffectiveMoveSpeed;
            _movement.ApplySluggishness(0.5f, 3f);

            Assert.That(_movement.IsSluggish, Is.True);
            Assert.That(_movement.EffectiveMoveSpeed, Is.EqualTo(normal * 0.5f).Within(0.001f));

            _movement.ClearSluggishness();
            Assert.That(_movement.IsSluggish, Is.False);
            Assert.That(_movement.EffectiveMoveSpeed, Is.EqualTo(normal).Within(0.001f));
        }

        [Test]
        public void Sluggishness_EatingAgainRenewsTheTime_WithoutStacking()
        {
            _movement.ApplySluggishness(0.5f, 3f);
            _movement.ApplySluggishness(0.5f, 3f);

            Assert.That(_movement.SluggishMultiplier, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(_movement.SluggishSecondsRemaining, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Sluggishness_IsSeparateFromWellbeingAndTerrain()
        {
            _movement.SetMovementMultipliers(0.7f, 0.8f);
            _movement.SetTerrainMultiplier(0.7f);
            var before = _movement.EffectiveMoveSpeed;
            _movement.ApplySluggishness(0.5f, 3f);

            Assert.That(_movement.EffectiveMoveSpeed, Is.EqualTo(before * 0.5f).Within(0.001f));
            Assert.That(_movement.MovementMultiplier, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(_movement.TerrainMultiplier, Is.EqualTo(0.7f).Within(0.001f));
        }
    }
}
