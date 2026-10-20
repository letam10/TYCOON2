using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static class WorldStreetMesh
    {
        public static Mesh CreatePrism(WorldStreetSurface surface, float bottom, float top)
        {
            if (surface == null)
                throw new ArgumentNullException(nameof(surface));
            if (float.IsNaN(bottom) || float.IsInfinity(bottom) ||
                float.IsNaN(top) || float.IsInfinity(top) || top <= bottom)
                throw new ArgumentOutOfRangeException(nameof(top), "Street prism offsets require finite bottom < top.");

            var outline = surface.Outline;
            var count = outline.Count;
            if (count < 3)
                throw new ArgumentException("Street prism requires a convex polygon with at least three points.", nameof(surface));

            var vertices = new List<Vector3>(count * 6);
            var uv = new List<Vector2>(count * 6);
            var triangles = new List<int>((count - 2) * 6 + count * 6);

            for (var i = 0; i < count; i++)
            {
                var point = outline[i];
                vertices.Add(point + Vector3.up * top);
                uv.Add(new Vector2(point.x, point.z));
            }
            for (var i = 0; i < count; i++)
            {
                var point = outline[i];
                vertices.Add(point + Vector3.up * bottom);
                uv.Add(new Vector2(point.x, point.z));
            }

            // Biên CCW trong XZ cho pháp tuyến xuống; đảo nắp trên để hướng lên.
            for (var i = 1; i < count - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i + 1);
                triangles.Add(i);
                triangles.Add(count);
                triangles.Add(count + i);
                triangles.Add(count + i + 1);
            }

            var perimeter = 0f;
            for (var i = 0; i < count; i++)
            {
                var start = outline[i];
                var end = outline[(i + 1) % count];
                var length = Vector3.Distance(start, end);
                var first = vertices.Count;

                // Tách đỉnh thành khỏi nắp để mép vỉa hè không bị làm trơn pháp tuyến.
                vertices.Add(start + Vector3.up * bottom);
                vertices.Add(start + Vector3.up * top);
                vertices.Add(end + Vector3.up * top);
                vertices.Add(end + Vector3.up * bottom);
                uv.Add(new Vector2(perimeter, start.y + bottom));
                uv.Add(new Vector2(perimeter, start.y + top));
                uv.Add(new Vector2(perimeter + length, end.y + top));
                uv.Add(new Vector2(perimeter + length, end.y + bottom));

                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 3);
                perimeter += length;
            }

            var mesh = new Mesh
            {
                name = $"StreetSurface_{surface.RouteId}_{surface.SegmentIndex}_{surface.Side}_{surface.PieceIndex}"
            };
            if (vertices.Count > ushort.MaxValue)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    [ExecuteAlways]
    public sealed class WorldStreetMeshOwner : MonoBehaviour
    {
        readonly HashSet<Mesh> meshes = new HashSet<Mesh>();

        public void Register(Mesh mesh)
        {
            if (mesh != null)
                meshes.Add(mesh);
        }

        void OnDestroy()
        {
            foreach (var mesh in meshes)
            {
                if (mesh == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(mesh);
                else
                    DestroyImmediate(mesh);
            }
            meshes.Clear();
        }
    }
}
