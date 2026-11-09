using System;
using UnityEngine;

namespace HealthyRace.Audio
{
    /// <summary>
    /// Liga cada <see cref="AudioClipId"/> ao seu clipe por referência serializada.
    /// Fica em <c>Resources</c> porque o <see cref="AudioService"/> é criado sob
    /// demanda, sem cena que possa lhe entregar a referência — e é isso que faz os
    /// arquivos de áudio entrarem na build.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_AudioCatalog", menuName = "Healthy Race/Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        public const string ResourcePath = "SO_AudioCatalog";

        [Serializable]
        public struct Entry
        {
            public AudioClipId Id;
            public AudioClip Clip;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        public int Count => _entries.Length;

        public AudioClip Find(AudioClipId id)
        {
            foreach (var entry in _entries)
            {
                if (entry.Id == id)
                {
                    return entry.Clip;
                }
            }

            return null;
        }

        public void SetEntries(Entry[] entries)
        {
            _entries = entries ?? Array.Empty<Entry>();
        }
    }
}
