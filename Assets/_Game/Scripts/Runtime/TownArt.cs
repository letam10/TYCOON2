using UnityEngine;

namespace Tycoon
{
    // Model thủ công dùng chung palette; không sở hữu hàng hay tác động gameplay.
    public static class TownArt
    {
        public static bool Supports(string key) => key is "corn_plant" or "soybean_plant" or "corn" or "soybean" or "animal_feed" or "soy_sauce" or "bottled_milk" or "wool" or "yarn" or "cloth" or "bread_dough" or "cake_batter" or "beef_soy" or "corn_soup" or "pasta" or "egg_sandwich" or "soy_vegetables" or "user_sheep" or "feedmill" or "soyextractor" or "milkbottler" or "spinner" or "loom" or "breadmixer" or "cakemixer";
        const string Cream = "#F4EBD6", Wood = "#A07B55", Sage = "#79A48E", Metal = "#CDD9D0", Dark = "#365B55";
        static Mesh roundMesh, cornKernelMesh, warpMesh;

        public static Vector3 Size(string key) => key switch
        {
            "corn_plant" => new(1, 1.65f, 1), "soybean_plant" => new(.75f, .72f, .75f),
            "user_sheep" => new(.98f, 1.45f, 1.72f),
            "feedmill" or "soyextractor" or "milkbottler" or "spinner" or "loom" or "breadmixer" or "cakemixer" => new(1.65f, 1.65f, 1.3f),
            "corn" => new(.27f, .63f, .27f), "soybean" => new(.39f, .22f, .3f),
            "bottled_milk" or "soy_sauce" => new(.25f, .66f, .25f),
            "cloth" => new(.48f, .18f, .32f), "yarn" => new(.42f, .4f, .42f),
            "wool" => new(.55f, .4f, .4f), "animal_feed" => new(.42f, .5f, .35f),
            "bread_dough" or "cake_batter" => new(.48f, .28f, .48f),
            _ => new(.6f, .3f, .6f)
        };

        // Dùng một mesh ít đỉnh cho mọi hạt ngô và cụm len, tránh tạo hình cầu quá nặng.
        static GameObject Ball(string name, Vector3 point, Vector3 size, string color, Transform parent)
        {
            if (!roundMesh)
            {
                const int sides = 10, rings = 6;
                var vertices = new Vector3[(rings + 1) * (sides + 1)];
                var normals = new Vector3[vertices.Length];
                var uv = new Vector2[vertices.Length];
                var triangles = new int[rings * sides * 6];
                for (int y = 0; y <= rings; y++)
                    for (int x = 0; x <= sides; x++)
                    {
                        float latitude = Mathf.PI * y / rings, longitude = 2 * Mathf.PI * x / sides;
                        int index = y * (sides + 1) + x;
                        normals[index] = new(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                        vertices[index] = normals[index] * .5f; uv[index] = new((float)x / sides, 1 - (float)y / rings);
                    }
                int triangle = 0;
                for (int y = 0; y < rings; y++)
                    for (int x = 0; x < sides; x++)
                    {
                        int a = y * (sides + 1) + x, b = a + sides + 1;
                        triangles[triangle++] = a; triangles[triangle++] = a + 1; triangles[triangle++] = b;
                        triangles[triangle++] = a + 1; triangles[triangle++] = b + 1; triangles[triangle++] = b;
                    }
                roundMesh = new Mesh { name = "TownSoftShape", vertices = vertices, normals = normals, uv = uv, triangles = triangles };
                roundMesh.RecalculateBounds();
            }
            return MeshPart(name, roundMesh, point, size, color, parent);
        }

        static GameObject MeshPart(string name, Mesh mesh, Vector3 point, Vector3 size, string color, Transform parent)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            root.transform.localPosition = point; root.transform.localScale = size;
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = Art.Material(color);
            return root;
        }

