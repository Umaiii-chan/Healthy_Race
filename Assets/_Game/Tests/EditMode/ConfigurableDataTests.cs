using System.Linq;
using HealthyRace.Collectibles;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class ConfigurableDataTests
    {
        [Test]
        public void FoodData_FactoryCreatesValidInstanceWithExpectedProperties()
        {
            var food = FoodData.Create(
                id: "test_apple",
                displayName: "Test Apple",
                category: FoodCategory.Healthy,
                basePoints: 100,
                wellbeingDelta: 10
            );

            try
            {
                Assert.That(food.Id, Is.EqualTo("test_apple"));
                Assert.That(food.DisplayName, Is.EqualTo("Test Apple"));
                Assert.That(food.Category, Is.EqualTo(FoodCategory.Healthy));
                Assert.That(food.BasePoints, Is.EqualTo(100));
                Assert.That(food.WellbeingDelta, Is.EqualTo(10));
                Assert.That(food.Sprite, Is.Null);
                Assert.That(food.CollectSound, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(food);
            }
        }

        [Test]
        public void FoodData_ExistingAssets_AppleAndBurger_AreValid()
        {
            var apple = AssetDatabase.LoadAssetAtPath<FoodData>(
                "Assets/_Game/ScriptableObjects/Food/SO_Food_Apple.asset"
            );
            var burger = AssetDatabase.LoadAssetAtPath<FoodData>(
                "Assets/_Game/ScriptableObjects/Food/SO_Food_Burger.asset"
            );

            Assert.That(apple, Is.Not.Null);
            Assert.That(apple.Id, Is.EqualTo("apple"));
            Assert.That(apple.DisplayName, Is.EqualTo("Apple"));
            Assert.That(apple.Category, Is.EqualTo(FoodCategory.Healthy));
            Assert.That(apple.BasePoints, Is.EqualTo(100));
            Assert.That(apple.WellbeingDelta, Is.EqualTo(10));

            Assert.That(burger, Is.Not.Null);
            Assert.That(burger.Id, Is.EqualTo("burger"));
            Assert.That(burger.DisplayName, Is.EqualTo("Burger"));
            Assert.That(burger.Category, Is.EqualTo(FoodCategory.UltraProcessed));
            Assert.That(burger.BasePoints, Is.EqualTo(0));
            Assert.That(burger.WellbeingDelta, Is.EqualTo(-18));
        }

        [Test]
        public void FoodData_NewAssets_BananaAndSoda_AreValid()
        {
            var banana = AssetDatabase.LoadAssetAtPath<FoodData>(
                "Assets/_Game/ScriptableObjects/Food/SO_Food_Banana.asset"
            );
            var soda = AssetDatabase.LoadAssetAtPath<FoodData>(
                "Assets/_Game/ScriptableObjects/Food/SO_Food_Soda.asset"
            );

            Assert.That(banana, Is.Not.Null);
            Assert.That(banana.Id, Is.EqualTo("banana"));
            Assert.That(banana.DisplayName, Is.EqualTo("Banana"));
            Assert.That(banana.Category, Is.EqualTo(FoodCategory.Healthy));
            Assert.That(banana.BasePoints, Is.EqualTo(80));
            Assert.That(banana.WellbeingDelta, Is.EqualTo(8));

            Assert.That(soda, Is.Not.Null);
            Assert.That(soda.Id, Is.EqualTo("soda"));
            Assert.That(soda.DisplayName, Is.EqualTo("Soda"));
            Assert.That(soda.Category, Is.EqualTo(FoodCategory.UltraProcessed));
            Assert.That(soda.BasePoints, Is.EqualTo(0));
            Assert.That(soda.WellbeingDelta, Is.EqualTo(-12));
        }

        [Test]
        public void WellbeingBandData_Contains_ReturnsTrueForValuesInRange()
        {
            var band = WellbeingBandData.Create(
                id: "tired",
                displayName: "VITA cansado",
                minValue: 26,
                maxValue: 50
            );

            try
            {
                Assert.That(band.Contains(26), Is.True, "Min value should be contained");
                Assert.That(band.Contains(38), Is.True, "Mid value should be contained");
                Assert.That(band.Contains(50), Is.True, "Max value should be contained");
            }
            finally
            {
                Object.DestroyImmediate(band);
            }
        }

        [Test]
        public void WellbeingBandData_Contains_ReturnsFalseForValuesOutOfRange()
        {
            var band = WellbeingBandData.Create(
                id: "tired",
                displayName: "VITA cansado",
                minValue: 26,
                maxValue: 50
            );

            try
            {
                Assert.That(band.Contains(25), Is.False, "Below min should not be contained");
                Assert.That(band.Contains(51), Is.False, "Above max should not be contained");
                Assert.That(band.Contains(-1), Is.False, "Negative value should not be contained");
                Assert.That(band.Contains(100), Is.False, "Far above max should not be contained");
            }
            finally
            {
                Object.DestroyImmediate(band);
            }
        }

        [Test]
        public void WellbeingBandData_ExistingBands_CoverZeroToOneHundredContinuouslyWithoutGaps()
        {
            var bands = new[]
            {
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Healthy.asset"
                ),
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Stable.asset"
                ),
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Tired.asset"
                ),
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Critical.asset"
                ),
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Salo.asset"
                ),
            };

            Assert.That(bands, Has.None.Null);

            for (var wellbeingValue = 0; wellbeingValue <= 100; wellbeingValue++)
            {
                var matchingBands = bands.Where(b => b.Contains(wellbeingValue)).ToList();
                Assert.That(
                    matchingBands.Count,
                    Is.EqualTo(1),
                    $"Wellbeing value {wellbeingValue} should belong to exactly 1 band, but matched {matchingBands.Count}"
                );
            }
        }

        [Test]
        public void WellbeingBandData_ExistingBands_MultipliersAreValidAndPositive()
        {
            var bands = new[]
            {
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Healthy.asset"
                ),
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Stable.asset"
                ),
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Tired.asset"
                ),
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Critical.asset"
                ),
                AssetDatabase.LoadAssetAtPath<WellbeingBandData>(
                    "Assets/_Game/ScriptableObjects/WellBeing/SO_Wellbeing_Salo.asset"
                ),
            };

            foreach (var band in bands)
            {
                Assert.That(band.MovementMultiplier, Is.GreaterThan(0f));
                Assert.That(band.JumpMultiplier, Is.GreaterThan(0f));
                Assert.That(band.MovementMultiplier, Is.LessThanOrEqualTo(2f));
                Assert.That(band.JumpMultiplier, Is.LessThanOrEqualTo(2f));
            }

            // Verify relative scaling: healthier states should have equal or better multipliers
            var healthy = bands[0];
            var salo = bands[4];
            Assert.That(healthy.MovementMultiplier, Is.GreaterThan(salo.MovementMultiplier));
            Assert.That(healthy.JumpMultiplier, Is.GreaterThan(salo.JumpMultiplier));
        }

        [Test]
        public void WellbeingBandData_FactoryCreatesValidBandWithProperties()
        {
            var band = WellbeingBandData.Create(
                id: "custom_band",
                displayName: "Custom Band",
                minValue: 10,
                maxValue: 20,
                movementMultiplier: 0.85f,
                jumpMultiplier: 0.9f,
                visualForm: PlayerVisualForm.Vita,
                characterColor: Color.blue,
                showTiredEyes: true
            );

            try
            {
                Assert.That(band.Id, Is.EqualTo("custom_band"));
                Assert.That(band.DisplayName, Is.EqualTo("Custom Band"));
                Assert.That(band.MinValue, Is.EqualTo(10));
                Assert.That(band.MaxValue, Is.EqualTo(20));
                Assert.That(band.MovementMultiplier, Is.EqualTo(0.85f));
                Assert.That(band.JumpMultiplier, Is.EqualTo(0.9f));
                Assert.That(band.VisualForm, Is.EqualTo(PlayerVisualForm.Vita));
                Assert.That(band.CharacterColor, Is.EqualTo(Color.blue));
                Assert.That(band.ShowTiredEyes, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(band);
            }
        }

        [Test]
        public void FoodCatalog_ContainsAll18MvpFoods_WithValidCategoriesAndCounts()
        {
            var guids = AssetDatabase.FindAssets(
                "t:FoodData",
                new[] { "Assets/_Game/ScriptableObjects/Food" }
            );
            Assert.That(
                guids.Length,
                Is.EqualTo(18),
                "Catalog should contain exactly 18 food assets."
            );

            var allFoods = guids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<FoodData>(path))
                .ToList();

            Assert.That(allFoods, Has.None.Null);

            var healthyFoods = allFoods.Where(f => f.Category == FoodCategory.Healthy).ToList();
            var ultraFoods = allFoods
                .Where(f => f.Category == FoodCategory.UltraProcessed)
                .ToList();

            Assert.That(
                healthyFoods.Count,
                Is.EqualTo(10),
                "Catalog should contain exactly 10 healthy foods."
            );
            Assert.That(
                ultraFoods.Count,
                Is.EqualTo(8),
                "Catalog should contain exactly 8 ultra-processed foods."
            );
        }

        [Test]
        public void FoodCatalog_All18Foods_HaveUniqueIdsAndValidFields()
        {
            var guids = AssetDatabase.FindAssets(
                "t:FoodData",
                new[] { "Assets/_Game/ScriptableObjects/Food" }
            );
            var allFoods = guids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<FoodData>(path))
                .ToList();

            var ids = new System.Collections.Generic.HashSet<string>();

            foreach (var food in allFoods)
            {
                Assert.That(
                    food.Id,
                    Is.Not.Null.And.Not.Empty,
                    $"Food at {food.name} must have a valid ID"
                );
                Assert.That(
                    food.Id,
                    Is.EqualTo(food.Id.ToLowerInvariant()),
                    $"Food ID {food.Id} must be lowercase"
                );
                Assert.That(ids.Add(food.Id), Is.True, $"Duplicate food ID found: {food.Id}");

                Assert.That(
                    food.DisplayName,
                    Is.Not.Null.And.Not.Empty,
                    $"Food {food.Id} must have a non-empty DisplayName"
                );

                Assert.That(
                    food.Sprite,
                    Is.Not.Null,
                    $"Food {food.Id} must have a configured sprite"
                );

                if (food.Category == FoodCategory.Healthy)
                {
                    Assert.That(
                        food.BasePoints,
                        Is.GreaterThan(0),
                        $"Healthy food {food.Id} must have positive BasePoints"
                    );
                    Assert.That(
                        food.WellbeingDelta,
                        Is.GreaterThan(0),
                        $"Healthy food {food.Id} must have positive WellbeingDelta"
                    );
                }
                else
                {
                    Assert.That(
                        food.BasePoints,
                        Is.EqualTo(0),
                        $"UltraProcessed food {food.Id} must have 0 BasePoints"
                    );
                    Assert.That(
                        food.WellbeingDelta,
                        Is.LessThan(0),
                        $"UltraProcessed food {food.Id} must have negative WellbeingDelta"
                    );
                }
            }
        }

        [Test]
        public void FoodCollectible_Configure_UpdatesVisualWithFoodSprite()
        {
            var apple = AssetDatabase.LoadAssetAtPath<FoodData>(
                "Assets/_Game/ScriptableObjects/Food/SO_Food_Apple.asset"
            );
            Assert.That(apple, Is.Not.Null);
            Assert.That(apple.Sprite, Is.Not.Null);

            var go = new GameObject("TestFood");
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                go.AddComponent<CircleCollider2D>();
                var collectible = go.AddComponent<FoodCollectible>();

                collectible.Configure(apple);

                Assert.That(collectible.FoodData, Is.SameAs(apple));
                Assert.That(sr.sprite, Is.SameAs(apple.Sprite));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FoodCollectible_ResetState_ReactivatesAndClearsCollectedFlag()
        {
            var apple = AssetDatabase.LoadAssetAtPath<FoodData>(
                "Assets/_Game/ScriptableObjects/Food/SO_Food_Apple.asset"
            );

            var go = new GameObject("TestFood");
            try
            {
                go.AddComponent<CircleCollider2D>();
                var collectible = go.AddComponent<FoodCollectible>();
                collectible.Configure(apple);

                Assert.That(collectible.IsCollected, Is.False);
                Assert.That(go.activeSelf, Is.True);

                go.SetActive(false);
                collectible.ResetState();

                Assert.That(collectible.IsCollected, Is.False);
                Assert.That(go.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
