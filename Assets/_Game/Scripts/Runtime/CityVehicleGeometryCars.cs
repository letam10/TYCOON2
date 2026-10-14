using UnityEngine;

namespace Tycoon
{
    public static class CityVehicleGeometryCars
    {
        internal const float DoorRear = -.88f;
        internal const float DoorFront = .18f;
        internal const float CabinRear = -1.03f;
        internal const float CabinFront = .88f;
        internal const float RoofRear = -.945f;
        internal const float RoofFront = .37f;

        public static void Build(Transform root, CityVehicleRig rig, string color)
        {
            bool sports = rig.Kind == CityVehicleKind.Sports;
            bool luxury = rig.Kind == CityVehicleKind.Luxury;
            bool taxi = rig.Kind == CityVehicleKind.Taxi;
            float length = luxury ? 5.05f : sports ? 4.35f : taxi ? 4.2f : 4.4f;
            float width = luxury || sports ? 1.94f : 1.85f;
            float roof = sports ? 1.15f : luxury ? 1.55f : 1.49f;
            float belt = sports ? .69f : .88f;
            float radius = sports || luxury ? .36f : .33f;
            float axle = length * .31f;
            float half = width * .5f;
            var paint = CityVehicleSurface.Paint(taxi ? "#E7B92D" : color);
            var g = new CityVehicleGeometry();
            g.Box(CityVehicleSurface.Dark, new(0, .25f, 0), new(width - .18f, .12f, length - .18f));
            Hood(g, paint, half, belt, length, radius, CabinFront, length * .5f);
            Hood(g, paint, half, belt, length, radius, -length * .5f, CabinRear);
            Roof(g, paint, half, roof);
            CityVehicleParts.Windshield(g, half * .9f, half * .77f,
                belt + .035f, roof - .045f, CabinFront, RoofFront);
            CityVehicleParts.Windshield(g, -half * .9f, -half * .77f,
                belt + .035f, roof - .045f, CabinRear, RoofRear);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * half;
                Side(g, paint, x, belt, length, axle, radius);
                QuarterWindow(g, paint, x, belt, roof, DoorFront, CabinFront, DoorFront, RoofFront);
                QuarterWindow(g, paint, x, belt, roof, CabinRear, DoorRear, RoofRear, DoorRear);
                g.Box(paint, new(x * .91f, .29f, -.35f), new(half * .11f, .04f, 1.11f));
                CityVehicleParts.Mirror(g, x + side * .055f, belt + .2f, .65f);
                g.Box(CityVehicleSurface.Red, new(x * .76f, belt - .1f, -length * .5f + .012f),
                    new(.32f, .12f, .05f));
                g.Box(CityVehicleSurface.Chrome, new(x * .76f, .31f, -length * .5f - .012f),
                    new(.15f, .085f, .1f));
                foreach (float z in new[] { -.72f, .4f })
                    CityVehicleParts.Seat(g, new(side * .43f, .3f, z), luxury, .54f);
            }
            g.Box(CityVehicleSurface.Dark, new(0, belt - .1f, .74f), new(width * .85f, .18f, .3f));
            g.Cylinder(CityVehicleSurface.Dark, new(-.44f, belt + .08f, .52f), .16f, .055f);
            CityVehicleParts.Front(g, width, length * .5f - .007f, belt - .3f, luxury);
            if (sports)
            {
                g.Box(CityVehicleSurface.Dark, new(0, .28f, length * .5f - .1f), new(width, .055f, .3f));
                g.Box(CityVehicleSurface.Dark, new(0, .9f, -length * .43f), new(width * .86f, .06f, .23f));
                for (int side = -1; side <= 1; side += 2)
                {
                    g.Box(CityVehicleSurface.Dark, new(side * .61f, .77f, -length * .43f), new(.055f, .25f, .12f));
                    g.Box(CityVehicleSurface.Dark, new(side * half, .59f, -1.05f), new(.035f, .15f, .18f));
                }
            }
            if (taxi)
            {
                g.Box(CityVehicleSurface.Dark, new(0, roof + .065f, -.15f), new(.6f, .04f, .28f));
                g.Box(CityVehicleSurface.Light, new(0, roof + .19f, -.15f), new(.48f, .22f, .21f), .035f);
                for (int i = -2; i <= 2; i++)
                    g.Box(CityVehicleSurface.Dark, new(i * .067f, roof + .19f, -.039f), new(.03f, .1f, .01f));
            }
            g.Build("SculptedCarHull", root, rig);
            rig.Wheels = CityVehicleParts.Wheels(root, rig, width, radius, -axle, axle);
            rig.DoorInsideLocal = new(half - .06f, .2f, -.35f);
            rig.RightDoors = new[] { Door(1) };
            rig.LeftDoors = new[] { Door(-1) };
            CityVehicleParts.Collider(root, new(0, roof * .5f + .13f, 0), new(width, roof - .2f, length));

            Transform Door(int side) => CityVehicleParts.CarDoor(root, rig, side, half, length, belt, roof, paint);
        }

        internal static float BodyWidth(float half, float length, float z)
        {
            return half * (1 - .12f * EndTaper(length, z));
        }

        internal static float BeltY(float belt, float length, float z) => belt - .2f * EndTaper(length, z);

        static float ArchBottom(float length, float radius, float z)
        {
            float distance = Mathf.Abs(Mathf.Abs(z) - length * .31f);
            float r = radius + .07f;
            return distance < r ? Mathf.Max(.3f, radius + Mathf.Sqrt(r * r - distance * distance)) : .3f;
        }

