using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityBeachDetails
    {
        public static void Umbrella(CityStaticGeometry g, Vector3 p)
        {
            g.Cylinder(p + Vector3.up * 1.52f, .055f, 3.04f, "wood", "#B99565");
            g.Cylinder(p + Vector3.up * .08f, .32f, .16f, "stone", "#B8AD90");
            g.Cylinder(p + Vector3.up * 3.27f, .09f, .13f, "wood", "#786B57");
            for (int sector = 0; sector < 8; sector++)
            {
                g.Sculpt(CanopyPanel(sector), p, "fabric", sector % 2 == 0 ? "#C5AA79" : "#D7CAB0");
                float a = sector * Mathf.PI / 4;
                g.Beam(p + new Vector3(0, 2.98f, 0),
                    p + new Vector3(Mathf.Cos(a) * 2.5f, 2.8f, Mathf.Sin(a) * 2.5f),
                    .028f, "metal", "#8D8C77");
            }
            g.Collider("UmbrellaPole", p + Vector3.up * 1.5f, new(.18f, 3, .18f));
        }

        public static void Lounger(CityStaticGeometry g, Vector3 p)
        {
            g.Box(p + Vector3.up * .32f, new(1.1f, .12f, 2.8f), "wood", "#B99565");
            g.Box(p + Vector3.up * .405f, new(1.02f, .05f, 2.5f), "fabric", "#D2CCAB");
            g.RotatedBox(p + new Vector3(0, .64f, 1.23f), new(1.05f, .1f, .9f),
                Quaternion.Euler(-35, 0, 0), "fabric", "#D2CCAB");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(p + new Vector3(side * .48f, .19f, -1), new(.085f, .38f, .1f),
                    "wood", "#8D7D64");
                g.Box(p + new Vector3(side * .48f, .19f, 1), new(.085f, .38f, .1f),
                    "wood", "#8D7D64");
                g.Box(p + new Vector3(side * .48f, .22f, 0), new(.08f, .09f, 2.2f),
                    "wood", "#8D7D64");
            }
            g.Collider("BeachLounger", p + new Vector3(0, .45f, .05f), new(1.15f, .9f, 3.3f));
        }

        static Mesh CanopyPanel(int sector)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            Vector3 center = new(0, 3.22f, 0);
            for (int wedge = 0; wedge < 3; wedge++)
            {
                float a = (sector + wedge / 3f) * Mathf.PI / 4;
                float b = (sector + (wedge + 1) / 3f) * Mathf.PI / 4;
                Vector3 innerA = Point(a, 1.3f, 3.1f);
                Vector3 innerB = Point(b, 1.3f, 3.1f);
                Vector3 outerA = Point(a, 2.6f, wedge == 0 ? 2.82f : 2.74f);
                Vector3 outerB = Point(b, 2.6f, wedge == 2 ? 2.82f : 2.74f);
                Face(center, innerB, innerA);
                Face(innerA, innerB, outerB);
                Face(innerA, outerB, outerA);
            }
            var mesh = new Mesh { name = "UmbrellaCanvasPanel" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                // Tách mặt dưới để normal không triệt tiêu khi nhìn lên mái dù.
                foreach (var v in new[] { a, b, c, c, b, a })
                {
                    indices.Add(vertices.Count);
                    vertices.Add(v);
                }
            }
        }

        static Vector3 Point(float angle, float radius, float height)
        {
            return new(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        }
    }
}
