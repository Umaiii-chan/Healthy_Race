using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Arte de cenário da arena, gerada por <c>tools/art/build_environment.py</c>.
    /// As bordas de 9-slice repetem BORDERS daquele script. Mudou lá, mude aqui.
    /// </summary>
    public static partial class VisualAssetBuilder
    {
        private const string ArenaArtRoot = Root + "Art/Environment/Arena/";

        [MenuItem("Healthy Race/Visuals/Build arena scenery")]
        public static void BuildEnvironment()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<VisualCatalog>(CatalogPath);
            if (_catalog == null)
                throw new InvalidOperationException(
                    "Catálogo visual ausente: rode 'Build owned visual assets' antes."
                );
            _catalog.Arena = ImportArenaArt();
            ApplyArenaArt();
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SCENERY_BUILD_COMPLETE: arena art imported and linked to the catalog.");
        }

        /// <summary>
        /// Terreno e perigos da arena passam a vir da arte nova. Os demais itens de
        /// <see cref="VisualCatalog.Environment"/> continuam os do atlas original.
        /// </summary>
        private static void ApplyArenaArt()
        {
            var arena = _catalog.Arena;
            if (arena.Ground == null)
                return;
            _catalog.Environment[0] = arena.Ground;
            _catalog.Environment[1] = arena.Plank;
            _catalog.Environment[2] = arena.Stone;
            _catalog.Environment[11] = arena.Spikes;
            _catalog.Environment[12] = arena.Mud;
        }

        private static ArenaArt ImportArenaArt()
        {
            return new ArenaArt
            {
                Sky = ImportArenaSprite("SP_Sky", Vector4.zero),
                Clouds = ImportArenaRows("SP_Clouds", 3),
                HillsFar = ImportArenaSprite("SP_HillsFar", Vector4.zero),
                HillsNear = ImportArenaSprite("SP_HillsNear", Vector4.zero),
                Bushes = ImportArenaSprite("SP_Bushes", Vector4.zero),
                // bordas: esquerda, baixo, direita, cima
                Ground = ImportArenaSprite("SP_Ground", new Vector4(96, 44, 96, 96)),
                Plank = ImportArenaSprite("SP_Plank", new Vector4(88, 40, 88, 40)),
                Stone = ImportArenaSprite("SP_Stone", Vector4.zero),
                Spikes = ImportArenaSprite("SP_Spikes", Vector4.zero),
                Mud = ImportArenaSprite("SP_Mud", Vector4.zero),
                Sign = ImportArenaSprite("SP_Sign", new Vector4(44, 44, 44, 44)),
            };
        }

        private static TextureImporter ArenaImporter(string name, SpriteImportMode mode)
        {
            var path = ArenaArtRoot + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException(
                    "Arte de cenário ausente: rode tools/art/build_environment.py.",
                    path
                );
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.spritePixelsPerUnit = 256;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            // malha retangular: exigida para desenhar o sprite ladrilhado ou fatiado
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            return importer;
        }

        private static Sprite ImportArenaSprite(string name, Vector4 border)
        {
            var importer = ArenaImporter(name, SpriteImportMode.Single);
            importer.spriteBorder = border;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArenaArtRoot + name + ".png");
            if (sprite == null)
                throw new InvalidOperationException($"Sprite '{name}' não foi importado.");
            return sprite;
        }

        /// <summary>Folha com um sprite por linha (as nuvens).</summary>
        private static Sprite[] ImportArenaRows(string name, int rows)
        {
            var path = ArenaArtRoot + name + ".png";
            var importer = ArenaImporter(name, SpriteImportMode.Multiple);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(r => r.name);
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            var cell = height / rows;
            var rects = new List<SpriteRect>();
            for (var row = 0; row < rows; row++)
            {
                var spriteName = name + "_" + row;
                rects.Add(
                    new SpriteRect
                    {
                        name = spriteName,
                        spriteID = previous.TryGetValue(spriteName, out var old)
                            ? old.spriteID
                            : GUID.Generate(),
                        rect = new Rect(0, height - (row + 1) * cell, width, cell),
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
                .LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();
        }
    }
}
