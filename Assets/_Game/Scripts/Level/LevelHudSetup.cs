using HealthyRace.UI;
using HealthyRace.WellBeing;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Level
{
    /// <summary>
    /// Monta o canvas do HUD de uma fase (faixa no alto) e o HUD de cada jogador.
    /// Separada de <see cref="LevelBootstrap"/> para o HUD poder ser redesenhado sem
    /// mexer na montagem da fase.
    /// </summary>
    public static class LevelHudSetup
    {
        public static GameObject CreateCanvas(Transform parent)
        {
            var canvasObject = new GameObject(
                "HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            var band = new GameObject("FaixaDoHud", typeof(RectTransform), typeof(Image));
            band.transform.SetParent(canvasObject.transform, false);
            var bandRect = (RectTransform)band.transform;
            bandRect.anchorMin = new Vector2(0f, 1f);
            bandRect.anchorMax = new Vector2(1f, 1f);
            bandRect.pivot = new Vector2(0.5f, 1f);
            bandRect.sizeDelta = new Vector2(0f, LevelBootstrap.HudBandPixels);
            // a faixa é o vão de cima da estufa: vidro claro, com uma viga de madeira
            // embaixo, de onde o resto da fase começa
            band.GetComponent<Image>().color = new Color32(0xD8, 0xEC, 0xE2, 255);
            band.GetComponent<Image>().raycastTarget = false;
            Beam(band.transform, 12f, new Color32(0x1E, 0x2A, 0x22, 255));
            Beam(band.transform, 8f, new Color32(0xE3, 0xA3, 0x5E, 255), 2f);
            return canvasObject;
        }

        private static void Beam(Transform band, float height, Color color, float lift = 0f)
        {
            var beam = new GameObject("Viga", typeof(RectTransform), typeof(Image));
            beam.transform.SetParent(band, false);
            var rect = (RectTransform)beam.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, lift);
            rect.sizeDelta = new Vector2(0f, height);
            var image = beam.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        /// <summary>HUD do jogador 1 no canto esquerdo, do jogador 2 no direito.</summary>
        public static PlayerHUD CreatePlayerHud(
            Transform canvas,
            int playerIndex,
            Color color,
            WellbeingComponent wellbeing,
            FatigueComponent fatigue
        )
        {
            var left = playerIndex == 1;
            var hud = new GameObject(
                $"P{playerIndex} HUD",
                typeof(RectTransform)
            ).AddComponent<PlayerHUD>();
            var corner = new Vector2(left ? 0f : 1f, 1f);
            hud.Initialize(
                canvas,
                $"P{playerIndex}",
                color,
                wellbeing,
                fatigue,
                corner,
                corner,
                new Vector2(left ? 12f : -12f, -4f)
            );
            return hud;
        }
    }
}
