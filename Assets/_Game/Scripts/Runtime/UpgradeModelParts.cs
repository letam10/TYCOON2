using UnityEngine;

namespace Tycoon
{
    public static class UpgradeModelParts
    {
        public static void Build(Transform parent, Station station, int tier, int capacity, int speed, int quality)
        {
            if (station is ProductionStation producer)
            {
                if (producer.Animal) Pen(parent, tier, capacity, speed);
                else Farm(parent, tier, capacity, speed);
            }
            else if (station is MachineStation) Machine(parent, tier, capacity, speed);
            else if (station is StorageStation) Storage(parent, tier, capacity);
            else Shelf(parent, tier, capacity);
            if (quality > 1)
            {
                float height = station is ProductionStation ? .65f : 1.45f;
                for (int i = 0; i < quality - 1; i++)
                    Art.Cylinder("QualitySeal", new Vector3(.18f * i, height, -.55f),
                        new Vector3(.13f, .022f, .13f), "#C2A05F", parent).transform.localRotation =
                        Quaternion.Euler(90, 0, 0);
            }
        }

        static void Farm(Transform p, int tier, int capacity, int speed)
        {
            if (tier > 1)
                for (int side = -1; side <= 1; side += 2)
                    Art.Box("ReinforcedPlotBorder", new Vector3(side * 1.92f, .2f, 0),
                        new Vector3(.14f, .17f, 3.85f), tier == 3 ? "#6B8375" : "#B4A085", p);
            if (tier > 2)
                for (int side = -1; side <= 1; side += 2)
                    Art.Box("PlotCornerCap", new Vector3(side * 1.92f, .32f, 1.85f),
                        new Vector3(.28f, .09f, .28f), "#A5B9AE", p);
            for (int i = 1; i < speed; i++)
            {
                var pipe = Art.Cylinder("IrrigationLine", new Vector3(-1.6f + i * 1.1f, .22f, 0),
                    new Vector3(.055f, 1.75f, .055f), "#526E70", p);
                pipe.transform.localRotation = Quaternion.Euler(90, 0, 0);
                for (int j = 0; j < 3; j++)
                {
                    Art.Cylinder("IrrigationNozzle", new Vector3(-1.6f + i * 1.1f, .27f, j - 1),
                        new Vector3(.10f, .055f, .10f), "#A1A790", p);
                    Art.Box("SprinklerHead", new Vector3(-1.6f + i * 1.1f, .34f, j - 1),
                        new Vector3(.26f, .035f, .045f), "#A9BCAD", p);
                }
            }
            for (int i = 1; i < capacity; i++)
            {
                var crate = HandItemModels.Build("crate", p);
                crate.name = "HarvestCapacityBasket";
                crate.transform.localScale = Vector3.one * 1.7f;
                crate.transform.localPosition = new Vector3(-1.45f + i * .60f, .18f, 1.72f);
            }
        }

        static void Pen(Transform p, int tier, int capacity, int speed)
        {
            for (int i = 1; i < tier; i++)
                Art.Box("HusbandryFeedTrough", new Vector3((i - 1.5f) * .9f, .3f, .6f),
                    new Vector3(.72f, .35f, .4f), "#9A8769", p);
            for (int i = 1; i < speed; i++)
                Art.Cylinder("AutomaticWaterBowl", new Vector3((i - 1.5f) * .6f, .35f, -.4f),
                    new Vector3(.36f, .08f, .36f), "#7C9D9F", p);
            for (int i = 1; i < capacity; i++)
                Art.Box("FeedStorage", new Vector3(.75f, .2f + i * .18f, .5f),
                    new Vector3(.35f, .17f, .36f), "#BCA37A", p);
        }

        static void Machine(Transform p, int tier, int capacity, int speed)
        {
            for (int i = 1; i < tier; i++)
                Art.Box("MachineSteelBrace", new Vector3((i == 1 ? -1 : 1) * .83f, .65f, .45f),
                    new Vector3(.09f, 1.25f, .16f), "#8FABA5", p);
            if (tier > 2)
            {
                Art.Box("DigitalControlPanel", new Vector3(.6f, 1.2f, -.61f),
                    new Vector3(.36f, .27f, .06f), "#415653", p);
                Art.Box("ControlDisplay", new Vector3(.6f, 1.22f, -.65f),
                    new Vector3(.25f, .13f, .02f), "#A6CCB8", p);
            }
            for (int i = 1; i < speed; i++)
            {
                var motor = Art.Cylinder("SpeedMotor", new Vector3(.7f, .4f + i * .33f, .73f),
                    new Vector3(.3f, .21f, .3f), "#748F93", p);
                motor.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Art.Cylinder("MotorHub", new Vector3(.7f, .4f + i * .33f, .95f),
                    new Vector3(.12f, .04f, .12f), "#B6C0AF", p).transform.localRotation = Quaternion.Euler(90, 0, 0);
                var rotor = new GameObject("MotorRotor").transform;
                rotor.SetParent(p, false);
                rotor.localPosition = new Vector3(.7f, .4f + i * .33f, 1.0f);
                rotor.localRotation = Quaternion.Euler(90, 0, 0);
                for (int blade = 0; blade < 3; blade++)
                    Art.Box("FanBlade", Vector3.zero, new Vector3(.22f, .025f, .045f),
                        "#C0CBBD", rotor).transform.localRotation = Quaternion.Euler(0, blade * 60, 0);
            }
            for (int i = 1; i < capacity; i++)
                Art.Box("ExtraOutputTray", new Vector3(-.7f, .45f + i * .23f, .55f),
                    new Vector3(.53f, .07f, .62f), "#A1B0A3", p);
        }

        static void Storage(Transform p, int tier, int capacity)
        {
            for (int i = 1; i < Mathf.Max(tier, capacity); i++)
            {
                float height = .60f + i * .4f;
                Art.Box("AdditionalStorageShelf", new Vector3(0, height, .65f),
                    new Vector3(2.45f, .09f, .65f), "#917859", p);
                for (int j = 0; j < 3; j++)
                {
                    var crate = HandItemModels.Build("crate", p);
                    crate.name = "StorageCapacityCrate";
                    crate.transform.localPosition = new Vector3((j - 1) * .75f, height + .045f, .65f);
                    crate.transform.localScale = Vector3.one * 1.55f;
                }
            }
            if (tier > 1 || capacity > 1)
                for (int side = -1; side <= 1; side += 2)
                    Art.Box("StorageRackFrame", new Vector3(side * 1.25f, .9f, .65f),
                        new Vector3(.09f, 1.8f, .09f), "#5C7C71", p);
        }

        static void Shelf(Transform p, int tier, int capacity)
        {
            for (int i = 1; i < tier; i++)
                Art.Box("ShelfFrontTrim", new Vector3(0, .25f + i * .37f, -.58f),
                    new Vector3(1.25f, .08f, .07f), tier > 2 ? "#BAA26D" : "#8B7453", p);
            for (int i = 1; i < capacity; i++)
            {
                Art.Box("ExtraDisplayShelf", new Vector3(0, 1.0f + i * .28f, .55f),
                    new Vector3(1.2f, .055f, .47f), "#A58E6B", p);
                Art.Box("DisplayShelfSupport", new Vector3(.55f, 1.0f + i * .14f, .70f),
                    new Vector3(.055f, i * .28f, .055f), "#6E887E", p);
            }
        }

        public static void Crew(Transform p, int count, int carry, int speed)
        {
            CrewUpgradeGeometry.Build(p, count, carry, speed);
        }
    }
}
