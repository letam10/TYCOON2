using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    [Serializable] public sealed class PurchaseProgress { public string id; public int paid, total; public bool complete; }
    [Serializable] public sealed class CrewState
    {
        public string id, role, area;
        public int count = 1, speedLevel = 1, carryLevel = 1;
    }
    [Serializable] public sealed class EventState
    {
        public float untilRush = 480, warning, rushRemaining, breakdownWarning;
        public string breakdownMachine;
        public int lastBreakBatch;
    }
    public sealed partial class GameSession
    {
        List<PurchaseProgress> purchases = new();
        List<CrewState> crewStates = new();
        ProgressionTracker progression;
        public ProgressionTracker Progression => progression ??= new ProgressionTracker(this);
        public List<PurchaseProgress> Purchases { get => Transactions == null ? purchases : Transactions.View.purchases.ConvertAll(x => new PurchaseProgress { id=x.id, paid=x.contributed, total=Definitions.Upgrade(x.id).cost, complete=x.complete }); set { if(Transactions!=null)throw new InvalidOperationException("Purchase chỉ được cập nhật qua transaction."); purchases=value; } }
        public List<CrewState> CrewStates { get => Transactions == null ? crewStates : Transactions.View.crews.ConvertAll(TransactionCore.Copy); set { if(Transactions!=null)throw new InvalidOperationException("Crew chỉ được cập nhật qua transaction."); crewStates=value; } }
        public CrewState FindCrew(string id)
        {
            var crew = ReadCrew(id);
            if (crew == null) return null;
            return new CrewState
            {
                id = crew.id,
                role = crew.role,
                area = crew.area,
                count = crew.count,
                speedLevel = crew.speedLevel,
                carryLevel = crew.carryLevel
            };
        }
        internal CrewState ReadCrew(string id)
        {
            // Nội bộ chỉ đọc reference; API công khai FindCrew vẫn trả bản sao độc lập.
            var source = Transactions == null ? crewStates : Transactions.View.crews;
            foreach (var crew in source)
                if (crew.id == id) return crew;
            return null;
        }
        public List<CustomerSave> PendingCustomers = new();
        public List<WorkerSave> PendingWorkers = new();
        public EventState Events = new();
        public bool RushActive => Events.rushRemaining > 0;
        public string ObjectiveText => Progression.NextObjectiveText();
        public StorageStation StorageFor(string area)
        {
            if (area == "market") area = "supermarket";
            foreach (var station in Stations)
                if (station is StorageStation storage && storage.AreaId == area) return storage;
            return null;
        }
        public int ItemPrice(string id)
        {
            var item = Definitions.Item(id); if (item == null) return 0;
            string family=Definitions.IsCrop(id)||id=="animal_feed"?"farm":Definitions.IsAnimal(id)?"animal":id is "flour" or "cheese" or "sauce" or "soy_sauce" or "bottled_milk" or "yarn" or "cloth"?"mill":id is "bread" or "cake" or "bread_dough" or "cake_batter"?"oven":"kitchen";
            int quality = Progression.AxisLevel(family,UpgradeAxis.QualityValue);
            int price = Mathf.CeilToInt(item.price * (1 + .25f * (quality - 1)));
            // Thành phẩm luôn có lãi so với bán trực tiếp lượng nguyên liệu tương ứng.
            var recipe = Array.Find(Definitions.Recipes, x => x.output == id);
            if (recipe != null) { int inputs = 0; foreach (var input in recipe.inputs) inputs += ItemPrice(input.id) * input.count; price = Math.Max(price, Mathf.CeilToInt(inputs * 1.35f / recipe.yield)); }
            return price;
        }
        public int Tier(string family) => Progression.StationLevel(family);
        public int Contribution(string id)
        {
            // Tra cứu một ô không cần tạo bản sao tiến độ của mọi ô trong thành phố.
            if (Transactions != null)
            {
                foreach (var purchase in Transactions.View.purchases)
                    if (purchase.id == id) return purchase.contributed;
                return 0;
            }
            foreach (var purchase in purchases)
                if (purchase.id == id) return purchase.paid;
            return 0;
        }
        public bool CanPurchase(UpgradeDefinition u, out string reason)
        {
            var evaluation = Progression.Evaluate(u);
            reason = evaluation.State == PurchaseState.Purchased ? "Purchase đã hoàn tất." : string.Join(" ", evaluation.Requirements);
            return evaluation.CanContribute;
        }
        public int Contribute(UpgradeDefinition u, int amount)
        {
            if (!CanPurchase(u, out _) || amount <= 0) return 0;
            if (Transactions != null) return Transactions.Contribute(u, amount);
            var progress = Purchases.Find(x => x.id == u.id);
            if (progress == null) { progress = new PurchaseProgress { id = u.id }; Purchases.Add(progress); }
            int moved = (int)System.Math.Min(amount, System.Math.Min(Economy.CashInHand, u.cost - progress.paid));
            if (moved <= 0 || !Economy.TrySpend(moved)) return 0;
            progress.paid += moved;
            if (progress.paid == u.cost) { Economy.Unlock(u.id); OnPurchased(u); }
            return moved;
        }
        public static CrewState CrewFor(UpgradeDefinition u)
        {
            string role = u.role switch { "Animal" => "AnimalWorker", "Baker" => "Cook", "RestockerMarket" => "Restocker", "CashierMarket" => "Cashier", _ => u.role };
            string area = u.id.Contains("market") ? "supermarket" : u.id.Contains("bakery") ? "bakery" : u.id is "cook" or "waiter" or "transport_restaurant" ? "restaurant" : u.id is "processor" or "transport_processing" ? "processing" : role is "Farmer" or "AnimalWorker" ? "farm" : "farm_shop";
            if(role=="Repairer")area=u.id.Substring("repair_".Length);
            if(role=="Loader")area=u.id.Substring("loader_".Length);
            if(role=="Driver")area="processing";
            return new CrewState { id = u.id, role = role, area = area };
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
            if(Transactions!=null)
            {
                var command=Transactions.Command(TransactionKind.UpgradeCrew,"player",id);command.secondary=kind;
                bool upgraded=Transactions.TryExecute(command,out _);
                if(upgraded)UpgradeModelView.RefreshAll(this,true);
                return upgraded;
            }
            int level = kind == "speed" ? c.speedLevel : kind == "carry" ? c.carryLevel : c.count;
            int maximum = kind == "count" ? WorkforceRules.Limit(c.id) : 3;
            if (level >= maximum || !Economy.TrySpend(CrewUpgradeCost(id, kind))) return false;
            if (kind == "speed") c.speedLevel++; else if (kind == "carry") c.carryLevel++; else c.count++;
            Say("Đã nâng đội " + Definitions.Upgrade(id).label); return true;
        }
        void ApplyProgression()
        {
            if(Transactions!=null)return;
            Player.Carry.Capacity = Progression.AxisCapacity("player", 12);
            foreach (var s in Stations)
            {
                string family = s is ProductionStation p ? (p.Animal ? "animal" : "farm") : s.AreaId switch
                { "processing" => "mill", "supermarket" => "market", "bakery" => "oven", "restaurant" => "kitchen", "farm_shop" => "counter", _ => s.AreaId };
                s.Level = Tier(family);
                if(s is ProductionStation crop&&!crop.Animal&&crop.Inventory!=null)crop.Inventory.Capacity=Progression.AxisCapacity("farm",24);
            }
        }
        public void TickEvents(float delta)
        {
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return;
            if ((Transactions == null ? crewStates.Count : Transactions.View.crews.Count) > 0)
            {
                if (Events.rushRemaining > 0) Events.rushRemaining = Mathf.Max(0, Events.rushRemaining - delta);
                else if (Events.warning > 0) { Events.warning = Mathf.Max(0,Events.warning-delta); if (Events.warning == 0) { Events.rushRemaining = 90; Say("Giờ cao điểm • 90 giây"); } }
                else { Events.untilRush -= delta; if (Events.untilRush <= 0) { Events.warning = 15; Events.untilRush = 480; Say("15 giây nữa bắt đầu giờ cao điểm"); } }
            }
            TickBreakdown(delta);
        }
        void TickBreakdown(float delta)
        {
            int batches=Machines.Sum(x=>x.Batches);
            if(!string.IsNullOrEmpty(Events.breakdownMachine))
            {
                var target=Machines.Find(x=>x.Id==Events.breakdownMachine);
                if(!target||target.Broken||Machines.Exists(x=>x!=target&&x.Broken))
                {Events.breakdownMachine=null;Events.breakdownWarning=0;return;}
                Events.breakdownWarning=Mathf.Max(0,Events.breakdownWarning-delta);
                if(Events.breakdownWarning==0)
                {
                    Events.breakdownMachine=null;Events.lastBreakBatch=batches;
                    target.BreakDown(Mathf.Clamp(target.Level*20,10,100));
                    Say(target.Label+" bị hỏng • dùng vùng Sửa máy");
                }
                return;
            }
            if(Machines.Exists(x=>x.Broken)||batches-Events.lastBreakBatch<40)return;
            var machine=Machines.Find(x=>x.IsUnlocked&&x.Batches>0&&!x.Broken);
            if(!machine)return;
            Events.breakdownMachine=machine.Id;Events.breakdownWarning=15;
            Say(machine.Label+" có dấu hiệu quá tải • hỏng sau 15 giây");
        }
    }
}
