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
        public void Asset_LoadsFromResources_WithTheV1Values()
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
            Assert.That(asset.UltraSlowMultiplier, Is.EqualTo(0.5f));
            Assert.That(asset.UltraSlowSeconds, Is.EqualTo(5f));
            Assert.That(asset.FatigueDurationSeconds, Is.EqualTo(2f));
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
        public void RaceScoring_AndFoodEffect_ReadTheBalanceInUse()
        {
            var custom = ScriptableObject.CreateInstance<BalanceConfig>();
            Set(custom, "_firstPlaceArrivalBonus", 900);
            Set(custom, "_ultraSlowSeconds", 3f);
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
            Assert.That(burger.SlowSeconds, Is.EqualTo(3f));
            var apple = FoodData.Create("apple", "Maçã", FoodCategory.Healthy, 100, 10);
            Assert.That(apple.SlowSeconds, Is.Zero);
            Assert.That(apple.SlowMultiplier, Is.EqualTo(1f));

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
