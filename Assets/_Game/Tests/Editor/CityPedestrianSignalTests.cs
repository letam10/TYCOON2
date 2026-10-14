using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityPedestrianSignalTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root) Object.DestroyImmediate(root);
        }

        [Test]
        public void PedestrianWaitMatchesCurrentWalkPhaseAtSignal()
        {
            root = new GameObject("PedestrianSignals");
            var signals = root.AddComponent<CityTrafficSignalSystem>();
            Vector3 crossing = CityTrafficSignalSystem.Definitions[0].Position;

            Assert.That(signals.TryNearest(crossing, out _, out float distance), Is.True);
            Assert.That(distance, Is.LessThan(.01f));
            bool expectedWait = signals.PhaseAt(crossing, true) != CitySignalPhase.Walk;
            Assert.That(signals.ShouldWaitPedestrian(crossing), Is.EqualTo(expectedWait));
        }

        [Test]
        public void PedestrianOutsideSignalRadiusNeverWaits()
        {
            root = new GameObject("PedestrianSignals");
            var signals = root.AddComponent<CityTrafficSignalSystem>();

            Assert.That(signals.ShouldWaitPedestrian(new Vector3(0, 0, 0)), Is.False);
        }
    }
}
