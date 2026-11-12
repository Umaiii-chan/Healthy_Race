using HealthyRace.Core;
using NUnit.Framework;

namespace HealthyRace.Tests.EditMode
{
    /// <summary>A foto de dentro da build só acontece com o pedido completo na linha de comando.</summary>
    public sealed class BuildSmokeShotTests
    {
        [Test]
        public void WithoutTheArgument_NothingIsRequested()
        {
            Assert.That(
                BuildSmokeShot.TryParse(new[] { "HealthyRace.exe" }, out _, out _, out _),
                Is.False
            );
            Assert.That(BuildSmokeShot.TryParse(null, out _, out _, out _), Is.False);
        }

        [Test]
        public void LevelAndFile_AreRead_WithTheDefaultWait()
        {
            var asked = BuildSmokeShot.TryParse(
                new[] { "HealthyRace.exe", "-smoke-shot", "3", "C:/tmp/foto.png" },
                out var level,
                out var path,
                out var seconds
            );
            Assert.That(asked, Is.True);
            Assert.That(level, Is.EqualTo(3));
            Assert.That(path, Is.EqualTo("C:/tmp/foto.png"));
            Assert.That(seconds, Is.EqualTo(BuildSmokeShot.DefaultSeconds));
        }

        [Test]
        public void Wait_IsReadWithADecimalPoint_WhateverTheSystemLanguage()
        {
            BuildSmokeShot.TryParse(
                new[] { "-smoke-shot", "1", "foto.png", "2.5" },
                out _,
                out _,
                out var seconds
            );
            Assert.That(seconds, Is.EqualTo(2.5f));
        }

        [TestCase("0")]
        [TestCase("99")]
        [TestCase("fase")]
        public void UnknownLevel_IsRefused(string level)
        {
            Assert.That(
                BuildSmokeShot.TryParse(
                    new[] { "-smoke-shot", level, "foto.png" },
                    out _,
                    out _,
                    out _
                ),
                Is.False
            );
        }

        [Test]
        public void MissingFile_IsRefused()
        {
            Assert.That(
                BuildSmokeShot.TryParse(new[] { "-smoke-shot", "1" }, out _, out _, out _),
                Is.False
            );
        }
    }
}
