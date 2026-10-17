using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class Stage4RTrafficDiagnosticsTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void VehicleStopDecisionMatchesEveryKnownSignalPhase()
        {
            root = new GameObject("Stage4RSignalDiagnostics");
            CityTrafficSignalSystem signals = root.AddComponent<CityTrafficSignalSystem>();

            for (int index = 0; index < CityTrafficSignalSystem.Definitions.Count; index++)
            {
                CityTrafficSignalDefinition definition = CityTrafficSignalSystem.Definitions[index];
                CitySignalPhase phase = signals.PhaseAt(definition.Position, false);
                bool expectedStop = phase != CitySignalPhase.Green;

                Assert.That(signals.ShouldStopVehicle(definition.Position), Is.EqualTo(expectedStop),
                    definition.Id + " must stop during " + phase);
                Assert.That(signals.PhaseAt(definition.Position, true), Is.Not.EqualTo(CitySignalPhase.Amber));
            }

            Assert.That(signals.PhaseAt(new Vector3(0, 0, 0), false), Is.EqualTo(CitySignalPhase.Green));
            Assert.That(signals.ShouldStopVehicle(new Vector3(0, 0, 0)), Is.False);
        }

        [Test]
        public void MovingObstacleOutOfVehicleCorridorClearsBlockedDiagnosticContract()
        {
            Vector3 vehicle = new Vector3(-20, 0, -20);
            Vector3 forward = Vector3.forward;
            Vector3 obstacle = vehicle + forward * 3f;

            Assert.That(CityTrafficSafety.ActorAhead(vehicle, forward, .1f, obstacle), Is.True);

            obstacle = vehicle - forward * 1f;
            Assert.That(CityTrafficSafety.ActorAhead(vehicle, forward, .1f, obstacle), Is.False);

            root = new GameObject("Stage4RTrafficDiagnostics");
            CityTraffic traffic = root.AddComponent<CityTraffic>();
            Assert.That(traffic.StuckVehicleCount, Is.Zero);
            Assert.That(traffic.DeadlockCount, Is.Zero);
            Assert.That(traffic.LastDiagnostic, Is.Null);
        }

        [Test]
        public void CornerReservationChoosesOneWinnerWithoutReciprocalGridlock()
        {
            Vector3 outer = new Vector3(-106.55f, 0, -33f);
            Vector3 inner = new Vector3(-93f, 0, -43.45f);

            bool outerBlocked = CityTrafficSafety.VehicleAhead(
                outer, Vector3.back, .1f, inner, Vector3.left);
            bool innerBlocked = CityTrafficSafety.VehicleAhead(
                inner, Vector3.left, .1f, outer, Vector3.back);

            Assert.That(outerBlocked ^ innerBlocked, Is.True);
        }
    }
}
