using HealthyRace.Collectibles;
using HealthyRace.Core;
using HealthyRace.Scoring;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class BalanceConfigTests
    {
        [TearDown]
        public void TearDown()
        {
            BalanceConfig.Use(null);
        }

        [Test]
        public void Asset_LoadsFromResources_WithTheCurrentValues()
        {
            BalanceConfig.Use(null);
            var asset = Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.That(asset, Is.Not.Null, "Resources/SO_Balance precisa existir na build");
            Assert.That(BalanceConfig.Current, Is.SameAs(asset));

            Assert.That(asset.PartnerWindowSeconds, Is.EqualTo(45f));
            Assert.That(asset.MatchTimeLimitSeconds, Is.EqualTo(300f));
            Assert.That(asset.FirstPlaceArrivalBonus, Is.EqualTo(500));
            Assert.That(asset.SecondPlaceArrivalBonus, Is.EqualTo(250));
            Assert.That(asset.PointsPerLever, Is.EqualTo(150));
            Assert.That(asset.RushMultiplier, Is.EqualTo(1.3f));
            Assert.That(asset.RushSeconds, Is.EqualTo(2f));
            Assert.That(asset.CrashMultiplier, Is.EqualTo(0.6f));
            Assert.That(asset.CrashSeconds, Is.EqualTo(4f));
            Assert.That(asset.FatigueDurationSeconds, Is.EqualTo(1.2f));
            Assert.That(asset.FatigueWarningSeconds, Is.EqualTo(1f));
            Assert.That(asset.VictoryByArrival, Is.True);
            Assert.That(asset.CountdownSeconds, Is.EqualTo(3f));
            Assert.That(asset.FatigueTriggerThreshold, Is.EqualTo(20));
        }

        [Test]
        public void Defaults_WithoutTheAsset_MatchTheAsset()
        {
            var defaults = ScriptableObject.CreateInstance<BalanceConfig>();
            var asset = Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            Assert.That(
                JsonUtility.ToJson(defaults),
                Is.EqualTo(JsonUtility.ToJson(asset)),
                "o asset e os padrões do código divergiram"
            );
            Object.DestroyImmediate(defaults);
        }

        [Test]
        public void RaceScoring_AndSnackEffect_ReadTheBalanceInUse()
        {
            var custom = ScriptableObject.CreateInstance<BalanceConfig>();
            Set(custom, "_firstPlaceArrivalBonus", 900);
            Set(custom, "_crashSeconds", 3f);
            BalanceConfig.Use(custom);

            var score = ScoreConfig.CreateForRace(120f);
            Assert.That(score.FirstPlaceArrivalBonus, Is.EqualTo(900));
            Assert.That(score.TargetTimeSeconds, Is.EqualTo(120f));

            var burger = FoodData.Create(
                "burger",
                "Hambúrguer",
                FoodCategory.UltraProcessed,
                0,
                -18
            );
            var host = new GameObject("Player");
            host.AddComponent<Rigidbody2D>();
            host.AddComponent<BoxCollider2D>();
            host.AddComponent<HealthyRace.Player.PlayerGroundCheck>();
            var movement = host.AddComponent<HealthyRace.Player.PlayerMovement>();
            movement.EatSnack();
            Assert.That(movement.Snack.PendingCrashSeconds, Is.EqualTo(3f));
            Object.DestroyImmediate(host);

            Assert.That(burger.StartsRush, Is.True);
            var apple = FoodData.Create("apple", "Maçã", FoodCategory.Healthy, 100, 10);
            Assert.That(apple.StartsRush, Is.False);

            Object.DestroyImmediate(burger);
            Object.DestroyImmediate(apple);
            Object.DestroyImmediate(custom);
        }

        private static void Set(BalanceConfig config, string field, object value)
        {
            typeof(BalanceConfig)
                .GetField(
                    field,
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                )
                .SetValue(config, value);
        }
    }
}
