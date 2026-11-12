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

            Render(Shots.Length, rows.Length, ColumnStep, RowStep, PixelsPerUnit, "Runners.png");
            Debug.Log("RUNNER_EVIDENCE_COMPLETE: " + (rows.Length * Shots.Length) + " poses.");
        }

        // estado, rosto e Bem-Estar com que cada animação é mostrada
        private static readonly (string State, RunnerFace Face, float Wellbeing)[] AnimationRows =
        {
            ("Idle", RunnerFace.Neutral, 1f),
            ("Move", RunnerFace.Neutral, 1f),
            ("Jump", RunnerFace.Effort, 1f),
            ("Fall", RunnerFace.Rush, 0.8f),
            ("Land", RunnerFace.Effort, 0.8f),
            ("Push", RunnerFace.Effort, 0.6f),
            ("Interact", RunnerFace.Neutral, 1f),
            ("Eat", RunnerFace.Neutral, 1f),
            ("Fatigue", RunnerFace.Sleepy, 0.1f),
            ("Recover", RunnerFace.Neutral, 0.4f),
            ("Victory", RunnerFace.Rush, 1f),
            ("Defeat", RunnerFace.Sleepy, 0f),
            (RunnerRig.RushState, RunnerFace.Rush, 0.8f),
            (RunnerRig.CrashState, RunnerFace.Sleepy, 0.4f),
        };

        private const int AnimationColumns = 8;
        private const float AnimationColumnStep = 1.35f;
        private const float AnimationRowStep = 1.95f;

        /// <summary>
        /// Prancha de quadros das animações de cada corredor: uma linha por estado, com o
        /// clipe amostrado em instantes igualmente espaçados. Um arquivo por personagem.
        /// </summary>
        [MenuItem("Healthy Race/Evidence/Capture runner animations")]
        public static void CaptureAnimations()
        {
            // Com mais de ~80 personagens na mesma imagem o renderizador 2D deixa de
            // desenhar parte das malhas dos membros: cada prancha sai em duas páginas.
            const int rowsPerPage = 7;
            foreach (var spec in VisualAssetBuilder.Runners)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    VisualAssetBuilder.RunnerPrefabPath(spec)
                );
                for (var page = 0; page * rowsPerPage < AnimationRows.Length; page++)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var rows = Mathf.Min(rowsPerPage, AnimationRows.Length - page * rowsPerPage);
                    // a segunda página fica longe da origem de propósito: confere que o
                    // desenho dos membros não depende da posição na cena
                    var origin = new Vector2(page * 37f, -page * 53f);
                    for (var row = 0; row < rows; row++)
                    {
                        var (state, face, wellbeing) = AnimationRows[page * rowsPerPage + row];
                        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                            VisualAssetBuilder.RunnerClipPath(spec, state)
                        );
                        for (var col = 0; col < AnimationColumns; col++)
                        {
                            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                            instance.transform.position = new Vector3(
                                origin.x + col * AnimationColumnStep,
                                origin.y - row * AnimationRowStep,
                                0f
                            );
                            var rig = instance.GetComponent<RunnerRig>();
                            rig.SetFace(face);
                            rig.SetWellbeing(wellbeing);
                            // clipe que repete: não amostra o último instante, igual ao primeiro
                            var span = clip.isLooping ? AnimationColumns : AnimationColumns - 1;
                            clip.SampleAnimation(instance, clip.length * col / span);
                            rig.RebuildLimbs();
                        }
                    }

                    Render(
                        AnimationColumns,
                        rows,
                        AnimationColumnStep,
                        AnimationRowStep,
                        150,
                        "Runner-" + spec.Name + "-Animations-" + (page + 1) + ".png",
                        origin
                    );
                }
            }

            Debug.Log("RUNNER_ANIMATION_EVIDENCE_COMPLETE: " + AnimationRows.Length + " estados.");
        }

        private static void Render(
            int columns,
            int rows,
            float columnStep,
            float rowStep,
            int pixelsPerUnit,
            string file,
            Vector2 origin = default
        )
        {
            var width = columns * columnStep;
            var height = rows * rowStep;
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0xE8, 0xF1, 0xE4, 255);
            camera.transform.position = new Vector3(
                origin.x + (columns - 1) * columnStep * 0.5f,
                origin.y - (rows - 1) * rowStep * 0.5f + 0.2f,
                -10f
            );

            var size = new Vector2Int(
                Mathf.RoundToInt(width * pixelsPerUnit),
                Mathf.RoundToInt(height * pixelsPerUnit)
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
            File.WriteAllBytes(OutputPath + "/" + file, texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            renderTarget.Release();
            Object.DestroyImmediate(renderTarget);
        }

        /// <summary>
        /// Uma passada de corrida montada à mão, para a prancha de poses: pé da frente
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
