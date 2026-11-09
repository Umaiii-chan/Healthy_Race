using System;
using System.Linq;
using HealthyRace.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealthyRace.Tests.Visuals
{
    /// <summary>
    /// O personagem articulado depende de três coisas serializadas baterem entre si:
    /// as peles no catálogo, os ossos no prefab e os caminhos usados pelos clipes.
    /// </summary>
    public sealed class CharacterRigConfigurationTests
    {
        private const string CatalogPath = "Assets/_Game/Art/VisualAtlas/SO_VisualCatalog.asset";
        private const string SheetPath = "Assets/_Game/Art/Characters/SP_CharacterRig.png";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player.prefab";

        private static readonly string[] VisualPrefabs =
        {
            "Assets/_Game/Prefabs/Visual/PF_VIS_Player_P1.prefab",
            "Assets/_Game/Prefabs/Visual/PF_VIS_Player_P2.prefab",
        };

        private static VisualCatalog Catalog() =>
            AssetDatabase.LoadAssetAtPath<VisualCatalog>(CatalogPath);

        [Test]
        public void Sheet_HasEveryPartForEveryWellbeingState()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToArray();
            Assert.That(sprites.Length, Is.EqualTo(5 * 8), "5 estados x 8 partes.");
        }

        [Test]
        public void Catalog_HasACompleteSkinForEveryAppearance()
        {
            var catalog = Catalog();
            foreach (WellbeingAppearance appearance in Enum.GetValues(typeof(WellbeingAppearance)))
            {
                Assert.That(
                    catalog.TryGetSkin(appearance, out var skin),
                    Is.True,
                    $"Pele incompleta para {appearance}."
                );
                Assert.That(skin.Face, Is.Not.SameAs(skin.FaceBlink));
                Assert.That(skin.Face, Is.Not.SameAs(skin.FaceEffort));
            }

            Assert.That(
                catalog.Skins.Select(skin => skin.Head).Distinct().Count(),
                Is.EqualTo(5),
                "Cada estado tem a sua própria cabeça."
            );
        }

        [Test]
        public void VisualPrefabs_HaveARigBoundToThePresentation()
        {
            foreach (var path in VisualPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                var rig = prefab.GetComponent<CharacterRig>();
                Assert.That(rig, Is.Not.Null, $"{path} sem CharacterRig.");
                Assert.That(prefab.GetComponent<PlayerPresentation>().Rig, Is.SameAs(rig));
                Assert.That(rig.Skin.IsComplete, Is.True, $"{path} sem pele inicial.");
            }
        }

        [Test]
        public void EveryClip_AnimatesOnlyBonesThatExistInThePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabs[0]);
            var clips = Catalog().PlayerAnimator.animationClips;
            Assert.That(clips.Length, Is.EqualTo(Enum.GetValues(typeof(VisualAction)).Length));

            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                Assert.That(bindings, Is.Not.Empty, $"{clip.name} não anima nada.");
                foreach (var binding in bindings)
                {
                    Assert.That(
                        prefab.transform.Find(binding.path),
                        Is.Not.Null,
                        $"{clip.name} anima '{binding.path}', que não existe no prefab."
                    );
                }
            }
        }

        [Test]
        public void RunClip_MovesLegsAndArmsNotJustTheWholeBody()
        {
            var move = Catalog().PlayerAnimator.animationClips.Single(c => c.name == "AN_VIS_Move");
            var bindings = AnimationUtility.GetCurveBindings(move);
            foreach (var limb in new[] { "LegFront", "LegBack", "ArmFront", "ArmBack" })
            {
                var rotation = bindings
                    .Where(b =>
                        b.path.EndsWith(limb) && b.propertyName.Contains("localEulerAngles")
                    )
                    .ToArray();
                Assert.That(rotation, Is.Not.Empty, $"{limb} sem curva de rotação na corrida.");

                // uma curva constante passaria num teste que só olha os caminhos
                var keys = AnimationUtility.GetEditorCurve(move, rotation[0]).keys;
                var swing = keys.Max(k => k.value) - keys.Min(k => k.value);
                Assert.That(swing, Is.GreaterThan(30f), $"{limb} quase não se mexe na corrida.");
            }
        }

        [Test]
        public void PlayerCollider_IsFrictionless()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var material = prefab.GetComponent<Collider2D>().sharedMaterial;
            Assert.That(material, Is.Not.Null, "Sem material o jogador gruda nas paredes.");
            Assert.That(material.friction, Is.EqualTo(0f));
        }
    }
}
