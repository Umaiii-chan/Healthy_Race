using System.Collections.Generic;
using System.IO;
using System.Linq;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Retratos e poses de Zuri e Tom para a interface. Saem de um render dos próprios
    /// prefabs, com fundo transparente: quando as peças do rig mudarem (o desenho final do
    /// ilustrador), regerar aqui atualiza menu e HUD sem redesenhar nada à mão.
    /// </summary>
    public static partial class VisualAssetBuilder
    {
        public const string RunnerPortraitSheetPath =
            Root + "Art/Characters/SP_RunnerPortraits.png";

        /// <summary>Um retrato por faixa de Bem-Estar, na ordem de WellbeingAppearance.</summary>
        public const int RunnerPortraitLevels = 5;

        private const int RunnerPortraitCell = 512;

        // lado da janela do retrato (só a cabeça) e da pose (corpo inteiro), em unidades
        private const float RunnerPortraitWindow = 1.1f;
        private const float RunnerPoseWindow = 1.72f;

        // instante da corrida usado na pose do menu, em fração do ciclo, por personagem
        private static readonly float[] RunnerPoseMoment = { 0f, 0.5f };

        public static string RunnerPortraitName(RunnerSpec spec, int level) =>
            "Portrait_" + spec.Name + "_" + level;

        public static string RunnerPoseName(RunnerSpec spec) => "Pose_" + spec.Name + "_Run";

        /// <summary>
        /// Renderiza a folha de retratos e poses, importa os sprites e os guarda no catálogo.
        /// Precisa dos prefabs e dos clipes já salvos.
        /// </summary>
        [MenuItem("Healthy Race/Visuals/Build runner portraits (Zuri e Tom)")]
        public static void BuildRunnerPortraitSheet()
        {
            if (
                !Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
            )
                return;
            _catalog = LoadRequired<VisualCatalog>(CatalogPath);
            BuildRunnerPortraits();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("RUNNER_PORTRAITS_COMPLETE: retratos e poses de Zuri e Tom salvos.");
        }

        private static void BuildRunnerPortraits()
        {
            // Todos na mesma cena, numa grade, fotografada de uma vez (como nas pranchas
            // de evidência): cada célula da grade vira um sprite.
            var columns = RunnerPortraitLevels + 1;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (var row = 0; row < Runners.Length; row++)
            {
                var spec = Runners[row];
                var prefab = LoadRequired<GameObject>(RunnerPrefabPath(spec));
                var clip = LoadRequired<AnimationClip>(RunnerClipPath(spec, "Move"));
                var moment = RunnerPoseMoment[row % RunnerPoseMoment.Length];
                for (var col = 0; col < columns; col++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    var rig = instance.GetComponent<RunnerRig>();
                    Vector2 focus;
                    if (col < RunnerPortraitLevels)
                    {
                        // a célula tem o tamanho da pose; o retrato é ampliado para enchê-la
                        instance.transform.localScale =
                            Vector3.one * (RunnerPoseWindow / RunnerPortraitWindow);
                        PosePortrait(rig, col);
                        var head = rig.transform.Find("Rig/Body/Torso/Head/HeadSprite");
                        focus = head.TransformPoint(new Vector3(-0.11f, 0.05f));
                    }
                    else
                    {
                        rig.SetFace(RunnerFace.Neutral);
                        rig.SetWellbeing(1f);
                        clip.SampleAnimation(instance, clip.length * moment);
                        // a origem do rig fica entre os pés
                        focus =
                            (Vector2)rig.transform.Find("Rig").position + new Vector2(0f, 0.76f);
                    }

                    var cell = new Vector2(
                        (col + 0.5f) * RunnerPoseWindow,
                        -(row + 0.5f) * RunnerPoseWindow
                    );
                    instance.transform.position += (Vector3)(cell - focus);
                    rig.RebuildLimbs();
                }
            }

            var size = new Vector2Int(
                columns * RunnerPortraitCell,
                Runners.Length * RunnerPortraitCell
            );
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Runners.Length * RunnerPoseWindow * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(
                columns * RunnerPoseWindow * 0.5f,
                -Runners.Length * RunnerPoseWindow * 0.5f,
                -10f
            );

            var sheet = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            sheet.SetPixels32(
                CutOut(Photograph(camera, size, Color.black), Photograph(camera, size, Color.white))
            );
            sheet.Apply();
            File.WriteAllBytes(RunnerPortraitSheetPath, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            AssetDatabase.ImportAsset(RunnerPortraitSheetPath, ImportAssetOptions.ForceUpdate);

            var sprites = ImportRunnerPortraitSheet(columns);
            _catalog.RunnerPortraits = Runners
                .SelectMany(spec =>
                    Enumerable
                        .Range(0, RunnerPortraitLevels)
                        .Select(level => sprites[RunnerPortraitName(spec, level)])
                )
                .ToArray();
            _catalog.RunnerPoses = Runners.Select(spec => sprites[RunnerPoseName(spec)]).ToArray();
            EditorUtility.SetDirty(_catalog);
        }

        /// <summary>
        /// Retrato: só a cabeça, rosto neutro, e as pontas da faixa na altura da faixa de
        /// Bem-Estar. O corpo e a pele não mudam com o estado; quem conta é a faixa.
        /// </summary>
        private static void PosePortrait(RunnerRig rig, int level)
        {
            rig.SetFace(RunnerFace.Neutral);
            rig.SetWellbeing(1f - level / (float)(RunnerPortraitLevels - 1));
            var head = rig.transform.Find("Rig/Body/Torso/Head");
            foreach (var renderer in rig.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = renderer.transform.IsChildOf(head);
            }
        }

        /// <summary>
        /// Recorta o fundo. O alfa não vem do alvo de render (o pipeline 2D não garante
        /// preservá-lo): a cena é fotografada sobre preto e sobre branco, e a diferença
        /// entre as duas fotos dá a transparência de cada pixel.
        /// </summary>
        private static Color32[] CutOut(Color32[] onBlack, Color32[] onWhite)
        {
            var pixels = new Color32[onBlack.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                var black = onBlack[i];
                var white = onWhite[i];
                // sobre branco cada canal ganha (1 - alfa) em relação ao preto
                var gap = ((white.r - black.r) + (white.g - black.g) + (white.b - black.b)) / 3f;
                var alpha = Mathf.Clamp01(1f - gap / 255f);
                if (alpha < 0.004f)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                    continue;
                }

                pixels[i] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(black.r / alpha), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(black.g / alpha), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(black.b / alpha), 0, 255),
                    (byte)Mathf.RoundToInt(alpha * 255f)
                );
            }

            return pixels;
        }

        private static Color32[] Photograph(Camera camera, Vector2Int size, Color background)
        {
            camera.backgroundColor = background;
            var target = new RenderTexture(size.x, size.y, 24) { antiAliasing = 4 };
            camera.targetTexture = target;
            // Na primeira vez que uma variante de shader é usada o Editor compila em
            // segundo plano e pula o desenho: a foto sairia sem os membros.
            var asyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            camera.Render();
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            var pixels = texture.GetPixels32();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
            return pixels;
        }

        private static Dictionary<string, Sprite> ImportRunnerPortraitSheet(int columns)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(RunnerPortraitSheetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = RunnerPortraitCell;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // 6 colunas de 512 px: passa do limite padrão de 2048
            importer.maxTextureSize = 4096;
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(r => r.name);
            var height = Runners.Length * RunnerPortraitCell;
            var rects = new List<SpriteRect>();
            for (var row = 0; row < Runners.Length; row++)
            for (var col = 0; col < columns; col++)
            {
                var name =
                    col < RunnerPortraitLevels
                        ? RunnerPortraitName(Runners[row], col)
                        : RunnerPoseName(Runners[row]);
                rects.Add(
                    new SpriteRect
                    {
                        name = name,
                        spriteID = previous.TryGetValue(name, out var old)
                            ? old.spriteID
                            : GUID.Generate(),
                        rect = new Rect(
                            col * RunnerPortraitCell,
                            height - (row + 1) * RunnerPortraitCell,
                            RunnerPortraitCell,
                            RunnerPortraitCell
                        ),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    }
                );
            }

            provider.SetSpriteRects(rects.ToArray());
            var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            return AssetDatabase
                .LoadAllAssetsAtPath(RunnerPortraitSheetPath)
                .OfType<Sprite>()
                .ToDictionary(s => s.name);
        }
    }
}
