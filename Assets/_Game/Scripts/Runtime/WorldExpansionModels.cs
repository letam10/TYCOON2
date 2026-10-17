using UnityEngine;
namespace Tycoon
{
    public static class WorldExpansionModels
    {
        public static void Build(Transform poiRoot, WorldExpansionPoi poi)
        {
            if (!poiRoot || poiRoot.Find("ExpansionModels"))
                return;
            Transform root = new GameObject("ExpansionModels").transform;
            root.SetParent(poiRoot, false);
            CityStaticGeometry geometry = new CityStaticGeometry(root);
            switch (poi.Id)
            {
                case "ForestReserve":
                    Forest(geometry, root, poi);
                    break;
                case "IndustrialYard":
                    Industrial(geometry, root, poi);
                    break;
                case "NorthVillage":
                    Village(geometry, root, poi);
                    break;
                case "EastMarina":
                    Marina(geometry, root, poi);
                    break;
                case "SouthHighway":
                    Highway(geometry, root, poi);
                    break;
            }
            geometry.Finish();
        }
        static bool CanPlace(Transform root, Vector3 local, Vector3 footprint, float radius)
        {
            float halfX = footprint.x * .5f - radius;
            float halfZ = footprint.z * .5f - radius;
            if (halfX <= 0f || halfZ <= 0f || Mathf.Abs(local.x) > halfX
                || Mathf.Abs(local.z) > halfZ)
                return false;
            Vector3 worldPoint = root.TransformPoint(local);
            return !WorldRouteCatalog.IsReserved(worldPoint, radius, out _);
        }
        static void Forest(CityStaticGeometry g, Transform root, WorldExpansionPoi poi)
        {
            for (int row = -3; row <= 3; row++)
            {
                for (int column = -4; column <= 4; column++)
                {
                    Vector3 local = new Vector3(column * 22f + (row & 1) * 6f, 0, row * 23f);
                    if ((row + column) % 4 == 0 || column == -1 || column == 3)
                        continue;
                    AddTree(g, root, poi, local, 4.8f + (column & 1) * .8f, (row + column) % 2 == 0);
                }
            }
            for (int row = -4; row <= 4; row++)
            {
                for (int column = -5; column <= 5; column++)
                {
                    if ((row * 3 + column) % 5 == 0)
                        continue;
                    Vector3 local = new Vector3(column * 13f + (row & 1) * 4f, 0, row * 13f);
                    AddGrass(g, root, poi, local, .65f + ((row + column) & 2) * .12f);
                }
            }
            Vector3[] rocks =
            {
                new Vector3(-54, 0, -18), new Vector3(-12, 0, -32),
                new Vector3(26, 0, -20), new Vector3(54, 0, 28)
            };
            for (int index = 0; index < rocks.Length; index++)
                AddRock(g, root, poi, rocks[index], 1.25f + index % 2 * .45f);
            AddBench(g, root, poi, new Vector3(-8, 0, 27), 0f);
            AddBench(g, root, poi, new Vector3(52, 0, -6), 90f);
        }

