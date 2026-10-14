using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class DinerApproachTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void CurrentAndLegacyApproachStepsUseGateThenActualTable(int step)
        {
            var root = new GameObject("DinerApproachTest");
            try
            {
                var table = root.AddComponent<TableStation>();
                table.Seat = new Vector3(-66, .02f, -22);
                var diner = root.AddComponent<DinerAgent>();
                diner.Table = table;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(DinerAgent).GetField("approachStep", flags).SetValue(diner, step);
                var point = (Vector3)typeof(DinerAgent).GetMethod("ApproachPoint", flags).Invoke(diner, null);
                Assert.That(point, Is.EqualTo(step == 0 ? TownLayout.Gate : table.Seat));
                Assert.That(CityPedestrianNetwork.Plan(TownLayout.Gate, table.Seat)[^1],
                    Is.EqualTo(table.Seat));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
