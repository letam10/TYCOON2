using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CoastalShoreTests
    {
        [TestCase(27.5f)] [TestCase(60.5f)] [TestCase(93.5f)] [TestCase(126.5f)]
        public void SandAndWaterShareTheCurvedEdgeWithoutGaps(float worldZ)
        {
            var sand = CityCoastWaterMesh.BeachSand(34, 33, worldZ);
            var water = CityCoastWaterMesh.BeachWater(20, 33, worldZ);
            try
            {
                var dry = sand.vertices; var wet = water.vertices;
                int sandStride = Mathf.CeilToInt(34 / 2f) + 1;
                int waterStride = Mathf.CeilToInt(20 / 2f) + 1;
                int rows = Mathf.CeilToInt(33 / 2f) + 1;
                for (int row = 0; row < rows; row++)
                {
                    Vector3 a = dry[row * sandStride + sandStride - 1] + Vector3.left * 3;
                    Vector3 b = wet[row * waterStride] + Vector3.right * 24;
                    Assert.That(dry[row * sandStride].y, Is.EqualTo(.0275f).Within(.0001f));
                    Assert.That(a.x, Is.EqualTo(b.x).Within(.0001f));
                    Assert.That(a.z, Is.EqualTo(b.z).Within(.0001f));
                    Assert.That(a.y, Is.EqualTo(b.y).Within(.0001f));
                }
                foreach (var normal in sand.normals) Assert.That(normal.y, Is.GreaterThan(.99f));
                foreach (var normal in water.normals) Assert.That(normal.y, Is.GreaterThan(.99f));
            }
            finally { Object.DestroyImmediate(sand); Object.DestroyImmediate(water); }
        }
    }
}
