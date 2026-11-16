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
            band.GetComponent<Image>().color = new Color(0.08f, 0.13f, 0.1f);
            return canvasObject;
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
