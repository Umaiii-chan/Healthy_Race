using System.IO;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Prancha de Zuri e Tom: render real dos prefabs, parados, com os rostos, com a
    /// faixa de cabeça em vários níveis de Bem-Estar, numa passada de corrida e virados
    /// para a esquerda. Serve para conferir a montagem do rig de membros em curva.
    /// </summary>
    public static class RunnerEvidence
    {
        private const string OutputPath = "docs/evidence/character";
        private const float ColumnStep = 1.5f;
        private const float RowStep = 2.0f;
        private const int PixelsPerUnit = 200;

        private readonly struct Shot
        {
            public Shot(
                RunnerFace face,
                float wellbeing,
                float facing = 1f,
                bool stride = false,
                float lean = 0f
            )
            {
                Face = face;
                Wellbeing = wellbeing;
                Facing = facing;
                Stride = stride;
                Lean = lean;
            }

            public RunnerFace Face { get; }
            public float Wellbeing { get; }
            public float Facing { get; }
            public bool Stride { get; }
            public float Lean { get; }
        }

        private static readonly Shot[] Shots =
        {
            new(RunnerFace.Neutral, 1f),
            new(RunnerFace.Blink, 0.6f),
            new(RunnerFace.Effort, 0.3f),
            new(RunnerFace.Sleepy, 0f),
            new(RunnerFace.Rush, 1f, stride: true, lean: -14f),
            new(RunnerFace.Neutral, 1f, stride: true, lean: -8f),
            new(RunnerFace.Neutral, 1f, facing: -1f),
            new(RunnerFace.Effort, 0.5f, facing: -1f, stride: true, lean: -8f),
        };

        [MenuItem("Healthy Race/Evidence/Capture runners (Zuri e Tom)")]
        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rows = VisualAssetBuilder.Runners;
            for (var row = 0; row < rows.Length; row++)
            {
                var spec = rows[row];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    VisualAssetBuilder.RunnerPrefabPath(spec)
                );
                for (var col = 0; col < Shots.Length; col++)
                {
                    var shot = Shots[col];
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.transform.position = new Vector3(col * ColumnStep, -row * RowStep, 0f);
                    var rig = instance.GetComponent<RunnerRig>();
                    rig.SetFace(shot.Face);
                    rig.SetWellbeing(shot.Wellbeing);
                    if (shot.Stride)
                    {
                        Stride(rig, spec, shot.Lean);
                    }

                    rig.SetFacing(shot.Facing);
                    rig.RebuildLimbs();
                }
            }

            var width = Shots.Length * ColumnStep;
            var height = rows.Length * RowStep;
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0xE8, 0xF1, 0xE4, 255);
            camera.transform.position = new Vector3(
                (Shots.Length - 1) * ColumnStep * 0.5f,
                -(rows.Length - 1) * RowStep * 0.5f + 0.2f,
                -10f
            );

            var size = new Vector2Int(
                Mathf.RoundToInt(width * PixelsPerUnit),
                Mathf.RoundToInt(height * PixelsPerUnit)
            );
            var renderTarget = new RenderTexture(size.x, size.y, 24) { antiAliasing = 4 };
            camera.targetTexture = renderTarget;
            // Na primeira vez que uma variante de shader é usada o Editor compila em
            // segundo plano e pula o desenho: uma foto única sairia sem os membros.
            var asyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            camera.Render();
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
            var previous = RenderTexture.active;
            RenderTexture.active = renderTarget;
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            Directory.CreateDirectory(OutputPath);
            File.WriteAllBytes(OutputPath + "/Runners.png", texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            renderTarget.Release();
            Object.DestroyImmediate(renderTarget);
            Debug.Log("RUNNER_EVIDENCE_COMPLETE: " + (rows.Length * Shots.Length) + " poses.");
        }

        /// <summary>
        /// Uma passada de corrida montada à mão (ainda não há clipes): pé da frente
        /// adiantado, pé de trás recolhido, braços em oposição e tronco inclinado.
        /// Os alvos estão na ordem do rig: mão de trás, pé de trás, pé da frente, mão da frente.
        /// </summary>
        private static void Stride(RunnerRig rig, VisualAssetBuilder.RunnerSpec spec, float lean)
        {
            rig.Targets[0].localPosition = new Vector2(0.24f, spec.Hip + 0.16f);
            rig.Targets[1].localPosition = new Vector2(-0.30f, 0.36f);
            rig.Targets[1].localRotation = Quaternion.Euler(0f, 0f, -55f);
            rig.Targets[2].localPosition = new Vector2(0.28f, 0.24f);
            rig.Targets[2].localRotation = Quaternion.Euler(0f, 0f, 18f);
            rig.Targets[3].localPosition = new Vector2(-0.22f, spec.Hip + 0.02f);
            var torso = rig.transform.Find("Rig/Body/Torso");
            torso.localRotation = Quaternion.Euler(0f, 0f, lean);
        }
    }
}
