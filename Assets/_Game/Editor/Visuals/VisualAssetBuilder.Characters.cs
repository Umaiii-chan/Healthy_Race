using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HealthyRace.Level;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Personagem articulado: importa a folha de partes gerada por
    /// <c>tools/art/build_character_rig.py</c>, monta o esqueleto e escreve os clipes.
    /// As medidas abaixo repetem PARTS e RIG daquele script. Mudou lá, mude aqui.
    /// </summary>
    public static partial class VisualAssetBuilder
    {
        public const string CharacterSheetPath = Root + "Art/Characters/SP_CharacterRig.png";
        public const string PlayerPrefabPath = Root + "Prefabs/Characters/PF_Player.prefab";
        public const string PlayerMaterialPath = Root + "Materials/PM_Player.physicsMaterial2D";

        /// <summary>Altura dos pés em relação ao centro do colisor do jogador (1,25 / 2).</summary>
        public const float FeetY = -0.625f;

        private const int RigCell = 256;
        private const string BodyPath = "Rig/Body";
        private const string TorsoPath = BodyPath + "/Torso";
        private const string HeadPath = TorsoPath + "/Head";
        private const string ArmFrontPath = TorsoPath + "/ArmFront";
        private const string ArmBackPath = TorsoPath + "/ArmBack";
        private const string LegFrontPath = BodyPath + "/LegFront";
        private const string LegBackPath = BodyPath + "/LegBack";
        private const float ArmRest = 18f;

        private static readonly string[] RigSkins =
        {
            "healthy",
            "stable",
            "tired",
            "critical",
            "salo",
        };

        private static readonly (string Name, Vector2 Pivot)[] RigParts =
        {
            ("head", new Vector2(0.5f, 0.5f)),
            ("face", new Vector2(0.5f, 0.5f)),
            ("face_blink", new Vector2(0.5f, 0.5f)),
            ("face_effort", new Vector2(0.5f, 0.5f)),
            ("leaf", new Vector2(0.5f, 20f / RigCell)),
            ("torso", new Vector2(0.5f, 0.5f)),
            ("arm", new Vector2(0.5f, 208f / RigCell)),
            ("leg", new Vector2(0.5f, 208f / RigCell)),
        };

        /// <summary>
        /// Regera só o que é do personagem: peles, clipes, prefabs visuais dos dois
        /// jogadores, o efeito de poeira e o material de física do jogador.
        /// </summary>
        [MenuItem("Healthy Race/Visuals/Build character rig")]
        public static void BuildCharacters()
        {
            if (
                !Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
            )
                return;
            _catalog = AssetDatabase.LoadAssetAtPath<VisualCatalog>(CatalogPath);
            if (_catalog == null)
                throw new InvalidOperationException(
                    "Catálogo visual ausente: rode 'Build owned visual assets' antes."
                );
            _material = _catalog.SpriteMaterial;
            _catalog.Skins = ImportCharacterRig();
            _catalog.PlayerAnimator = BuildAnimations();
            EditorUtility.SetDirty(_catalog);
            var badge = CreateShape("SP_Badge", false);
            var p1 = BuildPlayer(1, badge);
            var p2 = BuildPlayer(2, badge);

            var dust = new GameObject("PF_VIS_VFX_" + FeedbackKind.Dust);
            dust.AddComponent<VisualEffectEmitter>().Configure(_catalog, FeedbackKind.Dust);
            SavePrefab(dust);

            ConfigurePlayerPhysics();

            var phaseScene = EditorSceneManager.OpenScene(Phase1ScenePath);
            var phase = Object.FindFirstObjectByType<Phase1Bootstrap>();
            Assign(phase, "_playerOneVisualPrefab", p1);
            Assign(phase, "_playerTwoVisualPrefab", p2);
            EditorSceneManager.SaveScene(phaseScene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "CHARACTER_BUILD_COMPLETE: skins, 12 clips, player prefabs and physics saved."
            );
        }

        /// <summary>
        /// Sem atrito o jogador não gruda na parede quando anda contra ela no ar.
        /// O movimento no chão é por velocidade, então o atrito nunca foi necessário.
        /// </summary>
        private static void ConfigurePlayerPhysics()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PlayerMaterialPath));
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PlayerMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("PM_Player") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(material, PlayerMaterialPath);
            }

            var contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            contents.GetComponent<Collider2D>().sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static CharacterSkin[] ImportCharacterRig()
        {
            var importer = AssetImporter.GetAtPath(CharacterSheetPath) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException(
                    "Folha do personagem ausente: rode tools/art/build_character_rig.py.",
                    CharacterSheetPath
                );
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = RigCell;
            importer.alphaIsTransparency = true;
            // A folha é desenhada a 256 px por unidade e aparece a ~60: sem mipmaps
            // o contorno cintila em movimento.
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(r => r.name);
            var height = RigSkins.Length * RigCell;
            var rects = new List<SpriteRect>();
            for (var row = 0; row < RigSkins.Length; row++)
            for (var col = 0; col < RigParts.Length; col++)
            {
                var name = RigSpriteName(row, col);
                rects.Add(
                    new SpriteRect
                    {
                        name = name,
                        spriteID = previous.TryGetValue(name, out var old)
                            ? old.spriteID
                            : GUID.Generate(),
                        rect = new Rect(
                            col * RigCell,
                            height - (row + 1) * RigCell,
                            RigCell,
                            RigCell
                        ),
                        alignment = SpriteAlignment.Custom,
                        pivot = RigParts[col].Pivot,
                    }
                );
            }
            provider.SetSpriteRects(rects.ToArray());
            var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            var sprites = AssetDatabase
                .LoadAllAssetsAtPath(CharacterSheetPath)
                .OfType<Sprite>()
                .ToDictionary(s => s.name);
            var skins = new CharacterSkin[RigSkins.Length];
            for (var row = 0; row < RigSkins.Length; row++)
            {
                skins[row] = new CharacterSkin
                {
                    Head = sprites[RigSpriteName(row, 0)],
                    Face = sprites[RigSpriteName(row, 1)],
                    FaceBlink = sprites[RigSpriteName(row, 2)],
                    FaceEffort = sprites[RigSpriteName(row, 3)],
                    Leaf = sprites[RigSpriteName(row, 4)],
                    Torso = sprites[RigSpriteName(row, 5)],
                    Arm = sprites[RigSpriteName(row, 6)],
                    Leg = sprites[RigSpriteName(row, 7)],
                };
            }
            return skins;
        }

        private static string RigSpriteName(int row, int col) =>
            "Rig_" + RigSkins[row] + "_" + RigParts[col].Name;

        private static Transform Bone(Transform parent, string name, Vector2 position)
        {
            var bone = new GameObject(name).transform;
            bone.SetParent(parent, false);
            bone.localPosition = position;
            return bone;
        }

        private static SpriteRenderer Part(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            int order,
            float rotation = 0f
        )
        {
            var renderer = Bone(parent, name, position).gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = _material;
            renderer.sortingOrder = order;
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            return renderer;
        }

        private static GameObject BuildPlayer(int player, Sprite badgeSprite)
        {
            if (
                _catalog.Skins == null
                || _catalog.Skins.Length == 0
                || !_catalog.Skins[0].IsComplete
            )
            {
                _catalog.Skins = ImportCharacterRig();
                EditorUtility.SetDirty(_catalog);
            }

            var skin = _catalog.Skins[0];
            var root = new GameObject("PF_VIS_Player_P" + player);
            var flip = Bone(root.transform, "Rig", new Vector2(0f, FeetY));
            var body = Bone(flip, "Body", Vector2.zero);
            var legBack = Part(body, "LegBack", skin.Leg, new Vector2(-0.11f, 0.25f), 18);
            var legFront = Part(body, "LegFront", skin.Leg, new Vector2(0.11f, 0.25f), 18);
            var torsoBone = Bone(body, "Torso", new Vector2(0f, 0.36f));
            var torso = Part(torsoBone, "TorsoSprite", skin.Torso, Vector2.zero, 20);
            var armBack = Part(
                torsoBone,
                "ArmBack",
                skin.Arm,
                new Vector2(-0.18f, 0.11f),
                17,
                -ArmRest
            );
            var armFront = Part(
                torsoBone,
                "ArmFront",
                skin.Arm,
                new Vector2(0.18f, 0.11f),
                23,
                ArmRest
            );
            // a cabeça gira em torno do pescoço, não do próprio centro
            var headBone = Bone(torsoBone, "Head", new Vector2(0f, 0.14f));
            var head = Part(headBone, "HeadSprite", skin.Head, new Vector2(0f, 0.35f), 21);
            var face = Part(headBone, "Face", skin.Face, new Vector2(0f, 0.35f), 22);
            var leafBone = Bone(headBone, "Leaf", new Vector2(-0.02f, 0.72f));
            var leaf = Part(leafBone, "LeafSprite", skin.Leaf, Vector2.zero, 19);

            var rig = root.AddComponent<CharacterRig>();
            rig.Configure(
                flip,
                leafBone,
                head,
                face,
                leaf,
                torso,
                armFront,
                armBack,
                legFront,
                legBack
            );
            rig.Apply(skin);

            var identity = SpriteChild(
                root.transform,
                "Identity",
                badgeSprite,
                new Vector2(0, 1.62f),
                new Vector2(1.1f, 0.5f),
                24
            );
            var badge = WorldText(
                root.transform,
                "Badge",
                "P" + player,
                new Vector2(0, 1.62f),
                0.28f,
                Color.white
            );
            badge.GetComponent<MeshRenderer>().sortingOrder = 25;
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = _catalog.PlayerAnimator;
            var presentation = root.AddComponent<PlayerPresentation>();
            presentation.Configure(_catalog, null, identity, badge, animator, player);
            presentation.ConfigureRig(rig);
            return SavePrefab(root);
        }

        // ------------------------------------------------------------------
        // Clipes
        // ------------------------------------------------------------------
        private static RuntimeAnimatorController BuildAnimations()
        {
            Directory.CreateDirectory(Root + "Animations/Visual");
            var clips = new Dictionary<VisualAction, AnimationClip>();
            foreach (VisualAction action in Enum.GetValues(typeof(VisualAction)))
            {
                var clipPath = Root + "Animations/Visual/AN_VIS_" + action + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    clip = new AnimationClip { name = "AN_VIS_" + action, frameRate = 30 };
                    AssetDatabase.CreateAsset(clip, clipPath);
                }
                else
                {
                    clip.ClearCurves();
                }

                var loop = WriteClip(clip, action);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
                clips[action] = clip;
            }

            var path = Root + "Animations/Visual/AC_VIS_Player.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null)
            {
                foreach (var state in controller.layers[0].stateMachine.states)
                    if (Enum.TryParse<VisualAction>(state.state.name, out var action))
                        state.state.motion = clips[action];
                EditorUtility.SetDirty(controller);
                return controller;
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Action", AnimatorControllerParameterType.Int);
            var machine = controller.layers[0].stateMachine;
            foreach (var pair in clips)
            {
                var i = (int)pair.Key;
                var state = machine.AddState(
                    pair.Key.ToString(),
                    new Vector3((i % 4) * 240, (i / 4) * 110)
                );
                state.motion = pair.Value;
                if (i == 0)
                    machine.defaultState = state;
                var transition = machine.AddAnyStateTransition(state);
                transition.hasExitTime = false;
                transition.duration = 0.07f;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.Equals, i, "Action");
            }
            return controller;
        }

        private static void Rot(AnimationClip clip, string path, params float[] keys) =>
            Keys(clip, path, "localEulerAnglesRaw.z", keys);

        private static void PosY(AnimationClip clip, string path, params float[] keys) =>
            Keys(clip, path, "m_LocalPosition.y", keys);

        /// <summary>Escala em pares (x, y): keys = tempo, x, y, tempo, x, y...</summary>
        private static void Scale(AnimationClip clip, string path, params float[] keys)
        {
            var x = new List<float>();
            var y = new List<float>();
            var z = new List<float>();
            for (var i = 0; i < keys.Length; i += 3)
            {
                x.AddRange(new[] { keys[i], keys[i + 1] });
                y.AddRange(new[] { keys[i], keys[i + 2] });
                z.AddRange(new[] { keys[i], 1f });
            }
            Keys(clip, path, "m_LocalScale.x", x.ToArray());
            Keys(clip, path, "m_LocalScale.y", y.ToArray());
            Keys(clip, path, "m_LocalScale.z", z.ToArray());
        }

        /// <summary>keys = tempo, valor, tempo, valor...</summary>
        private static void Keys(AnimationClip clip, string path, string property, float[] keys)
        {
            var curve = new AnimationCurve();
            for (var i = 0; i < keys.Length; i += 2)
                curve.AddKey(new Keyframe(keys[i], keys[i + 1]));
            for (var i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0f);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve
            );
        }

        /// <summary>
        /// Escreve as curvas da ação e devolve se o clipe repete. Ângulo positivo leva
        /// braço e perna para a frente (o personagem olha para +x); no tronco e na
        /// cabeça, negativo inclina para a frente.
        /// </summary>
        private static bool WriteClip(AnimationClip c, VisualAction action)
        {
            const float R = ArmRest;
            switch (action)
            {
                case VisualAction.Idle:
                    // respiração, braços soltos e um leve balanço de cabeça
                    Scale(c, BodyPath, 0f, 1f, 1f, 0.9f, 0.985f, 1.03f, 1.8f, 1f, 1f);
                    Rot(c, HeadPath, 0f, 0f, 0.45f, 2.5f, 0.9f, 0f, 1.35f, -2.5f, 1.8f, 0f);
                    Rot(c, ArmFrontPath, 0f, R, 0.9f, R + 5f, 1.8f, R);
                    Rot(c, ArmBackPath, 0f, -R, 0.9f, -R - 5f, 1.8f, -R);
                    return true;

                case VisualAction.Move:
                    // passada completa em 0,36 s: pernas alternadas, braços no contratempo
                    Rot(c, LegFrontPath, 0f, 36f, 0.18f, -36f, 0.36f, 36f);
                    Rot(c, LegBackPath, 0f, -36f, 0.18f, 36f, 0.36f, -36f);
                    Rot(c, ArmFrontPath, 0f, -34f, 0.18f, 48f, 0.36f, -34f);
                    Rot(c, ArmBackPath, 0f, 48f, 0.18f, -34f, 0.36f, 48f);
                    PosY(c, BodyPath, 0f, 0f, 0.09f, 0.05f, 0.18f, 0f, 0.27f, 0.05f, 0.36f, 0f);
                    Rot(c, TorsoPath, 0f, -8f, 0.18f, -6f, 0.36f, -8f);
                    Rot(c, HeadPath, 0f, 4f, 0.09f, 2f, 0.18f, 4f, 0.27f, 2f, 0.36f, 4f);
                    return true;

                case VisualAction.Jump:
                    // estica na saída, braços para cima, pernas recolhidas
                    Scale(c, BodyPath, 0f, 0.9f, 1.14f, 0.12f, 0.97f, 1.05f, 0.25f, 1f, 1f);
                    Rot(c, ArmFrontPath, 0f, R, 0.12f, 118f, 0.25f, 128f);
                    Rot(c, ArmBackPath, 0f, -R, 0.12f, -118f, 0.25f, -128f);
                    Rot(c, LegFrontPath, 0f, 0f, 0.12f, 30f, 0.25f, 26f);
                    Rot(c, LegBackPath, 0f, 0f, 0.12f, -24f, 0.25f, -20f);
                    Rot(c, TorsoPath, 0f, 0f, 0.25f, -4f);
                    Rot(c, HeadPath, 0f, 0f, 0.25f, 6f);
                    return false;

                case VisualAction.Fall:
                    // braços abertos batendo, pernas soltas
                    Scale(c, BodyPath, 0f, 0.97f, 1.04f, 0.25f, 0.98f, 1.03f, 0.5f, 0.97f, 1.04f);
                    Rot(
                        c,
                        ArmFrontPath,
                        0f,
                        108f,
                        0.125f,
                        128f,
                        0.25f,
                        108f,
                        0.375f,
                        128f,
                        0.5f,
                        108f
                    );
                    Rot(
                        c,
                        ArmBackPath,
                        0f,
                        -108f,
                        0.125f,
                        -128f,
                        0.25f,
                        -108f,
                        0.375f,
                        -128f,
                        0.5f,
                        -108f
                    );
                    Rot(c, LegFrontPath, 0f, 14f, 0.25f, 4f, 0.5f, 14f);
                    Rot(c, LegBackPath, 0f, -14f, 0.25f, -4f, 0.5f, -14f);
                    Rot(c, HeadPath, 0f, 5f, 0.5f, 5f);
                    return true;

                case VisualAction.Land:
                    // amassa no impacto e volta
                    Scale(c, BodyPath, 0f, 1.22f, 0.74f, 0.09f, 0.95f, 1.06f, 0.16f, 1f, 1f);
                    Rot(c, ArmFrontPath, 0f, 70f, 0.16f, R);
                    Rot(c, ArmBackPath, 0f, -70f, 0.16f, -R);
                    Rot(c, LegFrontPath, 0f, 16f, 0.16f, 0f);
                    Rot(c, LegBackPath, 0f, -16f, 0.16f, 0f);
                    return false;

                case VisualAction.Push:
                    // corpo inclinado, os dois braços à frente, passos curtos
                    Rot(c, TorsoPath, 0f, -17f, 0.22f, -14f, 0.44f, -17f);
                    Rot(c, HeadPath, 0f, 9f, 0.44f, 9f);
                    Rot(c, ArmFrontPath, 0f, 86f, 0.22f, 80f, 0.44f, 86f);
                    Rot(c, ArmBackPath, 0f, 78f, 0.22f, 72f, 0.44f, 78f);
                    Rot(c, LegFrontPath, 0f, 28f, 0.22f, -22f, 0.44f, 28f);
                    Rot(c, LegBackPath, 0f, -22f, 0.22f, 28f, 0.44f, -22f);
                    PosY(c, BodyPath, 0f, 0f, 0.11f, 0.02f, 0.22f, 0f, 0.33f, 0.02f, 0.44f, 0f);
                    return true;

                case VisualAction.Interact:
                    Rot(c, ArmFrontPath, 0f, R, 0.12f, 122f, 0.3f, R);
                    Rot(c, TorsoPath, 0f, 0f, 0.12f, -6f, 0.3f, 0f);
                    return false;

                case VisualAction.Eat:
                    // as duas mãos à boca e a cabeça cresce num "nhac"
                    Rot(c, ArmFrontPath, 0f, R, 0.1f, 146f, 0.25f, R);
                    Rot(c, ArmBackPath, 0f, -R, 0.1f, -146f, 0.25f, -R);
                    Scale(c, HeadPath, 0f, 1f, 1f, 0.1f, 1.1f, 0.94f, 0.25f, 1f, 1f);
                    return false;

                case VisualAction.Fatigue:
                    // curvado, braços pendurados, respiração pesada
                    Rot(c, TorsoPath, 0f, -14f, 1f, -18f, 2f, -14f);
                    Rot(c, HeadPath, 0f, -8f, 1f, -12f, 2f, -8f);
                    Rot(c, ArmFrontPath, 0f, 30f, 1f, 36f, 2f, 30f);
                    Rot(c, ArmBackPath, 0f, 12f, 1f, 18f, 2f, 12f);
                    Rot(c, LegFrontPath, 0f, 8f, 2f, 8f);
                    Rot(c, LegBackPath, 0f, -8f, 2f, -8f);
                    Scale(c, BodyPath, 0f, 1.03f, 0.93f, 1f, 1.01f, 0.97f, 2f, 1.03f, 0.93f);
                    return true;

                case VisualAction.Recover:
                    // espreguiça e volta
                    Scale(c, BodyPath, 0f, 1f, 1f, 0.22f, 0.95f, 1.1f, 0.5f, 1f, 1f);
                    Rot(c, ArmFrontPath, 0f, R, 0.22f, 132f, 0.5f, R);
                    Rot(c, ArmBackPath, 0f, -R, 0.22f, -132f, 0.5f, -R);
                    Rot(c, HeadPath, 0f, 0f, 0.22f, 7f, 0.5f, 0f);
                    return false;

                case VisualAction.Victory:
                    // pulinhos de comemoração com os braços para cima
                    PosY(c, BodyPath, 0f, 0f, 0.3f, 0.24f, 0.6f, 0f);
                    Scale(c, BodyPath, 0f, 1.08f, 0.9f, 0.3f, 0.96f, 1.07f, 0.6f, 1.08f, 0.9f);
                    Rot(c, ArmFrontPath, 0f, 108f, 0.3f, 138f, 0.6f, 108f);
                    Rot(c, ArmBackPath, 0f, -108f, 0.3f, -138f, 0.6f, -108f);
                    Rot(c, LegFrontPath, 0f, 0f, 0.3f, 26f, 0.6f, 0f);
                    Rot(c, LegBackPath, 0f, 0f, 0.3f, -26f, 0.6f, 0f);
                    Rot(c, HeadPath, 0f, -5f, 0.3f, 6f, 0.6f, -5f);
                    return true;

                case VisualAction.Defeat:
                    // ombros caídos, cabeça baixa, balanço lento
                    Rot(c, TorsoPath, 0f, -9f, 0.8f, -12f, 1.6f, -9f);
                    Rot(c, HeadPath, 0f, -11f, 0.8f, -14f, 1.6f, -11f);
                    Rot(c, ArmFrontPath, 0f, 26f, 0.8f, 30f, 1.6f, 26f);
                    Rot(c, ArmBackPath, 0f, 6f, 0.8f, 10f, 1.6f, 6f);
                    Scale(c, BodyPath, 0f, 1.02f, 0.95f, 0.8f, 1.01f, 0.97f, 1.6f, 1.02f, 0.95f);
                    return true;

                default:
                    return false;
            }
        }
    }
}
