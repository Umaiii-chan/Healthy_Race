using System;
using HealthyRace.Core;
using HealthyRace.Level;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.UI
{
    /// <summary>
    /// UI overlay panel that displays final match rankings, race times, and wellbeing scores.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchResultsPanel : MonoBehaviour
    {
        private const float Width = 640f;
        private const float Height = 420f;

        private Text _winnerText;
        private Text _p1StatsText;
        private Text _p2StatsText;
        private GameObject _contentRoot;

        public bool IsVisible => _contentRoot != null && _contentRoot.activeSelf;

        public void Initialize(Transform parent)
        {
            transform.SetParent(parent, false);

            var rect =
                gameObject.GetComponent<RectTransform>()
                ?? gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(Width, Height);

            _contentRoot = new GameObject("ResultsContent", typeof(RectTransform));
            _contentRoot.transform.SetParent(transform, false);

            var contentRect = (RectTransform)_contentRoot.transform;
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;

            var bgImage = _contentRoot.AddComponent<Image>();
            bgImage.sprite = PlaceholderSpriteFactory.Square;
            bgImage.color = new Color(0.05f, 0.10f, 0.18f, 0.95f);

            CreateText(
                "Title",
                _contentRoot.transform,
                "RESULTADOS DA CORRIDA",
                26,
                FontStyle.Bold,
                Color.white,
                new Vector2(0f, 150f),
                new Vector2(500f, 40f)
            );

            _winnerText = CreateText(
                "WinnerText",
                _contentRoot.transform,
                string.Empty,
                22,
                FontStyle.Bold,
                new Color(1f, 0.85f, 0.25f),
                new Vector2(0f, 90f),
                new Vector2(500f, 36f)
            );

            _p1StatsText = CreateText(
                "P1Stats",
                _contentRoot.transform,
                string.Empty,
                18,
                FontStyle.Normal,
                new Color(0.4f, 0.8f, 1f),
                new Vector2(-140f, -10f),
                new Vector2(260f, 140f)
            );

            _p2StatsText = CreateText(
                "P2Stats",
                _contentRoot.transform,
                string.Empty,
                18,
                FontStyle.Normal,
                new Color(1f, 0.6f, 0.4f),
                new Vector2(140f, -10f),
                new Vector2(260f, 140f)
            );

            CreateText(
                "PromptText",
                _contentRoot.transform,
                "Corrida finalizada com sucesso!",
                15,
                FontStyle.Italic,
                new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0f, -160f),
                new Vector2(500f, 30f)
            );

            _contentRoot.SetActive(false);
        }

        public void Show(MatchResultData results)
        {
            if (_contentRoot == null)
            {
                Initialize(transform.parent != null ? transform.parent : transform);
            }

            _contentRoot.SetActive(true);

            if (_winnerText != null)
            {
                if (results.WinnerPlayerIndex == 1)
                {
                    _winnerText.text = "VENCEDOR: JOGADOR 1!";
                }
                else if (results.WinnerPlayerIndex == 2)
                {
                    _winnerText.text = "VENCEDOR: JOGADOR 2!";
                }
                else
                {
                    _winnerText.text = "EMPATE!";
                }
            }

            if (_p1StatsText != null)
            {
                _p1StatsText.text = FormatPlayerSummary(results.Player1);
            }

            if (_p2StatsText != null)
            {
                _p2StatsText.text = FormatPlayerSummary(results.Player2);
            }
        }

        public void Hide()
        {
            if (_contentRoot != null)
            {
                _contentRoot.SetActive(false);
            }
        }

        private static string FormatPlayerSummary(PlayerMatchResult player)
        {
            var placementStr = player.IsDnf ? "DNF (Tempo Expirado)" : $"{player.Placement}º LUGAR";
            var timeStr = player.IsDnf
                ? "--:--"
                : $"{TimeSpan.FromSeconds(Mathf.Max(0f, player.FinishTime)):mm\\:ss}";

            return $"JOGADOR {player.PlayerIndex}\n\n"
                + $"Posição: {placementStr}\n"
                + $"Tempo: {timeStr}\n"
                + $"Bem-Estar: {player.FinalWellbeing} WB";
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            Vector2 position,
            Vector2 size
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
            text.alignment = TextAnchor.MiddleCenter;

            var rect = (RectTransform)text.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return text;
        }
    }
}