        static void Stripes(string name, Vector3 point, Vector3 size, string color, Transform parent)
        {
            if (!warpMesh)
            {
                const int count = 8;
                var vertices = new Vector3[count * 4]; var normals = new Vector3[vertices.Length];
                var triangles = new int[count * 6];
                for (int i = 0; i < count; i++)
                {
                    float x = -.5f + (i + .5f) / count; int v = i * 4, index = i * 6;
                    vertices[v] = new(x - .012f, -.5f, 0); vertices[v + 1] = new(x - .012f, .5f, 0);
                    vertices[v + 2] = new(x + .012f, .5f, 0); vertices[v + 3] = new(x + .012f, -.5f, 0);
                    for (int n = 0; n < 4; n++) normals[v + n] = Vector3.back;
                    triangles[index] = v; triangles[index + 1] = v + 1; triangles[index + 2] = v + 2;
                    triangles[index + 3] = v; triangles[index + 4] = v + 2; triangles[index + 5] = v + 3;
                }
                warpMesh = new Mesh { name = "TownFineStripes", vertices = vertices, normals = normals, triangles = triangles };
                warpMesh.RecalculateBounds();
            }
            var detail = MeshPart(name, warpMesh, point, size, color, parent).GetComponent<Renderer>();
            detail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; detail.receiveShadows = false;
        }

        public static GameObject Build(string key)
        {
            var root = new GameObject(key); var parent = root.transform;
            if (key is "corn_plant" or "soybean_plant") Plant(key, parent);
            else if (key == "user_sheep") Sheep(parent);
            else if (key is "feedmill" or "soyextractor" or "milkbottler" or "spinner" or "loom" or "breadmixer" or "cakemixer") Machine(key, parent);
            else if (key is "beef_soy" or "corn_soup" or "pasta" or "egg_sandwich" or "soy_vegetables") Meal(key, parent);
            else Product(key, parent);
            return root;
        }

        static void Plant(string key, Transform parent)
        {
            bool corn = key == "corn_plant"; float height = corn ? 1.6f : .68f;
            Art.Cylinder("Stem", new(0, height * .5f, 0), new(.045f, height * .5f, .045f), "#6E9652", parent);
            for (int i = 0; i < (corn ? 5 : 6); i++)
            {
                float yaw = i * 137.5f * Mathf.Deg2Rad;
                var leaf = Ball("Leaf", new(Mathf.Cos(yaw) * .16f, .19f + i * height * .13f, Mathf.Sin(yaw) * .16f),
                    corn ? new(.12f, .05f, .65f) : new(.22f, .06f, .32f), i % 2 == 0 ? "#83AD5C" : "#659347", parent);
                leaf.transform.localRotation = Quaternion.Euler(corn ? -25 : -12, -i * 137.5f + 90, 0);
            }
            if (corn)
            {
                Art.Model("corn", new(.15f, .55f, 0), parent, .45f).transform.localRotation = Quaternion.Euler(0, 35, -15);
                Art.Model("corn", new(-.12f, .85f, 0), parent, .4f).transform.localRotation = Quaternion.Euler(0, 180, 18);
                for (int i = 0; i < 3; i++) Ball("Tassel", new((i - 1) * .055f, 1.55f, 0), new(.035f, .16f, .035f), "#D6BD6F", parent);
            }
            else for (int i = 0; i < 4; i++)
                Ball("Pod", new(i % 2 == 0 ? -.16f : .16f, .22f + i * .08f, .03f), new(.1f, .19f, .09f), "#B5BC71", parent);
        }

        static void Sheep(Transform parent)
        {
            Ball("Body", new(0, .85f, 0), new(.84f, .76f, 1.15f), Cream, parent);
            for (int i = 0; i < 7; i++) Ball("Fleece", new(Mathf.Sin(i * 2.4f) * .25f, 1 + i % 2 * .1f, (i / 2 - 1.5f) * .23f), Vector3.one * .39f, "#F9F1E2", parent);
            Ball("Head", new(0, 1.08f, .67f), new(.37f, .47f, .45f), "#6F6558", parent);
            Ball("Muzzle", new(0, .99f, .87f), new(.29f, .22f, .22f), "#8F7D68", parent);
            Ball("Fringe", new(0, 1.29f, .63f), new(.4f, .23f, .32f), Cream, parent);
            Ball("Tail", new(0, .95f, -.64f), new(.16f, .3f, .18f), Cream, parent);
            for (int i = 0; i < 4; i++)
            {
                Vector3 point = new(i % 2 == 0 ? -.27f : .27f, .3f, i < 2 ? -.35f : .35f);
                Art.Cylinder("Leg", point, new(.13f, .25f, .13f), "#7B705E", parent);
                Ball("Hoof", new(point.x, .08f, point.z), new(.18f, .15f, .2f), Dark, parent);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Ball("Ear", new(side * .24f, 1.13f, .64f), new(.25f, .11f, .15f), "#857462", parent);
                Ball("Eye", new(side * .145f, 1.17f, .854f), Vector3.one * .068f, "#242E29", parent);
                Ball("EyeGlint", new(side * .15f, 1.185f, .88f), Vector3.one * .018f, Cream, parent);
            }
        }

