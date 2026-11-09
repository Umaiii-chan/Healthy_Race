using System;
using HealthyRace.Audio;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    /// <summary>
    /// O áudio real só chega à build por referência serializada. Estes testes
    /// falham se um clipe sair do catálogo e o jogo passar a tocar o tom de reserva.
    /// </summary>
    public sealed class AudioCatalogTests
    {
        [Test]
        public void Catalog_LoadsFromResources()
        {
            var catalog = Resources.Load<AudioCatalog>(AudioCatalog.ResourcePath);
            Assert.That(catalog, Is.Not.Null, "SO_AudioCatalog precisa estar em Resources.");
        }

        [Test]
        public void Catalog_HasARealClipForEveryId()
        {
            var catalog = Resources.Load<AudioCatalog>(AudioCatalog.ResourcePath);
            Assert.That(catalog, Is.Not.Null);

            foreach (AudioClipId id in Enum.GetValues(typeof(AudioClipId)))
            {
                var clip = catalog.Find(id);
                Assert.That(clip, Is.Not.Null, $"Sem clipe para {id} no catálogo.");
                Assert.That(clip.length, Is.GreaterThan(0f), $"Clipe vazio para {id}.");
            }
        }

        [Test]
        public void Service_ResolvesCatalogClipsInsteadOfSyntheticFallback()
        {
            AudioService.ResetInstance();
            var host = new GameObject("AudioCatalogTests_Service");
            try
            {
                var service = host.AddComponent<AudioService>();
                var catalog = Resources.Load<AudioCatalog>(AudioCatalog.ResourcePath);

                foreach (AudioClipId id in Enum.GetValues(typeof(AudioClipId)))
                {
                    Assert.That(
                        service.GetClip(id),
                        Is.SameAs(catalog.Find(id)),
                        $"{id} deveria vir do catálogo."
                    );
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                AudioService.ResetInstance();
            }
        }
    }
}
