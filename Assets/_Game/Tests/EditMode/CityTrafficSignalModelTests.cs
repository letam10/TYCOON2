using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class CityTrafficSignalModelTests
    {
        [Test]
        public void SignalHasAnExtendedOverheadArmForTheRoadLane()
        {
            var root = new GameObject("TrafficSignalModelTest");
            try
            {
                var geometry = new CityStaticGeometry(root.transform);
                CityLeisureModels.Signal(geometry, Vector3.zero);
                geometry.Finish();

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = renderers[0].bounds;
                for (int index = 1; index < renderers.Length; index++)
                    bounds.Encapsulate(renderers[index].bounds);

                Assert.That(bounds.min.z, Is.LessThan(-2f));
                Assert.That(bounds.max.y, Is.GreaterThan(4.6f));
                Assert.That(root.GetComponentInChildren<BoxCollider>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
