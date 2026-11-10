using System;
using System.IO;
using System.Linq;
using HealthyRace.Audio;
using UnityEditor;
using UnityEngine;

namespace HealthyRace.Editor.Build
{
    /// <summary>
    /// Gera o catálogo de áudio a partir da convenção de nomes dos arquivos:
    /// <c>Music_Level01</c> -> <c>Audio/Music/mus_level01.wav</c>,
    /// <c>Sfx_PickupHealthy</c> -> <c>Audio/SFX/sfx_pickup_healthy.wav</c>.
    /// </summary>
    public static class AudioCatalogBuilder
    {
        public const string CatalogPath = "Assets/_Game/Resources/SO_AudioCatalog.asset";
        private const string AudioRoot = "Assets/_Game/Audio/";

        [MenuItem("Healthy Race/Audio/Build audio catalog")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
            AssetDatabase.Refresh();

            var entries = Enum.GetValues(typeof(AudioClipId))
                .Cast<AudioClipId>()
                .Select(id => new AudioCatalog.Entry
                {
                    Id = id,
                    Clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PathFor(id)),
                })
                .ToArray();

            var missing = entries.Where(e => e.Clip == null).Select(e => PathFor(e.Id)).ToArray();
            if (missing.Length > 0)
            {
                throw new FileNotFoundException(
                    "Clipes de áudio ausentes: " + string.Join(", ", missing)
                );
            }

            // Só agora, com todos os clipes conferidos: uma geração que falha não
            // pode deixar para trás um catálogo vazio que a build carregaria.
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AudioCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"AUDIO_CATALOG_COMPLETE: {entries.Length} clipes ligados.");
        }

        public static string PathFor(AudioClipId id)
        {
            var name = id.ToString();
            var isMusic = name.StartsWith("Music_", StringComparison.Ordinal);
            var stem = name.Substring(name.IndexOf('_') + 1);
            var snake = string.Concat(
                stem.Select(
                    (c, i) =>
                        i > 0 && char.IsUpper(c) && !char.IsUpper(stem[i - 1])
                            ? "_" + char.ToLowerInvariant(c)
                            : char.ToLowerInvariant(c).ToString()
                )
            );
            return AudioRoot + (isMusic ? "Music/mus_" : "SFX/sfx_") + snake + ".wav";
        }
    }
}
