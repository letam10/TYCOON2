using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityCivicDetails
    {
        const string Trim = "#D5D1C4";
        const string Metal = "#465B60";
        const string Glass = "#72949B";

        public static void Roof(CityStaticGeometry g, float width, float depth, float height, string wall)
        {
            ChamferedBox(g, new(0, height + .15f, 0), new(width + .5f, .3f, depth + .5f), "stone", Trim);
            g.Box(new(0, height + .32f, 0), new(width - .6f, .08f, depth - .6f), "roof", Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(new(side * (width * .5f - .12f), height + .52f, 0),
                    new(.28f, .44f, depth), "plaster", wall);
                g.Box(new(0, height + .52f, side * (depth * .5f - .12f)),
                    new(width, .44f, .28f), "plaster", wall);
                g.Box(new(side * (width * .5f - .12f), height + .76f, 0),
                    new(.4f, .08f, depth + .2f), "metal", Metal);
                g.Box(new(0, height + .76f, side * (depth * .5f - .12f)),
                    new(width + .2f, .08f, .4f), "metal", Metal);
                Vector3 pipe = new(side * (width * .5f - .55f), height * .5f, depth * .5f + .21f);
                g.Cylinder(pipe, .07f, height, "metal", Metal);
                for (int strap = 1; strap < height; strap += 2)
                    g.Box(new(pipe.x, strap, pipe.z - .07f), new(.22f, .06f, .26f), "metal", Metal);
            }
            Vector3 unit = new(width * .28f, height + .73f, depth * .25f);
            g.Box(unit, new(2.6f, .75f, 1.8f), "metal", Metal);
            g.Box(unit + Vector3.up * .4f, new(2.7f, .08f, 1.9f), "metal", Trim);
            for (int slat = 0; slat < 5; slat++)
                g.Box(unit + new Vector3(0, -.26f + slat * .12f, -.92f),
                    new(2.25f, .045f, .04f), "metal", Trim);
        }

        public static void Window(CityStaticGeometry g, Vector3 p, float yaw)
        {
            Quaternion r = Quaternion.Euler(0, yaw, 0);
            // Trục +Z cục bộ luôn hướng ra ngoài để nẹp kính không bị chìm trong tường.
            g.RotatedBox(p, new(2.28f, 2.45f, .15f), r, "stone", Trim);
            g.RotatedBox(p + r * Vector3.forward * .09f, new(2.02f, 2.18f, .035f), r, "glass", Glass);
            g.RotatedBox(p + r * Vector3.forward * .12f, new(.075f, 2.18f, .055f), r, "metal", Metal);
            g.RotatedBox(p + r * Vector3.forward * .12f, new(2.02f, .075f, .055f), r, "metal", Metal);
            g.RotatedBox(p + Vector3.down * 1.25f + r * Vector3.forward * .09f,
                new(2.48f, .12f, .43f), r, "stone", Trim);
            g.RotatedBox(p + Vector3.up * 1.25f, new(2.48f, .12f, .24f), r, "stone", Trim);
            g.RotatedBox(p + Vector3.up * 1.3f + r * Vector3.forward * .04f,
                new(2.16f, .07f, .08f), r, "metal", Metal);
        }

        public static void ServiceDoor(CityStaticGeometry g, float depth)
        {
            Vector3 p = new(0, 1.3f, depth * .5f + .11f);
            g.Box(p, new(1.5f, 2.6f, .18f), "metal", Trim);
            g.Box(p + Vector3.forward * .11f, new(1.28f, 2.4f, .065f), "metal", Metal);
            g.Box(p + new Vector3(.43f, -.05f, .16f), new(.28f, .055f, .07f), "metal", Trim);
            for (int slat = 0; slat < 4; slat++)
                g.Box(p + new Vector3(0, -.8f + slat * .13f, .15f),
                    new(.8f, .05f, .025f), "metal", Trim);
            g.Box(p + new Vector3(0, 1.42f, .33f), new(2.1f, .1f, .8f), "metal", Metal);
        }

        public static void ArcadeCabinet(CityStaticGeometry g, Vector3 p)
        {
            const string Blue = "#566F87";
            g.Box(p + Vector3.up * .07f, new(1.24f, .14f, 1.24f), "metal", Metal);
            g.Box(p + Vector3.up * .58f, new(1.2f, 1.02f, 1.05f), "metal", Blue);
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(p + new Vector3(side * .57f, 1.42f, .2f), new(.1f, 1.1f, .68f), "metal", Blue);
                g.Box(p + new Vector3(side * .627f, .65f, 0), new(.02f, .52f, .7f), "metal", Trim);
            }
            g.Box(p + new Vector3(0, 1.45f, .52f), new(1.2f, 1.04f, .1f), "metal", Metal);
            g.RotatedBox(p + new Vector3(0, 1.5f, .03f), new(1.02f, .82f, .12f),
                Quaternion.Euler(-12, 0, 0), "metal", Metal);
            g.RotatedBox(p + new Vector3(0, 1.5f, -.035f), new(.87f, .66f, .025f),
                Quaternion.Euler(-12, 0, 0), "glass", "#75ACB3");
            g.Box(p + new Vector3(0, 1.96f, .13f), new(1.26f, .2f, .93f), "metal", Blue);
            g.Box(p + new Vector3(0, 1.96f, -.345f), new(1.02f, .12f, .025f), "glass", "#D5BA6F");
            g.Box(p + new Vector3(0, 1.08f, -.37f), new(1.25f, .15f, .67f), "metal", Metal);
            g.Cylinder(p + new Vector3(-.3f, 1.22f, -.5f), .025f, .18f, "metal", Trim);
            g.Cylinder(p + new Vector3(-.3f, 1.31f, -.5f), .065f, .06f, "metal", "#BB6955");
            for (int button = 0; button < 3; button++)
                g.Cylinder(p + new Vector3(.05f + button * .14f, 1.17f, -.5f), .045f, .04f,
                    "metal", "#D5BA6F");
            g.Box(p + new Vector3(0, .63f, -.54f), new(.28f, .36f, .035f), "metal", Metal);
            g.Box(p + new Vector3(0, .71f, -.565f), new(.13f, .027f, .025f), "metal", Trim);
            g.Collider("ArcadeCabinet", p + Vector3.up * 1.03f, new(1.3f, 2.06f, 1.5f));
        }

        public static void ChamferedBox(CityStaticGeometry g, Vector3 p, Vector3 size, string surface, string tint)
        {
            Vector3 half = size * .5f;
            float bevel = Mathf.Min(.035f, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * .35f);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector2[] corners =
            {
                new(-half.x + bevel, -half.z), new(half.x - bevel, -half.z),
                new(half.x, -half.z + bevel), new(half.x, half.z - bevel),
                new(half.x - bevel, half.z), new(-half.x + bevel, half.z),
                new(-half.x, half.z - bevel), new(-half.x, -half.z + bevel)
            };
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                Vector3 low = Ring(i, -half.y + bevel, 0);
                Vector3 high = Ring(i, half.y - bevel, 0);
                Vector3 nextLow = Ring(next, -half.y + bevel, 0);
                Vector3 nextHigh = Ring(next, half.y - bevel, 0);
                Vector3 top = Ring(i, half.y, bevel);
                Vector3 nextTop = Ring(next, half.y, bevel);
                Vector3 bottom = Ring(i, -half.y, bevel);
                Vector3 nextBottom = Ring(next, -half.y, bevel);
                Quad(low, high, nextHigh, nextLow);
                Quad(high, top, nextTop, nextHigh);
                Quad(bottom, low, nextLow, nextBottom);
                Triangle(new(0, half.y, 0), nextTop, top);
                Triangle(new(0, -half.y, 0), bottom, nextBottom);
            }
            var mesh = new Mesh { name = "CivicChamfer" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            g.Sculpt(mesh, p, surface, tint);

            Vector3 Ring(int index, float y, float inset)
            {
                Vector2 v = corners[index];
                return new(v.x - Mathf.Sign(v.x) * inset, y, v.y - Mathf.Sign(v.y) * inset);
            }

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Triangle(a, b, c);
                Triangle(a, c, d);
            }

            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = vertices.Count;
                vertices.AddRange(new[] { a, b, c });
                triangles.AddRange(new[] { start, start + 1, start + 2 });
            }
        }
    }
}
