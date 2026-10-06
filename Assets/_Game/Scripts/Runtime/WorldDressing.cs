using UnityEngine;

namespace Tycoon
{
    // Chỉ dựng hình ảnh; owner, hàng, footprint và điểm thao tác thuộc hệ thống gameplay.
    public static class WorldDressing
    {
        const string Cream = "#F4EBD6", Wood = "#A07B55", Sage = "#79A48E", Dark = "#365B55";

        public static void Environment(Transform world)
        {
            var scenery = new GameObject("AssetScenery").transform; scenery.SetParent(world, false);
            string[] trees = { "tree_oak", "tree_pine", "tree_small" };
            // Vành cây ở ngoài đường xe và vùng pad; không thêm vật cản cho người chơi.
            for (int i = 0; i < 18; i++)
            {
                Tree(new Vector3(-49 + i * 7, 0, 71), i);
                Tree(new Vector3(-49 + i * 7, 0, -31 - i % 2 * 4), i + 8);
            }
            for (int i = 0; i < 10; i++)
            {
                Tree(new Vector3(-56 - i % 2 * 3, 0, -17 + i * 8), i + 4);
                Tree(new Vector3(70 + i % 2 * 3, 0, 1 + i * 8), i + 9);
            }
            for (int i = 0; i < 14; i++)
            {
                var point = new Vector3(-43 + i * 8, .02f, -24);
                Art.Model(i % 4 == 0 ? "garden_rock" : "garden_bush", point, scenery, .65f + i % 3 * .12f, i * 43);
                if (i % 3 == 0) Flowers(point + new Vector3(.85f, .02f, .4f), scenery);
            }
            for (int i = 0; i < 10; i++)
                Art.Model("garden_bush", new Vector3(-47 + i * 12, 0, 61), scenery, .7f, i * 71);
            Sign("01  NÔNG TRẠI\nGIEO HẠT • THU HOẠCH", new Vector3(-19, 0, 33), scenery, 4.2f, "#7DA16A");
            Sign("THỊ TRẤN NHỎ\nCÙNG NHAU LỚN LÊN", new Vector3(69, 0, -22), scenery, 5.2f, Sage);
            Art.Model("park_bench", new Vector3(0, .02f, -25), scenery, 1, 180);
            Art.Model("street_lamp", new Vector3(2.6f, .02f, -25), scenery, .9f);
            void Tree(Vector3 point, int index) => Art.Model(trees[index % trees.Length], point, scenery, .9f + index % 4 * .08f, index * 47);
        }

        public static void Floor(Transform floor, Vector2 size, string label)
        {
            bool shop = label.Contains("NÔNG SẢN"), market = label.Contains("SIÊU THỊ"),
                bakery = label.Contains("TIỆM BÁNH"), restaurant = label.Contains("NHÀ HÀNG");
            string accent = shop ? "#88A36C" : market ? "#719B91" : bakery ? "#CC9A75" : restaurant ? "#BB8A7B" : "#7D9CAC";
            string ground = shop || bakery ? "#DDC4A1" : restaurant ? "#EAD8BF" : market ? "#DFE6D7" : "#DAE2DC";
            var visual = new GameObject("CozyFloorDetails").transform; visual.SetParent(floor, false);
            var baseFloor = floor.Find("BusinessFloor");
            if (baseFloor) baseFloor.GetComponent<Renderer>().sharedMaterial = Art.Material(ground);
            foreach (Transform child in floor)
            {
                if (child.name is "FloorEdge" or "LeftEdge" or "BackTrim" or "RightTrim")
                    child.GetComponent<Renderer>().sharedMaterial = Art.Material(accent);
                if (child.name is "BackWall" or "RightWall")
                    child.GetComponent<Renderer>().sharedMaterial = Art.Material(Cream);
                if (child.name == "Label")
                    child.GetComponent<TextMesh>().color = Art.Hex(Dark).linear;
            }
            // Mạch sàn ít geometry: góc nhìn toàn cảnh vẫn rõ khu, không tạo hàng trăm tile.
            float spacing = shop || bakery ? 1.8f : 3;
            for (float z = -size.y / 2 + spacing; z < size.y / 2; z += spacing)
                Art.Box("FloorSeam", new(0, .037f, z), new(size.x, .008f, .016f), shop || bakery ? "#C5A887" : "#C2CDC4", visual);
            if (!shop && !bakery)
                for (float x = -size.x / 2 + spacing; x < size.x / 2; x += spacing)
                    Art.Box("FloorSeam", new(x, .037f, 0), new(.016f, .008f, size.y), "#C2CDC4", visual);
            Art.Box("EntryMat", new(-size.x / 2 + 2.2f, .045f, -size.y / 2 + 1.8f), new(3.2f, .025f, 1.9f), accent, visual);
            Art.Box("EntryMatInset", new(-size.x / 2 + 2.2f, .059f, -size.y / 2 + 1.8f), new(2.8f, .009f, 1.5f), Cream, visual);
            float right = size.x / 2 - 1.1f, back = size.y / 2 - 1;
            Art.Model("planter", new Vector3(right, .04f, back), visual, .85f);
            Art.Model("planter", new Vector3(-right, .04f, back), visual, .85f);
            Sign(shop ? "02  NÔNG SẢN\nTƯƠI MỖI NGÀY" : market ? "04  SIÊU THỊ\nMỌI THỨ BẠN CẦN" :
                bakery ? "05  TIỆM BÁNH\nẤM TỪ LÒ • THƠM MỖI NGÀY" : restaurant ? "05  NHÀ HÀNG\nTỪ NÔNG TRẠI ĐẾN BÀN ĂN" : "03  CHẾ BIẾN\nNGUYÊN LIỆU THÀNH GIÁ TRỊ",
                new Vector3(-size.x / 2 + 3.9f, 0, size.y / 2 + .15f), visual, 5.5f, accent);
            if (market || shop)
            {
                Art.Model("park_bench", new Vector3(-size.x / 2 + 2, .04f, size.y / 2 - 1), visual, .85f, 90);
                Art.Model("street_lamp", new Vector3(-size.x / 2 + 1, .04f, -size.y / 2 + 1), visual, .8f);
            }
            else if (bakery || restaurant)
                Flowers(new Vector3(right, .52f, back), visual);
        }

