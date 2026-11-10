using System;
using System.Collections.Generic;
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
        // Tabela independente do gerador: se alguém inserir ou reordenar um ID sem
        // regerar o catálogo, o som trocado aparece aqui.
        private static readonly Dictionary<AudioClipId, string> ExpectedClips = new()
        {
            { AudioClipId.Music_Menu, "mus_menu" },
            { AudioClipId.Music_Level01, "mus_level01" },
            { AudioClipId.Sfx_Jump, "sfx_jump" },
            { AudioClipId.Sfx_Step, "sfx_step" },
            { AudioClipId.Sfx_PickupHealthy, "sfx_pickup_healthy" },
            { AudioClipId.Sfx_PickupUltra, "sfx_pickup_ultra" },
            { AudioClipId.Sfx_WellbeingUp, "sfx_wellbeing_up" },
            { AudioClipId.Sfx_WellbeingDown, "sfx_wellbeing_down" },
            { AudioClipId.Sfx_Fatigue, "sfx_fatigue" },
            { AudioClipId.Sfx_Button, "sfx_button" },
            { AudioClipId.Sfx_Lever, "sfx_lever" },
            { AudioClipId.Sfx_Door, "sfx_door" },
            { AudioClipId.Sfx_PuzzleSolved, "sfx_puzzle_solved" },
            { AudioClipId.Sfx_Checkpoint, "sfx_checkpoint" },
            { AudioClipId.Sfx_Finish, "sfx_finish" },
            { AudioClipId.Sfx_Victory, "sfx_victory" },
            { AudioClipId.Sfx_Defeat, "sfx_defeat" },
            { AudioClipId.Sfx_Tie, "sfx_tie" },
        };

        [Test]
        public void Catalog_MapsEveryIdToItsOwnClip()
        {
            var catalog = Resources.Load<AudioCatalog>(AudioCatalog.ResourcePath);
            Assert.That(catalog, Is.Not.Null);

            foreach (AudioClipId id in Enum.GetValues(typeof(AudioClipId)))
            {
                Assert.That(ExpectedClips.ContainsKey(id), Is.True, $"{id} sem clipe esperado.");
                Assert.That(catalog.Find(id).name, Is.EqualTo(ExpectedClips[id]), id.ToString());
            }
        }

        [Test]
        public void ClipIds_KeepTheirSerializedValues()
        {
            // O catálogo grava o número do enum: estes valores não podem mudar.
            Assert.That((int)AudioClipId.Music_Menu, Is.EqualTo(0));
            Assert.That((int)AudioClipId.Sfx_Jump, Is.EqualTo(2));
            Assert.That((int)AudioClipId.Sfx_Button, Is.EqualTo(9));
            Assert.That((int)AudioClipId.Sfx_Tie, Is.EqualTo(17));
        }

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
