using System.IO;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Editor.Visuals
{
    public static class VisualEvidence
    {
        public static void RebuildAndCapture()
        {
            VisualAssetBuilder.Build();
            Capture();
        }

        // A real Unity camera/UI render, independent of desktop window size.
        public static void Capture()
        {
            EditorSceneManager.OpenScene(VisualAssetBuilder.ScenePath);
            var camera = Object.FindFirstObjectByType<Camera>();
            var screen = Object.FindFirstObjectByType<RaceScreenView>();
            VisualTestDriver.ApplyDemoData(screen);
            var canvas = screen.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 2;
            canvas.sortingOrder = 1000;
            Directory.CreateDirectory("Logs/VisualEvidence");
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) })
            {
                foreach (
                    var state in new[]
                    {
                        VisualScreen.Gameplay,
                        VisualScreen.MainMenu,
                        VisualScreen.Results,
                        VisualScreen.Pause,
                        VisualScreen.Controls,
                        VisualScreen.Audio,
                        VisualScreen.Boot,
                    }
                )
                {
                    var target = new RenderTexture(size.x, size.y, 24);
                    camera.targetTexture = target;
                    screen.Show(state);
                    Canvas.ForceUpdateCanvases();
                    // CanvasScaler normally updates on the editor loop, so explicitly apply the reference scale for this offscreen render.
                    canvas.scaleFactor = size.x / 1920f;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(
                        screen.GetComponent<RectTransform>()
                    );
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    var previous = RenderTexture.active;
                    RenderTexture.active = target;
                    var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                    texture.Apply();
                    var pixels = texture.GetPixels32();
                    using (
                        var stream = new BinaryWriter(
                            File.Create($"Logs/VisualEvidence/{state}-{size.x}.ppm")
                        )
                    )
                    {
                        stream.Write(
                            System.Text.Encoding.ASCII.GetBytes($"P6\n{size.x} {size.y}\n255\n")
                        );
                        for (var y = size.y - 1; y >= 0; y--)
                        for (var x = 0; x < size.x; x++)
                        {
                            var color = pixels[y * size.x + x];
                            stream.Write(color.r);
                            stream.Write(color.g);
                            stream.Write(color.b);
                        }
                    }
                    RenderTexture.active = previous;
                    camera.targetTexture = null;
                    Object.DestroyImmediate(texture);
                    target.Release();
                    Object.DestroyImmediate(target);
                }
            }
            Debug.Log("VISUAL_EVIDENCE_COMPLETE: 14 Unity renders saved.");
        }
    }
}
