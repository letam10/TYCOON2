using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldStreetSurfaceTests
    {
        const float AreaTolerance = .005f;

        [Test]
        public void BeveledPavementFillsOuterCornerWithoutOverlapOrSpike()
        {
            Vector3[] samples = { new(-10, 0, 0), Vector3.zero, new(0, 0, 10) };
            IReadOnlyList<WorldStreetSurface> surfaces =
                WorldStreetGeometry.BuildPavement("bevel", samples, 4);

            // Hai dải 40 m2 trừ 4 m2 chồng trong, cộng tam giác ngoài 2 m2.
            Assert.That(TotalArea(surfaces), Is.EqualTo(78).Within(AreaTolerance));
            Assert.That(CoverCount(surfaces, new(.7f, 0, -.7f)), Is.EqualTo(1));
            Assert.That(CoverCount(surfaces, new(-.7f, 0, .7f)), Is.EqualTo(1));
            Assert.That(CoverCount(surfaces, new(1.6f, 0, -1.6f)), Is.EqualTo(0));
        }

        [Test]
        public void ClosedPavementAlsoJoinsItsLastEdgeToTheFirst()
        {
            Vector3[] samples =
            {
                Vector3.zero, new(10, 0, 0), new(10, 0, 10), new(0, 0, 10), Vector3.zero
            };
            IReadOnlyList<WorldStreetSurface> surfaces =
                WorldStreetGeometry.BuildPavement("closed-bevel", samples, 2);

            Assert.That(TotalArea(surfaces), Is.EqualTo(78).Within(AreaTolerance));
            Assert.That(CoverCount(surfaces, new(-.2f, 0, -.2f)), Is.EqualTo(1));
            Assert.That(CoverCount(surfaces, new(-.8f, 0, -.8f)), Is.EqualTo(0));
        }

        [Test]
        public void StraightStreetPreservesExactPavementAndSidewalkAreas()
        {
            var routes = new[] { Route("straight", new(-10, 0, 0), new(10, 0, 0)) };
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);

            Assert.That(TotalArea(layout.Pavements), Is.EqualTo(80).Within(AreaTolerance));
            Assert.That(TotalArea(layout.Sidewalks), Is.EqualTo(72).Within(AreaTolerance));
            AssertValid(layout, routes);
        }

        [TestCase(0f)]
        [TestCase(31f)]
        [TestCase(90f)]
        public void InteriorCrossingHasAnalyticUnionAreaAtAnyRotation(float angle)
        {
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            var routes = new[]
            {
                Route("across", rotation * new Vector3(-10, 0, 0), rotation * new Vector3(10, 0, 0)),
                Route("through", rotation * new Vector3(0, 0, -10), rotation * new Vector3(0, 0, 10))
            };
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);

            Assert.That(TotalArea(layout.Pavements), Is.EqualTo(144).Within(AreaTolerance));
            AssertValid(layout, routes);
            AssertGridCoverage(layout, routes, -13, 13, .731f);
        }

        [Test]
        public void AngledIntersectionUsesFootprintsRatherThanAxisAlignedBounds()
        {
            const float angle = 37f;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            var routes = new[]
            {
                Route("east-west", new(-20, 0, 0), new(20, 0, 0)),
                Route("oblique", rotation * new Vector3(-20, 0, 0), rotation * new Vector3(20, 0, 0))
            };
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);
            double expectedArea = 320 - 16 / Math.Sin(angle * Math.PI / 180);

            Assert.That(TotalArea(layout.Pavements), Is.EqualTo(expectedArea).Within(AreaTolerance));
            AssertValid(layout, routes);
            AssertGridCoverage(layout, routes, -22, 22, .973f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CollinearContinuationKeepsBothSidewalksContinuous(bool reverseSecond)
        {
            Vector3 start = Vector3.zero;
            Vector3 end = new(10, 0, 0);
            var routes = new[]
            {
                Route("west", new(-10, 0, 0), Vector3.zero),
                Route("east", reverseSecond ? end : start, reverseSecond ? start : end)
            };
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);

            Assert.That(WorldRouteGeometry.FindJunctions(routes), Is.Empty);
            Assert.That(TotalArea(layout.Pavements), Is.EqualTo(80).Within(AreaTolerance));
            Assert.That(TotalArea(layout.Sidewalks), Is.EqualTo(72).Within(AreaTolerance));
            foreach (float x in new[] { -.1f, .1f })
            {
                Assert.That(CoverCount(layout.Pavements, new(x, 0, .137f)), Is.EqualTo(1));
                Assert.That(CoverCount(layout.Sidewalks, new(x, 0, 2.9f)), Is.EqualTo(1));
                Assert.That(CoverCount(layout.Sidewalks, new(x, 0, -2.9f)), Is.EqualTo(1));
            }
            AssertValid(layout, routes);
        }

        [Test]
        public void StraightPolylineIntermediateNodeDoesNotCreateJunction()
        {
            var route = new WorldRouteCorridor("polyline", RouteKind.Highway,
                new[] { new Vector3(-10, 0, 0), Vector3.zero, new Vector3(10, 0, 0) }, 4, true);
            Assert.That(WorldRouteGeometry.FindJunctions(new[] { route }), Is.Empty);
        }

        [TestCase("bend", 76f)]
        [TestCase("tee", 112f)]
        [TestCase("cross", 144f)]
        public void NonCollinearJunctionsRemainAndTheirPavementIsContinuous(string kind, float area)
        {
            Vector3 firstStart = new(-10, 0, 0);
            Vector3 firstEnd = kind == "bend" ? Vector3.zero : new Vector3(10, 0, 0);
            Vector3 secondStart = kind == "cross" ? new Vector3(0, 0, -10) : Vector3.zero;
            var routes = new[]
            {
                Route("first", firstStart, firstEnd),
                Route("second", secondStart, new(0, 0, 10))
            };
            IReadOnlyList<WorldRouteJunction> junctions = WorldRouteGeometry.FindJunctions(routes);
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);

            Assert.That(junctions, Has.Count.EqualTo(1));
            Assert.That(junctions[0].Point.sqrMagnitude, Is.LessThan(.00001f));
            Assert.That(TotalArea(layout.Pavements), Is.EqualTo(area).Within(AreaTolerance));
            AssertValid(layout, routes);
            AssertGridCoverage(layout, routes, -12, 12, .817f);
        }

        [Test]
        public void BentPolylinePreservesItsCornerJunction()
        {
            var route = new WorldRouteCorridor("corner", RouteKind.Highway,
                new[] { new Vector3(-10, 0, 0), Vector3.zero, new Vector3(0, 0, 10) }, 4, true);
            Assert.That(WorldRouteGeometry.FindJunctions(new[] { route }), Has.Count.EqualTo(1));
        }

        [TestCase(0f, 80f, 72f)]
        [TestCase(5f, 160f, 144f)]
        public void CoincidentRoadsUnionOnlyAtTheSameHeight(float secondHeight, float pavementArea, float sidewalkArea)
        {
            var routes = new[]
            {
                Route("ground", new(-10, 0, 0), new(10, 0, 0)),
                Route("other", new(-10, secondHeight, 0), new(10, secondHeight, 0))
            };
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);

            Assert.That(TotalArea(layout.Pavements), Is.EqualTo(pavementArea).Within(AreaTolerance));
            Assert.That(TotalArea(layout.Sidewalks), Is.EqualTo(sidewalkArea).Within(AreaTolerance));
            AssertValid(layout, routes);
            Assert.That(CoverCount(layout.Pavements, new(.137f, secondHeight, .231f)), Is.EqualTo(1));
        }

        [Test]
        public void ElevatedCrossingDoesNotCutGroundPavementOrSidewalks()
        {
            var routes = new[]
            {
                Route("ground", new(-10, 0, 0), new(10, 0, 0)),
                Route("bridge", new(0, 5, -10), new(0, 5, 10))
            };
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);
            Assert.That(TotalArea(layout.Pavements), Is.EqualTo(160).Within(AreaTolerance));
            Assert.That(TotalArea(layout.Sidewalks), Is.EqualTo(144).Within(AreaTolerance));
            AssertValid(layout, routes);
        }

        [Test]
        public void CurrentCatalogHasStableValidNonoverlappingSurfacesAndCoverage()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildStreetNetwork();
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);
            Assert.That(routes, Has.Count.EqualTo(22));
            Assert.That(layout.Pavements.Count, Is.GreaterThanOrEqualTo(22));
            AssertValid(layout, routes);

            foreach (WorldRouteCorridor route in routes)
            {
                Vector3 start = route.Nodes[0];
                Vector3 delta = route.Nodes[1] - start;
                Vector3 normal = new Vector3(-delta.z, 0, delta.x).normalized;
                foreach (float t in new[] { .0137f, .193f, .487f, .713f, .983f })
                {
                    Vector3 center = start + delta * t;
                    AssertPointCoverage(layout, routes, center + normal * .137f);
                    AssertPointCoverage(layout, routes, center + normal * (route.Width * .5f + .713f));
                    AssertPointCoverage(layout, routes, center - normal * (route.Width * .5f + .713f));
                }
            }
        }

        [Test]
        public void RepeatedBuildKeepsPieceIdsAndExactOutlineOrder()
        {
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildStreetNetwork();
            WorldStreetLayout first = WorldStreetGeometry.Build(routes);
            WorldStreetLayout second = WorldStreetGeometry.Build(routes);
            AssertSame(first.Pavements, second.Pavements);
            AssertSame(first.Sidewalks, second.Sidewalks);
        }

        [TestCase(0f)]
        [TestCase(3f)]
        public void PrismHasClosedOutwardFacesExactCapsAndHeightOffsets(float height)
        {
            var outline = new[]
            {
                new Vector3(0, height, 0),
                new Vector3(5, height, 0),
                new Vector3(5, height, 2),
                new Vector3(2, height, 4),
                new Vector3(0, height, 3)
            };
            var surface = new WorldStreetSurface("mesh-test", 0, 0, 0,
                new WorldRouteSegment(new(0, height, 0), new(5, height, 0), 4), outline);
            Mesh mesh = WorldStreetMesh.CreatePrism(surface, .03f, .085f);
            try
            {
                AssertMesh(mesh, outline, height + .03f, height + .085f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void SubtractedRotatedPiecesAlsoProduceClosedPrisms()
        {
            Quaternion rotation = Quaternion.Euler(0, 31, 0);
            var routes = new[]
            {
                Route("main", new(-10, 0, 0), new(10, 0, 0)),
                Route("cut", rotation * new Vector3(0, 0, -10), rotation * new Vector3(0, 0, 10))
            };
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);
            var surfaces = new List<WorldStreetSurface>(layout.Pavements);
            surfaces.AddRange(layout.Sidewalks);
            foreach (WorldStreetSurface surface in surfaces)
            {
                Mesh mesh = WorldStreetMesh.CreatePrism(surface, .03f, .085f);
                try
                {
                    AssertMesh(mesh, surface.Outline, .03f, .085f);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }
        }

        static WorldRouteCorridor Route(string id, Vector3 start, Vector3 end)
        {
            return new WorldRouteCorridor(id, RouteKind.Highway, new[] { start, end }, 4, true);
        }

        static void AssertValid(WorldStreetLayout layout, IReadOnlyList<WorldRouteCorridor> routes)
        {
            var ids = new HashSet<string>();
            var routeIds = new HashSet<string>();
            foreach (WorldRouteCorridor route in routes)
                routeIds.Add(route.Id);
            var all = new List<WorldStreetSurface>(layout.Pavements);
            all.AddRange(layout.Sidewalks);
            foreach (WorldStreetSurface surface in all)
            {
                Assert.That(routeIds.Contains(surface.RouteId), Is.True, surface.RouteId);
                Assert.That(surface.SegmentIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(surface.PieceIndex, Is.GreaterThanOrEqualTo(0));
                string id = surface.RouteId + "/" + surface.SegmentIndex + "/" + surface.Side + "/" + surface.PieceIndex;
                Assert.That(ids.Add(id), Is.True, "Duplicate piece " + id);
                Assert.That(surface.Outline.Count, Is.GreaterThanOrEqualTo(3));
                Assert.That(SignedArea(surface.Outline), Is.GreaterThan(.000001));
                for (int index = 0; index < surface.Outline.Count; index++)
                {
                    Vector3 point = surface.Outline[index];
                    Assert.That(Finite(point), Is.True, id);
                    Assert.That(point.y, Is.EqualTo(surface.Source.Start.y).Within(.00001f));
                    Vector3 previous = surface.Outline[(index + surface.Outline.Count - 1) % surface.Outline.Count];
                    Vector3 next = surface.Outline[(index + 1) % surface.Outline.Count];
                    Assert.That((point - previous).sqrMagnitude, Is.GreaterThan(.00000001f));
                    Assert.That(Cross(point - previous, next - point), Is.GreaterThan(.000001));
                }
            }
            foreach (WorldStreetSurface pavement in layout.Pavements)
                Assert.That(pavement.Side, Is.EqualTo(0));
            foreach (WorldStreetSurface sidewalk in layout.Sidewalks)
                Assert.That(Math.Abs(sidewalk.Side), Is.EqualTo(1));
            AssertNoOverlap(layout.Pavements, layout.Pavements, true);
            AssertNoOverlap(layout.Sidewalks, layout.Sidewalks, true);
            AssertNoOverlap(layout.Pavements, layout.Sidewalks, false);
        }

        static void AssertNoOverlap(IReadOnlyList<WorldStreetSurface> first,
            IReadOnlyList<WorldStreetSurface> second, bool sameList)
        {
            for (int firstIndex = 0; firstIndex < first.Count; firstIndex++)
            {
                for (int secondIndex = sameList ? firstIndex + 1 : 0; secondIndex < second.Count; secondIndex++)
                {
                    if (Math.Abs(first[firstIndex].Source.Start.y - second[secondIndex].Source.Start.y) > .001f)
                        continue;
                    double area = IntersectionArea(first[firstIndex].Outline, second[secondIndex].Outline);
                    Assert.That(area, Is.LessThan(AreaTolerance),
                        first[firstIndex].RouteId + " overlaps " + second[secondIndex].RouteId);
                }
            }
        }

        // Oracle chỉ tính giao hai đa giác lồi; không gọi phép trừ/Build để suy ra đáp án.
        static double IntersectionArea(IReadOnlyList<Vector3> subject, IReadOnlyList<Vector3> clip)
        {
            var points = new List<Vector3>(subject);
            for (int edge = 0; edge < clip.Count && points.Count > 0; edge++)
            {
                Vector3 start = clip[edge];
                Vector3 delta = clip[(edge + 1) % clip.Count] - start;
                var next = new List<Vector3>();
                Vector3 previous = points[points.Count - 1];
                double previousSide = Cross(delta, previous - start);
                foreach (Vector3 point in points)
                {
                    double side = Cross(delta, point - start);
                    if ((side >= 0) != (previousSide >= 0))
                        next.Add(Vector3.LerpUnclamped(previous, point, (float)(previousSide / (previousSide - side))));
                    if (side >= 0)
                        next.Add(point);
                    previous = point;
                    previousSide = side;
                }
                points = next;
            }
            return Math.Abs(SignedArea(points));
        }

        static void AssertGridCoverage(WorldStreetLayout layout, IReadOnlyList<WorldRouteCorridor> routes,
            float minimum, float maximum, float step)
        {
            for (float x = minimum + .137f; x < maximum; x += step)
                for (float z = minimum + .291f; z < maximum; z += step)
                    AssertPointCoverage(layout, routes, new Vector3(x, 0, z));
        }

        static void AssertPointCoverage(WorldStreetLayout layout, IReadOnlyList<WorldRouteCorridor> routes, Vector3 point)
        {
            bool road = false;
            bool walk = false;
            foreach (WorldRouteCorridor route in routes)
            {
                for (int index = 1; index < route.Nodes.Count; index++)
                {
                    Vector3 start = route.Nodes[index - 1];
                    Vector3 delta = route.Nodes[index] - start;
                    if (Math.Abs(point.y - start.y) > .001f)
                        continue;
                    double length = Math.Sqrt(delta.x * delta.x + delta.z * delta.z);
                    double along = ((point.x - start.x) * delta.x + (point.z - start.z) * delta.z) / length;
                    double across = Math.Abs(((point.x - start.x) * -delta.z + (point.z - start.z) * delta.x) / length);
                    if (along < .0001 || along > length - .0001)
                        continue;
                    road |= across < route.Width * .5 - .0001;
                    walk |= across > route.Width * .5 + .0001 && across < route.Width * .5 + 1.8 - .0001;
                }
            }
            Assert.That(CoverCount(layout.Pavements, point) > 0, Is.EqualTo(road), "Road at " + point);
            Assert.That(CoverCount(layout.Sidewalks, point) > 0, Is.EqualTo(walk && !road), "Sidewalk at " + point);
        }

        static int CoverCount(IReadOnlyList<WorldStreetSurface> surfaces, Vector3 point)
        {
            int count = 0;
            foreach (WorldStreetSurface surface in surfaces)
            {
                if (Math.Abs(point.y - surface.Source.Start.y) > .001f)
                    continue;
                bool inside = true;
                for (int index = 0; index < surface.Outline.Count; index++)
                {
                    Vector3 start = surface.Outline[index];
                    Vector3 end = surface.Outline[(index + 1) % surface.Outline.Count];
                    if (Cross(end - start, point - start) < -.00001)
                    {
                        inside = false;
                        break;
                    }
                }
                if (inside)
                    count++;
            }
            return count;
        }

        static void AssertSame(IReadOnlyList<WorldStreetSurface> first, IReadOnlyList<WorldStreetSurface> second)
        {
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(second[index].RouteId, Is.EqualTo(first[index].RouteId));
                Assert.That(second[index].SegmentIndex, Is.EqualTo(first[index].SegmentIndex));
                Assert.That(second[index].Side, Is.EqualTo(first[index].Side));
                Assert.That(second[index].PieceIndex, Is.EqualTo(first[index].PieceIndex));
                CollectionAssert.AreEqual(first[index].Outline, second[index].Outline);
            }
        }

        static void AssertMesh(Mesh mesh, IReadOnlyList<Vector3> outline, float bottom, float top)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uv = mesh.uv;
            int[] triangles = mesh.triangles;
            Assert.That(vertices.Length, Is.GreaterThan(0));
            Assert.That(normals.Length, Is.EqualTo(vertices.Length));
            Assert.That(uv.Length, Is.EqualTo(vertices.Length));
            Assert.That(triangles.Length % 3, Is.EqualTo(0));
            Assert.That(mesh.bounds.min.y, Is.EqualTo(bottom).Within(.00001f));
            Assert.That(mesh.bounds.max.y, Is.EqualTo(top).Within(.00001f));
            var edges = new Dictionary<string, int>();
            double topArea = 0;
            double bottomArea = 0;
            double volume = 0;
            for (int index = 0; index < vertices.Length; index++)
            {
                Assert.That(Finite(vertices[index]), Is.True);
                Assert.That(Finite(normals[index]), Is.True);
                Assert.That(normals[index].magnitude, Is.EqualTo(1).Within(.0001f));
                Assert.That(float.IsNaN(uv[index].x) || float.IsInfinity(uv[index].x), Is.False);
                Assert.That(float.IsNaN(uv[index].y) || float.IsInfinity(uv[index].y), Is.False);
                Vector3 minimum = mesh.bounds.min - Vector3.one * .00001f;
                Vector3 maximum = mesh.bounds.max + Vector3.one * .00001f;
                Assert.That(vertices[index].x, Is.InRange(minimum.x, maximum.x));
                Assert.That(vertices[index].y, Is.InRange(minimum.y, maximum.y));
                Assert.That(vertices[index].z, Is.InRange(minimum.z, maximum.z));
            }
            for (int index = 0; index < triangles.Length; index += 3)
            {
                int ia = triangles[index];
                int ib = triangles[index + 1];
                int ic = triangles[index + 2];
                Assert.That(ia, Is.InRange(0, vertices.Length - 1));
                Assert.That(ib, Is.InRange(0, vertices.Length - 1));
                Assert.That(ic, Is.InRange(0, vertices.Length - 1));
                Vector3 a = vertices[ia];
                Vector3 b = vertices[ib];
                Vector3 c = vertices[ic];
                Vector3 cross = Vector3.Cross(b - a, c - a);
                Assert.That(cross.magnitude, Is.GreaterThan(.000001f));
                Assert.That(Vector3.Dot(cross.normalized, normals[ia]), Is.GreaterThan(.999f));
                if (Math.Abs(a.y - top) < .00001f && Math.Abs(b.y - top) < .00001f && Math.Abs(c.y - top) < .00001f)
                {
                    Assert.That(cross.y, Is.GreaterThan(0));
                    topArea += cross.magnitude * .5;
                }
                else if (Math.Abs(a.y - bottom) < .00001f && Math.Abs(b.y - bottom) < .00001f && Math.Abs(c.y - bottom) < .00001f)
                {
                    Assert.That(cross.y, Is.LessThan(0));
                    bottomArea += cross.magnitude * .5;
                }
                volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6.0;
                AddEdge(edges, a, b);
                AddEdge(edges, b, c);
                AddEdge(edges, c, a);
            }
            double expectedArea = SignedArea(outline);
            Assert.That(topArea, Is.EqualTo(expectedArea).Within(.0001));
            Assert.That(bottomArea, Is.EqualTo(expectedArea).Within(.0001));
            Assert.That(volume, Is.EqualTo(expectedArea * (top - bottom)).Within(.0001));
            foreach (KeyValuePair<string, int> edge in edges)
                Assert.That(edge.Value, Is.EqualTo(2), "Open/nonmanifold geometric edge " + edge.Key);
        }

        static void AddEdge(Dictionary<string, int> edges, Vector3 first, Vector3 second)
        {
            string a = VertexKey(first);
            string b = VertexKey(second);
            string key = string.CompareOrdinal(a, b) < 0 ? a + "/" + b : b + "/" + a;
            edges.TryGetValue(key, out int count);
            edges[key] = count + 1;
        }

        static string VertexKey(Vector3 value)
        {
            return Mathf.RoundToInt(value.x * 100000) + ":" + Mathf.RoundToInt(value.y * 100000)
                + ":" + Mathf.RoundToInt(value.z * 100000);
        }

        static double TotalArea(IReadOnlyList<WorldStreetSurface> surfaces)
        {
            double total = 0;
            foreach (WorldStreetSurface surface in surfaces)
                total += SignedArea(surface.Outline);
            return total;
        }

        static double SignedArea(IReadOnlyList<Vector3> points)
        {
            double area = 0;
            for (int index = 0; index < points.Count; index++)
            {
                Vector3 next = points[(index + 1) % points.Count];
                area += (double)points[index].x * next.z - (double)next.x * points[index].z;
            }
            return area * .5;
        }

        static double Cross(Vector3 first, Vector3 second)
        {
            return (double)first.x * second.z - (double)first.z * second.x;
        }

        static bool Finite(Vector3 point)
        {
            return !float.IsNaN(point.x) && !float.IsInfinity(point.x)
                && !float.IsNaN(point.y) && !float.IsInfinity(point.y)
                && !float.IsNaN(point.z) && !float.IsInfinity(point.z);
        }
    }
}