        static void Sign(string text, Vector3 point, Transform parent, float width, string accent)
        {
            var root = new GameObject("DistrictSign").transform; root.SetParent(parent, false); root.localPosition = point;
            Art.Box("SignBoard", new(0, 1.65f, 0), new(width, .88f, .16f), Wood, root);
            Art.Box("SignFace", new(0, 1.65f, -.09f), new(width - .14f, .7f, .024f), accent, root);
            for (int side = -1; side <= 1; side += 2)
                Art.Box("SignPost", new(side * (width / 2 - .3f), .83f, .08f), new(.12f, 1.66f, .12f), Wood, root);
            var label = Art.Label(text, new(0, 1.65f, -.115f), root, .1f, Cream, false);
            label.lineSpacing = .85f;
        }

        static void Flowers(Vector3 point, Transform parent)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 stem = point + new Vector3((i - 1) * .12f, .09f + i % 2 * .05f, i % 2 * .08f);
                Art.Cylinder("FlowerStem", stem, new(.022f, .11f, .022f), Sage, parent);
                Art.Cylinder("Flower", stem + Vector3.up * .115f, new(.13f, .017f, .13f), i % 2 == 0 ? "#E8BB88" : "#CDA59E", parent);
                Art.Cylinder("FlowerHeart", stem + Vector3.up * .135f, new(.045f, .012f, .045f), "#EEDBAC", parent);
            }
        }

        public static void Storage(Transform root)
        {
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
            var root = station.transform;
            Art.Box("MachineFloor", new(0, .023f, .05f), new(2.45f, .04f, 2.12f), "#D6DCC7", root);
            switch (station.Recipe.id)
            {
                case "oven": case "cakeoven":
                    Art.Model("oven_asset", Vector3.zero, root);
                    Art.Model("extractor", new Vector3(0, 2.1f, .1f), root);
                    Art.Model("rolling_pin", new Vector3(.3f, 1.65f, -.3f), root);
                    break;
                case "kitchen": case "cook_beef": case "cook_soup": case "cook_pasta": case "cook_sandwich": case "cook_vegetables":
                    Art.Model("cooking_range", Vector3.zero, root);
                    Art.Model("extractor", new Vector3(0, 2.1f, .1f), root);
                    Art.Model("cooking_pot", new Vector3(.48f, 1.05f, .1f), root);
                    Art.Model("cutting_board", new Vector3(-.55f, 1.05f, -.15f), root);
                    break;
                default:
                    var model = Art.Model(station.Recipe.id, Vector3.zero, root, 1.45f);
                    foreach (var child in model.GetComponentsInChildren<Transform>())
                        if (child.name.StartsWith("Rotor")) { station.Rotor = child; break; }
                    break;
            }
            Art.Box("MachineIndicatorPlate", new(0, 1.17f, -1.12f), new(1.1f, .2f, .045f), Dark, root);
            var lamp = Art.Cylinder("MachineStatusLamp", new(.42f, 1.17f, -1.16f), new(.11f, .022f, .11f), Cream, root);
            lamp.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Art.Box("MachineProgressTrack", new(-.13f, 1.17f, -1.155f), new(.75f, .065f, .015f), "#9BA99D", root);
            var fill = Art.Box("MachineProgress", new(-.13f, 1.17f, -1.168f), new(.75f, .065f, .015f), Sage, root);
            var status = root.gameObject.AddComponent<MachineStatusLight>(); status.Station = station;
            status.Lamp = lamp.GetComponent<Renderer>(); status.Fill = fill.transform;
        }

        public static void Livestock(Transform pen, Vector3 point, bool chicken)
        {
            if (chicken) Art.Model("coop", point + new Vector3(0, .03f, 2.4f), pen, .65f, 180);
            else Art.Model("farm_shelter", point + new Vector3(0, .03f, 2.7f), pen);
            Art.Box("HayBale", point + new Vector3(-2, .24f, 2.5f), new(1.05f, .48f, .55f), "#D6BE80", pen);
            for (int i = 0; i < 2; i++)
                Art.Box("HayTie", point + new Vector3(-2.27f + i * .54f, .49f, 2.5f), new(.055f, .014f, .56f), Wood, pen);
        }

        public static void BackRooms(Transform world)
        {
            Room("FarmShopDecor", "farm_shop", new Vector3(12.2f, 0, 13.4f), false);
            Room("ProcessingDecor", "mill", new Vector3(21.7f, 0, 14.8f), true);
            Room("BakeryDecor", "bakery", new Vector3(10, 0, 45.5f), true);
            Room("RestaurantDecor", "restaurant", new Vector3(-28, 0, 47.5f), true);
            void Room(string name, string requirement, Vector3 point, bool sink)
            {
                var root = new GameObject(name).transform; root.SetParent(world, false); root.localPosition = point;
                root.gameObject.AddComponent<UnlockVisual>().Requirement = requirement;
                Art.Model("cold_cabinet", Vector3.zero, root);
                Art.Model(sink ? "kitchen_sink" : "prep_table", new Vector3(1.8f, 0, 0), root);
                Art.Model("trash_bin", new Vector3(3, 0, .3f), root);
                if (requirement == "bakery") Art.Model("coffee_machine", new Vector3(1.8f, 1, 0), root);
                Art.Box("ServiceShelf", new(1.8f, 1.65f, .55f), new(1.6f, .07f, .28f), Wood, root);
                Art.Model("clean_plate", new Vector3(1.45f, 1.7f, .55f), root, .8f);
                Art.Model("clean_plate", new Vector3(1.45f, 1.74f, .55f), root, .8f);
            }
        }

        public static void Table(TableStation table)
        {
            var root = table.transform;
            Art.Model("dining_table", Vector3.zero, root);
            Art.Model("dining_chair", new Vector3(1.4f, 0, 0), root, 1, -90);
            Art.Box("TableCloth", new(0, .848f, 0), new(1.05f, .022f, .86f), Cream, root);
            Art.Model("menu_board", new Vector3(-.35f, .875f, .15f), root, .6f, 25);
            Art.Cylinder("BudVase", new(-.43f, .91f, -.3f), new(.09f, .045f, .09f), Sage, root);
            Flowers(new Vector3(-.43f, .955f, -.3f), root);
            var view = root.gameObject.AddComponent<TableMealView>(); view.Table = table;
            view.Meal = Art.Model("meal", new Vector3(.1f, .87f, 0), root);
            view.Dirty = Art.Model("dirty_plate", new Vector3(.1f, .87f, 0), root);
            view.Empty = Art.Model("clean_plate", new Vector3(.1f, .87f, 0), root);
            view.Meal.SetActive(false); view.Dirty.SetActive(false); view.Empty.SetActive(false);
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
                    state == 3 ? "#E0BD74" : state == 1 ? "#8DB9AC" : "#D4D6C3"); previous = state;
            }
            float progress = state == 2 ? Mathf.Clamp01(1 - Station.Remaining / Station.Recipe.seconds) : state == 3 ? 1 : 0;
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
            bool occupied = Table.IsUnlocked && Table.Occupant && Table.Occupant.Phase == 2 && Table.Occupant.Basket.Total > 0;
            if (occupied && Table.Occupant.WantedItem != item)
            {
                Object.Destroy(Meal); item = Table.Occupant.WantedItem;
                Meal = Art.Model(Definitions.Item(item).model, new(.1f, .87f, 0), transform);
            }
            Meal.SetActive(occupied); Dirty.SetActive(Table.IsUnlocked && Table.Cleaning > 0);
            Empty.SetActive(Table.IsUnlocked && Table.Occupant && Table.Occupant.Phase == 1);
        }
    }
}
