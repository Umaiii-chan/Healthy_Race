using System.IO;
using System.Reflection;
using HealthyRace.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Editor.Build
{
    /// <summary>
    /// Render real de uma fase em 1920×1080: a fase inteira (sem HUD) e cada quarto
    /// dela do jeito que o jogo mostra, com a faixa do HUD em cima. Independe do
    /// tamanho da janela do Editor.
    /// </summary>
    public static class LevelEvidence
    {
        private const string ScenePath = "Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity";
        private const string OutputPath = "docs/evidence/fase-01";
        private const int UiLayer = 5;
        private static readonly Vector2Int Size = new(1920, 1080);

        [MenuItem("Healthy Race/Evidence/Capture Level 01")]
        public static void Capture()
        {
            CaptureScene(ScenePath, OutputPath, "fase-inteira", quarters: true);
            Debug.Log("LEVEL_EVIDENCE_COMPLETE: 5 PNG renders saved.");
        }

        /// <summary>Uma imagem da fase inteira para cada uma das 10 fases.</summary>
        [MenuItem("Healthy Race/Evidence/Capture all levels")]
        public static void CaptureAll()
        {
            var levels = HealthyRace.Editor.Visuals.VisualAssetBuilder.LevelScenes;
            for (var i = 0; i < levels.Length; i++)
            {
                CaptureScene(
                    levels[i].Scene,
                    "docs/evidence/fases",
                    $"fase-{i + 1:00}",
                    quarters: false
                );
            }

            Debug.Log($"LEVEL_EVIDENCE_COMPLETE: {levels.Length} level overviews saved.");
        }

        private static void CaptureScene(
            string scenePath,
            string folder,
            string wholeName,
            bool quarters
        )
        {
            EditorSceneManager.OpenScene(scenePath);
            var bootstrap = Object.FindFirstObjectByType<LevelBootstrap>();
            // Awake muda a gravidade e o frame rate; no editor isso iria para o
            // ProjectSettings, então os dois voltam ao fim da captura.
            var previousGravity = Physics2D.gravity;
            var previousFrameRate = Application.targetFrameRate;
            try
            {
                typeof(LevelBootstrap)
                    .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(bootstrap, null);
                // fora do modo de jogo os membros em curva não se redesenham sozinhos
                foreach (
                    var rig in Object.FindObjectsByType<HealthyRace.Visuals.RunnerRig>(
                        FindObjectsSortMode.None
                    )
                )
                {
                    rig.RebuildLimbs();
                }

                CaptureViews(bootstrap, folder, wholeName, quarters);
            }
            finally
            {
                Physics2D.gravity = previousGravity;
                Application.targetFrameRate = previousFrameRate;
            }
        }

        private static void CaptureViews(
            LevelBootstrap bootstrap,
            string folder,
            string wholeName,
            bool quarters
        )
        {
            var world = bootstrap.CameraController.GetComponent<Camera>();
            var canvas = bootstrap.PlayerOneHud.GetComponentInParent<Canvas>();
            var ui = new GameObject("UiCamera").AddComponent<Camera>();
            ui.clearFlags = CameraClearFlags.Nothing;
            ui.cullingMask = 1 << UiLayer;
            ui.orthographic = true;
            ui.depth = world.depth + 1;
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = UiLayer;
            }

            world.cullingMask &= ~(1 << UiLayer);
            world.rect = new Rect(0f, 0f, 1f, 1f);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = ui;
            canvas.planeDistance = 1f;
            ui.clearFlags = CameraClearFlags.SolidColor;
            ui.backgroundColor = new Color(0f, 0f, 0f, 0f);

            Directory.CreateDirectory(folder);
            var grid = bootstrap.Grid;

            // fase inteira, sem HUD: 64×36 cabe exatamente em 16:9
            var whole = RenderWorld(
                world,
                Size,
                new Vector2(grid.Width, grid.Height) * 0.5f,
                // cabe na altura ou na largura (fases largas)
                Mathf.Max(grid.Height * 0.5f, grid.Width * 0.5f * Size.y / Size.x)
            );
            Save(whole, folder, wholeName);
            if (!quarters)
            {
                Object.DestroyImmediate(whole);
                Object.DestroyImmediate(ui.gameObject);
                return;
            }

            // quartos, como o jogo mostra: mundo abaixo da faixa do HUD, HUD por cima
            var band = Mathf.RoundToInt(LevelBootstrap.HudBandPixels);
            var worldSize = new Vector2Int(Size.x, Size.y - band);
            var half = LevelBootstrap.MinimumViewHeight * 0.5f;
            var aspect = worldSize.x / (float)worldSize.y;
            var hud = RenderUi(ui, canvas);
            var names = new[]
            {
                "baixo-esquerda",
                "baixo-direita",
                "cima-esquerda",
                "cima-direita",
            };
            for (var i = 0; i < 4; i++)
            {
                var x = i % 2 == 0 ? half * aspect : grid.Width - half * aspect;
                var y = i < 2 ? half : grid.Height - half;
                var view = RenderWorld(world, worldSize, new Vector2(x, y), half);
                Save(Compose(view, hud), folder, names[i]);
                Object.DestroyImmediate(view);
            }

            Object.DestroyImmediate(hud);
            Object.DestroyImmediate(whole);
            Object.DestroyImmediate(ui.gameObject);
        }

        private static Texture2D RenderWorld(
            Camera world,
            Vector2Int size,
            Vector2 center,
            float orthoSize
        )
        {
            world.transform.position = new Vector3(center.x, center.y, -10f);
            world.orthographicSize = orthoSize;
            return Grab(world, size);
        }

        private static Texture2D RenderUi(Camera ui, Canvas canvas)
        {
            canvas.scaleFactor = Size.x / 1920f;
            Canvas.ForceUpdateCanvases();
            return Grab(ui, Size);
        }

        private static Texture2D Grab(Camera camera, Vector2Int size)
        {
            var target = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            // sem isto, a primeira captura depois de uma variante nova de shader sai sem
            // os membros dos corredores (o Editor compila em segundo plano e pula o desenho)
            var asyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            camera.Render();
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(target);
            return texture;
        }

        /// <summary>Mundo embaixo (linhas 0..altura-1), HUD em cima com alfa.</summary>
        private static Texture2D Compose(Texture2D world, Texture2D hud)
        {
            var result = new Texture2D(Size.x, Size.y, TextureFormat.RGB24, false);
            var pixels = new Color[Size.x * Size.y];
            var worldPixels = world.GetPixels();
            var hudPixels = hud.GetPixels();
            for (var y = 0; y < Size.y; y++)
            for (var x = 0; x < Size.x; x++)
            {
                var i = y * Size.x + x;
                var under = y < world.height ? worldPixels[y * world.width + x] : Color.black;
                var over = hudPixels[i];
                pixels[i] = Color.Lerp(under, over, over.a);
            }

            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        private static void Save(Texture2D texture, string folder, string name)
        {
            File.WriteAllBytes($"{folder}/{name}.png", texture.EncodeToPNG());
        }
    }
}
