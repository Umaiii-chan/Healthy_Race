using System.IO;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Prancha do membro em curva (<see cref="HoseLimb"/>): o mesmo membro com o alvo em
    /// várias posições, perto e longe da âncora, dobrando para os dois lados, nas cores de
    /// Zuri e de Tom e em duas larguras. É um render real da malha, para conferir o traço
    /// antes de montar o personagem.
    /// </summary>
    public static class HoseLimbEvidence
    {
        private const string OutputPath = "docs/evidence/character";
        private const int Columns = 8;
        private const float Cell = 1.1f;
        private const int PixelsPerUnit = 256;

        private static readonly Color Ink = new Color32(0x1E, 0x2A, 0x22, 255);
        private static readonly Color ZuriSkin = new Color32(0x97, 0x60, 0x3C, 255);
        private static readonly Color TomSkin = new Color32(0xF0, 0xCB, 0xA2, 255);

        // alvo em relação à âncora (unidades do jogo) e lado da dobra
        private static readonly (Vector2 Offset, float Bend)[] Poses =
        {
            (new Vector2(0f, -0.48f), 1f),
            (new Vector2(0.12f, -0.36f), 1f),
            (new Vector2(0.12f, -0.36f), -1f),
            (new Vector2(0.3f, -0.2f), 1f),
            (new Vector2(0.38f, 0.1f), -1f),
            (new Vector2(0.1f, 0.3f), 1f),
            (new Vector2(-0.25f, -0.1f), -1f),
            (new Vector2(0.05f, -0.08f), 1f),
        };

        private static readonly (string Name, Color Skin, float Length, float Width)[] Rows =
        {
            ("Zuri braço", ZuriSkin, 0.5f, 0.09f),
            ("Zuri perna", ZuriSkin, 0.62f, 0.112f),
            ("Tom braço", TomSkin, 0.52f, 0.09f),
            ("Tom perna", TomSkin, 0.66f, 0.112f),
        };

        [MenuItem("Healthy Race/Evidence/Capture hose limb")]
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (var row = 0; row < Rows.Length; row++)
            {
                var (name, skin, length, width) = Rows[row];
                for (var col = 0; col < Columns; col++)
                {
                    var (offset, bend) = Poses[col];
                    var origin = new Vector2(col * Cell, -row * Cell);
                    var anchor = new GameObject(name + " âncora").transform;
                    anchor.position = origin + new Vector2(0f, 0.25f);
                    var target = new GameObject(name + " alvo").transform;
                    target.position = (Vector2)anchor.position + offset;
                    var limb = new GameObject(name).AddComponent<HoseLimb>();
                    limb.SetInk(Ink, 0.012f, 0.026f);
                    limb.Configure(anchor, target, length, width, skin, bend);
                }
            }

            var worldWidth = Columns * Cell;
            var worldHeight = Rows.Length * Cell;
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = worldHeight * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0xE8, 0xF1, 0xE4, 255);
            camera.transform.position = new Vector3(
                (Columns - 1) * Cell * 0.5f,
                -(Rows.Length - 1) * Cell * 0.5f + 0.05f,
                -10f
            );

            var size = new Vector2Int(
                Mathf.RoundToInt(worldWidth * PixelsPerUnit),
                Mathf.RoundToInt(worldHeight * PixelsPerUnit)
            );
            var renderTarget = new RenderTexture(size.x, size.y, 24) { antiAliasing = 4 };
            camera.targetTexture = renderTarget;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = renderTarget;
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            Directory.CreateDirectory(OutputPath);
            File.WriteAllBytes(OutputPath + "/HoseLimb.png", texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            renderTarget.Release();
            Object.DestroyImmediate(renderTarget);
            Debug.Log("HOSE_LIMB_EVIDENCE_COMPLETE: " + (Rows.Length * Columns) + " membros.");
        }
    }
}
