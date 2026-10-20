using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldPathSurfaceTests
    {
        const double AreaTolerance = .005;

        [TestCase(0f)]
        [TestCase(31f)]
        public void CrossingHasAnalyticAreaAndSinglePriorityOwner(float angle)
        {
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            var high = Request("high", rotation * new Vector3(-10, 0, 0),
                rotation * new Vector3(10, 0, 0), 300);
            var low = Request("low", rotation * new Vector3(0, 0, -10),
                rotation * new Vector3(0, 0, 10), 100);
            WorldPathSurfaceLayout layout = Build(high, low);

            Assert.That(Area(layout, "high"), Is.EqualTo(80).Within(AreaTolerance));
            Assert.That(Area(layout, "low"), Is.EqualTo(64).Within(AreaTolerance));
            AssertOwner(layout, rotation * new Vector3(.31f, 0, .27f), "high");
            AssertOwner(layout, rotation * new Vector3(.31f, 0, 7.27f), "low");
            AssertValid(layout);
        }

        [Test]
        public void ReverseRequestInputKeepsOwnersPieceIdsAndExactCoordinates()
        {
            var first = Request("first", new(-10, 0, 0), new(10, 0, 0), 100);
            var second = Request("second", new(0, 0, -10), new(0, 0, 10), 300);
            var third = Request("third", new(-8, 0, -8), new(8, 0, 8), 200);
            WorldPathSurfaceLayout forward = Build(first, second, third);
            WorldPathSurfaceLayout reverse = Build(third, second, first);

            Assert.That(reverse.Requests.Count, Is.EqualTo(forward.Requests.Count));
            for (int i = 0; i < forward.Requests.Count; i++)
                Assert.That(reverse.Requests[i].Id, Is.EqualTo(forward.Requests[i].Id));
            Assert.That(reverse.Pieces.Count, Is.EqualTo(forward.Pieces.Count));
            for (int i = 0; i < forward.Pieces.Count; i++)
            {
                WorldPathSurfacePiece a = forward.Pieces[i];
                WorldPathSurfacePiece b = reverse.Pieces[i];
                Assert.That(b.Request.Id, Is.EqualTo(a.Request.Id));
                Assert.That(b.Geometry.RouteId, Is.EqualTo(a.Geometry.RouteId));
                Assert.That(b.Geometry.SegmentIndex, Is.EqualTo(a.Geometry.SegmentIndex));
                Assert.That(b.Geometry.Side, Is.EqualTo(a.Geometry.Side));
                Assert.That(b.Geometry.PieceIndex, Is.EqualTo(a.Geometry.PieceIndex));
                CollectionAssert.AreEqual(a.Geometry.Outline, b.Geometry.Outline);
            }
            AssertValid(forward);
        }

        [Test]
        public void EqualPriorityUsesOrdinalIdRatherThanInputOrderOrCulture()
        {
            var lowerCase = Request("a", new(-10, 0, 0), new(10, 0, 0), 200);
            var upperCase = Request("Z", new(0, 0, -10), new(0, 0, 10), 200);
            WorldPathSurfaceLayout layout = Build(lowerCase, upperCase);

            Assert.That(layout.Requests[0].Id, Is.EqualTo("Z"));
            Assert.That(Area(layout, "Z"), Is.EqualTo(80).Within(AreaTolerance));
            Assert.That(Area(layout, "a"), Is.EqualTo(64).Within(AreaTolerance));
            AssertOwner(layout, new(.31f, 0, .27f), "Z");
        }

        [Test]
        public void ProtectedStreetFootprintWinsOverEvenHighestPathPriority()
        {
            var request = Request("dock", new(-10, 0, 0), new(10, 0, 0), int.MaxValue);
            WorldStreetSurface blocker = Rectangle("street", -2, -20, 2, 20, 0);
            WorldPathSurfaceLayout layout = WorldPathSurfaceGeometry.Build(
                new[] { request }, new[] { blocker });

            Assert.That(Area(layout, request.Id), Is.EqualTo(64).Within(AreaTolerance));
            Assert.That(Covered(layout, new(.31f, 0, .27f)), Is.EqualTo(0));
            AssertOwner(layout, new(6.31f, 0, .27f), request.Id);
            Assert.That(layout.Blockers[0], Is.SameAs(blocker));
            AssertValid(layout);
        }

        [Test]
        public void FullyOccludedRequestRemainsInCatalogWithoutGeometry()
        {
            var request = Request("hidden", new(-10, 0, 0), new(10, 0, 0), 100);
            WorldStreetSurface blocker = Rectangle("street", -11, -3, 11, 3, 0);
            WorldPathSurfaceLayout layout = WorldPathSurfaceGeometry.Build(
                new[] { request }, new[] { blocker });

            Assert.That(layout.Requests.Count, Is.EqualTo(1));
            Assert.That(layout.Requests[0], Is.SameAs(request));
            Assert.That(layout.Pieces, Is.Empty);
        }

        [Test]
        public void ClippingOuterBevelRemovesOnlyItsTwoSquareMetres()
        {
            var request = new WorldPathSurfaceRequest("bend",
                new[] { new Vector3(-10, 0, 0), Vector3.zero, new Vector3(0, 0, 10) },
                4, "soil", "#9A754C", .0005f, .0555f, 100);
            WorldStreetSurface blocker = Rectangle("outer-quarter", 0, -2, 2, 0, 0);
            WorldPathSurfaceLayout layout = WorldPathSurfaceGeometry.Build(
                new[] { request }, new[] { blocker });

            // Hai dải 40 m² trừ giao 4 m²; tam giác bevel 2 m² bị cắt vừa đủ.
            Assert.That(Area(layout, request.Id), Is.EqualTo(76).Within(AreaTolerance));
            Assert.That(Covered(layout, new(.31f, 0, -.27f)), Is.EqualTo(0));
            AssertOwner(layout, new(-.31f, 0, -.27f), request.Id);
            AssertOwner(layout, new(.31f, 0, .27f), request.Id);
            AssertValid(layout);
        }

        [Test]
        public void DuplicateIdsAreRejectedRatherThanSilentlyDroppingRequests()
        {
            var first = Request("same", new(-10, 0, 0), new(10, 0, 0), 100);
            var second = Request("same", new(0, 0, -10), new(0, 0, 10), 300);
            Assert.Throws<ArgumentException>(() => Build(first, second));
        }

        [TestCase(.00001f)]
        [TestCase(5f)]
        public void ElevatedRequestKeepsItsFootprintAboveGroundRequestAndBlocker(float height)
        {
            var ground = Request("ground", new(-10, 0, 0), new(10, 0, 0), 400);
            var raised = Request("raised", new(-10, height, 0), new(10, height, 0), 100);
            WorldPathSurfaceLayout unblocked = Build(ground, raised);
            Assert.That(Area(unblocked, ground.Id), Is.EqualTo(80).Within(AreaTolerance));
            Assert.That(Area(unblocked, raised.Id), Is.EqualTo(80).Within(AreaTolerance));

            WorldPathSurfaceLayout blocked = WorldPathSurfaceGeometry.Build(
                new[] { ground, raised }, new[] { Rectangle("ground-block", -11, -3, 11, 3, 0) });
            Assert.That(Area(blocked, ground.Id), Is.EqualTo(0));
            Assert.That(Area(blocked, raised.Id), Is.EqualTo(80).Within(AreaTolerance));
            AssertOwner(blocked, new(.31f, height, .27f), raised.Id);
            AssertValid(blocked);
        }

        [Test]
        public void SubtractionPreservesMetadataAndDoesNotMutateInputs()
        {
            WorldStreetSurface rectangle = Rectangle("original", -10, -2, 10, 2, 0);
            var subject = new WorldStreetSurface(rectangle.RouteId, 7, -1, 99,
                rectangle.Source, rectangle.Outline);
            var blocker = Rectangle("cut", -2, -3, 2, 3, 0);
            var before = new List<Vector3>(subject.Outline);
            IReadOnlyList<WorldStreetSurface> pieces = WorldStreetGeometry.SubtractSurfaces(
                new[] { subject }, new[] { blocker });
            var pieceIds = new HashSet<int>();
            double total = 0;
            foreach (WorldStreetSurface piece in pieces)
            {
                Assert.That(piece.RouteId, Is.EqualTo("original"));
                Assert.That(piece.SegmentIndex, Is.EqualTo(7));
                Assert.That(piece.Side, Is.EqualTo(-1));
                Assert.That(piece.Source.Start, Is.EqualTo(subject.Source.Start));
                Assert.That(piece.Source.End, Is.EqualTo(subject.Source.End));
                Assert.That(piece.Source.Width, Is.EqualTo(subject.Source.Width));
                Assert.That(pieceIds.Add(piece.PieceIndex), Is.True);
                total += SignedArea(piece.Outline);
            }
            Assert.That(total, Is.EqualTo(64).Within(AreaTolerance));
            Assert.That(pieces.Count, Is.GreaterThanOrEqualTo(2));
            CollectionAssert.AreEqual(before, subject.Outline);
            Assert.That(subject.PieceIndex, Is.EqualTo(99));
        }

        [Test]
        public void SubtractionRejectsConcaveExternalOutlines()
        {
            WorldStreetSurface rectangle = Rectangle("concave", -2, -2, 2, 2, 0);
            var concave = new WorldStreetSurface("concave", 0, 0, 0, rectangle.Source,
                new[] { new Vector3(-2, 0, -2), new Vector3(2, 0, -2),
                    new Vector3(2, 0, 0), Vector3.zero, new Vector3(0, 0, 2),
                    new Vector3(-2, 0, 2) });
            Assert.Throws<ArgumentException>(() => WorldStreetGeometry.SubtractSurfaces(
                new[] { concave }, Array.Empty<WorldStreetSurface>()));
            Assert.That(concave.Outline.Count, Is.EqualTo(6));
        }

        [Test]
        public void RequestAndLayoutTakeImmutableSnapshotsOfCallerLists()
        {
            var points = new List<Vector3> { new(-10, 0, 0), new(10, 0, 0) };
            var request = new WorldPathSurfaceRequest("snapshot", points, 4,
                "wood", "#A18B68", .02f, .08f, 400);
            points[0] = new Vector3(999, 999, 999);
            var requests = new List<WorldPathSurfaceRequest> { request };
            WorldPathSurfaceLayout built = WorldPathSurfaceGeometry.Build(requests,
                Array.Empty<WorldStreetSurface>());
            var pieces = new List<WorldPathSurfacePiece>(built.Pieces);
            var blockers = new List<WorldStreetSurface>();
            var snapshot = new WorldPathSurfaceLayout(requests, pieces, blockers);
            requests.Clear();
            pieces.Clear();
            blockers.Add(Rectangle("late", -1, -1, 1, 1, 0));

            Assert.That(request.Points[0], Is.EqualTo(new Vector3(-10, 0, 0)));
            Assert.That(snapshot.Requests.Count, Is.EqualTo(1));
            Assert.That(snapshot.Pieces.Count, Is.GreaterThan(0));
            Assert.That(snapshot.Blockers, Is.Empty);
            Assert.Throws<NotSupportedException>(() => ((IList<Vector3>)request.Points).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<WorldPathSurfaceRequest>)snapshot.Requests).Clear());
            foreach (WorldPathSurfacePiece piece in snapshot.Pieces)
            {
                Assert.That(piece.Request, Is.SameAs(request));
                Assert.That(piece.Request.Surface, Is.EqualTo("wood"));
                Assert.That(piece.Request.Tint, Is.EqualTo("#A18B68"));
                Assert.That(piece.Request.Bottom, Is.EqualTo(.02f));
                Assert.That(piece.Request.Top, Is.EqualTo(.08f));
            }
        }

        [Test]
        public void CurrentCatalogPreservesSampledCoordinatesWidthsAndMaterialPriorities()
        {
            IReadOnlyList<WorldPathSurfaceRequest> requests = WorldPathSurfaceCatalog.BuildDefault();
            var byId = new Dictionary<string, WorldPathSurfaceRequest>(StringComparer.Ordinal);
            foreach (WorldPathSurfaceRequest request in requests)
                byId.Add(request.Id, request);
            int expectedCount = 0;
            foreach (WorldRouteCorridor route in WorldRouteCatalog.BuildDefault())
            {
                if (route.Kind == RouteKind.Rail)
                {
                    Assert.That(byId.ContainsKey(route.Id), Is.False);
                    continue;
                }
                expectedCount++;
                Assert.That(byId.ContainsKey(route.Id), Is.True, route.Id);
                WorldPathSurfaceRequest request = byId[route.Id];
                CollectionAssert.AreEqual(route.SampledNodes, request.Points);
                Assert.That(request.Width, Is.EqualTo(route.Width));
                Assert.That(request.Bottom, Is.EqualTo(.0005f));
                Assert.That(request.Top, Is.EqualTo(.0555f));
                int priority = route.Kind == RouteKind.Dock ? 400
                    : route.Kind == RouteKind.Highway ? 300
                    : route.Kind == RouteKind.FarmPath ? 100 : 200;
                string material = route.Kind == RouteKind.Dock ? "wood"
                    : route.Kind == RouteKind.Highway ? "stone"
                    : route.Kind == RouteKind.FarmPath ? "soil" : "paving";
                Assert.That(request.Priority, Is.EqualTo(priority));
                Assert.That(request.Surface, Is.EqualTo(material));
            }
            foreach (WorldPublicApproach approach in WorldRouteCatalog.BuildPublicApproaches())
            {
                expectedCount++;
                WorldPathSurfaceRequest request = byId[approach.Id];
                CollectionAssert.AreEqual(approach.SampledNodes, request.Points);
                Assert.That(request.Width, Is.EqualTo(approach.Width));
                Assert.That(request.Priority, Is.EqualTo(200));
                Assert.That(request.Surface, Is.EqualTo("paving"));
            }
            Assert.That(requests.Count, Is.EqualTo(expectedCount));
            var landmark = WorldPathSurfaceCatalog.CreateLandmark("bank", 2,
                new[] { Vector3.zero, Vector3.forward * 3 }, 2.5f);
            Assert.That(landmark.Id, Is.EqualTo("landmark:bank:2"));
            Assert.That(landmark.Tint, Is.EqualTo("#C0B8A0"));
            Assert.That(landmark.Bottom, Is.EqualTo(0));
            Assert.That(landmark.Top, Is.EqualTo(.05f));
            Assert.That(landmark.Width, Is.EqualTo(2.5f));
            Assert.That(landmark.Priority, Is.EqualTo(200));
        }

        [Test]
        public void CurrentCatalogHasNoSameHeightOverlapOrStreetIntrusion()
        {
            IReadOnlyList<WorldPathSurfaceRequest> requests = WorldPathSurfaceCatalog.BuildDefault();
            IReadOnlyList<WorldStreetSurface> blockers = WorldPathSurfaceCatalog.BuildStreetBlockers();
            WorldPathSurfaceLayout layout = WorldPathSurfaceGeometry.Build(requests, blockers);
            Assert.That(layout.Requests.Count, Is.EqualTo(requests.Count));
            Assert.That(layout.Pieces.Count, Is.GreaterThan(0));
            Assert.That(layout.Blockers.Count, Is.EqualTo(blockers.Count));
            AssertValid(layout);
        }

        [Test]
        public void RendererSharesExactMeshAndPoseAndReleasesOnlyOwnedMeshes()
        {
            var root = new GameObject("path-surface-test-root");
            var unrelated = new Mesh { name = "unrelated-test-mesh" };
            var owned = new List<Mesh>();
            try
            {
                root.transform.position = new Vector3(17, 3, -11);
                root.transform.rotation = Quaternion.Euler(0, 37, 0);
                var first = Request("first", new(-10, 0, 0), new(10, 0, 0), 300);
                var sameBatch = Request("same-batch", new(-10, 0, 10), new(10, 0, 10), 100);
                var otherBatch = new WorldPathSurfaceRequest("wood",
                    new[] { new Vector3(-10, 0, 20), new Vector3(10, 0, 20) },
                    4, "wood", "#A18B68", .03f, .09f, 400);
                WorldPathSurfaceMesh.Build(root.transform, Build(first, sameBatch, otherBatch));
                MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
                Assert.That(filters.Length, Is.EqualTo(2));
                foreach (MeshFilter filter in filters)
                {
                    Assert.That(filter.name, Does.StartWith("PathSurface_"));
                    MeshCollider collider = filter.GetComponent<MeshCollider>();
                    Assert.That(collider, Is.Not.Null);
                    Assert.That(filter.GetComponent<MeshRenderer>(), Is.Not.Null);
                    Assert.That(filter.sharedMesh, Is.Not.Null);
                    Assert.That(collider.sharedMesh, Is.SameAs(filter.sharedMesh));
                    Assert.That(collider.isTrigger, Is.False);
                    Assert.That(filter.transform.localPosition, Is.EqualTo(Vector3.zero));
                    Assert.That(filter.transform.localRotation, Is.EqualTo(Quaternion.identity));
                    Assert.That(filter.transform.localScale, Is.EqualTo(Vector3.one));
                    bool wood = filter.sharedMesh.bounds.center.z > 15;
                    Assert.That(filter.sharedMesh.bounds.min.y,
                        Is.EqualTo(wood ? .03f : .0005f).Within(.00001f));
                    Assert.That(filter.sharedMesh.bounds.max.y,
                        Is.EqualTo(wood ? .09f : .0555f).Within(.00001f));
                    Assert.That(TopTriangleArea(filter.sharedMesh),
                        Is.EqualTo(wood ? 80 : 160).Within(AreaTolerance));
                    owned.Add(filter.sharedMesh);
                }
                Assert.That(root.GetComponentInChildren<WorldStreetMeshOwner>(), Is.Not.Null);
                UnityEngine.Object.DestroyImmediate(root);
                foreach (Mesh mesh in owned)
                    Assert.That(mesh == null, Is.True, "Owned combined mesh leaked after root destruction.");
                Assert.That(unrelated == null, Is.False);
            }
            finally
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
                foreach (Mesh mesh in owned)
                    if (mesh != null)
                        UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(unrelated);
            }
        }

        static WorldPathSurfaceRequest Request(string id, Vector3 start, Vector3 end, int priority)
        {
            return new WorldPathSurfaceRequest(id, new[] { start, end }, 4,
                "paving", "#C0B8A0", .0005f, .0555f, priority);
        }

        static double TopTriangleArea(Mesh mesh)
        {
            double area = 0;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (cross.y > .000001f && Math.Abs(a.y - b.y) < .000001f
                    && Math.Abs(a.y - c.y) < .000001f)
                    area += cross.y * .5;
            }
            return area;
        }

        static WorldPathSurfaceLayout Build(params WorldPathSurfaceRequest[] requests)
        {
            return WorldPathSurfaceGeometry.Build(requests, Array.Empty<WorldStreetSurface>());
        }

        static WorldStreetSurface Rectangle(string id, float x0, float z0, float x1, float z1, float height)
        {
            var source = new WorldRouteSegment(new(x0, height, (z0 + z1) * .5f),
                new(x1, height, (z0 + z1) * .5f), z1 - z0);
            return new WorldStreetSurface(id, 0, 0, 0, source,
                new[] { new Vector3(x0, height, z0), new Vector3(x1, height, z0),
                    new Vector3(x1, height, z1), new Vector3(x0, height, z1) });
        }

        static double Area(WorldPathSurfaceLayout layout, string requestId)
        {
            double total = 0;
            foreach (WorldPathSurfacePiece piece in layout.Pieces)
                if (piece.Request.Id == requestId)
                    total += SignedArea(piece.Geometry.Outline);
            return total;
        }

        static int Covered(WorldPathSurfaceLayout layout, Vector3 point)
        {
            int count = 0;
            foreach (WorldPathSurfacePiece piece in layout.Pieces)
                if (piece.Geometry.Source.Start.y == point.y && Contains(piece.Geometry.Outline, point))
                    count++;
            return count;
        }

        static void AssertOwner(WorldPathSurfaceLayout layout, Vector3 point, string id)
        {
            Assert.That(Covered(layout, point), Is.EqualTo(1), point.ToString());
            foreach (WorldPathSurfacePiece piece in layout.Pieces)
                if (piece.Geometry.Source.Start.y == point.y && Contains(piece.Geometry.Outline, point))
                    Assert.That(piece.Request.Id, Is.EqualTo(id));
        }

        // Ray crossing độc lập với phép trừ polygon, điểm mẫu tránh biên chung.
        static bool Contains(IReadOnlyList<Vector3> polygon, Vector3 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector3 a = polygon[i];
                Vector3 b = polygon[j];
                if ((a.z > point.z) != (b.z > point.z)
                    && point.x < (double)(b.x - a.x) * (point.z - a.z) / (b.z - a.z) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        static void AssertValid(WorldPathSurfaceLayout layout)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (WorldPathSurfacePiece piece in layout.Pieces)
            {
                WorldStreetSurface geometry = piece.Geometry;
                string id = geometry.RouteId + "/" + geometry.SegmentIndex + "/"
                    + geometry.Side + "/" + geometry.PieceIndex;
                Assert.That(ids.Add(id), Is.True, "Duplicate geometry id " + id);
                Assert.That(geometry.RouteId, Is.EqualTo(piece.Request.Id));
                Assert.That(geometry.Outline.Count, Is.GreaterThanOrEqualTo(3));
                Assert.That(SignedArea(geometry.Outline), Is.GreaterThan(.000001), id);
                for (int i = 0; i < geometry.Outline.Count; i++)
                {
                    Vector3 point = geometry.Outline[i];
                    Assert.That(float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z), Is.True);
                    Assert.That(point.y, Is.EqualTo(geometry.Source.Start.y));
                    Vector3 previous = geometry.Outline[(i + geometry.Outline.Count - 1) % geometry.Outline.Count];
                    Vector3 next = geometry.Outline[(i + 1) % geometry.Outline.Count];
                    Assert.That(Cross(point - previous, next - point), Is.GreaterThan(0), id);
                }
                foreach (WorldStreetSurface blocker in layout.Blockers)
                    if (geometry.Source.Start.y == blocker.Source.Start.y)
                        Assert.That(IntersectionArea(geometry.Outline, blocker.Outline),
                            Is.LessThan(AreaTolerance), id + " enters street " + blocker.RouteId);
            }
            for (int a = 0; a < layout.Pieces.Count; a++)
                for (int b = a + 1; b < layout.Pieces.Count; b++)
                {
                    WorldStreetSurface first = layout.Pieces[a].Geometry;
                    WorldStreetSurface second = layout.Pieces[b].Geometry;
                    if (first.Source.Start.y == second.Source.Start.y)
                        Assert.That(IntersectionArea(first.Outline, second.Outline),
                            Is.LessThan(AreaTolerance), first.RouteId + " overlaps " + second.RouteId);
                }
        }

        struct Point
        {
            public double X;
            public double Z;

            public Point(double x, double z)
            {
                X = x;
                Z = z;
            }
        }

        // Oracle chỉ tính giao lồi trong double; không tái sử dụng Build/Subtract.
        static double IntersectionArea(IReadOnlyList<Vector3> first, IReadOnlyList<Vector3> second)
        {
            float firstMinX = float.PositiveInfinity;
            float firstMaxX = float.NegativeInfinity;
            float firstMinZ = float.PositiveInfinity;
            float firstMaxZ = float.NegativeInfinity;
            foreach (Vector3 point in first)
            {
                firstMinX = Mathf.Min(firstMinX, point.x);
                firstMaxX = Mathf.Max(firstMaxX, point.x);
                firstMinZ = Mathf.Min(firstMinZ, point.z);
                firstMaxZ = Mathf.Max(firstMaxZ, point.z);
            }
            bool allLeft = true;
            bool allRight = true;
            bool allBelow = true;
            bool allAbove = true;
            foreach (Vector3 point in second)
            {
                allLeft &= point.x <= firstMinX;
                allRight &= point.x >= firstMaxX;
                allBelow &= point.z <= firstMinZ;
                allAbove &= point.z >= firstMaxZ;
            }
            if (allLeft || allRight || allBelow || allAbove)
                return 0;
            var points = new List<Point>();
            foreach (Vector3 point in first)
                points.Add(new Point(point.x, point.z));
            for (int edge = 0; edge < second.Count && points.Count > 0; edge++)
            {
                Vector3 a = second[edge];
                Vector3 b = second[(edge + 1) % second.Count];
                var next = new List<Point>();
                Point previous = points[points.Count - 1];
                double previousSide = Side(a, b, previous);
                foreach (Point point in points)
                {
                    double side = Side(a, b, point);
                    if ((side >= 0) != (previousSide >= 0))
                    {
                        double t = previousSide / (previousSide - side);
                        next.Add(new Point(previous.X + (point.X - previous.X) * t,
                            previous.Z + (point.Z - previous.Z) * t));
                    }
                    if (side >= 0)
                        next.Add(point);
                    previous = point;
                    previousSide = side;
                }
                points = next;
            }
            double area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                Point next = points[(i + 1) % points.Count];
                area += points[i].X * next.Z - next.X * points[i].Z;
            }
            return Math.Abs(area * .5);
        }

        static double Side(Vector3 a, Vector3 b, Point point)
        {
            return (b.x - (double)a.x) * (point.Z - a.z) - (b.z - (double)a.z) * (point.X - a.x);
        }

        static double SignedArea(IReadOnlyList<Vector3> points)
        {
            double area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 next = points[(i + 1) % points.Count];
                area += points[i].x * (double)next.z - next.x * (double)points[i].z;
            }
            return area * .5;
        }

        static double Cross(Vector3 a, Vector3 b)
        {
            return a.x * (double)b.z - a.z * (double)b.x;
        }
    }
}