        static Transform Rotor(Transform parent, Vector3 point, bool sideways = false)
        {
            var root = new GameObject("Rotor").transform; root.SetParent(parent, false); root.localPosition = point;
            if (sideways) root.localRotation = Quaternion.Euler(0, 0, 90);
            return root;
        }

        static void Machine(string key, Transform parent)
        {
            bool textile = key is "spinner" or "loom";
            string accent = key == "cakemixer" ? "#CF948B" : key == "feedmill" ? "#B5A16B" : textile ? Wood : Sage;
            Art.Box("MachineBase", new(0, .12f, 0), new(1.6f, .24f, 1.2f), textile ? Wood : Dark, parent);
            Stripes("BaseVent", new(0, .13f, -.605f), new(.58f, .09f, 1), "#9BAE9E", parent);
            for (int side = -1; side <= 1; side += 2)
            {
                Art.Box("Foot", new(side * .57f, .05f, -.4f), new(.18f, .1f, .18f), Dark, parent);
                Art.Box("Foot", new(side * .57f, .05f, .4f), new(.18f, .1f, .18f), Dark, parent);
            }
            if (key == "loom")
            {
                for (int side = -1; side <= 1; side += 2) Art.Box("Frame", new(side * .68f, .84f, .17f), new(.15f, 1.32f, .18f), Wood, parent);
                Art.Box("TopBeam", new(0, 1.51f, .17f), new(1.5f, .15f, .18f), Wood, parent);
                Art.Box("WovenCloth", new(0, .71f, -.17f), new(1.12f, .07f, .66f), "#80B3B8", parent);
                Stripes("WarpThreads", new(0, 1.12f, .128f), new(1.12f, .65f, 1), Cream, parent);
                var roll = Rotor(parent, new(0, .72f, -.49f), true);
                Art.Cylinder("FabricRoll", Vector3.zero, new(.24f, .61f, .24f), "#80B3B8", roll);
                Art.Cylinder("RollCap", new(0, .64f, 0), new(.28f, .035f, .28f), Wood, roll);
                Art.Box("Shuttle", new(.36f, .8f, -.15f), new(.34f, .06f, .08f), Cream, parent);
            }
            else if (key == "spinner")
            {
                Art.Box("DrivePost", new(.51f, .66f, .1f), new(.18f, 1.04f, .2f), Wood, parent);
                var wheel = Rotor(parent, new(-.17f, .79f, -.05f), true);
                Art.Cylinder("Wheel", Vector3.zero, new(.9f, .04f, .9f), Wood, wheel);
                Art.Cylinder("WheelFace", new(0, -.045f, 0), new(.73f, .01f, .73f), "#C6AF82", wheel);
                Art.Box("Spoke", new(0, -.064f, 0), new(.77f, .027f, .055f), Cream, wheel);
                Art.Box("Spoke", new(0, -.065f, 0), new(.055f, .027f, .77f), Cream, wheel);
                Art.Cylinder("Axle", Vector3.zero, new(.16f, .11f, .16f), Dark, wheel);
                Art.Model("yarn", new(.5f, 1.07f, .08f), parent, .7f);
                Art.Box("Pedal", new(0, .3f, -.48f), new(.57f, .09f, .22f), "#C6AF82", parent);
            }
            else if (key == "feedmill")
            {
                Art.Box("MillBody", new(0, .57f, .11f), new(1.14f, .68f, .69f), accent, parent);
                Art.Cylinder("Hopper", new(0, 1.15f, .18f), new(.88f, .26f, .88f), Metal, parent);
                Art.Cylinder("Grain", new(0, 1.417f, .18f), new(.73f, .013f, .73f), "#D4BA73", parent);
                var screw = Rotor(parent, new(0, 1.43f, .18f));
                for (int i = 0; i < 3; i++) Art.Box("HopperBlade", new(0, .015f, 0), new(.65f, .035f, .075f), Dark, screw).transform.localRotation = Quaternion.Euler(0, i * 60, 0);
                Art.Box("OutputChute", new(0, .58f, -.44f), new(.4f, .24f, .37f), Metal, parent);
                Art.Box("ChuteLip", new(0, .475f, -.595f), new(.44f, .045f, .055f), Cream, parent);
                Art.Model("animal_feed", new(0, .23f, -.35f), parent, .62f);
            }
            else if (key == "soyextractor")
            {
                Art.Box("PressFrame", new(0, .83f, .3f), new(1.2f, 1.16f, .37f), Sage, parent);
                Art.Cylinder("PressTank", new(0, .75f, -.16f), new(.82f, .3f, .82f), Metal, parent);
                Art.Box("PressArm", new(0, 1.39f, -.1f), new(1.2f, .15f, .74f), Cream, parent);
                Art.Box("PressGauge", new(-.4f, 1.42f, -.485f), new(.23f, .105f, .035f), Dark, parent);
                Stripes("GaugeMarks", new(-.4f, 1.42f, -.508f), new(.17f, .055f, 1), Cream, parent);
                Art.Cylinder("Piston", new(0, 1.15f, -.16f), new(.18f, .24f, .18f), Dark, parent);
                var press = Rotor(parent, new(0, 1.54f, .24f));
                Art.Box("PressHandle", Vector3.zero, new(.62f, .065f, .07f), Dark, press);
                Art.Cylinder("Spout", new(.49f, .58f, -.24f), new(.09f, .17f, .09f), Metal, parent).transform.localRotation = Quaternion.Euler(0, 0, 90);
                Art.Model("soy_sauce", new(.65f, .24f, -.22f), parent, .65f);
            }
            else if (key == "milkbottler")
            {
                Art.Box("FillingCabinet", new(0, .91f, .34f), new(1.25f, 1.32f, .43f), Cream, parent);
                Art.Box("MilkStripe", new(0, 1.31f, .113f), new(1.17f, .13f, .032f), Sage, parent);
                Art.Box("FillingWindow", new(0, 1.07f, .111f), new(.93f, .32f, .033f), "#A7C3BE", parent);
                Art.Box("Conveyor", new(0, .52f, -.22f), new(1.4f, .16f, .58f), Dark, parent);
                var carousel = Rotor(parent, new(0, .63f, -.14f));
                Art.Cylinder("Carousel", Vector3.zero, new(.87f, .05f, .87f), Metal, carousel);
                for (int i = 0; i < 2; i++)
                {
                    Art.Model("bottled_milk", new(-.32f + i * .64f, .69f, -.14f), parent, .68f);
                    Art.Cylinder("Nozzle", new(-.32f + i * .64f, 1.17f, -.14f), new(.07f, .14f, .07f), Metal, parent);
                }
            }
            else
            {
                Art.Box("MixerPost", new(.47f, .86f, .19f), new(.32f, 1.23f, .43f), accent, parent);
                Ball("MixerArm", new(.05f, 1.43f, -.02f), new(1.1f, .31f, .5f), Cream, parent);
                Art.Cylinder("MixingBowl", new(-.15f, .73f, -.15f), new(.85f, .23f, .85f), Metal, parent);
                Art.Cylinder("Mix", new(-.15f, .965f, -.15f), new(.73f, .02f, .73f), key == "cakemixer" ? "#E5C790" : "#EED8AD", parent);
                var whisk = Rotor(parent, new(-.15f, 1.14f, -.15f));
                Art.Cylinder("Spindle", Vector3.zero, new(.07f, .19f, .07f), Dark, whisk);
                for (int i = 0; i < 3; i++) Art.Box("Whisk", new(0, -.15f, 0), new(.3f, .05f, .03f), Metal, whisk).transform.localRotation = Quaternion.Euler(0, i * 60, 0);
                Art.Box("SpeedDial", new(.47f, 1.16f, -.038f), new(.15f, .15f, .045f), Dark, parent);
            }
            if (!textile)
            {
                Art.Box("ControlPlate", new(.56f, .48f, -.49f), new(.24f, .27f, .055f), Dark, parent);
                Ball("StartButton", new(.55f, .53f, -.53f), Vector3.one * .07f, "#A4C788", parent);
                Ball("Dial", new(.55f, .42f, -.53f), Vector3.one * .07f, Cream, parent);
            }
        }

