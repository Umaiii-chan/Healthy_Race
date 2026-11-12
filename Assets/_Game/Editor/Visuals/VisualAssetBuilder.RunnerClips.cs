using System.Collections.Generic;
using System.IO;
using HealthyRace.Visuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HealthyRace.Editor.Visuals
{
    /// <summary>
    /// Clipes de Zuri e Tom. Diferente do rig dos brotos, que gira braços e pernas, aqui
    /// os clipes movem os <b>alvos</b> de mão e pé (posição em <c>Rig/Body</c>) e os
    /// membros em curva acompanham. A deformação continua só na raiz (<c>Rig/Body</c>),
    /// como na linguagem de animação aprovada (elástico moderado). Cada personagem tem os
    /// 12 estados do rig antigo, com os mesmos nomes e números de ação, mais pico e queda.
    /// </summary>
    public static partial class VisualAssetBuilder
    {
        public const string RunnerAnimationFolder = Root + "Animations/Visual/Runner/";

        private const string RunnerBody = "Rig/Body";
        private const string RunnerTorso = RunnerBody + "/Torso";
        private const string RunnerHead = RunnerTorso + "/Head";
        private const string HandBackPath = RunnerBody + "/HandBack";
        private const string HandFrontPath = RunnerBody + "/HandFront";
        private const string FootBackPath = RunnerBody + "/FootBack";
        private const string FootFrontPath = RunnerBody + "/FootFront";

        /// <summary>Altura do tornozelo com o tênis no chão.</summary>
        private const float Ground = 0.18f;

        /// <summary>Estados do controlador dos corredores, na ordem do número de ação.</summary>
        public static readonly string[] RunnerStates =
        {
            nameof(VisualAction.Idle),
            nameof(VisualAction.Move),
            nameof(VisualAction.Jump),
            nameof(VisualAction.Fall),
            nameof(VisualAction.Land),
            nameof(VisualAction.Push),
            nameof(VisualAction.Interact),
            nameof(VisualAction.Eat),
            nameof(VisualAction.Fatigue),
            nameof(VisualAction.Recover),
            nameof(VisualAction.Victory),
            nameof(VisualAction.Defeat),
            RunnerRig.RushState,
            RunnerRig.CrashState,
        };

        public static string RunnerControllerPath(RunnerSpec spec) =>
            RunnerAnimationFolder + "AC_VIS_Runner_" + spec.Name + ".controller";

        public static string RunnerClipPath(RunnerSpec spec, string state) =>
            RunnerAnimationFolder + "AN_RUN_" + spec.Name + "_" + state + ".anim";

        private static RuntimeAnimatorController BuildRunnerAnimations(RunnerSpec spec)
        {
            Directory.CreateDirectory(RunnerAnimationFolder);
            var clips = new List<AnimationClip>();
            foreach (var state in RunnerStates)
            {
                var clipPath = RunnerClipPath(spec, state);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    clip = new AnimationClip
                    {
                        name = Path.GetFileNameWithoutExtension(clipPath),
                        frameRate = 30,
                    };
                    AssetDatabase.CreateAsset(clip, clipPath);
                }
                else
                {
                    clip.ClearCurves();
                }

                var loop = WriteRunnerClip(clip, spec, state);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
                clips.Add(clip);
            }

            var path = RunnerControllerPath(spec);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null)
            {
                foreach (var state in controller.layers[0].stateMachine.states)
                {
                    var index = System.Array.IndexOf(RunnerStates, state.state.name);
                    if (index >= 0)
                        state.state.motion = clips[index];
                }

                EditorUtility.SetDirty(controller);
                return controller;
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Action", AnimatorControllerParameterType.Int);
            var machine = controller.layers[0].stateMachine;
            for (var i = 0; i < RunnerStates.Length; i++)
            {
                var state = machine.AddState(
                    RunnerStates[i],
                    new Vector3((i % 4) * 240, (i / 4) * 110)
                );
                state.motion = clips[i];
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

        /// <summary>Posição em trios: keys = tempo, x, y, tempo, x, y...</summary>
        private static void Pos(AnimationClip clip, string path, params float[] keys)
        {
            var x = new List<float>();
            var y = new List<float>();
            for (var i = 0; i < keys.Length; i += 3)
            {
                x.AddRange(new[] { keys[i], keys[i + 1] });
                y.AddRange(new[] { keys[i], keys[i + 2] });
            }

            Keys(clip, path, "m_LocalPosition.x", x.ToArray());
            Keys(clip, path, "m_LocalPosition.y", y.ToArray());
        }

        /// <summary>Parâmetros de uma passada: corrida, pico e queda usam a mesma conta.</summary>
        private readonly struct Gait
        {
            public Gait(
                float period,
                float stride,
                float lift,
                float bob,
                float dip,
                float lean,
                float swingForward,
                float swingBack,
                float handRise,
                float headTilt
            )
            {
                Period = period;
                Stride = stride;
                Lift = lift;
                Bob = bob;
                Dip = dip;
                Lean = lean;
                SwingForward = swingForward;
                SwingBack = swingBack;
                HandRise = handRise;
                HeadTilt = headTilt;
            }

            /// <summary>Duração da passada completa (dois passos).</summary>
            public float Period { get; }

            /// <summary>Quanto o pé vai à frente e atrás do quadril.</summary>
            public float Stride { get; }

            /// <summary>Altura do pé recolhido.</summary>
            public float Lift { get; }

            /// <summary>Quanto o corpo sobe no voo.</summary>
            public float Bob { get; }

            /// <summary>Quanto o corpo afunda no apoio.</summary>
            public float Dip { get; }

            /// <summary>Inclinação do tronco (negativo = para a frente).</summary>
            public float Lean { get; }
            public float SwingForward { get; }
            public float SwingBack { get; }

            /// <summary>Altura da mão que vai à frente, acima do quadril.</summary>
            public float HandRise { get; }
            public float HeadTilt { get; }
        }

        /// <summary>
        /// Passada em quatro poses por perna (contato, apoio, impulso, recolhimento), com a
        /// outra perna meia passada atrás e os braços em oposição às pernas.
        /// </summary>
        private static void WriteGait(AnimationClip c, RunnerSpec spec, Gait g)
        {
            var quarter = g.Period * 0.25f;
            // pé: contato à frente, apoio sob o corpo, impulso atrás, recolhido passando
            var foot = new (float X, float Y, float Rot)[]
            {
                (g.Stride, Ground + 0.02f, 16f),
                (0.02f, Ground, 0f),
                (-g.Stride, Ground + g.Lift * 0.45f, -48f),
                (-0.02f, Ground + g.Lift, -22f),
            };
            // mão do mesmo lado: atrás quando o pé está à frente
            var hand = new (float X, float Y)[]
            {
                (-g.SwingBack, spec.Hip + 0.05f),
                (0.0f, spec.Hip - 0.01f),
                (g.SwingForward, spec.Hip + g.HandRise),
                (0.09f, spec.Hip + g.HandRise * 0.45f),
            };

            void Side(string footPath, string handPath, int phase)
            {
                var position = new List<float>();
                var rotation = new List<float>();
                var reach = new List<float>();
                for (var k = 0; k <= 4; k++)
                {
                    var pose = (k + phase) % 4;
                    var t = k * quarter;
                    position.AddRange(new[] { t, foot[pose].X, foot[pose].Y });
                    rotation.AddRange(new[] { t, foot[pose].Rot });
                    reach.AddRange(new[] { t, hand[pose].X, hand[pose].Y });
                }

                Pos(c, footPath, position.ToArray());
                Rot(c, footPath, rotation.ToArray());
                Pos(c, handPath, reach.ToArray());
            }

            Side(FootFrontPath, HandFrontPath, 0);
            Side(FootBackPath, HandBackPath, 2);

            // dois passos por passada: o corpo afunda no apoio e sobe no voo, duas vezes
            var eighth = g.Period * 0.125f;
            PosY(
                c,
                RunnerBody,
                0f,
                0f,
                eighth,
                -g.Dip,
                eighth * 3f,
                g.Bob,
                eighth * 4f,
                0f,
                eighth * 5f,
                -g.Dip,
                eighth * 7f,
                g.Bob,
                g.Period,
                0f
            );
            Rot(
                c,
                RunnerTorso,
                0f,
                g.Lean,
                quarter,
                g.Lean + 2f,
                quarter * 2f,
                g.Lean,
                g.Period,
                g.Lean
            );
            Rot(c, RunnerHead, 0f, g.HeadTilt, g.Period, g.HeadTilt);
        }

        /// <summary>
        /// Escreve as curvas do estado e devolve se o clipe repete. O personagem olha para
        /// +x; no tronco e na cabeça, ângulo negativo inclina para a frente.
        /// </summary>
        private static bool WriteRunnerClip(AnimationClip c, RunnerSpec spec, string state)
        {
            var zuri = spec.Name == "Zuri";
            var hip = spec.Hip;
            var shoulder = spec.Hip + spec.Shoulder;
            var hf = spec.HandFront;
            var hb = spec.HandBack;
            var ff = spec.FootFront;
            var fb = spec.FootBack;

            switch (state)
            {
                case nameof(VisualAction.Idle):
                    // respiração na raiz; Zuri de mãos na cintura, Tom com uma solta
                    Scale(c, RunnerBody, 0f, 1f, 1f, 0.9f, 0.985f, 1.025f, 1.8f, 1f, 1f);
                    Rot(c, RunnerHead, 0f, 0f, 0.45f, 2.5f, 0.9f, 0f, 1.35f, -2.5f, 1.8f, 0f);
                    if (zuri)
                    {
                        Pos(
                            c,
                            HandFrontPath,
                            0f,
                            0.10f,
                            hip + 0.05f,
                            0.9f,
                            0.10f,
                            hip + 0.065f,
                            1.8f,
                            0.10f,
                            hip + 0.05f
                        );
                        Pos(
                            c,
                            HandBackPath,
                            0f,
                            -0.10f,
                            hip + 0.05f,
                            0.9f,
                            -0.10f,
                            hip + 0.065f,
                            1.8f,
                            -0.10f,
                            hip + 0.05f
                        );
                    }
                    else
                    {
                        Pos(
                            c,
                            HandFrontPath,
                            0f,
                            hf.x,
                            hf.y,
                            0.9f,
                            hf.x + 0.01f,
                            hf.y + 0.015f,
                            1.8f,
                            hf.x,
                            hf.y
                        );
                        Pos(
                            c,
                            HandBackPath,
                            0f,
                            -0.10f,
                            hip + 0.06f,
                            0.9f,
                            -0.10f,
                            hip + 0.075f,
                            1.8f,
                            -0.10f,
                            hip + 0.06f
                        );
                    }

                    return true;

                case nameof(VisualAction.Move):
                    // Zuri corre aos saltos (passada longa, corpo sobe); Tom corre de tiro
                    WriteGait(
                        c,
                        spec,
                        zuri
                            ? new Gait(
                                0.68f,
                                0.17f,
                                0.20f,
                                0.09f,
                                0.02f,
                                -8f,
                                0.22f,
                                0.17f,
                                0.13f,
                                4f
                            )
                            : new Gait(
                                0.52f,
                                0.19f,
                                0.16f,
                                0.04f,
                                0.015f,
                                -14f,
                                0.23f,
                                0.18f,
                                0.12f,
                                6f
                            )
                    );
                    return true;

                case RunnerRig.RushState:
                    // pico: corpo todo esticado para a frente, passada rápida e larga
                    WriteGait(
                        c,
                        spec,
                        new Gait(0.34f, 0.22f, 0.20f, 0.06f, 0.01f, -22f, 0.25f, 0.14f, 0.11f, 12f)
                    );
                    Scale(c, RunnerBody, 0f, 1.04f, 0.97f, 0.34f, 1.04f, 0.97f);
                    return true;

                case RunnerRig.CrashState:
                    // queda: passo arrastado, tronco e cabeça caídos, mãos soltas
                    WriteGait(
                        c,
                        spec,
                        new Gait(1.10f, 0.07f, 0.03f, 0f, 0.012f, -7f, 0.05f, 0.04f, 0.0f, -13f)
                    );
                    return true;

                case nameof(VisualAction.Jump):
                    // antecipação, estica na saída, braços para cima, pernas recolhidas
                    Scale(c, RunnerBody, 0f, 1.1f, 0.86f, 0.08f, 0.92f, 1.13f, 0.25f, 1f, 1f);
                    Pos(
                        c,
                        HandFrontPath,
                        0f,
                        hf.x,
                        hf.y,
                        0.08f,
                        0.27f,
                        shoulder + 0.08f,
                        0.25f,
                        0.33f,
                        shoulder + 0.18f
                    );
                    Pos(
                        c,
                        HandBackPath,
                        0f,
                        hb.x,
                        hb.y,
                        0.08f,
                        -0.25f,
                        shoulder + 0.06f,
                        0.25f,
                        -0.30f,
                        shoulder + 0.14f
                    );
                    Pos(c, FootFrontPath, 0f, ff.x, ff.y, 0.08f, 0.10f, 0.26f, 0.25f, 0.12f, 0.30f);
                    Pos(
                        c,
                        FootBackPath,
                        0f,
                        fb.x,
                        fb.y,
                        0.08f,
                        -0.10f,
                        0.24f,
                        0.25f,
                        -0.10f,
                        0.27f
                    );
                    Rot(c, FootBackPath, 0f, 0f, 0.25f, -28f);
                    Rot(c, RunnerTorso, 0f, 0f, 0.25f, -4f);
                    Rot(c, RunnerHead, 0f, 0f, 0.25f, 6f);
                    return false;

                case nameof(VisualAction.Fall):
                    // braços abertos batendo, pernas soltas
                    Scale(c, RunnerBody, 0f, 0.97f, 1.04f, 0.25f, 0.98f, 1.03f, 0.5f, 0.97f, 1.04f);
                    Pos(
                        c,
                        HandFrontPath,
                        0f,
                        0.33f,
                        shoulder + 0.10f,
                        0.125f,
                        0.32f,
                        shoulder + 0.19f,
                        0.25f,
                        0.33f,
                        shoulder + 0.10f,
                        0.375f,
                        0.32f,
                        shoulder + 0.19f,
                        0.5f,
                        0.33f,
                        shoulder + 0.10f
                    );
                    Pos(
                        c,
                        HandBackPath,
                        0f,
                        -0.31f,
                        shoulder + 0.17f,
                        0.125f,
                        -0.33f,
                        shoulder + 0.08f,
                        0.25f,
                        -0.31f,
                        shoulder + 0.17f,
                        0.375f,
                        -0.33f,
                        shoulder + 0.08f,
                        0.5f,
                        -0.31f,
                        shoulder + 0.17f
                    );
                    Pos(
                        c,
                        FootFrontPath,
                        0f,
                        0.09f,
                        0.17f,
                        0.25f,
                        0.06f,
                        0.15f,
                        0.5f,
                        0.09f,
                        0.17f
                    );
                    Pos(
                        c,
                        FootBackPath,
                        0f,
                        -0.11f,
                        0.21f,
                        0.25f,
                        -0.08f,
                        0.19f,
                        0.5f,
                        -0.11f,
                        0.21f
                    );
                    Rot(c, FootFrontPath, 0f, -12f, 0.5f, -12f);
                    Rot(c, FootBackPath, 0f, -26f, 0.5f, -26f);
                    Rot(c, RunnerHead, 0f, 5f, 0.5f, 5f);
                    return true;

                case nameof(VisualAction.Land):
                    // amassa só na raiz e volta; pés afastados, mãos baixas
                    Scale(c, RunnerBody, 0f, 1.22f, 0.74f, 0.09f, 0.95f, 1.06f, 0.16f, 1f, 1f);
                    Pos(c, FootFrontPath, 0f, 0.15f, Ground, 0.16f, ff.x, ff.y);
                    Pos(c, FootBackPath, 0f, -0.16f, Ground, 0.16f, fb.x, fb.y);
                    Pos(c, HandFrontPath, 0f, 0.22f, hip + 0.02f, 0.16f, hf.x, hf.y);
                    Pos(c, HandBackPath, 0f, -0.22f, hip + 0.02f, 0.16f, hb.x, hb.y);
                    return false;

                case nameof(VisualAction.Push):
                    // corpo inclinado, as duas mãos à frente, passos curtos
                    Rot(c, RunnerTorso, 0f, -17f, 0.22f, -14f, 0.44f, -17f);
                    Rot(c, RunnerHead, 0f, 9f, 0.44f, 9f);
                    Pos(
                        c,
                        HandFrontPath,
                        0f,
                        0.31f,
                        hip + 0.24f,
                        0.22f,
                        0.30f,
                        hip + 0.22f,
                        0.44f,
                        0.31f,
                        hip + 0.24f
                    );
                    Pos(
                        c,
                        HandBackPath,
                        0f,
                        0.29f,
                        hip + 0.14f,
                        0.22f,
                        0.28f,
                        hip + 0.12f,
                        0.44f,
                        0.29f,
                        hip + 0.14f
                    );
                    Pos(
                        c,
                        FootFrontPath,
                        0f,
                        0.10f,
                        Ground,
                        0.22f,
                        -0.08f,
                        Ground + 0.03f,
                        0.44f,
                        0.10f,
                        Ground
                    );
                    Pos(
                        c,
                        FootBackPath,
                        0f,
                        -0.10f,
                        Ground + 0.03f,
                        0.22f,
                        0.06f,
                        Ground,
                        0.44f,
                        -0.10f,
                        Ground + 0.03f
                    );
                    PosY(c, RunnerBody, 0f, 0f, 0.11f, 0.02f, 0.22f, 0f, 0.33f, 0.02f, 0.44f, 0f);
                    return true;

                case nameof(VisualAction.Interact):
                    Pos(
                        c,
                        HandFrontPath,
                        0f,
                        hf.x,
                        hf.y,
                        0.12f,
                        0.28f,
                        hip + 0.30f,
                        0.3f,
                        hf.x,
                        hf.y
                    );
                    Rot(c, RunnerTorso, 0f, 0f, 0.12f, -6f, 0.3f, 0f);
                    return false;

                case nameof(VisualAction.Eat):
                    // as duas mãos à boca e a cabeça cresce num "nhac"
                    Pos(
                        c,
                        HandFrontPath,
                        0f,
                        hf.x,
                        hf.y,
                        0.1f,
                        0.15f,
                        shoulder + 0.10f,
                        0.25f,
                        hf.x,
                        hf.y
                    );
                    Pos(
                        c,
                        HandBackPath,
                        0f,
                        hb.x,
                        hb.y,
                        0.1f,
                        0.08f,
                        shoulder + 0.08f,
                        0.25f,
                        hb.x,
                        hb.y
                    );
                    Scale(c, RunnerHead, 0f, 1f, 1f, 0.1f, 1.08f, 0.95f, 0.25f, 1f, 1f);
                    return false;

                case nameof(VisualAction.Fatigue):
                    // dobrado para a frente, mãos quase nos joelhos, respiração pesada
                    Rot(c, RunnerTorso, 0f, -20f, 1f, -24f, 2f, -20f);
                    Rot(c, RunnerHead, 0f, -8f, 1f, -12f, 2f, -8f);
                    Pos(
                        c,
                        HandFrontPath,
                        0f,
                        0.17f,
                        hip - 0.09f,
                        1f,
                        0.18f,
                        hip - 0.11f,
                        2f,
                        0.17f,
                        hip - 0.09f
                    );
                    Pos(
                        c,
                        HandBackPath,
                        0f,
                        0.05f,
                        hip - 0.05f,
                        1f,
                        0.06f,
                        hip - 0.07f,
                        2f,
                        0.05f,
                        hip - 0.05f
                    );
                    Scale(c, RunnerBody, 0f, 1.03f, 0.94f, 1f, 1.01f, 0.97f, 2f, 1.03f, 0.94f);
                    return true;

                case nameof(VisualAction.Recover):
                    // espreguiça e volta
                    Scale(c, RunnerBody, 0f, 1f, 1f, 0.22f, 0.95f, 1.09f, 0.5f, 1f, 1f);
                    Pos(
                        c,
                        HandFrontPath,
                        0f,
                        hf.x,
                        hf.y,
                        0.22f,
                        0.33f,
                        shoulder + 0.20f,
                        0.5f,
                        hf.x,
                        hf.y
                    );
                    Pos(
                        c,
                        HandBackPath,
                        0f,
                        hb.x,
                        hb.y,
                        0.22f,
                        -0.30f,
                        shoulder + 0.18f,
                        0.5f,
                        hb.x,
                        hb.y
                    );
                    Rot(c, RunnerHead, 0f, 0f, 0.22f, 7f, 0.5f, 0f);
                    return false;

                case nameof(VisualAction.Victory):
                    // Zuri pula com os dois braços; Tom dá um soco no ar
                    PosY(c, RunnerBody, 0f, 0f, 0.3f, zuri ? 0.24f : 0.14f, 0.6f, 0f);
                    Scale(c, RunnerBody, 0f, 1.08f, 0.9f, 0.3f, 0.96f, 1.07f, 0.6f, 1.08f, 0.9f);
                    Pos(
                        c,
                        HandFrontPath,
                        0f,
                        0.29f,
                        shoulder + 0.08f,
                        0.3f,
                        0.33f,
                        shoulder + 0.21f,
                        0.6f,
                        0.29f,
                        shoulder + 0.08f
                    );
                    if (zuri)
                    {
                        Pos(
                            c,
                            HandBackPath,
                            0f,
                            -0.28f,
                            shoulder + 0.06f,
                            0.3f,
                            -0.30f,
                            shoulder + 0.19f,
                            0.6f,
                            -0.28f,
                            shoulder + 0.06f
                        );
                    }
                    else
                    {
                        Pos(c, HandBackPath, 0f, -0.10f, hip + 0.06f, 0.6f, -0.10f, hip + 0.06f);
                    }

                    Pos(c, FootFrontPath, 0f, ff.x, ff.y, 0.3f, 0.12f, 0.27f, 0.6f, ff.x, ff.y);
                    Pos(c, FootBackPath, 0f, fb.x, fb.y, 0.3f, -0.12f, 0.25f, 0.6f, fb.x, fb.y);
                    Rot(c, RunnerHead, 0f, -5f, 0.3f, 7f, 0.6f, -5f);
                    return true;

                case nameof(VisualAction.Defeat):
                    // ombros caídos, cabeça baixa, balanço lento
                    Rot(c, RunnerTorso, 0f, -9f, 0.8f, -12f, 1.6f, -9f);
                    Rot(c, RunnerHead, 0f, -11f, 0.8f, -14f, 1.6f, -11f);
                    Pos(
                        c,
                        HandFrontPath,
                        0f,
                        0.13f,
                        hip - 0.03f,
                        0.8f,
                        0.14f,
                        hip - 0.05f,
                        1.6f,
                        0.13f,
                        hip - 0.03f
                    );
                    Pos(
                        c,
                        HandBackPath,
                        0f,
                        -0.02f,
                        hip - 0.02f,
                        0.8f,
                        -0.01f,
                        hip - 0.04f,
                        1.6f,
                        -0.02f,
                        hip - 0.02f
                    );
                    Scale(c, RunnerBody, 0f, 1.02f, 0.95f, 0.8f, 1.01f, 0.97f, 1.6f, 1.02f, 0.95f);
                    return true;

                default:
                    return false;
            }
        }
    }
}
