using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityStreetNetwork
    {
        public static IEnumerable<Bounds> Bounds()
        {
            foreach (WorldRouteCorridor corridor in WorldRouteCatalog.BuildStreetNetwork())
            {
                foreach (WorldRouteSegment segment in WorldRouteGeometry.Segments(corridor))
                    yield return WorldRouteGeometry.ToBounds(segment);
            }
        }

        public static List<Vector3> Access(string id)
        {
            Vector3 dock = TruckRoutes.Dock(id);
            return id switch
            {
                "storage_farm" => new() { dock, new(-35, 0, 29) },
                "storage_farm_shop" => new() { dock, new(21, 0, -7), new(21, 0, 29) },
                "storage_processing" => new() { dock, new(102, 0, -18), new(102, 0, 29) },
                "storage_supermarket" => new() { dock, new(46, 0, 56), new(46, 0, 29) },
                "storage_bakery" => new() { dock, new(9, 0, 91), new(46, 0, 91), new(46, 0, 29) },
                _ => new() { dock, new(-82, 0, 65), new(-82, 0, 29) }
            };
        }

        public static void Build(Transform world)
        {
            var root = new GameObject("CityStreets").transform;
            root.SetParent(world, false);
            var owner = root.gameObject.AddComponent<WorldStreetMeshOwner>();
            IReadOnlyList<WorldRouteCorridor> routes = WorldRouteCatalog.BuildStreetNetwork();
            WorldStreetLayout layout = WorldStreetGeometry.Build(routes);
            BuildSurfaces(root, owner, layout.Pavements, false);
            BuildSurfaces(root, owner, layout.Sidewalks, true);
            IReadOnlyList<WorldRouteJunction> junctions = WorldRouteGeometry.FindJunctions(routes);
            foreach (WorldRouteCorridor corridor in routes)
            {
                foreach (WorldRouteSegment segment in WorldRouteGeometry.Segments(corridor))
                    BuildLaneMarks(root, segment, junctions);
            }
        }

        static void BuildSurfaces(
            Transform root,
            WorldStreetMeshOwner owner,
            IReadOnlyList<WorldStreetSurface> surfaces,
            bool sidewalk)
        {
            if (surfaces.Count == 0)
                return;
            var pieces = new List<CombineInstance>(surfaces.Count);
            int vertices = 0;
            foreach (WorldStreetSurface surface in surfaces)
            {
                Mesh piece = WorldStreetMesh.CreatePrism(surface,
                    sidewalk ? .045f : .030f, sidewalk ? .085f : .058f);
                vertices += piece.vertexCount;
                pieces.Add(new CombineInstance { mesh = piece, transform = Matrix4x4.identity });
            }

            // Gộp theo vật liệu, rồi dùng cùng mesh cho hình ảnh và va chạm.
            var combined = new Mesh { name = sidewalk ? "StreetSidewalkUnion" : "StreetPavementUnion" };
            if (vertices > 65535)
                combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combined.CombineMeshes(pieces.ToArray(), true, true);
            combined.RecalculateBounds();
            foreach (CombineInstance piece in pieces)
            {
                if (Application.isPlaying)
                    Object.Destroy(piece.mesh);
                else
                    Object.DestroyImmediate(piece.mesh);
            }
            owner.Register(combined);
            var part = new GameObject(sidewalk ? "CitySidewalk_Surface" : "RoadPavement_Surface");
            part.transform.SetParent(root, false);
            part.AddComponent<MeshFilter>().sharedMesh = combined;
            part.AddComponent<MeshRenderer>().sharedMaterial = TownSurfaceMaterials.Get(
                sidewalk ? "paving" : "stone", sidewalk ? "#BEB7A4" : "#666C6C");
            part.AddComponent<MeshCollider>().sharedMesh = combined;
        }

        static void BuildLaneMarks(Transform root, WorldRouteSegment segment,
            IReadOnlyList<WorldRouteJunction> junctions)
        {
            float length = segment.Length;
            for (float at = -length * .5f + 2; at < length * .5f - 1; at += 6)
            {
                Vector3 offset = segment.Forward * at;
                Vector3 center = segment.Center + offset;
                bool crossesJunction = false;
                foreach (WorldRouteJunction junction in junctions)
                {
                    float clearance = junction.Width * .5f + 1.4f;
                    if ((center - junction.Point).sqrMagnitude > clearance * clearance)
                        continue;
                    crossesJunction = true;
                    break;
                }
                if (crossesJunction)
                    continue;
                GameObject mark = Art.Box(
                    "CityLaneMark",
                    segment.Center + offset + Vector3.up * .069f,
                    new(.11f, .009f, 2.8f),
                    "#EEE1B3",
                    root);
                mark.transform.localRotation = segment.Rotation;
            }
        }
    }
}
