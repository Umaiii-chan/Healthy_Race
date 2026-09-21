using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HealthyRace.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Editor.Build
{
    public static class Phase1Evidence
    {
        private const string ScenePath = "Assets/_Game/Scenes/Phase1_CorridaDoEquilibrio.unity";
        private const string OutputPath = "docs/evidence/level-redesign";

        private static readonly IReadOnlyDictionary<string, ViewSpec> Views = new Dictionary<
            string,
            ViewSpec
        >
        {
            {
                "Z1-Spawn",
                new(new Vector2(12f, -0.1f), new Vector2(5f, -2.3f), new Vector2(15f, -0.1f))
            },
            {
                "Z2-Garden",
                new(new Vector2(48f, 0.2f), new Vector2(38f, -2.25f), new Vector2(52f, 1.15f))
            },
            {
                "Z3-Routes-A",
                new(new Vector2(78f, 0.5f), new Vector2(76f, 3.55f), new Vector2(76f, -2.25f))
            },
            {
                "Z3-Routes-B",
                new(new Vector2(103f, 0.5f), new Vector2(101f, 1.35f), new Vector2(105f, -2.25f))
            },
            {
                "Z3-Routes-C",
                new(new Vector2(127f, 0.3f), new Vector2(126f, 3.85f), new Vector2(130f, -2.25f))
            },
            {
                "Z4-Rally",
                new(new Vector2(151f, -0.1f), new Vector2(147f, -0.75f), new Vector2(155f, -0.75f))
            },
            {
                "Z5-Puzzle",
                new(new Vector2(174f, 0.8f), new Vector2(168f, -0.75f), new Vector2(173f, 3.55f))
            },
            {
                "Z6-Finish",
                new(new Vector2(215f, 0.3f), new Vector2(210f, 0.05f), new Vector2(218f, -0.75f))
            },
        };

        [MenuItem("Healthy Race/Evidence/Capture Level 01")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var bootstrap = Object.FindFirstObjectByType<Phase1Bootstrap>();
            typeof(Phase1Bootstrap)
                .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(bootstrap, null);

            var camera = Object.FindFirstObjectByType<Camera>();
            var canvas = bootstrap.GetComponentInChildren<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 2f;
            canvas.sortingOrder = 1000;

            Directory.CreateDirectory(OutputPath);
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                foreach (var view in Views)
                    CaptureView(bootstrap, camera, canvas, view.Key, view.Value, size);
            }

            Debug.Log($"PHASE1_EVIDENCE_COMPLETE: {Views.Count * 2} PNG renders saved.");
        }

        private static void CaptureView(
            Phase1Bootstrap bootstrap,
            Camera camera,
            Canvas canvas,
            string name,
            ViewSpec view,
            Vector2Int size
        )
        {
            bootstrap.PlayerOne.transform.position = view.PlayerOne;
            bootstrap.PlayerTwo.transform.position = view.PlayerTwo;
            camera.transform.position = new Vector3(view.Center.x, view.Center.y, -10f);
            camera.orthographicSize = 6.2f;

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

        private readonly struct ViewSpec
        {
            public ViewSpec(Vector2 center, Vector2 playerOne, Vector2 playerTwo)
            {
                Center = center;
                PlayerOne = playerOne;
                PlayerTwo = playerTwo;
            }

            public Vector2 Center { get; }
            public Vector2 PlayerOne { get; }
            public Vector2 PlayerTwo { get; }
        }
    }
}
