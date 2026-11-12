using System.Linq;
using HealthyRace.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HealthyRace.Tests.EditMode.Visuals
{
    /// <summary>
    /// Zuri e Tom na interface (menu e retrato do HUD-modelo) e a garantia de que os
    /// membros em curva são desenhados de fato, com o shader próprio.
    /// </summary>
    public sealed class RunnerInterfaceTests
    {
        private const string Root = "Assets/_Game/";
        private const string PrefabFolder = Root + "Prefabs/Visual/";
        private static readonly string[] Names = { "Zuri", "Tom" };

        // longe da origem: nada que outro teste tenha deixado na cena entra na foto
        private static readonly Vector3 Stage = new(500f, 500f, 0f);
        private static readonly Color32 Backdrop = new(255, 0, 255, 255);
        private const int PhotoSize = 256;
        private const float PhotoHalfHeight = 1f;

        private GameObject _instance;
        private GameObject _camera;

        [TearDown]
        public void TearDown()
        {
            if (_camera != null)
            {
                Object.DestroyImmediate(_camera);
            }

            if (_instance == null)
            {
                return;
            }

            foreach (var limb in _instance.GetComponentsInChildren<HoseLimb>())
            {
                if (limb.Mesh != null)
                {
                    Object.DestroyImmediate(limb.Mesh);
                }
            }

            Object.DestroyImmediate(_instance);
        }

        private static VisualCatalog Catalog() =>
            AssetDatabase.LoadAssetAtPath<VisualCatalog>(
                Root + "Art/VisualAtlas/SO_VisualCatalog.asset"
            );

        [Test]
        public void Catalog_HasFivePortraitsAndOnePose_ForEachRunner()
        {
            var catalog = Catalog();
            Assert.That(catalog.RunnerPortraits.Length, Is.EqualTo(10));
            Assert.That(catalog.RunnerPortraits.All(s => s != null), Is.True);
            Assert.That(catalog.RunnerPortraits.Distinct().Count(), Is.EqualTo(10));
            Assert.That(catalog.RunnerPoses.Length, Is.EqualTo(2));

            for (var player = 1; player <= 2; player++)
            {
                var name = Names[player - 1];
                Assert.That(catalog.RunnerPose(player).name, Is.EqualTo("Pose_" + name + "_Run"));
                foreach (
                    WellbeingAppearance appearance in System.Enum.GetValues(
                        typeof(WellbeingAppearance)
                    )
                )
                {
                    Assert.That(
                        catalog.Portrait(player, appearance).name,
                        Is.EqualTo("Portrait_" + name + "_" + (int)appearance)
                    );
                }
            }
        }

        [Test]
        public void Catalog_WithoutRunnerArt_FallsBackToTheOldSprites()
        {
            var real = Catalog();
            var old = ScriptableObject.CreateInstance<VisualCatalog>();
            try
            {
                old.Characters = real.Characters;
                old.RunnerPortraits = new Sprite[0];
                old.RunnerPoses = null;
                Assert.That(
                    old.Portrait(2, WellbeingAppearance.Tired),
                    Is.SameAs(real.Characters[(int)WellbeingAppearance.Tired])
                );
                Assert.That(old.RunnerPose(1), Is.SameAs(real.Characters[0]));
            }
            finally
            {
                Object.DestroyImmediate(old);
            }
        }

        [Test]
        public void MenuScreen_IsOnlyTheTitle_AndLetsClicksThrough()
        {
            // Menu 2: quem aparece no menu são os próprios jogadores, na estufa jogável.
            // A tela do prefab é só o letreiro, por cima do palco.
            var catalog = Catalog();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabFolder + "PF_VIS_Screens.prefab"
            );
            var menu = prefab.GetComponent<RaceScreenView>().ScreenRoot(VisualScreen.MainMenu);
            Assert.That(menu, Is.Not.Null);

            Assert.That(menu.GetComponentsInChildren<Button>(true), Is.Empty);
            var graphics = menu.GetComponentsInChildren<Graphic>(true);
            Assert.That(
                graphics.Where(graphic => graphic.raycastTarget).Select(graphic => graphic.name),
                Is.Empty,
                "nada no letreiro pode tapar o clique nas placas do palco"
            );
            var sprouts = menu.GetComponentsInChildren<Image>(true)
                .Where(image => catalog.Characters.Contains(image.sprite))
                .Select(image => image.name)
                .ToArray();
            Assert.That(sprouts, Is.Empty, "o menu não mostra mais os brotos");
            Assert.That(
                menu.GetComponentsInChildren<Text>(true).Select(text => text.text),
                Has.Some.EqualTo("HEALTHY").And.Some.EqualTo("RACE")
            );
        }

        [Test]
        public void HudView_ShowsEachPlayersOwnPortrait_AndOnlyTheHeadbandChanges()
        {
            var catalog = Catalog();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabFolder + "PF_VIS_Screens.prefab"
            );
            _instance = Object.Instantiate(prefab);
            var players = _instance.GetComponent<RaceScreenView>().Players;
            for (var player = 1; player <= 2; player++)
            {
                var view = players[player - 1];
                var portrait = view.transform.Find("Portrait").GetComponent<Image>();

                view.Render(new PlayerHudData { Player = player, Wellbeing = 100 });
                Assert.That(
                    portrait.sprite,
                    Is.SameAs(catalog.Portrait(player, WellbeingAppearance.Healthy))
                );

                view.Render(new PlayerHudData { Player = player, Wellbeing = 0 });
                Assert.That(
                    portrait.sprite,
                    Is.SameAs(catalog.Portrait(player, WellbeingAppearance.Salo))
                );
                Assert.That(portrait.color, Is.EqualTo(Color.white), "o retrato não é tingido");
            }
        }

        [TestCaseSource(nameof(Names))]
        public void Limbs_UseTheirOwnShader_NotTheSpriteOne(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabFolder + "PF_VIS_Runner_" + name + ".prefab"
            );
            var limbs = prefab.GetComponentsInChildren<HoseLimb>(true);
            Assert.That(limbs.Length, Is.EqualTo(4));
            foreach (var limb in limbs)
            {
                // O shader de sprite do URP depende de valores que só um SpriteRenderer
                // define; numa malha comum o membro some conforme a ordem de desenho.
                var material = limb.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(material, Is.Not.Null, limb.name);
                Assert.That(material.shader.name, Is.EqualTo(HoseLimb.ShaderName), limb.name);
                Assert.That(material.shader.isSupported, Is.True, limb.name);
            }
        }

        [TestCaseSource(nameof(Names))]
        public void Limbs_AreDrawn_AloneAndAmongTheSprites(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabFolder + "PF_VIS_Runner_" + name + ".prefab"
            );
            Assert.That(prefab, Is.Not.Null, "rode 'Build runner rig (Zuri e Tom)'");
            _instance = Object.Instantiate(prefab, Stage, Quaternion.identity);
            var rig = _instance.GetComponent<RunnerRig>();
            rig.RebuildLimbs();
            var sprites = _instance.GetComponentsInChildren<SpriteRenderer>();

            foreach (var sprite in sprites)
            {
                sprite.enabled = false;
            }

            var limbsOnly = Photograph();

            foreach (var sprite in sprites)
            {
                sprite.enabled = true;
            }

            var everything = Photograph();

            foreach (var limb in rig.Limbs)
            {
                var bounds = limb.GetComponent<MeshRenderer>().bounds;
                var alone = Painted(limbsOnly, bounds);
                var among = Painted(everything, bounds);
                Assert.That(alone, Is.GreaterThan(60), limb.name + " não foi desenhado");
                // as peças em sprite só acrescentam tinta por cima: se a área pintada
                // diminui, o membro sumiu quando os sprites entraram na cena
                Assert.That(
                    among,
                    Is.GreaterThanOrEqualTo(alone),
                    limb.name + " some quando há sprites na cena"
                );
            }
        }

        /// <summary>Foto quadrada do palco, com fundo de cor que não existe nos personagens.</summary>
        private Color32[] Photograph()
        {
            if (_camera == null)
            {
                _camera = new GameObject("RunnerInterfaceTests_Camera", typeof(Camera));
                var setup = _camera.GetComponent<Camera>();
                setup.orthographic = true;
                setup.orthographicSize = PhotoHalfHeight;
                setup.clearFlags = CameraClearFlags.SolidColor;
                setup.backgroundColor = Backdrop;
                // a origem do prefab fica no centro do jogador; os pés, 0,5 abaixo
                _camera.transform.position = Stage + new Vector3(0f, 0.25f, -10f);
            }

            var camera = _camera.GetComponent<Camera>();
            var target = new RenderTexture(PhotoSize, PhotoSize, 24);
            camera.targetTexture = target;
            // na primeira vez que uma variante de shader é usada o Editor compila em
            // segundo plano e pula o desenho
            var asyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            camera.Render();
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(PhotoSize, PhotoSize, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, PhotoSize, PhotoSize), 0, 0);
            texture.Apply();
            var pixels = texture.GetPixels32();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
            return pixels;
        }

        /// <summary>Quantos pixels dentro da área do membro deixaram de ser fundo.</summary>
        private int Painted(Color32[] pixels, Bounds bounds)
        {
            var center = _camera.transform.position;
            var scale = PhotoSize / (2f * PhotoHalfHeight);
            int Pixel(float world, float origin) =>
                Mathf.Clamp(
                    Mathf.RoundToInt((world - origin + PhotoHalfHeight) * scale),
                    0,
                    PhotoSize - 1
                );
            var count = 0;
            for (var y = Pixel(bounds.min.y, center.y); y <= Pixel(bounds.max.y, center.y); y++)
            for (var x = Pixel(bounds.min.x, center.x); x <= Pixel(bounds.max.x, center.x); x++)
            {
                var pixel = pixels[y * PhotoSize + x];
                if (
                    Mathf.Abs(pixel.r - Backdrop.r) > 24
                    || Mathf.Abs(pixel.g - Backdrop.g) > 24
                    || Mathf.Abs(pixel.b - Backdrop.b) > 24
                )
                {
                    count++;
                }
            }

            return count;
        }
    }
}
