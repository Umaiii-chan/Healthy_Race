using System;
using HealthyRace.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.UI
{
    /// <summary>
    /// Displays match-wide timing and final window status independently of individual player stats.
    /// </summary>
    public sealed class MatchHUD : MonoBehaviour
    {
        private const float Width = 280f;
        private const float Height = 64f;

        private Text _timerText;
        private Text _finalWindowText;
        private Image _panelImage;

        private float _currentTime;
        private bool _isFinalWindowActive;
        private float _finalWindowTimeRemaining;

        public float CurrentTime => _currentTime;

        public bool IsFinalWindowActive => _isFinalWindowActive;

        public float FinalWindowTimeRemaining => _finalWindowTimeRemaining;

        public string FormattedTime
        {
            get
            {
                var timeSpan = TimeSpan.FromSeconds(Mathf.Max(0f, _currentTime));
                return $"{((int)timeSpan.TotalMinutes):D2}:{timeSpan.Seconds:D2}";
            }
        }

        public void Initialize(Transform parent, Vector2 position)
        {
            transform.SetParent(parent, false);

            var rect = transform as RectTransform ?? gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(Width, Height);

            _panelImage = gameObject.AddComponent<Image>();
            _panelImage.sprite = PlaceholderSpriteFactory.Square;
            _panelImage.color = new Color(0.04f, 0.08f, 0.14f, 0.85f);

            _timerText = CreateText(
                "TimerText",
                transform,
                "Tempo: 00:00",
                20,
                FontStyle.Bold,
                Color.white,
                new Vector2(0f, -8f),
                new Vector2(Width - 20f, 26f),
                TextAnchor.MiddleCenter
            );

            _finalWindowText = CreateText(
                "FinalWindowText",
                transform,
                string.Empty,
                14,
                FontStyle.Bold,
                new Color(1f, 0.35f, 0.35f),
                new Vector2(0f, -36f),
                new Vector2(Width - 20f, 20f),
                TextAnchor.MiddleCenter
            );

            _finalWindowText.gameObject.SetActive(false);
        }

        public void UpdateTimer(float seconds)
        {
            _currentTime = Mathf.Max(0f, seconds);
            if (_timerText != null)
            {
                _timerText.text = $"Tempo: {FormattedTime}";
            }
        }

        /// <summary>Mostra uma mensagem de estado no lugar da janela final (ex.: aguardando parceiro).</summary>
        public void ShowStatusMessage(string message)
        {
            if (_finalWindowText == null)
            {
                return;
            }

            _finalWindowText.gameObject.SetActive(true);
            _finalWindowText.text = message;
            if (_panelImage != null)
            {
                _panelImage.color = new Color(0.04f, 0.14f, 0.12f, 0.92f);
            }
        }

        public void SetFinalWindow(bool active, float secondsRemaining = 20f)
        {
            _isFinalWindowActive = active;
            _finalWindowTimeRemaining = Mathf.Max(0f, secondsRemaining);

            if (_finalWindowText == null)
            {
                return;
            }

            if (active)
            {
                _finalWindowText.gameObject.SetActive(true);
                _finalWindowText.text =
                    $"JANELA FINAL: {Mathf.CeilToInt(_finalWindowTimeRemaining)}s";
                if (_panelImage != null)
                {
                    _panelImage.color = new Color(0.18f, 0.04f, 0.04f, 0.92f);
                }
            }
            else
            {
                _finalWindowText.gameObject.SetActive(false);
                if (_panelImage != null)
                {
                    _panelImage.color = new Color(0.04f, 0.08f, 0.14f, 0.85f);
                }
            }
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
            TextAnchor alignment
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

            var rect = (RectTransform)text.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return text;
        }
    }
}
