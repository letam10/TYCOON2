using UnityEngine;

namespace Tycoon
{
    public static class CityCoastWaterMesh
    {
        public static Mesh Create(float width, float depth)
            => Surface(width, depth, 0, false, false);
        public static Mesh BeachWater(float width, float depth, float worldZ)
            => Surface(width, depth, worldZ, true, false);
        public static Mesh BeachSand(float width, float depth, float worldZ)
            => Surface(width, depth, worldZ, true, true);

        static Mesh Surface(float width, float depth, float worldZ, bool coast, bool sand)
        {
            // Bước 2 m nhỏ hơn 1/6 bước sóng ngắn nhất (~12 m); mặt nước không gãy theo tấm lớn.
            int columns = Mathf.CeilToInt(width / 2), rows = Mathf.CeilToInt(depth / 2);
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var triangles = new int[columns * rows * 6];
            for (int z = 0; z <= rows; z++)
                for (int x = 0; x <= columns; x++)
                {
                    float u = x / (float)columns, localZ = (z / (float)rows - .5f) * depth;
                    float offset = coast ? .6f * Mathf.Sin((worldZ + localZ) * .06f)
                        + .25f * Mathf.Sin((worldZ + localZ) * .13f) : 0;
                    // Nâng cát từ cao độ đất tới đúng cao độ nước tại bờ, khép khe đứng 2,5 cm.
                    float height = sand ? Mathf.Lerp(.0275f, .0525f, u) : .0525f;
                    vertices[z * (columns + 1) + x] = new((u - .5f) * width
                        + offset * (sand ? u : 1 - u), height, localZ);
                }
            int at = 0;
            for (int z = 0; z < rows; z++)
                for (int x = 0; x < columns; x++)
                {
                    int a = z * (columns + 1) + x;
                    triangles[at++] = a; triangles[at++] = a + columns + 1; triangles[at++] = a + 1;
                    triangles[at++] = a + 1; triangles[at++] = a + columns + 1; triangles[at++] = a + columns + 2;
                }
            var mesh = new Mesh { name = sand ? "CoastalSandGrid" : "CoastalWaveGrid" };
            mesh.vertices = vertices; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var bounds = mesh.bounds; bounds.Expand(new Vector3(0, .08f, 0)); mesh.bounds = bounds;
            return mesh;
        }
    }
}
