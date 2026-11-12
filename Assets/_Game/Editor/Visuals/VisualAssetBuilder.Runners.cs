using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Zuri e Tom, os corredores humanos de membros em curva: importa a folha de peças
    /// provisórias gerada por <c>tools/art/build_runner_rig.py</c> e monta um prefab por
    /// personagem. As medidas abaixo repetem PARTS e RIGS daquele script. Mudou lá, mude
    /// aqui. Os clipes estão em <c>VisualAssetBuilder.RunnerClips.cs</c>.
    /// </summary>
    public static partial class VisualAssetBuilder
    {
        public const string RunnerSheetPath = Root + "Art/Characters/SP_RunnerRig.png";
        public const string RunnerPrefabFolder = Root + "Prefabs/Visual/";

        public const float RunnerArmWidth = 0.09f;
        public const float RunnerLegWidth = 0.11f;

        private const int RunnerCell = 256;

        private static readonly (string Name, Vector2 Pivot)[] RunnerParts =
        {
            ("head", RunnerPivot(128, 128)),
            ("face", RunnerPivot(128, 128)),
            ("face_blink", RunnerPivot(128, 128)),
            ("face_effort", RunnerPivot(128, 128)),
            ("face_sleepy", RunnerPivot(128, 128)),
            ("face_rush", RunnerPivot(128, 128)),
            ("hair_back", RunnerPivot(128, 128)),
            ("hair_front", RunnerPivot(128, 128)),
            ("band", RunnerPivot(128, 128)),
            ("tail", RunnerPivot(200, 128)),
            ("torso", RunnerPivot(128, 176)),
            ("hand", RunnerPivot(128, 128)),
            ("shoe", RunnerPivot(104, 118)),
        };

        /// <summary>Medidas de um corredor, em unidades do jogo, com a origem entre os pés.</summary>
        public readonly struct RunnerSpec
        {
            public RunnerSpec(
                string name,
                int player,
                Color32 skin,
                Color32 skinShade,
                float hip,
                float neck,
                float head,
                float shoulder,
                float arm,
                float leg,
                Vector2 footBack,
                Vector2 footFront,
                Vector2 handBack,
                Vector2 handFront
            )
            {
                Name = name;
                Player = player;
                Skin = skin;
                SkinShade = skinShade;
                Hip = hip;
                Neck = neck;
                Head = head;
                Shoulder = shoulder;
                Arm = arm;
                Leg = leg;
                FootBack = footBack;
                FootFront = footFront;
                HandBack = handBack;
                HandFront = handFront;
            }

            public string Name { get; }
            public int Player { get; }
            public Color32 Skin { get; }
            public Color32 SkinShade { get; }

            /// <summary>Altura do quadril.</summary>
            public float Hip { get; }

            /// <summary>Do quadril ao pescoço.</summary>
            public float Neck { get; }

            /// <summary>Do pescoço ao centro da cabeça (e raio da cabeça).</summary>
            public float Head { get; }

            /// <summary>Do quadril ao ombro.</summary>
            public float Shoulder { get; }
            public float Arm { get; }
            public float Leg { get; }
            public Vector2 FootBack { get; }
            public Vector2 FootFront { get; }
            public Vector2 HandBack { get; }
            public Vector2 HandFront { get; }

            /// <summary>Altura do topo do crânio (sem cabelo).</summary>
            public float BodyHeight => Hip + Neck + Head * 2f;

            public string PrefabName => "PF_VIS_Runner_" + Name;
        }

        public static readonly RunnerSpec[] Runners =
        {
            new(
                "Zuri",
                1,
                new Color32(0x97, 0x60, 0x3C, 255),
                new Color32(0x87, 0x59, 0x39, 255),
                hip: 0.60f,
                neck: 0.34f,
                head: 0.21f,
                shoulder: 0.31f,
                arm: 0.38f,
                leg: 0.45f,
                footBack: new Vector2(-0.10f, 0.18f),
                footFront: new Vector2(0.08f, 0.18f),
                handBack: new Vector2(-0.13f, 0.58f),
                handFront: new Vector2(0.11f, 0.56f)
            ),
            new(
                "Tom",
                2,
                new Color32(0xF0, 0xCB, 0xA2, 255),
                new Color32(0xD5, 0xB6, 0x91, 255),
                hip: 0.64f,
                neck: 0.36f,
                head: 0.21f,
                shoulder: 0.34f,
                arm: 0.40f,
                leg: 0.49f,
                footBack: new Vector2(-0.09f, 0.18f),
                footFront: new Vector2(0.09f, 0.18f),
                handBack: new Vector2(-0.12f, 0.60f),
                handFront: new Vector2(0.10f, 0.60f)
            ),
        };

        private static readonly Color RunnerInk = new Color32(0x1E, 0x2A, 0x22, 255);

        /// <summary>Pivô em px da célula (origem em cima, à esquerda) para pivô de sprite.</summary>
        private static Vector2 RunnerPivot(int x, int y) =>
            new(x / (float)RunnerCell, 1f - y / (float)RunnerCell);

        public static string RunnerPrefabPath(RunnerSpec spec) =>
            RunnerPrefabFolder + spec.PrefabName + ".prefab";

        /// <summary>Importa a folha de peças e monta os prefabs de Zuri e Tom.</summary>
        [MenuItem("Healthy Race/Visuals/Build runner rig (Zuri e Tom)")]
        public static void BuildRunners()
        {
            if (
                !Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
            )
                return;
            _catalog = AssetDatabase.LoadAssetAtPath<VisualCatalog>(CatalogPath);
            if (_catalog == null)
                throw new InvalidOperationException(
                    "Catálogo visual ausente: rode 'Build owned visual assets' antes."
                );
            _material = _catalog.SpriteMaterial;
            var sprites = ImportRunnerSheet();
            foreach (var spec in Runners)
            {
                BuildRunner(spec, sprites, BuildRunnerAnimations(spec));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "RUNNER_BUILD_COMPLETE: "
                    + Runners.Length
                    + " prefabs de corredor e "
                    + (Runners.Length * RunnerStates.Length)
                    + " clipes salvos."
            );
        }

        private static Dictionary<string, Sprite> ImportRunnerSheet()
        {
            var importer = AssetImporter.GetAtPath(RunnerSheetPath) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException(
                    "Folha dos corredores ausente: rode tools/art/build_runner_rig.py.",
                    RunnerSheetPath
                );
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = RunnerCell;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // 13 colunas de 256 px: passa do limite padrão de 2048
            importer.maxTextureSize = 4096;
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(r => r.name);
            var height = Runners.Length * RunnerCell;
            var rects = new List<SpriteRect>();
            for (var row = 0; row < Runners.Length; row++)
            for (var col = 0; col < RunnerParts.Length; col++)
            {
                var name = RunnerSpriteName(Runners[row], RunnerParts[col].Name);
                rects.Add(
                    new SpriteRect
                    {
                        name = name,
                        spriteID = previous.TryGetValue(name, out var old)
                            ? old.spriteID
                            : GUID.Generate(),
                        rect = new Rect(
                            col * RunnerCell,
                            height - (row + 1) * RunnerCell,
                            RunnerCell,
                            RunnerCell
                        ),
                        alignment = SpriteAlignment.Custom,
                        pivot = RunnerParts[col].Pivot,
                    }
                );
            }
            provider.SetSpriteRects(rects.ToArray());
            var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            return AssetDatabase
                .LoadAllAssetsAtPath(RunnerSheetPath)
                .OfType<Sprite>()
                .ToDictionary(s => s.name);
        }

        public static string RunnerSpriteName(RunnerSpec spec, string part) =>
            "Runner_" + spec.Name + "_" + part;

        private static HoseLimb Limb(
            Transform parent,
            string name,
            Transform anchor,
            Transform target,
            float length,
            float width,
            Color fill,
            float bend,
            int order
        )
        {
            var limb = Bone(parent, name, Vector2.zero).gameObject.AddComponent<HoseLimb>();
            limb.SetInk(RunnerInk, 0.012f, 0.026f);
            limb.SetMaterial(_material);
            // sem malha no prefab: ela é refeita em tempo de execução
            limb.Configure(anchor, target, length, width, fill, bend, rebuild: false);
            limb.SortingOrder = order;
            return limb;
        }

        private static GameObject BuildRunner(
            RunnerSpec spec,
            Dictionary<string, Sprite> sprites,
            RuntimeAnimatorController controller
        )
        {
            Sprite Art(string part) => sprites[RunnerSpriteName(spec, part)];

            var root = new GameObject(spec.PrefabName);
            var flip = Bone(root.transform, "Rig", new Vector2(0f, FeetY));
            var body = Bone(flip, "Body", Vector2.zero);

            var hipBack = Bone(body, "HipBack", new Vector2(-0.03f, spec.Hip));
            var hipFront = Bone(body, "HipFront", new Vector2(0.03f, spec.Hip));
            var torsoBone = Bone(body, "Torso", new Vector2(0f, spec.Hip));
            Part(torsoBone, "TorsoSprite", Art("torso"), Vector2.zero, 17);
            var shoulderBack = Bone(torsoBone, "ShoulderBack", new Vector2(-0.07f, spec.Shoulder));
            var shoulderFront = Bone(torsoBone, "ShoulderFront", new Vector2(0.07f, spec.Shoulder));

            // a cabeça gira em torno do pescoço, não do próprio centro
            var headBone = Bone(torsoBone, "Head", new Vector2(0f, spec.Neck));
            var headCenter = new Vector2(0f, spec.Head);
            Part(headBone, "HairBack", Art("hair_back"), headCenter, 19);
            Part(headBone, "HeadSprite", Art("head"), headCenter, 20);
            var face = Part(headBone, "Face", Art("face"), headCenter, 21);
            Part(headBone, "HairFront", Art("hair_front"), headCenter, 22);
            Part(headBone, "Band", Art("band"), headCenter, 23);
            // nó da faixa, atrás da cabeça: daqui saem as duas pontas do indicador
            var indicator = Bone(headBone, "Indicator", headCenter + new Vector2(-0.20f, -0.02f));
            var tailA = Part(indicator, "TailA", Art("tail"), Vector2.zero, 18);
            var tailB = Part(indicator, "TailB", Art("tail"), Vector2.zero, 18);

            var handBack = Part(body, "HandBack", Art("hand"), spec.HandBack, 11);
            var handFront = Part(body, "HandFront", Art("hand"), spec.HandFront, 25);
            var footBack = Part(body, "FootBack", Art("shoe"), spec.FootBack, 13);
            var footFront = Part(body, "FootFront", Art("shoe"), spec.FootFront, 16);

            // cotovelo para trás (dobra negativa), joelho para a frente (positiva)
            var armBack = Limb(
                body,
                "ArmBack",
                shoulderBack,
                handBack.transform,
                spec.Arm,
                RunnerArmWidth,
                spec.SkinShade,
                -1f,
                10
            );
            var legBack = Limb(
                body,
                "LegBack",
                hipBack,
                footBack.transform,
                spec.Leg,
                RunnerLegWidth,
                spec.SkinShade,
                1f,
                12
            );
            var legFront = Limb(
                body,
                "LegFront",
                hipFront,
                footFront.transform,
                spec.Leg,
                RunnerLegWidth,
                spec.Skin,
                1f,
                15
            );
            var armFront = Limb(
                body,
                "ArmFront",
                shoulderFront,
                handFront.transform,
                spec.Arm,
                RunnerArmWidth,
                spec.Skin,
                -1f,
                24
            );

            var rig = root.AddComponent<RunnerRig>();
            rig.Configure(
                flip,
                face,
                new[]
                {
                    Art("face"),
                    Art("face_blink"),
                    Art("face_effort"),
                    Art("face_sleepy"),
                    Art("face_rush"),
                },
                tailA.transform,
                tailB.transform,
                new[] { armBack, legBack, legFront, armFront },
                new[]
                {
                    handBack.transform,
                    footBack.transform,
                    footFront.transform,
                    handFront.transform,
                }
            );
            rig.SetWellbeing(1f);
            root.AddComponent<Animator>().runtimeAnimatorController = controller;
            return SavePrefab(root);
        }
    }
}
