using System;
using HealthyRace.Audio;
using NUnit.Framework;
using UnityEngine;

namespace HealthyRace.Tests.EditMode
{
    [TestFixture]
    public sealed class AudioServiceTests
    {
        private GameObject _audioHost;
        private AudioService _audioService;

        [SetUp]
        public void SetUp()
        {
            _audioHost = new GameObject("Test_AudioServiceHost");
            _audioService = _audioHost.AddComponent<AudioService>();
            _audioService.InitializeSources();
        }

        [TearDown]
        public void TearDown()
        {
            if (_audioHost != null)
            {
                UnityEngine.Object.DestroyImmediate(_audioHost);
            }
        }

        [Test]
        public void AudioService_DefaultVolumes_AreValid()
        {
            Assert.That(_audioService.MasterVolume, Is.InRange(0f, 1f));
            Assert.That(_audioService.MusicVolume, Is.InRange(0f, 1f));
            Assert.That(_audioService.SfxVolume, Is.InRange(0f, 1f));
        }

        [Test]
        public void SetMasterVolume_ClampsBetweenZeroAndOne()
        {
            _audioService.SetMasterVolume(-0.5f);
            Assert.That(_audioService.MasterVolume, Is.EqualTo(0f));

            _audioService.SetMasterVolume(1.5f);
            Assert.That(_audioService.MasterVolume, Is.EqualTo(1f));

            _audioService.SetMasterVolume(0.42f);
            Assert.That(_audioService.MasterVolume, Is.EqualTo(0.42f).Within(0.001f));
        }

        [Test]
        public void SetMusicVolume_ClampsBetweenZeroAndOne()
        {
            _audioService.SetMusicVolume(-1f);
            Assert.That(_audioService.MusicVolume, Is.EqualTo(0f));

            _audioService.SetMusicVolume(2f);
            Assert.That(_audioService.MusicVolume, Is.EqualTo(1f));

            _audioService.SetMusicVolume(0.65f);
            Assert.That(_audioService.MusicVolume, Is.EqualTo(0.65f).Within(0.001f));
        }

        [Test]
        public void SetSfxVolume_ClampsBetweenZeroAndOne()
        {
            _audioService.SetSfxVolume(-0.2f);
            Assert.That(_audioService.SfxVolume, Is.EqualTo(0f));

            _audioService.SetSfxVolume(1.2f);
            Assert.That(_audioService.SfxVolume, Is.EqualTo(1f));

            _audioService.SetSfxVolume(0.75f);
            Assert.That(_audioService.SfxVolume, Is.EqualTo(0.75f).Within(0.001f));
        }

        [Test]
        public void SetVolumes_UpdatesBothMusicAndSfx()
        {
            _audioService.SetVolumes(0.35f, 0.85f);
            Assert.That(_audioService.MusicVolume, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(_audioService.SfxVolume, Is.EqualTo(0.85f).Within(0.001f));
        }

        [Test]
        public void VolumeChanged_EventFiresOnVolumeUpdate()
        {
            var fired = false;
            float receivedMaster = 0f,
                receivedMusic = 0f,
                receivedSfx = 0f;

            _audioService.VolumeChanged += (m, mu, s) =>
            {
                fired = true;
                receivedMaster = m;
                receivedMusic = mu;
                receivedSfx = s;
            };

            _audioService.SetMasterVolume(0.5f);

            Assert.That(fired, Is.True);
            Assert.That(receivedMaster, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void SaveSettings_PersistsToPlayerPrefs()
        {
            _audioService.SetMasterVolume(0.33f);
            _audioService.SetMusicVolume(0.44f);
            _audioService.SetSfxVolume(0.55f);

            Assert.That(
                PlayerPrefs.GetFloat("HealthyRace_MasterVolume"),
                Is.EqualTo(0.33f).Within(0.001f)
            );
            Assert.That(
                PlayerPrefs.GetFloat("HealthyRace_MusicVolume"),
                Is.EqualTo(0.44f).Within(0.001f)
            );
            Assert.That(
                PlayerPrefs.GetFloat("HealthyRace_SfxVolume"),
                Is.EqualTo(0.55f).Within(0.001f)
            );
        }

        [Test]
        public void GetClip_AllEnumIds_ReturnValidClipsWithPositiveLength()
        {
            foreach (AudioClipId id in Enum.GetValues(typeof(AudioClipId)))
            {
                var clip = _audioService.GetClip(id);
                Assert.That(clip, Is.Not.Null, $"Clip for {id} should not be null.");
                Assert.That(clip.length, Is.GreaterThan(0f), $"Clip {id} should have length > 0.");
            }
        }

        [Test]
        public void PlayMusicAndStopMusic_AssignsAndClearsClip()
        {
            _audioService.PlayMusic(AudioClipId.Music_Menu);
            Assert.That(_audioService.CurrentMusicClip, Is.Not.Null);

            _audioService.StopMusic();
            Assert.That(_audioService.CurrentMusicClip, Is.Null);
        }

        [Test]
        public void PlaySfx_ExecutesWithoutExceptions()
        {
            Assert.DoesNotThrow(() =>
            {
                _audioService.PlaySfx(AudioClipId.Sfx_Jump);
                _audioService.PlaySfx(AudioClipId.Sfx_Button, 0.5f);
                _audioService.PlaySfx(AudioClipId.Sfx_Checkpoint);
            });
        }
    }
}
