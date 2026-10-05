using UnityEngine;

namespace Tycoon
{
    // Chỉ dựng hình ảnh; owner, hàng, footprint và các điểm thao tác thuộc hệ thống cũ.
    public static class WorldDressing
    {
        public static void Environment(Transform world)
        {
            var scenery = new GameObject("AssetScenery").transform; scenery.SetParent(world, false);
            string[] trees = { "tree_oak", "tree_pine", "tree_small" };
            for (int i = 0; i < 19; i++)
                for (int row = 0; row < 2; row++)
                {
                    Tree(new Vector3(-44 + i * 6, 0, 55 + row * 6), i + row * 7);
                    Tree(new Vector3(-44 + i * 6, 0, -29 - row * 6), i + row * 11);
                }
            for (int i = 0; i < 13; i++)
            {
                Tree(new Vector3(-47, 0, -20 + i * 6), i + 4);
                Tree(new Vector3(60, 0, -20 + i * 6), i + 9);
            }
            for (int i = 0; i < 16; i++)
            {
                var point = new Vector3(-44 + i * 6.6f, 0, 52.8f);
                Art.Model(i % 4 == 0 ? "garden_rock" : "garden_bush", point, scenery, .9f + i % 3 * .12f, i * 43);
            }
            void Tree(Vector3 point, int index) => Art.Model(trees[index % trees.Length], point, scenery, .9f + index % 4 * .08f, index * 47);
        }

        public static void Floor(Transform floor, Vector2 size, string label)
        {
            // Tile đã có UV/atlas; co ô biên để không vượt construction footprint.
            if (label.Contains("CHẾ BIẾN") || label.Contains("TIỆM BÁNH") || label.Contains("NHÀ HÀNG"))
                for (float x = 0; x < size.x; x += 4)
                    for (float z = 0; z < size.y; z += 4)
                    {
                        float width = Mathf.Min(4, size.x - x), depth = Mathf.Min(4, size.y - z);
                        var tile = Art.Model("floor_tile", new Vector3(-size.x / 2 + x + width / 2, .034f, -size.y / 2 + z + depth / 2), floor);
                        tile.transform.localScale = new Vector3(width / 4, 1, depth / 4);
                    }
            float right = size.x / 2 - 1.1f, back = size.y / 2 - 1.0f;
            Art.Model("planter", new Vector3(right, .04f, back), floor);
            Art.Model("planter", new Vector3(-right, .04f, back), floor);
            if (label.Contains("SIÊU THỊ") || label.Contains("NÔNG SẢN"))
            {
                Art.Model("park_bench", new Vector3(-size.x / 2 + 2, .04f, size.y / 2 - 1), floor, 1, 90);
                Art.Model("street_lamp", new Vector3(-size.x / 2 + 1, .04f, -size.y / 2 + 1), floor);
            }
        }

        public static void Storage(Transform root)
        {
            Art.Model("storage_rack", new Vector3(0, .15f, .65f), root);
            for (int i = 0; i < 2; i++)
                Art.Model("supply_crate", new Vector3(-.76f + i * 1.52f, .15f, .52f), root, .55f);
        }

        public static void Checkout(Transform root)
        {
            Art.Model("checkout_counter", Vector3.zero, root);
            Art.Model("pos_screen", new Vector3(.63f, 1.0f, .14f), root, 1, 180);
            Art.Model("pos_keyboard", new Vector3(.63f, 1.0f, -.26f), root);
        }

        public static void Machine(MachineStation station)
        {
            var root = station.transform;
            switch (station.Recipe.id)
            {
                case "oven": case "cakeoven":
                    Art.Model("oven_asset", Vector3.zero, root);
                    Art.Model("extractor", new Vector3(0, 2.1f, .10f), root);
                    Art.Model("rolling_pin", new Vector3(.3f, 1.65f, -.3f), root);
                    break;
                case "kitchen":case "cook_beef":case "cook_soup":case "cook_pasta":case "cook_sandwich":case "cook_vegetables":
                    Art.Model("cooking_range", Vector3.zero, root);
                    Art.Model("extractor", new Vector3(0, 2.1f, .10f), root);
                    Art.Model("cooking_pot", new Vector3(.48f, 1.05f, .10f), root);
                    Art.Model("cutting_board", new Vector3(-.55f, 1.05f, -.15f), root);
                    break;
                default:
                    var model = Art.Model(station.Recipe.id, Vector3.zero, root, 1.45f);
                    foreach (var child in model.GetComponentsInChildren<Transform>())
                        if (child.name.StartsWith("Rotor")) { station.Rotor = child; break; }
                    break;
            }
        }

        public static void Livestock(Transform pen, Vector3 point, bool chicken)
        {
            if (chicken) Art.Model("coop", point + new Vector3(0, .03f, 2.4f), pen, .65f, 180);
            else Art.Model("farm_shelter", point + new Vector3(0, .03f, 2.7f), pen);
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
                Art.Model("trash_bin", new Vector3(3.0f, 0, .3f), root);
                if (requirement == "bakery") Art.Model("coffee_machine", new Vector3(1.8f, 1.0f, 0), root);
            }
        }

        public static void Table(TableStation table)
        {
            var root = table.transform;
            Art.Model("dining_table", Vector3.zero, root);
            Art.Model("dining_chair", new Vector3(1.4f, 0, 0), root, 1, -90);
            Art.Model("menu_board", new Vector3(-.35f, .85f, .15f), root, .6f, 25);
            var view = root.gameObject.AddComponent<TableMealView>();
            view.Table = table;
            view.Meal = Art.Model("meal", new Vector3(.10f, .87f, 0), root);
            view.Dirty = Art.Model("dirty_plate", new Vector3(.10f, .87f, 0), root);
            view.Empty = Art.Model("clean_plate", new Vector3(.10f, .87f, 0), root);
            view.Meal.SetActive(false); view.Dirty.SetActive(false); view.Empty.SetActive(false);
        }
    }

    public sealed class TableMealView : MonoBehaviour
    {
        public TableStation Table;
        public GameObject Meal, Dirty, Empty;
        void LateUpdate()
        {
            // Món chỉ hiện khi basket thật đã nhận; không tạo thêm inventory hoặc payment.
            bool occupied = Table.IsUnlocked && Table.Occupant && Table.Occupant.Phase == 2 && Table.Occupant.Basket.Total > 0;
            Meal.SetActive(occupied); Dirty.SetActive(Table.IsUnlocked && Table.Cleaning > 0);
            Empty.SetActive(Table.IsUnlocked && Table.Occupant && Table.Occupant.Phase == 1);
        }
    }
}
