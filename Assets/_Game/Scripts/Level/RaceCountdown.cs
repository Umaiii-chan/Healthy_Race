using System;
using System.Collections;
using HealthyRace.Audio;
using HealthyRace.Core;
using HealthyRace.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Level
{
    /// <summary>
    /// Contagem de largada "3, 2, 1, JÁ!": os jogadores ficam parados e o relógio da
    /// partida não anda até o "JÁ!", para os dois largarem juntos. Pausa junto com o
    /// jogo (usa o tempo escalado). Em modo batch (testes) não há contagem, a menos
    /// que <see cref="ForceEnabled"/> esteja ligado.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceCountdown : MonoBehaviour
    {
        public const string GoText = "JÁ!";

        /// <summary>Quanto tempo o "JÁ!" fica na tela depois da largada.</summary>
        public const float GoSeconds = 0.6f;

        private Text _label;

        public static bool ForceEnabled { get; set; }

        /// <summary>Segundos de contagem; 0 quando não há contagem.</summary>
        public static float Seconds =>
            Application.isBatchMode && !ForceEnabled
                ? 0f
                : Mathf.Max(0f, BalanceConfig.Current.CountdownSeconds);

        public bool IsCounting { get; private set; }

        /// <summary>Texto mostrado agora ("3", "2", "1", "JÁ!" ou vazio).</summary>
        public string CurrentText => _label != null && _label.enabled ? _label.text : string.Empty;

        public event Action Started;

        /// <summary>
        /// Trava os jogadores e o relógio e conta até a largada. Sem contagem
        /// (<see cref="Seconds"/> = 0), larga na hora.
        /// </summary>
        public void Begin(
            Transform canvas,
            MatchCompletionController match,
            params PlayerController[] players
        )
        {
            var seconds = Seconds;
            if (seconds <= 0f)
            {
                Started?.Invoke();
                return;
            }

            _label = CreateLabel(canvas);
            IsCounting = true;
            SetFrozen(match, players, true);
            StartCoroutine(Count(seconds, match, players));
        }

        private IEnumerator Count(
            float seconds,
            MatchCompletionController match,
            PlayerController[] players
        )
        {
            var remaining = Mathf.CeilToInt(seconds);
            var step = seconds / remaining;
            for (var n = remaining; n >= 1; n--)
            {
                _label.text = n.ToString();
                AudioService.Instance.PlaySfx(AudioClipId.Sfx_Button);
                yield return new WaitForSeconds(step);
            }

            _label.text = GoText;
            AudioService.Instance.PlaySfx(AudioClipId.Sfx_Checkpoint);
            SetFrozen(match, players, false);
            IsCounting = false;
            Started?.Invoke();

            yield return new WaitForSeconds(GoSeconds);
            _label.enabled = false;
        }

        private static void SetFrozen(
            MatchCompletionController match,
            PlayerController[] players,
            bool frozen
        )
        {
            if (match != null)
            {
                match.ClockPaused = frozen;
            }

            foreach (var player in players)
            {
                if (player != null)
                {
                    player.SetMovementEnabled(!frozen);
                }
            }
        }

        private static Text CreateLabel(Transform canvas)
        {
            var labelObject = new GameObject("Contagem", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(canvas, false);
            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 360f);
            var text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 260;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.97f, 0.95f, 0.86f);
            text.raycastTarget = false;
            var outline = labelObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.16f, 0.13f);
            outline.effectDistance = new Vector2(6f, -6f);
            return text;
        }
    }
}
