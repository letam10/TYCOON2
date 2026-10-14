using UnityEngine;

namespace Tycoon
{
    internal static class HandItemCarrotGeometry
    {
        public static Mesh Root()
        {
            // Từ chóp nhỏ đến vai củ; đóng đầu trên ngay dưới gốc lá.
            float[] heights = { 0, .05f, .14f, .25f, .38f, .52f, .66f, .79f, .89f, .95f, .98f, 1, 1 };
            float[] radii = { 0, .017f, .064f, .125f, .205f, .285f, .367f, .438f, .5f, .48f, .405f, .16f, 0 };
            const int sides = 20;
            int stride = sides + 1;
            var vertices = new Vector3[heights.Length * stride];
            var uv = new Vector2[vertices.Length];
            for (int ring = 0; ring < heights.Length; ring++)
            {
                float height = heights[ring];
                for (int side = 0; side <= sides; side++)
                {
                    float u = (float)side / sides;
                    float angle = u * Mathf.PI * 2;
                    float organic = 1 + .018f * Mathf.Cos(angle * 3 + height * 2);
                    float groove = 1 - .012f * Mathf.Pow(Mathf.Cos(height * 32), 4);
                    float radius = radii[ring] * organic * groove;
                    int index = ring * stride + side;
                    vertices[index] = new Vector3(Mathf.Cos(angle) * radius, height - .5f,
                        Mathf.Sin(angle) * radius);
                    uv[index] = new Vector2(u, height);
                }
            }
            var mesh = Finish("CarrySource_carrot", vertices, uv, heights.Length, sides);
            var normals = mesh.normals;
            for (int ring = 0; ring < heights.Length; ring++)
            {
                int first = ring * stride;
                var joined = (normals[first] + normals[first + sides]).normalized;
                normals[first] = joined;
                normals[first + sides] = joined;
            }
            mesh.normals = normals;
            mesh.RecalculateTangents();
            return mesh;
        }

        public static Mesh Leaf()
        {
            float[] widths = { .015f, .055f, .22f, .11f, .28f, .12f, .20f, .09f, 0 };
            const int sides = 4;
            var vertices = new Vector3[widths.Length * (sides + 1)];
            var uv = new Vector2[vertices.Length];
            for (int ring = 0; ring < widths.Length; ring++)
            {
                float v = (float)ring / (widths.Length - 1);
                for (int side = 0; side <= sides; side++)
                {
                    bool right = side is 0 or 1 or 4;
                    bool front = side is 0 or 3 or 4;
                    int index = ring * (sides + 1) + side;
                    float bend = .18f * v * v;
                    vertices[index] = new Vector3((right ? 1 : -1) * widths[ring], v,
                        bend + (front ? -.018f : .018f));
                    uv[index] = new Vector2(right ? 1 : 0, v);
                }
            }
            return Finish("CarrySource_carrotLeaf", vertices, uv, widths.Length, sides);
        }

        static Mesh Finish(string name, Vector3[] vertices, Vector2[] uv, int rings, int sides)
        {
            var triangles = new int[(rings - 1) * sides * 6];
            int cursor = 0;
            for (int ring = 0; ring < rings - 1; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int a = ring * (sides + 1) + side;
                    int b = a + sides + 1;
                    triangles[cursor++] = a;
                    triangles[cursor++] = b;
                    triangles[cursor++] = a + 1;
                    triangles[cursor++] = a + 1;
                    triangles[cursor++] = b;
                    triangles[cursor++] = b + 1;
                }
            }
            var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
