using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class TransactionCore
    {
        int OperateProducer(TransactionState s, TransactionCommand c)
        {
            var m = Producer(s, c); var p = m.progress;
            Require(double.IsFinite(c.duration) && c.duration > 0, "duration", "Delta không hợp lệ.");
            if (Definitions.IsAnimal(m.item))
            {
                Require(p.feed > 0, "feed", "Hãy đặt "+Definitions.Item(FeedItem(m.item)).label+" vào vùng Cho ăn.");
                return 1;
            }
            Require(p.phase < 2, "phase", p.phase == 2 ? "Cây đang lớn." : "Cây đã chín; hãy sang vùng Lấy hàng.");
            p.action += (float)c.duration;
            if (p.phase == 0 && p.cycleYield == 0) p.cycleYield = m.batchYield + ProgressionTracker.StationLevel(s, "farm") - 1;
            if (p.action >= 1) { p.action = 0; if (p.phase == 0) p.phase = 1; else { p.phase = 2; p.remaining = ProgressionTracker.FarmGrowDuration(s); } }
            return 1;
        }
        int RestockProducer(TransactionState s, TransactionCommand c)
        {
            var m=Producer(s,c);var p=m.progress;
            Require(Definitions.IsAnimal(m.item)&&p.herd<ProductionStation.MaximumHerd&&p.breeding==0,"restock","Đàn đã đủ hoặc đang tái đàn.");
            Require(p.feed>0,"feed","Cần thức ăn để bắt đầu tái đàn.");
            p.feed--;p.breeding=60;m.workCount++;if(c.actor=="player")m.playerWorkCount++;return 1;
        }
        int TickProducer(TransactionState s, TransactionCommand c)
        {
            Require(c.actor == "simulation" && double.IsFinite(c.duration) && c.duration > 0, "authority", "Tick không hợp lệ.");
            var m = Producer(s, c, false); var p = m.progress;
            if (!Definitions.IsAnimal(m.item))
            { if (p.phase == 2) { p.remaining = Math.Max(0, p.remaining - (float)c.duration); if (p.remaining == 0) p.phase = 3; } return 1; }
            if (p.breeding > 0) { p.breeding = Math.Max(0, p.breeding - (float)c.duration); if (p.breeding == 0) p.herd++; }
            if (p.herd > 0 && p.feed > 0) { p.cycle = Math.Max(0, p.cycle - (float)c.duration * (clock() < m.operatorUntil ? 2 : 1)*(1+.15f*(ProgressionTracker.AxisLevel(s,"animal",UpgradeAxis.Speed)-1))); p.remaining = p.cycle; }
            return 1;
        }
        int HarvestProducer(TransactionState s, TransactionCommand c)
        {
            var m = Producer(s, c); var p = m.progress; var carrier = Owner(s, c.destination); WriteAccess(carrier, c.actor);
            Require(carrier.actor == c.actor && carrier.kind is OwnerKind.Player or OwnerKind.Worker, "authority", "Chỉ lấy vào giỏ người thao tác.");
            bool animal = Definitions.IsAnimal(m.item);
            int count=animal?1:Math.Max(1,p.cycleYield > 0 ? p.cycleYield : m.batchYield);
            Require(double.IsFinite(c.duration) && c.duration > 0 && Free(s, carrier, m.item) >= count, "capacity", "Giỏ cần đủ chỗ cho cả lượt thu "+count+" "+Definitions.Item(m.item).label+".");
            Require(animal ? p.herd > 0 && p.feed > 0 && p.cycle == 0 : p.phase == 3, "phase", "Chưa đến lúc thu hoạch.");
            p.action += (float)c.duration;
            if (p.action < 1) return 0;
            // Một lần lấy thịt luôn tiêu thụ đúng một con và tạo một đơn vị thịt.
            AddStack(s, "harvest:" + c.effectId, carrier.id, m.item, count);
            if(carrier.id=="player"&&s.stacks.Where(x=>x.owner=="player").Sum(x=>x.quantity)==count){s.carryOrigin=m.output;s.carryBatch=c.effectId;}
            p.action = 0; p.produced += count; m.workCount++;if(c.actor=="player")m.playerWorkCount++;
            if (animal) { if (m.item == "beef") p.herd--; p.feed--; p.cycle = m.cycleSeconds>0?m.cycleSeconds:m.item == "beef" ? 45 : 30; } else { p.phase = 0; p.cycleYield = 0; }
            return count;
        }
        int FeedProducer(TransactionState s, TransactionCommand c)
        {
            var m = Producer(s, c); var p = m.progress;
            string feed = FeedItem(m.item);
            Require(feed != null && p.feed < 3, "feed", "Máng ăn đã đầy hoặc trạm không nhận thức ăn.");
            var source = Owner(s, c.source); WriteAccess(source, c.actor);
            Require(source.kind != OwnerKind.Customer, "escrow", "Không lấy thức ăn từ khách.");
            Require(c.item == feed, "feed-item", "Thức ăn không đúng loại cho vật nuôi.");
            Consume(s, Allocate(s, source.id, feed, 1)); p.feed++; m.workCount++;if(c.actor=="player")m.playerWorkCount++; return 1;
        }
        int RepairMachine(TransactionState s, TransactionCommand c)
        {
            return AdvanceRepair(s,c);
        }
        int UpgradeCrew(TransactionState s, TransactionCommand c)
        {
            Player(s, c.actor); var crew = s.crews.Find(x => x.id == c.target);
            Require(crew != null && c.secondary is "speed" or "carry" or "count", "crew", "Không có nâng cấp đội.");
            Require(c.secondary != "carry" || crew.role is not ("Cashier" or "Repairer" or "Driver"), "crew", "Nghề này không dùng nâng sức mang.");
            Require(crew.role!="Driver"||c.secondary=="speed","crew","Tài xế kèm xe nâng tốc độ; mỗi xe có một tài xế.");
            int level = c.secondary == "speed" ? crew.speedLevel : c.secondary == "carry" ? crew.carryLevel : crew.count;
            int cost = checked(150 * level * level * (crew.area is "farm" or "farm_shop" ? 1 : 3));
            int maximum = c.secondary == "count" && !legacyWorkforceReplay ? WorkforceRules.Limit(crew.id) : 3;
            Require(level < maximum && PhysicalCashRules.Hand(s) >= cost,
                "funds", "Không đủ tiền hoặc đội đã đạt cấp tối đa.");
            PhysicalCashRules.Spend(s, cost); if (c.secondary == "speed") crew.speedLevel++; else if (c.secondary == "carry") crew.carryLevel++; else crew.count++;
            RefreshProgression(s); return cost;
        }
        void RefreshProgression(TransactionState s)
        {
            UnlockCrops(s);
            if (!legacyWorkforceReplay) WorkforceMigration.Apply(s);
            foreach (var owner in s.owners)
            {
                if (owner.kind == OwnerKind.Player) owner.capacity = CarryLimits.Player(s);
                if (owner.kind == OwnerKind.Worker && !string.IsNullOrEmpty(owner.worker?.upgrade))
                {
                    var crew = s.crews.Find(x => x.id == owner.worker.upgrade);
                    if (crew != null)
                    {
                        // Journal cũ dùng sức mang cũ trước khi chuyển đổi toàn bộ checkpoint.
                        owner.capacity = legacyWorkforceReplay && s.schemaVersion >= 3 ?
                            CarryLimits.Worker(crew, false) * 2 : CarryLimits.Worker(crew, s.schemaVersion >= 3);
                    }
                }
                var station = s.stations.Find(x => x.id == owner.id);
                if (station?.kind == "producer" && station.area == "farm" && Definitions.IsCrop(station.item))
                    owner.capacity = ProgressionTracker.AxisCapacity(s, "farm", 24);
                var machine=s.stations.Find(x=>x.kind=="machine"&&(x.input==owner.id||x.output==owner.id));
                string family=owner.kind==OwnerKind.Storage&&station!=null?"storage_"+station.area:machine!=null?ProgressionTracker.Family(machine):station?.kind is "shelf" or "counter"?ProgressionTracker.Family(station):null;
                if(family!=null&&owner.capacity>0)
                {
                    int capacity=ProgressionTracker.AxisCapacity(s,family,owner.capacity);
                    if(capacity>owner.capacity){owner.limits=owner.limits.Select(x=>new ItemAmount(x.id,Math.Max(x.count,(int)Math.Ceiling(x.count*(double)capacity/owner.capacity)))).ToList();owner.capacity=capacity;}
                }
            }
            foreach (var m in s.stations)
            {
                m.level = ProgressionTracker.StationLevel(s, ProgressionTracker.Family(m));
            }
        }
        static void ValidatePurchaseRequirements(TransactionState s,UpgradeDefinition d)
        {
            Require(ProgressionTracker.MissingRequirements(s, d).Length == 0, "requirement", "Chưa đạt điều kiện mở khóa.");
        }
        int CleanTable(TransactionState s, TransactionCommand c)
        {
            var m = Station(s, c.target); WriteAccess(Owner(s, m.output), c.actor);
            Require(m.kind == "table" && m.progress.remaining > 0 && double.IsFinite(c.duration) && c.duration > 0, "table", "Bàn chưa cần dọn.");
            Lease(m, c.actor, clock()); m.progress.remaining = Math.Max(0, m.progress.remaining - (float)c.duration);
            if (m.progress.remaining == 0) {m.workCount++;if(c.actor=="player"){m.playerWorkCount++;m.progress.playerCleanCount++;}} return 1;
        }
        int AdvanceDiner(TransactionState s, TransactionCommand c)
        {
            Require(c.actor == "simulation", "authority", "Chỉ mô phỏng tiến triển khách bàn.");
            var o = Order(s, c, false); Require(!string.IsNullOrEmpty(o.table), "table", "Đơn không thuộc bàn.");
            if (c.secondary == "seat" && o.dinerPhase == 0) { o.dinerPhase = 1; return 1; }
            if (o.status == OrderStatus.Open && clock() >= o.deadline) return FailOrder(s, c);
            if (o.dinerPhase != 2) return 0;
            Require(double.IsFinite(c.duration) && c.duration > 0, "duration", "Delta không hợp lệ.");
            o.eatingRemaining = Math.Max(0, o.eatingRemaining - c.duration);
            if (o.eatingRemaining > 0) return 1;
            CreatePayment(s, c); o.dinerPhase = 3;
            // Ăn xong là tiêu thụ có chủ đích; không trả món về kho.
            s.stacks.RemoveAll(x => x.owner == o.customer);
            var table = Station(s, o.table); table.progress.remaining = 3; table.workCount++; return 1;
        }
    }
}
