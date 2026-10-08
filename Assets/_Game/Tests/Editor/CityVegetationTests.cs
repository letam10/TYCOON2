using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CityVegetationTests
    {
        [TestCase(0, 4.9f)]
        [TestCase(1, 5.8f)]
        [TestCase(2, 5.2f)]
        public void TreeSpeciesHaveDistinctHeightWithTrunkAndRootColliders(int species, float minimumHeight)
        {
            var root = new GameObject("VegetationTest");
            try
            {
                var geometry = new CityStaticGeometry(root.transform);
                CityVegetation.Tree(geometry, Vector3.zero, 1, species);
                geometry.Finish();
                var colliders = root.GetComponentsInChildren<BoxCollider>();
                Assert.That(colliders, Has.Length.EqualTo(2));
                var trunk = root.transform.Find("TreeTrunkCollision").GetComponent<BoxCollider>();
                Assert.That(trunk.size.x, Is.InRange(.29f, .41f));
                Assert.That(trunk.size.z, Is.EqualTo(trunk.size.x));
                Assert.That(trunk.size.y, Is.GreaterThan(3));
                Bounds total = new(Vector3.zero, Vector3.zero);
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    total.Encapsulate(filter.sharedMesh.bounds);
                    var renderer = filter.GetComponent<MeshRenderer>();
                    Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("TYCOON/Town Surface"));
                    Assert.That(renderer.gameObject.isStatic, Is.True);
                }
                Assert.That(total.max.y, Is.GreaterThan(minimumHeight));
                Assert.That(total.size.x, Is.LessThan(4.6f), "Canopy must stay inside reserved orchard width.");
                Assert.That(total.size.z, Is.LessThan(4.5f), "Canopy must stay inside reserved orchard depth.");
                Assert.That(total.min.y, Is.GreaterThan(-.05f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BenchPlanterAndLampHaveMatchingSolidColliders()
        {
            var root = new GameObject("PropColliderTest");
            try
            {
                var geometry = new CityStaticGeometry(root.transform);
                CityProps.Bench(geometry, Vector3.zero);
                CityProps.Planter(geometry, Vector3.right * 5, 2.5f);
                CityProps.Lamp(geometry, Vector3.right * 10);
                geometry.Finish();
                AssertSize("BenchCollision", new Vector3(2.5f, 1.18f, .8f));
                AssertSize("PlanterCollision", new Vector3(2.62f, .66f, 1.12f));
                AssertSize("LampBaseCollision", new Vector3(.5f, .3f, .5f));
                AssertSize("LampPostCollision", new Vector3(.2f, 4.6f, .2f));
                void AssertSize(string name, Vector3 size)
                {
                    var collider = root.transform.Find(name).GetComponent<BoxCollider>();
                    Assert.That(Vector3.Distance(collider.size, size), Is.LessThan(.001f));
                    Assert.That(collider.isTrigger, Is.False);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
