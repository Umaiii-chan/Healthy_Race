using HealthyRace.Core;
using HealthyRace.WellBeing;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.UI
{
    /// <summary>
    /// A lightweight per-player HUD panel. It subscribes to only one wellbeing source.
    /// </summary>
    public sealed class PlayerHUD : MonoBehaviour
    {
        private const float PanelWidth = 260f;
        private const float PanelHeight = 132f;

        private Text _bandText;
        private Text _episodeText;
        private FatigueComponent _fatigue;
        private Image _fillImage;
        private Text _valueText;
        private WellbeingComponent _wellbeing;

        public void Initialize(
            Transform parent,
            string playerLabel,
            Color accentColor,
            WellbeingComponent wellbeing,
            FatigueComponent fatigue,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position
        )
        {
            _fatigue = fatigue;
            _wellbeing = wellbeing;
            transform.SetParent(parent, false);

            var panelRect = (RectTransform)transform;
            panelRect.anchorMin = anchor;
            panelRect.anchorMax = anchor;
            panelRect.pivot = pivot;
            panelRect.anchoredPosition = position;
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var panelImage = gameObject.AddComponent<Image>();
            panelImage.sprite = PlaceholderSpriteFactory.Square;
            panelImage.color = new Color(0.04f, 0.08f, 0.14f, 0.88f);

            CreateText(
                "PlayerLabel",
                transform,
                playerLabel,
                24,
                FontStyle.Bold,
                Color.white,
                new Vector2(14f, -9f)
            );
            _valueText = CreateText(
                "WellbeingValue",
                transform,
                string.Empty,
                18,
                FontStyle.Normal,
                Color.white,
                new Vector2(14f, -37f)
            );
            _bandText = CreateText(
                "WellbeingBand",
                transform,
                string.Empty,
                16,
                FontStyle.Normal,
                Color.white,
                new Vector2(14f, -60f)
            );

            var barBackground = CreateImage(
                "BarBackground",
                transform,
                new Color(0.1f, 0.16f, 0.23f)
            );
            ConfigureRect(
                (RectTransform)barBackground.transform,
                new Vector2(14f, -88f),
                new Vector2(232f, 12f)
            );

            _fillImage = CreateImage("WellbeingFill", barBackground.transform, accentColor);
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Horizontal;
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fillImage.fillAmount = 1f;
            ConfigureRect((RectTransform)_fillImage.transform, Vector2.zero, Vector2.one, true);

            _episodeText = CreateText(
                "FatigueEpisodes",
                transform,
                string.Empty,
                14,
                FontStyle.Normal,
                new Color(0.82f, 0.9f, 1f),
                new Vector2(14f, -107f)
            );

            _wellbeing.ValueChanged += HandleValueChanged;
            _wellbeing.WellbeingBandChanged += HandleBandChanged;

            if (_fatigue != null)
            {
                _fatigue.EpisodeCountChanged += HandleEpisodeCountChanged;
            }

            RefreshValue(_wellbeing.CurrentValue);
            RefreshBand(_wellbeing.CurrentBand);
            RefreshEpisodeCount(_fatigue?.FatigueEpisodeCount ?? 0);
        }

        private void OnDestroy()
        {
            if (_wellbeing != null)
            {
                _wellbeing.ValueChanged -= HandleValueChanged;
                _wellbeing.WellbeingBandChanged -= HandleBandChanged;
            }

            if (_fatigue != null)
            {
                _fatigue.EpisodeCountChanged -= HandleEpisodeCountChanged;
            }
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);

            var image = imageObject.AddComponent<Image>();
            image.sprite = PlaceholderSpriteFactory.Square;
            image.color = color;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            Vector2 position
        )
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            ConfigureRect((RectTransform)text.transform, position, new Vector2(232f, 24f));
            return text;
        }

        private static void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ConfigureRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool stretch
        )
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            if (!stretch)
            {
                rect.sizeDelta = anchorMax;
            }
        }

        private void HandleValueChanged(int currentValue)
        {
            RefreshValue(currentValue);
        }

        private void HandleBandChanged(WellbeingBandData band)
        {
            RefreshBand(band);
        }

        private void HandleEpisodeCountChanged(int episodeCount)
        {
            RefreshEpisodeCount(episodeCount);
        }

        private void RefreshBand(WellbeingBandData band)
        {
            _bandText.text = $"Estado: {band?.DisplayName ?? "Sem faixa"}";

            if (band != null)
            {
                _fillImage.color = band.CharacterColor;
            }
        }

        private void RefreshEpisodeCount(int episodeCount)
        {
            _episodeText.text = $"Fadigas: {episodeCount}";
        }

        private void RefreshValue(int currentValue)
        {
            _valueText.text = $"Bem-estar: {currentValue} / {WellbeingComponent.MaximumValue}";
            _fillImage.fillAmount = currentValue / (float)WellbeingComponent.MaximumValue;
        }
    }
}
