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
