using HealthyRace.Input;
using HealthyRace.Player;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    public sealed class WellbeingComponentTests
    {
        [Test]
        public void ApplyDelta_AtMaximumValue_StaysAtOneHundred()
        {
            var wellbeing = CreateWellbeing(out var gameObject);

            try
            {
                wellbeing.SetValue(100);
                wellbeing.ApplyDelta(10);

                Assert.That(wellbeing.CurrentValue, Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ApplyDelta_AfterBurgerPenalty_ReducesToEightyTwo()
        {
            var wellbeing = CreateWellbeing(out var gameObject);

            try
            {
                wellbeing.SetValue(100);
                wellbeing.ApplyDelta(-18);

                Assert.That(wellbeing.CurrentValue, Is.EqualTo(82));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ApplyDelta_BelowMinimum_ClampsToZero()
        {
            var wellbeing = CreateWellbeing(out var gameObject);

            try
            {
                wellbeing.SetValue(5);
                wellbeing.ApplyDelta(-10);

                Assert.That(wellbeing.CurrentValue, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [TestCase(76, "VITA saudável")]
        [TestCase(75, "VITA estável")]
        [TestCase(51, "VITA estável")]
        [TestCase(50, "VITA cansado")]
        [TestCase(26, "VITA cansado")]
        [TestCase(25, "Estado crítico")]
        [TestCase(1, "Estado crítico")]
        [TestCase(0, "SALO")]
        public void SetValue_UsesTheExpectedWellbeingBand(int value, string expectedBandName)
        {
            var wellbeing = CreateWellbeing(out var gameObject);

            try
            {
                wellbeing.ConfigureBands(LoadBands());
                wellbeing.SetValue(value);

                Assert.That(wellbeing.CurrentBand, Is.Not.Null);
                Assert.That(wellbeing.CurrentBand.DisplayName, Is.EqualTo(expectedBandName));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SetValue_ClampsAboveMaximum_ToMaximumValue()
        {
            var wellbeing = CreateWellbeing(out var gameObject);

            try
            {
                wellbeing.SetValue(150);
                Assert.That(wellbeing.CurrentValue, Is.EqualTo(100));
                Assert.That(wellbeing.IsAtMaximum, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SetValue_ClampsBelowMinimum_ToMinimumValue()
        {
            var wellbeing = CreateWellbeing(out var gameObject);

            try
            {
                wellbeing.SetValue(-50);
                Assert.That(wellbeing.CurrentValue, Is.EqualTo(0));
                Assert.That(wellbeing.IsAtMinimum, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SetValue_WhenValueChanges_EmitsExpectedEvents()
        {
            var wellbeing = CreateWellbeing(out var gameObject);

            try
            {
                wellbeing.ConfigureBands(LoadBands());
                wellbeing.SetValue(100);

                var valueChangedFired = false;
                var receivedNewValue = -1;
                wellbeing.ValueChanged += val =>
                {
                    valueChangedFired = true;
                    receivedNewValue = val;
                };

                var wellbeingChangedFired = false;
                var receivedPrev = -1;
                var receivedCur = -1;
                wellbeing.WellbeingChanged += (prev, cur) =>
                {
                    wellbeingChangedFired = true;
                    receivedPrev = prev;
                    receivedCur = cur;
                };

                var bandChangedFired = false;
                WellbeingBandData receivedBand = null;
                wellbeing.WellbeingBandChanged += band =>
                {
                    bandChangedFired = true;
                    receivedBand = band;
                };

                var multipliersFired = false;
                var receivedMove = -1f;
                var receivedJump = -1f;
                wellbeing.MultipliersChanged += (move, jump) =>
                {
                    multipliersFired = true;
                    receivedMove = move;
                    receivedJump = jump;
                };

                wellbeing.SetValue(40); // Transitions to "VITA cansado"

                Assert.That(valueChangedFired, Is.True);
                Assert.That(receivedNewValue, Is.EqualTo(40));
                Assert.That(wellbeingChangedFired, Is.True);
                Assert.That(receivedPrev, Is.EqualTo(100));
                Assert.That(receivedCur, Is.EqualTo(40));
                Assert.That(bandChangedFired, Is.True);
                Assert.That(receivedBand.Id, Is.EqualTo("tired"));
                Assert.That(multipliersFired, Is.True);
                Assert.That(receivedMove, Is.EqualTo(0.9f));
                Assert.That(receivedJump, Is.EqualTo(0.95f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SetValue_WhenValueDoesNotChange_DoesNotEmitEvents()
        {
            var wellbeing = CreateWellbeing(out var gameObject);

            try
            {
                wellbeing.ConfigureBands(LoadBands());
                wellbeing.SetValue(50);

                var eventCount = 0;
                wellbeing.ValueChanged += _ => eventCount++;
                wellbeing.WellbeingChanged += (_, _) => eventCount++;

                wellbeing.SetValue(50);

                Assert.That(eventCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TwoPlayers_WellbeingChanges_RemainStrictlyIsolated()
        {
            var p1Wellbeing = CreateWellbeing(out var p1Object);
            var p2Wellbeing = CreateWellbeing(out var p2Object);

            try
            {
                var bands = LoadBands();
                p1Wellbeing.ConfigureBands(bands);
                p2Wellbeing.ConfigureBands(bands);

                p1Wellbeing.SetValue(100);
                p2Wellbeing.SetValue(100);

                // Modify P1
                p1Wellbeing.SetValue(20); // Critical
                Assert.That(p1Wellbeing.CurrentValue, Is.EqualTo(20));
                Assert.That(p1Wellbeing.CurrentBand.Id, Is.EqualTo("critical"));
                Assert.That(p1Wellbeing.MovementMultiplier, Is.EqualTo(0.8f));

                // Assert P2 is unaffected
                Assert.That(p2Wellbeing.CurrentValue, Is.EqualTo(100));
                Assert.That(p2Wellbeing.CurrentBand.Id, Is.EqualTo("healthy"));
                Assert.That(p2Wellbeing.MovementMultiplier, Is.EqualTo(1.0f));

                // Modify P2
                p2Wellbeing.SetValue(0); // Salo
                Assert.That(p2Wellbeing.CurrentValue, Is.EqualTo(0));
                Assert.That(p2Wellbeing.CurrentBand.Id, Is.EqualTo("salo"));
                Assert.That(p2Wellbeing.MovementMultiplier, Is.EqualTo(0.7f));

                // Assert P1 is still at 20
                Assert.That(p1Wellbeing.CurrentValue, Is.EqualTo(20));
                Assert.That(p1Wellbeing.CurrentBand.Id, Is.EqualTo("critical"));
            }
            finally
            {
                Object.DestroyImmediate(p1Object);
                Object.DestroyImmediate(p2Object);
            }
        }

        [Test]
        public void NormalizedValue_ReturnsExpectedRatio()
        {
            var wellbeing = CreateWellbeing(out var gameObject);

            try
            {
                wellbeing.SetValue(100);
                Assert.That(wellbeing.NormalizedValue, Is.EqualTo(1.0f).Within(0.001f));

                wellbeing.SetValue(50);
                Assert.That(wellbeing.NormalizedValue, Is.EqualTo(0.5f).Within(0.001f));

                wellbeing.SetValue(0);
                Assert.That(wellbeing.NormalizedValue, Is.EqualTo(0.0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PlayerController_ImmediatelyUpdatesMovementMultipliers_OnWellbeingChange()
        {
            var go = new GameObject("Player Test");
            try
            {
                go.AddComponent<Rigidbody2D>();
                go.AddComponent<BoxCollider2D>();
                go.AddComponent<PlayerGroundCheck>();
                go.AddComponent<PlayerInputReader>();
                var movement = go.AddComponent<PlayerMovement>();
                var wellbeing = go.AddComponent<WellbeingComponent>();
                var controller = go.AddComponent<PlayerController>();

                wellbeing.ConfigureBands(LoadBands());
                controller.EnsureInitialized();
                wellbeing.SetValue(100);

                Assert.That(movement.MovementMultiplier, Is.EqualTo(1.0f));
                Assert.That(movement.JumpMultiplier, Is.EqualTo(1.0f));

                // Change to Salo (0)
                wellbeing.SetValue(0);

                Assert.That(movement.MovementMultiplier, Is.EqualTo(0.7f));
                Assert.That(movement.JumpMultiplier, Is.EqualTo(0.8f));

                // Change to Critical (20)
                wellbeing.SetValue(20);

                Assert.That(movement.MovementMultiplier, Is.EqualTo(0.8f));
                Assert.That(movement.JumpMultiplier, Is.EqualTo(0.9f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static WellbeingComponent CreateWellbeing(out GameObject gameObject)
        {
            gameObject = new GameObject("Wellbeing Test");
            return gameObject.AddComponent<WellbeingComponent>();
        }

        private static WellbeingBandData[] LoadBands()
        {
            return new[]
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
        }
    }
}
