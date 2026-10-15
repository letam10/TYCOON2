using UnityEngine;

namespace Tycoon
{
    public static class CityOuterDistricts
    {
        public static void Build(Transform world, CityDressingSpace space)
        {
            Landmark("PoliceStation", new(-226, 0, -8), new(38, 12, 34), CityCivicModels.Police);
            Landmark("FireStation", new(-226, 0, 64), new(38, 12, 38), CityCivicModels.Fire);
            Landmark("SeasideBar", new(152, 0, 127), new(32, 10, 30), CityCivicModels.Bar);
            Landmark("EntertainmentHall", new(-72, 0, 181), new(40, 12, 32), CityCivicModels.Arcade);
            Landmark("Playground", new(12, 0, 181), new(40, 10, 32), CityLeisureModels.Playground);
            Landmark("FoundersMonument", new(-150, 0, 181), new(30, 12, 30), CityLeisureModels.Monument);
            Landmark("Harbor", new(231, 0, -123), new(62, 12, 66), CityCoastModels.Harbor);
            Landmark("Beach", new(233, 0, 77), new(68, 12, 136), CityCoastModels.Beach);
            Landmark("CoffeeHouse", new(-226, 0, -149), new(32, 10, 30), CityCommercialModels.Cafe);
            Landmark("Bank", new(-226, 0, 120), new(38, 12, 38), CityCommercialModels.Bank);
            Landmark("Hotel", new(128, 0, 185), new(48, 20, 36), CityCommercialModels.Hotel);
            Landmark("Theater", new(75, 0, 181), new(44, 12, 34), CityCommercialModels.Theater);
            Landmark("Marina", new(231, 0, -40), new(62, 12, 44), CityMarinaModels.Build);
            CityAccessPaths.Build(world);
            CityOceanEdge.Build(world);
            Streetscape(world, space);

            void Landmark(string id, Vector3 position, Vector3 size, System.Action<CityStaticGeometry> build)
            {
                if (!space.Take(position, size.x, size.z, id))
                    throw new System.InvalidOperationException("Vị trí khu mới bị chặn: " + id);
                var root = new GameObject(id).transform;
                root.SetParent(world, false);
                root.localPosition = position;
                if (id == "CoffeeHouse") root.localRotation = Quaternion.Euler(0, 180, 0);
                var marker = root.gameObject.AddComponent<CityLandmark>();
                marker.Id = id;
                marker.Footprint = size;
                var geometry = new CityStaticGeometry(root);
                build(geometry);
                geometry.Finish();
                if (id == "Harbor" || id == "Marina")
                    WorldExpansionBoat.Build(root, new Vector3(10, 0, 18));
                if (id is "Beach" or "Harbor" or "Marina") CityCoastModels.BlockWater(root, id);
            }
        }

        static void Streetscape(Transform world, CityDressingSpace space)
        {
            var root = new GameObject("OuterStreetFurniture").transform;
            root.SetParent(world, false);
            var g = new CityStaticGeometry(root);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side < 0 ? -184 : 179;
                for (int z = -96; z <= 140; z += 24)
                {
                    Vector3 p = new(x, 0, z);
                    if (!space.Take(p, 4, 8, "Avenue furniture")) continue;
                    CityProps.Lamp(g, p);
                    CityProps.Planter(g, p + Vector3.forward * 3, 2.2f);
                    Vector3 treePoint = p + Vector3.back * 2.6f;
                    CityVegetation.Tree(g, treePoint, .85f,
                        WorldVegetationCatalog.StableSpecies("coastal", treePoint) % 3);
                }
            }
            foreach (float x in new[] { -195f, 190f })
            {
                foreach (float z in new[] { -45f, 29f, 155f, -112f })
                    CityLeisureModels.Signal(g, new(x + 6.2f, 0, z + 6.2f));
            }
            for (int x = -160; x <= 160; x += 32)
            {
                Vector3 p = new(x, 0, -138);
                if (!space.Take(p, 16, 16, "Southern grove")) continue;
                CityVegetation.Tree(g, p, 1.3f,
                    WorldVegetationCatalog.StableSpecies("forest", p) % 3);
                Vector3 second = p + new Vector3(5, 0, 4);
                Vector3 third = p + new Vector3(-4, 0, -3);
                CityVegetation.Tree(g, second, 1, WorldVegetationCatalog.StableSpecies("forest", second) % 3);
                CityVegetation.Tree(g, third, .9f, WorldVegetationCatalog.StableSpecies("forest", third) % 3);
            }
            g.Finish();
        }
    }
}
