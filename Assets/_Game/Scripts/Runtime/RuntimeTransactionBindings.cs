using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class RuntimeTransactions
    {
        void Bind(Inventory inventory, string owner)
        {
            if (!view.owners.Any(x => x.id == owner)) throw new InvalidDataException("Owner không tồn tại: " + owner);
            inventories[inventory] = owner; inventory.Bind(this, owner); inventory.Project(view);
        }
        public void BindWorker(WorkerAgent worker, WorkerSave saved = null)
        {
            string id = WorkerOwner(worker.WorkerId);
            var crew=view.crews.Find(x=>x.id==worker.UpgradeId);
            int capacity = crew == null ? (view.schemaVersion >= 3 ? 12 : 6) :
                CarryLimits.Worker(crew, view.schemaVersion >= 3);
            EnsureOwner(new OwnerState { id = id, actor = id, location = id, kind = OwnerKind.Worker,
                singleItem = true, capacity = capacity,
                worker = saved ?? new WorkerSave { id = worker.WorkerId, upgrade = worker.UpgradeId, x = worker.transform.position.x, z = worker.transform.position.z } });
            actors[worker.GetEntityId()] = id; Bind(worker.Carry, id);
        }
        void EnsureOwner(OwnerState owner)
        {
            if (view.owners.Any(x => x.id == owner.id)) return;
            var c = Command(TransactionKind.RegisterOwner, "simulation", owner.id, effect: "register:" + owner.id); c.owner = owner;
            Execute(c);
        }
        public void BindCustomer(CustomerAgent customer, CustomerSave saved, bool create)
        {
            string id = CustomerId(saved.receipt);
            var owner=new OwnerState { id = id, actor = id, location = id, kind = OwnerKind.Customer,
                capacity = Math.Max(3, saved.order.Sum(x => x.requested)), customer = saved,
                writers = view.owners.Where(x => x.kind is OwnerKind.Player or OwnerKind.Worker).Select(x => x.actor).ToList() };
            if (create) CreateOrder(saved.receipt, saved.lane, id, saved.order, saved.remaining,owner:owner);
            Bind(customer.Basket, id);
            customer.Order.Bind(this);
        }
        public void BindDiner(DinerAgent diner, DinerSave saved, bool create)
        {
            string id = CustomerId(saved.receipt);
            var owner=new OwnerState { id = id, actor = id, location = id, kind = OwnerKind.Customer, capacity = 1, diner = saved,
                writers = view.owners.Where(x => x.kind is OwnerKind.Player or OwnerKind.Worker).Select(x => x.actor).ToList() };
            if (create)
            {
                var counter = game.Checkouts.Find(x => x.ShopId == "restaurant");
                CreateOrder(saved.receipt, counter.Id, id, new() { new OrderLine(saved.item??"meal", 1, saved.price) }, saved.remaining, saved.table,owner);
            }
            Bind(diner.Basket, id);
        }
        void CreateOrder(long receipt, string counter, string customer, List<OrderLine> lines, float patience, string table = null,OwnerState owner=null)
        {
            var c = Command(TransactionKind.CreateOrder, "simulation", OrderId(receipt), effect: "create:" + OrderId(receipt));
            c.source = counter; c.destination = customer; c.lines = lines; c.expiresAt = Now + patience; c.secondary = table;c.owner=owner; Execute(c);
        }
        public bool Fail(long receipt)
        {
            var o = Order(receipt); if (o == null || o.status != OrderStatus.Open) return false;
            return TryExecute(Command(TransactionKind.FailOrder, "simulation", o.id, effect: "fail:" + o.id), out _);
        }
        public int Deliver(long receipt, Inventory source, string reservation = null, EntityId? actingActor = null)
        {
            var o = Order(receipt); if (o == null || o.status != OrderStatus.Open) return 0;
            if (Now >= o.deadline) { Fail(receipt); return 0; }
            var shipments=new List<OrderLine>();
            var held=reservation==null?null:Reservation(reservation);
            foreach(var line in o.lines)
            {
                if (reservation != null)
                {
                    if(held!=null&&held.status==ReservationStatus.Active&&held.item==line.id&&held.source==OwnerId(source)&&held.quantity<=line.Remaining)
                    {shipments.Add(new OrderLine(line.id,held.quantity,0));break;}
                    continue;
                }
                int quantity=Math.Min(line.Remaining,source.Available(line.id));
                if(quantity>0)shipments.Add(new OrderLine(line.id,quantity,0));
            }
            if(shipments.Count==0)return 0;
            string actorId=actingActor.HasValue?Actor(actingActor.Value):view.owners.Find(x=>x.id==OwnerId(source)).actor;
            var command=Command(TransactionKind.DeliverOrder,actorId,o.id);command.source=OwnerId(source);command.reservation=reservation;
            command.lines=shipments;
            int moved=TryExecute(command,out int delivered)?delivered:0;
            Settle(receipt); return moved;
        }
        public void Settle(long receipt)
        {
            var o = Order(receipt); if (o == null || o.status == OrderStatus.Failed) return;
            if (o.status == OrderStatus.Open && o.lines.All(x => x.Remaining == 0))
                TryExecute(Command(TransactionKind.CompleteOrder, "simulation", o.id, effect: "complete:" + o.id), out _);
            o = Order(receipt);
            if (o.status == OrderStatus.Complete && string.IsNullOrEmpty(o.table) && !view.payments.Any(x => x.order == o.id)&&!view.legacyPaid.Contains(receipt))
                TryExecute(Command(TransactionKind.CreatePayment, "simulation", o.id, effect: "payment:" + o.id), out _);
        }
        public long Collect(string counter)
        {
            long before = game.Economy.CashInHand;
            foreach (var payment in view.payments.Where(x => x.counter == counter && !x.collected).ToArray())
                TryExecute(Command(TransactionKind.CollectPayment, "player", payment.id,
                    effect: "collect:" + payment.id), out _);
            if ((view.legacyCash.Find(x => x.id == counter)?.amount ?? 0) > 0)
                TryExecute(Command(TransactionKind.CollectPayment, "player", "legacy-cash:" + counter,
                    effect: "collect-legacy:" + counter), out _);
            return game.Economy.CashInHand - before;
        }
        public int Contribute(UpgradeDefinition definition, int amount)
        {
            var c = Command(TransactionKind.ContributePurchase, "player", definition.id); c.quantity = amount;
            if (!TryExecute(c, out int moved)) return 0;
            CompletePurchases(); return moved;
        }
        public void CompletePurchases()
        {
            foreach (var p in view.purchases.Where(x => !x.complete && x.contributed == Definitions.Upgrade(x.definitionId).cost).ToArray())
                if (TryExecute(Command(TransactionKind.CompletePurchase, "player", p.id, effect: "grant:" + p.id), out int granted) && granted > 0)
                    game.OnPurchased(Definitions.Upgrade(p.id));
        }
        public void Checkpoint()
        {
            if (store == null || !store.CanCheckpoint) return;
            var saved = core.CaptureCheckpoint();
            saved.simulationTime = Now;
            store.CheckpointOwned(saved);
        }
        public bool CheckpointPending => store != null && !store.CanCheckpoint;
        public void DrainCheckpoint() => store?.DrainCheckpoint();
        void Refresh()
        {
            // Lệnh bị từ chối hoặc chạy lại không đổi state, không cần chiếu lại toàn thế giới.
            if (view.revision == core.Revision) return;
            using var refreshTiming = QaCpuProbe.Measure(QaCpuProbe.Work.Refresh);
            using (QaCpuProbe.Measure(QaCpuProbe.Work.ViewSnapshot))
                view = core.RuntimeSnapshot();
            using (QaCpuProbe.Measure(QaCpuProbe.Work.InventoryProjection))
            {
                foreach (var inventory in inventories.Keys)
                    if (inventory.Authority == this)
                        inventory.Project(view);
            }
        }
        public void Detach()
        { foreach (var inventory in inventories.Keys) inventory.Unbind(); game.Economy.Unbind();store?.Dispose(); }

    }
}
