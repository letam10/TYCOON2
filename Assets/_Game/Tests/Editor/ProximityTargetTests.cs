using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class ProximityTargetTests
    {
        GameObject sessionObject;
        GameObject stationObject;

        [SetUp]
        public void SetUp()
        {
            sessionObject = new GameObject("ProximityTargetTestSession");
            var game = sessionObject.AddComponent<GameSession>();
            GameSession.Instance = game;
            game.Economy = new Economy();
        }

        [TearDown]
        public void TearDown()
        {
            if (stationObject) Object.DestroyImmediate(stationObject);
            if (sessionObject) Object.DestroyImmediate(sessionObject);
            GameSession.Instance = null;
        }

        [Test]
        public void OffsetInteractionPointMeasuresFromStationModel()
        {
            stationObject = new GameObject("machine");
            var machine = stationObject.AddComponent<MachineStation>();
            machine.InteractionPoint = new Vector3(0, 0, -2.3f);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "MachineVisual";
            visual.transform.SetParent(stationObject.transform, false);
            visual.transform.localScale = new Vector3(2.65f, 1f, 2.25f);

            var target = new ProximityTarget(machine);

            Assert.That(target.Distance(machine.InteractionPoint), Is.GreaterThan(1f));
            Assert.That(target.Contains(machine.InteractionPoint), Is.True);
        }
    }
}
