using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    public static class CityVehicleModel
    {
        public static Transform Build(string color, Transform parent, bool van)
        {
            var root = TownModelParts.Root(van ? "CityServiceVan" : "CityPassengerCar", Vector3.zero, parent);
            var parts = TownModelParts.Root("VehicleParts", Vector3.zero, root);
            Box("Body", new(0, .66f, 0), new(1.85f, .65f, 3.85f), "metal", color);
            Box("Cabin", new(0, 1.17f, -.12f), new(1.6f, .58f, van ? 2.9f : 1.9f), "metal", color);
            Box("Roof", new(0, 1.49f, -.12f), new(1.68f, .09f, van ? 3 : 2), "metal", color);
            Box("FrontGlass", new(0, 1.22f, van ? 1.35f : .86f), new(1.36f, .42f, .035f), "glass", "#546A73");
            Box("BackGlass", new(0, 1.2f, -1.09f), new(1.37f, .36f, .035f), "glass", "#546A73");
            Box("Bumper", new(0, .47f, 1.97f), new(1.7f, .15f, .14f), "metal", "#A6ADAD");
            Box("Grille", new(0, .67f, 1.945f), new(.75f, .21f, .045f), "metal", "#343E40");
            Box("Plate", new(0, .48f, 2.05f), new(.48f, .12f, .015f), "plaster", "#D8D9CA");
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Headlight", new(side * .66f, .75f, 1.95f), new(.31f, .16f, .045f), "glass", "#EEEBCB");
                Box("TailLight", new(side * .68f, .78f, -1.95f), new(.26f, .14f, .045f), "glass", "#AF5549");
                Box("SideGlass", new(side * .81f, 1.2f, -.1f), new(.035f, .37f, 1.56f), "glass", "#546A73");
                Box("WindowPillar", new(side * .834f, 1.2f, -.1f), new(.035f, .42f, .09f), "metal", color);
                Box("DoorHandle", new(side * .948f, .98f, -.38f), new(.026f, .034f, .21f), "metal", "#CDD3D1");
                Box("Mirror", new(side * 1.02f, 1.16f, .68f), new(.21f, .13f, .21f), "metal", color);
                foreach (float z in new[] { -1.22f, 1.23f })
                {
                    var tire = TownModelParts.Cylinder("Tire", new(side * .9f, .38f, z),
                        new(.74f, .14f, .74f), "metal", "#343837", parts);
                    tire.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    var hub = TownModelParts.Cylinder("WheelRim", new(side * 1.045f, .38f, z),
                        new(.43f, .014f, .43f), "metal", "#B3BFBE", parts);
                    hub.transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
            }
            Bake(parts, root);
            return root;

            void Box(string name, Vector3 at, Vector3 size, string surface, string tint) =>
                TownModelParts.Box(name, at, size, surface, tint, parts);
        }

        static void Bake(Transform source, Transform target)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>())
            {
                var material = filter.GetComponent<MeshRenderer>().sharedMaterial;
                if (!groups.TryGetValue(material, out var parts)) groups[material] = parts = new();
                parts.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    transform = source.worldToLocalMatrix * filter.transform.localToWorldMatrix
                });
            }
            foreach (var pair in groups)
            {
                var mesh = new Mesh { name = "CityVehicleCombined" };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                var group = TownModelParts.Root("VehicleSurface", Vector3.zero, target);
                group.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                group.gameObject.AddComponent<MeshRenderer>().sharedMaterial = pair.Key;
            }
            Object.Destroy(source.gameObject);
        }
    }
}
