using System.Collections.Generic;
using HealthyRace.Player;
using UnityEngine;

namespace HealthyRace.Puzzles
{
    /// <summary>
    /// Elevador de autoatendimento: sobe enquanto alguém está com os pés sobre a
    /// plataforma e volta quando ela fica vazia. A plataforma é de mão única, para
    /// passar por quem espera embaixo em vez de esmagá-lo; por isso quem está em cima
    /// é carregado aqui, explicitamente, e não pelo contato da física. Só conta quem
    /// está com os pés no topo: quem a plataforma atravessa ao descer não chama.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LiftRiderSensor : MonoBehaviour
    {
        /// <summary>Faixa acima do topo em que os pés contam como apoiados.</summary>
        private const float Above = 0.12f;

        /// <summary>Quanto os pés podem afundar no topo (um passo de subida) e ainda contar.</summary>
        private const float Below = 0.16f;

        private static readonly Collider2D[] Hits = new Collider2D[8];

        private readonly List<Rigidbody2D> _riders = new();
        private BoxCollider2D _platformCollider;
        private ActivatedPlatform _platform;

        public bool HasRider { get; private set; }

        public void Configure(BoxCollider2D platformCollider, ActivatedPlatform platform)
        {
            _platformCollider = platformCollider;
            _platform = platform;
        }

        private void FixedUpdate()
        {
            if (_platformCollider == null || _platform == null)
            {
                return;
            }

            var top = _platformCollider.bounds.max.y;
            FindRiders(top);
            foreach (var rider in _riders)
            {
                Carry(rider, top);
            }

            var hasRider = _riders.Count > 0;
            if (hasRider != HasRider)
            {
                HasRider = hasRider;
                _platform.SetState(hasRider);
            }
        }

        private void FindRiders(float top)
        {
            _riders.Clear();
            var bounds = _platformCollider.bounds;
            var center = new Vector2(bounds.center.x, top + (Above - Below) * 0.5f);
            var size = new Vector2(bounds.size.x - 0.1f, Above + Below);
            var filter = new ContactFilter2D { useTriggers = false };
            var count = Physics2D.OverlapBox(center, size, 0f, filter, Hits);
            for (var i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit == null || hit.attachedRigidbody == null)
                {
                    continue;
                }

                if (hit.GetComponentInParent<PlayerMovement>() == null)
                {
                    continue;
                }

                var feet = hit.bounds.min.y;
                if (feet >= top - Below && feet <= top + Above)
                {
                    _riders.Add(hit.attachedRigidbody);
                }
            }
        }

        /// <summary>Pés afundados no topo (a plataforma subiu um passo): sobe junto.</summary>
        private static void Carry(Rigidbody2D rider, float top)
        {
            var collider = rider.GetComponent<Collider2D>();
            var feet = collider != null ? collider.bounds.min.y : rider.position.y;
            if (feet >= top)
            {
                return;
            }

            rider.position += Vector2.up * (top - feet);
            var velocity = rider.linearVelocity;
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
                rider.linearVelocity = velocity;
            }
        }
    }
}
