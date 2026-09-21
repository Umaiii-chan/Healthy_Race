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
        private const float Width = 660f;
        private const float Height = 460f;

        private Text _winnerText;
        private Text _p1StatsText;
        private Text _p2StatsText;
        private GameObject _contentRoot;
        private Button _replayButton;

        public bool IsVisible => _contentRoot != null && _contentRoot.activeSelf;
        public Button ReplayButton => _replayButton;

        public event Action ReplayRequested;

        public void Initialize(Transform parent)
        {
            transform.SetParent(parent, false);

            if (transform is RectTransform rect)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(Width, Height);
            }

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
                24,
                FontStyle.Bold,
                Color.white,
                new Vector2(0f, 190f),
                new Vector2(500f, 36f)
            );

            _winnerText = CreateText(
                "WinnerText",
                _contentRoot.transform,
                string.Empty,
                20,
                FontStyle.Bold,
                new Color(1f, 0.85f, 0.25f),
                new Vector2(0f, 150f),
                new Vector2(500f, 32f)
            );

            _p1StatsText = CreateText(
                "P1Stats",
                _contentRoot.transform,
                string.Empty,
                13,
                FontStyle.Normal,
                new Color(0.4f, 0.8f, 1f),
                new Vector2(-150f, 0f),
                new Vector2(280f, 240f),
                TextAnchor.UpperLeft
            );

            _p2StatsText = CreateText(
                "P2Stats",
                _contentRoot.transform,
                string.Empty,
                13,
                FontStyle.Normal,
                new Color(1f, 0.6f, 0.4f),
                new Vector2(150f, 0f),
                new Vector2(280f, 240f),
                TextAnchor.UpperLeft
            );

            _replayButton = CreateButton(
                "ReplayButton",
                _contentRoot.transform,
                "JOGAR NOVAMENTE",
                new Vector2(0f, -174f),
                new Vector2(230f, 44f)
            );
            _replayButton.onClick.AddListener(RequestReplay);

            CreateText(
                "PromptText",
                _contentRoot.transform,
                "Corrida finalizada com sucesso",
                14,
                FontStyle.Italic,
                new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0f, -216f),
                new Vector2(500f, 28f)
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

        public void RequestReplay()
        {
            ReplayRequested?.Invoke();
        }

        private static string FormatPlayerSummary(PlayerMatchResult player)
        {
            var placementStr = player.IsDnf ? "DNF" : $"{player.Placement}º LUGAR";
            var timeStr = player.IsDnf
                ? "Não concluiu"
                : $"{TimeSpan.FromSeconds(Mathf.Max(0f, player.FinishTime)):mm\\:ss}";

            var score = player.Score;

            return $"JOGADOR {player.PlayerIndex} ({placementStr})\n"
                + $"SCORE: {score.FinalScore}\n\n"
                + $"Tempo: {timeStr}\n"
                + $"Bem-Estar: {player.FinalWellbeing}/100\n\n"
                + $"Alimentos: +{score.HealthyPoints}\n"
                + $"Puzzles: +{score.PuzzlePoints}\n"
                + $"Bônus Tempo: +{score.TimeBonus}\n"
                + $"Bônus Chegada: +{score.ArrivalBonus}\n"
                + $"Bônus Bem-Estar: +{score.WellbeingBonus}\n"
                + (score.UltraPenalty > 0 ? $"Penalidade Ultra: -{score.UltraPenalty}\n" : "")
                + (score.FatiguePenalty > 0 ? $"Penalidade Fadiga: -{score.FatiguePenalty}\n" : "")
                + (score.DnfPenalty > 0 ? $"Penalidade DNF: -{score.DnfPenalty}\n" : "");
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
            TextAnchor alignment = TextAnchor.MiddleCenter
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
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size
        )
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            buttonObject.transform.SetParent(parent, false);

            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.sprite = PlaceholderSpriteFactory.Square;
            image.color = new Color(0.18f, 0.62f, 0.34f, 1f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            CreateText(
                "Label",
                buttonObject.transform,
                label,
                16,
                FontStyle.Bold,
                Color.white,
                Vector2.zero,
                size
            );
            return button;
        }
    }
}