        static void AddGrass(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local,
            float height)
        {
            if (!CanPlace(root, local, poi.Footprint, .8f))
                return;
            string color = ((Mathf.RoundToInt(local.x) + Mathf.RoundToInt(local.z)) & 1) == 0
                ? "#4E824A" : "#6B9954";
            g.Cone(local + Vector3.up * height * .5f, .55f, height, "grass", color);
        }
        static void AddTree(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local,
            float height,
            bool evergreen)
        {
            if (!CanPlace(root, local, poi.Footprint, 3.5f))
                return;
            float crown = evergreen ? 3.3f : 3.8f;
            Vector3 trunk = local + Vector3.up * (height * .5f);
            g.Cylinder(trunk, .5f, height, "wood", "#604633");
            g.Collider("ForestTree", trunk, new Vector3(1f, height, 1f));
            g.Crown(local + Vector3.up * (height + crown * .45f),
                new Vector3(crown, crown * .9f, crown),
                evergreen ? "#2F6B4B" : "#467A4D");
            if (evergreen)
                g.Cone(local + Vector3.up * (height + crown * .8f), crown * .72f,
                    crown * 1.6f, "grass", "#356343");
        }
        static void AddRock(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local,
            float radius)
        {
            if (!CanPlace(root, local, poi.Footprint, radius + .5f))
                return;
            g.Cylinder(local + Vector3.up * radius * .35f, radius, radius * .7f,
                "stone", "#7B8078");
            g.Collider("ForestRock", local + Vector3.up * radius * .35f,
                new Vector3(radius * 1.7f, radius * .7f, radius * 1.7f));
        }
        static void AddBench(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local,
            float yaw)
        {
            if (!CanPlace(root, local, poi.Footprint, 2.2f))
                return;
            g.Box(local + Vector3.up * 1.05f, new Vector3(3.5f, .18f, .55f),
                "wood", "#806449", yaw);
            g.Collider("ForestBench", local + Vector3.up * .55f,
                new Vector3(3.6f, 1.1f, .8f));
        }
        static void Industrial(CityStaticGeometry g, Transform root, WorldExpansionPoi poi)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    Vector3 local = new Vector3(-62f + column * 30f, 0, -43f + row * 39f);
                    AddContainer(g, root, poi, local, row == 2, (row + column) % 2 == 0);
                }
            }
            AddContainer(g, root, poi, new Vector3(46, 0, 42), true, true);
            AddSilo(g, root, poi, new Vector3(48, 0, 49));
            AddSilo(g, root, poi, new Vector3(70, 0, 49));
            AddSilo(g, root, poi, new Vector3(70, 0, 23));
            AddPipeRun(g, root, poi, new Vector3(48, 0, 49), new Vector3(48, 0, 14));
            AddPipeRun(g, root, poi, new Vector3(70, 0, 49), new Vector3(30, 0, 49));
            AddPipeRun(g, root, poi, new Vector3(70, 0, 23), new Vector3(70, 0, -8));
            AddStackBoundary(g, root, poi, new Vector3(24, 0, 53));
            AddWarehouse(g, root, poi, new Vector3(-62, 0, 56), false);
            AddWarehouse(g, root, poi, new Vector3(6, 0, 56), true);
        }

        // Ống chỉ tạo khi cả hai đầu và điểm giữa còn cách route đủ xa.
        static void AddPipeRun(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 start,
            Vector3 end)
        {
            Vector3 midpoint = (start + end) * .5f;
            if (!CanPlace(root, start, poi.Footprint, 3.2f)
                || !CanPlace(root, midpoint, poi.Footprint, 3.2f)
                || !CanPlace(root, end, poi.Footprint, 3.2f))
                return;
            Vector3 elevatedStart = start + Vector3.up * 8.2f;
            Vector3 elevatedEnd = end + Vector3.up * 8.2f;
            g.Beam(elevatedStart, elevatedEnd, .7f, "metal", "#788883");
            g.Cylinder(start + Vector3.up * 4.1f, .42f, 8.2f, "metal", "#65746F");
            g.Cylinder(end + Vector3.up * 4.1f, .42f, 8.2f, "metal", "#65746F");
        }
        static void AddContainer(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local,
            bool secondLevel,
            bool blue)
        {
            float baseY = secondLevel ? 6.6f : 3.3f;
            if (!CanPlace(root, local, poi.Footprint, 9.5f))
                return;
            string paint = blue ? "#536F78" : "#A4654D";
            g.Box(local + Vector3.up * baseY, new Vector3(20f, 6.2f, 8f), "metal", paint);
            for (int rib = -2; rib <= 2; rib++)
                g.Box(new Vector3(local.x + rib * 2.8f, baseY + .15f, local.z - 4.08f),
                    new Vector3(.14f, 5.8f, .16f), "metal", "#B4BCB6");
            Vector3 door = new Vector3(local.x, baseY, local.z - 4.13f);
            g.Box(door, new Vector3(5.2f, 4.6f, .12f), "metal", "#425B5E");
            for (int rib = 0; rib < 3; rib++)
                g.Box(door + Vector3.up * (-1.7f + rib * .85f),
                    new Vector3(5.25f, .08f, .18f), "metal", "#BBC3BA");
            g.Collider("IndustrialContainer", local + Vector3.up * baseY,
                new Vector3(20f, 6.2f, 8f));
        }
        static void AddSilo(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local)
        {
            if (!CanPlace(root, local, poi.Footprint, 7f))
                return;
            g.Cylinder(local + Vector3.up * 6.4f, 5.5f, 12.8f, "metal", "#9BA6A0");
            g.Cone(local + Vector3.up * 13.5f, 5.9f, 2.2f, "metal", "#5A7070");
            for (int ring = 0; ring < 4; ring++)
                g.Cylinder(local + Vector3.up * (2.4f + ring * 2.7f), 5.7f, .14f,
                    "metal", "#5F6D69");
            for (int side = -1; side <= 1; side += 2)
                g.Box(local + new Vector3(-2.2f + side * .58f, 6.1f, -5.57f),
                    new Vector3(.1f, 10.8f, .1f), "metal", "#667A75");
            for (int rung = 0; rung < 9; rung++)
                g.Box(local + new Vector3(-2.2f, 1.4f + rung * 1.15f, -5.57f),
                    new Vector3(1.25f, .1f, .1f), "metal", "#667A75");
            g.Cylinder(local + Vector3.up * 15.1f, .5f, 1.1f, "metal", "#667A75");
            g.Cone(local + Vector3.up * 15.8f, .68f, .35f, "metal", "#536562");
            g.Collider("IndustrialSilo", local + Vector3.up * 6.4f,
                new Vector3(11f, 12.8f, 11f));
        }
        static void AddStackBoundary(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local)
        {
            if (!CanPlace(root, local, poi.Footprint, 2f))
                return;
            Vector3 start = local + Vector3.left * 11f;
            Vector3 end = local + Vector3.right * 11f;
            g.Beam(start + Vector3.up * 1.7f, end + Vector3.up * 1.7f,
                .12f, "metal", "#566967");
            g.Cylinder(start + Vector3.up * 1.1f, .16f, 2.2f, "metal", "#566967");
            g.Cylinder(end + Vector3.up * 1.1f, .16f, 2.2f, "metal", "#566967");
        }

        static void AddWarehouse(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local,
            bool cool)
        {
            const float width = 28f;
            const float depth = 17f;
            const float wallHeight = 6.6f;
            if (!CanPlace(root, local, poi.Footprint, 12f))
                return;

            string wall = cool ? "#63777A" : "#8B7562";
            string roof = cool ? "#3F5559" : "#5C4E49";
            g.Box(local + Vector3.up * (wallHeight * .5f),
                new Vector3(width, wallHeight, depth), "metal", wall);
            g.RotatedBox(local + Vector3.up * 6.85f,
                new Vector3(width + 1.4f, .24f, depth + 1.2f),
                Quaternion.Euler(0, 0, 0), "roof", roof);
            g.Box(local + new Vector3(0, 3.1f, -8.58f),
                new Vector3(10.5f, 5.5f, .14f), "metal", "#334A4E");
            for (int rib = -2; rib <= 2; rib++)
                g.Box(local + new Vector3(rib * 2.1f, 3.1f, -8.72f),
                    new Vector3(.14f, 5.15f, .08f), "metal", "#A8B4AE");
            for (int level = 0; level < 3; level++)
                g.Box(local + new Vector3(0, 1.35f + level * 1.35f, -8.78f),
                    new Vector3(10.2f, .08f, .08f), "metal", "#A8B4AE");
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box(local + new Vector3(side * 9.2f, 3.1f, -8.7f),
                    new Vector3(3.2f, 5.2f, .16f), "glass", "#6E8E92");
                g.Box(local + new Vector3(side * 9.2f, 3.1f, -8.84f),
                    new Vector3(2.82f, 4.82f, .07f), "glass", "#8BAEB0");
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 vent = local + new Vector3(side * 9.4f, 7.5f, 2.2f);
                g.Box(vent, new Vector3(3.2f, .16f, 2.2f), "metal", "#B6B9AD");
                for (int vane = -1; vane <= 1; vane++)
                    g.RotatedBox(vent + Vector3.up * (vane * .42f),
                        new Vector3(2.5f, .08f, .12f),
                        Quaternion.Euler(0, 0, vane * 8f), "metal", "#566967");
            }
            g.Collider("IndustrialWarehouse", local + Vector3.up * (wallHeight * .5f),
                new Vector3(width, wallHeight, depth));
        }
        static void Village(CityStaticGeometry g, Transform root, WorldExpansionPoi poi)
        {
            for (int row = 0; row < 2; row++)
            {
                for (int column = 0; column < 2; column++)
                {
                    Vector3 local = new Vector3(column == 0 ? -48f : 48f,
                        0, row == 0 ? -24f : 24f);
                    AddCabin(g, root, poi, local, (row + column) % 2 == 0);
                }
            }
            AddWell(g, root, poi, new Vector3(0, 0, 30));
        }
        static void AddCabin(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local,
            bool warm)
        {
            const float width = 19f;
            const float depth = 14f;
            const float wallHeight = 4.8f;
            if (!CanPlace(root, local, poi.Footprint, 10.5f))
                return;
            string wall = warm ? "#C49A73" : "#A9B6A3";
            string roof = warm ? "#7B5C4D" : "#566B64";
            g.Box(local + Vector3.up * .18f, new Vector3(width, .32f, depth),
                "stone", "#AAA89A");
            g.Box(local + new Vector3(-9.35f, wallHeight * .5f, 0),
                new Vector3(.3f, wallHeight, depth), "plaster", wall);
            g.Box(local + new Vector3(9.35f, wallHeight * .5f, 0),
                new Vector3(.3f, wallHeight, depth), "plaster", wall);
            g.Box(local + new Vector3(0, wallHeight * .5f, 6.85f),
                new Vector3(width, wallHeight, .3f), "plaster", wall);
            g.Box(local + new Vector3(-6.8f, wallHeight * .5f, -6.85f),
                new Vector3(5.4f, wallHeight, .3f), "plaster", wall);
            g.Box(local + new Vector3(6.8f, wallHeight * .5f, -6.85f),
                new Vector3(5.4f, wallHeight, .3f), "plaster", wall);
            Vector3 ridge = local + Vector3.up * 7.2f;
            g.RotatedBox(ridge + Vector3.left * 4.5f, new Vector3(9.4f, .22f, depth + 1f),
                Quaternion.Euler(0, 0, -28f), "roof", roof);
            g.RotatedBox(ridge + Vector3.right * 4.5f, new Vector3(9.4f, .22f, depth + 1f),
                Quaternion.Euler(0, 0, 28f), "roof", roof);
            g.Box(local + new Vector3(-5.8f, 2.35f, -7.08f),
                new Vector3(3.2f, 2.4f, .14f), "wood", "#E1DFCC");
            g.Box(local + new Vector3(-5.8f, 2.35f, -7.18f),
                new Vector3(2.7f, 2.05f, .06f), "glass", "#6D9295");
            g.Box(local + new Vector3(-5.8f, 2.35f, -7.25f),
                new Vector3(.1f, 2.05f, .08f), "wood", "#E1DFCC");
            g.Box(local + new Vector3(-5.8f, 2.35f, -7.25f),
                new Vector3(2.7f, .1f, .08f), "wood", "#E1DFCC");
            g.Box(local + new Vector3(5.8f, 1.65f, -7.08f),
                new Vector3(2.15f, 3.3f, .14f), "wood", "#806449");
            g.Box(local + new Vector3(5.8f, 1.45f, -7.2f),
                new Vector3(1.62f, 2.85f, .06f), "glass", "#6D9295");
            g.Box(local + new Vector3(0, .55f, -7.65f),
                new Vector3(5.2f, .22f, 1.25f), "stone", "#AAA89A");
            for (int side = -1; side <= 1; side += 2)
                g.Box(local + new Vector3(side * 2.1f, 1.5f, -8.16f),
                    new Vector3(.12f, 2.4f, .12f), "wood", "#E1DFCC");
            g.Cylinder(local + new Vector3(5.3f, 7.75f, 2.6f), .44f, 1.55f,
                "brick", warm ? "#8B6656" : "#5E6E68");
            g.Cone(local + new Vector3(5.3f, 8.62f, 2.6f), .55f, .26f,
                "roof", warm ? "#5D514C" : "#455A57");
            Vector3 table = local + new Vector3(-4.2f, 0, .4f);
            if (CanPlace(root, table, poi.Footprint, 1.1f))
            {
                g.Box(table + Vector3.up * 1.1f, new Vector3(2.4f, .16f, 1.2f),
                    "wood", "#806449");
            }
            Vector3 bed = local + new Vector3(4.1f, 0, 2.6f);
            if (CanPlace(root, bed, poi.Footprint, 1.5f))
                g.Box(bed + Vector3.up * .48f, new Vector3(3.6f, .7f, 2.1f),
                    "wood", "#806449");
            g.Collider("VillageCabin", local + Vector3.up * 2.4f,
                new Vector3(width, wallHeight, depth));
        }
        static void AddWell(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local)
        {
            if (!CanPlace(root, local, poi.Footprint, 2.5f))
                return;
            g.Cylinder(local + Vector3.up * .45f, 2.1f, .9f, "stone", "#7B8078");
            g.Box(local + Vector3.up * 4.4f, new Vector3(4.4f, .18f, 3.2f),
                "roof", "#7B5C4D");
            g.Collider("VillageWell", local + Vector3.up * .45f, new Vector3(4.2f, .9f, 4.2f));
        }
        static void Marina(CityStaticGeometry g, Transform root, WorldExpansionPoi poi)
        {
            g.Box(new Vector3(30, .02f, 0), new Vector3(84f, .04f, 70f),
                "water", "#346D73");
            const float terminalX = -43f;
            g.Box(new Vector3(terminalX, .05f, 0), new Vector3(38f, .1f, 18f),
                "stone", "#AAA89A");
            g.Box(new Vector3(terminalX, 2.2f, 8.7f), new Vector3(38f, 4.4f, .3f),
                "plaster", "#A9B6A3");
            g.Box(new Vector3(terminalX - 18.85f, 2.2f, 0), new Vector3(.3f, 4.4f, 18f),
                "plaster", "#A9B6A3");
            g.Box(new Vector3(terminalX + 18.85f, 2.2f, 0), new Vector3(.3f, 4.4f, 18f),
                "plaster", "#A9B6A3");
            g.Box(new Vector3(terminalX, 4.55f, 0), new Vector3(39f, .25f, 19f),
                "roof", "#5D7772");
            g.Box(new Vector3(terminalX, 1.05f, -3.2f), new Vector3(10f, .18f, 1.8f),
                "wood", "#806449");
            g.Collider("MarinaTerminalBackWall", new Vector3(terminalX, 2.2f, 8.7f),
                new Vector3(38f, 4.4f, .3f));
            g.Collider("MarinaTerminalSideWall", new Vector3(terminalX - 18.85f, 2.2f, 0),
                new Vector3(.3f, 4.4f, 18f));
            g.Collider("MarinaTerminalSideWall", new Vector3(terminalX + 18.85f, 2.2f, 0),
                new Vector3(.3f, 4.4f, 18f));
            WorldExpansionBoat.Build(root, new Vector3(20, 0, -27));
            AddPier(g, root, poi, -32f);
            AddPier(g, root, poi, 27f);
            AddMooring(g, root, poi, new Vector3(-58, 0, 27));
            AddMooring(g, root, poi, new Vector3(58, 0, 27));
        }
        static void AddPier(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            float z)
        {
            const float startX = -62f;
            for (int index = 0; index < 6; index++)
            {
                float x = startX + index * 20f + 10f;
                Vector3 local = new Vector3(x, 0, z);
                if (!CanPlace(root, local, poi.Footprint, 3.2f))
                    continue;
                g.Box(local + Vector3.up * .18f, new Vector3(20f, .36f, 5.6f),
                    "wood", "#927957");
                g.Cylinder(new Vector3(x, .7f, z - 2.15f), .22f, 1.4f,
                    "wood", "#5B5145");
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float railZ = z + side * 2.65f;
                for (int segment = 0; segment < 5; segment++)
                {
                    if (segment == 2)
                        continue;
                    float from = startX + segment * 20f + 1f;
                    Vector3 midpoint = new Vector3(from + 9f, 0, railZ);
                    if (!CanPlace(root, midpoint, poi.Footprint, 1.2f))
                        continue;
                    g.Beam(midpoint + Vector3.up * 1.2f + Vector3.left * 9f,
                        midpoint + Vector3.up * 1.2f + Vector3.right * 9f,
                        .12f, "wood", "#604633");
                }
            }
        }
        static void AddMooring(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local)
        {
            if (!CanPlace(root, local, poi.Footprint, 1f))
                return;
            g.Cylinder(local + Vector3.up * .6f, .3f, 1.2f, "metal", "#4B5856");
            g.Cylinder(local + Vector3.up * 1.25f, .38f, .12f, "metal", "#71817B");
            g.Beam(local + new Vector3(-.38f, .88f, 0),
                local + new Vector3(.38f, .88f, 0), .08f, "metal", "#4B5856");
            g.Cylinder(local + Vector3.up * 1.36f, .08f, .12f, "glass", "#C76E4D");
        }
        static void Highway(CityStaticGeometry g, Transform root, WorldExpansionPoi poi)
        {
            g.Box(Vector3.up * .04f, new Vector3(420f, .08f, 13f),
                "asphalt", "#50595B");
            for (int index = -8; index <= 8; index++)
            {
                float x = index * 24f;
                g.Box(new Vector3(x, .1f, 0), new Vector3(13f, .025f, .18f),
                    "paving", "#E8D9A7");
                g.Box(new Vector3(x, .1f, -5.4f), new Vector3(13f, .025f, .12f),
                    "paving", "#D7D8CA");
                AddLamp(g, root, poi, new Vector3(x, 0, -10f));
                AddLamp(g, root, poi, new Vector3(x, 0, 10f));
                AddGuardrail(g, root, poi, new Vector3(x, 0, -8f));
                AddGuardrail(g, root, poi, new Vector3(x, 0, 8f));
            }
        }
        static void AddLamp(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local)
        {
            if (!CanPlace(root, local, poi.Footprint, 1.1f))
                return;
            g.Cylinder(local + Vector3.up * 3.2f, .13f, 6.4f, "metal", "#526C6C");
            Vector3 arm = local + Vector3.up * 6.3f;
            Vector3 end = arm + (local.z < 0f ? Vector3.forward : Vector3.back) * 2.1f;
            g.Beam(arm, end, .12f, "metal", "#526C6C");
            g.Box(end + Vector3.down * .1f, new Vector3(.7f, .16f, .35f),
                "metal", "#E2D8AC");
        }
        static void AddGuardrail(
            CityStaticGeometry g,
            Transform root,
            WorldExpansionPoi poi,
            Vector3 local)
        {
            if (!CanPlace(root, local, poi.Footprint, 1.2f))
                return;
            g.Cylinder(local + Vector3.up * .65f, .1f, 1.3f, "metal", "#65716F");
            g.Beam(local + Vector3.up * 1.05f + Vector3.left * 12f,
                local + Vector3.up * 1.05f + Vector3.right * 12f,
                .14f, "metal", "#65716F");
        }
    }
}
