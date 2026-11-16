using System.Linq;
using HealthyRace.Visuals;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode.Visuals
{
    /// <summary>
    /// Membro em curva do rig de Zuri e Tom: comprimento fixo, lado da dobra e contorno
    /// fino na luz e grosso no peso.
    /// </summary>
    public sealed class HoseLimbTests
    {
        private GameObject _root;
        private Transform _anchor;
        private Transform _target;
        private HoseLimb _limb;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Rig");
            _anchor = new GameObject("Ombro").transform;
            _anchor.SetParent(_root.transform, false);
            _target = new GameObject("Mao").transform;
            _target.SetParent(_root.transform, false);
            var limbObject = new GameObject("Braco");
            limbObject.transform.SetParent(_root.transform, false);
            _limb = limbObject.AddComponent<HoseLimb>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_limb != null && _limb.Mesh != null)
            {
                Object.DestroyImmediate(_limb.Mesh);
            }

            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Curve_KeepsTheLimbLength_WhileTheArcFits()
        {
            var anchor = new Vector2(0f, 1f);
            foreach (var distance in new[] { 0.3f, 0.36f, 0.42f, 0.49f })
            {
                var curve = HoseCurve.Solve(anchor, anchor + Vector2.down * distance, 0.5f, 1f);
                Assert.That(curve.IsStraight, Is.False);
                Assert.That(curve.Length, Is.EqualTo(0.5f).Within(0.002f), $"alvo a {distance}");
                Assert.That(curve.ArcDegrees, Is.LessThan(HoseCurve.MaxArcDegrees));
            }
        }

        [Test]
        public void Curve_ShortensInsteadOfCurlingUp_WhenTheTargetIsTooClose()
        {
            // alvo a 0,1 de um membro de 0,5: o arco para no limite e o membro encurta
            var anchor = new Vector2(0f, 1f);
            var curve = HoseCurve.Solve(anchor, anchor + new Vector2(0.06f, -0.08f), 0.5f, 1f);
            Assert.That(curve.ArcDegrees, Is.EqualTo(HoseCurve.MaxArcDegrees).Within(0.01f));
            Assert.That(curve.Length, Is.LessThan(0.25f));
            Assert.That(curve.Length, Is.GreaterThan(0.1f));

            // alvo em cima da âncora: nada a desenhar além das pontas, e nenhum erro
            var collapsed = HoseCurve.Solve(anchor, anchor, 0.5f, 1f);
            Assert.That(collapsed.IsStraight, Is.True);
            Assert.That(collapsed.Length, Is.Zero);
            Assert.That(collapsed.Tangent(0.5f).magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Curve_IsStraight_WhenTheTargetIsAtOrBeyondTheLength()
        {
            var curve = HoseCurve.Solve(Vector2.zero, new Vector2(0.8f, 0f), 0.5f, 1f);
            Assert.That(curve.IsStraight, Is.True);
            Assert.That(curve.Point(0.5f), Is.EqualTo(new Vector2(0.4f, 0f)));
            Assert.That(curve.Length, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(curve.Tangent(0.3f), Is.EqualTo(Vector2.right));
        }

        [Test]
        public void Curve_BendsToTheChosenSide()
        {
            // da âncora para a direita: bend positivo dobra para cima, negativo para baixo
            var anchor = Vector2.zero;
            var target = new Vector2(0.3f, 0f);
            var up = HoseCurve.Solve(anchor, target, 0.5f, 1f).Point(0.5f);
            var down = HoseCurve.Solve(anchor, target, 0.5f, -1f).Point(0.5f);
            Assert.That(up.y, Is.GreaterThan(0.05f));
            Assert.That(up.x, Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(down.x, Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(down.y, Is.EqualTo(-up.y).Within(0.001f));
        }

        [Test]
        public void Curve_StartsAtTheAnchor_EndsAtTheTarget_AndTurnsSmoothly()
        {
            var anchor = new Vector2(0.1f, 0.9f);
            var target = new Vector2(0.35f, 0.6f);
            var curve = HoseCurve.Solve(anchor, target, 0.5f, -1f);
            Assert.That(Vector2.Distance(curve.Point(0f), anchor), Is.LessThan(0.0005f));
            Assert.That(Vector2.Distance(curve.Point(1f), target), Is.LessThan(0.0005f));

            // arco de círculo: a direção gira a mesma quantidade a cada passo, sem quina
            const int steps = 10;
            var turns = new float[steps];
            for (var i = 0; i < steps; i++)
            {
                var from = curve.Tangent(i / (float)steps);
                var to = curve.Tangent((i + 1) / (float)steps);
                Assert.That(from.magnitude, Is.EqualTo(1f).Within(0.001f));
                turns[i] = Vector2.SignedAngle(from, to);
            }

            Assert.That(turns.Max() - turns.Min(), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(turns.Sum()), Is.EqualTo(curve.ArcDegrees).Within(0.05f));
        }

        [Test]
        public void Limb_BuildsInkUnderTheFill_WithRoundCaps()
        {
            _anchor.position = new Vector3(0f, 1f);
            _target.position = new Vector3(0.3f, 0.7f);
            _limb.Configure(_anchor, _target, 0.5f, 0.09f, Color.white);

            var mesh = _limb.Mesh;
            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.vertexCount, Is.EqualTo(HoseLimb.VerticesPerStrip * 2));
            // tinta na primeira metade, preenchimento na segunda
            var colors = mesh.colors32;
            Assert.That(colors[0], Is.Not.EqualTo(colors[HoseLimb.VerticesPerStrip]));
            Assert.That(colors[HoseLimb.VerticesPerStrip], Is.EqualTo((Color32)Color.white));

            // as pontas redondas cobrem a âncora e o alvo com folga de meia largura
            Assert.That(mesh.bounds.Contains(_anchor.position), Is.True);
            Assert.That(mesh.bounds.Contains(_target.position), Is.True);
            Assert.That(mesh.bounds.max.y, Is.GreaterThan(1f + 0.04f));
        }

        [Test]
        public void Limb_InkIsThinTowardsTheLight_AndThickOnTheWeightSide()
        {
            // membro horizontal e esticado: a luz vem de cima
            _anchor.position = new Vector3(0f, 1f);
            _target.position = new Vector3(0.5f, 1f);
            _limb.Configure(_anchor, _target, 0.5f, 0.1f, Color.white);
            _limb.SetInk(Color.black, 0.01f, 0.03f);
            _limb.Rebuild();

            var vertices = _limb.Mesh.vertices;
            var ink = vertices.Take(HoseLimb.VerticesPerStrip).ToArray();
            var fill = vertices.Skip(HoseLimb.VerticesPerStrip).ToArray();
            var above = ink.Max(v => v.y) - fill.Max(v => v.y);
            var below = fill.Min(v => v.y) - ink.Min(v => v.y);

            Assert.That(above, Is.GreaterThan(0f), "há contorno em cima");
            Assert.That(below, Is.GreaterThan(above * 1.5f), "o contorno de baixo é mais grosso");
            Assert.That(below, Is.LessThanOrEqualTo(0.03f + 0.0005f));
        }

        [Test]
        public void Limb_FollowsTheTarget_AndTheBend()
        {
            _anchor.position = new Vector3(0f, 1f);
            _target.position = new Vector3(0f, 0.7f);
            _limb.Configure(_anchor, _target, 0.5f, 0.08f, Color.white, bend: 1f);
            var leftward = _limb.Mesh.bounds.center.x;

            _limb.Bend = -1f;
            _limb.Rebuild();
            var rightward = _limb.Mesh.bounds.center.x;
            Assert.That(leftward, Is.Not.EqualTo(rightward).Within(0.02f));
            Assert.That(Mathf.Sign(leftward), Is.Not.EqualTo(Mathf.Sign(rightward)));

            _target.position = new Vector3(0.4f, 1f);
            _limb.Rebuild();
            Assert.That(_limb.Mesh.bounds.Contains(_target.position), Is.True);
        }

        [Test]
        public void Limb_WithoutAnchorOrTarget_DoesNothing()
        {
            _limb.Rebuild();
            Assert.That(_limb.Mesh, Is.Null);
        }
    }
}
