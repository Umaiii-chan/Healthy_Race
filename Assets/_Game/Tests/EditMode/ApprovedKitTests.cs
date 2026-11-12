using System.Linq;
using HealthyRace.Collectibles;
using HealthyRace.Scoring;
using HealthyRace.UI;
using HealthyRace.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    /// <summary>
    /// O kit aprovado em 11/11/2026 no jogo: peças do HUD 5 e do Cenário 3 em Resources,
    /// alimentos com arte própria, folhas do Bem-Estar e a liderança da tela de resultado.
    /// </summary>
    public sealed class ApprovedKitTests
    {
        [Test]
        public void HudKit_IsInResources_WithEveryPiece_AndTenPortraits()
        {
            var kit = HudKit.Load();
            Assert.That(kit, Is.Not.Null, "rode 'Build HUD kit'");
            Assert.That(kit.IsComplete, Is.True);
            Assert.That(kit.StarOn, Is.Not.Null);
            Assert.That(kit.StarOff, Is.Not.Null);
            Assert.That(kit.Portraits.Length, Is.EqualTo(10));
            Assert.That(kit.Portraits.All(portrait => portrait != null), Is.True);
            Assert.That(kit.Portrait(1, 0).name, Is.EqualTo("Portrait_Zuri_0"));
            Assert.That(kit.Portrait(2, 4).name, Is.EqualTo("Portrait_Tom_4"));
            // a etiqueta é fatiada: sem borda, o canto arredondado estica junto com o texto
            Assert.That(kit.Tag.border, Is.Not.EqualTo(Vector4.zero));
        }

        [Test]
        public void SceneryKit_IsInResources_WithEveryPiece_AndALandmarkForEachLevel()
        {
            var kit = SceneryKit.Load();
            Assert.That(kit, Is.Not.Null, "rode 'Build HUD kit'");
            Assert.That(kit.IsComplete, Is.True);
            Assert.That(kit.Landmarks.Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(kit.Landmarks.Distinct().Count(), Is.EqualTo(kit.Landmarks.Length));
            for (var level = 1; level <= 10; level++)
            {
                Assert.That(kit.LandmarkFor(level), Is.Not.Null, "fase " + level);
            }

            // fases vizinhas não repetem o marco
            Assert.That(kit.LandmarkFor(1), Is.Not.SameAs(kit.LandmarkFor(2)));
            Assert.That(kit.ForkLeft, Is.Not.SameAs(kit.ForkRight));
            // elevador fatiado e grade ladrilhada pedem retângulo cheio
            Assert.That(kit.Lift.border, Is.Not.EqualTo(Vector4.zero));
            Assert.That(kit.Gate.triangles.Length, Is.EqualTo(6), "a grade é um retângulo cheio");
        }

        [Test]
        public void Foods_UseTheNewArt_NotThePixelSprites()
        {
            var foods = AssetDatabase
                .FindAssets("t:FoodData", new[] { "Assets/_Game/ScriptableObjects/Food" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<FoodData>)
                .ToArray();
            Assert.That(foods.Length, Is.EqualTo(18));
            foreach (var food in foods)
            {
                var sprite = food.Sprite;
                Assert.That(sprite, Is.Not.Null, food.name);
                Assert.That(sprite.texture.width, Is.EqualTo(256), food.name);
                Assert.That(sprite.texture.filterMode, Is.Not.EqualTo(FilterMode.Point), food.name);
                // o item, com a folha ou a bandeja, ocupa perto de uma célula da fase
                Assert.That(sprite.bounds.size.x, Is.InRange(0.9f, 1.4f), food.name);
            }

            Assert.That(foods.Select(food => food.Sprite).Distinct().Count(), Is.EqualTo(18));
        }

        [TestCase(100, 5)]
        [TestCase(81, 5)]
        [TestCase(80, 4)]
        [TestCase(41, 3)]
        [TestCase(40, 2)]
        [TestCase(1, 1)]
        [TestCase(0, 0)]
        public void Leaves_EachOneIsWorthTwentyPoints(int wellbeing, int leaves)
        {
            Assert.That(GreenhouseHud.LeavesFor(wellbeing), Is.EqualTo(leaves));
        }

        [Test]
        public void Trace_KeepsOneSampleEachQuarterSecond()
        {
            var trace = new RaceTrace();
            trace.Record(0f, 0f, 0f, false, false);
            trace.Record(0.1f, 0.1f, 0f, false, false);
            trace.Record(0.24f, 0.2f, 0f, false, false);
            trace.Record(0.25f, 0.3f, 0f, false, false);
            Assert.That(trace.Count, Is.EqualTo(2));
        }

        [Test]
        public void Trace_FindsTheLastLeadChange_AndWhetherItCameWithARush()
        {
            var trace = new RaceTrace();
            // P1 sai na frente, P2 passa no meio, P1 retoma perto do fim, no impulso do lanche
            var one = new[] { 0f, 0.10f, 0.20f, 0.28f, 0.36f, 0.50f, 0.62f, 0.80f, 1.00f };
            var two = new[] { 0f, 0.08f, 0.18f, 0.30f, 0.44f, 0.58f, 0.66f, 0.74f, 0.90f };
            for (var i = 0; i < one.Length; i++)
            {
                trace.Record(i, one[i], two[i], rushOne: i == 7, rushTwo: false);
            }

            Assert.That(trace.TryLastLeadChange(out var position, out var leader, out var rush));
            Assert.That(leader, Is.EqualTo(1));
            Assert.That(rush, Is.True);
            Assert.That(position, Is.EqualTo(0.8f).Within(0.001f));

            var leads = trace.Leads(9);
            Assert.That(leads[1], Is.GreaterThan(0f), "P1 na frente no começo");
            Assert.That(leads[4], Is.LessThan(0f), "P2 na frente no meio");
            Assert.That(leads[8], Is.GreaterThan(0f), "P1 na frente no fim");
            Assert.That(
                trace.Describe(1, 5),
                Is.EqualTo("O impulso do lanche no 5º andar colocou P1 na frente.")
            );
        }

        [Test]
        public void Trace_WithoutALeadChange_SaysWhoLedAllTheWay()
        {
            var trace = new RaceTrace();
            for (var i = 0; i <= 8; i++)
            {
                trace.Record(i, i * 0.1f, i * 0.12f, false, false);
            }

            Assert.That(trace.TryLastLeadChange(out _, out _, out _), Is.False);
            Assert.That(trace.Describe(2, 6), Is.EqualTo("P2 liderou do começo ao fim."));
            Assert.That(new RaceTrace().Leads(4), Is.EqualTo(new float[4]));
        }
    }
}
