using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public static class WorldFactory
    {
        public static void Build(GameSession game)
        {
            var world = new GameObject("World").transform; world.SetParent(game.transform);
            Art.Box("Grass", new Vector3(8, -.13f, 12), new Vector3(112, .25f, 92), "#94D661", world, true);
            Lighting(world);
            for (int i = 0; i < 115; i++)
            {
                float x = -43 + (i % 23) * 4.9f, z = i < 46 ? 55 + (i / 23) * 5 : -20 - ((i - 46) / 23) * 4.7f;
                var tree = Art.Model("tree", new Vector3(x + (i % 3) * .6f, 0, z), world, 1 + (i % 4) * .13f, i * 47);
                foreach (var renderer in tree.GetComponentsInChildren<Renderer>())
                {
                    var mats = renderer.sharedMaterials;
                    for (int m = 0; m < mats.Length; m++) if (mats[m].name.ToLower().Contains("green")) mats[m] = Art.Material(i % 3 == 0 ? "#AFEC26" : "#66CF20");
                    renderer.sharedMaterials = mats;
                }
            }
            Art.Box("StarterField", new Vector3(-13, .012f, 8), new Vector3(19, .04f, 12), "#A7D96B", world);
            Floor(world, "02  FARM SHOP", new Vector3(7, 0, 6), new Vector2(22, 20));
            Crop(game, world, "field_carrot", "carrot", "CÀ RỐT • LUỐNG 1", new Vector3(-19, 0, 8), 3.8f);
            Crop(game, world, "field_carrot_2", "carrot", "CÀ RỐT • LUỐNG 2", new Vector3(-13, 0, 8), 3.8f);
            Crop(game, world, "field_carrot_3", "carrot", "CÀ RỐT • LUỐNG 3", new Vector3(-7, 0, 8), 3.8f);
            Crop(game, world, "field_tomato", "tomato", "CÀ CHUA", new Vector3(-19, 0, 14), 4.1f);
            Crop(game, world, "field_wheat", "wheat", "LÚA MÌ", new Vector3(-13, 0, 14), 4.3f);
            foreach (var id in new[] { "field_tomato", "field_wheat" })
            {
                var locked = game.Producers.Find(x => x.Id == id); locked.Requirement = "farm_shop";
                locked.gameObject.AddComponent<UnlockVisual>().Requirement = "farm_shop";
            }
            Pen(world, new Vector3(-14, 0, 25), new Vector2(20, 12), "user_cow", 28, 1, "barn", "milk_line");
            Pen(world, new Vector3(-28, 0, 25), new Vector2(8, 12), "user_chicken", 12, .9f, "egg_line");
            AnimalProducer(game, world, "dairy_milk", "milk", "SỮA", new Vector3(-3.5f, 0, 15), 30, "milk_line");
            AnimalProducer(game, world, "coop_egg", "egg", "TRỨNG", new Vector3(-3.5f, 0, 10), 30, "egg_line");
            AnimalProducer(game, world, "ranch_beef", "beef", "THỊT BÒ", new Vector3(-7, 0, 15), 45, "barn");
            game.Storage = Warehouse(game, world, new Vector3(-1, 0, 10), "storage_farm", "farm", 72);
            Warehouse(game, world, new Vector3(18, 0, 10), "storage_farm_shop", "farm_shop", 72);
            Shelf(game, world, "shelf_farm_1", "CÀ RỐT", new Vector3(14.8f, 0, 11), new[] { "carrot" }, "farm");
            Shelf(game, world, "shelf_farm_2", "NÔNG SẢN", new Vector3(14.8f, 0, 6), new[] { "tomato", "wheat", "milk", "egg", "beef", "flour", "cheese", "sauce" }, "farm", "farm_shop");
            Checkout(game, world, "checkout_farm", new Vector3(10, 0, -3), "farm");
            Floor(world, "03  PROCESSING", new Vector3(28, 0, 7), new Vector2(18, 18));
            Machine(game, world, "mill", new Vector3(23, 0, 12), "mill");
            Machine(game, world, "cheesemaker", new Vector3(28, 0, 12), "dairy");
            Machine(game, world, "saucemaker", new Vector3(33, 0, 12), "dairy");
            FinalBusinesses.Build(game, world);
            int farmPad = 0, processPad = 0;
            foreach (var upgrade in Definitions.Upgrades)
            {
                if (System.Array.IndexOf(new[] { "supermarket", "restocker_market", "cashier_market", "bakery", "cook_bakery", "restaurant", "cook", "waiter", "transport_bakery", "cashier_bakery", "transport_restaurant" }, upgrade.id) >= 0) continue;
                bool processing = upgrade.id is "mill" or "processor" or "dairy" or "machine_upgrade" or "transport_processing" or "conveyor_processing";
                int index = processing ? processPad++ : farmPad++;
                Vector3 point = processing
                    ? new Vector3(19 + (index % 6) * 3.7f, 0, -5 + (index / 6) * 3)
                    : new Vector3(-23 + (index % 7) * 3.7f, 0, -8 + (index / 7) * 3);
                Pad(game, world, upgrade, point);
            }
            world.gameObject.AddComponent<NavigationWorld>();
        }
        public static Transform Floor(Transform parent, string label, Vector3 position, Vector2 size)
        {
            var floor = new GameObject(label).transform; floor.SetParent(parent); floor.position = position;
            Art.Box("OrangeFloor", new Vector3(0, .012f, 0), new Vector3(size.x, .04f, size.y), "#EDB5A2", floor);
            float x = size.x / 2, z = size.y / 2;
            Art.Box("BackWall", new Vector3(0, .65f, z), new Vector3(size.x, 1.3f, .22f), "#F7D435", floor, true);
            Art.Box("BlueBackTrim", new Vector3(0, 1.31f, z), new Vector3(size.x + .15f, .14f, .3f), "#039BDC", floor);
            Art.Box("RightWall", new Vector3(x, .65f, 0), new Vector3(.22f, 1.3f, size.y), "#F7D435", floor, true);
            Art.Box("BlueRightTrim", new Vector3(x, 1.31f, 0), new Vector3(.3f, .14f, size.y), "#039BDC", floor);
            Art.Box("FloorEdge", new Vector3(0, .03f, -z), new Vector3(size.x, .08f, .2f), "#039BDC", floor);
            Art.Box("LeftEdge", new Vector3(-x, .03f, 0), new Vector3(.2f, .08f, size.y), "#039BDC", floor);
            Art.Label(label, new Vector3(-x + 4, .06f, -z + 1), floor, .25f, "#FFFFFF", false).transform.rotation = Quaternion.Euler(90, 0, 0);
            return floor;
        }
        static void Pen(Transform parent, Vector3 position, Vector2 size, string key, int count, float scale, string requirement, string alternative = "")
        {
            var pen = new GameObject("Pen_" + key).transform;
            pen.SetParent(parent, false);
            var visual = pen.gameObject.AddComponent<UnlockVisual>();
            visual.Requirement = requirement;
            if (!string.IsNullOrEmpty(alternative)) visual.AnyRequirements = new[] { alternative };
            Art.Box("PenGround", position + new Vector3(0, .008f, 0), new Vector3(size.x, .025f, size.y), "#CBA671", pen);
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < Mathf.CeilToInt(size.x / 2.2f); i++) Art.Model("fence", position + new Vector3(-size.x / 2 + 1.1f + i * 2.2f, 0, (side == 0 ? -1 : 1) * size.y / 2), pen, .85f);
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < Mathf.CeilToInt(size.y / 2.2f); i++) Art.Model("fence", position + new Vector3((side == 0 ? -1 : 1) * size.x / 2, 0, -size.y / 2 + 1.1f + i * 2.2f), pen, .85f, 90);
            int columns = key == "user_cow" ? 7 : 3;
            for (int i = 0; i < count; i++)
            {
                var animal = Art.Model(key, position + new Vector3(-size.x / 2 + 1.3f + (i % columns) * (size.x - 2.6f) / (columns - 1), .03f, -size.y / 2 + 1.3f + (i / columns) * 2.7f), pen, scale, 190 + i % 3 * 18);
                if (i >= (key == "user_cow" ? 8 : 3)) animal.AddComponent<UnlockVisual>().Requirement = key == "user_cow" ? "barn" : requirement;
                if(animal.GetComponentInChildren<Animator>()){var view=animal.AddComponent<ActorView>();view.Initialize();view.SetMotion(0,false);}
                animal.AddComponent<AnimalMotion>().Phase=i*.8f;
            }
            Art.Box("PenCollision", position + new Vector3(0, .6f, 0), new Vector3(size.x, 1.2f, size.y), "#CBA671", pen, true).GetComponent<Renderer>().enabled = false;
        }
        static void Lighting(Transform parent)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Art.Hex("#E8F5DC"); RenderSettings.ambientEquatorColor = Art.Hex("#CADDB0"); RenderSettings.ambientGroundColor = Art.Hex("#A0B577"); RenderSettings.fog = false;
            var sun = new GameObject("Sun"); sun.transform.SetParent(parent); sun.transform.rotation = Quaternion.Euler(55, -35, 0);
            var light = sun.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.15f; light.color = Color.white; light.shadows = LightShadows.Soft; light.shadowStrength = .48f; light.shadowBias = .04f; RenderSettings.sun = light;
            var volume = new GameObject("ColorGrade"); volume.transform.SetParent(parent); var component = volume.AddComponent<Volume>(); component.isGlobal = true; component.sharedProfile = Art.Catalog.lightingProfile;
        }
        static T Register<T>(GameSession game, Transform parent, string id, string label, Vector3 position, int capacity = 0) where T : Station
        {
            var root = new GameObject(id); root.transform.SetParent(parent); root.transform.position = position;
            var station = root.AddComponent<T>(); station.Id = id; station.Label = label; station.InteractionPoint = position + Vector3.back * 1.5f;
            if (capacity > 0) station.Inventory = new Inventory(capacity);
            station.StatusLabel = Art.Label(label, new Vector3(0, 2.0f, 0), root.transform, .22f, "#FFFFFF"); game.Stations.Add(station); return station;
        }
        static void Stack(GameSession game, Station station, Vector3 offset, float scale, int columns = 3, int rows = 2, int maximum = 36)
        {
            var root = new GameObject("Stock"); root.transform.SetParent(station.transform, false); root.transform.localPosition = offset;
            var view = root.AddComponent<InventoryStack>(); view.Inventory = station.Inventory; view.Pool = game.Pool; view.Scale = scale;
            view.Columns = columns; view.Rows = rows; view.Maximum = maximum; view.Spacing = .38f; view.LayerHeight = .24f;
        }
        static void Crop(GameSession game, Transform parent, string id, string item, string label, Vector3 point, float interval)
        {
            var station = Register<ProductionStation>(game, parent, id, label, point, 24); station.AreaId = "farm"; station.ItemId = item; station.Interval = interval; station.Remaining = interval;
            Art.Box("Soil", new Vector3(0, .07f, 0), new Vector3(4, .14f, 3.5f), "#B4864F", station.transform);
            var plants = new List<Transform>();
            for (int i = 0; i < 9; i++) plants.Add(Art.Model(item == "carrot" ? "carrot_crop" : item, new Vector3(-.95f + i % 3 * .95f, .14f, -.75f + i / 3 * .75f), station.transform, item == "tomato" ? .8f : 1).transform);
            station.Plants = plants.ToArray(); station.InteractionPoint = point + Vector3.back * 2.25f; game.Producers.Add(station);
        }
        static void AnimalProducer(GameSession game, Transform parent, string id, string item, string label, Vector3 point, float interval, string requirement)
        {
            var station = Register<ProductionStation>(game, parent, id, label, point, 36); station.AreaId = "farm"; station.ItemId = item; station.Interval = interval; station.Remaining = interval; station.Yield = 4; station.Requirement = requirement; station.Cycle = interval;
            Art.Box("YellowBin", new Vector3(0, .43f, 0), new Vector3(1.9f, .85f, 1.9f), "#F6DF29", station.transform);
            Art.Box("BlueBinBase", new Vector3(0, .08f, 0), new Vector3(2, .16f, 2), "#049CDA", station.transform);
            Art.Box("BluePipe", new Vector3(-1.5f, .4f, 0), new Vector3(2, .25f, .25f), "#20BEE8", station.transform);
            Stack(game, station, new Vector3(0, .85f, 0), .75f, 2, 2, 24); game.Producers.Add(station); Locked(station);
        }
        public static StorageStation Warehouse(GameSession game, Transform parent, Vector3 point, string id = "storage", string area = "farm", int capacity = 800)
        {
            var station = Register<StorageStation>(game, parent, id, "KHO • " + area, point, capacity); station.AreaId = area;
            Art.Box("StorageBase", new Vector3(0, .08f, 0), new Vector3(3.4f, .15f, 3.4f), "#FFFFFF", station.transform);
            Art.Box("StorageCollision",new Vector3(0,.55f,.5f),new Vector3(3.1f,1.1f,2.5f),"#FFFFFF",station.transform,true).GetComponent<Renderer>().enabled=false;
            for (int i = 0; i < 3; i++) Art.Model("crate", new Vector3(-1.1f + i * 1.1f, .15f, .8f), station.transform, 1);
            Stack(game, station, new Vector3(0, .2f, 0), .7f); return station;
        }
        public static ShelfStation Shelf(GameSession game, Transform parent, string id, string label, Vector3 point, string[] items, string shop, string requirement = "")
        {
            var station = Register<ShelfStation>(game, parent, id, label, point, 36); station.AllowedItems = items; station.ShopId = shop; station.AreaId = shop == "farm" ? "farm_shop" : shop == "market" ? "supermarket" : shop; station.Requirement = requirement; station.InteractionPoint = point + Vector3.left * 1.25f;
            Art.Box("BlueCounter", new Vector3(0, .45f, 0), new Vector3(1.4f, .9f, 4.7f), "#039BDD", station.transform, true);
            Art.Box("WhiteCounter", new Vector3(0, .95f, 0), new Vector3(1.48f, .13f, 4.8f), "#EAF8EF", station.transform);
            Stack(game, station, new Vector3(0, 1.04f, -.25f), .75f, 2, 5, 36); game.Shelves.Add(station); Locked(station); return station;
        }
        public static CheckoutStation Checkout(GameSession game, Transform parent, string id, Vector3 point, string shop, string requirement = "")
        {
            var station = Register<CheckoutStation>(game, parent, id, "QUẦY " + shop, point); station.ShopId = shop; station.AreaId = shop == "farm" ? "farm_shop" : shop == "market" ? "supermarket" : shop; station.Requirement = requirement; Art.Model("user_checkout", Vector3.zero, station.transform, 1.25f);
            Art.Box("CheckoutCollision", new Vector3(.3f, .55f, 0), new Vector3(2.1f, 1.1f, .9f), "#05A4E3", station.transform, true).GetComponent<Renderer>().enabled = false;
            station.InteractionPoint = point;
            Zone(game, parent, station, "serve", point + Vector3.left * 2.7f + Vector3.back * 1.5f);
            Zone(game, parent, station, "cash", point + Vector3.right * 2.7f + Vector3.back * 1.5f);
            game.Checkouts.Add(station); Locked(station); return station;
        }
        static void Zone(GameSession game, Transform parent, Station target, string mode, Vector3 point)
        {
            var zone = Register<StationZone>(game, parent, target.Id + "_" + mode, mode == "serve" ? "GIAO HÀNG" : "THU TIỀN", point);
            zone.Target = target; zone.Mode = mode; zone.AreaId = target.AreaId; zone.Requirement = target.Requirement; zone.InteractionPoint = point;
            target.PlayerUsesZones = true; zone.InteractionRadius = .85f;
            if (target is CheckoutStation checkout && mode == "cash") checkout.CashZone = zone;
            Art.Box("InteractionZone", new Vector3(0, .035f, 0), new Vector3(1.8f, .07f, 1.8f), mode == "serve" ? "#73CF4F" : "#FFD63E", zone.transform);
            Locked(zone);
        }
        public static MachineStation Machine(GameSession game, Transform parent, string key, Vector3 point, string requirement)
        {
            var recipe = Definitions.Recipe(key); var station = Register<MachineStation>(game, parent, "machine_" + key, recipe.label, point, 36); station.Recipe = recipe; station.Requirement = requirement; station.AreaId = key is "oven" or "cakeoven" ? "bakery" : key == "kitchen" ? "restaurant" : "processing";
            var model = Art.Model(key, Vector3.zero, station.transform, 1.45f);
            foreach (var child in model.GetComponentsInChildren<Transform>()) if (child.name.StartsWith("Rotor")) { station.Rotor = child; break; }
            Art.Box("MachineCollision", new Vector3(0, .9f, 0), new Vector3(2.65f, 1.8f, 2.25f), "#039BDD", station.transform, true).GetComponent<Renderer>().enabled = false;
            station.InteractionPoint = point + Vector3.back * 2.3f; Stack(game, station, new Vector3(.9f, .18f, -.9f), .55f, 1, 2, 12); game.Machines.Add(station); Locked(station); return station;
        }
        static void Locked(Station station) { if (!string.IsNullOrEmpty(station.Requirement)) station.gameObject.AddComponent<UnlockVisual>().Requirement = station.Requirement; }
        public static PurchasePad Pad(GameSession game, Transform parent, UpgradeDefinition upgrade, Vector3 point)
        {
            var station = Register<PurchasePad>(game, parent, "pad_" + upgrade.id, upgrade.label, point); station.Upgrade = upgrade; station.Requirement = upgrade.requirement; station.InteractionPoint = point;
            Art.Box("PadBorder", new Vector3(0, .03f, 0), new Vector3(1.5f, .04f, 1.5f), "#FFFFFF", station.transform); Art.Box("PadFace", new Vector3(0, .06f, 0), new Vector3(1.3f, .025f, 1.3f), "#343B4A", station.transform);
            Art.Label("↑\n" + upgrade.cost, new Vector3(0, .08f, 0), station.transform, .31f, "#FFFFFF", false).transform.rotation = Quaternion.Euler(90, 0, 0);
            station.StatusLabel.transform.localPosition = new Vector3(0, .8f, 0); station.StatusLabel.characterSize *= .7f; return station;
        }
    }
}
