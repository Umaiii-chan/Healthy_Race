using System.Collections;
using System.Collections.Generic;
using HealthyRace.Collectibles;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class FoodCollectiblePlayModeTests
    {
        private readonly List<GameObject> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();
        }

        private GameObject CreatePlayer(string name, Vector2 position, int initialWellbeing = 50)
        {
            var go = new GameObject(name);
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var wb = go.AddComponent<WellbeingComponent>();
            wb.Initialize(initialWellbeing);

            _createdObjects.Add(go);
            return go;
        }

        private GameObject CreateCollectible(
            Vector2 position,
            FoodData data,
            out FoodCollectible collectible
        )
        {
            var go = new GameObject($"Food_{data.Id}");
            go.transform.position = position;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            collectible = go.AddComponent<FoodCollectible>();
            collectible.Configure(data);

            _createdObjects.Add(go);
            return go;
        }

        [UnityTest]
        public IEnumerator HealthyFood_WhenCollected_IncreasesCollectorWellbeingAndDeactivates()
        {
            var player = CreatePlayer("Player1", Vector2.zero, 50);
            var wellbeing = player.GetComponent<WellbeingComponent>();

            var foodData = FoodData.Create(
                "test_healthy",
                "Test Healthy",
                FoodCategory.Healthy,
                100,
                10
            );
            CreateCollectible(Vector2.zero, foodData, out var collectible);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(wellbeing.CurrentValue, Is.EqualTo(60));
            Assert.That(collectible.IsCollected, Is.True);
            Assert.That(collectible.gameObject.activeSelf, Is.False);

            Object.DestroyImmediate(foodData);
        }

        [UnityTest]
        public IEnumerator UltraProcessedFood_WhenCollected_DecreasesCollectorWellbeingAndDeactivates()
        {
            var player = CreatePlayer("Player1", Vector2.zero, 50);
            var wellbeing = player.GetComponent<WellbeingComponent>();

            var foodData = FoodData.Create(
                "test_ultra",
                "Test Ultra",
                FoodCategory.UltraProcessed,
                0,
                -18
            );
            CreateCollectible(Vector2.zero, foodData, out var collectible);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(wellbeing.CurrentValue, Is.EqualTo(32));
            Assert.That(collectible.IsCollected, Is.True);
            Assert.That(collectible.gameObject.activeSelf, Is.False);

            Object.DestroyImmediate(foodData);
        }

        [UnityTest]
        public IEnumerator FoodCollection_IsolatesPlayerOneFromPlayerTwo()
        {
            var player1 = CreatePlayer("Player1", Vector2.zero, 50);
            var player2 = CreatePlayer("Player2", new Vector2(10f, 10f), 50);

            var p1Wellbeing = player1.GetComponent<WellbeingComponent>();
            var p2Wellbeing = player2.GetComponent<WellbeingComponent>();

            var foodData = FoodData.Create("test_apple", "Apple", FoodCategory.Healthy, 100, 10);
            CreateCollectible(Vector2.zero, foodData, out var collectible);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(
                p1Wellbeing.CurrentValue,
                Is.EqualTo(60),
                "Player 1 should have collected the food and gained +10 wellbeing."
            );
            Assert.That(
                p2Wellbeing.CurrentValue,
                Is.EqualTo(50),
                "Player 2 should remain completely unaffected by Player 1's food collection."
            );
            Assert.That(collectible.IsCollected, Is.True);

            Object.DestroyImmediate(foodData);
        }

        [UnityTest]
        public IEnumerator FoodCollectible_PreventsDoubleCollection()
        {
            var player = CreatePlayer("Player1", Vector2.zero, 50);
            var wellbeing = player.GetComponent<WellbeingComponent>();

            var foodData = FoodData.Create("test_apple", "Apple", FoodCategory.Healthy, 100, 10);
            CreateCollectible(Vector2.zero, foodData, out var collectible);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(wellbeing.CurrentValue, Is.EqualTo(60));
            Assert.That(collectible.IsCollected, Is.True);

            // Re-triggering or sending another collider when already collected
            var col = player.GetComponent<Collider2D>();
            var method = typeof(FoodCollectible).GetMethod(
                "OnTriggerEnter2D",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            method.Invoke(collectible, new object[] { col });

            Assert.That(
                wellbeing.CurrentValue,
                Is.EqualTo(60),
                "Wellbeing should not change on second collection attempt."
            );

            Object.DestroyImmediate(foodData);
        }

        [UnityTest]
        public IEnumerator NonPlayerCollider_DoesNotTriggerFoodCollection()
        {
            var obstacle = new GameObject("Obstacle");
            obstacle.transform.position = Vector2.zero;
            var rb = obstacle.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = obstacle.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            _createdObjects.Add(obstacle);

            var foodData = FoodData.Create("test_apple", "Apple", FoodCategory.Healthy, 100, 10);
            CreateCollectible(Vector2.zero, foodData, out var collectible);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(collectible.IsCollected, Is.False);
            Assert.That(collectible.gameObject.activeSelf, Is.True);

            Object.DestroyImmediate(foodData);
        }

        [UnityTest]
        public IEnumerator FoodCollectible_FiresCollectedEventOnPickup()
        {
            var player = CreatePlayer("Player1", Vector2.zero, 50);
            var wellbeing = player.GetComponent<WellbeingComponent>();

            var foodData = FoodData.Create("test_apple", "Apple", FoodCategory.Healthy, 100, 10);
            CreateCollectible(Vector2.zero, foodData, out var collectible);

            FoodCollectible eventCollectible = null;
            WellbeingComponent eventWellbeing = null;
            var eventFired = false;

            collectible.Collected += (col, wb) =>
            {
                eventFired = true;
                eventCollectible = col;
                eventWellbeing = wb;
            };

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(eventFired, Is.True);
            Assert.That(eventCollectible, Is.SameAs(collectible));
            Assert.That(eventWellbeing, Is.SameAs(wellbeing));

            Object.DestroyImmediate(foodData);
        }
    }
}
