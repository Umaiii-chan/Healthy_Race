using System.Collections.Generic;
using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>
    /// Braço ou perna "de mangueira": uma faixa curva desenhada em malha entre a âncora
    /// (ombro, quadril) e o alvo (mão, pé), com pontas redondas. Por baixo vai uma segunda
    /// faixa na cor da tinta, mais larga, que faz o contorno: fino no lado da luz e grosso
    /// no lado do peso, como pede a Art Bible. Os clipes animam só a posição do alvo e
    /// <see cref="Bend"/>; a malha é refeita depois do Animator, em <c>LateUpdate</c>.
    /// Só desenha: não escreve em física, entrada nem regra de jogo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HoseLimb : MonoBehaviour
    {
        /// <summary>Divisões ao longo do membro.</summary>
        public const int Segments = 14;

        /// <summary>Divisões de cada ponta redonda.</summary>
        public const int CapSegments = 6;

        /// <summary>De onde vem a luz (cima, um pouco à esquerda), em espaço de mundo.</summary>
        public static readonly Vector2 LightDirection = new Vector2(-0.45f, 1f).normalized;

        private const string SpriteShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        private const string FallbackShader = "Sprites/Default";

        private static Material _sharedMaterial;

        [SerializeField]
        private Transform _anchor;

        [SerializeField]
        private Transform _target;

        [SerializeField, Min(0.01f), Tooltip("Comprimento do membro em unidades do jogo.")]
        private float _length = 0.5f;

        [SerializeField, Min(0.005f), Tooltip("Largura do preenchimento.")]
        private float _width = 0.09f;

        [SerializeField, Tooltip("Lado para onde o membro dobra: positivo ou negativo.")]
        private float _bend = 1f;

        [SerializeField]
        private Color _fill = Color.white;

        [SerializeField]
        private Color _ink = new(0.118f, 0.165f, 0.133f);

        [SerializeField, Min(0f), Tooltip("Contorno no lado da luz.")]
        private float _inkLight = 0.012f;

        [SerializeField, Min(0f), Tooltip("Contorno no lado do peso.")]
        private float _inkHeavy = 0.024f;

        [SerializeField, Tooltip("Material de sprite; sem ele usa o padrão do pipeline.")]
        private Material _material;

        private readonly List<Vector3> _vertices = new();
        private readonly List<Color32> _colors = new();
        private readonly List<int> _triangles = new();
        private Mesh _mesh;
        private MeshRenderer _renderer;

        public Transform Anchor => _anchor;

        public Transform Target => _target;

        public float Length => _length;

        public float Width => _width;

        /// <summary>Lado da dobra; os clipes animam este valor.</summary>
        public float Bend
        {
            get => _bend;
            set => _bend = value;
        }

        public Mesh Mesh => _mesh;

        public int SortingOrder
        {
            get => Renderer.sortingOrder;
            set => Renderer.sortingOrder = value;
        }

        /// <summary>Vértices de uma faixa (corpo mais as duas pontas).</summary>
        public static int VerticesPerStrip => (Segments + 1) * 2 + (CapSegments + 2) * 2;

        private MeshRenderer Renderer => _renderer ??= GetComponent<MeshRenderer>();

        public void Configure(
            Transform anchor,
            Transform target,
            float length,
            float width,
            Color fill,
            float bend = 1f,
            bool rebuild = true
        )
        {
            _anchor = anchor;
            _target = target;
            _length = Mathf.Max(0.01f, length);
            _width = Mathf.Max(0.005f, width);
            _fill = fill;
            _bend = bend;
            if (rebuild)
            {
                Rebuild();
            }
        }

        /// <summary>Material de sprite usado pela malha (o do catálogo visual).</summary>
        public void SetMaterial(Material material)
        {
            _material = material;
            Renderer.sharedMaterial = material;
        }

        public void SetInk(Color ink, float lightSide, float heavySide)
        {
            _ink = ink;
            _inkLight = Mathf.Max(0f, lightSide);
            _inkHeavy = Mathf.Max(0f, heavySide);
        }

        private void OnEnable()
        {
            Rebuild();
        }

        private void LateUpdate()
        {
            Rebuild();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_mesh);
                }
                else
                {
                    DestroyImmediate(_mesh);
                }
            }
        }

        /// <summary>Refaz a malha para a posição atual da âncora e do alvo.</summary>
        public void Rebuild()
        {
            if (_anchor == null || _target == null)
            {
                return;
            }

            EnsureMesh();
            Vector2 anchor = _anchor.position;
            Vector2 target = _target.position;
            // o lado da dobra acompanha o personagem: virado para a esquerda, o joelho
            // continua apontando para a frente dele
            var facing = transform.lossyScale.x < 0f ? -1f : 1f;
            var curve = HoseCurve.Solve(anchor, target, _length, _bend * facing);

            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
            // a tinta primeiro: os triângulos de uma malha são desenhados na ordem dos índices
            AddStrip(curve, _ink, _inkLight, _inkHeavy);
            AddStrip(curve, _fill, 0f, 0f);

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        private void EnsureMesh()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "HoseLimb" };
                _mesh.MarkDynamic();
                GetComponent<MeshFilter>().sharedMesh = _mesh;
            }

            if (Renderer.sharedMaterial == null)
            {
                Renderer.sharedMaterial = _material != null ? _material : SharedMaterial();
            }
        }

        private static Material SharedMaterial()
        {
            if (_sharedMaterial == null)
            {
                var shader = Shader.Find(SpriteShader) ?? Shader.Find(FallbackShader);
                if (shader != null)
                {
                    _sharedMaterial = new Material(shader) { name = "HoseLimb" };
                }
            }

            return _sharedMaterial;
        }

        /// <summary>
        /// Uma faixa ao longo da curva, com meia largura <c>_width / 2</c> mais o extra de
        /// tinta: <paramref name="lightExtra"/> na borda virada para a luz,
        /// <paramref name="heavyExtra"/> na borda oposta.
        /// </summary>
        private void AddStrip(HoseCurve curve, Color color, float lightExtra, float heavyExtra)
        {
            var first = _vertices.Count;
            Color32 color32 = color;
            var half = _width * 0.5f;

            for (var i = 0; i <= Segments; i++)
            {
                var t = i / (float)Segments;
                var point = curve.Point(t);
                var tangent = curve.Tangent(t);
                var normal = new Vector2(-tangent.y, tangent.x);
                Add(point + normal * (half + Extra(normal, lightExtra, heavyExtra)), color32);
                Add(point - normal * (half + Extra(-normal, lightExtra, heavyExtra)), color32);
                if (i > 0)
                {
                    var a = first + (i - 1) * 2;
                    Quad(a, a + 1, a + 3, a + 2);
                }
            }

            AddCap(curve.Point(0f), -curve.Tangent(0f), half, color32, lightExtra, heavyExtra);
            AddCap(curve.Point(1f), curve.Tangent(1f), half, color32, lightExtra, heavyExtra);
        }

        /// <summary>Meio círculo na ponta do membro, virado para <paramref name="outward"/>.</summary>
        private void AddCap(
            Vector2 center,
            Vector2 outward,
            float half,
            Color32 color,
            float lightExtra,
            float heavyExtra
        )
        {
            var centerIndex = _vertices.Count;
            Add(center, color);
            var side = new Vector2(-outward.y, outward.x);
            for (var i = 0; i <= CapSegments; i++)
            {
                var angle = Mathf.PI * i / CapSegments;
                var direction = side * Mathf.Cos(angle) + outward * Mathf.Sin(angle);
                Add(center + direction * (half + Extra(direction, lightExtra, heavyExtra)), color);
                if (i > 0)
                {
                    _triangles.Add(centerIndex);
                    _triangles.Add(centerIndex + i);
                    _triangles.Add(centerIndex + i + 1);
                }
            }
        }

        /// <summary>Espessura da tinta numa borda: fina virada para a luz, grossa do outro lado.</summary>
        private static float Extra(Vector2 outward, float lightExtra, float heavyExtra)
        {
            var facingLight = (Vector2.Dot(outward, LightDirection) + 1f) * 0.5f;
            return Mathf.Lerp(heavyExtra, lightExtra, facingLight);
        }

        private void Add(Vector2 world, Color32 color)
        {
            _vertices.Add(transform.InverseTransformPoint(world));
            _colors.Add(color);
        }

        private void Quad(int a, int b, int c, int d)
        {
            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
            _triangles.Add(a);
            _triangles.Add(c);
            _triangles.Add(d);
        }
    }
}
