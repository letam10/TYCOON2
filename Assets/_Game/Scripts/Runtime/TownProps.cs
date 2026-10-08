using UnityEngine;

namespace Tycoon
{
    public static class TownProps
    {
        public static void Planter(Transform parent, Vector3 point, string tint = "#8E8875")
        {
            var root = TownModelParts.Root("StonePlanter", point + Vector3.up * .06f, parent);
            TownModelParts.Box("PlanterStone", new(0, .35f, 0), new(1.2f, .7f, .7f),
                "stone", tint, root, solid: true);
            TownModelParts.Box("PlanterSoil", new(0, .71f, 0), new(1.04f, .03f, .54f), "soil", "#4B4533", root);
            for (int i = 0; i < 5; i++)
            {
                var leaf = TownModelParts.Box("ShrubLeaf", new(-.4f + i * .2f, 1, (i % 2 - .5f) * .15f),
                    new(.3f, .56f + i % 3 * .1f, .24f), "grass", "#637344", root, i * 43);
                leaf.transform.localRotation *= Quaternion.Euler(8 + i * 4, 0, 12 - i * 6);
            }
        }

        public static void Storage(Transform root)
        {
            var detail = TownModelParts.Root("WarehouseDetails", Vector3.zero, root);
            for (int side = -1; side <= 1; side += 2)
            {
                TownModelParts.Box("RackUpright", new(side * 1.1f, 1, .58f), new(.065f, 2, .09f),
                    "metal", "#798680", detail);
                for (int i = 0; i < 3; i++)
                    TownModelParts.Box("RackBrace", new(side * 1.1f, .35f + i * .54f, .56f),
                        new(.09f, .035f, 1.2f), "metal", "#ADB2A8", detail);
            }
            TownModelParts.Box("WarehousePallet", new(0, .1f, .5f), new(2.4f, .12f, 1.45f),
                "wood", "#978365", detail);
        }

        public static void Machine(MachineStation station)
        {
            var detail = TownModelParts.Root("MachineEngineering", Vector3.zero, station.transform);
            for (int side = -1; side <= 1; side += 2)
            {
                TownModelParts.Box("MachineFoot", new(side * .73f, .13f, .46f), new(.15f, .26f, .25f),
                    "metal", "#707874", detail);
                var bolt = TownModelParts.Cylinder("PanelFastener", new(side * .65f, .45f, -.83f),
                    new(.065f, .012f, .065f), "metal", "#C0C4BC", detail);
                bolt.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            TownModelParts.Box("ServicePanel", new(.86f, .77f, .37f), new(.05f, .66f, .58f),
                "metal", "#909D96", detail);
            for (int i = 0; i < 5; i++)
                TownModelParts.Box("CoolingSlat", new(.895f, .59f + i * .075f, .37f),
                    new(.022f, .018f, .36f), "metal", "#3F4947", detail);
            TownModelParts.Box("SafetySticker", new(.86f, 1.17f, .35f), new(.025f, .15f, .18f),
                "plaster", "#CCB261", detail);
        }

        public static void Dock(Transform root)
        {
            TownModelParts.Box("LoadingApron", new(0, .037f, 0), new(3.4f, .04f, 4),
                "paving", "#A5A58F", root);
            for (int i = 0; i < 5; i++)
                TownModelParts.Box("DockMark", new(1.45f, .063f, -1.55f + i * .75f),
                    new(.16f, .012f, .4f), "plaster", "#DDC983", root);
        }
    }
}
