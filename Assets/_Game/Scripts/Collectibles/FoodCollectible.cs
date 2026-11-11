using System;
using System.Collections.Generic;
using HealthyRace.Player;
using HealthyRace.WellBeing;
using UnityEngine;

namespace HealthyRace.Collectibles
{
    /// <summary>
    /// Alimento coletável. Por padrão é <b>disputado</b>: o primeiro que encosta leva e
    /// o item some. Com <see cref="SetPerPlayer"/> cada jogador tem a sua vez (decisão
    /// D3): quem já pegou não pega de novo, e o item só some quando todos pegaram —
    /// quem vem atrás não encontra o caminho vazio.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class FoodCollectible : MonoBehaviour
    {
        /// <summary>Opacidade do item que já foi pego por alguém e espera o outro jogador.</summary>
        public const float WaitingAlpha = 0.55f;

        [SerializeField]
        private FoodData _foodData;

        private readonly HashSet<WellbeingComponent> _collectors = new();
        private int _requiredCollectors = 1;
        private Func<WellbeingComponent, Color> _waitingTint;
        private SpriteRenderer _renderer;
        private Color _restColor = Color.white;
        private bool _hasRestColor;

        public FoodData FoodData => _foodData;

        /// <summary>Verdadeiro quando ninguém mais pode pegar este item.</summary>
        public bool IsCollected => _collectors.Count >= _requiredCollectors;

        /// <summary>Cada jogador tem a sua vez (verdadeiro) ou o primeiro leva (falso).</summary>
        public bool IsPerPlayer => _requiredCollectors > 1;

        public int CollectorCount => _collectors.Count;

        /// <summary>Dispara a cada coleta, com quem coletou.</summary>
        public event Action<FoodCollectible, WellbeingComponent> Collected;

        private void Awake()
        {
            UpdateVisual();
        }

        public void Configure(FoodData foodData)
        {
            _foodData = foodData;
            UpdateVisual();
        }

        /// <summary>
        /// Dá uma vez a cada um dos <paramref name="playerCount"/> jogadores.
        /// <paramref name="waitingTint"/> devolve a cor de quem ainda falta pegar, usada
        /// para tingir o item enquanto ele espera.
        /// </summary>
        public void SetPerPlayer(
            int playerCount,
            Func<WellbeingComponent, Color> waitingTint = null
        )
        {
            _requiredCollectors = Mathf.Max(1, playerCount);
            _waitingTint = waitingTint;
        }

        public bool IsCollectedBy(WellbeingComponent player) =>
            player != null && _collectors.Contains(player);

        public void ResetState()
        {
            _collectors.Clear();
            gameObject.SetActive(true);
            if (_renderer != null && _hasRestColor)
            {
                _renderer.color = _restColor;
            }
        }

        private void UpdateVisual()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                return;
            }

            if (!_hasRestColor)
            {
                _restColor = _renderer.color;
                _hasRestColor = true;
            }

            if (_foodData != null && _foodData.Sprite != null)
            {
                _renderer.sprite = _foodData.Sprite;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsCollected || _foodData == null)
            {
                return;
            }

            var wellbeing = other.GetComponentInParent<WellbeingComponent>();

            if (wellbeing == null || !_collectors.Add(wellbeing))
            {
                return;
            }

            wellbeing.ApplyDelta(_foodData.WellbeingDelta);
            // lanche: pico agora, queda depois; fruta e água acordam da queda
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null)
            {
                if (_foodData.StartsRush)
                {
                    movement.EatSnack();
                }
                else if (_foodData.EndsCrash)
                {
                    movement.CureCrash();
                }
            }

            Collected?.Invoke(this, wellbeing);

            if (_foodData.CollectSound != null)
            {
                AudioSource.PlayClipAtPoint(_foodData.CollectSound, transform.position);
            }

            if (IsCollected)
            {
                gameObject.SetActive(false);
            }
            else
            {
                ShowWaiting(wellbeing);
            }
        }

        /// <summary>Item já pego por alguém: fica mais apagado, na cor de quem falta.</summary>
        private void ShowWaiting(WellbeingComponent lastCollector)
        {
            if (_renderer == null)
            {
                return;
            }

            var tint = _waitingTint != null ? _waitingTint(lastCollector) : _restColor;
            // meio caminho entre a cor do item e a de quem falta, para o desenho continuar legível
            var color = Color.Lerp(_restColor, tint, 0.5f);
            color.a = WaitingAlpha;
            _renderer.color = color;
        }
    }
}
