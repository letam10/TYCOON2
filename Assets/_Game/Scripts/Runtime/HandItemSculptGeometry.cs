using UnityEngine;

namespace Tycoon
{
    internal static class HandItemSculptGeometry
    {
        public static Mesh Build(string shape)
        {
            if (shape is "ring" or "curl" or "score" or "vein" or "handle" or "strand")
                return HandItemTubeGeometry.Build(shape);
            if (shape == "leaf") return Leaf();
            float[] y;
            float[] r;
            switch (shape)
            {
                case "bottle":
                    y = new[] { 0f, 0, .04f, .09f, .60f, .68f, .75f, .78f, .94f, 1, 1 };
                    r = new[] { 0f, .35f, .43f, .45f, .45f, .41f, .22f, .19f, .19f, .22f, 0 };
                    break;
                case "churn":
                    y = new[] { 0f, 0, .03f, .09f, .65f, .73f, .82f, .9f, 1, 1 };
                    r = new[] { 0f, .39f, .47f, .46f, .46f, .43f, .31f, .31f, .34f, 0 };
                    break;
                case "bowl":
                    y = new[] { 0f, 0, .08f, .35f, .78f, 1, 1.02f, .95f, .72f, .31f, .13f, .13f };
                    r = new[] { 0f, .23f, .28f, .37f, .46f, .50f, .47f, .45f, .42f, .31f, .21f, 0 };
                    break;
                case "plate":
                    y = new[] { 0f, 0, .15f, .3f, .8f, 1, .9f, .55f, .4f, .4f };
                    r = new[] { 0f, .26f, .30f, .38f, .49f, .5f, .46f, .36f, .29f, 0 };
                    break;
                case "steak":
                case "cloth":
                case "cheese":
                    y = new[] { 0f, 0, .08f, .2f, .8f, .94f, 1, 1 };
                    r = new[] { 0f, .40f, .47f, .5f, .5f, .47f, .40f, 0 };
                    break;
                default:
                    y = new[] { 0f, 0, .05f, .16f, .34f, .55f, .76f, .91f, .98f, 1, 1 };
                    r = new[] { 0f, .20f, .34f, .45f, .5f, .49f, .43f, .31f, .17f, .06f, 0 };
                    break;
            }
            const int sides = 24;
            var vertices = new Vector3[y.Length * (sides + 1)];
            var uv = new Vector2[vertices.Length];
            for (int ring = 0; ring < y.Length; ring++)
            {
                for (int side = 0; side <= sides; side++)
                {
                    float u = (float)side / sides;
                    float angle = u * Mathf.PI * 2;
                    float x = Mathf.Cos(angle);
                    float z = Mathf.Sin(angle);
                    float radius = r[ring];
                    if (shape == "tomato") radius *= 1 + .085f * Mathf.Cos(angle * 6);
                    if (shape == "pod") radius *= 1 + .15f * Mathf.Cos(y[ring] * Mathf.PI * 6);
                    if (shape == "steak") radius *= 1 + .15f * Mathf.Sin(angle * 3) + .08f * x;
                    if (shape == "wool") radius *= 1 + .08f * Mathf.Sin(angle * 7 + y[ring] * 20);
                    if (shape is "loaf" or "cloth" or "sack")
                    {
                        x = Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), .40f);
                        z = Mathf.Sign(z) * Mathf.Pow(Mathf.Abs(z), .40f);
                    }
                    if (shape == "sack")
                        radius *= (1 - .45f * Mathf.Pow(y[ring], 6)) *
                            (1 + .055f * Mathf.Sin(angle * 8 + y[ring] * 8));
                    if (shape == "cloth") radius *= 1 + .025f * Mathf.Sin(angle * 7 + y[ring] * 3);
                    if (shape == "cheese")
                    {
                        // Thu nhỏ bề ngang về đầu nhọn của miếng phô mai.
                        x = Mathf.Lerp(-.65f, .65f, (x + 1) * .5f);
                        z *= .22f + .78f * (x + .65f) / 1.3f;
                    }
                    int index = ring * (sides + 1) + side;
                    vertices[index] = new Vector3(x * radius, y[ring], z * radius);
                    uv[index] = new Vector2(u, (float)ring / (y.Length - 1));
                }
            }
            return Finish(shape, vertices, uv, y.Length, sides);
        }

        static Mesh Leaf()
        {
            const int rings = 13;
            const int sides = 8;
            var vertices = new Vector3[rings * (sides + 1)];
            var uv = new Vector2[vertices.Length];
            for (int ring = 0; ring < rings; ring++)
            {
                float v = (float)ring / (rings - 1);
                // Sin(PI float) có thể âm rất nhỏ; căn lũy thừa cần miền không âm.
                float width = Mathf.Pow(Mathf.Max(0, Mathf.Sin(v * Mathf.PI)), .8f) * .5f;
                for (int side = 0; side <= sides; side++)
                {
                    float u = (float)side / sides;
                    float a = u * Mathf.PI * 2;
                    int index = ring * (sides + 1) + side;
                    vertices[index] = new Vector3(Mathf.Cos(a) * width, v,
                        .2f * v * v + Mathf.Sin(a) * .018f + width * .09f);
                    uv[index] = new Vector2(u, v);
                }
            }
            return Finish("leaf", vertices, uv, rings, sides);
        }

        internal static Mesh Finish(string shape, Vector3[] vertices, Vector2[] uv, int rings, int sides)
        {
            var indices = new int[(rings - 1) * sides * 6];
            int index = 0;
            for (int ring = 0; ring < rings - 1; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int a = ring * (sides + 1) + side;
                    int b = a + sides + 1;
                    indices[index++] = a;
                    indices[index++] = b;
                    indices[index++] = a + 1;
                    indices[index++] = a + 1;
                    indices[index++] = b;
                    indices[index++] = b + 1;
                }
            }
            var mesh = new Mesh { name = "CarrySource_" + shape, vertices = vertices, uv = uv, triangles = indices };
            mesh.RecalculateNormals();
            var normals = mesh.normals;
            for (int ring = 0; ring < rings; ring++)
            {
                int first = ring * (sides + 1);
                Vector3 joined = (normals[first] + normals[first + sides]).normalized;
                normals[first] = joined;
                normals[first + sides] = joined;
            }
            mesh.normals = normals;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
