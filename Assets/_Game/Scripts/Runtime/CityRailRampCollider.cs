using UnityEngine;

namespace Tycoon
{
    public static class CityRailRampCollider
    {
        public static void Build(Transform root, Vector3 bottom, Vector3 top)
        {
            // Khối dốc kín loại bỏ mặt trần thấp bên dưới tấm dốc khỏi phép bake NavMesh.
            float a = bottom.x;
            float b = top.x;
            float z = bottom.z;
            var mesh = new Mesh { name = "StationSolidRampCollision" };
            mesh.vertices = new[]
            {
                new Vector3(a, -.08f, z - 1.3f), new Vector3(b, -.08f, z - 1.3f),
                new Vector3(b, -.08f, z + 1.3f), new Vector3(a, -.08f, z + 1.3f),
                new Vector3(a, bottom.y, z - 1.3f), new Vector3(b, top.y, z - 1.3f),
                new Vector3(b, top.y, z + 1.3f), new Vector3(a, bottom.y, z + 1.3f)
            };
            mesh.triangles = new[]
            {
                4, 6, 5, 4, 7, 6,
                0, 1, 2, 0, 2, 3,
                0, 4, 5, 0, 5, 1,
                3, 2, 6, 3, 6, 7,
                1, 5, 6, 1, 6, 2,
                0, 3, 7, 0, 7, 4
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var collision = TownModelParts.Root("StationAccessibleRamp", Vector3.zero, root);
            collision.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            collision.gameObject.AddComponent<CityStaticGeometryLifetime>().Meshes = new[] { mesh };
        }
    }
}
