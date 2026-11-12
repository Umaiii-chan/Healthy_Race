using System.Collections.Generic;
using System.Linq;
using HealthyRace.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HealthyRace.Tests.EditMode.Visuals
{
    /// <summary>
    /// Clipes de Zuri e Tom: os 12 estados do rig antigo mais pico e queda, com os mesmos
    /// números de ação; membros dentro do alcance; rosto livre nas poses de braços para cima.
    /// </summary>
    public sealed class RunnerClipTests
    {
        private const string PrefabFolder = "Assets/_Game/Prefabs/Visual/";
        private const string AnimationFolder = "Assets/_Game/Animations/Visual/Runner/";
        private const int Samples = 12;

        private static readonly string[] States =
        {
            "Idle",
            "Move",
            "Jump",
            "Fall",
            "Land",
            "Push",
            "Interact",
            "Eat",
            "Fatigue",
            "Recover",
            "Victory",
            "Defeat",
            "Rush",
            "Crash",
        };

        private static readonly string[] Looping =
        {
            "Idle",
            "Move",
            "Fall",
            "Push",
            "Fatigue",
            "Victory",
            "Defeat",
            "Rush",
            "Crash",
        };

        private static readonly string[] Names = { "Zuri", "Tom" };

        private static IEnumerable<object[]> EveryClip() =>
            from name in Names
            from state in States
            select new object[] { name, state };

        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
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

        private static AnimationClip Clip(string name, string state) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                AnimationFolder + "AN_RUN_" + name + "_" + state + ".anim"
            );

        private RunnerRig Spawn(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabFolder + "PF_VIS_Runner_" + name + ".prefab"
            );
            Assert.That(prefab, Is.Not.Null, "rode 'Build runner rig (Zuri e Tom)'");
            _instance = Object.Instantiate(prefab);
            return _instance.GetComponent<RunnerRig>();
        }

        [TestCaseSource(nameof(Names))]
        public void Controller_HasTheTwelveOldStates_PlusRushAndCrash(string name)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                AnimationFolder + "AC_VIS_Runner_" + name + ".controller"
            );
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.parameters.Select(p => p.name), Is.EqualTo(new[] { "Action" }));

            var machine = controller.layers[0].stateMachine;
            Assert.That(machine.states.Select(s => s.state.name), Is.EquivalentTo(States));
            Assert.That(machine.defaultState.name, Is.EqualTo("Idle"));
            foreach (var child in machine.states)
            {
                Assert.That(
                    child.state.motion,
                    Is.SameAs(Clip(name, child.state.name)),
                    child.state.name
                );
            }

            // cada estado é alcançado pelo número de ação: os 12 do rig antigo, na mesma
            // ordem de VisualAction, e depois pico (12) e queda (13)
            foreach (var transition in machine.anyStateTransitions)
            {
                var index = (int)transition.conditions.Single().threshold;
                Assert.That(transition.destinationState.name, Is.EqualTo(States[index]));
            }

            Assert.That(States[(int)VisualAction.Move], Is.EqualTo("Move"));
            Assert.That(States[(int)VisualAction.Defeat], Is.EqualTo("Defeat"));
            Assert.That(States[RunnerRig.RushAction], Is.EqualTo(RunnerRig.RushState));
            Assert.That(States[RunnerRig.CrashAction], Is.EqualTo(RunnerRig.CrashState));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabFolder + "PF_VIS_Runner_" + name + ".prefab"
            );
            Assert.That(
                prefab.GetComponent<Animator>().runtimeAnimatorController,
                Is.SameAs(controller)
            );
        }

        [TestCaseSource(nameof(EveryClip))]
        public void Clip_KeepsEveryLimbWithinReach(string name, string state)
        {
            var clip = Clip(name, state);
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.length, Is.GreaterThan(0.1f));
            Assert.That(clip.isLooping, Is.EqualTo(Looping.Contains(state)));
            Assert.That(clip.empty, Is.False);

            var rig = Spawn(name);
            for (var i = 0; i <= Samples; i++)
            {
                clip.SampleAnimation(_instance, clip.length * i / Samples);
                foreach (var limb in rig.Limbs)
                {
                    var reach = Vector2.Distance(limb.Anchor.position, limb.Target.position);
                    // membro de mangueira pode esticar um pouco, não virar um risco comprido
                    Assert.That(
                        reach,
                        Is.LessThan(limb.Length * 1.15f),
                        $"{limb.name} esticado demais no quadro {i}"
                    );
                    Assert.That(
                        reach,
                        Is.GreaterThan(0.04f),
                        $"{limb.name} colapsado no quadro {i}"
                    );
                }
            }
        }

        [TestCase("Zuri", 0.68f)]
        [TestCase("Tom", 0.52f)]
        public void Move_HasItsOwnStride_AndFeetStayAboveTheGround(string name, float period)
        {
            var clip = Clip(name, "Move");
            Assert.That(clip.length, Is.EqualTo(period).Within(0.001f));
            // pico é mais rápido que a corrida; queda, mais lenta
            Assert.That(Clip(name, "Rush").length, Is.LessThan(period));
            Assert.That(Clip(name, "Crash").length, Is.GreaterThan(period));

            var rig = Spawn(name);
            var feet = _instance.transform.Find("Rig").position.y;
            var lowest = float.MaxValue;
            var highest = float.MinValue;
            var spread = 0f;
            for (var i = 0; i <= Samples * 2; i++)
            {
                clip.SampleAnimation(_instance, clip.length * i / (Samples * 2));
                var back = rig.Targets[1].position;
                var front = rig.Targets[2].position;
                lowest = Mathf.Min(lowest, back.y - feet, front.y - feet);
                highest = Mathf.Max(highest, back.y - feet, front.y - feet);
                spread = Mathf.Max(spread, Mathf.Abs(front.x - back.x));
            }

            // o tornozelo em repouso fica a 0,18 do chão (altura do tênis)
            Assert.That(lowest, Is.GreaterThan(0.18f - 0.035f), "o tênis afunda no chão");
            Assert.That(highest, Is.GreaterThan(0.18f + 0.12f), "o pé não sai do chão");
            Assert.That(spread, Is.GreaterThan(0.25f), "a passada não abre");
        }

        [TestCase("Zuri", "Jump")]
        [TestCase("Zuri", "Fall")]
        [TestCase("Zuri", "Recover")]
        [TestCase("Zuri", "Victory")]
        [TestCase("Tom", "Jump")]
        [TestCase("Tom", "Fall")]
        [TestCase("Tom", "Recover")]
        [TestCase("Tom", "Victory")]
        public void ArmsUp_NeverCoverTheFace(string name, string state)
        {
            var clip = Clip(name, state);
            var rig = Spawn(name);
            var head = _instance.transform.Find("Rig/Body/Torso/Head/HeadSprite");
            var frontHand = rig.Targets[3];
            for (var i = 0; i <= Samples; i++)
            {
                clip.SampleAnimation(_instance, clip.length * i / Samples);
                // raio da cabeça 0,21 mais meia mão
                Assert.That(
                    Vector2.Distance(frontHand.position, head.position),
                    Is.GreaterThan(0.26f),
                    $"a mão da frente cobre o rosto no quadro {i}"
                );
            }
        }

        [TestCaseSource(nameof(Names))]
        public void Clips_DeformOnlyTheRoot_NeverTheBodyParts(string name)
        {
            // linguagem aprovada: squash e stretch só em Rig/Body (e o "nhac" da cabeça ao comer)
            foreach (var state in States)
            {
                var scaled = AnimationUtility
                    .GetCurveBindings(Clip(name, state))
                    .Where(b => b.propertyName.StartsWith("m_LocalScale"))
                    .Select(b => b.path)
                    .Distinct()
                    .ToArray();
                var allowed =
                    state == "Eat"
                        ? new[] { "Rig/Body", "Rig/Body/Torso/Head" }
                        : new[] { "Rig/Body" };
                Assert.That(scaled, Is.SubsetOf(allowed), state);
            }
        }
    }
}
