using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    [Serializable]
    public sealed class ItemDefinition
    {
        public readonly string id;
        public readonly string label;
        public readonly int price;
        public readonly Color color;
        public readonly string model;
        public int version => 1;
        public ItemDefinition(string id, string label, int price, string hex, string model = null)
        {
            this.id = id; this.label = label; this.price = price; this.model = model ?? id;
            ColorUtility.TryParseHtmlString(hex, out color);
        }
    }

    [Serializable]
    public sealed class ItemAmount
    {
        public string id;
        public int count;
        public ItemAmount() { }
        public ItemAmount(string id, int count) { this.id = id; this.count = count; }
    }

    [Serializable]
    public sealed class RecipeDefinition
    {
        public readonly string id;
        public readonly string label;
        readonly ItemAmount[] ingredients;
        public ItemAmount[] inputs => Array.ConvertAll(ingredients, x => new ItemAmount(x.id, x.count));
        public readonly string output;
        public readonly int yield;
        public readonly float seconds;
        public int version => id is "oven" or "cakeoven" ? 2 : 1;
        public RecipeDefinition(string id, string label, string output, int yield, float seconds, params ItemAmount[] inputs)
        {
            this.id = id; this.label = label; this.output = output;
            this.yield = yield; this.seconds = seconds;
            ingredients = Array.ConvertAll(inputs, x => new ItemAmount(x.id, x.count));
        }
        public bool CanMake(Inventory inventory)
        {
            foreach (var input in ingredients) if (inventory.Available(input.id) < input.count) return false;
            return true;
        }
        public bool Consume(Inventory inventory)
        {
            if (!CanMake(inventory)) return false;
            foreach (var input in inputs) inventory.TryRemove(input.id, input.count);
            return true;
        }
    }

    [Serializable]
    public sealed class UpgradeDefinition
    {
        public readonly string id;
        public readonly string label;
        public readonly string requirement;
        public readonly int cost;
        public readonly string kind;
        public readonly string role;
        public readonly string family;
        public readonly UpgradeAxis axis;
        public readonly int axisLevel, capacityValue;
        public readonly ProgressionRequirement progression;
        public int version => 1;
        public UpgradeDefinition(string id, string label, int cost, string requirement = "", string kind = "unlock", string role = "",
            string family = "", UpgradeAxis axis = UpgradeAxis.None, int axisLevel = 0, int capacityValue = 0,
            ProgressionRequirement progression = null)
        {
            this.id = id; this.label = label; this.cost = cost; this.requirement = requirement;
            this.kind = kind; this.role = role; this.family=family;this.axis=axis;this.axisLevel=axisLevel;
            this.capacityValue=capacityValue;this.progression=progression;
        }
    }

    public enum UpgradeAxis { None, StationLevel, QualityValue, Speed, Capacity }
    [Serializable] public sealed class RecipeBatchRequirement { public string recipeId; public int batches; }
    [Serializable] public sealed class ProgressionRequirement
    {
        public string stationFamily, successfulOrderArea, successfulJobArea;
        public int stationLevel, successfulOrders, successfulOrdersAtArea, successfulJobs;
        public int playerCookJobs, playerTableServes, playerTableCleans;
        public string[] anyStationFamilies = Array.Empty<string>();
        public int anyStationLevel;
        public string[] allUnlocks = Array.Empty<string>(), anyUnlocks = Array.Empty<string>();
        public RecipeBatchRequirement[] recipeBatches = Array.Empty<RecipeBatchRequirement>();
    }

    public static class Definitions
    {
        public const int Version = 1;
        public static ItemDefinition[] Items => (ItemDefinition[])items.Clone();
        static readonly ItemDefinition[] items = {
            new("carrot", "Cà rốt", 10, "#F68D4C"),
            new("tomato", "Cà chua", 14, "#EF6461"),
            new("wheat", "Lúa mì", 12, "#E9C26A"),
            new("corn", "Ngô", 14, "#F5D64F"),
            new("soybean", "Đậu nành", 16, "#B8BD70"),
            new("milk", "Sữa", 35, "#E3F2F5"),
            new("egg", "Trứng", 25, "#F6E5BA"),
            new("flour", "Bột mì", 120, "#EED9AD"),
            new("cheese", "Phô mai", 240, "#F4CC50"),
            new("sauce", "Sốt cà chua", 120, "#C84648"),
            new("bread", "Bánh mì", 300, "#C58C4E"),
            new("cake", "Bánh kem", 600, "#F4ABC5"),
            new("meal", "Suất ăn", 600, "#AFC986"),
            new("beef", "Thịt bò", 60, "#F4654E")
            ,new("animal_feed","Thức ăn vật nuôi",20,"#C6AD75")
            ,new("soy_sauce","Tương đậu nành",120,"#6F3C25")
            ,new("bottled_milk","Sữa đóng chai",80,"#F1EFE1")
            ,new("wool","Len thô",35,"#F3EBDE")
            ,new("yarn","Sợi len",120,"#E5C792")
            ,new("cloth","Vải len",240,"#71A5B3")
            ,new("bread_dough","Bột bánh mì",180,"#E0C695")
            ,new("cake_batter","Bột bánh kem",300,"#E8C58B")
            ,new("beef_soy","Bò sốt tương",720,"#94613D")
            ,new("corn_soup","Súp ngô sữa",650,"#E2C05A")
            ,new("pasta","Mì sốt cà phô mai",720,"#DC824E")
            ,new("egg_sandwich","Bánh mì kẹp trứng",650,"#D9AD6D")
            ,new("soy_vegetables","Đậu nành xào rau",650,"#87B85D")
        };
        public static RecipeDefinition[] Recipes => (RecipeDefinition[])recipes.Clone();
        static readonly RecipeDefinition[] recipes = {
            new("mill", "Xay bột mì", "flour", 3, 6, new ItemAmount("wheat", 4)),
            new("cheesemaker", "Làm phô mai", "cheese", 2, 8, new ItemAmount("milk", 3)),
            new("saucemaker", "Nấu sốt", "sauce", 3, 7, new ItemAmount("tomato", 4)),
            new("oven", "Nướng bánh mì", "bread", 3, 6, new ItemAmount("bread_dough", 3)),
            new("cakeoven", "Nướng bánh kem", "cake", 2, 7, new ItemAmount("cake_batter", 2)),
            new("breadmixer", "Trộn bột bánh mì", "bread_dough", 3, 3, new ItemAmount("flour", 2),new ItemAmount("egg",1),new ItemAmount("milk",1)),
            new("cakemixer", "Trộn bột bánh kem", "cake_batter", 2, 4, new ItemAmount("flour",2),new ItemAmount("egg",2),new ItemAmount("milk",2)),
            new("feedmill","Trộn thức ăn vật nuôi","animal_feed",3,6,new ItemAmount("wheat",1),new ItemAmount("corn",1),new ItemAmount("soybean",1)),
            new("soyextractor","Làm tương đậu nành","soy_sauce",3,7,new ItemAmount("soybean",4)),
            new("milkbottler","Chiết sữa","bottled_milk",2,5,new ItemAmount("milk",2)),
            new("spinner","Kéo sợi len","yarn",2,6,new ItemAmount("wool",3)),
            new("loom","Dệt vải len","cloth",2,8,new ItemAmount("yarn",2)),
            new("cook_beef","Nấu bò sốt tương","beef_soy",2,10,new ItemAmount("beef",1),new ItemAmount("soy_sauce",1),new ItemAmount("tomato",1),new ItemAmount("carrot",1)),
            new("cook_soup","Nấu súp ngô sữa","corn_soup",2,10,new ItemAmount("corn",2),new ItemAmount("milk",1),new ItemAmount("flour",1),new ItemAmount("carrot",1)),
            new("cook_pasta","Nấu mì sốt cà","pasta",2,10,new ItemAmount("flour",2),new ItemAmount("egg",1),new ItemAmount("sauce",1),new ItemAmount("cheese",1)),
            new("cook_sandwich","Làm bánh mì kẹp trứng","egg_sandwich",2,10,new ItemAmount("bread",1),new ItemAmount("egg",1),new ItemAmount("tomato",1)),
            new("cook_vegetables","Xào đậu nành rau","soy_vegetables",2,10,new ItemAmount("soybean",2),new ItemAmount("corn",1),new ItemAmount("carrot",1),new ItemAmount("sauce",1)),
            new("kitchen", "Nấu suất ăn", "meal", 2, 10, new ItemAmount("carrot", 2), new ItemAmount("tomato", 2), new ItemAmount("cheese", 1), new ItemAmount("bread", 1))
        };
        public static UpgradeDefinition[] Upgrades => (UpgradeDefinition[])upgrades.Clone();
        static readonly UpgradeDefinition[] upgrades = {
            new("farmer", "Thuê nông dân", 150, "", "worker", "Farmer"),
            new("restocker", "Thuê xếp hàng Farm Shop", 200, "", "worker", "Restocker"),
            new("cashier", "Thuê bán hàng Farm Shop", 250, "", "worker", "Cashier"),
            new("transport_farm_shop", "Thuê vận chuyển Farm Shop", 350, "farm_shop", "worker", "Transporter"),
            new("barn", "Tuyến thịt bò: chuồng, đàn, trạm và quầy", 500,"farm_level2"),
            new("milk_line", "Tuyến sữa: chuồng, đàn, trạm và quầy", 1000,"farm_level2"),
            new("egg_line", "Tuyến trứng: chuồng, đàn, trạm và quầy", 1000,"farm_level2"),
            new("sheep_line", "Tuyến cừu: chuồng, đàn, len và quầy",1000,"farm_level2"),
            new("animal_worker", "Thuê chăm vật nuôi", 300, "", "worker", "Animal"),
            new("repair_farm","Thuê kỹ thuật viên Farm",500,"farm_level2","worker","Repairer"),
            new("repair_processing","Thuê kỹ thuật viên Processing",1500,"mill","worker","Repairer"),
            new("repair_bakery","Thuê kỹ thuật viên Bakery",2000,"bakery","worker","Repairer"),
            new("repair_restaurant","Thuê kỹ thuật viên Restaurant",2000,"restaurant","worker","Repairer"),
            new("farm_shop", "Mở rộng Farm Shop", 2000, progression:new ProgressionRequirement { anyStationFamilies=new[]{"farm","animal"}, anyStationLevel=3, successfulOrders=50 }),
            new("mill", "Mở khu Processing", 12000, "farm_shop", progression:new ProgressionRequirement { stationFamily="animal", stationLevel=3, successfulOrders=200 }),
            new("processor", "Thuê chế biến", 750, "mill", "worker", "Processor"),
            new("transport_processing", "Thuê vận chuyển Processing", 600, "mill", "worker", "Transporter"),
            new("storage_processing_level2","Kho Processing cấp 2",500,"mill","upgrade",family:"storage_processing",axis:UpgradeAxis.StationLevel,axisLevel:2),
            new("storage_processing_level3","Kho Processing cấp 3",1500,"storage_processing_level2","upgrade",family:"storage_processing",axis:UpgradeAxis.StationLevel,axisLevel:3),
            new("truck_bundle","Xe tải + tài xế + bến hàng",5000,"mill","worker","Driver"),
            new("loader_farm","Thuê bốc hàng Farm",600,"truck_bundle","worker","Loader"),
            new("loader_farm_shop","Thuê bốc hàng Farm Shop",750,"truck_bundle","worker","Loader"),
            new("loader_processing","Thuê bốc hàng Processing",1000,"truck_bundle","worker","Loader"),
            new("loader_supermarket","Thuê bốc hàng Supermarket",1500,"truck_bundle","worker","Loader"),
            new("loader_bakery","Thuê bốc hàng Bakery",1500,"truck_bundle","worker","Loader"),
            new("loader_restaurant","Thuê bốc hàng Restaurant",1500,"truck_bundle","worker","Loader"),
            new("dairy", "Gói máy cũ đã gộp vào Processing", 1800, "mill", "legacy"),
            new("supermarket", "Mở Supermarket", 65000, "mill", progression:new ProgressionRequirement {
                successfulOrders=600, recipeBatches=new[] { new RecipeBatchRequirement { recipeId="mill", batches=50 },
                    new RecipeBatchRequirement { recipeId="cheesemaker", batches=50 }, new RecipeBatchRequirement { recipeId="saucemaker", batches=50 } } }),
            new("restocker_market", "Thuê xếp hàng Supermarket", 1500, "supermarket", "worker", "RestockerMarket"),
            new("cashier_market", "Thuê bán hàng Supermarket", 1500, "supermarket", "worker", "CashierMarket"),
            new("bakery", "Mở Bakery", 100000, "supermarket"),
            new("cook_bakery", "Thuê thợ bánh", 2000, "bakery", "worker", "Baker"),
            new("transport_bakery", "Thuê vận chuyển Bakery", 1500, "bakery", "worker", "Transporter"),
            new("cashier_bakery", "Thuê bán bánh", 2000, "bakery", "worker", "Cashier"),
            new("restaurant", "Mở Restaurant", 80000, "bakery", progression:new ProgressionRequirement { stationFamily="oven", stationLevel=3, successfulOrderArea="bakery", successfulOrdersAtArea=60 }),
            new("cook", "Thuê đầu bếp", 2500, "restaurant", "worker", "Cook",
                progression:new ProgressionRequirement{playerCookJobs=1,playerTableServes=1,playerTableCleans=1}),
            new("waiter", "Thuê phục vụ", 2000, "restaurant", "worker", "Waiter",
                progression:new ProgressionRequirement{playerCookJobs=1,playerTableServes=1,playerTableCleans=1}),
            new("transport_restaurant", "Thuê vận chuyển Restaurant", 2000, "restaurant", "worker", "Transporter",
                progression:new ProgressionRequirement{playerCookJobs=1,playerTableServes=1,playerTableCleans=1}),
            new("farm_level2", "Cây trồng cấp 2", 100, "", "upgrade", family:"farm", axis:UpgradeAxis.StationLevel, axisLevel:2),
            new("farm_level3", "Cây trồng cấp 3", 300, "farm_level2", "upgrade", family:"farm", axis:UpgradeAxis.StationLevel, axisLevel:3),
            new("counter_level2", "Quầy Farm Shop cấp 2", 120, "", "upgrade", family:"counter", axis:UpgradeAxis.StationLevel, axisLevel:2),
            new("counter_level3", "Quầy Farm Shop cấp 3", 360, "counter_level2", "upgrade", family:"counter", axis:UpgradeAxis.StationLevel, axisLevel:3),
            new("animal_level2", "Chăn nuôi cấp 2", 250, "", "upgrade", family:"animal", axis:UpgradeAxis.StationLevel, axisLevel:2,
                progression:new ProgressionRequirement { anyUnlocks=new[]{"barn","milk_line","egg_line"} }),
            new("animal_level3", "Chăn nuôi cấp 3", 750, "animal_level2", "upgrade", family:"animal", axis:UpgradeAxis.StationLevel, axisLevel:3),
            new("mill_level2", "Máy Processing cấp 2", 800, "mill", "upgrade", family:"mill", axis:UpgradeAxis.StationLevel, axisLevel:2),
            new("mill_level3", "Máy Processing cấp 3", 2400, "mill_level2", "upgrade", family:"mill", axis:UpgradeAxis.StationLevel, axisLevel:3),
            new("market_level2", "Supermarket cấp 2", 4000, "supermarket", "upgrade", family:"market", axis:UpgradeAxis.StationLevel, axisLevel:2),
            new("market_level3", "Supermarket cấp 3", 12000, "market_level2", "upgrade", family:"market", axis:UpgradeAxis.StationLevel, axisLevel:3),
            new("oven_level2", "Lò Bakery cấp 2", 5000, "bakery", "upgrade", family:"oven", axis:UpgradeAxis.StationLevel, axisLevel:2),
            new("oven_level3", "Lò Bakery cấp 3", 15000, "oven_level2", "upgrade", family:"oven", axis:UpgradeAxis.StationLevel, axisLevel:3),
            new("kitchen_level2", "Bếp Restaurant cấp 2", 5000, "restaurant", "upgrade", family:"kitchen", axis:UpgradeAxis.StationLevel, axisLevel:2),
            new("kitchen_level3", "Bếp Restaurant cấp 3", 15000, "kitchen_level2", "upgrade", family:"kitchen", axis:UpgradeAxis.StationLevel, axisLevel:3),
            new("farm_value2", "Chất lượng nông sản cấp 2", 250, "", "upgrade", family:"farm", axis:UpgradeAxis.QualityValue, axisLevel:2),
            new("farm_value3", "Chất lượng nông sản cấp 3", 750, "farm_value2", "upgrade", family:"farm", axis:UpgradeAxis.QualityValue, axisLevel:3),
            new("farm_speed2", "Tốc độ trồng cấp 2", 60, "", "upgrade", family:"farm", axis:UpgradeAxis.Speed, axisLevel:2),
            new("farm_speed3", "Tốc độ trồng cấp 3", 180, "farm_speed2", "upgrade", family:"farm", axis:UpgradeAxis.Speed, axisLevel:3),
            new("farm_capacity2", "Sức chứa luống cấp 2", 150, "", "upgrade", family:"farm", axis:UpgradeAxis.Capacity, axisLevel:2, capacityValue:36),
            new("farm_capacity3", "Sức chứa luống cấp 3", 450, "farm_capacity2", "upgrade", family:"farm", axis:UpgradeAxis.Capacity, axisLevel:3, capacityValue:48),
            new("carry10", "Sức mang 10", 100, "", "upgrade", family:"player", axis:UpgradeAxis.Capacity, axisLevel:2, capacityValue:10),
            new("carry16", "Sức mang 16", 600, "carry10", "upgrade", family:"player", axis:UpgradeAxis.Capacity, axisLevel:3, capacityValue:16),
            new("carry24", "Sức mang 24", 2500, "carry16", "upgrade", family:"player", axis:UpgradeAxis.Capacity, axisLevel:4, capacityValue:24),
            new("conveyor_processing", "Băng chuyền kho → máy Chế biến", 3000, "mill"),
            new("conveyor_bakery", "Băng chuyền kho → mixer Tiệm bánh", 8000, "bakery"),
            new("farmer_2", "Nhân viên cũ", 200, "farmer", "legacy"),
            new("carry_upgrade", "Giỏ cũ", 110, "", "legacy"),
            new("farm_upgrade", "Nâng cấp cũ", 160, "farmer", "legacy"),
            new("worker_upgrade", "Nâng cấp cũ", 260, "cashier", "legacy"),
            new("machine_upgrade", "Nâng cấp cũ", 320, "processor", "legacy"),
        };
        static readonly Dictionary<string, ItemDefinition> byItem = new();
        public static bool IsCrop(string id)=>id is "carrot" or "tomato" or "wheat" or "corn" or "soybean";
        public static bool IsAnimal(string id)=>id is "milk" or "egg" or "beef" or "wool";
        public static string[] KitchenRecipes=>new[]{"cook_beef","cook_soup","cook_pasta","cook_sandwich","cook_vegetables","kitchen"};
        public static RecipeDefinition LegacyRecipe(string id)=>id=="oven"?new("legacy_bread","Mẻ bánh mì cũ","bread",3,9,new ItemAmount("flour",2),new ItemAmount("egg",1),new ItemAmount("milk",1)):
            id=="cakeoven"?new("legacy_cake","Mẻ bánh kem cũ","cake",2,11,new ItemAmount("flour",2),new ItemAmount("egg",2),new ItemAmount("milk",2)):Recipe(id);
        static readonly Dictionary<string, RecipeDefinition> byRecipe = new();
        static readonly Dictionary<string, UpgradeDefinition> byUpgrade = new();
        static Definitions()
        {
            var axes=new List<UpgradeDefinition>(upgrades);
            void Axis(string family,string suffix,string label,UpgradeAxis axis,int price,string requirement,int capacity2=0,int capacity3=0)
            {
                axes.Add(new(family+"_"+suffix+"2",label+" cấp 2",price,requirement,"upgrade",family:family,axis:axis,axisLevel:2,capacityValue:capacity2));
                axes.Add(new(family+"_"+suffix+"3",label+" cấp 3",price*3,family+"_"+suffix+"2","upgrade",family:family,axis:axis,axisLevel:3,capacityValue:capacity3));
            }
            foreach(var row in new[]{("mill","Chế biến","mill",600),("oven","Tiệm bánh","bakery",2500),("kitchen","Nhà hàng","restaurant",2500)})
            {
                Axis(row.Item1,"value","Chất lượng "+row.Item2,UpgradeAxis.QualityValue,row.Item4,row.Item3);
                Axis(row.Item1,"speed","Tốc độ "+row.Item2,UpgradeAxis.Speed,row.Item4,row.Item3);
                Axis(row.Item1,"capacity","Sức chứa "+row.Item2,UpgradeAxis.Capacity,row.Item4,row.Item3,54,72);
            }
            Axis("animal","value","Chất lượng chăn nuôi",UpgradeAxis.QualityValue,400,"farm_level2");
            Axis("animal","speed","Tốc độ chăn nuôi",UpgradeAxis.Speed,400,"farm_level2");
            Axis("counter","capacity","Sức chứa quầy nông sản",UpgradeAxis.Capacity,300,"",54,72);
            Axis("market","capacity","Sức chứa quầy Siêu thị",UpgradeAxis.Capacity,2500,"supermarket",54,72);
            foreach(var area in new[]{"farm","farm_shop","processing","supermarket","bakery","restaurant"})
            {string requirement=area=="farm"?"":area=="processing"?"mill":area;Axis("storage_"+area,"capacity","Sức chứa kho "+GameHud.AreaLabel(area),UpgradeAxis.Capacity,area=="farm"?300:area=="farm_shop"?600:area=="processing"?1200:3000,requirement,144,216);}
            upgrades=axes.ToArray();
            foreach (var item in Items) byItem.Add(item.id, item);
            foreach (var recipe in Recipes) byRecipe.Add(recipe.id, recipe);
            foreach (var upgrade in Upgrades) byUpgrade.Add(upgrade.id, upgrade);
        }
        public static ItemDefinition Item(string id) => byItem.TryGetValue(id, out var item) ? item : null;
        public static RecipeDefinition Recipe(string id) => byRecipe.TryGetValue(id, out var recipe) ? recipe : null;
        public static UpgradeDefinition Upgrade(string id) => byUpgrade.TryGetValue(id, out var upgrade) ? upgrade : null;
    }

    [Serializable]
    public sealed class ModelEntry
    {
        public string key;
        public GameObject prefab;
        public Vector3 size;
        public ModelEntry(string key, GameObject prefab) { this.key = key; this.prefab = prefab; }
    }
}
