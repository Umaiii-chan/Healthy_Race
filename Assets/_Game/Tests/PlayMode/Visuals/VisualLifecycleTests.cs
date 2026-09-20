using System.Collections;
using HealthyRace.Puzzles;
using HealthyRace.Visuals;
using HealthyRace.WellBeing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealthyRace.Tests.Visuals
{
    public sealed class VisualLifecycleTests
    {
        [UnityTest]
        public IEnumerator PlayerBinding_DisableReenableAndRebindStayIsolated()
        {
            var a = new GameObject("P1");
            var b = new GameObject("P2");
            var visual = new GameObject("Visual");
            try
            {
                var source1 = a.AddComponent<WellbeingComponent>();
                var source2 = b.AddComponent<WellbeingComponent>();
                var presenter = visual.AddComponent<PlayerPresentation>();
                presenter.Bind(source1);
                source1.SetValue(25);
                Assert.That(presenter.Appearance, Is.EqualTo(WellbeingAppearance.Critical));
                visual.SetActive(false);
                var before = presenter.PresentationUpdates;
                source1.SetValue(0);
                Assert.That(presenter.PresentationUpdates, Is.EqualTo(before));
                visual.SetActive(true);
                Assert.That(presenter.Appearance, Is.EqualTo(WellbeingAppearance.Salo));
                before = presenter.PresentationUpdates;
                source1.SetValue(75);
                Assert.That(presenter.PresentationUpdates, Is.EqualTo(before + 1));
                presenter.Bind(source2);
                before = presenter.PresentationUpdates;
                source1.SetValue(5);
                Assert.That(presenter.PresentationUpdates, Is.EqualTo(before));
                source2.SetValue(50);
                Assert.That(presenter.Appearance, Is.EqualTo(WellbeingAppearance.Tired));
                Assert.That(source1.CurrentValue, Is.EqualTo(5));
                Assert.That(source2.CurrentValue, Is.EqualTo(50));
                yield return null;
            }
            finally
            {
                Object.Destroy(a);
                Object.Destroy(b);
                Object.Destroy(visual);
            }
        }

        [UnityTest]
        public IEnumerator PressureView_SynchronizesOnReenableWithoutChangingButton()
        {
            var buttonObject = new GameObject("Button");
            var visualObject = new GameObject("Visual");
            try
            {
                var button = buttonObject.AddComponent<PressureButton>();
                var visual = visualObject.AddComponent<PuzzlePresentation>();
                visual.Bind(button);
                button.ForceSetPressed(true);
                Assert.That(visual.State, Is.EqualTo(PuzzleAppearance.Active));
                visual.enabled = false;
                button.ForceSetPressed(false);
                Assert.That(visual.State, Is.EqualTo(PuzzleAppearance.Active));
                visual.enabled = true;
                Assert.That(visual.State, Is.EqualTo(PuzzleAppearance.Inactive));
                Assert.That(button.IsPressed, Is.False);
                yield return null;
            }
            finally
            {
                Object.Destroy(buttonObject);
                Object.Destroy(visualObject);
            }
        }
    }
}
