using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityCameraFramingTests
    {
        [TestCase(16f / 9)]
        [TestCase(16f / 10)]
        [TestCase(4f / 3)]
        public void OverviewContainsEveryExpandedWorldCorner(float aspect)
        {
            var root = new GameObject("OverviewFramingTest");
            var camera = root.AddComponent<Camera>();
            camera.aspect = aspect;
            camera.fieldOfView = 45;
            var rotation = Quaternion.Euler(55, 40, 0);
            camera.transform.rotation = rotation;
            camera.transform.position = CityDistricts.WorldCenter
                - rotation * Vector3.forward * CityCameraFraming.OverviewDistance(camera, rotation);
            try
            {
                var bounds = CityDistricts.WorldBounds;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner = bounds.center
                                + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                            Vector3 viewport = camera.WorldToViewportPoint(corner);
                            Assert.That(viewport.x, Is.InRange(0f, 1f));
                            Assert.That(viewport.y, Is.InRange(0f, 1f));
                            Assert.That(viewport.z, Is.GreaterThan(0));
                        }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
