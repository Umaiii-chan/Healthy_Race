using System;
using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>Partes de um personagem para um estado de Bem-Estar.</summary>
    [Serializable]
    public struct CharacterSkin
    {
        public Sprite Head;
        public Sprite Face;
        public Sprite FaceBlink;
        public Sprite FaceEffort;
        public Sprite Leaf;
        public Sprite Torso;
        public Sprite Arm;
        public Sprite Leg;

        public bool IsComplete =>
            Head != null
            && Face != null
            && FaceBlink != null
            && FaceEffort != null
            && Leaf != null
            && Torso != null
            && Arm != null
            && Leg != null;
    }

    public enum CharacterExpression
    {
        Normal,
        Effort,
    }

    /// <summary>
    /// Personagem articulado: troca a pele por estado de Bem-Estar, vira para o lado
    /// do movimento, pisca e balança a folha conforme a velocidade. As poses (correr,
    /// pular, empurrar...) vêm dos clipes do Animator; aqui fica só o que reage ao
    /// jogo quadro a quadro. Não escreve em física, entrada nem regra de jogo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterRig : MonoBehaviour
    {
        private const float BlinkDuration = 0.11f;
        private const float LeafStiffness = 90f;
        private const float LeafDamping = 9f;
        private const float LeafMaxAngle = 42f;

        [SerializeField]
        private Transform _flip;

        [SerializeField]
        private Transform _leafBone;

        [SerializeField]
        private SpriteRenderer _head;

        [SerializeField]
        private SpriteRenderer _face;

        [SerializeField]
        private SpriteRenderer _leaf;

        [SerializeField]
        private SpriteRenderer _torso;

        [SerializeField]
        private SpriteRenderer _armFront;

        [SerializeField]
        private SpriteRenderer _armBack;

        [SerializeField]
        private SpriteRenderer _legFront;

        [SerializeField]
        private SpriteRenderer _legBack;

        // Serializada: o prefab já nasce com uma pele, e piscar antes da primeira
        // troca de Bem-Estar não apaga o rosto.
        [SerializeField]
        private CharacterSkin _skin;

        private float _blinkUntil;
        private CharacterExpression _expression;
        private float _leafAngle;
        private float _leafVelocity;
        private float _nextBlink;

        /// <summary>+1 olhando para a direita, -1 para a esquerda.</summary>
        public int Facing { get; private set; } = 1;

        public CharacterExpression Expression => _expression;

        public bool IsBlinking => Time.time < _blinkUntil;

        public float LeafAngle => _leafAngle;

        /// <summary>Desliga piscar e balanço (opção "reduzir flashes e tremores").</summary>
        public bool ReduceMotion { get; set; }

        public CharacterSkin Skin => _skin;

        /// <summary>Velocidade do jogador no mundo, informada pela apresentação.</summary>
        public Vector2 Velocity { get; set; }

        public void Configure(
            Transform flip,
            Transform leafBone,
            SpriteRenderer head,
            SpriteRenderer face,
            SpriteRenderer leaf,
            SpriteRenderer torso,
            SpriteRenderer armFront,
            SpriteRenderer armBack,
            SpriteRenderer legFront,
            SpriteRenderer legBack
        )
        {
            _flip = flip;
            _leafBone = leafBone;
            _head = head;
            _face = face;
            _leaf = leaf;
            _torso = torso;
            _armFront = armFront;
            _armBack = armBack;
            _legFront = legFront;
            _legBack = legBack;
        }

        public void Apply(CharacterSkin skin)
        {
            _skin = skin;
            Set(_head, skin.Head);
            Set(_leaf, skin.Leaf);
            Set(_torso, skin.Torso);
            Set(_armFront, skin.Arm);
            Set(_armBack, skin.Arm);
            Set(_legFront, skin.Leg);
            Set(_legBack, skin.Leg);
            RefreshFace();
        }

        public void SetFacing(float direction)
        {
            if (Mathf.Abs(direction) < 0.01f)
            {
                return;
            }

            Facing = direction > 0f ? 1 : -1;
            if (_flip != null)
            {
                var scale = _flip.localScale;
                scale.x = Mathf.Abs(scale.x) * Facing;
                _flip.localScale = scale;
            }
        }

        public void SetExpression(CharacterExpression expression)
        {
            if (_expression == expression)
            {
                return;
            }

            _expression = expression;
            RefreshFace();
        }

        /// <summary>Fecha os olhos agora (também usado pelos testes).</summary>
        public void Blink()
        {
            _blinkUntil = Time.time + BlinkDuration;
            RefreshFace();
        }

        private void OnEnable()
        {
            ScheduleBlink();
        }

        private void LateUpdate()
        {
            UpdateBlink();
            UpdateLeaf(Time.deltaTime);
        }

        private void UpdateBlink()
        {
            if (ReduceMotion)
            {
                return;
            }

            var wasBlinking = _face != null && _face.sprite == _skin.FaceBlink;
            if (Time.time >= _nextBlink)
            {
                Blink();
                ScheduleBlink();
            }
            else if (wasBlinking && !IsBlinking)
            {
                RefreshFace();
            }
        }

        private void ScheduleBlink()
        {
            _nextBlink = Time.time + UnityEngine.Random.Range(2.2f, 5.5f);
        }

        /// <summary>
        /// A folha é uma mola: fica para trás quando o personagem corre e volta
        /// balançando quando ele para.
        /// </summary>
        private void UpdateLeaf(float deltaTime)
        {
            if (_leafBone == null || deltaTime <= 0f)
            {
                return;
            }

            if (ReduceMotion)
            {
                _leafAngle = 0f;
                _leafVelocity = 0f;
            }
            else
            {
                var forwardSpeed = Velocity.x * Facing;
                var target = Mathf.Clamp(
                    forwardSpeed * 4.5f - Velocity.y * 1.2f + Mathf.Sin(Time.time * 2.1f) * 3f,
                    -LeafMaxAngle,
                    LeafMaxAngle
                );
                // passos curtos mantêm a mola estável mesmo com quadros longos
                var step = Mathf.Min(deltaTime, 0.033f);
                _leafVelocity += (target - _leafAngle) * LeafStiffness * step;
                _leafVelocity -= _leafVelocity * LeafDamping * step;
                _leafAngle = Mathf.Clamp(
                    _leafAngle + _leafVelocity * step,
                    -LeafMaxAngle * 1.3f,
                    LeafMaxAngle * 1.3f
                );
            }

            _leafBone.localRotation = Quaternion.Euler(0f, 0f, _leafAngle);
        }

        private void RefreshFace()
        {
            if (_face == null)
            {
                return;
            }

            if (IsBlinking && !ReduceMotion && _skin.FaceBlink != null)
            {
                _face.sprite = _skin.FaceBlink;
            }
            else if (_expression == CharacterExpression.Effort && _skin.FaceEffort != null)
            {
                _face.sprite = _skin.FaceEffort;
            }
            else
            {
                _face.sprite = _skin.Face;
            }
        }

        private static void Set(SpriteRenderer renderer, Sprite sprite)
        {
            if (renderer != null && sprite != null)
            {
                renderer.sprite = sprite;
            }
        }
    }
}