        static float EndTaper(float length, float z)
        {
            float t = z > CabinFront ? Mathf.InverseLerp(CabinFront, length * .5f, z) :
                z < CabinRear ? Mathf.InverseLerp(CabinRear, -length * .5f, z) : 0;
            return t * t * (3 - 2 * t);
        }

        internal static Vector3 PanelPoint(float half, float belt, float length, float z, float y)
        {
            float top = BeltY(belt, length, z);
            float t = Mathf.Clamp01((y - .3f) / (top - .3f));
            float width = BodyWidth(half, length, z) * (.955f + .045f * Mathf.Sin(t * Mathf.PI * .5f));
            return new(width, y, z);
        }

        static void Hood(CityVehicleGeometry g, Material paint, float half, float belt,
            float length, float radius, float start, float end)
        {
            float[] across = { -1, -.9f, -.75f, -.5f, 0, .5f, .75f, .9f, 1 };
            for (int ring = 0; ring < 24; ring++)
            {
                float za = Mathf.Lerp(start, end, ring / 24f);
                float zb = Mathf.Lerp(start, end, (ring + 1) / 24f);
                for (int strip = 0; strip < across.Length - 1; strip++)
                {
                    float a = across[strip];
                    float b = across[strip + 1];
                    Vector3 Point(float z, float s) => new(s * BodyWidth(half, length, z),
                        Mathf.Max(BeltY(belt, length, z), ArchBottom(length, radius, z) + .035f) +
                        .035f * Mathf.Clamp01((1 - Mathf.Abs(s)) / .1f), z);
                    g.Quad(paint, Point(za, a), Point(zb, a), Point(zb, b), Point(za, b));
                }
            }
            float cap = Mathf.Abs(start) > Mathf.Abs(end) ? start : end;
            for (int strip = 0; strip < across.Length - 1; strip++)
                for (int band = 0; band < 4; band++)
                {
                    Vector3 Point(float s, float t) => new(s * BodyWidth(half, length, cap) *
                        (.955f + .045f * Mathf.Sin(t * Mathf.PI * .5f)),
                        Mathf.Lerp(.3f, BeltY(belt, length, cap) +
                            .035f * Mathf.Clamp01((1 - Mathf.Abs(s)) / .1f), t), cap);
                    var a = Point(across[strip], band / 4f);
                    var b = Point(across[strip + 1], band / 4f);
                    var c = Point(across[strip + 1], (band + 1) / 4f);
                    var d = Point(across[strip], (band + 1) / 4f);
                    g.Quad(paint, a, b, c, d);
                    g.Quad(paint, d, c, b, a);
                }
        }

        static void Roof(CityVehicleGeometry g, Material paint, float half, float y)
        {
            Vector2[] crown =
            {
                new(-half * .77f, -.045f), new(half * .77f, -.045f),
                new(half * .75f, .005f), new(half * .4f, .045f),
                new(-half * .4f, .045f), new(-half * .75f, .005f)
            };
            g.Loft(paint, new(0, y, 0), new[] { RoofRear, RoofFront }, crown, new[] { 1f, 1f });
        }

        static void QuarterWindow(CityVehicleGeometry g, Material paint, float x, float belt, float roof,
            float bottomRear, float bottomFront, float topRear, float topFront)
        {
            g.Quad(CityVehicleSurface.Glass, new(x * .9f, belt + .035f, bottomRear),
                new(x * .9f, belt + .035f, bottomFront), new(x * .77f, roof - .045f, topFront),
                new(x * .77f, roof - .045f, topRear));
            g.Quad(paint, new(x, belt, bottomRear), new(x, belt, bottomFront),
                new(x * .9f, belt + .035f, bottomFront), new(x * .9f, belt + .035f, bottomRear));
            g.Quad(paint, new(x * .9f, belt + .035f, bottomRear),
                new(x * .9f, belt + .035f, bottomFront), new(x, belt, bottomFront), new(x, belt, bottomRear));
            g.Beam(paint, new(x * .9f, belt + .035f, bottomRear),
                new(x * .77f, roof - .045f, topRear), .04f);
            g.Beam(paint, new(x * .9f, belt + .035f, bottomFront),
                new(x * .77f, roof - .045f, topFront), .04f);
        }

        static void Side(CityVehicleGeometry g, Material paint, float x, float belt, float length,
            float axle, float radius)
        {
            // Chia đúng hai mép cửa, tránh bỏ cả đoạn lấy mẫu gây khe hở dọc thân.
            Section(-length * .5f, CabinRear, 24);
            Section(CabinRear, DoorRear, 4);
            Section(DoorFront, CabinFront, 4);
            Section(CabinFront, length * .5f, 24);
            foreach (float z in new[] { -axle, axle })
                g.Arch(CityVehicleSurface.Dark, x + Mathf.Sign(x) * .006f, z, radius, radius + .035f, .04f);

            void Section(float start, float end, int segments)
            {
                for (int i = 0; i < segments; i++)
                {
                    float a = Mathf.Lerp(start, end, i / (float)segments);
                    float b = Mathf.Lerp(start, end, (i + 1) / (float)segments);
                    for (int band = 0; band < 4; band++)
                    {
                        Vector3 Point(float z, float t)
                        {
                            float bottom = ArchBottom(length, radius, z);
                            float top = Mathf.Max(BeltY(belt, length, z), bottom + .035f);
                            return PanelPoint(x, belt, length, z, Mathf.Lerp(bottom, top, t));
                        }
                        var p0 = Point(a, band / 4f);
                        var p1 = Point(b, band / 4f);
                        var p2 = Point(b, (band + 1) / 4f);
                        var p3 = Point(a, (band + 1) / 4f);
                        g.Quad(paint, p0, p1, p2, p3);
                        g.Quad(paint, p3, p2, p1, p0);
                    }
                }
            }
        }
    }
}
