using UnityEngine;

namespace Tycoon
{
    public static class TownLandscape
    {
        public static void Build(Transform world)
        {
            var root = TownModelParts.Root("TownLandscape", Vector3.zero, world);
            world.gameObject.AddComponent<CozyCropMotion>();
            Hills(root);
            TownModelParts.Box("FarmMarker", new(-23.9f, 1.45f, 3.3f), new(4.8f, .7f, .15f),
                "wood", "#66734E", root);
            Art.Label("01  NÔNG TRẠI", new(-23.9f, 1.45f, 3.21f), root, .17f, "#F4EEDB", false);
            for (int side = -1; side <= 1; side += 2)
                TownModelParts.Box("FarmSignPost", new(-23.9f + side * 2, .66f, 3.37f),
                    new(.12f, 1.3f, .13f), "wood", "#8F8062", root);
            for (int side = -1; side <= 1; side += 2)
                TownModelParts.Box("RiverShoreMud", new(4, -.015f, 66 + side * 3.45f),
                    new(126, .055f, .75f), "soil", "#8B8267", root);
            for (int i = 0; i < 23; i++)
            {
                TownFoliage.Tree(new(-60 + i * 6.8f, 0, 77 + i % 3 * 3), root, 1.25f + i % 3 * .12f, i);
                if (i % 2 == 0)
                    TownFoliage.Tree(new(-65 + i * 7, 0, -36 - i % 3 * 2), root, .85f, i + 31);
            }
            for (int i = 0; i < 11; i++)
            {
                TownFoliage.Tree(new(-57 - i % 2 * 3, 0, -15 + i * 7.2f), root, 1, i + 14);
                TownFoliage.Tree(new(68 + i % 2 * 3, 0, -5 + i * 7.7f), root, 1.1f, i + 57);
            }
            for (int i = 0; i < 8; i++)
            {
                TownModelParts.Box("RiverBankStone", new(-49 + i * 16, .06f, 70.5f),
                    new(12.5f, .4f, 1.3f), "stone", "#A39D83", root, i % 3 * 2);
                TownProps.Planter(root, new(-45 + i * 13, 0, 61), "#878E78");
            }
            TownModelParts.Box("TownEntrancePlinth", new(56.8f, .5f, -20.7f), new(6, 1, .7f),
                "brick", "#BCA58B", root);
            TownModelParts.Box("TownEntranceSign", new(56.8f, 1.8f, -20.7f), new(5.4f, 1.3f, .18f),
                "wood", "#596D52", root);
            Art.Label("THỊ TRẤN NÔNG NGHIỆP", new(56.8f, 1.8f, -20.82f), root, .14f, "#F2EBDD", false);
            TownModelParts.Box("FarmWalkway", new(-24, .025f, 17), new(1.5f, .026f, 25),
                "paving", "#A5A18B", root);
            TownModelParts.Box("FarmThreshold", new(-14, .022f, 4.2f), new(20, .025f, 1.25f),
                "paving", "#AEA38A", root);
            for (int i = 0; i < 6; i++)
                TownProps.Planter(root, new(-43 + i * 16, 0, -22), "#8C8975");
        }

        static void Hills(Transform parent)
        {
            const int columns = 64;
            const int rows = 20;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var triangles = new int[columns * rows * 6];
            for (int z = 0; z <= rows; z++)
            {
                for (int x = 0; x <= columns; x++)
                {
                    float px = -100 + x * 4;
                    float pz = 74 + z * 4;
                    float fade = Mathf.SmoothStep(0, 1, z / 8f);
                    float height = Mathf.PerlinNoise(x * .085f, z * .09f + 12) * 16
                        + Mathf.Sin(px * .047f) * 3;
                    vertices[z * (columns + 1) + x] = new(px, height * fade - .12f, pz);
                    if (x == columns || z == rows) continue;
                    int a = z * (columns + 1) + x;
                    int b = a + columns + 1;
                    int t = (z * columns + x) * 6;
                    triangles[t] = a;
                    triangles[t + 1] = b;
                    triangles[t + 2] = a + 1;
                    triangles[t + 3] = a + 1;
                    triangles[t + 4] = b;
                    triangles[t + 5] = b + 1;
                }
            }
            var mesh = new Mesh { name = "RollingCountryside", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var root = TownModelParts.Root("LandscapeHills", Vector3.zero, parent);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = TownSurfaceMaterials.Get("grass", "#76825A");
        }
    }
}
