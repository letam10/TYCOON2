using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    [Serializable] public sealed class PurchaseProgress { public string id; public int paid; }
    [Serializable] public sealed class CrewState
    {
        public string id, role, area;
        public int count = 1, speedLevel = 1, carryLevel = 1;
    }
    [Serializable] public sealed class EventState
    {
        public float untilRush = 480, warning, rushRemaining;
        public int lastBreakBatch;
    }
    public sealed partial class GameSession
    {
        public List<PurchaseProgress> Purchases = new();
        public List<CrewState> CrewStates = new();
        public List<CustomerSave> PendingCustomers = new();
        public List<WorkerSave> PendingWorkers = new();
        public EventState Events = new();
        public bool RushActive => Events.rushRemaining > 0;
        public string ObjectiveText
        {
            get
            {
                string id = !Economy.Has("farm_shop") ? "farm_shop" : !Economy.Has("mill") ? "mill" : !Economy.Has("supermarket") ? "supermarket" : !Economy.Has("bakery") ? "bakery" : !Economy.Has("restaurant") ? "restaurant" : "";
                if (id == "") return "Vận hành năm khu • thu tiền và xử lý chỗ thiếu hàng";
                var u = Definitions.Upgrade(id); CanPurchase(u, out var why);
                return u.label + " • " + (why == "" ? Contribution(id) + "/" + u.cost + " xu" : why);
            }
        }
        public StorageStation StorageFor(string area)
        {
            if (area == "market") area = "supermarket";
            return Stations.Find(x => x is StorageStation && x.AreaId == area) as StorageStation ?? Storage;
        }
        public int ItemPrice(string id)
        {
            var item = Definitions.Item(id); if (item == null) return 0;
            int tier = id is "carrot" or "wheat" or "tomato" ? Tier("farm") : 1;
            int price = Mathf.CeilToInt(item.price * (1 + .25f * (tier - 1)));
            // Thành phẩm luôn có lãi so với bán trực tiếp lượng nguyên liệu tương ứng.
            var recipe = Array.Find(Definitions.Recipes, x => x.output == id);
            if (recipe != null) { int inputs = 0; foreach (var input in recipe.inputs) inputs += ItemPrice(input.id) * input.count; price = Math.Max(price, Mathf.CeilToInt(inputs * 1.35f / recipe.yield)); }
            return price;
        }
        public int Tier(string family) => Economy.Has(family + "_level3") ? 3 : Economy.Has(family + "_level2") ? 2 : 1;
        public int Contribution(string id) => Purchases.Find(x => x.id == id)?.paid ?? 0;
        public bool CanPurchase(UpgradeDefinition u, out string reason)
        {
            reason = "";
            if (u == null) { reason = "Không có nâng cấp"; return false; }
            if (Economy.Has(u.id)) { reason = "Đã mở"; return false; }
            if (!string.IsNullOrEmpty(u.requirement) && !Economy.Has(u.requirement)) reason = "Cần " + (Definitions.Upgrade(u.requirement)?.label ?? u.requirement);
            int sales = 0;
            switch (u.id)
            {
                case "farm_shop": if (Tier("farm") < 3) reason = "Cây trồng cấp 3"; sales = 50; break;
                case "mill": if (Tier("animal") < 3) reason = "Chăn nuôi cấp 3"; sales = 200; break;
                case "supermarket": foreach (string recipe in new[] { "mill", "cheesemaker", "saucemaker" }) if ((Machines.Find(x => x.Recipe.id == recipe)?.Batches ?? 0) < 50) reason = "50 mẻ bột, phô mai và sốt"; sales = 600; break;
                case "bakery": if (Tier("market") < 3) reason = "Siêu thị cấp 3"; sales = 1000; break;
                case "restaurant": if (BakerySales < 60 || Tier("oven") < 3) reason = "60 đơn bánh và lò cấp 3"; break;
                case "animal_level2": if (!Economy.Has("barn") && !Economy.Has("milk_line") && !Economy.Has("egg_line")) reason = "Mở một tuyến chăn nuôi"; break;
            }
            if (sales > Economy.Transactions) reason = sales + " đơn thành công • " + Economy.Transactions + "/" + sales;
            if (u.kind == "worker")
            {
                var crew = CrewFor(u); int work = WorkFor(crew), tier = Tier(FamilyFor(crew));
                if (tier < 3 || work < 30) reason = "Trạm cấp 3 và 30 lượt việc • " + work + "/30";
            }
            return reason.Length == 0;
        }
        public int Contribute(UpgradeDefinition u, int amount)
        {
            if (!CanPurchase(u, out _) || amount <= 0) return 0;
            var progress = Purchases.Find(x => x.id == u.id);
            if (progress == null) { progress = new PurchaseProgress { id = u.id }; Purchases.Add(progress); }
            int moved = Mathf.Min(amount, Economy.Money, u.cost - progress.paid);
            if (moved <= 0 || !Economy.TrySpend(moved)) return 0;
            progress.paid += moved;
            if (progress.paid == u.cost) { Economy.Unlock(u.id); OnPurchased(u); }
            return moved;
        }
        public static CrewState CrewFor(UpgradeDefinition u)
        {
            string role = u.role switch { "Animal" => "AnimalWorker", "Baker" => "Cook", "RestockerMarket" => "Restocker", "CashierMarket" => "Cashier", _ => u.role };
            string area = u.id.Contains("market") ? "supermarket" : u.id.Contains("bakery") ? "bakery" : u.id is "cook" or "waiter" or "transport_restaurant" ? "restaurant" : u.id is "processor" or "transport_processing" ? "processing" : role is "Farmer" or "AnimalWorker" ? "farm" : "farm_shop";
            return new CrewState { id = u.id, role = role, area = area };
        }
        static string FamilyFor(CrewState c) => c.role == "Farmer" ? "farm" : c.role == "AnimalWorker" ? "animal" : c.area == "supermarket" ? "market" : c.area == "processing" ? "mill" : c.area == "bakery" ? "oven" : c.area == "restaurant" ? "kitchen" : "counter";
        int WorkFor(CrewState crew)
        {
            int n = 0;
            foreach (var s in Stations) if (s.AreaId == crew.area && (crew.role != "Farmer" || s is ProductionStation p && p.ItemId is "carrot" or "tomato" or "wheat") && (crew.role != "AnimalWorker" || s is ProductionStation a && a.ItemId is "milk" or "egg" or "beef")) n += s.WorkCount;
            return n;
        }
        public int CrewUpgradeCost(string id, string kind)
        {
            var c = CrewStates.Find(x => x.id == id); if (c == null) return 0;
            int level = kind == "speed" ? c.speedLevel : kind == "carry" ? c.carryLevel : c.count;
            return 150 * level * level * (c.area is "farm" or "farm_shop" ? 1 : 3);
        }
        public bool UpgradeCrew(string id, string kind)
        {
            var c = CrewStates.Find(x => x.id == id); if (c == null || kind is not ("speed" or "carry" or "count")) return false;
            if (kind == "carry" && c.role == "Cashier") return false;
            int level = kind == "speed" ? c.speedLevel : kind == "carry" ? c.carryLevel : c.count;
            if (level >= 3 || !Economy.TrySpend(CrewUpgradeCost(id, kind))) return false;
            if (kind == "speed") c.speedLevel++; else if (kind == "carry") c.carryLevel++; else c.count++;
            Say("Đã nâng đội " + Definitions.Upgrade(id).label); return true;
        }
        void ApplyProgression()
        {
            Player.Carry.Capacity = Economy.Has("carry24") ? 24 : Economy.Has("carry16") ? 16 : Economy.Has("carry10") ? 10 : 6;
            foreach (var s in Stations)
            {
                string family = s is ProductionStation p ? (p.ItemId is "milk" or "egg" or "beef" ? "animal" : "farm") : s.AreaId == "processing" ? "mill" : s.AreaId == "supermarket" ? "market" : s.AreaId == "bakery" ? "oven" : s.AreaId == "restaurant" ? "kitchen" : "counter";
                s.Level = Tier(family);
            }
        }
        void TickEvents()
        {
            if (CrewStates.Count == 0) return;
            if (Events.rushRemaining > 0) Events.rushRemaining = Mathf.Max(0, Events.rushRemaining - Time.deltaTime);
            else if (Events.warning > 0) { Events.warning -= Time.deltaTime; if (Events.warning <= 0) { Events.rushRemaining = 90; Say("Giờ cao điểm • 90 giây"); } }
            else { Events.untilRush -= Time.deltaTime; if (Events.untilRush <= 0) { Events.warning = 15; Events.untilRush = 480; Say("15 giây nữa bắt đầu giờ cao điểm"); } }
            int batches = 0; foreach (var m in Machines) { if (m.Broken) return; batches += m.Batches; }
            if (batches - Events.lastBreakBatch >= 40)
            {
                var machine = Machines.Find(x => x.IsUnlocked && x.Batches > 0);
                if (machine) { machine.BreakDown(Mathf.Clamp(machine.Level * 20, 10, 100)); Events.lastBreakBatch = batches; Say(machine.Label + " cần sửa chữa"); }
            }
        }
    }
}
