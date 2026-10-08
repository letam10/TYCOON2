using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class TransactionCore
    {
        internal static TransactionState NormalizeState(TransactionState s)
            => NormalizeRecoveryState(s, true);

        static TransactionState NormalizeRecoveryState(TransactionState s, bool applyWorkforce)
        {
            Require(s.schemaVersion is 1 or 2 or 3, "schema", "Version transaction chưa hỗ trợ.");
            // Save cũ có receipt Collect nhưng chưa có số liệu tiền thực thu.
            var collected=s.outbox.Where(x=>x.kind==nameof(TransactionKind.CollectPayment)).Select(x=>x.receiptId).ToHashSet();
            s.cashCollected=Math.Max(s.cashCollected,s.receipts.Where(x=>collected.Contains(x.id)).Sum(x=>(long)x.amount));
            foreach(var owner in s.owners.Where(x=>x.kind==OwnerKind.Storage))
            {
                var station=s.stations.Find(x=>x.kind=="storage"&&(x.output==owner.id||x.id==owner.id));
                if(station!=null&&!string.IsNullOrEmpty(station.area))owner.location=station.area;
                foreach(var stack in s.stacks.Where(x=>x.owner==owner.id))stack.location=StackLocation(owner,stack.item);
            }
            s.schemaVersion=Math.Max(2,s.schemaVersion);
            s.crates??=new();s.transportJobs??=new();
            if(s.unlocked.Contains("truck_bundle"))EnsureTruck(s);
            UnlockCrops(s,true);
            foreach(var machine in s.stations.Where(x=>x.kind=="machine"))
            {
                if(machine.running && (machine.batch==null||string.IsNullOrEmpty(machine.batch.output)||machine.batch.yield<=0))machine.batch=RecipeBatchSnapshot.From(s.contentVersion<2?Definitions.LegacyRecipe(machine.definitionId):Definitions.Recipe(machine.definitionId),machine.operatorId??"simulation");
                machine.definitionVersion=Definitions.Recipe(machine.definitionId).version;
                if(machine.recipeOptions.Count==0)machine.recipeOptions.Add(machine.definitionId);
                if(s.contentVersion<2 && machine.definitionId is "oven" or "cakeoven")machine.legacyInput=s.stacks.Any(x=>x.owner==machine.input && x.item is "flour" or "milk" or "egg");
            }
            foreach(var machine in s.stations.Where(x=>x.kind=="machine"))
                if(machine.progress.repairVersion==0)
                {var p=machine.progress;p.repairProgress=p.broken?Math.Clamp(1-p.repairRemaining/8f,0,1):0;p.repairRemaining=(1-p.repairProgress)*5;p.repairVersion=1;p.playerOnlyRepair=true;}
            s.contentVersion=Math.Max(3,s.contentVersion);
            foreach(var station in s.stations)if(station.batchYield==0)station.batchYield=1;
            foreach(var station in s.stations.Where(x=>x.kind=="producer" && x.progress.phase>0 && x.progress.cycleYield==0))
                station.progress.cycleYield=station.batchYield+ProgressionTracker.AxisLevel(s,"farm",UpgradeAxis.Capacity)-1;
            RefreshMachinePhases(s);
            if (applyWorkforce) WorkforceMigration.Apply(s);
            return s;
        }
        static void RefreshMachinePhases(TransactionState s)
        {
            foreach(var machine in s.stations.Where(x=>x.kind=="machine"))
            {
                if(machine.running){machine.machinePhase=MachinePhase.Operating;continue;}
                var recipe=MachineRecipe(s,machine);
                if(recipe==null){machine.machinePhase=MachinePhase.WaitingInput;continue;}
                if(s.stacks.Any(x=>x.owner==machine.output&&x.item==recipe.output&&x.quantity>0))
                {machine.machinePhase=MachinePhase.CompletedWaitingPickup;continue;}
                bool ready=recipe.inputs.All(input=>s.stacks.Where(x=>x.owner==machine.input&&x.item==input.id).Sum(x=>x.quantity)>=input.count)&&
                    Free(s,Owner(s,machine.output),recipe.output)>=recipe.yield&&!machine.progress.broken;
                machine.machinePhase=ready?MachinePhase.Ready:MachinePhase.WaitingInput;
            }
        }
        public static void Validate(TransactionState s)
        {
            if (s == null) throw new InvalidDataException("Thiếu transaction state.");
            PhysicalCashRules.Validate(s);
            Require(s.schemaVersion is 2 or 3 && s.catalogVersion == Definitions.Version && s.revision >= 0 && s.money >= 0 && s.revenue >= 0 && s.cashCollected>=0 && double.IsFinite(s.simulationTime), "schema", "Version hoặc tiền không hợp lệ.");
            Unique(s.owners.Select(x => x.id)); Unique(s.stacks.Select(x => x.id)); Unique(s.reservations.Select(x => x.id));
            Unique(s.orders.Select(x => x.id)); Unique(s.payments.Select(x => x.id)); Unique(s.purchases.Select(x => x.id)); Unique(s.crews.Select(x => x.id)); Unique(s.stations.Select(x => x.id));
            Unique(s.receipts.Select(x => x.id)); Unique(s.receipts.Select(x => x.key)); Unique(s.receipts.Select(x => x.effectId)); Unique(s.outbox.Select(x => x.id)); Unique(s.unlocked);
            Unique(s.jobIds); Unique(s.consumers.Select(x => x.id)); Unique(s.receipts.Select(x => x.revision.ToString()));
            ValidateCargo(s);
            foreach (var consumer in s.consumers) Require(consumer.appliedEvents == s.outbox.Count(x => x.consumers.Contains(consumer.id)), "consumer", "Consumer projection lệch dedup.");
            foreach (var owner in s.owners)
            {
                Require(!string.IsNullOrWhiteSpace(owner.actor) && !string.IsNullOrWhiteSpace(owner.location) && owner.capacity >= 0 && Enum.IsDefined(typeof(OwnerKind), owner.kind), "owner", "Owner không hợp lệ.");
                Unique(owner.limits.Select(x => x.id)); Require(owner.limits.All(x => Definitions.Item(x.id) != null && x.count >= 0), "limits", "Giới hạn sai.");
                Unique(owner.accepts);Require(owner.accepts.All(x=>Definitions.Item(x)!=null),"limits","Loại nhận hàng không hợp lệ.");
                var items = s.stacks.Where(x => x.owner == owner.id).ToArray();
                var reserved = s.reservations.Where(x => x.status == ReservationStatus.Active && (x.destination == owner.id || x.relay == owner.id)).ToArray();
                Require((long)items.Sum(x => x.quantity) + reserved.Sum(x => x.quantity) <= owner.capacity, "capacity", "Owner vượt capacity.");
                var types = items.Select(x => x.item).Concat(reserved.Select(x => x.item)).Distinct().ToArray();
                Require(!owner.singleItem || types.Length <= 1, "single-item", "Người mang có nhiều loại hàng.");
                foreach (var limit in owner.limits) Require((long)items.Where(x => x.item == limit.id).Sum(x => x.quantity) + reserved.Where(x => x.item == limit.id).Sum(x => x.quantity) <= limit.count, "limits", "Vượt item limit.");
            }
            foreach (var stack in s.stacks)
            {
                var owner = Owner(s, stack.owner);
                Require(stack.quantity > 0 && Definitions.Item(stack.item) != null && stack.definitionVersion == 1 && stack.location == StackLocation(owner,stack.item) && Reserved(s, stack.id) <= stack.quantity, "stack", "Stack không hợp lệ.");
                Require(owner.accepts.Count==0||owner.accepts.Contains(stack.item),"item","Owner chứa sai loại hàng.");
            }
            foreach (var r in s.reservations)
            {
                Require(!string.IsNullOrWhiteSpace(r.holder) && r.quantity > 0 && Definitions.Item(r.item) != null && double.IsFinite(r.expiresAt) && Enum.IsDefined(typeof(ReservationStatus), r.status), "reservation", "Reservation không hợp lệ.");
                Owner(s, r.destination);
                if (!string.IsNullOrEmpty(r.relay))
                {
                    var relay=Owner(s,r.relay);
                    Require((relay.kind==OwnerKind.Worker&&relay.actor==r.holder||relay.kind==OwnerKind.Conveyor&&relay.actor=="simulation"&&r.holder=="simulation")&&
                        r.relay!=r.destination&&r.relay!=r.source,"reservation","Relay không hợp lệ.");
                }
                if (r.status != ReservationStatus.Active) continue;
                if (string.IsNullOrEmpty(r.source)) Require(s.stations.Any(x => x.running && x.reservationId == r.id && x.output == r.destination), "reservation", "Reservation capacity không thuộc job.");
                else
                {
                    Owner(s, r.source); Unique(r.allocations.Select(x => x.stack));
                    Require(r.allocations.Sum(x => x.quantity) == r.quantity && r.allocations.All(x => x.quantity > 0 && Stack(s, x.stack).owner == r.source && Stack(s, x.stack).item == r.item), "reservation", "Reservation không khớp stock.");
                }
            }
            foreach (var order in s.orders)
            {
                Require(Owner(s, order.customer).kind == OwnerKind.Customer && Owner(s, order.counter).kind == OwnerKind.Counter && double.IsFinite(order.deadline), "order", "Order owner sai.");
                Unique(order.lines.Select(x => x.id));
                Require(order.lines.Count > 0 && Enum.IsDefined(typeof(OrderStatus), order.status) && order.lines.All(x => Definitions.Item(x.id) != null && x.requested > 0 && x.delivered >= 0 && x.delivered <= x.requested && x.unitPrice >= 0), "order", "Order line sai.");
                Require(order.status != OrderStatus.Complete || order.lines.All(x => x.Remaining == 0), "order", "Đơn complete chưa đủ hàng.");
            }
            Unique(s.payments.Select(x => x.order));
            foreach (var p in s.payments)
            { var order = s.orders.Find(x => x.id == p.order); Require(order != null && order.status == OrderStatus.Complete && p.counter == order.counter && p.amount == order.lines.Sum(x => checked(x.requested * x.unitPrice)), "payment", "Payment không khớp đơn."); }
            foreach (var p in s.purchases)
            { var d = Definitions.Upgrade(p.definitionId); Require(d != null && p.id == d.id && p.definitionVersion == d.version && p.contributed >= 0 && p.contributed <= d.cost && (!p.complete || p.contributed == d.cost && s.unlocked.Contains(d.id)), "purchase", "Purchase sai."); }
            foreach (var crew in s.crews)
            {
                var definition=Definitions.Upgrade(crew.id);var expected=definition==null?null:GameSession.CrewFor(definition);
                Require(definition?.kind=="worker"&&s.unlocked.Contains(crew.id)&&crew.role==expected.role&&crew.area==expected.area&&
                    crew.count is >=1 and <=3&&crew.speedLevel is >=1 and <=3&&crew.carryLevel is >=1 and <=3,"crew","Crew sai.");
            }
            foreach (var m in s.stations)
            {
                Owner(s, m.input); Owner(s, m.output); var recipe = Array.Find(Definitions.Recipes, x => x.id == m.definitionId);
                Require(m.level >= 1 && m.batches >= 0 && m.playerBatches>=0&&m.playerBatches<=m.batches&&m.workCount >= 0 && m.playerWorkCount>=0 && double.IsFinite(m.remaining) && m.remaining >= 0 && m.progress != null, "station", "Station sai: " + m.id);
                if(m.kind=="table")Require(m.progress.playerServeCount>=0&&m.progress.playerCleanCount>=0,"table","Số lượt phục vụ/dọn bàn không hợp lệ.");
                if (m.kind != "machine")
                {
                    Require(m.kind is "producer" or "storage" or "shelf" or "counter" or "table" or "purchase" or "conveyor" or "dock", "station", "Station kind sai: " + m.id);
                    if (m.kind == "producer") Require(Definitions.Item(m.item) != null && m.batchYield>=1&&float.IsFinite(m.cycleSeconds)&&m.cycleSeconds>=0&&m.progress.phase is >= 0 and <= 3 && m.progress.herd is >= 0 and <= 3 && m.progress.feed is >= 0 and <= 3 && float.IsFinite(m.progress.remaining) && m.progress.remaining >= 0 && float.IsFinite(m.progress.action) && m.progress.action >= 0 && float.IsFinite(m.progress.cycle) && m.progress.cycle >= 0 && float.IsFinite(m.progress.breeding) && m.progress.breeding >= 0, "station", "Producer state sai: " + m.id);
                    continue;
                }
                Require(recipe != null && m.definitionVersion == recipe.version, "station", "Recipe version sai: " + m.id);
                Require(Enum.IsDefined(typeof(MachinePhase),m.machinePhase)&&
                    (m.running?m.machinePhase==MachinePhase.Operating:m.machinePhase!=MachinePhase.Operating),"machine-phase","Machine phase không khớp job.");
                var batch=m.batch??RecipeBatchSnapshot.From(recipe,"simulation");
                if (m.running) Require(s.reservations.Any(x => x.id == m.reservationId && x.status == ReservationStatus.Active && x.destination == m.output && x.item == batch.output && x.quantity == batch.yield), "station", "Job mất reservation.");
                if(m.running&&!string.IsNullOrEmpty(m.escrow))Require(Owner(s,m.escrow).kind==OwnerKind.Escrow&&batch.inputs.All(i=>s.stacks.Where(x=>x.owner==m.escrow&&x.item==i.id).Sum(x=>x.quantity)==i.count),"escrow","Job thiếu nguyên liệu đang xử lý.");
            }
            Require(s.stations.Count(x=>x.kind=="machine"&&x.progress.broken)<=1,"breakdown","Chỉ một máy được phép hỏng cùng lúc.");
            Require(s.receipts.Count == s.outbox.Count && s.receipts.Count == s.revision, "journal", "Mutation thiếu receipt hoặc outbox.");
            var events=s.outbox.ToDictionary(x=>x.id);
            foreach (var r in s.receipts) Require(r.revision > 0 && r.revision <= s.revision && !string.IsNullOrEmpty(r.fingerprint) && !string.IsNullOrEmpty(r.effectFingerprint) && events.TryGetValue(r.eventId,out var e) && e.receiptId == r.id && e.effectId == r.effectId && e.revision == r.revision, "journal", "Receipt không khớp event.");
        }
        static void Unique(IEnumerable<string> ids)
        { var set = new HashSet<string>(); foreach (var id in ids) Require(!string.IsNullOrWhiteSpace(id) && set.Add(id), "duplicate", "ID thiếu hoặc lặp: " + id); }
    }
}
