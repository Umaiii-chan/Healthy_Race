using System;
using HealthyRace.Collectibles;
using HealthyRace.Core;
using HealthyRace.WellBeing;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.UI
{
    /// <summary>
    /// A reactive per-player HUD panel. It displays Wellbeing, visual bands, fatigue states,
    /// food collection counts, score, and race progress in real time via events.
    /// </summary>
    public sealed class PlayerHUD : MonoBehaviour
    {
        private const float PanelWidth = 280f;
        private const float PanelHeight = 175f;

        private string _playerLabel = "P1";
        private Color _accentColor = Color.white;

        private Text _playerLabelText;
        private Text _scoreText;
        private Text _valueText;
        private Text _bandText;
        private Image _fillImage;
        private Text _fatigueStateText;
        private Text _episodeText;
        private Text _foodText;
        private Text _progressText;

        private WellbeingComponent _wellbeing;
        private FatigueComponent _fatigue;

        private int _score;
        private int _healthyFoodCount;
        private int _ultraProcessedFoodCount;
        private float _progress;

        public string PlayerLabel => _playerLabel;

        public int Score => _score;

        public int HealthyFoodCount => _healthyFoodCount;

        public int UltraProcessedFoodCount => _ultraProcessedFoodCount;

        public int TotalFoodCount => _healthyFoodCount + _ultraProcessedFoodCount;

        public float Progress => _progress;

        public WellbeingComponent Wellbeing => _wellbeing;

        public FatigueComponent Fatigue => _fatigue;

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
            _playerLabel = playerLabel;
            _accentColor = accentColor;
            transform.SetParent(parent, false);

            var panelRect = transform as RectTransform;
            if (panelRect != null)
            {
                panelRect.anchorMin = anchor;
                panelRect.anchorMax = anchor;
                panelRect.pivot = pivot;
                panelRect.anchoredPosition = position;
                panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            }

            var panelImage = gameObject.AddComponent<Image>();
            panelImage.sprite = PlaceholderSpriteFactory.Square;
            panelImage.color = new Color(0.04f, 0.08f, 0.14f, 0.88f);

            // Row 1: Player label (left) and Score (right)
            _playerLabelText = CreateText(
                "PlayerLabel",
                transform,
                _playerLabel,
                22,
                FontStyle.Bold,
                _accentColor,
                new Vector2(14f, -8f),
                new Vector2(120f, 26f),
                TextAnchor.UpperLeft
            );

            _scoreText = CreateText(
                "ScoreText",
                transform,
                "Pontos: 0",
                16,
                FontStyle.Bold,
                new Color(1f, 0.86f, 0.2f),
                new Vector2(136f, -12f),
                new Vector2(130f, 22f),
                TextAnchor.UpperRight
            );

            // Row 2: Wellbeing Value
            _valueText = CreateText(
                "WellbeingValue",
                transform,
                string.Empty,
                16,
                FontStyle.Normal,
                Color.white,
                new Vector2(14f, -34f),
                new Vector2(252f, 22f),
                TextAnchor.UpperLeft
            );

            // Row 3: Wellbeing Band Name
            _bandText = CreateText(
                "WellbeingBand",
                transform,
                string.Empty,
                14,
                FontStyle.Normal,
                new Color(0.85f, 0.92f, 1f),
                new Vector2(14f, -54f),
                new Vector2(252f, 20f),
                TextAnchor.UpperLeft
            );

            // Row 4: Wellbeing Fill Bar
            var barBackground = CreateImage(
                "BarBackground",
                transform,
                new Color(0.1f, 0.16f, 0.23f)
            );
            ConfigureRect(
                (RectTransform)barBackground.transform,
                new Vector2(14f, -76f),
                new Vector2(252f, 12f)
            );

            _fillImage = CreateImage("WellbeingFill", barBackground.transform, _accentColor);
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Horizontal;
            _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fillImage.fillAmount = 1f;
            ConfigureRect((RectTransform)_fillImage.transform, Vector2.zero, Vector2.one, true);

            // Row 5: Fatigue State & Episodes
            _fatigueStateText = CreateText(
                "FatigueState",
                transform,
                "Fadiga: Normal",
                13,
                FontStyle.Normal,
                new Color(0.82f, 0.9f, 1f),
                new Vector2(14f, -94f),
                new Vector2(130f, 20f),
                TextAnchor.UpperLeft
            );

            _episodeText = CreateText(
                "FatigueEpisodes",
                transform,
                "Episódios: 0",
                13,
                FontStyle.Normal,
                new Color(0.82f, 0.9f, 1f),
                new Vector2(146f, -94f),
                new Vector2(120f, 20f),
                TextAnchor.UpperRight
            );

            // Row 6: Food counts
            _foodText = CreateText(
                "FoodCount",
                transform,
                "Alimentos: 0 (0 🥗 | 0 🍔)",
                13,
                FontStyle.Normal,
                new Color(0.9f, 0.95f, 0.85f),
                new Vector2(14f, -116f),
                new Vector2(252f, 20f),
                TextAnchor.UpperLeft
            );

            // Row 7: Race progress
            _progressText = CreateText(
                "ProgressText",
                transform,
                "Progresso: 0%",
                13,
                FontStyle.Normal,
                new Color(0.75f, 0.85f, 0.95f),
                new Vector2(14f, -138f),
                new Vector2(252f, 20f),
                TextAnchor.UpperLeft
            );

            Bind(wellbeing, fatigue);
        }

        public void Bind(WellbeingComponent wellbeing, FatigueComponent fatigue)
        {
            Unbind();

            _wellbeing = wellbeing;
            _fatigue = fatigue;

            if (_wellbeing != null)
            {
                _wellbeing.ValueChanged += HandleValueChanged;
                _wellbeing.WellbeingBandChanged += HandleBandChanged;
                RefreshValue(_wellbeing.CurrentValue);
                RefreshBand(_wellbeing.CurrentBand);
            }

            if (_fatigue != null)
            {
                _fatigue.EpisodeCountChanged += HandleEpisodeCountChanged;
                _fatigue.StateChanged += HandleFatigueStateChanged;
                RefreshEpisodeCount(_fatigue.FatigueEpisodeCount);
                RefreshFatigueState(_fatigue.State);
            }
        }

        public void Unbind()
        {
            if (_wellbeing != null)
            {
                _wellbeing.ValueChanged -= HandleValueChanged;
                _wellbeing.WellbeingBandChanged -= HandleBandChanged;
                _wellbeing = null;
            }

            if (_fatigue != null)
            {
                _fatigue.EpisodeCountChanged -= HandleEpisodeCountChanged;
                _fatigue.StateChanged -= HandleFatigueStateChanged;
                _fatigue = null;
            }
        }

        public void UpdateScore(int score)
        {
            _score = score;
            if (_scoreText != null)
            {
                _scoreText.text = $"Pontos: {_score}";
            }
        }

        public void AddScore(int delta)
        {
            UpdateScore(_score + delta);
        }

        public void RegisterFoodCollected(FoodData food)
        {
            if (food == null)
            {
                return;
            }

            if (food.Category == FoodCategory.Healthy)
            {
                _healthyFoodCount++;
            }
            else
            {
                _ultraProcessedFoodCount++;
            }

            RefreshFoodDisplay();
        }

        public void UpdateFoodCounts(int healthyCount, int ultraProcessedCount)
        {
            _healthyFoodCount = Mathf.Max(0, healthyCount);
            _ultraProcessedFoodCount = Mathf.Max(0, ultraProcessedCount);
            RefreshFoodDisplay();
        }

        public void UpdateProgress(float progress01)
        {
            _progress = Mathf.Clamp01(progress01);
            if (_progressText != null)
            {
                _progressText.text = $"Progresso: {Mathf.RoundToInt(_progress * 100f)}%";
            }
        }

        private void OnDestroy()
        {
            Unbind();
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
            Vector2 position,
            Vector2 size,
            TextAnchor alignment = TextAnchor.UpperLeft
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
            text.alignment = alignment;
            ConfigureRect((RectTransform)text.transform, position, size);
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

        private void HandleFatigueStateChanged(FatigueState state)
        {
            RefreshFatigueState(state);
        }

        private void RefreshBand(WellbeingBandData band)
        {
            if (_bandText != null)
            {
                _bandText.text = $"Faixa: {band?.DisplayName ?? "Sem faixa"}";
            }

            if (band != null && _fillImage != null)
            {
                _fillImage.color = band.CharacterColor;
            }
        }

        private void RefreshEpisodeCount(int episodeCount)
        {
            if (_episodeText != null)
            {
                _episodeText.text = $"Episódios: {episodeCount}";
            }
        }

        private void RefreshFatigueState(FatigueState state)
        {
            if (_fatigueStateText == null)
            {
                return;
            }

            if (state == FatigueState.Fatigued)
            {
                _fatigueStateText.text = "Fadiga: EM FADIGA!";
                _fatigueStateText.color = new Color(1f, 0.28f, 0.28f);
                _fatigueStateText.fontStyle = FontStyle.Bold;
            }
            else
            {
                _fatigueStateText.text = "Fadiga: Normal";
                _fatigueStateText.color = new Color(0.82f, 0.9f, 1f);
                _fatigueStateText.fontStyle = FontStyle.Normal;
            }
        }

        private void RefreshValue(int currentValue)
        {
            if (_valueText != null)
            {
                _valueText.text = $"Bem-estar: {currentValue} / {WellbeingComponent.MaximumValue}";
            }

            if (_fillImage != null)
            {
                _fillImage.fillAmount = currentValue / (float)WellbeingComponent.MaximumValue;
            }
        }

        private void RefreshFoodDisplay()
        {
            if (_foodText != null)
            {
                _foodText.text =
                    $"Alimentos: {TotalFoodCount} ({_healthyFoodCount} 🥗 | {_ultraProcessedFoodCount} 🍔)";
            }
        }
    }
}
