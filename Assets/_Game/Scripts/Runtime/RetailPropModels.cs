using UnityEngine;

namespace Tycoon
{
    public static class RetailPropModels
    {
        public static bool Supports(string key) => key is "storage_rack" or "display_counter"
            or "checkout_counter" or "supply_crate";

        public static Vector3 Size(string key) => key switch
        {
            "storage_rack" => new(3, 1.65f, .85f),
            "display_counter" => new(1.4f, 1.04f, 4.7f),
            "checkout_counter" => new(2.2f, 1.1f, 1.1f),
            "supply_crate" => new(1.15f, .7f, 1.15f),
            _ => throw new System.ArgumentException("Unknown retail prop: " + key, nameof(key))
        };

        public static GameObject Build(string key)
        {
            if (!Supports(key)) throw new System.ArgumentException("Unknown retail prop: " + key, nameof(key));
            var root = new GameObject(key);
            if (RetailPropDetails.Restore(key, root.transform)) return root;
            switch (key)
            {
                case "storage_rack":
                    Storage(root.transform);
                    break;
                case "display_counter":
                    Display(root.transform);
                    break;
                case "checkout_counter":
                    Checkout(root.transform);
                    break;
                case "supply_crate":
                    RetailPropDetails.Crate(root.transform);
                    break;
            }
            RetailPropDetails.Bake(key, root.transform);
            return root;
        }

        static void Storage(Transform root)
        {
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    RetailPropDetails.Box(root, "RackFoot", new(x * 1.4f, .025f, z * .325f),
                        new(.2f, .05f, .2f), RetailPropDetails.Iron);
                    RetailPropDetails.Box(root, "RackUpright", new(x * 1.4f, .84f, z * .335f),
                        new(.1f, 1.62f, .1f), RetailPropDetails.Sage);
                }
                RetailPropDetails.Box(root, "EndTie", new(x * 1.4f, .15f, 0),
                    new(.11f, .14f, .75f), RetailPropDetails.Sage);
                RetailPropDetails.Box(root, "EndTie", new(x * 1.4f, 1.51f, 0),
                    new(.11f, .16f, .75f), RetailPropDetails.Sage);
            }
            // Để trống phía trước cho hàng thật và khoảng giữa cho kệ nâng cấp.
            RetailPropDetails.Box(root, "RearKickBeam", new(0, .12f, .345f),
                new(2.8f, .18f, .12f), RetailPropDetails.Sage);
            RetailPropDetails.Box(root, "RackBack", new(0, .91f, .405f),
                new(2.72f, 1.3f, .04f), RetailPropDetails.Wood, "wood");
            RetailPropDetails.Box(root, "RackCanopy", new(0, 1.61f, 0),
                new(3, .08f, .85f), RetailPropDetails.Sage);
            RetailPropDetails.Box(root, "UpperBeam", new(0, 1.5f, -.34f),
                new(2.8f, .15f, .1f), RetailPropDetails.Sage);
            for (int side = -1; side <= 1; side += 2)
            {
                RetailPropDetails.Bolt(root, new(side * 1.4f, .15f, -.393f));
                RetailPropDetails.Bolt(root, new(side * 1.4f, 1.5f, -.393f));
            }
        }

        static void Display(Transform root)
        {
            RetailPropDetails.Box(root, "RecessedPlinth", new(0, .09f, 0),
                new(1.22f, .18f, 4.46f), RetailPropDetails.Iron);
            RetailPropDetails.Box(root, "CabinetCarcass", new(0, .565f, 0),
                new(1.3f, .77f, 4.56f), RetailPropDetails.Sage);
            RetailPropDetails.Box(root, "DisplayStockDeck", new(0, .995f, 0),
                new(1.4f, .09f, 4.7f), RetailPropDetails.Wood, "wood");
            for (int side = -1; side <= 1; side += 2)
            {
                for (int panel = 0; panel < 3; panel++)
                    RetailPropDetails.Box(root, "SideTimberPanel",
                        new(side * .661f, .565f, (panel - 1) * 1.49f),
                        new(.028f, .61f, 1.46f), RetailPropDetails.Wood, "wood");
                RetailPropDetails.Box(root, "SideEdgeBand", new(side * .678f, .946f, 0),
                    new(.044f, .05f, 4.58f), RetailPropDetails.Steel);
                RetailPropDetails.Box(root, "SideKickRail", new(side * .661f, .225f, 0),
                    new(.028f, .09f, 4.56f), RetailPropDetails.Steel);
            }
            for (int end = -1; end <= 1; end += 2)
            {
                RetailPropDetails.Box(root, "EndPanel", new(0, .565f, end * 2.296f),
                    new(1.3f, .61f, .032f), RetailPropDetails.Wood, "wood");
                RetailPropDetails.Box(root, "LabelChannel", new(0, .85f, end * 2.32f),
                    new(.58f, .12f, .04f), RetailPropDetails.Iron);
                RetailPropDetails.Box(root, "LabelInsert", new(0, .85f, end * 2.343f),
                    new(.51f, .075f, .012f), RetailPropDetails.Cream, "plaster");
            }
        }

        static void Checkout(Transform root)
        {
            RetailPropDetails.Box(root, "CheckoutPlinth", new(.3f, .09f, 0),
                new(1.96f, .18f, .78f), RetailPropDetails.Iron);
            RetailPropDetails.Box(root, "CheckoutCarcass", new(.3f, .6f, 0),
                new(2.1f, .84f, .9f), RetailPropDetails.Sage);
            RetailPropDetails.Box(root, "CheckoutStockDeck", new(.3f, 1.06f, .05f),
                new(2.2f, .08f, 1.1f), RetailPropDetails.Wood, "wood");
            RetailPropDetails.Box(root, "FrontFascia", new(.3f, .58f, -.464f),
                new(2.04f, .68f, .028f), RetailPropDetails.Wood, "wood");
            RetailPropDetails.Box(root, "FrontKickPlate", new(.3f, .225f, -.463f),
                new(2.04f, .09f, .026f), RetailPropDetails.Steel);
            for (int side = -1; side <= 1; side += 2)
                RetailPropDetails.Box(root, "SidePanel", new(.3f + side * 1.063f, .59f, 0),
                    new(.026f, .68f, .84f), RetailPropDetails.Wood, "wood");
            for (int drawer = 0; drawer < 2; drawer++)
            {
                float x = -.205f + drawer * 1.01f;
                RetailPropDetails.Box(root, "CashierCabinet", new(x, .54f, .463f),
                    new(.98f, .58f, .026f), RetailPropDetails.Wood, "wood");
                RetailPropDetails.Box(root, "DrawerFace", new(x, .93f, .463f),
                    new(.98f, .16f, .026f), RetailPropDetails.Sage);
                RetailPropDetails.Handle(root, new(x, .925f, .503f));
            }
        }
    }
}
