using UnityEngine;

namespace Tycoon
{
    public static class SafeModelDetails
    {
        public static GameObject Build(Transform parent)
        {
            var root = TownModelParts.Root("SafeEngineering", Vector3.zero, parent);
            if (RetailPropDetails.Restore("safe", root)) return root.gameObject;
            RetailPropDetails.Box(root, "SafePlinth", new(0, .12f, 0),
                new(1.6f, .24f, 1.3f), RetailPropDetails.Iron);
            RetailPropDetails.Box(root, "SafeShell", new(0, .83f, 0),
                new(1.3f, 1.3f, 1.05f), RetailPropDetails.Sage);
            RetailPropDetails.Box(root, "DoorGasket", new(0, .84f, .535f),
                new(1.13f, 1.12f, .035f), RetailPropDetails.Iron);
            RetailPropDetails.Box(root, "ArmoredDoor", new(0, .84f, .59f),
                new(1.06f, 1.05f, .085f), RetailPropDetails.Steel);
            RetailPropDetails.Box(root, "DoorFace", new(-.015f, .84f, .638f),
                new(.9f, .89f, .016f), RetailPropDetails.Sage);
            for (int hinge = -1; hinge <= 1; hinge += 2)
            {
                float y = .84f + hinge * .35f;
                RetailPropDetails.Box(root, "HingeLeaf", new(.535f, y, .589f),
                    new(.17f, .13f, .064f), RetailPropDetails.Steel);
                RetailPropDetails.Cylinder(root, "HingeBarrel", new(.585f, y, .632f), .08f, .18f);
                RetailPropDetails.Cylinder(root, "HingePin", new(.585f, y + .097f, .632f),
                    .057f, .02f, RetailPropDetails.Iron);
            }
            RetailPropDetails.Cylinder(root, "DialBezel", new(.16f, .93f, .667f), .28f, .035f,
                forward: true);
            RetailPropDetails.Cylinder(root, "CombinationDial", new(.16f, .93f, .697f), .22f, .037f,
                RetailPropDetails.Iron, true);
            RetailPropDetails.Box(root, "DialIndex", new(.16f, 1.052f, .69f),
                new(.024f, .031f, .02f), RetailPropDetails.Cream, "plaster");
            RetailPropDetails.Box(root, "DialPointer", new(.16f, .998f, .719f),
                new(.012f, .033f, .008f), RetailPropDetails.Cream, "plaster");
            for (int end = -1; end <= 1; end += 2)
                RetailPropDetails.Cylinder(root, "HandleRosette", new(-.23f, .79f + end * .135f, .67f),
                    .11f, .046f, forward: true);
            RetailPropDetails.Box(root, "SafePullHandle", new(-.23f, .79f, .723f),
                new(.064f, .34f, .064f), RetailPropDetails.Steel);
            RetailPropDetails.Box(root, "ManufacturerPlate", new(0, 1.195f, .652f),
                new(.3f, .073f, .016f), RetailPropDetails.Cream, "plaster");
            for (int side = -1; side <= 1; side += 2)
            {
                RetailPropDetails.Bolt(root, new(side * .41f, .455f, .654f));
                RetailPropDetails.Bolt(root, new(side * .41f, 1.225f, .654f));
                RetailPropDetails.Box(root, "BaseMountPlate", new(side * .65f, .245f, -.48f),
                    new(.19f, .025f, .19f), RetailPropDetails.Steel);
            }
            RetailPropDetails.Bake("safe", root);
            return root.gameObject;
        }
    }
}
