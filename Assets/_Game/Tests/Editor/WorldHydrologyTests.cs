using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldHydrologyTests
    {
        [Test]
        public void RelocatedIndustryIsDryAndMarinaConnectsToTheCoast()
        {
            Assert.That(WorldExpansionCatalog.Validate(out string failure), Is.True, failure);
            WorldExpansionPoi industry = WorldExpansionCatalog.BuildDefault().Single(p => p.Id == "IndustrialYard");
            WorldExpansionPoi marina = WorldExpansionCatalog.BuildDefault().Single(p => p.Id == "EastMarina");
            Assert.That(WorldHydrologyLayout.CrossesWater(industry.FootprintBounds), Is.False);
            Assert.That(WorldHydrologyLayout.IsWater(CoastalTownLayout.IndustrialVisit), Is.False);
            Assert.That(WorldHydrologyLayout.IsWater(CoastalTownLayout.MarinaEntry), Is.False);
            Assert.That(WorldHydrologyLayout.IsOcean(marina.Center + Vector3.right * 60), Is.True);
            Assert.That(CityMapLocations.Points.Single(p => p.id == "industrial_yard").position.x,
                Is.EqualTo(CoastalTownLayout.IndustrialVisit.x));
            Assert.That(CityMapLocations.Points.Single(p => p.id == "east_marina").position.z,
                Is.EqualTo(CoastalTownLayout.MarinaEntry.z));
            Assert.That(WorldRouteCatalog.TryGet("expansion-industrial-approach", out var route), Is.True);
            Assert.That(route.Nodes[route.Nodes.Count - 1], Is.EqualTo(CoastalTownLayout.IndustrialVisit));
        }

        [Test]
        public void RiverIsContinuousFromOutsideTheMapToTheOceanWithoutSkippedSections()
        {
            var river = WorldHydrologyLayout.River;
            Assert.That(river[0].Center.z, Is.GreaterThan(CityDistricts.WorldBounds.max.z));
            for (int index = 1; index < river.Count; index++)
            {
                Assert.That(Vector3.Distance(river[index].Center, river[index - 1].Center),
                    Is.InRange(.001f, 3.1f));
                Assert.That(river[index].HalfWidth, Is.InRange(4f, 12f));
                Assert.That(river[index].Center.y, Is.EqualTo(CityWaterSurface.WaterBaseline));
            }
            RiverSection mouth = river[river.Count - 1];
            Assert.That(mouth.Left.x, Is.EqualTo(WorldHydrologyLayout.CoastX(mouth.Left.z)).Within(.001f));
            Assert.That(mouth.Right.x, Is.EqualTo(WorldHydrologyLayout.CoastX(mouth.Right.z)).Within(.001f));
            Assert.That(WorldHydrologyLayout.IsOcean(mouth.Center + Vector3.right), Is.True);
        }

        [Test]
        public void OceanMeshActuallyCoversTheFormerGrassStripAndRiverMeshHasNoDegenerateTriangles()
        {
            Mesh ocean = WorldHydrologyMeshes.Ocean(CityDistricts.WorldBounds);
            Mesh river = WorldHydrologyMeshes.RiverWater();
            try
            {
                foreach (Vector2 point in new[] { new Vector2(350, 77), new(500, 300), new(400, -215) })
                    Assert.That(Covers(ocean, point), Is.True, "Missing actual water triangle at " + point);
                for (float z = WorldHydrologyLayout.Mouth.z - 8; z <= WorldHydrologyLayout.Mouth.z + 8; z++)
                    Assert.That(Covers(river, new Vector2(WorldHydrologyLayout.CoastX(z) - .05f, z)), Is.True,
                        "River water must meet the curved ocean edge across the whole estuary: " + z);
                foreach (Mesh mesh in new[] { ocean, river })
                {
                    Vector3[] vertices = mesh.vertices;
                    int[] triangles = mesh.triangles;
                    for (int index = 0; index < triangles.Length; index += 3)
                    {
                        Vector3 cross = Vector3.Cross(vertices[triangles[index + 1]] - vertices[triangles[index]],
                            vertices[triangles[index + 2]] - vertices[triangles[index]]);
                        Assert.That(float.IsFinite(cross.sqrMagnitude) && cross.sqrMagnitude > .00000001f, Is.True);
                        Assert.That(cross.y, Is.GreaterThan(0));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(ocean);
                Object.DestroyImmediate(river);
            }
        }

        [Test]
        public void UnifiedSandStaysOnLandAtTheEstuaryAndPreservesTheDryBeach()
        {
            Mesh sand = WorldHydrologyMeshes.CoastBank(CityDistricts.WorldBounds);
            try
            {
                foreach (Vector2 point in new[] { new Vector2(239, 143), new(243, 146), new(245, 151) })
                {
                    Assert.That(WorldHydrologyLayout.IsWater(new Vector3(point.x, 0, point.y), .25f), Is.True);
                    Assert.That(Covers(sand, point), Is.False, "Sand must not protrude into the estuary: " + point);
                }
                Assert.That(Covers(sand, new Vector2(230, 77)), Is.True, "The dry beach remains a real sand surface.");
            }
            finally
            {
                Object.DestroyImmediate(sand);
            }
        }

        static bool Covers(Mesh mesh, Vector2 point)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int index = 0; index < triangles.Length; index += 3)
            {
                Vector3 av = vertices[triangles[index]];
                Vector3 bv = vertices[triangles[index + 1]];
                Vector3 cv = vertices[triangles[index + 2]];
                Vector2 a = new(av.x, av.z);
                Vector2 b = new(bv.x, bv.z);
                Vector2 c = new(cv.x, cv.z);
                float first = Cross(b - a, point - a);
                float second = Cross(c - b, point - b);
                float third = Cross(a - c, point - c);
                if (first >= 0 && second >= 0 && third >= 0 || first <= 0 && second <= 0 && third <= 0)
                    return true;
            }
            return false;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
