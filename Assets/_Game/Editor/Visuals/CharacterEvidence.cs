using System.IO;
using System.Linq;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Prancha de quadros das animações do personagem: uma linha por ação, com o
    /// clipe amostrado em instantes igualmente espaçados. É um render real do prefab.
    /// </summary>
    public static class CharacterEvidence
    {
        private const string OutputPath = "docs/evidence/character";
        private const int Columns = 8;
        private const float ColumnStep = 1.7f;
        private const float RowStep = 2.5f;

        private static readonly (VisualAction Action, int Wellbeing)[] Rows =
        {
            (VisualAction.Idle, 100),
            (VisualAction.Move, 100),
            (VisualAction.Jump, 100),
            (VisualAction.Fall, 70),
            (VisualAction.Land, 70),
            (VisualAction.Push, 45),
            (VisualAction.Fatigue, 15),
            (VisualAction.Victory, 100),
            (VisualAction.Defeat, 0),
        };

        [MenuItem("Healthy Race/Evidence/Capture character animations")]
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var catalog = AssetDatabase.LoadAssetAtPath<VisualCatalog>(
                VisualAssetBuilder.CatalogPath
            );
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                VisualAssetBuilder.Root + "Prefabs/Visual/PF_VIS_Player_P1.prefab"
            );
            var clips = catalog.PlayerAnimator.animationClips.ToDictionary(clip => clip.name);

            for (var row = 0; row < Rows.Length; row++)
            {
                var (action, wellbeing) = Rows[row];
                var clip = clips["AN_VIS_" + action];
                for (var col = 0; col < Columns; col++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.transform.position = new Vector3(col * ColumnStep, -row * RowStep, 0f);
                    var presentation = instance.GetComponent<PlayerPresentation>();
                    presentation.PresentWellbeing(wellbeing);
                    presentation.Rig.SetExpression(
                        action == VisualAction.Push || action == VisualAction.Jump
                            ? CharacterExpression.Effort
                            : CharacterExpression.Normal
                    );
                    // clipe que repete: não amostra o último instante, igual ao primeiro
                    var span = clip.isLooping ? Columns : Columns - 1;
                    clip.SampleAnimation(instance, clip.length * col / span);
                }
            }

            var width = Columns * ColumnStep;
            var height = Rows.Length * RowStep;
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(120, 190, 235, 255);
            camera.transform.position = new Vector3(
                (Columns - 1) * ColumnStep * 0.5f,
                -(Rows.Length - 1) * RowStep * 0.5f + 0.35f,
                -10f
            );

            var pixelsPerUnit = 100;
            var size = new Vector2Int(
                Mathf.RoundToInt(width * pixelsPerUnit),
                Mathf.RoundToInt(height * pixelsPerUnit)
            );
            var target = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            Directory.CreateDirectory(OutputPath);
            File.WriteAllBytes(OutputPath + "/Animations.png", texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
            Debug.Log("CHARACTER_EVIDENCE_COMPLETE: " + Rows.Length + " ações capturadas.");
        }
    }
}