        static void Product(string key, Transform parent)
        {
            if (key == "corn")
            {
                Ball("Cob", new(0, .31f, 0), new(.22f, .58f, .22f), "#E7BF58", parent);
                // Cả cụm hạt dùng một renderer và một mesh cache thay vì 16 object.
                if (!cornKernelMesh)
                {
                    var kernels = new CombineInstance[16];
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 4; j++)
                        {
                            Vector3 point = new(Mathf.Cos(j * Mathf.PI * .5f) * .088f, .13f + i * .115f, Mathf.Sin(j * Mathf.PI * .5f) * .088f);
                            kernels[i * 4 + j] = new CombineInstance { mesh = roundMesh,
                                transform = Matrix4x4.TRS(point, Quaternion.identity, new(.072f, .09f, .072f)) };
                        }
                    cornKernelMesh = new Mesh { name = "TownCornKernels" }; cornKernelMesh.CombineMeshes(kernels, true, true);
                }
                MeshPart("KernelCluster", cornKernelMesh, Vector3.zero, Vector3.one, "#F7D775", parent);
                Ball("Husk", new(.04f, .12f, .055f), new(.16f, .29f, .11f), "#A8B876", parent).transform.localRotation = Quaternion.Euler(-18, 0, -15);
            }
            else if (key == "soybean")
                for (int i = 0; i < 5; i++) Ball("Bean", new((i % 3 - 1) * .12f, .06f + i / 3 * .07f, (i / 3 - .5f) * .13f), new(.14f, .11f, .1f), i % 2 == 0 ? "#D2CDA2" : "#BDBB8B", parent);
            else if (key is "soy_sauce" or "bottled_milk")
            {
                bool milk = key == "bottled_milk";
                Art.Cylinder("Bottle", new(0, .25f, 0), new(.24f, .22f, .24f), milk ? "#F0EDE1" : "#79523B", parent);
                Ball("Shoulder", new(0, .46f, 0), new(.24f, .18f, .24f), milk ? "#F0EDE1" : "#79523B", parent);
                Art.Cylinder("Neck", new(0, .56f, 0), new(.115f, .065f, .115f), milk ? Cream : "#79523B", parent);
                Art.Cylinder("Cap", new(0, .635f, 0), new(.14f, .022f, .14f), milk ? Sage : "#C48A65", parent);
                Art.Box("Label", new(0, .28f, -.123f), new(.16f, .17f, .015f), Cream, parent);
                Ball("BrandMark", new(0, .29f, -.14f), new(.055f, .065f, .012f), milk ? Sage : "#AF8353", parent);
            }
            else if (key == "cloth")
            {
                for (int i = 0; i < 3; i++) Art.Box("Fold", new(i % 2 * .018f, .03f + i * .055f, 0), new(.46f, .05f, .3f), i % 2 == 0 ? "#91BBC0" : "#73A0AA", parent);
                Art.Box("PaperWrap", new(0, .153f, 0), new(.12f, .014f, .32f), Cream, parent);
            }
            else if (key == "yarn")
            {
                Art.Cylinder("Thread", new(0, .2f, 0), new(.37f, .15f, .37f), "#DDBF8E", parent);
                for (int i = 0; i < 4; i++) Art.Cylinder("Winding", new(0, .085f + i * .073f, 0), new(.395f, .013f, .395f), "#EBD3A7", parent);
                Art.Cylinder("Core", new(0, .2f, 0), new(.09f, .2f, .09f), Wood, parent);
            }
            else if (key == "animal_feed")
            {
                Ball("FeedSack", new(0, .23f, 0), new(.4f, .44f, .33f), "#C9B37F", parent);
                Ball("SackNeck", new(0, .44f, 0), new(.22f, .11f, .21f), "#DFC999", parent);
                Art.Box("SackLabel", new(0, .26f, -.155f), new(.18f, .15f, .028f), Cream, parent);
                Ball("LeafMark", new(0, .27f, -.174f), new(.065f, .1f, .012f), Sage, parent);
            }
            else if (key == "wool")
                for (int i = 0; i < 4; i++) Ball("FleeceBundle", new((i % 2 - .5f) * .23f, .16f + i / 2 * .1f, (i / 2 - .5f) * .12f), new(.31f, .27f, .26f), i % 2 == 0 ? Cream : "#FCF3E1", parent);
            else
            {
                Art.Cylinder("Bowl", new(0, .09f, 0), new(.46f, .09f, .46f), key == "cake_batter" ? Sage : "#C6B48F", parent);
                Ball("Mixture", new(0, .18f, 0), new(.4f, .17f, .4f), key == "cake_batter" ? "#E8C58B" : "#F0DBAF", parent);
                if (key == "cake_batter") Art.Box("Spatula", new(.13f, .21f, .09f), new(.055f, .03f, .33f), Wood, parent).transform.localRotation = Quaternion.Euler(-25, 25, 0);
            }
        }

