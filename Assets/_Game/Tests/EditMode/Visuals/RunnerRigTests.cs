using System.Linq;
using HealthyRace.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealthyRace.Tests.EditMode.Visuals
{
    /// <summary>
    /// Prefabs de Zuri e Tom (rig de membros em curva): peças no lugar, quatro membros
    /// ligados a âncora e alvo, ordem de desenho, rosto, faixa de cabeça e lado.
    /// </summary>
    public sealed class RunnerRigTests
    {
        private const string Folder = "Assets/_Game/Prefabs/Visual/";

        // nome, altura do topo do crânio a partir dos pés (folha de modelos: 1,36 e 1,42)
        private static readonly object[] Runners =
        {
            new object[] { "Zuri", 1.36f },
            new object[] { "Tom", 1.42f },
        };

        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
            if (_instance == null)
            {
                return;
            }

            foreach (var limb in _instance.GetComponentsInChildren<HoseLimb>())
            {
                if (limb.Mesh != null)
                {
                    Object.DestroyImmediate(limb.Mesh);
                }
            }

            Object.DestroyImmediate(_instance);
        }

        private RunnerRig Spawn(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                Folder + "PF_VIS_Runner_" + name + ".prefab"
            );
            Assert.That(prefab, Is.Not.Null, "rode 'Build runner rig (Zuri e Tom)'");
            _instance = Object.Instantiate(prefab);
            return _instance.GetComponent<RunnerRig>();
        }

        [TestCaseSource(nameof(Runners))]
        public void Prefab_HasEveryPart_AndTheModelSheetHeight(string name, float bodyHeight)
        {
            var rig = Spawn(name);
            Assert.That(rig, Is.Not.Null);

            var sprites = _instance.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(
                sprites.Select(s => s.name),
                Is.EquivalentTo(
                    new[]
                    {
                        "TorsoSprite",
                        "HairBack",
                        "HeadSprite",
                        "Face",
                        "HairFront",
                        "Band",
                        "TailA",
                        "TailB",
                        "HandBack",
                        "HandFront",
                        "FootBack",
                        "FootFront",
                    }
                )
            );
            Assert.That(sprites.All(s => s.sprite != null), Is.True, "peça sem sprite");
            Assert.That(
                sprites.All(s => s.sprite.name.StartsWith("Runner_" + name + "_")),
                Is.True,
                "peça de outro personagem"
            );

            // topo do crânio: centro da cabeça mais o raio (0,21), medido a partir dos pés
            var feet = _instance.transform.Find("Rig");
            var head = _instance.transform.Find("Rig/Body/Torso/Head/HeadSprite");
            var top = head.position.y - feet.position.y + 0.21f;
            Assert.That(top, Is.EqualTo(bodyHeight).Within(0.005f));
        }

        [TestCaseSource(nameof(Runners))]
        public void Prefab_HasFourCurvedLimbs_FromBodyToHandsAndFeet(string name, float _)
        {
            var rig = Spawn(name);
            Assert.That(
                rig.Limbs.Select(l => l.name),
                Is.EqualTo(new[] { "ArmBack", "LegBack", "LegFront", "ArmFront" })
            );
            Assert.That(
                rig.Targets.Select(t => t.name),
                Is.EqualTo(new[] { "HandBack", "FootBack", "FootFront", "HandFront" })
            );

            for (var i = 0; i < rig.Limbs.Count; i++)
            {
                var limb = rig.Limbs[i];
                Assert.That(limb.Anchor, Is.Not.Null, limb.name);
                Assert.That(limb.Target, Is.SameAs(rig.Targets[i]), limb.name);
                Assert.That(limb.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.Null);
                // na pose de repouso o membro alcança o alvo com folga: está dobrado, não esticado
                var reach = Vector2.Distance(limb.Anchor.position, limb.Target.position);
                Assert.That(reach, Is.LessThan(limb.Length), limb.name + " esticado no repouso");
                Assert.That(
                    reach,
                    Is.GreaterThan(limb.Length * 0.6f),
                    limb.name + " dobrado demais"
                );
            }

            // cotovelo para trás, joelho para a frente
            Assert.That(rig.Limbs[0].Bend, Is.LessThan(0f));
            Assert.That(rig.Limbs[3].Bend, Is.LessThan(0f));
            Assert.That(rig.Limbs[1].Bend, Is.GreaterThan(0f));
            Assert.That(rig.Limbs[2].Bend, Is.GreaterThan(0f));

            rig.RebuildLimbs();
            Assert.That(rig.Limbs.All(l => l.Mesh != null && l.Mesh.vertexCount > 0), Is.True);
        }

        [TestCaseSource(nameof(Runners))]
        public void Prefab_DrawsBackToFront(string name, float _)
        {
            var rig = Spawn(name);
            int Order(string part) =>
                _instance
                    .GetComponentsInChildren<Renderer>()
                    .Single(r => r.name == part)
                    .sortingOrder;

            var order = new[]
            {
                "ArmBack",
                "HandBack",
                "LegBack",
                "FootBack",
                "LegFront",
                "FootFront",
                "TorsoSprite",
                "TailA",
                "HairBack",
                "HeadSprite",
                "Face",
                "HairFront",
                "Band",
                "ArmFront",
                "HandFront",
            };
            for (var i = 1; i < order.Length; i++)
            {
                Assert.That(
                    Order(order[i]),
                    Is.GreaterThan(Order(order[i - 1])),
                    order[i] + " tem de ficar na frente de " + order[i - 1]
                );
            }

            Assert.That(rig.Limbs.Count, Is.EqualTo(4));
        }

        [Test]
        public void Rig_ChangesFace_WithoutTouchingBodyOrSkin()
        {
            var rig = Spawn("Zuri");
            var head = _instance.transform.Find("Rig/Body/Torso/Head/HeadSprite");
            var headSprite = head.GetComponent<SpriteRenderer>().sprite;
            var scale = _instance.transform.Find("Rig/Body").localScale;

            var seen = new System.Collections.Generic.HashSet<Sprite>();
            foreach (
                var face in new[]
                {
                    RunnerFace.Neutral,
                    RunnerFace.Blink,
                    RunnerFace.Effort,
                    RunnerFace.Sleepy,
                    RunnerFace.Rush,
                }
            )
            {
                rig.SetFace(face);
                Assert.That(rig.Face, Is.EqualTo(face));
                Assert.That(seen.Add(rig.FaceSprite), Is.True, face + " repete outro rosto");
            }

            // o estado nunca mexe no corpo nem na pele
            Assert.That(head.GetComponent<SpriteRenderer>().sprite, Is.SameAs(headSprite));
            Assert.That(_instance.transform.Find("Rig/Body").localScale, Is.EqualTo(scale));
        }

        [Test]
        public void Rig_HeadbandTails_RiseWithWellbeing_AndDroopWithout()
        {
            var rig = Spawn("Tom");
            // a ponta sai do nó para trás (−x): ângulo negativo levanta, positivo deixa cair
            float TipHeight(Transform tail) =>
                (tail.TransformPoint(Vector3.left) - tail.position).y;

            rig.SetWellbeing(1f);
            var up = (TipHeight(rig.TailA), TipHeight(rig.TailB));
            rig.SetWellbeing(0.5f);
            var middle = (TipHeight(rig.TailA), TipHeight(rig.TailB));
            rig.SetWellbeing(0f);
            var down = (TipHeight(rig.TailA), TipHeight(rig.TailB));

            Assert.That(
                up.Item1,
                Is.GreaterThan(0.1f),
                "com Bem-Estar cheio a ponta aponta para cima"
            );
            Assert.That(down.Item1, Is.LessThan(-0.1f), "com Bem-Estar zero a ponta cai");
            Assert.That(down.Item2, Is.LessThan(-0.1f));
            Assert.That(middle.Item1, Is.InRange(down.Item1, up.Item1));
            Assert.That(middle.Item2, Is.InRange(down.Item2, up.Item2));
        }

        [Test]
        public void Rig_FacingLeft_MirrorsTheBody_AndKeepsTheKneeForward()
        {
            var rig = Spawn("Zuri");
            var flip = _instance.transform.Find("Rig");
            var leg = rig.Limbs[2];

            rig.SetFacing(1f);
            rig.RebuildLimbs();
            // barriga do arco em relação à linha quadril-pé, em espaço de mundo
            float Bulge() =>
                leg.transform.TransformPoint(leg.Mesh.bounds.center).x
                - (leg.Anchor.position.x + leg.Target.position.x) * 0.5f;
            var right = Bulge();

            rig.SetFacing(-1f);
            rig.RebuildLimbs();
            var left = Bulge();

            Assert.That(rig.Facing, Is.EqualTo(-1));
            Assert.That(flip.localScale.x, Is.EqualTo(-1f));
            Assert.That(
                right,
                Is.GreaterThan(0.005f),
                "virado para a direita o joelho vai para +x"
            );
            Assert.That(left, Is.LessThan(-0.005f), "virado para a esquerda o joelho vai para −x");

            rig.SetFacing(0f);
            Assert.That(rig.Facing, Is.EqualTo(-1), "sem direção, mantém o lado");
        }
    }
}
