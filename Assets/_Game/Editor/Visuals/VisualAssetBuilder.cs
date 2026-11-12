using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HealthyRace.Level;
using HealthyRace.UI;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>Creates only assets owned by the visual track using Unity serialization APIs.</summary>
    public static partial class VisualAssetBuilder
    {
        public const string Root = "Assets/_Game/";
        public const string CatalogPath = Root + "Art/VisualAtlas/SO_VisualCatalog.asset";
        public const string ScenePath = Root + "Scenes/Test/VisualTest.unity";
        public const string MenuScenePath = Root + "Scenes/MainMenu.unity";
        public const string Phase1ScenePath = Root + "Scenes/Phase1_CorridaDoEquilibrio.unity";
        public const string ScreensPrefabPath = Root + "Prefabs/Visual/PF_VIS_Screens.prefab";
        public const string InputActionsPath = Root + "Input/HealthyRaceInputActions.inputactions";
        private static Material _material;
        private static VisualCatalog _catalog;
        private static Sprite _fill;
        private static Sprite _round;
        private static readonly Color Leaf = new Color32(140, 224, 78, 255);
        private static readonly Color Ink = new Color32(29, 47, 43, 255);
        private static readonly Color Cream = new Color32(248, 247, 224, 255);
        private static readonly List<Button> Buttons = new();
        private static readonly List<VisualCommand> Commands = new();
        private static readonly string[] SpriteNames =
        {
            "VitaHealthy",
            "VitaStable",
            "VitaTired",
            "VitaCritical",
            "Salo",
            "Tree",
            "Apple",
            "Banana",
            "Orange",
            "Grapes",
            "Strawberry",
            "Carrot",
            "Broccoli",
            "Tomato",
            "Water",
            "Nuts",
            "Burger",
            "Pizza",
            "Soda",
            "Fries",
            "Donut",
            "Hotdog",
            "Icecream",
            "Chocolate",
            "Ground",
            "Platform",
            "Wall",
            "Shrub",
            "Checkpoint",
            "Finish",
            "Box",
            "PressureButton",
            "Lever",
            "Door",
            "Spikes",
            "Mud",
        };

        [MenuItem("Healthy Race/Visuals/Build owned visual assets")]
        public static void Build()
        {
            if (
                !Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
            )
                return;
            foreach (
                var path in new[]
                {
                    "Animations/Visual",
                    "Prefabs/Visual",
                    "Scenes/Test",
                    "Art/VisualAtlas",
                    "Art/Environment/Visual",
                    "Art/UI/Visual",
                    "Art/VFX/Visual",
                }
            )
                Directory.CreateDirectory(Root + path);
            AssetDatabase.Refresh();
            _material = AssetDatabase.LoadAssetAtPath<Material>(
                Root + "Art/VisualAtlas/MAT_VisualUnlit.mat"
            );
            if (_material == null)
            {
                _material = new Material(
                    Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                );
                AssetDatabase.CreateAsset(_material, Root + "Art/VisualAtlas/MAT_VisualUnlit.mat");
            }
            ImportAtlas();
            var sprites = AssetDatabase
                .LoadAllAssetsAtPath(Root + "Art/VisualAtlas/SP_VisualAtlas.png")
                .OfType<Sprite>()
                .ToDictionary(s => s.name);
            _catalog = AssetDatabase.LoadAssetAtPath<VisualCatalog>(CatalogPath);
            if (_catalog == null)
            {
                _catalog = ScriptableObject.CreateInstance<VisualCatalog>();
                AssetDatabase.CreateAsset(_catalog, CatalogPath);
            }
            _catalog.Characters = SpriteNames.Take(5).Select(n => sprites[n]).ToArray();
            _catalog.Foods = SpriteNames.Skip(6).Take(18).Select(n => sprites[n]).ToArray();
            _catalog.Environment = new[]
            {
                "Ground",
                "Platform",
                "Wall",
                "Tree",
                "Shrub",
                "Checkpoint",
                "Finish",
                "Box",
                "PressureButton",
                "Lever",
                "Door",
                "Spikes",
                "Mud",
            }
                .Select(n => sprites[n])
                .ToArray();
            _catalog.Background = AssetDatabase.LoadAssetAtPath<Sprite>(
                Root + "Art/Environment/Visual/SP_Background.png"
            );
            // o terreno e os perigos da arena vêm da arte nova, quando ela existe
            ApplyArenaArt();
            _catalog.Particle = CreateShape("SP_Spark", true);
            _catalog.Heart = CreateShape("SP_Heart", true);
            _catalog.Droplet = CreateShape("SP_Droplet", true);
            _catalog.Ring = CreateShape("SP_Ring", true);
            _catalog.SpriteMaterial = _material;
            var badgeSprite = CreateShape("SP_Badge", false);
            _fill = CreateShape("SP_Fill", false);
            _catalog.PlayerAnimator = BuildAnimations();
            EditorUtility.SetDirty(_catalog);
            var p1 = BuildPlayer(1, badgeSprite);
            var p2 = BuildPlayer(2, badgeSprite);
            var foods = new List<GameObject>();
            for (var i = 0; i < 18; i++)
            {
                var food = new GameObject("PF_VIS_Food_" + SpriteNames[i + 6]);
                var art = SpriteChild(
                    food.transform,
                    "Artwork",
                    _catalog.Foods[i],
                    Vector2.zero,
                    Vector2.one,
                    12
                );
                food.AddComponent<FoodPresentation>().Configure(_catalog, art, i);
                foods.Add(SavePrefab(food));
            }
            var environments = new List<GameObject>();
            var names = new[]
            {
                "Ground",
                "Platform",
                "Wall",
                "Tree",
                "Shrub",
                "Checkpoint",
                "Finish",
                "Box",
                "PressureButton",
                "Lever",
                "Door",
                "Spikes",
                "Mud",
            };
            for (var i = 0; i < names.Length; i++)
            {
                var root = new GameObject("PF_VIS_" + names[i]);
                var art = SpriteChild(
                    root.transform,
                    "Artwork",
                    _catalog.Environment[i],
                    Vector2.zero,
                    Vector2.one,
                    i < 5 ? 0 : 10
                );
                if (i >= 7 && i <= 10)
                    root.AddComponent<PuzzlePresentation>()
                        .Configure(
                            art,
                            WorldText(
                                root.transform,
                                "State",
                                "○ INATIVO",
                                new Vector2(0, -0.85f),
                                0.28f,
                                Ink
                            ),
                            _catalog,
                            (PuzzleVisualKind)(i - 7)
                        );
                environments.Add(SavePrefab(root));
            }
            var falling = new GameObject("PF_VIS_FallingPlatform");
            var fallingArt = SpriteChild(
                falling.transform,
                "Artwork",
                _catalog.Environment[1],
                Vector2.zero,
                Vector2.one,
                10
            );
            falling
                .AddComponent<HazardPresentation>()
                .Configure(
                    fallingArt.transform,
                    WorldText(
                        falling.transform,
                        "Warning",
                        "!  INSTÁVEL",
                        new Vector2(0, 0.5f),
                        0.32f,
                        Ink
                    )
                );
            var fallingPrefab = SavePrefab(falling);
            var driven = new GameObject("PF_VIS_ActivatedPlatform");
            var drivenArt = SpriteChild(
                driven.transform,
                "Artwork",
                _catalog.Environment[1],
                Vector2.zero,
                Vector2.one,
                10
            );
            driven
                .AddComponent<PuzzlePresentation>()
                .Configure(
                    drivenArt,
                    WorldText(
                        driven.transform,
                        "State",
                        "○ INATIVO",
                        new Vector2(0, -0.65f),
                        0.28f,
                        Ink
                    ),
                    _catalog
                );
            var drivenPrefab = SavePrefab(driven);
            foreach (FeedbackKind kind in Enum.GetValues(typeof(FeedbackKind)))
            {
                var effect = new GameObject("PF_VIS_VFX_" + kind);
                effect.AddComponent<VisualEffectEmitter>().Configure(_catalog, kind);
                SavePrefab(effect);
            }
            var ui = BuildUi();
            BuildScene(p1, p2, foods, environments, fallingPrefab, drivenPrefab, ui);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("VISUAL_BUILD_COMPLETE: VisualTest, catalog, prefabs and 12 clips saved.");
        }

        /// <summary>
        /// Regera só o prefab de telas e liga o fluxo do jogo: cena MainMenu, campos
        /// de fluxo da Fase 1 e a lista de cenas da build. Não toca nos demais prefabs.
        /// </summary>
        [MenuItem("Healthy Race/Visuals/Build screens and game flow")]
        public static void BuildFlow()
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
            _fill = CreateShape("SP_Fill", false);
            var ui = BuildUi();

            var menuScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
            var menu = new GameObject("MainMenu").AddComponent<MainMenuBootstrap>();
            Assign(menu, "_screensPrefab", ui);
            EditorSceneManager.SaveScene(menuScene, MenuScenePath);

            var phaseScene = EditorSceneManager.OpenScene(Phase1ScenePath);
            var phase = Object.FindFirstObjectByType<LevelBootstrap>();
            Assign(phase, "_screensPrefab", ui);
            Assign(
                phase,
                "_inputActions",
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath)
            );
            EditorSceneManager.SaveScene(phaseScene);

            ApplyBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "FLOW_BUILD_COMPLETE: screens prefab, MainMenu scene and build scenes saved."
            );
        }

        private static void Assign(Object target, string field, Object value)
        {
            if (target == null || value == null)
                throw new InvalidOperationException($"Não foi possível atribuir '{field}'.");
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"Campo '{field}' não existe em {target}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>Retângulo arredondado 9-slice para botões, cartões e etiquetas.</summary>
        private static Sprite CreateRounded()
        {
            var path = Root + "Art/UI/Visual/SP_Round.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null && existing.texture != null)
                return existing;
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            const int size = 64;
            const float radius = 22f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SP_RoundTexture",
                filterMode = FilterMode.Bilinear,
            };
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Max(radius - (x + 0.5f), x + 0.5f - (size - radius), 0f);
                var dy = Mathf.Max(radius - (y + 0.5f), y + 0.5f - (size - radius), 0f);
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius - distance + 0.5f)));
            }
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                64,
                0,
                SpriteMeshType.FullRect,
                new Vector4(24, 24, 24, 24)
            );
            sprite.name = "SP_Round";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SetMainObject(sprite, path);
            EditorUtility.SetDirty(sprite);
            return sprite;
        }

        private static Image Rounded(Image image, float cornerScale = 1f)
        {
            image.sprite = _round;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale;
            return image;
        }

        private static Text Bold(Text text, Color? stroke = null, float strokeWidth = 0f)
        {
            text.fontStyle = FontStyle.Bold;
            if (stroke.HasValue)
            {
                // Três contornos encadeados (diagonal, horizontal, vertical): um só
                // deixa degraus visíveis nos cantos em fontes grandes.
                foreach (
                    var distance in new[]
                    {
                        new Vector2(strokeWidth * 0.7f, strokeWidth * 0.7f),
                        new Vector2(strokeWidth, 0f),
                        new Vector2(0f, strokeWidth),
                    }
                )
                {
                    var outline = text.gameObject.AddComponent<Outline>();
                    outline.effectColor = stroke.Value;
                    outline.effectDistance = distance;
                }
                var shadow = text.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(
                    stroke.Value.r,
                    stroke.Value.g,
                    stroke.Value.b,
                    0.55f
                );
                shadow.effectDistance = new Vector2(0, -strokeWidth * 2f);
            }
            return text;
        }

        private static Image Decor(
            Transform parent,
            string name,
            Sprite sprite,
            float x,
            float y,
            float size,
            float amplitude,
            float period,
            float phase,
            float tilt = 0f
        )
        {
            var image = Panel(Box(parent, name, x, y, size, size), Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.gameObject.AddComponent<UiFloat>().Configure(amplitude, period, phase, tilt);
            return image;
        }

        private static void Chip(
            Transform parent,
            string label,
            float x,
            float y,
            float width,
            float height,
            int size,
            Color fill,
            Color ink
        )
        {
            var chip = Box(parent, label + " chip", x, y, width, height);
            Rounded(Panel(chip, fill), 1.2f);
            Bold(Label(chip, label, 0, 0, width, height, size, ink, TextAnchor.MiddleCenter));
        }

        private static void Scenery(GameObject screen)
        {
            // Mesma arte do menu, escurecida: as telas internas ficam da mesma família.
            var image = screen.GetComponent<Image>();
            image.sprite = _catalog.Background;
            image.color = new Color(0.2f, 0.31f, 0.27f);
        }

        private static void BuildMenu(GameObject screen)
        {
            var menu = screen.transform;
            var background = screen.GetComponent<Image>();
            background.sprite = _catalog.Background;
            background.color = Color.white;

            // Alimentos saudáveis flutuando no céu, em volta do título.
            Decor(menu, "Food_Banana", _catalog.Foods[1], 930, 70, 150, 14, 3.1f, 0.4f, 5);
            Decor(menu, "Food_Apple", _catalog.Foods[0], 790, 250, 170, 16, 2.6f, 1.3f, -6);
            Decor(menu, "Food_Carrot", _catalog.Foods[5], 1040, 300, 130, 12, 2.9f, 2.2f, 7);
            Decor(menu, "Food_Broccoli", _catalog.Foods[6], 880, 470, 130, 13, 3.3f, 3.0f, -5);
            Decor(menu, "Food_Water", _catalog.Foods[8], 1085, 520, 105, 11, 2.7f, 4.1f, 4);
            Decor(menu, "Food_Strawberry", _catalog.Foods[4], 690, 590, 105, 12, 3.0f, 5.2f, -7);

            // Título.
            Bold(Label(menu, "HEALTHY", 108, 48, 1000, 190, 150, Cream), Ink, 3.5f);
            Bold(Label(menu, "RACE", 108, 196, 760, 250, 210, Leaf), Ink, 4f);
            Chip(menu, "CORRIDA DO EQUILÍBRIO", 118, 462, 560, 58, 28, Ink, Leaf);
            Bold(
                Label(
                    menu,
                    "Duas pessoas. Um percurso.\nCada escolha muda a corrida.",
                    122,
                    536,
                    640,
                    96,
                    32,
                    Ink
                )
            );

            // Zuri e Tom no gramado, os dois correndo para o mesmo lado.
            var runnerOne = Decor(
                menu,
                "Runner_P1",
                _catalog.RunnerPose(1),
                130,
                640,
                380,
                10,
                1.7f,
                0f
            );
            runnerOne.raycastTarget = false;
            var runnerTwo = Decor(
                menu,
                "Runner_P2",
                _catalog.RunnerPose(2),
                410,
                650,
                380,
                10,
                1.7f,
                1.6f
            );
            runnerTwo.raycastTarget = false;
            Chip(menu, "P1", 280, 1018, 100, 44, 26, _catalog.PlayerOne, Color.white);
            Chip(menu, "P2", 550, 1022, 100, 44, 26, _catalog.PlayerTwo, Color.white);

            // Os lanches ficam sem personagem ao lado: comida não muda o corpo de ninguém.
            Decor(menu, "Food_Burger", _catalog.Foods[10], 1100, 830, 110, 8, 3.4f, 0.9f, 6);
            Decor(menu, "Food_Donut", _catalog.Foods[14], 800, 910, 100, 8, 3.1f, 2.7f, -6);
            Decor(menu, "Food_Soda", _catalog.Foods[12], 1125, 950, 90, 7, 2.9f, 4.4f, 5);

            // Cartão de ações.
            var card = Box(menu, "MenuCard", 1250, 200, 560, 620);
            Rounded(Panel(card, new Color32(21, 40, 35, 225)));
            Bold(
                Label(
                    card,
                    "PRONTOS PARA CORRER?",
                    40,
                    28,
                    480,
                    40,
                    24,
                    Leaf,
                    TextAnchor.MiddleCenter
                )
            );
            Button(card, "JOGAR", 40, 90, 480, VisualCommand.Play, 112, 48, Leaf);
            Button(card, "Como jogar", 40, 230, 480, VisualCommand.Controls, 68, 28);
            Button(card, "Áudio", 40, 316, 480, VisualCommand.Audio, 68, 28);
            Button(card, "Sair", 40, 402, 480, VisualCommand.Quit, 68, 28);
            Label(
                card,
                "MULTIPLAYER LOCAL  •  2 JOGADORES",
                40,
                500,
                480,
                36,
                21,
                Cream,
                TextAnchor.MiddleCenter
            );
            Label(
                card,
                "Mouse ou setas + Enter",
                40,
                546,
                480,
                32,
                19,
                new Color(Cream.r, Cream.g, Cream.b, 0.7f),
                TextAnchor.MiddleCenter
            );
            Chip(menu, "ODS 3  •  SAÚDE E BEM-ESTAR", 1250, 850, 560, 56, 24, Cream, Ink);
        }

        private static void ImportAtlas()
        {
            var path = Root + "Art/VisualAtlas/SP_VisualAtlas.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 128;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(r => r.name);
            // The generated sheet uses taller character cells; these observed gutters avoid clipping.
            var xs = new[] { 0, 211, 420, 627, 837, 1045, 1254 };
            var ys = new[] { 0, 278, 475, 677, 883, 1054, 1254 };
            var rects = new List<SpriteRect>();
            for (var i = 0; i < SpriteNames.Length; i++)
            {
                var row = i / 6;
                var col = i % 6;
                rects.Add(
                    new SpriteRect
                    {
                        name = SpriteNames[i],
                        spriteID = previous.TryGetValue(SpriteNames[i], out var old)
                            ? old.spriteID
                            : GUID.Generate(),
                        rect = new Rect(
                            xs[col],
                            1254 - ys[row + 1],
                            xs[col + 1] - xs[col],
                            ys[row + 1] - ys[row]
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
        }

        private static Sprite CreateShape(string name, bool star)
        {
            var path = Root + "Art/" + (star ? "VFX" : "UI") + "/Visual/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null && existing.texture != null)
                return existing;
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            // Small code-native UI/VFX primitives, serialized once as owned Unity assets.
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = name + "Texture",
                filterMode = FilterMode.Bilinear,
            };
            for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
            {
                var p = new Vector2((x - 31.5f) / 32, (y - 31.5f) / 32);
                var distance = star ? Mathf.Abs(p.x) + Mathf.Abs(p.y) : p.magnitude;
                if (name == "SP_Ring")
                    distance = Mathf.Abs(p.magnitude - 0.62f) * 5;
                if (name == "SP_Droplet")
                    distance =
                        p.y > 0
                            ? Mathf.Abs(p.x) * 1.8f + p.y * 0.95f
                            : new Vector2(p.x * 1.8f, p.y).magnitude;
                if (name == "SP_Heart")
                {
                    var upper =
                        Mathf.Min(
                            Vector2.Distance(p, new Vector2(-0.3f, 0.25f)),
                            Vector2.Distance(p, new Vector2(0.3f, 0.25f))
                        ) * 1.7f;
                    distance = p.y > 0 ? upper : Mathf.Abs(p.x) + Mathf.Abs(p.y);
                }
                texture.SetPixel(
                    x,
                    y,
                    new Color(
                        1,
                        1,
                        1,
                        name == "SP_Fill" ? 1 : Mathf.Clamp01((0.88f - distance) * 32)
                    )
                );
            }
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 64, 64),
                new Vector2(0.5f, 0.5f),
                64
            );
            sprite.name = name;
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SetMainObject(sprite, path);
            EditorUtility.SetDirty(sprite);
            return sprite;
        }

        private static GameObject SavePrefab(GameObject root)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(
                root,
                Root + "Prefabs/Visual/" + root.name + ".prefab"
            );
            Object.DestroyImmediate(root);
            return saved;
        }

        private static SpriteRenderer SpriteChild(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            Vector2 scale,
            int order
        )
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = _material;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static TextMesh WorldText(
            Transform parent,
            string name,
            string content,
            Vector2 position,
            float size,
            Color color
        )
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var text = go.AddComponent<TextMesh>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.characterSize = size * 0.25f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            go.GetComponent<MeshRenderer>().sortingOrder = 30;
            return text;
        }

        private static RectTransform Rect(
            Transform parent,
            string name,
            Vector2 min,
            Vector2 max,
            Vector2 offsetMin,
            Vector2 offsetMax
        )
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static RectTransform Box(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height
        )
        {
            return Rect(
                parent,
                name,
                new Vector2(0, 1),
                new Vector2(0, 1),
                new Vector2(x, -y - height),
                new Vector2(x + width, -y)
            );
        }

        private static Image Panel(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text Label(
            Transform parent,
            string content,
            float x,
            float y,
            float width,
            float height,
            int size,
            Color color,
            TextAnchor align = TextAnchor.MiddleLeft
        )
        {
            var text = Box(parent, content.Split('\n')[0], x, y, width, height)
                .gameObject.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button Button(
            Transform parent,
            string label,
            float x,
            float y,
            float width,
            VisualCommand command,
            float height = 64,
            int fontSize = 27,
            Color? fill = null
        )
        {
            var rect = Box(parent, label, x, y, width, height);
            // Pivô no centro: o destaque cresce o botão sem deslocá-lo.
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x + width * 0.5f, -y - height * 0.5f);
            var image = Rounded(Panel(rect, fill ?? Cream), 1.5f);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = fill.HasValue
                ? new Color(0.86f, 1f, 0.72f)
                : new Color(0.79f, 1f, 0.55f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.62f, 0.84f, 0.4f);
            button.colors = colors;
            rect.gameObject.AddComponent<UiHoverScale>();
            var text = Label(
                rect,
                label,
                16,
                0,
                width - 32,
                height,
                fontSize,
                Ink,
                TextAnchor.MiddleCenter
            );
            if (fill.HasValue)
                text.fontStyle = FontStyle.Bold;
            Buttons.Add(button);
            Commands.Add(command);
            return button;
        }

        private static Image Bar(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color color
        )
        {
            var track = Box(parent, name, x, y, width, height);
            Panel(track, new Color(0.08f, 0.14f, 0.12f));
            var fill = Panel(
                Rect(
                    track,
                    "Fill",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(3, 3),
                    new Vector2(-3, -3)
                ),
                color
            );
            fill.sprite = _fill;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1;
            return fill;
        }

        private static PlayerHudView Hud(Transform parent, int player)
        {
            var min = player == 1 ? new Vector2(0, 1) : new Vector2(1, 1);
            var offset = player == 1 ? new Vector2(32, -222) : new Vector2(-612, -222);
            var panel = Rect(
                parent,
                "HUD_P" + player,
                min,
                min,
                offset,
                offset + new Vector2(580, 190)
            );
            Panel(panel, new Color32(29, 47, 43, 245));
            var portrait = Panel(Box(panel, "Portrait", 12, 22, 84, 116), Color.white);
            portrait.preserveAspect = true;
            var identity = Label(
                panel,
                "P" + player,
                110,
                6,
                130,
                40,
                28,
                _catalog.IdentityColor(player)
            );
            var score = Label(panel, "0 pts", 280, 6, 275, 40, 30, Cream, TextAnchor.MiddleRight);
            var wellbeing = Label(panel, "VITA saudável 100/100", 110, 48, 445, 34, 25, Cream);
            var bar = Bar(panel, "Wellbeing", 110, 88, 445, 25, Color.green);
            var counts = Label(panel, "", 20, 126, 540, 30, 22, Cream);
            Label(panel, "PROGRESSO", 20, 160, 130, 20, 16, Cream);
            var progress = Bar(
                panel,
                "Progress",
                154,
                164,
                400,
                12,
                _catalog.IdentityColor(player)
            );
            var view = panel.gameObject.AddComponent<PlayerHudView>();
            view.Configure(_catalog, identity, portrait, bar, wellbeing, score, counts, progress);
            view.Render(new PlayerHudData { Player = player, Wellbeing = 100 });
            return view;
        }

        private static GameObject BuildUi()
        {
            Buttons.Clear();
            Commands.Clear();
            _round = CreateRounded();
            var root = new GameObject(
                "PF_VIS_Screens",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var screens = new GameObject[7];
            for (var i = 0; i < screens.Length; i++)
            {
                screens[i] = Rect(
                    root.transform,
                    ((VisualScreen)i).ToString(),
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero
                ).gameObject;
                if (i != (int)VisualScreen.Gameplay)
                    Panel(
                        screens[i].GetComponent<RectTransform>(),
                        new Color32(
                            21,
                            40,
                            35,
                            i == (int)VisualScreen.Pause ? (byte)245 : (byte)255
                        )
                    );
            }
            var boot = screens[0].transform;
            Label(boot, "HEALTHY RACE", 160, 360, 1600, 130, 92, Cream, TextAnchor.MiddleCenter);
            Label(
                boot,
                "Preparando a corrida…",
                160,
                520,
                1600,
                60,
                32,
                Cream,
                TextAnchor.MiddleCenter
            );
            BuildMenu(screens[1]);
            Scenery(screens[0]);
            Scenery(screens[4]);
            Scenery(screens[5]);
            Scenery(screens[6]);
            var gameplay = screens[2].transform;
            var hud1 = Hud(gameplay, 1);
            var hud2 = Hud(gameplay, 2);
            var timer = Rect(
                gameplay,
                "SharedRace",
                new Vector2(0.5f, 1),
                new Vector2(0.5f, 1),
                new Vector2(-235, -195),
                new Vector2(235, -32)
            );
            Panel(timer, Ink);
            Label(
                timer,
                "CORRIDA DO EQUILÍBRIO",
                10,
                6,
                450,
                28,
                20,
                Cream,
                TextAnchor.MiddleCenter
            );
            var clock = Label(timer, "00:00", 10, 36, 450, 66, 48, Cream, TextAnchor.MiddleCenter);
            var progress = Bar(timer, "LevelProgress", 24, 108, 422, 12, new Color(0.8f, 1f, 0.5f));
            var window = Label(
                timer,
                "JANELA FINAL 20s",
                10,
                128,
                450,
                28,
                21,
                new Color(1f, 0.76f, 0.35f),
                TextAnchor.MiddleCenter
            );
            var pause = screens[3].transform;
            Label(pause, "PAUSA", 700, 120, 520, 100, 68, Cream, TextAnchor.MiddleCenter);
            var pauseLabels = new[]
            {
                "Continuar",
                "Reiniciar",
                "Controles",
                "Áudio",
                "Sair para menu",
            };
            var pauseCommands = new[]
            {
                VisualCommand.Continue,
                VisualCommand.Restart,
                VisualCommand.Controls,
                VisualCommand.Audio,
                VisualCommand.Menu,
            };
            for (var i = 0; i < 5; i++)
                Button(pause, pauseLabels[i], 700, 270 + i * 88, 520, pauseCommands[i]);
            var result = screens[4].transform;
            var winner = Label(
                result,
                "VENCEDOR",
                250,
                34,
                1420,
                82,
                54,
                Cream,
                TextAnchor.MiddleCenter
            );
            var reason = Label(
                result,
                "As escolhas contam até a chegada.",
                250,
                113,
                1420,
                44,
                27,
                Cream,
                TextAnchor.MiddleCenter
            );
            var resultTexts = new Text[2];
            for (var i = 0; i < 2; i++)
            {
                var card = Box(result, "Result_P" + (i + 1), 310 + i * 690, 180, 610, 740);
                Panel(card, new Color32(41, 65, 55, 255));
                Label(card, "P" + (i + 1), 28, 12, 554, 50, 36, _catalog.IdentityColor(i + 1));
                resultTexts[i] = Label(card, "", 28, 72, 554, 655, 25, Cream, TextAnchor.UpperLeft);
            }
            // Linha com layout automático: sem "Próxima fase" os outros dois recentram.
            var actions = Box(result, "Actions", 210, 946, 1500, 84);
            var row = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 36;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            Button(actions, "Próxima fase", 0, 0, 430, VisualCommand.Next, 72, 30, Leaf);
            Button(actions, "Jogar novamente", 0, 0, 430, VisualCommand.Restart, 72, 30);
            Button(actions, "Menu", 0, 0, 430, VisualCommand.Menu, 72, 30);
            var controls = screens[5].transform;
            Label(controls, "COMO JOGAR", 250, 90, 1420, 100, 64, Cream, TextAnchor.MiddleCenter);
            Label(
                controls,
                "Alimentos saudáveis aumentam o Bem-Estar; ultraprocessados reduzem.\n"
                    + "Bem-Estar baixo deixa você mais lento e pode causar fadiga.\n"
                    + "A fase termina quando os dois chegam às suas portas.",
                260,
                200,
                1400,
                130,
                28,
                Cream,
                TextAnchor.MiddleCenter
            );
            Label(
                controls,
                "P1  ●\n\nMover       A / D\nPular       W ou Espaço",
                360,
                380,
                550,
                330,
                36,
                _catalog.PlayerOne
            );
            Label(
                controls,
                "P2  ◆\n\nMover       ← / →\nPular       ↑",
                1050,
                380,
                550,
                330,
                36,
                _catalog.PlayerTwo
            );
            Label(controls, "Pausa: Esc", 300, 740, 1320, 65, 28, Cream, TextAnchor.MiddleCenter);
            Button(controls, "Voltar", 720, 860, 480, VisualCommand.Back);
            var audio = screens[6].transform;
            Label(
                audio,
                "ÁUDIO E ACESSIBILIDADE",
                250,
                130,
                1420,
                100,
                58,
                Cream,
                TextAnchor.MiddleCenter
            );
            var music = Slider(audio, "Música", 600, 360);
            var effects = Slider(audio, "Efeitos", 600, 490);
            var toggleRoot = Box(audio, "ReduceFlashes", 600, 650, 720, 64);
            var toggle = toggleRoot.gameObject.AddComponent<Toggle>();
            var toggleBg = Panel(Box(toggleRoot, "Box", 0, 4, 56, 56), Cream);
            toggleBg.raycastTarget = true;
            var check = Panel(
                Box(toggleBg.transform, "Check", 10, 10, 36, 36),
                new Color(0.28f, 0.65f, 0.22f)
            );
            toggle.targetGraphic = toggleBg;
            toggle.graphic = check;
            Label(toggleRoot, "Reduzir flashes e tremores", 80, 0, 620, 64, 31, Cream);
            Button(audio, "Voltar", 720, 840, 480, VisualCommand.Back);
            var view = root.AddComponent<RaceScreenView>();
            view.Configure(
                screens,
                clock,
                window,
                progress,
                winner,
                resultTexts,
                new[] { hud1, hud2 },
                Buttons.ToArray(),
                Commands.ToArray(),
                music,
                effects,
                toggle,
                reason
            );
            view.Show(VisualScreen.Gameplay);
            view.RenderRace(0, 0, null);
            return SavePrefab(root);
        }

        private static Slider Slider(Transform parent, string name, float x, float y)
        {
            Label(parent, name, x, y - 52, 720, 45, 32, Cream);
            var rect = Box(parent, name + "Slider", x, y, 720, 40);
            var slider = rect.gameObject.AddComponent<Slider>();
            Panel(Box(rect, "Track", 0, 12, 720, 16), new Color(0.1f, 0.2f, 0.15f));
            var area = Rect(
                rect,
                "HandleArea",
                Vector2.zero,
                Vector2.one,
                new Vector2(20, 0),
                new Vector2(-20, 0)
            );
            var handle = Rect(
                area,
                "Handle",
                Vector2.zero,
                new Vector2(0, 1),
                new Vector2(-20, 0),
                new Vector2(20, 0)
            );
            var image = Panel(handle, Cream);
            image.raycastTarget = true;
            slider.handleRect = handle;
            slider.targetGraphic = image;
            slider.value = 0.7f;
            return slider;
        }

        private static GameObject Place(GameObject prefab, Vector2 position, float scale = 1)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * scale;
            return instance;
        }

        private static void BuildScene(
            GameObject p1,
            GameObject p2,
            List<GameObject> foods,
            List<GameObject> env,
            GameObject falling,
            GameObject driven,
            GameObject ui
        )
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Visual Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 7.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(213, 234, 208, 255);
            var backgroundPath = Root + "Art/Environment/Visual/SP_Background.png";
            if (File.Exists(backgroundPath))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(backgroundPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                var bg = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath);
                var renderer = SpriteChild(null, "Background", bg, Vector2.zero, Vector2.one, -30);
                renderer.transform.localScale = new Vector3(
                    27.1f / bg.bounds.size.x,
                    15.3f / bg.bounds.size.y,
                    1
                );
            }
            var gallery = new GameObject("Visual gallery");
            WorldText(
                gallery.transform,
                "Title",
                "HEALTHY RACE  /  ESTÚDIO VISUAL",
                new Vector2(0, 3.25f),
                0.27f,
                Ink
            );
            var players = new List<PlayerPresentation>();
            var values = new[] { 100, 75, 50, 25, 0 };
            for (var i = 0; i < 5; i++)
            {
                var x = -10.4f + i * 4.15f;
                var a = Place(p1, new Vector2(x, 1.85f), 0.75f).GetComponent<PlayerPresentation>();
                a.PresentWellbeing(values[i]);
                players.Add(a);
                var b = Place(p2, new Vector2(x + 1.6f, 1.85f), 0.75f)
                    .GetComponent<PlayerPresentation>();
                b.PresentWellbeing(values[i]);
                players.Add(b);
                WorldText(
                    gallery.transform,
                    "Band",
                    VisualCatalog.StateNames[i] + "  " + values[i],
                    new Vector2(x + 0.8f, 0.55f),
                    0.17f,
                    Ink
                );
            }
            var foodViews = new List<FoodPresentation>();
            for (var i = 0; i < 18; i++)
            {
                var pos = new Vector2(-11.7f + (i % 9) * 2.9f, -0.65f - (i / 9) * 1.65f);
                foodViews.Add(Place(foods[i], pos, 0.61f).GetComponent<FoodPresentation>());
                WorldText(
                    gallery.transform,
                    "Food label",
                    VisualCatalog.FoodNames[i],
                    pos + new Vector2(0, -0.65f),
                    0.16f,
                    Ink
                );
            }
            var puzzleViews = new List<PuzzlePresentation>();
            for (var i = 0; i < 6; i++)
            {
                var x = -11.2f + i * 4.45f;
                Place(env[0], new Vector2(x, -5.9f), 1.4f);
                WorldText(
                    gallery.transform,
                    "Zone",
                    new[]
                    {
                        "Z1  SPAWN",
                        "Z2  CAIXA",
                        "Z3  ROTAS",
                        "Z4  REENCONTRO",
                        "Z5  PUZZLE",
                        "Z6  CHEGADA",
                    }[i],
                    new Vector2(x, -6.8f),
                    0.18f,
                    Ink
                );
            }
            Place(env[3], new Vector2(-11.2f, -4.55f), 0.9f);
            puzzleViews.Add(
                Place(env[7], new Vector2(-6.75f, -4.65f), 0.8f).GetComponent<PuzzlePresentation>()
            );
            Place(env[11], new Vector2(-3, -4.8f), 0.75f);
            Place(env[12], new Vector2(-1.3f, -4.75f), 0.65f);
            Place(env[5], new Vector2(2.1f, -4.6f), 0.8f);
            Place(env[4], new Vector2(0.7f, -5.1f), 0.55f);
            puzzleViews.Add(
                Place(env[8], new Vector2(4.7f, -4.5f), 0.6f).GetComponent<PuzzlePresentation>()
            );
            puzzleViews.Add(
                Place(env[9], new Vector2(6.5f, -4.5f), 0.65f).GetComponent<PuzzlePresentation>()
            );
            puzzleViews.Add(
                Place(env[10], new Vector2(8.3f, -4.5f), 0.7f).GetComponent<PuzzlePresentation>()
            );
            Place(env[6], new Vector2(11, -4.5f), 0.85f);
            var hazard = Place(falling, new Vector2(-0.3f, -3.55f), 0.55f)
                .GetComponent<HazardPresentation>();
            puzzleViews.Add(
                Place(driven, new Vector2(5.5f, -3.4f), 0.5f).GetComponent<PuzzlePresentation>()
            );
            Place(env[2], new Vector2(-8.8f, -5), 0.5f);
            var screen = Place(ui, Vector2.zero).GetComponent<RaceScreenView>();
            var eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule)
            );
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var preview = new GameObject(
                "Visual preview controls"
            ).AddComponent<VisualTestDriver>();
            preview.Configure(
                _catalog,
                screen,
                players.ToArray(),
                foodViews.ToArray(),
                puzzleViews.ToArray(),
                hazard
            );
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
