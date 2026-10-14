using UnityEngine;

namespace Tycoon
{
    public static class DiningFurnitureBuilder
    {
        public const float Ground = .06f;
        public const float CushionTop = .56f;
        public const float ChairDepth = .66f;
        public const float EdgeGap = .18f;
        const string Wood = "#A5825D", Frame = "#365B55", Cushion = "#AEC4AE";

        public static DiningFurniture Build(Transform parent, DiningTableShape shape)
        {
            var root = TownModelParts.Root("DiningFurniture", Vector3.up * Ground, parent);
            var furniture = root.gameObject.AddComponent<DiningFurniture>();
            furniture.Shape = shape;
            furniture.TableTop = .98f;
            bool round = shape == DiningTableShape.Round;
            float half = round ? .82f : .88f;
            var table = TownModelParts.Root("Table", Vector3.zero, root);
            if (round)
            {
                Cylinder("TableTop", new(0, .92f, 0), new(half * 2, .06f, half * 2), Wood, table);
                Cylinder("TableEdge", new(0, .872f, 0), new(1.58f, .025f, 1.58f), Frame, table);
                Cylinder("Pedestal", new(0, .43f, 0), new(.19f, .38f, .19f), Frame, table);
                Cylinder("PedestalFoot", new(0, .025f, 0), new(.84f, .025f, .84f), Frame, table);
                Cylinder("TopBracket", new(0, .83f, 0), new(.55f, .035f, .55f), Frame, table);
            }
            else
            {
                Box("TableTop", new(0, .92f, 0), new(1.76f, .12f, 1.76f), Wood, table);
                Box("TableApron", new(0, .81f, 0), new(1.52f, .14f, 1.52f), Frame, table);
                for (int x = -1; x <= 1; x += 2)
                    for (int z = -1; z <= 1; z += 2)
                        Box("TableLeg", new(x * .67f, .405f, z * .67f), new(.105f, .81f, .105f),
                            Frame, table);
            }
            TownFurnitureCollision.Fit(table);
            DiningMeshBatch.Combine(table);
            furniture.Seats = new Transform[2];
            float distance = half + EdgeGap + ChairDepth * .5f;
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? 1 : -1;
                var chair = Chair(root, new(side * distance, 0, 0), round ? .76f : 1.82f,
                    side == 1 ? -90 : 90, round ? "Chair" : "Bench");
                furniture.Seats[i] = TownModelParts.Root("SeatAnchor", new(0, CushionTop, 0), chair);
            }
            // Anchor cục bộ đi cùng bàn khi CityExpansion dịch cả khu nhà hàng.
            float sideClearance = (round ? .38f : .91f) + .7f;
            furniture.Approach = TownModelParts.Root("Approach", new(distance, -.04f, -sideClearance), root);
            return furniture;
        }

        public static Transform Chair(Transform parent, Vector3 point, float width, float yaw, string name)
        {
            var root = TownModelParts.Root(name, point, parent);
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            Box("SeatFrame", new(0, .43f, 0), new(width, .10f, ChairDepth), Wood, root);
            Box("SeatCushion", new(0, .52f, 0), new(width - .05f, .08f, ChairDepth - .04f), Cushion, root);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (width * .5f - .095f);
                Box("FrontLeg", new(x, .215f, .24f), new(.075f, .43f, .075f), Frame, root);
                Box("BackLeg", new(x, .51f, -.265f), new(.075f, 1.02f, .075f), Frame, root);
                Box("SideRail", new(x, .23f, 0), new(.05f, .055f, .51f), Frame, root);
                Cylinder("BackFastener", new(x, .86f, -.318f), new(.045f, .014f, .045f), Wood, root)
                    .transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            Box("BackRest", new(0, .885f, -.27f), new(width, .29f, .105f), Wood, root);
            Box("BackCushion", new(0, .875f, -.202f), new(width - .13f, .22f, .035f), Cushion, root);
            Box("FootRail", new(0, .20f, .21f), new(width - .13f, .045f, .045f), Frame, root);
            TownFurnitureCollision.Fit(root);
            DiningMeshBatch.Combine(root);
            return root;
        }

        public static void Counter(Transform restaurant)
        {
            // Quầy ăn nằm phía đông, xa quầy thu ngân và đường phục vụ sáu bàn.
            var root = TownModelParts.Root("RestaurantCounterSeating", new(8.4f, Ground, .6f), restaurant);
            var furniture = root.gameObject.AddComponent<DiningFurniture>();
            furniture.Shape = DiningTableShape.Counter;
            furniture.TableTop = 1.08f;
            furniture.Seats = new Transform[3];
            Box("CounterBase", new(0, .47f, 0), new(.65f, .94f, 5.8f), Frame, root);
            Box("CounterTop", new(0, 1.01f, 0), new(1, .14f, 6), Wood, root);
            for (int i = 0; i < 7; i++)
                Box("CounterPanel", new(-.334f, .49f, -2.4f + i * .8f), new(.04f, .76f, .68f),
                    Wood, root);
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = new(0, .54f, 0);
            collider.size = new(1, 1.08f, 6);
            DiningMeshBatch.Combine(root);
            for (int i = 0; i < 3; i++)
            {
                var seat = Stool(root, new(-1.01f, 0, -1.9f + i * 1.9f));
                furniture.Seats[i] = TownModelParts.Root("CounterSeatAnchor", new(0, .82f, 0), seat);
            }
            furniture.Approach = TownModelParts.Root("Approach", new(-2, -.04f, -3.75f), root);
        }

        static Transform Stool(Transform parent, Vector3 point)
        {
            var root = TownModelParts.Root("CounterStool", point, parent);
            root.localRotation = Quaternion.Euler(0, 90, 0);
            Box("StoolFrame", new(0, .69f, 0), new(.70f, .10f, ChairDepth), Wood, root);
            Box("StoolCushion", new(0, .78f, 0), new(.65f, .08f, .62f), Cushion, root);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("StoolFrontLeg", new(side * .25f, .345f, .24f), new(.075f, .69f, .075f), Frame, root);
                Box("StoolBackLeg", new(side * .25f, .615f, -.265f), new(.075f, 1.23f, .075f), Frame, root);
                Box("StoolSideRail", new(side * .25f, .32f, 0), new(.05f, .055f, .51f), Frame, root);
            }
            Box("StoolFootRest", new(0, .32f, .27f), new(.55f, .055f, .075f), Frame, root);
            Box("StoolBackRest", new(0, 1.08f, -.27f), new(.70f, .30f, .105f), Wood, root);
            Box("StoolBackCushion", new(0, 1.07f, -.202f), new(.57f, .23f, .035f), Cushion, root);
            TownFurnitureCollision.Fit(root);
            DiningMeshBatch.Combine(root);
            return root;
        }

        static GameObject Box(string name, Vector3 point, Vector3 size, string tint, Transform parent) =>
            TownModelParts.Box(name, point, size, tint == Cushion ? "fabric" : tint == Frame ? "metal" : "wood",
                tint, parent);

        static GameObject Cylinder(string name, Vector3 point, Vector3 size, string tint, Transform parent) =>
            TownModelParts.Cylinder(name, point, size, tint == Frame ? "metal" : "wood", tint, parent);
    }
}
