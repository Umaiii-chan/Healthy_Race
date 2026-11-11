using System.Collections.Generic;
using UnityEngine;

namespace HealthyRace.Visuals
{
    public enum RunnerFace
    {
        Neutral,
        Blink,
        Effort,

        /// <summary>Queda do lanche: pálpebra pesada e bocejo.</summary>
        Sleepy,

        /// <summary>Pico do lanche: olho arregalado e sorriso aberto.</summary>
        Rush,
    }

    /// <summary>
    /// Corredor humano (Zuri ou Tom) com braços e pernas em curva. O corpo e a pele não
    /// mudam com o Bem-Estar: quem mostra o estado são o rosto e as pontas da faixa de
    /// cabeça, que levantam com Bem-Estar alto e caem com ele baixo. Os clipes animam os
    /// alvos das mãos e dos pés; os membros (<see cref="HoseLimb"/>) se redesenham sozinhos.
    /// Não escreve em física, entrada nem regra de jogo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunnerRig : MonoBehaviour
    {
        /// <summary>Ângulo das duas pontas da faixa com Bem-Estar cheio (levantadas).</summary>
        public static readonly Vector2 TailsUp = new(-32f, -8f);

        /// <summary>Ângulo das duas pontas da faixa com Bem-Estar zero (caídas).</summary>
        public static readonly Vector2 TailsDown = new(40f, 62f);

        [SerializeField]
        private Transform _flip;

        [SerializeField]
        private SpriteRenderer _face;

        [SerializeField]
        private Sprite[] _faces = new Sprite[5];

        [SerializeField]
        private Transform _tailA;

        [SerializeField]
        private Transform _tailB;

        [SerializeField]
        private HoseLimb[] _limbs = new HoseLimb[0];

        [SerializeField]
        private Transform[] _targets = new Transform[0];

        private RunnerFace _currentFace;

        public int Facing { get; private set; } = 1;

        public RunnerFace Face => _currentFace;

        public Sprite FaceSprite => _face != null ? _face.sprite : null;

        /// <summary>Membros na ordem: braço de trás, perna de trás, perna da frente, braço da frente.</summary>
        public IReadOnlyList<HoseLimb> Limbs => _limbs;

        /// <summary>Alvos na mesma ordem dos membros: mão, pé, pé, mão.</summary>
        public IReadOnlyList<Transform> Targets => _targets;

        public Transform TailA => _tailA;

        public Transform TailB => _tailB;

        public void Configure(
            Transform flip,
            SpriteRenderer face,
            Sprite[] faces,
            Transform tailA,
            Transform tailB,
            HoseLimb[] limbs,
            Transform[] targets
        )
        {
            _flip = flip;
            _face = face;
            _faces = faces;
            _tailA = tailA;
            _tailB = tailB;
            _limbs = limbs;
            _targets = targets;
        }

        /// <summary>Vira o personagem para o lado do movimento; zero mantém o lado atual.</summary>
        public void SetFacing(float direction)
        {
            if (Mathf.Abs(direction) < 0.01f || _flip == null)
            {
                return;
            }

            Facing = direction > 0f ? 1 : -1;
            var scale = _flip.localScale;
            scale.x = Mathf.Abs(scale.x) * Facing;
            _flip.localScale = scale;
        }

        public void SetFace(RunnerFace face)
        {
            _currentFace = face;
            var index = (int)face;
            if (_face != null && _faces != null && index < _faces.Length && _faces[index] != null)
            {
                _face.sprite = _faces[index];
            }
        }

        /// <summary>
        /// Posição das pontas da faixa para o Bem-Estar dado (0 a 1): é o indicador que
        /// se lê em silhueta e em tamanho de jogo.
        /// </summary>
        public void SetWellbeing(float wellbeing01)
        {
            var angles = Vector2.Lerp(TailsDown, TailsUp, Mathf.Clamp01(wellbeing01));
            if (_tailA != null)
            {
                _tailA.localRotation = Quaternion.Euler(0f, 0f, angles.x);
            }

            if (_tailB != null)
            {
                _tailB.localRotation = Quaternion.Euler(0f, 0f, angles.y);
            }
        }

        /// <summary>Redesenha os quatro membros agora (útil fora do modo de jogo).</summary>
        public void RebuildLimbs()
        {
            foreach (var limb in _limbs)
            {
                if (limb != null)
                {
                    limb.Rebuild();
                }
            }
        }
    }
}
