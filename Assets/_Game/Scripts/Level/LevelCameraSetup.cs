using HealthyRace.Core;
using HealthyRace.Visuals;
using UnityEngine;

namespace HealthyRace.Level
{
    /// <summary>
    /// Monta a câmera compartilhada de uma fase: ortográfica, abaixo da faixa do HUD,
    /// enquadrando os dois jogadores dentro do retângulo da fase. Separada de
    /// <see cref="LevelBootstrap"/> para a regra de câmera poder mudar sozinha.
    /// </summary>
    public static class LevelCameraSetup
    {
        public static SharedCameraController Create(
            Transform parent,
            LevelGrid grid,
            Vector2 spawnOne,
            Vector2 spawnTwo,
            Transform playerOne,
            Transform playerTwo,
            ParallaxLayer foliage
        )
        {
            var cameraObject = new GameObject(
                "Camera Compartilhada",
                typeof(Camera),
                typeof(AudioListener)
            );
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.19f, 0.15f);
            // a faixa de cima é do HUD: a câmera desenha só abaixo dela
            camera.rect = new Rect(0f, 0f, 1f, 1f - LevelBootstrap.HudBandPixels / 1080f);
            camera.orthographicSize = LevelBootstrap.MinimumViewHeight * 0.5f;
            var midpoint = (spawnOne + spawnTwo) * 0.5f;
            cameraObject.transform.position = new Vector3(midpoint.x, midpoint.y, -10f);

            var controller = cameraObject.AddComponent<SharedCameraController>();
            controller.Configure(playerOne, playerTwo);
            controller.SetPlayableBounds(
                new Bounds(
                    new Vector3(grid.Width * 0.5f, grid.Height * 0.5f, 0f),
                    new Vector3(grid.Width, grid.Height, 20f)
                ),
                true
            );
            // afasta até caber a fase inteira, na altura ou na largura (fases largas)
            var viewAspect = 1920f / (1080f - LevelBootstrap.HudBandPixels);
            controller.SetZoomLimits(
                LevelBootstrap.MinimumViewHeight * 0.5f,
                Mathf.Max(grid.Height * 0.5f, grid.Width * 0.5f / viewAspect)
            );
            controller.SetPadding(new Vector2(4f, 3f));
            if (foliage != null)
            {
                foliage.Configure(
                    cameraObject.transform,
                    LevelBootstrap.FoliageFollowFactor,
                    0f,
                    0f
                );
            }

            return controller;
        }
    }
}
