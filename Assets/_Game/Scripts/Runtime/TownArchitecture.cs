using UnityEngine;

namespace Tycoon
{
    public static class TownArchitecture
    {
        public static void Dress(Transform floor, Vector2 size, string label)
        {
            bool processing = label.Contains("CHẾ BIẾN");
            bool shop = label.Contains("NÔNG SẢN");
            bool bakery = label.Contains("TIỆM BÁNH");
            bool restaurant = label.Contains("NHÀ HÀNG");
            string accent = processing ? "#667C82" : shop ? "#63744B"
                : bakery ? "#A86743" : restaurant ? "#847151" : "#536B68";
            var root = TownModelParts.Root("DistrictArchitecture", Vector3.zero, floor);
            float x = size.x * .5f;
            float z = size.y * .5f;
            floor.Find("BusinessFloor").GetComponent<Renderer>().sharedMaterial =
                TownSurfaceMaterials.Get(shop || bakery ? "wood" : "paving", "#C3B4A0");
            foreach (Transform child in floor)
            {
                if (child.name is "BackWall" or "RightWall" or "BackTrim" or "RightTrim")
                    child.gameObject.SetActive(false);
                if (child.name is "FloorEdge" or "LeftEdge" or "BackTrim" or "RightTrim")
                    child.GetComponent<Renderer>().sharedMaterial = TownSurfaceMaterials.Get("stone", accent);
                if (child.name == "Label") child.gameObject.SetActive(false);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float width = (size.x - 4) * .5f;
                float cx = side * (2 + width * .5f);
                TownModelParts.Box("BrickPlinth", new(cx, .38f, z), new(width, .64f, .28f),
                    "brick", processing ? "#9B8873" : "#B18C73", root, solid: true);
                TownModelParts.Box("RearPlaster", new(cx, 1.85f, z), new(width, 2.3f, .22f),
                    "plaster", "#D8D2C1", root, solid: true);
                float length = (size.y - 4) * .5f;
                float cz = side * (2 + length * .5f);
                TownModelParts.Box("SidePlinth", new(x, .38f, cz), new(.28f, .64f, length),
                    "brick", processing ? "#9B8873" : "#B18C73", root, solid: true);
                TownModelParts.Box("SidePlaster", new(x, 1.85f, cz),
                    new(.22f, 2.3f, length), "plaster", "#D8D2C1", root, solid: true);
                TownModelParts.Box("SideCornice", new(x, 3.08f, cz), new(.4f, .18f, length),
                    "wood", accent, root);
                TownModelParts.Box("RearCornice", new(cx, 3.08f, z), new(width + .2f, .18f, .4f),
                    "wood", accent, root);
                TownModelParts.Box("RearRoof", new(cx, 3.36f, z - .55f), new(width + .35f, .15f, 1.8f),
                    "roof", processing ? "#636E70" : "#8C5541", root);
                Window(root, cx, z - .15f, Mathf.Min(3.2f, width - 1.1f), accent);
            }
            for (int i = 0; i < 4; i++)
            {
                float px = -x + .3f + i * (size.x - .6f) / 3;
                if (Mathf.Abs(px) < 2.2f) continue;
                TownModelParts.Box("FacadeColumn", new(px, 1.76f, z - .28f), new(.2f, 3.4f, .32f),
                    "wood", accent, root, solid: true);
            }
            // Mái hẹp ở phía sau giữ lối nhìn từ trước trái, không che máy hay hàng thật.
            TownModelParts.Box("EntryLintel", new(0, 3.08f, z), new(4.3f, .26f, .35f),
                "wood", accent, root);
            TownModelParts.Box("DistrictPlaque", new(-x + 4, 2.93f, z - .45f), new(5.9f, .65f, .16f),
                "wood", accent, root);
            var text = Art.Label(label, new(-x + 4, 2.93f, z - .54f), root, .14f, "#F8F0DE", false);
            text.transform.localRotation = Quaternion.identity;
            for (int i = 0; i < 5; i++)
            {
                TownModelParts.Box("VentGrille", new(x - 1.2f, 2.8f, z - .17f - i * .03f),
                    new(.72f, .025f, .04f), "metal", "#687170", root);
            }
            TownModelParts.Box("EntryPaving", new(0, .025f, -z - 1.35f), new(size.x, .028f, 2.5f),
                "paving", "#B8B0A0", root);
            for (int i = 0; i < 3; i++)
                TownModelParts.Box("DoorThreshold", new(-1.1f + i * 1.1f, .07f, -z),
                    new(.95f, .05f, .38f), "stone", "#8C9188", root);
            if (restaurant || bakery) Awning(root, x, z, accent);
            if (processing) IndustrialServices(root, x, z);
            TownProps.Planter(root, new(-x + .6f, 0, z - 1), accent);
            TownFixtures.Dress(floor, size, label);
        }

        static void Window(Transform root, float x, float z, float width, string accent)
        {
            TownModelParts.Box("WindowGlass", new(x, 2.06f, z), new(width, 1.15f, .035f),
                "glass", "#64848C", root);
            for (int i = -1; i <= 1; i++)
                TownModelParts.Box("WindowMullion", new(x + i * width * .5f, 2.06f, z - .035f),
                    new(.055f, 1.26f, .07f), "wood", accent, root);
            for (int i = -1; i <= 1; i += 2)
                TownModelParts.Box("WindowFrame", new(x, 2.06f + i * .61f, z - .04f),
                    new(width + .14f, .07f, .09f), "wood", accent, root);
            TownModelParts.Box("WindowSill", new(x, 1.44f, z - .11f), new(width + .25f, .12f, .32f),
                "stone", "#BBB9AE", root, solid: true);
        }

        static void Awning(Transform root, float x, float z, string accent)
        {
            var awning = TownModelParts.Root("CafeAwning", new(x - 2, 2.6f, z - 1.5f), root);
            TownModelParts.Box("CanvasAwning", Vector3.zero, new(3.6f, .12f, 1.7f),
                "roof", accent, awning);
            for (int i = 0; i < 6; i++)
                TownModelParts.Box("AwningStripe", new(-1.5f + i * .6f, -.08f, -.84f),
                    new(.3f, .3f, .055f), "plaster", "#DBD0B9", awning);
        }

        static void IndustrialServices(Transform root, float x, float z)
        {
            TownModelParts.Box("ExtractorDuct", new(x - 1.5f, 3.15f, z - .8f), new(.6f, .4f, 1.9f),
                "metal", "#A7ACA7", root);
            TownModelParts.Cylinder("RoofExhaust", new(x - 1.5f, 3.7f, z - .8f), new(.45f, .45f, .45f),
                "metal", "#969D99", root);
        }
    }
}
