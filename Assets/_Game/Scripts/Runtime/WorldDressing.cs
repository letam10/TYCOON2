using UnityEngine;

namespace Tycoon
{
    // Chỉ dựng hình ảnh; owner, hàng, footprint và điểm thao tác thuộc hệ thống gameplay.
    public static class WorldDressing
    {
        const string Cream = "#F4EBD6", Wood = "#A07B55", Sage = "#79A48E", Dark = "#365B55";

        public static void Environment(Transform world) => world.gameObject.AddComponent<CozyCropMotion>();

        public static void Floor(Transform floor, Vector2 size, string label) =>
            TownArchitecture.Dress(floor, size, label);

        static void Flowers(Vector3 point, Transform parent)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 stem = point + new Vector3((i - 1) * .12f, .09f + i % 2 * .05f, i % 2 * .08f);
                Art.Cylinder("FlowerStem", stem, new(.022f, .11f, .022f), Sage, parent);
                Art.Cylinder("Flower", stem + Vector3.up * .115f, new(.13f, .017f, .13f),
                    i % 2 == 0 ? "#E8BB88" : "#CDA59E", parent);
                Art.Cylinder("FlowerHeart", stem + Vector3.up * .135f, new(.045f, .012f, .045f), "#EEDBAC", parent);
            }
        }

        public static void Storage(Transform root)
        {
            TownProps.Storage(root);
            Art.Model("storage_rack", new Vector3(0, .15f, .65f), root);
            for (int i = 0; i < 2; i++)
                Art.Model("supply_crate", new Vector3(-.76f + i * 1.52f, .15f, .52f), root, .55f);
            Art.Box("StorageHeader", new(0, 1.88f, .53f), new(2.4f, .32f, .08f), Sage, root);
            Art.Label("KHO • NGUYÊN LIỆU", new(0, 1.89f, .477f), root, .075f, Cream, false);
        }

        public static void Checkout(Transform root)
        {
            Art.Model("checkout_counter", Vector3.zero, root);
            Art.Model("pos_screen", new Vector3(.63f, 1, .14f), root, 1, 180);
            Art.Model("pos_keyboard", new Vector3(.63f, 1, -.26f), root);
            Art.Box("CounterTrim", new(0, .87f, -.48f), new(1.9f, .08f, .06f), Sage, root);
            Art.Box("PaymentPlaque", new(-.47f, .58f, -.49f), new(.58f, .27f, .035f), Cream, root);
            Art.Label("THANH TOÁN", new(-.47f, .58f, -.515f), root, .09f, Dark, false);
        }

        public static void Machine(MachineStation station)
        {
            TownProps.Machine(station);
            var root = station.transform;
            GameObject processModel;
            bool hot = false;
            Vector3 steamSource = new(0, 1.75f, .08f);
            Art.Box("MachineFloor", new(0, .023f, .05f), new(2.45f, .04f, 2.12f), "#D6DCC7", root);
            switch (station.Recipe.id)
            {
                case "oven":
                case "cakeoven":
                    processModel = Art.Model("oven_asset", Vector3.zero, root);
                    hot = true;
                    Art.Model("extractor", new Vector3(0, 2.1f, .1f), root);
                    Art.Model("rolling_pin", new Vector3(.3f, 1.65f, -.3f), root);
                    break;
                case "kitchen":
                case "cook_beef":
                case "cook_soup":
                case "cook_pasta":
                case "cook_sandwich":
                case "cook_vegetables":
                    processModel = Art.Model("cooking_range", Vector3.zero, root);
                    hot = true;
                    steamSource = new(.48f, 1.27f, .1f);
                    Art.Model("extractor", new Vector3(0, 2.1f, .1f), root);
                    Art.Model("cooking_pot", new Vector3(.48f, 1.05f, .1f), root);
                    Art.Model("cutting_board", new Vector3(-.55f, 1.05f, -.15f), root);
                    break;
                default:
                    processModel = Art.Model(station.Recipe.id, Vector3.zero, root, 1.45f);
                    foreach (var child in processModel.GetComponentsInChildren<Transform>())
                    {
                        if (!child.name.StartsWith("Rotor")) continue;
                        station.Rotor = child;
                        break;
                    }
                    break;
            }
            Art.Box("MachineIndicatorPlate", new(0, 1.17f, -1.12f), new(1.1f, .2f, .045f), Dark, root);
            var lamp = Art.Cylinder("MachineStatusLamp", new(.42f, 1.17f, -1.16f), new(.11f, .022f, .11f), Cream, root);
            lamp.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Art.Box("MachineProgressTrack", new(-.13f, 1.17f, -1.155f), new(.75f, .065f, .015f), "#9BA99D", root);
            var fill = Art.Box("MachineProgress", new(-.13f, 1.17f, -1.168f), new(.75f, .065f, .015f), Sage, root);
            var status = root.gameObject.AddComponent<MachineStatusLight>();
            status.Station = station;
            status.Lamp = lamp.GetComponent<Renderer>();
            status.Fill = fill.transform;
            root.gameObject.AddComponent<CozyMachineMotion>()
                .Initialize(station, processModel.transform, hot, steamSource);
        }

        public static void Livestock(Transform pen, Vector3 point, bool chicken)
        {
            if (chicken) Art.Model("coop", point + new Vector3(0, .03f, 2.4f), pen, .65f, 180);
            else Art.Model("farm_shelter", point + new Vector3(0, .03f, 2.7f), pen);
            Art.Box("HayBale", point + new Vector3(-2, .24f, 2.5f), new(1.05f, .48f, .55f), "#D6BE80", pen);
            for (int i = 0; i < 2; i++)
                Art.Box("HayTie", point + new Vector3(-2.27f + i * .54f, .49f, 2.5f),
                    new(.055f, .014f, .56f), Wood, pen);
        }

        public static void BackRooms(Transform world)
        {
            Room("FarmShopDecor", "farm_shop", new Vector3(12.2f, 0, 13.4f), false);
            Room("ProcessingDecor", "mill", new Vector3(21.7f, 0, 14.8f), true);
            Room("BakeryDecor", "bakery", new Vector3(10, 0, 45.5f), true);
            Room("RestaurantDecor", "restaurant", new Vector3(-28, 0, 47.5f), true);
            void Room(string name, string requirement, Vector3 point, bool sink)
            {
                var root = TownModelParts.Root(name, point, world);
                root.gameObject.AddComponent<UnlockVisual>().Requirement = requirement;
                TownFurnitureCollision.Model("cold_cabinet", Vector3.zero, root);
                TownFurnitureCollision.Model(sink ? "kitchen_sink" : "prep_table", new(1.8f, 0, 0), root);
                TownFurnitureCollision.Model("trash_bin", new(.15f, 0, -1.2f), root, .38f);
                if (requirement == "bakery") Art.Model("coffee_machine", new Vector3(1.8f, 1.06f, 0), root);
                TownModelParts.Box("ServiceShelf", new(1.8f, 1.71f, .55f), new(1.6f, .07f, .28f),
                    "wood", Wood, root, solid: true);
                Art.Model("clean_plate", new Vector3(1.45f, 1.76f, .55f), root, .8f);
                Art.Model("clean_plate", new Vector3(1.45f, 1.8f, .55f), root, .8f);
            }
        }

        public static void Table(TableStation table)
        {
            var root = TownModelParts.Root("DiningFurniture", Vector3.up * .06f, table.transform);
            TownFurnitureCollision.Model("dining_table", Vector3.zero, root, support: 0);
            // Ghế lùi khỏi điểm đón khách tại x = 1.4 để NavMesh giữ lối đến bàn.
            TownFurnitureCollision.Model("dining_chair", new Vector3(2.15f, 0, 0), root, 1, -90, 0);
            Art.Box("TableCloth", new(0, .848f, 0), new(1.05f, .022f, .86f), Cream, root);
            Art.Model("menu_board", new Vector3(-.35f, .875f, .15f), root, .6f, 25);
            Art.Cylinder("BudVase", new(-.43f, .91f, -.3f), new(.09f, .045f, .09f), Sage, root);
            Flowers(new Vector3(-.43f, .955f, -.3f), root);
            var view = root.gameObject.AddComponent<TableMealView>();
            view.Table = table;
            view.Meal = Art.Model("meal", new Vector3(.1f, .87f, 0), root);
            view.Dirty = Art.Model("dirty_plate", new Vector3(.1f, .87f, 0), root);
            view.Empty = Art.Model("clean_plate", new Vector3(.1f, .87f, 0), root);
            view.Meal.SetActive(false);
            view.Dirty.SetActive(false);
            view.Empty.SetActive(false);
        }
    }

    public sealed class MachineStatusLight : MonoBehaviour
    {
        public MachineStation Station;
        public Renderer Lamp;
        public Transform Fill;
        int previous = -1;
        void LateUpdate()
        {
            if (!Station || Station.Recipe == null) return;
            // Đèn chỉ đọc state thật, không tự chạy máy hay sinh sản phẩm minh họa.
            int state = Station.Broken ? 4 : Station.Phase == MachinePhase.Operating ? 2 :
                Station.Phase == MachinePhase.Ready ? 1 : Station.Phase == MachinePhase.CompletedWaitingPickup ? 3 : 0;
            if (state != previous)
            {
                Lamp.sharedMaterial = Art.Material(state == 4 ? "#CC8977" : state == 2 ? "#91BC7B" :
                    state == 3 ? "#E0BD74" : state == 1 ? "#8DB9AC" : "#D4D6C3");
                previous = state;
            }
            float progress = state == 2 ? Mathf.Clamp01(1 - Station.Remaining / Station.Recipe.seconds)
                : state == 3 ? 1 : 0;
            Fill.localScale = new(Mathf.Max(.01f, .75f * progress), .065f, .015f);
            Fill.localPosition = new(-.505f + .375f * progress, 1.17f, -1.168f);
        }
    }

    public sealed class TableMealView : MonoBehaviour
    {
        public TableStation Table;
        public GameObject Meal, Dirty, Empty;
        string item = "meal";
        void LateUpdate()
        {
            // Món chỉ hiện khi basket thật đã nhận; không tạo inventory hoặc payment.
            bool occupied = Table.IsUnlocked && Table.Occupant && Table.Occupant.Phase == 2
                && Table.Occupant.Basket.Total > 0;
            if (occupied && Table.Occupant.WantedItem != item)
            {
                Object.Destroy(Meal);
                item = Table.Occupant.WantedItem;
                Meal = Art.Model(Definitions.Item(item).model, new(.1f, .87f, 0), transform);
            }
            Meal.SetActive(occupied);
            Dirty.SetActive(Table.IsUnlocked && Table.Cleaning > 0);
            Empty.SetActive(Table.IsUnlocked && Table.Occupant && Table.Occupant.Phase == 1);
        }
    }
}
