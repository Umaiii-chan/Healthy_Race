using System.Collections;
using System.Collections.Generic;
using HealthyRace.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.PlayMode
{
    public sealed class AudioPlayModeTests
    {
        private AudioService _audioService;

        [SetUp]
        public void SetUp()
        {
            _audioService = AudioService.Instance;
            _audioService.InitializeSources();
        }

        [TearDown]
        public void TearDown()
        {
            if (_audioService != null)
            {
                _audioService.StopMusic();
            }
        }

        [UnityTest]
        public IEnumerator PlayMusic_AssignsClipToMusicSource()
        {
            _audioService.PlayMusic(AudioClipId.Music_Level01);
            yield return null;

            Assert.That(_audioService.MusicSource, Is.Not.Null);
            Assert.That(_audioService.CurrentMusicClip, Is.Not.Null);
            Assert.That(_audioService.MusicSource.clip, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator StopMusic_ClearsClipFromMusicSource()
        {
            _audioService.PlayMusic(AudioClipId.Music_Level01);
            yield return null;

            _audioService.StopMusic();
            yield return null;

            Assert.That(_audioService.CurrentMusicClip, Is.Null);
            Assert.That(_audioService.MusicSource.clip, Is.Null);
        }

        [UnityTest]
        public IEnumerator ApplyVolumes_CalculatesEffectiveSourceVolume()
        {
            _audioService.SetMasterVolume(0.5f);
            _audioService.SetMusicVolume(0.6f);
            _audioService.SetSfxVolume(0.8f);
            yield return null;

            Assert.That(_audioService.MusicSource.volume, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(_audioService.SfxSource.volume, Is.EqualTo(0.4f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator PlaySfx_AllIdsPlayWithoutExceptions()
        {
            foreach (AudioClipId sfxId in System.Enum.GetValues(typeof(AudioClipId)))
            {
                Assert.DoesNotThrow(() => _audioService.PlaySfx(sfxId));
            }
            yield return null;
        }
    }
}
