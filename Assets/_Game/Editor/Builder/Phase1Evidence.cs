using System.IO;
using System.Reflection;
using HealthyRace.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Editor.Build
{
    /// <summary>
    /// Render real da arena da Fase 1 (câmera fixa + HUD), independente do tamanho
    /// da janela do Editor. A arena cabe inteira na tela, então há uma vista só.
    /// </summary>
    public static class Phase1Evidence
    {
        private const string ScenePath = "Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity";
        private const string OutputPath = "docs/evidence/arena";

        [MenuItem("Healthy Race/Evidence/Capture Level 01")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            typeof(Phase1Bootstrap)
                .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(bootstrap, null);

            var camera = Object.FindFirstObjectByType<Camera>();
            camera.transform.position = Phase1Bootstrap.CameraPosition;
            camera.orthographicSize = Phase1Bootstrap.CameraOrthographicSize;

            // Só a HUD entra no render; as telas de pausa/resultados ficam escondidas.
            var canvas = bootstrap.PlayerOneHud.GetComponentInParent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 2f;
            canvas.sortingOrder = 1000;

            Directory.CreateDirectory(OutputPath);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                CaptureView(camera, canvas, "Arena", size);
            }

            Debug.Log("PHASE1_EVIDENCE_COMPLETE: 2 PNG renders saved.");
        }

        private static void CaptureView(Camera camera, Canvas canvas, string name, Vector2Int size)
        {
            var target = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = target;
            canvas.scaleFactor = size.x / 1920f;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            File.WriteAllBytes($"{OutputPath}/{name}-{size.x}.png", texture.EncodeToPNG());

            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }
}
