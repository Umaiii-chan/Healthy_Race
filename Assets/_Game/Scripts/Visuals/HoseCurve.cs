using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>
    /// Geometria do membro "de mangueira" (rig 1 da folha de modelos de Zuri e Tom): um
    /// <b>arco de círculo</b> entre a âncora (ombro, quadril) e o alvo (mão, pé). O
    /// comprimento do membro é fixo: com o alvo perto, o arco abre para o lado de
    /// <c>bend</c>; com o alvo no limite ou além, o membro fica reto (e estica). O arco
    /// nunca passa de <see cref="MaxArcDegrees"/>: com o alvo perto demais o membro
    /// encurta em vez de se enrolar sobre si mesmo. Lógica pura, sem componentes:
    /// <see cref="HoseLimb"/> só transforma isto em malha.
    /// </summary>
    public readonly struct HoseCurve
    {
        /// <summary>Abertura máxima do arco, pouco mais que meio círculo.</summary>
        public const float MaxArcDegrees = 200f;

        private const int SolveIterations = 24;

        private readonly Vector2 _anchor;
        private readonly Vector2 _target;
        private readonly Vector2 _center;
        private readonly float _radius;
        private readonly float _startAngle;
        private readonly float _sweep;

        private HoseCurve(
            Vector2 anchor,
            Vector2 target,
            Vector2 center,
            float radius,
            float startAngle,
            float sweep
        )
        {
            _anchor = anchor;
            _target = target;
            _center = center;
            _radius = radius;
            _startAngle = startAngle;
            _sweep = sweep;
        }

        /// <summary>Verdadeiro quando o membro está reto (alvo no limite, além dele ou em cima da âncora).</summary>
        public bool IsStraight => _radius <= 0f;

        /// <summary>Abertura do arco em graus; 0 quando reto.</summary>
        public float ArcDegrees => Mathf.Abs(_sweep) * Mathf.Rad2Deg;

        /// <summary>Comprimento desenhado: o do membro, ou menos se o arco bateu no limite.</summary>
        public float Length =>
            IsStraight ? Vector2.Distance(_anchor, _target) : _radius * Mathf.Abs(_sweep);

        /// <summary>
        /// Resolve o arco. <paramref name="bend"/> escolhe o lado para onde o membro dobra,
        /// visto de quem vai da âncora ao alvo: positivo à esquerda, negativo à direita.
        /// </summary>
        public static HoseCurve Solve(Vector2 anchor, Vector2 target, float length, float bend)
        {
            var chord = target - anchor;
            var distance = chord.magnitude;
            if (length <= 0f || distance >= length || distance < 1e-5f)
            {
                return new HoseCurve(anchor, target, Vector2.zero, 0f, 0f, 0f);
            }

            // arco de abertura θ com corda d mede d · (θ/2) / sen(θ/2); procura o θ que dá "length"
            var maxHalf = MaxArcDegrees * 0.5f * Mathf.Deg2Rad;
            var wanted = length / distance;
            var half = maxHalf;
            if (wanted < maxHalf / Mathf.Sin(maxHalf))
            {
                var low = 0f;
                var high = maxHalf;
                for (var i = 0; i < SolveIterations; i++)
                {
                    var mid = (low + high) * 0.5f;
                    if (mid / Mathf.Sin(mid) < wanted)
                    {
                        low = mid;
                    }
                    else
                    {
                        high = mid;
                    }
                }

                half = (low + high) * 0.5f;
            }

            if (half < 1e-4f)
            {
                return new HoseCurve(anchor, target, Vector2.zero, 0f, 0f, 0f);
            }

            var side = bend < 0f ? -1f : 1f;
            var direction = chord / distance;
            var normal = new Vector2(-direction.y, direction.x) * side;
            var radius = distance / (2f * Mathf.Sin(half));
            // o centro fica do lado oposto ao da barriga do arco
            var center = (anchor + target) * 0.5f - normal * (radius * Mathf.Cos(half));
            var start = anchor - center;
            var startAngle = Mathf.Atan2(start.y, start.x);
            // barriga à esquerda de quem vai: gira no sentido horário; à direita, anti-horário
            return new HoseCurve(anchor, target, center, radius, startAngle, -side * 2f * half);
        }

        public Vector2 Point(float t)
        {
            if (IsStraight)
            {
                return Vector2.LerpUnclamped(_anchor, _target, t);
            }

            var angle = _startAngle + _sweep * t;
            return _center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _radius;
        }

        /// <summary>Direção do membro em <paramref name="t"/> (normalizada; nunca zero).</summary>
        public Vector2 Tangent(float t)
        {
            if (IsStraight)
            {
                var chord = _target - _anchor;
                return chord.sqrMagnitude < 1e-10f ? Vector2.down : chord.normalized;
            }

            var angle = _startAngle + _sweep * t;
            return new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * Mathf.Sign(_sweep);
        }
    }
}
