using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static class CityOceanHorizon
    {
        public const string Tint = "#28A6BA";

        public static void Build(Transform parent, Bounds bounds, float left)
        {
            const float margin = 1600;
            float west = bounds.min.x - margin;
            float right = bounds.max.x + margin;
            float bottom = bounds.min.z - margin;
            float top = bounds.max.z + margin;
            var water = TownSurfaceMaterials.Get("water", Tint);
            var grass = TownSurfaceMaterials.Get("grass", "#6C8151");
            Plane(parent, "OceanVisualHorizon", left, right, bottom, top, .0525f, water);
            Plane(parent, "OceanNorthContext", 261, left, bottom, bounds.min.z - 3, .0525f, water);
            Plane(parent, "OceanSouthContext", 261, left, bounds.max.z + 3, top, .0525f, water);
            // Bối cảnh ngoài bản đồ không thêm đất chơi, collider hay đường đi.
            Plane(parent, "CoastalNorthGround", west, 261, bottom, bounds.min.z, 0, grass);
            Plane(parent, "CoastalSouthGround", west, 261, bounds.max.z, top, 0, grass);
            Plane(parent, "CoastalWestGround", west, bounds.min.x, bounds.min.z, bounds.max.z, 0, grass);
        }

        static void Plane(Transform parent, string name, float left, float right,
            float bottom, float top, float height, Material material)
        {
            const int columns = 4;
            const int rows = 8;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var triangles = new int[columns * rows * 6];
            for (int z = 0; z <= rows; z++)
            {
                for (int x = 0; x <= columns; x++)
                {
                    vertices[z * (columns + 1) + x] = new Vector3(
                        Mathf.Lerp(left, right, x / (float)columns), height,
                        Mathf.Lerp(bottom, top, z / (float)rows));
                }
            }
            int index = 0;
            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int corner = z * (columns + 1) + x;
                    triangles[index++] = corner;
                    triangles[index++] = corner + columns + 1;
                    triangles[index++] = corner + 1;
                    triangles[index++] = corner + 1;
                    triangles[index++] = corner + columns + 1;
                    triangles[index++] = corner + columns + 2;
                }
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            // Biển xa chỉ để nhìn; không mở rộng va chạm hoặc vùng đi của NPC.
            var horizon = new GameObject(name);
            horizon.transform.SetParent(parent, false);
            horizon.isStatic = true;
            horizon.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = horizon.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            horizon.AddComponent<CityStaticGeometryLifetime>().Meshes = new[] { mesh };
            bool inspect = GameSession.Instance && GameSession.Instance.IsQa
                && !GameSession.Instance.IsOrdinaryPlayCheck;
            mesh.UploadMeshData(!inspect);
        }
    }
}
