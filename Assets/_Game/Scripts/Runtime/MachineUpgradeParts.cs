using UnityEngine;

namespace Tycoon
{
    public static class MachineUpgradeParts
    {
        public static void Build(Transform p, int tier, int capacity, int speed)
        {
            for (int i = 1; i < tier; i++)
                Art.Box("MachineSteelBrace", new Vector3((i == 1 ? -1 : 1) * .83f, .65f, .45f),
                    new Vector3(.09f, 1.25f, .16f), "#8FABA5", p);
            if (tier > 2)
            {
                Art.Box("ControlPanelStand", new(.6f, .55f, -.60f), new(.08f, 1.10f, .08f), "#748F93", p);
                Art.Box("DigitalControlPanel", new(.6f, 1.2f, -.61f), new(.36f, .27f, .06f), "#415653", p);
                Art.Box("ControlDisplay", new(.6f, 1.22f, -.65f), new(.25f, .13f, .02f), "#A6CCB8", p);
            }
            if (speed > 1) Motors(p, speed);
            if (capacity > 1) Trays(p, capacity);
        }

        static void Motors(Transform p, int speed)
        {
            float top = .4f + (speed - 1) * .33f + .17f;
            Art.Box("MotorMountPost", new(.7f, top * .5f, .53f), new(.10f, top, .12f), "#526E70", p);
            Art.Box("MotorMountFoot", new(.7f, .045f, .57f), new(.50f, .09f, .65f), "#526E70", p);
            for (int i = 1; i < speed; i++)
            {
                float height = .4f + i * .33f;
                Art.Box("MotorMountPlate", new(.7f, height, .53f), new(.34f, .33f, .10f), "#8FABA5", p);
                var motor = Art.Cylinder("SpeedMotor", new(.7f, height, .73f),
                    new(.3f, .21f, .3f), "#748F93", p);
                motor.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Art.Cylinder("MotorHub", new(.7f, height, .95f), new(.12f, .04f, .12f),
                    "#B6C0AF", p).transform.localRotation = Quaternion.Euler(90, 0, 0);
                var rotor = new GameObject("MotorRotor").transform;
                rotor.SetParent(p, false);
                rotor.localPosition = new(.7f, height, 1.0f);
                rotor.localRotation = Quaternion.Euler(90, 0, 0);
                for (int blade = 0; blade < 3; blade++)
                    Art.Box("FanBlade", Vector3.zero, new(.22f, .025f, .045f),
                        "#C0CBBD", rotor).transform.localRotation = Quaternion.Euler(0, blade * 60, 0);
            }
        }

        static void Trays(Transform p, int capacity)
        {
            // Khay mới chạm bốn cột chung, tránh lơ lửng khi thân máy có hình dáng khác nhau.
            float top = .45f + (capacity - 1) * .23f + .035f;
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Art.Box("TrayRackPost", new(-.7f + x * .23f, top * .5f, .55f + z * .27f),
                        new(.045f, top, .045f), "#526E70", p);
            for (int i = 1; i < capacity; i++)
                Art.Box("ExtraOutputTray", new(-.7f, .45f + i * .23f, .55f),
                    new(.53f, .07f, .62f), "#A1B0A3", p);
        }

        public static void QualityMount(Transform p, int quality)
        {
            float center = .09f * (quality - 2);
            Art.Box("QualityPlaqueStand", new(center, .72f, -.50f),
                new(.06f, 1.44f, .06f), "#526E70", p);
            Art.Box("QualityPlaque", new(center, 1.45f, -.51f),
                new(.18f * (quality - 2) + .23f, .20f, .06f), "#415653", p);
        }
    }
}