        static void Meal(string key, Transform parent)
        {
            Art.Cylinder("PlateRim", new(0, .035f, 0), new(.57f, .025f, .57f), Cream, parent);
            Art.Cylinder("Plate", new(0, .06f, 0), new(.46f, .014f, .46f), "#FFFAE9", parent);
            if (key == "corn_soup")
            {
                Art.Cylinder("Bowl", new(0, .12f, 0), new(.4f, .08f, .4f), Sage, parent);
                Art.Cylinder("Soup", new(0, .202f, 0), new(.34f, .008f, .34f), "#EACB79", parent);
                for (int i = 0; i < 3; i++) Ball("Corn", new(-.09f + i * .09f, .216f, 0), new(.045f, .025f, .045f), "#F3D47A", parent);
                Ball("Herb", new(.03f, .22f, .07f), new(.055f, .015f, .045f), "#73A35F", parent);
            }
            else if (key == "egg_sandwich")
            {
                Art.Model("bread", new(0, .07f, 0), parent, .6f);
                Ball("EggWhite", new(0, .18f, 0), new(.29f, .04f, .22f), "#FFF8E6", parent);
                Ball("Yolk", new(0, .209f, 0), new(.12f, .04f, .12f), "#E9BE50", parent);
                Ball("Lettuce", new(.07f, .14f, -.08f), new(.24f, .03f, .11f), "#8CAF63", parent);
            }
            else if (key == "pasta")
            {
                for (int i = 0; i < 7; i++) Art.Box("Noodle", new(-.11f + i * .035f, .095f, 0), new(.024f, .04f, .27f), "#E7CA8D", parent).transform.localRotation = Quaternion.Euler(0, (i % 3 - 1) * 12, 0);
                Ball("Sauce", new(0, .143f, 0), new(.24f, .055f, .2f), "#B66C51", parent);
                Ball("Cheese", new(0, .18f, 0), new(.1f, .02f, .12f), "#F0E3B8", parent);
                Ball("Basil", new(.08f, .18f, .04f), new(.06f, .02f, .055f), "#6D9A58", parent);
            }
            else if (key == "beef_soy")
            {
                Art.Model("beef", new(0, .075f, 0), parent, .5f);
                for (int i = 0; i < 4; i++) Ball("Carrot", new((i % 2 - .5f) * .28f, .11f, (i / 2 - .5f) * .25f), new(.075f, .045f, .07f), "#D89253", parent);
                Ball("Sauce", new(.04f, .13f, .04f), new(.2f, .02f, .13f), "#7F5637", parent);
            }
            else for (int i = 0; i < 6; i++) Ball("Vegetable", new((i % 3 - 1) * .11f, .11f, (i / 3 - .5f) * .16f), new(.15f, .12f, .15f), i % 2 == 0 ? "#8AAF69" : "#D7C790", parent);
        }
    }
}
