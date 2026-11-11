using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HealthyRace.Collectibles;
using HealthyRace.Level;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Arte das fases em labirinto (estufa e lanchonete), gerada por
    /// <c>tools/art/build_greenhouse.py</c>, e as cenas montadas a partir dos mapas
    /// em texto de <c>Assets/_Game/Levels</c>. A borda de 9-slice do elevador repete
    /// a do script. Mudou lá, mude aqui.
    /// </summary>
    public static partial class VisualAssetBuilder
    {
        private const string GreenhouseArtRoot = Root + "Art/Environment/Greenhouse/";
        public const string LevelsRoot = Root + "Levels/";
        public const string FoodDataRoot = Root + "ScriptableObjects/Food/";
        public const string CollectiblePrefabPath =
            Root + "Prefabs/Collectibles/PF_FoodCollectible.prefab";
        public const string PlayerOneVisualPath = Root + "Prefabs/Visual/PF_VIS_Player_P1.prefab";
        public const string PlayerTwoVisualPath = Root + "Prefabs/Visual/PF_VIS_Player_P2.prefab";

        /// <summary>Mapa de cada fase e a cena que ele gera.</summary>
        public static readonly (string Map, string Scene)[] LevelScenes = BuildLevelList();

        /// <summary>Fase 1 tem a cena histórica; as outras, Scenes/FaseNN.unity.</summary>
        private static (string Map, string Scene)[] BuildLevelList()
        {
            var list = new (string Map, string Scene)[10];
            list[0] = (LevelsRoot + "Fase01.txt", Phase1ScenePath);
            for (var n = 2; n <= 10; n++)
            {
                list[n - 1] = ($"{LevelsRoot}Fase{n:00}.txt", $"{Root}Scenes/Fase{n:00}.unity");
            }

            return list;
        }

        /// <summary>Menu, as 10 fases em ordem e a cena de teste que um teste PlayMode carrega.</summary>
        public static void ApplyBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
            };
            foreach (var (_, scene) in LevelScenes)
            {
                scenes.Add(new EditorBuildSettingsScene(scene, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Healthy Race/Visuals/Build greenhouse levels")]
        public static void BuildGreenhouse()
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
            _catalog.Greenhouse = ImportGreenhouseArt();
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();

            foreach (var (map, scene) in LevelScenes)
            {
                BuildLevelScene(map, scene);
            }

            ApplyBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"GREENHOUSE_BUILD_COMPLETE: art imported and {LevelScenes.Length} level scene(s) saved."
            );
        }

        private static GreenhouseArt ImportGreenhouseArt()
        {
            return new GreenhouseArt
            {
                Wall = ImportGreenhouseSprite("SP_GH_Wall", 32, Vector4.zero),
                Leaves = ImportGreenhouseSprite("SP_GH_Leaves", 128, Vector4.zero),
                Brick = ImportGreenhouseSprite("SP_GH_Brick", 256, Vector4.zero),
                BrickTop = ImportGreenhouseSprite("SP_GH_BrickTop", 256, Vector4.zero),
                Ramp = ImportGreenhouseSprite("SP_GH_Ramp", 256, Vector4.zero),
                FastFloor = ImportGreenhouseSprite("SP_GH_FastFloor", 256, Vector4.zero),
                FastSign = ImportGreenhouseSprite("SP_GH_FastSign", 256, Vector4.zero),
                Pot = ImportGreenhouseSprite("SP_GH_Pot", 256, Vector4.zero),
                Vine = ImportGreenhouseSprite("SP_GH_Vine", 256, Vector4.zero),
                // bordas: esquerda, baixo, direita, cima
                Lift = ImportGreenhouseSprite("SP_GH_Lift", 256, new Vector4(32, 16, 32, 16)),
                Gate = ImportGreenhouseSprite("SP_GH_Gate", 256, Vector4.zero),
            };
        }

        private static Sprite ImportGreenhouseSprite(
            string name,
            float pixelsPerUnit,
            Vector4 border
        )
        {
            var path = GreenhouseArtRoot + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException(
                    "Arte da estufa ausente: rode tools/art/build_greenhouse.py.",
                    path
                );
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.spriteBorder = border;
            // malha retangular: exigida para desenhar o sprite ladrilhado ou fatiado
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"Sprite '{name}' não foi importado.");
            return sprite;
        }

        /// <summary>
        /// Cena nova com um único objeto: o <see cref="LevelBootstrap"/>, que monta a
        /// fase inteira em tempo de execução a partir do mapa.
        /// </summary>
        private static void BuildLevelScene(string mapPath, string scenePath)
        {
            var map = AssetDatabase.LoadAssetAtPath<TextAsset>(mapPath);
            if (map == null)
                throw new FileNotFoundException("Mapa de fase ausente.", mapPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Fase").AddComponent<LevelBootstrap>();
            Assign(bootstrap, "_map", map);
            Assign(bootstrap, "_playerPrefab", LoadRequired<GameObject>(PlayerPrefabPath));
            Assign(
                bootstrap,
                "_collectiblePrefab",
                LoadRequired<GameObject>(CollectiblePrefabPath)
            );
            Assign(bootstrap, "_visualCatalog", _catalog);
            Assign(
                bootstrap,
                "_playerOneVisualPrefab",
                LoadRequired<GameObject>(PlayerOneVisualPath)
            );
            Assign(
                bootstrap,
                "_playerTwoVisualPrefab",
                LoadRequired<GameObject>(PlayerTwoVisualPath)
            );
            Assign(bootstrap, "_screensPrefab", LoadRequired<GameObject>(ScreensPrefabPath));
            Assign(bootstrap, "_inputActions", LoadRequired<InputActionAsset>(InputActionsPath));

            var foods = AssetDatabase
                .FindAssets("t:FoodData", new[] { FoodDataRoot.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<FoodData>)
                .Where(f => f != null)
                .ToArray();
            var serialized = new SerializedObject(bootstrap);
            var list = serialized.FindProperty("_foodCatalog");
            list.arraySize = foods.Length;
            for (var i = 0; i < foods.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = foods[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static T LoadRequired<T>(string path)
            where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException(
                    $"Asset obrigatório ausente ({typeof(T).Name}).",
                    path
                );
            return asset;
        }
    }
}
