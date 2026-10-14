using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CarryContainerGeometry
    {
        static readonly Dictionary<string, Mesh> meshes = new();
        static Mesh cube;

        public static Mesh Get(string id, CarryContainerProfile profile)
        {
            if (meshes.TryGetValue(id, out var mesh) && mesh) return mesh;
            if (!cube)
            {
                var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube = source.GetComponent<MeshFilter>().sharedMesh;
                source.SetActive(false);
                if (Application.isPlaying) Object.Destroy(source);
                else Object.DestroyImmediate(source);
            }
            var parts = new List<CombineInstance>();
            float floor = CarryContainerProfile.Floor;
            float wall = CarryContainerProfile.Wall;
            float width = profile.Width;
            float depth = profile.Depth;
            float divider = id == "egg" ? .035f : .045f;
            Add(parts, new Vector3(0, floor * .5f, 0), new Vector3(width, floor, depth));

            // Sáu ô rỗng thật; vách thấp giữ nhìn rõ từng món, cột góc đỡ tầng kế tiếp.
            Add(parts, new Vector3(0, floor + divider * .5f, 0),
                new Vector3(wall, divider, depth - wall * 2));
            for (int side = -1; side <= 1; side += 2)
            {
                Add(parts, new Vector3(0, floor + divider * .5f, side * profile.CellZ * .5f),
                    new Vector3(width - wall * 2, divider, wall));
                Add(parts, new Vector3(side * (width - wall) * .5f, floor + divider * .5f, 0),
                    new Vector3(wall, divider, depth - wall * 2));
                Add(parts, new Vector3(0, floor + divider * .5f, side * (depth - wall) * .5f),
                    new Vector3(width - wall * 2, divider, wall));
                for (int end = -1; end <= 1; end += 2)
                {
                    float height = profile.LayerHeight - floor;
                    Add(parts, new Vector3(side * (width - wall) * .5f, floor + height * .5f,
                        end * (depth - wall) * .5f), new Vector3(wall, height, wall));
                }
            }
            mesh = new Mesh { name = "CarryContainerMesh_" + id };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            meshes[id] = mesh;
            return mesh;
        }

        public static Material Material(string id) =>
            TownSurfaceMaterials.Get("wood", id == "egg" ? "#B9A17C" : "#927052");

        static void Add(List<CombineInstance> parts, Vector3 position, Vector3 size)
        {
            parts.Add(new CombineInstance
            {
                mesh = cube,
                transform = Matrix4x4.TRS(position, Quaternion.identity, size)
            });
        }
    }
}
