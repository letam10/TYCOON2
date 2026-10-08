using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tycoon
{
    public static class WorldFactory
    {
        public static void Build(GameSession game)
        {
            var world = new GameObject("World").transform; world.SetParent(game.transform);
            Art.Box("Grass", new Vector3(8, -.13f, 22),
                new Vector3(CityDistricts.GroundWidth, .25f, CityDistricts.GroundDepth), "#94B985", world, true);
            TownLighting.Apply(world);
            SafeStation.Build(game, world);
            WorldDressing.Environment(world);
            // Nền cỏ dùng chung một mặt; không chồng lớp mỏng gây viền bóng rung ở mép.
            Art.Box("StarterSelling",new Vector3(-13,.012f,0),new Vector3(17,.04f,6),"#EDB5A2",world);
            var shopFloor=Floor(world, "02  CỬA HÀNG NÔNG SẢN", new Vector3(7, 0, 6), new Vector2(22, 20));
            shopFloor.gameObject.AddComponent<UnlockVisual>().Requirement="farm_shop";
            Crop(game, world, "field_carrot", "carrot", "CÀ RỐT • LUỐNG 1", new Vector3(-19, 0, 8), 3.8f);
            Crop(game, world, "field_carrot_2", "carrot", "CÀ RỐT • LUỐNG 2", new Vector3(-13, 0, 8), 3.8f);
            Crop(game, world, "field_carrot_3", "carrot", "CÀ RỐT • LUỐNG 3", new Vector3(-7, 0, 8), 3.8f);
            Crop(game, world, "field_tomato", "tomato", "CÀ CHUA", new Vector3(-13, 0, 26), 4.1f);
            Crop(game, world, "field_wheat", "wheat", "LÚA MÌ", new Vector3(-13, 0, 14), 4.3f);
            Crop(game,world,"field_corn","corn","NGÔ",new Vector3(-19,0,14),4);
            Crop(game,world,"field_soybean","soybean","ĐẬU NÀNH",new Vector3(-7,0,14),4);
            Crop(game,world,"field_wheat_2","wheat","LÚA MÌ • 2",new Vector3(-19,0,20),4);
            Crop(game,world,"field_corn_2","corn","NGÔ • 2",new Vector3(-13,0,20),4);
            Crop(game,world,"field_soybean_2","soybean","ĐẬU NÀNH • 2",new Vector3(-7,0,20),4);
            foreach(var plot in game.Producers)
                if((plot.Id.EndsWith("_2") && plot.ItemId!="carrot") || plot.ItemId is "corn" or "soybean")
                {plot.Requirement=plot.Id.EndsWith("_2")?"crop_expansion3":"crop_"+plot.ItemId;plot.gameObject.AddComponent<UnlockVisual>().Requirement=plot.Requirement;}
            foreach (var id in new[] { "field_tomato", "field_wheat" })
            {
                var locked = game.Producers.Find(x => x.Id == id); locked.Requirement = "crop_"+locked.ItemId;
                locked.gameObject.AddComponent<UnlockVisual>().Requirement = locked.Requirement;
                foreach (var station in game.Stations)
                    if (station is StationZone zone && zone.Target == locked)
                    {
                        zone.Requirement = locked.Requirement;
                        zone.gameObject.AddComponent<UnlockVisual>().Requirement = locked.Requirement;
                    }
            }
            var cows=Pen(world, new Vector3(-33, 0, 18), new Vector2(10, 8), "user_cow", 6, 1, "barn", "milk_line");
            var chickens=Pen(world, new Vector3(-39, 0, 28), new Vector2(8, 7), "user_chicken", 3, .9f, "egg_line");
            AnimalProducer(game, world, "dairy_milk", "milk", "SỮA", new Vector3(-26, 0, 18), 12, "milk_line");
            AnimalProducer(game, world, "coop_egg", "egg", "TRỨNG", new Vector3(-31, 0, 25), 12, "egg_line");
            AnimalProducer(game, world, "ranch_beef", "beef", "THỊT BÒ", new Vector3(-26, 0, 12), 45, "barn");
            cows.Bind(game.Producers.FindAll(x=>x.ItemId is "milk" or "beef").ToArray());
            chickens.Bind(game.Producers.FindAll(x=>x.ItemId=="egg").ToArray());
            var sheep=Pen(world,new Vector3(-38,0,39),new Vector2(8,8),"user_sheep",3,1,"sheep_line");
            AnimalProducer(game,world,"sheep_wool","wool","LEN CỪU",new Vector3(-32,0,39),18,"sheep_line");
            sheep.Bind(game.Producers.FindAll(x=>x.ItemId=="wool").ToArray());
            Machine(game,world,"feedmill",new Vector3(-24,0,25),"feed_route");
            game.Storage = Warehouse(game, world, new Vector3(-6, 0, 14), "storage_farm", "farm", 72);
            var farmShopStorage=Warehouse(game, world, new Vector3(3, 0, 13), "storage_farm_shop", "farm_shop", 72);farmShopStorage.Requirement="farm_shop";Locked(farmShopStorage);
            Shelf(game, world, "shelf_farm_1", "CÀ RỐT", new Vector3(-4, 0, 0), new[] { "carrot" }, "farm");
            // Quầy này nằm trong tuyến cơ bản, không phụ thuộc gói mở Farm Shop.
            Shelf(game, world, "shelf_livestock", "SẢN PHẨM CHĂN NUÔI", new Vector3(-1,0,8), new[] { "milk", "egg", "beef","wool" }, "farm");
            Shelf(game, world, "shelf_farm_2", "NÔNG SẢN", new Vector3(14.8f, 0, 6), new[] { "carrot", "tomato", "wheat","corn","soybean", "milk", "egg", "beef","wool", "flour", "cheese", "sauce" }, "farm_shop", "farm_shop");
            Checkout(game, world, "checkout_farm", new Vector3(-13, 0, 0), "farm");
            Checkout(game, world, "checkout_farm_shop", new Vector3(7,0,0), "farm_shop", "farm_shop");
            var processFloor=Floor(world, "03  CHẾ BIẾN", new Vector3(28, 0, 7), new Vector2(18, 18));
            processFloor.gameObject.AddComponent<UnlockVisual>().Requirement="mill";
            Machine(game, world, "mill", new Vector3(23, 0, 12), "mill");
            Machine(game, world, "cheesemaker", new Vector3(28, 0, 12), "mill");
            Machine(game, world, "saucemaker", new Vector3(33, 0, 12), "mill");
            Machine(game,world,"soyextractor",new Vector3(23,0,5),"mill");
            Machine(game,world,"milkbottler",new Vector3(28,0,5),"mill");
            Machine(game,world,"spinner",new Vector3(33,0,5),"mill");
            Machine(game,world,"loom",new Vector3(23,0,-1),"mill");
            FinalBusinesses.Build(game, world);
            var processingStorage=Warehouse(game,world,new Vector3(36,0,2),"storage_processing","processing",72);processingStorage.Requirement="mill";Locked(processingStorage);
            var shopStorage=game.Stations.Find(x=>x.Id=="storage_farm_shop") as StorageStation;
            Conveyor(game,world,"conveyor_mill","wheat",game.Storage,shopStorage,game.Machines.Find(x=>x.Recipe.id=="mill"),-1);
            Conveyor(game,world,"conveyor_cheesemaker","milk",shopStorage,game.Storage,game.Machines.Find(x=>x.Recipe.id=="cheesemaker"),0);
            Conveyor(game,world,"conveyor_saucemaker","tomato",game.Storage,shopStorage,game.Machines.Find(x=>x.Recipe.id=="saucemaker"),1);
            var marketStorage=Warehouse(game,world,new Vector3(18.7f,0,32),"storage_supermarket","supermarket",72);marketStorage.Requirement="supermarket";Locked(marketStorage);
            var bakeryStorage=Warehouse(game,world,new Vector3(-4,0,44),"storage_bakery","bakery",72);bakeryStorage.Requirement="bakery";Locked(bakeryStorage);
            var restaurantStorage=Warehouse(game,world,new Vector3(-30.7f,0,44),"storage_restaurant","restaurant",72);restaurantStorage.Requirement="restaurant";Locked(restaurantStorage);
            var bakeryBelt=Conveyor(game,world,"conveyor_oven","flour",bakeryStorage,bakeryStorage,game.Machines.Find(m=>m.Recipe.id=="breadmixer"),0);
            bakeryBelt.Requirement="conveyor_bakery";bakeryBelt.AreaId="bakery";
            int farmPad = 0, processPad = 0;
            foreach (var upgrade in Definitions.Upgrades)
            {
                if(upgrade.kind=="legacy")continue;
                if(upgrade.axisLevel==3&&upgrade.axis is UpgradeAxis.QualityValue or UpgradeAxis.Speed or UpgradeAxis.Capacity&&upgrade.family is not ("farm" or "player"))continue;
                if (System.Array.IndexOf(new[] { "supermarket", "restocker_market", "cashier_market", "bakery", "cook_bakery", "restaurant", "cook", "waiter" }, upgrade.id) >= 0) continue;
                bool processing = upgrade.id is "mill" or "processor" or "dairy" or "machine_upgrade" or "transport_processing" or "conveyor_processing";
                int index = processing ? processPad++ : farmPad++;
                Vector3 point = processing
                    ? new Vector3(19 + (index % 6) * 3.7f, 0, -5 + (index / 6) * 3)
                    : new Vector3(-23 + (index % 7) * 3.7f, 0, -8 + (index / 7) * 3);
                Pad(game, world, upgrade, point);
            }
            WorldDressing.BackRooms(world);
            TownLayout.Apply(game,world);
            TruckRoutes.BuildDocks(game,world);
            CityDressing.Build(game, world);
            world.gameObject.AddComponent<CityLife>();
            CityPublicTransport.Build(game, world);
            TownSurfaceMaterials.Apply(world);
            world.gameObject.AddComponent<NavigationWorld>();
        }
        public static Transform Floor(Transform parent, string label, Vector3 position, Vector2 size)
        {
            var floor = new GameObject(label).transform; floor.SetParent(parent); floor.position = position;
            string color=label.Contains("NÔNG SẢN")?"#E7C99A":label.Contains("SIÊU THỊ")?"#E7ECDA":"#E7D9C5";
            Art.Box("BusinessFloor", new Vector3(0, .012f, 0), new Vector3(size.x, .04f, size.y), color, floor);
            float x = size.x / 2, z = size.y / 2;
            for(int side=-1;side<=1;side+=2)
            {
                float width=(size.x-4)/2,length=(size.y-4)/2;
                Art.Box("BackWall",new Vector3(side*(2+width/2),.65f,z),new Vector3(width,1.3f,.22f),"#EDE1C9",floor,true);
                Art.Box("BackTrim",new Vector3(side*(2+width/2),1.31f,z),new Vector3(width,.14f,.3f),"#559989",floor);
                Art.Box("RightWall",new Vector3(x,.65f,side*(2+length/2)),new Vector3(.22f,1.3f,length),"#EDE1C9",floor,true);
                Art.Box("RightTrim",new Vector3(x,1.31f,side*(2+length/2)),new Vector3(.3f,.14f,length),"#559989",floor);
            }
            Art.Box("FloorEdge", new Vector3(0, .03f, -z), new Vector3(size.x, .08f, .2f), "#039BDC", floor);
            Art.Box("LeftEdge", new Vector3(-x, .03f, 0), new Vector3(.2f, .08f, size.y), "#039BDC", floor);
            Art.Label(label, new Vector3(-x + 4, .06f, -z + 1), floor, .25f, "#FFFFFF", false).transform.rotation = Quaternion.Euler(90, 0, 0);
            WorldDressing.Floor(floor,size,label);
            return floor;
        }
        static LivestockPopulationView Pen(Transform parent, Vector3 position, Vector2 size, string key, int count, float scale, string requirement, string alternative = "")
        {
            var pen = new GameObject("Pen_" + key).transform;
            pen.SetParent(parent, false);
            var visual = pen.gameObject.AddComponent<UnlockVisual>();
            visual.Requirement = requirement;
            if (!string.IsNullOrEmpty(alternative)) visual.AnyRequirements = new[] { requirement,alternative };
            int horizontal=Mathf.CeilToInt(size.x/2.2f),vertical=Mathf.CeilToInt(size.y/2.2f);
            float stepX=size.x/horizontal,stepZ=size.y/vertical;
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < horizontal; i++)
                    Art.Model("farm_fence", position + new Vector3(-size.x / 2 + stepX*.5f + i * stepX, 0, (side == 0 ? -1 : 1) * size.y / 2), pen).transform.localScale=new Vector3(stepX/2.2f,1,1);
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < vertical; i++)
                    Art.Model("farm_fence", position + new Vector3((side == 0 ? -1 : 1) * size.x / 2, 0, -size.y / 2 + stepZ*.5f + i * stepZ), pen, 1, 90).transform.localScale=new Vector3(stepZ/2.2f,1,1);
            int columns = 3;
            var members=new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                var animal = Art.Model(key, position + new Vector3(-size.x / 2 + 1.3f + (i % columns) * (size.x - 2.6f) / (columns - 1), .03f, -size.y / 2 + 1.3f + (i / columns) * 2.7f), pen, scale, 190 + i % 3 * 18);
                if(animal.GetComponentInChildren<Animator>()){var view=animal.AddComponent<ActorView>();view.Initialize();view.SetMotion(0,false);}
                animal.AddComponent<AnimalMotion>().Phase=i*.8f;
                members.Add(animal);
            }
            Art.Box("PenCollision", position + new Vector3(0, .6f, 0), new Vector3(size.x, 1.2f, size.y), "#CBA671", pen, true).GetComponent<Renderer>().enabled = false;
            WorldDressing.Livestock(pen,position,key=="user_chicken");
            var population=pen.gameObject.AddComponent<LivestockPopulationView>();population.Members=members.ToArray();return population;
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
            station.Yield=item=="carrot"?2:4;
            station.gameObject.AddComponent<FarmPlotView>().Build();
            var plants = new List<Transform>();
            for (int i = 0; i < 9; i++)
            {
                var plant=Art.Model(item=="carrot"?"carrot_crop":item=="wheat"?"wheat_crop":item=="corn"?"corn_plant":item=="soybean"?"soybean_plant":"crop_leaves",new Vector3(-.95f+i%3*.95f,.14f,-.75f+i/3*.75f),station.transform);
                if(item=="tomato")Art.Model("tomato",new Vector3(0,.20f,0),plant.transform,.65f);
                plants.Add(plant.transform);
            }
            station.Plants = plants.ToArray(); station.InteractionPoint = point + Vector3.back * 2.25f; game.Producers.Add(station);
            Zone(game, parent, station, "operate", station.InteractionPoint + Vector3.back);
            Zone(game, parent, station, "withdraw", station.InteractionPoint + Vector3.right * 1.7f);
        }
        static void AnimalProducer(GameSession game, Transform parent, string id, string item, string label, Vector3 point, float interval, string requirement)
        {
            var station = Register<ProductionStation>(game, parent, id, label, point, 36); station.AreaId = "farm"; station.ItemId = item; station.Interval = interval; station.Remaining = interval; station.Requirement = requirement; station.Cycle = interval;
            Art.Model("feed_trough",Vector3.zero,station.transform);
            var stockSurface = new Vector3(.8f, .72f, -.3f);
            StationStockSurface.Build(station, stockSurface, new Vector2(1.3f, 1.45f), true);
            Stack(game, station, stockSurface, .75f, 2, 2, 24);
            game.Producers.Add(station);
            Locked(station);
            Zone(game, parent, station, "operate", station.InteractionPoint + Vector3.back);
            Zone(game, parent, station, "restock", point+Vector3.right*4.3f+Vector3.back*(item=="egg"?.8f:2));
            Zone(game, parent, station, "withdraw", point+Vector3.right*2.1f+Vector3.back*.8f);
            Zone(game, parent, station, "deposit", point+Vector3.right*(item=="egg"?3.8f:2.1f)+Vector3.back*3.3f);
        }
        public static StorageStation Warehouse(GameSession game, Transform parent, Vector3 point, string id = "storage", string area = "farm", int capacity = 800)
        {
            var station = Register<StorageStation>(game, parent, id, "KHO • " + GameHud.AreaLabel(area), point, capacity); station.AreaId = area;
            station.ConfigureItemBins(capacity);
            Art.Box("StorageBase", new Vector3(0, .08f, 0), new Vector3(3.4f, .15f, 3.4f), "#FFFFFF", station.transform);
            Art.Box("StorageCollision",new Vector3(0,.55f,.5f),new Vector3(3.1f,1.1f,2.5f),"#FFFFFF",station.transform,true).GetComponent<Renderer>().enabled=false;
            WorldDressing.Storage(station.transform);
            Stack(game, station, new Vector3(0, .16f, -.45f), .7f);
            Zone(game, parent, station, "withdraw", station.InteractionPoint + Vector3.left * 1.6f);
            Zone(game, parent, station, "deposit", station.InteractionPoint + Vector3.right * 1.6f);
            return station;
        }
        public static ShelfStation Shelf(GameSession game, Transform parent, string id, string label, Vector3 point, string[] items, string shop, string requirement = "")
        {
            var station = Register<ShelfStation>(game, parent, id, label, point, 36); station.AllowedItems = items; station.ShopId = shop; station.AreaId = shop == "farm" ? "farm_shop" : shop == "market" ? "supermarket" : shop; station.Requirement = requirement; station.InteractionPoint = point + Vector3.left * 1.5f;
            foreach(string item in items)station.Inventory.SetLimit(item,items.Length==1?36:6);
            Art.Box("ShelfCollision", new Vector3(0, .45f, 0), new Vector3(1.4f, .9f, 4.7f), "#039BDD", station.transform, true).GetComponent<Renderer>().enabled=false;
            Art.Model("display_counter",Vector3.zero,station.transform);
            var stockSurface = new Vector3(0, 1.04f, -.25f);
            StationStockSurface.Build(station, stockSurface, new Vector2(1.2f, 3.4f), false);
            Stack(game, station, stockSurface, .75f, 2, 5, 36);
            game.Shelves.Add(station);
            Locked(station);
            Zone(game, parent, station, "withdraw", station.InteractionPoint + Vector3.left * 1.2f);
            Zone(game, parent, station, "deposit", station.InteractionPoint + Vector3.back * 1.6f);
            return station;
        }
        public static CheckoutStation Checkout(GameSession game, Transform parent, string id, Vector3 point, string shop, string requirement = "")
        {
            var station = Register<CheckoutStation>(game, parent, id, "QUẦY "+GameHud.AreaLabel(shop).ToUpperInvariant(), point); station.ShopId = shop; station.AreaId = shop == "farm" ? "farm_shop" : shop == "market" ? "supermarket" : shop; station.Requirement = requirement; station.Inventory=new Inventory(36);
            WorldDressing.Checkout(station.transform);
            Art.Box("CheckoutCollision", new Vector3(.3f, .55f, 0), new Vector3(2.1f, 1.1f, .9f), "#05A4E3", station.transform, true).GetComponent<Renderer>().enabled = false;
            var stockSurface = new Vector3(-.25f, 1.1f, .1f);
            StationStockSurface.Build(station, stockSurface, new Vector2(1.05f, .96f), false);
            Stack(game, station, stockSurface, .5f, 3, 2, 36);
            station.InteractionPoint = point + Vector3.left * 2.7f + Vector3.forward * .6f;
            Zone(game, parent, station, "serve", station.InteractionPoint);
            Zone(game,parent,station,"deposit",point+Vector3.forward*2.6f);
            Zone(game, parent, station, "cash", point + Vector3.right * 2.7f + Vector3.forward * .6f);
            game.Checkouts.Add(station); Locked(station); return station;
        }
        public static void Zone(GameSession game, Transform parent, Station target, string mode, Vector3 point)
        {
            // Điểm work của NPC được giữ; player tương tác với vật thể, không sinh ô hành động.
            if(target is CheckoutStation && mode=="cash")
                Art.Box("CashStand",new Vector3(2.7f,.35f,.6f),new Vector3(1.1f,.7f,.7f),"#8B6B49",target.transform);
        }
        public static MachineStation Machine(GameSession game, Transform parent, string key, Vector3 point, string requirement)
        {
            var recipe = Definitions.Recipe(key); var station = Register<MachineStation>(game, parent, "machine_" + key, recipe.label, point, 36); station.Recipe = recipe; station.Requirement = requirement; station.AreaId = key is "oven" or "cakeoven" ? "bakery" : key == "kitchen" ? "restaurant" : "processing";
            if(key is "breadmixer" or "cakemixer")station.AreaId="bakery";
            if(key=="feedmill")station.AreaId="farm";
            if(key=="kitchen"){station.RecipeOptions=Definitions.KitchenRecipes;station.Recipe=Definitions.Recipe("cook_beef");station.Label="BẾP NHÀ HÀNG";}
            station.ConfigureInputLimits();
            WorldDressing.Machine(station);
            if(key is "oven" or "cakeoven" or "kitchen")station.StatusLabel.transform.localPosition=new Vector3(0,3.0f,0);
            Art.Box("MachineCollision", new Vector3(0, .9f, 0), new Vector3(2.65f, 1.8f, 2.25f), "#039BDD", station.transform, true).GetComponent<Renderer>().enabled = false;
            station.InteractionPoint = point + Vector3.back * 2.3f;
            var stockSurface = new Vector3(2, .8f, -.75f);
            StationStockSurface.Build(station, stockSurface, new Vector2(.9f, 1.3f), true);
            Stack(game, station, stockSurface, .55f, 1, 2, 12);
            game.Machines.Add(station);
            Locked(station);
            Zone(game, parent, station, "operate", station.InteractionPoint + Vector3.back * 1.25f);
            Zone(game, parent, station, "withdraw", station.InteractionPoint + Vector3.right * 1.55f + Vector3.back * .35f);
            Zone(game, parent, station, "deposit", station.InteractionPoint + Vector3.left * 1.55f + Vector3.back * .35f);
            Zone(game,parent,station,"repair",key=="kitchen"?point+Vector3.right*4.1f+Vector3.back*1.1f:point+Vector3.back*5.9f);
            return station;
        }
        static ConveyorStation Conveyor(GameSession game,Transform parent,string id,string item,StorageStation primary,StorageStation alternate,MachineStation target,float laneOffset)
        {
            Vector3 start=primary.transform.position+new Vector3(1.8f,0,laneOffset);
            Vector3 end=target.transform.position+Vector3.left*2.1f+Vector3.forward*laneOffset;
            var route=Register<ConveyorStation>(game,parent,id,"BĂNG CHUYỀN • "+Definitions.Item(item).label,(start+end)*.5f,Mathf.Max(4,Definitions.Recipe(target.Recipe.id).inputs[0].count));
            route.AreaId="processing";route.Requirement="conveyor_processing";route.Target=target;route.ItemId=item;route.Sources=primary==alternate?new[]{primary}:new[]{primary,alternate};
            var belt=new GameObject("BeltVisuals");belt.transform.SetParent(route.transform,false);belt.SetActive(false);route.VisualRoot=belt;
            Vector3 direction=end-start;float length=direction.magnitude;
            belt.transform.position=start;belt.transform.rotation=Quaternion.LookRotation(direction);
            Art.Box("Belt",new Vector3(0,.12f,length*.5f),new Vector3(.7f,.18f,length),"#46524A",belt.transform);
            Art.Box("BeltRailLeft",new Vector3(-.4f,.22f,length*.5f),new Vector3(.08f,.16f,length),"#E2C45D",belt.transform);
            Art.Box("BeltRailRight",new Vector3(.4f,.22f,length*.5f),new Vector3(.08f,.16f,length),"#E2C45D",belt.transform);
            return route;
        }
        static void Locked(Station station) { if (!string.IsNullOrEmpty(station.Requirement)) station.gameObject.AddComponent<UnlockVisual>().Requirement = station.Requirement; }
        public static PurchasePad Pad(GameSession game, Transform parent, UpgradeDefinition upgrade, Vector3 point)
        {
            var station = Register<PurchasePad>(game, parent, "pad_" + upgrade.id, upgrade.label, point); station.Upgrade = upgrade; station.Requirement = upgrade.requirement; station.InteractionPoint = point; station.InteractionRadius=1.35f;
            Art.Box("PadBorder", new Vector3(0, .03f, 0), new Vector3(1.5f, .04f, 1.5f), "#F3E9CC", station.transform); Art.Box("PadFace", new Vector3(0, .06f, 0), new Vector3(1.3f, .025f, 1.3f), "#36564B", station.transform);
            Art.Label("↑\n" + upgrade.cost, new Vector3(0, .08f, 0), station.transform, .31f, "#FFFFFF", false).transform.rotation = Quaternion.Euler(90, 0, 0);
            station.StatusLabel.transform.localPosition = new Vector3(0, .8f, 0); station.StatusLabel.characterSize *= .7f; return station;
        }
    }
}
