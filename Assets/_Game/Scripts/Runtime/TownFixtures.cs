using UnityEngine;

namespace Tycoon
{
    public static class TownFixtures
    {
        public static void Dress(Transform floor, Vector2 size, string label)
        {
            var root = TownModelParts.Root("DistrictFixtures", Vector3.zero, floor);
            bool market = label.Contains("SIÊU THỊ");
            bool shop = label.Contains("NÔNG SẢN");
            bool processing = label.Contains("CHẾ BIẾN");
            bool restaurant = label.Contains("NHÀ HÀNG");
            float x = size.x * .5f;
            float z = size.y * .5f;
            if (market)
            {
                for (int i = -1; i <= 1; i += 2)
                    Cabinet(root, new(i * 4.2f, 0, z - 2), 2.8f, true);
                Bench(root, new(-x + .9f, 0, 2.5f), 3.4f, 90);
            }
            else if (shop)
            {
                Cabinet(root, new(-x + 1.4f, 0, z - 2.7f), 2.2f, false);
                Bench(root, new(-x + .9f, 0, -.5f), 2.5f, 90);
            }
            else if (processing)
            {
                Cabinet(root, new(-x + 1.3f, 0, z - 1.1f), 2, false);
                TownModelParts.Box("UtilityToolBoard", new(-x + 1.5f, 1.9f, z - .25f),
                    new(1.7f, .6f, .08f), "metal", "#899C92", root);
                for (int i = 0; i < 4; i++)
                    TownModelParts.Cylinder("MountedTool", new(-x + .9f + i * .35f, 1.85f, z - .35f),
                        new(.035f, .18f, .035f), "metal", "#C0C5B9", root);
            }
            else if (restaurant)
            {
                Bench(root, new(-x + .8f, 0, -.6f), 4.5f, 90);
                Cabinet(root, new(x - 1, 0, z - 1.5f), 2.8f, false, 90);
            }
            else
            {
                Cabinet(root, new(-x + .85f, 0, 0), 2.4f, true, 90);
                Bench(root, new(-x + .85f, 0, -4), 2.8f, 90);
            }
        }

        static void Cabinet(Transform parent, Vector3 point, float width, bool glass, float yaw = 0)
        {
            var root = TownModelParts.Root("CabinetCollision", point + Vector3.up * .06f, parent);
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            TownModelParts.Box("CabinetBody", new(0, .5f, 0), new(width, 1, .7f),
                "wood", "#A78E68", root, solid: true);
            TownModelParts.Box("CabinetCounter", new(0, 1.05f, 0), new(width + .1f, .1f, .8f),
                "stone", "#C2BDA8", root, solid: true);
            for (int i = -1; i <= 1; i += 2)
                TownModelParts.Box("CabinetDoor", new(i * width * .24f, .55f, -.37f),
                    new(width * .45f, .78f, .035f), "wood", "#9C8463", root);
            if (!glass) return;
            TownModelParts.Box("DisplayGlass", new(0, 1.32f, .2f), new(width, .44f, .045f),
                "glass", "#94AAA3", root, solid: true);
            TownModelParts.Box("DisplayTop", new(0, 1.56f, 0), new(width, .045f, .7f),
                "metal", "#BBC3B7", root, solid: true);
        }

        static void Bench(Transform parent, Vector3 point, float width, float yaw)
        {
            var root = TownModelParts.Root("BenchCollision", point + Vector3.up * .06f, parent);
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            TownModelParts.Box("BenchSeat", new(0, .45f, 0), new(width, .14f, .6f),
                "wood", "#9D8668", root, solid: true);
            TownModelParts.Box("BenchBack", new(0, .83f, .28f), new(width, .55f, .1f),
                "wood", "#9D8668", root, solid: true);
            for (int i = -1; i <= 1; i += 2)
                TownModelParts.Box("BenchLeg", new(i * (width * .5f - .22f), .2f, 0),
                    new(.1f, .4f, .48f), "metal", "#606E61", root, solid: true);
        }
    }
}
