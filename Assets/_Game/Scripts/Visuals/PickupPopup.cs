using UnityEngine;

namespace HealthyRace.Visuals
{
    /// <summary>
    /// Texto que sobe do jogador quando ele come: o nome do alimento, quanto mudou o
    /// Bem-Estar e o efeito ("PICO!", "ACORDOU!"). É o feedback de coleta (F5): o
    /// jogador vê o que ganhou perto do personagem, sem olhar para o HUD. Dura menos
    /// de um segundo e some sozinho.
    /// </summary>
    public sealed class PickupPopup : MonoBehaviour
    {
        public const float Lifetime = 0.9f;
        public const float RiseDistance = 1.1f;

        /// <summary>Altura do texto em tiles: legível mesmo com a câmera afastada.</summary>
        public const float TextHeight = 0.62f;

        private TextMesh _text;
        private Vector3 _origin;
        private Color _color;
        private float _age;

        public string Text => _text != null ? _text.text : string.Empty;

        public static Color HealthyColor => new(0.32f, 0.78f, 0.25f);
        public static Color SnackColor => new(0.98f, 0.74f, 0.12f);

        /// <summary>
        /// Monta a frase do pop-up. <paramref name="effect"/> é o efeito que o alimento
        /// teve agora (pode ser vazio).
        /// </summary>
        public static string Compose(string foodName, int wellbeingDelta, string effect)
        {
            var sign =
                wellbeingDelta > 0 ? "+"
                : wellbeingDelta < 0 ? "−"
                : string.Empty;
            var value = wellbeingDelta != 0 ? $" {sign}{Mathf.Abs(wellbeingDelta)}" : string.Empty;
            var line = $"{foodName}{value}".Trim();
            return string.IsNullOrEmpty(effect) ? line : $"{line}\n{effect}";
        }

        public static PickupPopup Spawn(
            Transform parent,
            Vector3 position,
            string text,
            Color color
        )
        {
            var popupObject = new GameObject("Coleta");
            popupObject.transform.SetParent(parent, false);
            popupObject.transform.position = position;
            var popup = popupObject.AddComponent<PickupPopup>();
            popup.Initialize(text, color);
            return popup;
        }

        private void Initialize(string text, Color color)
        {
            _origin = transform.position;
            _color = color;
            _text = gameObject.AddComponent<TextMesh>();
            _text.text = text;
            _text.anchor = TextAnchor.LowerCenter;
            _text.alignment = TextAlignment.Center;
            _text.fontStyle = FontStyle.Bold;
            // fonte grande reduzida pelo characterSize: nítida de perto e de longe
            _text.fontSize = 64;
            _text.characterSize = TextHeight * 10f / 64f;
            _text.color = color;
            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = 40;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / Lifetime);
            // sobe rápido e desacelera; some no terço final
            var eased = 1f - (1f - t) * (1f - t);
            transform.position = _origin + Vector3.up * (RiseDistance * eased);
            var color = _color;
            color.a = t < 0.66f ? 1f : Mathf.InverseLerp(1f, 0.66f, t);
            _text.color = color;
            if (_age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
