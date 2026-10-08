using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public static class CityPublicTransportModel
    {
        static Material glass;

        static Material WindowMaterial()
        {
            if (glass) return glass;
            glass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            glass.name = "SharedTransitGlass";
            glass.SetColor("_BaseColor", new Color(.42f, .62f, .67f, .24f));
            glass.SetFloat("_Surface", 1);
            glass.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glass.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite", 0);
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.renderQueue = 3000;
            glass.enableInstancing = true;
            return glass;
        }

        public static Transform Build(int kind, Transform root)
        {
            bool taxi = kind == 2;
            float length = taxi ? 4.2f : 8.6f;
            float width = taxi ? 1.85f : 2.45f;
            float roof = taxi ? 1.65f : kind == 1 ? 3.05f : 2.8f;
            string color = taxi ? "#E7BE46" : kind == 1 ? "#DDDFD1" : "#478B91";
            var parts = TownModelParts.Root("SharedVehicleParts", Vector3.zero, root);
            Box("Floor", new(0, .42f, 0), new(width, .24f, length), "metal", "#374B52");
            Box("Roof", new(0, roof, 0), new(width, .16f, length - .18f), "metal", color);
            Box("Front", new(0, .96f, length * .5f), new(width, .75f, .14f), "metal", color);
            Box("Rear", new(0, 1.05f, -length * .5f), new(width, .95f, .14f), "metal", color);
            Box("FrontGlass", new(0, roof - .7f, length * .5f - .06f),
                new(width - .2f, taxi ? .5f : 1.08f, .06f), "glass", "#758E95");
            Box("RearGlass", new(0, roof - .7f, -length * .5f),
                new(width - .2f, taxi ? .5f : 1.08f, .05f), "glass", "#758E95");
            Box("FrontBumper", new(0, .42f, length * .5f + .1f),
                new(width, .18f, .2f), "metal", "#B2BCBD");
            Box("RearBumper", new(0, .42f, -length * .5f - .1f),
                new(width, .18f, .2f), "metal", "#B2BCBD");
            Box("FrontPlate", new(0, .68f, length * .5f + .09f),
                new(.48f, .15f, .025f), "plaster", "#EFEBD4");
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * width * .5f;
                Box("SidePanel", new(x, .9f, -.55f),
                    new(.1f, .8f, length - 1.3f), "metal", color);
                Box("Stripe", new(x + side * .06f, 1.02f, -.55f),
                    new(.025f, .13f, length - 1.4f), "metal", taxi ? "#343A3E" : "#E6BA65");
                int windows = taxi ? 2 : 5;
                for (int i = 0; i < windows; i++)
                {
                    float z = -length * .5f + .8f + i * (taxi ? 1.2f : 1.35f);
                    Box("Window", new(x, roof - .7f, z),
                        new(.045f, taxi ? .48f : 1.02f, 1.1f), "glass", "#758E95");
                    Box("WindowPillar", new(x, roof - .68f, z + .58f),
                        new(.09f, taxi ? .65f : 1.23f, .1f), "metal", color);
                }
                foreach (float z in new[] { -length * .32f, length * .32f })
                {
                    var tire = TownModelParts.Cylinder("Tire", new(x, .47f, z),
                        new(.9f, .17f, .9f), "metal", "#303B3E", parts);
                    tire.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    var rim = TownModelParts.Cylinder("Rim", new(x + side * .17f, .47f, z),
                        new(.52f, .018f, .52f), "metal", "#BDC5C5", parts);
                    rim.transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
                Box("Mirror", new(x + side * .18f, roof - .66f, length * .38f),
                    new(.2f, .27f, .3f), "metal", "#36494F");
                Box("Headlight", new(side * width * .36f, .94f, length * .5f + .09f),
                    new(.32f, .23f, .04f), "glass", "#FFF1C4");
                Box("TailLight", new(side * width * .37f, .95f, -length * .5f - .09f),
                    new(.22f, .3f, .04f), "glass", "#BD564C");
            }
            if (taxi)
                Box("TaxiRoofSign", new(0, roof + .17f, 0), new(.68f, .25f, .3f), "metal", "#FFF0B5");
            else
            {
                Box("RouteDisplay", new(0, roof - .14f, length * .5f + .035f),
                    new(1.65f, .22f, .055f), "metal", "#263B3B");
                for (int row = 0; row < 3; row++)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float z = -2.7f + row * 1.1f;
                        Box("Seat", new(side * .57f, .87f, z),
                            new(.65f, .15f, .62f), "fabric", "#496B7D");
                        Box("SeatBack", new(side * .57f, 1.15f, z - .28f),
                            new(.65f, .62f, .12f), "fabric", "#496B7D");
                    }
            }
            Bake(parts, root);
            var door = TownModelParts.Root("PassengerDoor", new(width * .5f + .04f,
                taxi ? 1.05f : 1.44f, taxi ? -.35f : 2.6f), root);
            TownModelParts.Box("DoorFrame", Vector3.zero, new(.09f, taxi ? .8f : 2.1f, .85f),
                "metal", color, door);
            TownModelParts.Box("DoorGlass", new(.055f, .25f, 0),
                new(.035f, taxi ? .35f : .8f, .7f), "glass", "#758E95", door);
            TownModelParts.Box("DoorHandle", new(.09f, -.2f, -.24f),
                new(.05f, .06f, .23f), "metal", "#CCD4D5", door);
            door.Find("DoorGlass").GetComponent<MeshRenderer>().sharedMaterial = WindowMaterial();
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = new(0, roof * .5f + .15f, 0);
            collider.size = new(width, roof - .3f, length);
            var obstacle = root.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = collider.center;
            obstacle.size = collider.size;
            obstacle.carving = false;
            return door;

            void Box(string name, Vector3 at, Vector3 size, string material, string tint)
            {
                var part = TownModelParts.Box(name, at, size, material, tint, parts);
                if (material == "glass" && name.Contains("Glass") || name == "Window")
                    part.GetComponent<MeshRenderer>().sharedMaterial = WindowMaterial();
            }
        }

        static void Bake(Transform source, Transform target)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>())
            {
                var material = filter.GetComponent<MeshRenderer>().sharedMaterial;
                if (!groups.TryGetValue(material, out var list)) groups[material] = list = new();
                list.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    transform = target.worldToLocalMatrix * filter.transform.localToWorldMatrix
                });
            }
            foreach (var group in groups)
            {
                var mesh = new Mesh { name = "PublicTransitBatchedSurface" };
                mesh.CombineMeshes(group.Value.ToArray(), true, true);
                var part = TownModelParts.Root("BatchedSurface", Vector3.zero, target);
                part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
            }
            source.gameObject.SetActive(false);
            if (Application.isPlaying) Object.Destroy(source.gameObject);
            else Object.DestroyImmediate(source.gameObject);
        }
    }
}
