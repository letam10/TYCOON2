using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class CarrotShapeTests
    {
        [Test]
        public void RootHasClosedPointAndLeavesGrowOnlyAtBroadShoulder()
        {
            var root = new GameObject("CarrotShapeTest");
            try
            {
                typeof(HandItemModels).GetMethod("Carrot", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { root.transform });
                var body = root.transform.Find("Tapered carrot");
                var vertices = body.GetComponent<MeshFilter>().sharedMesh.vertices;
                float low = vertices.Min(x => x.y);
                float high = vertices.Max(x => x.y);
                var point = vertices.Where(x => Mathf.Abs(x.y - low) < .00001f).ToArray();
                Assert.That(point.All(x => new Vector2(x.x, x.z).magnitude < .00001f), Is.True);
                float tipRadius = Radius(vertices, Mathf.Lerp(low, high, .14f));
                float shoulderRadius = Radius(vertices, Mathf.Lerp(low, high, .89f));
                Assert.That(shoulderRadius, Is.GreaterThan(tipRadius * 5));
                float shoulder = body.localPosition.y + body.localScale.y * (.85f - .5f);
                int leaves = 0;
                foreach (Transform child in root.transform)
                {
                    if (child.name != "Carrot leaf")
                        continue;
                    leaves++;
                    foreach (var vertex in child.GetComponent<MeshFilter>().sharedMesh.vertices)
                        Assert.That(child.TransformPoint(vertex).y, Is.GreaterThan(shoulder));
                }
                Assert.That(leaves, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(1)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(24)]
        [TestCase(48)]
        public void CarryIsTenPercentSmallerWithUniformDirectionAndAlignedTwoByThreeLayers(int count)
        {
            var root = new GameObject("CarrotGridTest");
            try
            {
                var stack = root.AddComponent<InventoryStack>();
                stack.Inventory = new Inventory(48, true);
                stack.Inventory.TryAdd("carrot", count);
                stack.Pool = new ItemPool(root.transform);
                stack.HeldLayout = true;
                stack.Refresh();
                var models = root.GetComponentsInChildren<MeshRenderer>();
                Assert.That(models.Length, Is.EqualTo(count));
                var layout = CarryItemLayout.For("carrot");
                Assert.That(layout.Columns, Is.EqualTo(2));
                Assert.That(layout.Rows, Is.EqualTo(3));
                Assert.That(layout.Scale / .84f, Is.EqualTo(.9f).Within(.00001f));
                var direction = layout.Rotation * Vector3.up;
                Assert.That(Vector3.Dot(direction, Vector3.left), Is.GreaterThan(.999f));
                foreach (var model in models)
                {
                    Assert.That(Quaternion.Angle(model.transform.localRotation, layout.Rotation),
                        Is.LessThan(.001f));
                    Assert.That(model.transform.localScale.x, Is.EqualTo(layout.Scale));
                }
                for (int i = 6; i < count; i++)
                {
                    var above = models[i].transform.localPosition;
                    var below = models[i - 6].transform.localPosition;
                    Assert.That(above.x, Is.EqualTo(below.x).Within(.00001f));
                    Assert.That(above.z, Is.EqualTo(below.z).Within(.00001f));
                    Assert.That(above.y, Is.GreaterThan(below.y));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static float Radius(Vector3[] vertices, float height)
        {
            return vertices.Where(x => Mathf.Abs(x.y - height) < .00001f)
                .Max(x => new Vector2(x.x, x.z).magnitude);
        }
    }
}
