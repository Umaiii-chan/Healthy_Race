using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.UI
{
    /// <summary>
    /// Gráfico da tela de resultado: uma linha de liderança da largada à chegada. Acima da
    /// linha do meio, o jogador 1 estava na frente; abaixo, o jogador 2. A área entre a
    /// linha e o meio é preenchida, para a virada ser lida de relance.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LeadChart : MaskableGraphic
    {
        private static readonly Color32 Ink = new(0x1E, 0x2A, 0x22, 255);
        private static readonly Color32 Line = new(0x5B, 0xC2, 0x4A, 255);
        private static readonly Color32 Area = new(0xD6, 0xDC, 0xDE, 255);
        private static readonly Color32 Middle = new(0x9A, 0xA3, 0x9C, 255);

        private float[] _leads = new float[0];

        /// <summary>Menor vantagem que ocupa a altura toda: evita ampliar ruído.</summary>
        public const float MinimumSpan = 0.06f;

        public float[] Leads => _leads;

        public void SetLeads(float[] leads)
        {
            _leads = leads ?? new float[0];
            SetVerticesDirty();
        }

        /// <summary>Ponto da linha (em coordenadas locais) numa posição do percurso de 0 a 1.</summary>
        public Vector2 PointAt(float position)
        {
            var rect = GetPixelAdjustedRect();
            if (_leads.Length < 2)
            {
                return new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, position), rect.center.y);
            }

            var scaled = Mathf.Clamp01(position) * (_leads.Length - 1);
            var index = Mathf.Min(Mathf.FloorToInt(scaled), _leads.Length - 2);
            var lead = Mathf.Lerp(_leads[index], _leads[index + 1], scaled - index);
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, position),
                rect.center.y + lead * Scale(rect)
            );
        }

        private float Scale(Rect rect)
        {
            var span = MinimumSpan;
            foreach (var lead in _leads)
            {
                span = Mathf.Max(span, Mathf.Abs(lead));
            }

            return rect.height * 0.45f / span;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var middle = rect.center.y;

            // linha do meio, tracejada
            for (var x = rect.xMin; x < rect.xMax; x += 26f)
            {
                Quad(
                    vh,
                    new Vector2(x, middle - 1.5f),
                    new Vector2(x, middle + 1.5f),
                    new Vector2(Mathf.Min(x + 14f, rect.xMax), middle + 1.5f),
                    new Vector2(Mathf.Min(x + 14f, rect.xMax), middle - 1.5f),
                    Middle
                );
            }

            if (_leads.Length < 2)
            {
                return;
            }

            var points = new Vector2[_leads.Length];
            var scale = Scale(rect);
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(
                    Mathf.Lerp(rect.xMin, rect.xMax, i / (float)(points.Length - 1)),
                    middle + _leads[i] * scale
                );
            }

            for (var i = 0; i < points.Length - 1; i++)
            {
                Quad(
                    vh,
                    new Vector2(points[i].x, middle),
                    points[i],
                    points[i + 1],
                    new Vector2(points[i + 1].x, middle),
                    Area
                );
            }

            Stroke(vh, points, 15f, Ink);
            Stroke(vh, points, 8f, Line);
        }

        private static void Stroke(VertexHelper vh, Vector2[] points, float width, Color32 color)
        {
            var half = width * 0.5f;
            for (var i = 0; i < points.Length - 1; i++)
            {
                var direction = (points[i + 1] - points[i]).normalized;
                var normal = new Vector2(-direction.y, direction.x) * half;
                Quad(
                    vh,
                    points[i] - normal,
                    points[i] + normal,
                    points[i + 1] + normal,
                    points[i + 1] - normal,
                    color
                );
            }

            // um quadrado em cada vértice fecha a quina entre dois segmentos
            foreach (var point in points)
            {
                Quad(
                    vh,
                    point + new Vector2(-half, -half),
                    point + new Vector2(-half, half),
                    point + new Vector2(half, half),
                    point + new Vector2(half, -half),
                    color
                );
            }
        }

        private static void Quad(
            VertexHelper vh,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            Vector2 d,
            Color32 color
        )
        {
            var start = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddVert(d, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
