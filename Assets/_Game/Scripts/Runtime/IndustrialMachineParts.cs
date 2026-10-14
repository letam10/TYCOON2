using UnityEngine;

namespace Tycoon
{
    public static class IndustrialMachineParts
    {
        public const string Steel = "#B9C6C6";
        public const string Iron = "#485859";
        public const string Sage = "#638C7C";
        public const string Cream = "#E5DDC3";
        public const string Copper = "#B98357";
        public const string Rubber = "#29393A";

        public static Transform Box(Transform t, string name, Vector3 p, Vector3 size,
            string color = Steel, string surface = "metal", Vector3 angle = default)
        {
            var part = TownModelParts.Box(name, p, size, surface, color, t).transform;
            part.localRotation = Quaternion.Euler(angle);
            return part;
        }

        public static Transform Cylinder(Transform t, string name, Vector3 p, float diameter,
            float height, string color = Steel, Vector3 angle = default, string surface = "metal")
        {
            var part = TownModelParts.Cylinder(name, p, new(diameter, height * .5f, diameter),
                surface, color, t).transform;
            part.localRotation = Quaternion.Euler(angle);
            return part;
        }

        public static Transform Pipe(Transform t, string name, Vector3 a, Vector3 b,
            float diameter = .06f, string color = Steel)
        {
            var part = Cylinder(t, name, (a + b) * .5f, diameter, Vector3.Distance(a, b), color);
            part.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            return part;
        }

        public static void Frame(Transform t, float width = 1.8f, float depth = 1.35f, float top = .42f)
        {
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    var p = new Vector3(x * (width * .5f - .12f), .075f, z * (depth * .5f - .12f));
                    Cylinder(t, "IsolationFoot", p, .19f, .15f, Rubber, surface: "plaster");
                    Box(t, "FrameLeg", new(p.x, (top + .12f) * .5f, p.z),
                        new(.11f, top - .12f, .11f), Iron);
                }
            }
            Box(t, "LoadBearingBed", new(0, top, 0), new(width, .12f, depth), Iron);
        }

        public static void Controls(Transform t, Vector3 at, string accent = Sage)
        {
            Box(t, "ControlHousing", at, new(.34f, .4f, .12f), accent);
            Box(t, "MeterBezel", at + new Vector3(0, .08f, -.067f), new(.24f, .14f, .025f), Rubber);
            Box(t, "MeterGlass", at + new Vector3(0, .08f, -.083f),
                new(.18f, .08f, .012f), "#9CC6BE", "glass");
            for (int i = 0; i < 2; i++)
                Cylinder(t, i == 0 ? "StartButton" : "StopButton",
                    at + new Vector3(-.075f + i * .15f, -.1f, -.09f), .055f, .028f,
                    i == 0 ? Sage : "#B46C55", new(90, 0, 0));
        }

        public static void Motor(Transform t, Vector3 at, float size = 1)
        {
            Cylinder(t, "MotorCase", at, .28f * size, .42f * size, Iron, new(90, 0, 0));
            for (int i = 0; i < 5; i++)
                Cylinder(t, "MotorCoolingFin", at + Vector3.forward * ((i - 2) * .065f * size),
                    .32f * size, .018f * size, Steel, new(90, 0, 0));
            Box(t, "MotorMount", at + Vector3.down * (.16f * size),
                new Vector3(.36f, .06f, .42f) * size, Iron);
        }

        public static void Panel(Transform t, Vector3 at, Vector2 size, string color = Sage)
        {
            Box(t, "ServicePanel", at, new(size.x, size.y, .045f), color);
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    Cylinder(t, "CaptiveBolt", at + new Vector3(x * (size.x * .5f - .055f),
                        y * (size.y * .5f - .055f), -.035f), .035f, .02f, Steel, new(90, 0, 0));
            for (int i = 0; i < 4; i++)
                Box(t, "VentSlot", at + new Vector3(0, -.09f + i * .055f, -.025f),
                    new(size.x * .56f, .022f, .012f), Rubber);
        }

        public static Transform Rotor(MachineStation station, Transform root, Vector3 p,
            Vector3 angle = default)
        {
            var rotor = TownModelParts.Root("Rotor", p, root);
            rotor.localRotation = Quaternion.Euler(angle);
            station.Rotor = rotor;
            return rotor;
        }

        public static void Wheel(Transform rotor, float diameter, string color = Iron)
        {
            IndustrialMachineVessels.Ring(rotor, "FlywheelRim", Vector3.zero,
                diameter * .5f, diameter * .5f - .055f, .06f, color);
            Cylinder(rotor, "FlywheelHub", Vector3.zero, .15f, .12f, Steel);
            for (int i = 0; i < 4; i++)
                Box(rotor, "FlywheelSpoke", Vector3.zero, new(.045f, .035f, diameter - .06f),
                    color, angle: new(0, i * 45, 0));
        }
    }
}
