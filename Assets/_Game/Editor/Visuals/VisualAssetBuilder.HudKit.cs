using System.IO;
using System.Linq;
using HealthyRace.UI;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Kit aprovado em 11/11/2026: importa as peças geradas por
    /// <c>tools/art/build_kit.py</c> (HUD 5 e alimentos) e preenche o asset que o HUD das
    /// fases carrega de Resources.
    /// </summary>
    public static partial class VisualAssetBuilder
    {
        public const string KitFolder = Root + "Art/Kit/";
        public const string HudKitPath = Root + "Resources/" + HudKit.ResourcePath + ".asset";

        // as peças são desenhadas a 2 px por unidade de interface (1920x1080)
        private const float KitPixelsPerUnit = 200f;

        [MenuItem("Healthy Race/Visuals/Build HUD kit (placas, folhas, trepadeira)")]
        public static void BuildHudKit()
        {
            if (
                !Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
            )
                return;
            _catalog = LoadRequired<VisualCatalog>(CatalogPath);
            // a identidade do jogador 2 é o laranja do Tom (Art Bible, seção 7)
            _catalog.PlayerTwo = new Color32(0xF2, 0x99, 0x4A, 255);
            EditorUtility.SetDirty(_catalog);

            var kit = AssetDatabase.LoadAssetAtPath<HudKit>(HudKitPath);
            if (kit == null)
            {
                kit = ScriptableObject.CreateInstance<HudKit>();
                Directory.CreateDirectory(Path.GetDirectoryName(HudKitPath));
                AssetDatabase.CreateAsset(kit, HudKitPath);
            }

            kit.Sign = KitSprite("hud_sign");
            kit.LeafOn = KitSprite("hud_leaf_on");
            kit.LeafOff = KitSprite("hud_leaf_off");
            kit.Clock = KitSprite("hud_clock");
            kit.Vine = KitSprite("hud_vine");
            kit.Goal = KitSprite("hud_goal");
            kit.Disk = KitSprite("hud_disk");
            kit.RingInk = KitSprite("hud_ring_ink");
            kit.RingFill = KitSprite("hud_ring_fill");
            // etiqueta fatiada: 16 unidades de borda, desenhadas a 2 px por unidade
            kit.Tag = KitSprite("hud_tag", new Vector4(32f, 32f, 32f, 32f));
            kit.Pad = KitSprite("menu_pad");
            kit.StarOn = KitSprite("ui_star_on");
            kit.StarOff = KitSprite("ui_star_off");
            kit.Portraits = (Sprite[])_catalog.RunnerPortraits.Clone();
            EditorUtility.SetDirty(kit);

            BuildSceneryKit();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HUD_KIT_COMPLETE: peças do HUD importadas e SO_HudKit salvo.");
        }

        public const string SceneryKitPath =
            Root + "Resources/" + SceneryKit.ResourcePath + ".asset";

        // as peças de cenário são desenhadas a 256 px por unidade do jogo
        private const float SceneryPixelsPerUnit = 256f;

        private static void BuildSceneryKit()
        {
            var kit = AssetDatabase.LoadAssetAtPath<SceneryKit>(SceneryKitPath);
            if (kit == null)
            {
                kit = ScriptableObject.CreateInstance<SceneryKit>();
                AssetDatabase.CreateAsset(kit, SceneryKitPath);
            }

            Sprite Piece(string name, Vector4 border = default) =>
                KitSprite(name, border, SceneryPixelsPerUnit, repeat: true);

            kit.ForkRight = Piece("env_fork_right");
            kit.ForkLeft = Piece("env_fork_left");
            kit.LeverSign = Piece("env_lever_sign");
            kit.FastBadge = Piece("env_fast_badge");
            kit.Spikes = Piece("env_spikes");
            kit.Hazard = Piece("env_hazard");
            kit.Gate = Piece("env_gate");
            kit.GateCap = Piece("env_gate_cap");
            // elevador fatiado: a tábua de cima e as pontas não esticam
            kit.Lift = Piece("env_lift", new Vector4(32f, 14f, 32f, 34f));
            kit.LeverBase = Piece("env_lever_base");
            kit.Knob = Piece("env_knob");
            kit.HangingPot = Piece("env_hanging_pot");
            kit.Light = Piece("env_light");
            kit.Landmarks = Enumerable
                .Range(0, 5)
                .Select(index => Piece("env_landmark_" + index))
                .ToArray();
            EditorUtility.SetDirty(kit);
        }

        /// <summary>Importa uma peça do kit como sprite único e a devolve.</summary>
        private static Sprite KitSprite(
            string name,
            Vector4 border = default,
            float pixelsPerUnit = KitPixelsPerUnit,
            bool repeat = false
        )
        {
            var path = KitFolder + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException(
                    "Peça do kit ausente: rode tools/art/build_kit.py.",
                    path
                );
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            // retângulo cheio: é o que ladrilhar e fatiar pedem
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return LoadRequired<Sprite>(path);
        }
    }
}
