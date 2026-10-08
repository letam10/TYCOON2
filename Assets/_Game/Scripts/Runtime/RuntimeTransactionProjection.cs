using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class RuntimeTransactions
    {
        internal static SaveData Project(TransactionState state, SaveData data) =>
            ProjectSnapshot(state, data, false);

        internal static SaveData ProjectOwned(TransactionState state, SaveData data) =>
            ProjectSnapshot(state, data, true);

        static SaveData ProjectSnapshot(TransactionState state, SaveData data, bool ownsState)
        {
            data.transactionState = ownsState ? state : CheckpointSnapshot.Copy(state);
            data.transactionVersion = state.schemaVersion;
            data.version = state.schemaVersion;
            data.cashInHand = state.cashInHand;
            data.cashInSafe = state.cashInSafe;
            data.money = state.money;
            data.revenue = state.revenue;
            data.cashCollected = state.cashCollected;
            data.transactions = state.legacyTransactions + state.payments.Count;
            data.unlocked = new(state.unlocked); data.crews = state.crews.Select(CheckpointSnapshot.Copy).ToList();
            data.purchases = state.purchases.Select(x => new PurchaseProgress { id = x.id, paid = x.contributed, total = Definitions.Upgrade(x.id).cost, complete = x.complete }).ToList();
            data.cash = state.owners.Where(x => x.kind == OwnerKind.Counter).Select(x => new CashSave { id = x.id,
                amount = state.payments.Where(p => p.counter == x.id && !p.collected).Sum(p => (long)p.amount) + (state.legacyCash.Find(p => p.id == x.id)?.amount ?? 0) }).ToList();
            data.pendingCash = data.cash.Sum(x => x.amount);
            data.receipts = state.legacyPaid.Concat(state.payments.Select(x => long.Parse(x.order.Substring(6)))).Distinct().ToList();
            data.losses = state.legacyLosses.Select(CheckpointSnapshot.Copy).ToList();
            foreach (var order in state.orders.Where(x => x.status == OrderStatus.Failed))
            {
                var goods=Items(state,order.customer);
                if(goods.Sum(x=>x.count)>0&&!data.losses.Any(x => x.receipt == long.Parse(order.id.Substring(6)))) data.losses.Add(new LossRecord {
                    receipt = long.Parse(order.id.Substring(6)), goods = goods });
            }
            data.inventories = state.owners.Where(x => x.kind is not (OwnerKind.Worker or OwnerKind.Customer) && (x.id == "player" || data.inventories.Any(i => i.id == x.id)))
                .Select(x => new InventorySave(x.id, new Inventory(0)) { items = Items(state, x.id) }).ToList();
            data.stationStates = state.stations.Select(x => { var p = CheckpointSnapshot.Copy(x.progress); p.id = x.id; p.level = x.level; p.workCount = x.workCount;
                if (x.kind == "machine") { p.remaining = (float)x.remaining; p.running = x.running; p.batches = x.batches;p.playerBatches=x.playerBatches; } return p; }).ToList();
            foreach (var owner in state.owners.Where(x => x.kind == OwnerKind.Worker && !string.IsNullOrEmpty(x.worker?.id)))
            {
                var worker = data.workers.Find(x => x.id == owner.worker.id) ?? CheckpointSnapshot.Copy(owner.worker);
                if (!data.workers.Contains(worker)) data.workers.Add(worker);
                worker.carry = Items(state, owner.id);
                var reservation = state.reservations.Find(x => x.holder == owner.actor && x.status == ReservationStatus.Active && !string.IsNullOrEmpty(x.source));
                worker.reservation = reservation?.id;
                if (reservation != null)
                {
                    worker.phase = reservation.source == owner.id ? 2 : 1; worker.count = reservation.quantity; worker.item = reservation.item;
                    if(worker.phase==1)worker.source=reservation.source;
                    var order=state.orders.Find(x=>x.customer==reservation.destination);
                    worker.destination=order==null?reservation.destination:string.IsNullOrEmpty(order.table)?order.counter:order.table;
                    if(order!=null)worker.tableReceipt=long.Parse(order.id.Substring(6));
                }
                else if (worker.phase is 1 or 2) { worker.phase = 0; worker.count = 0; }
            }
            foreach (var owner in state.owners.Where(x => x.kind == OwnerKind.Customer))
            {
                var order = state.orders.Find(x => x.customer == owner.id); if (order == null) continue;
                long receipt = long.Parse(order.id.Substring(6)); data.nextReceipt = Math.Max(data.nextReceipt, receipt + 1);
                if (owner.customer?.receipt>0)
                {
                    var customer = data.customers.Find(x => x.receipt == receipt);
                    if (customer == null && order.status == OrderStatus.Open) { customer = CheckpointSnapshot.Copy(owner.customer); data.customers.Add(customer); }
                    if (customer == null) continue;
                    customer.basket = Items(state, owner.id); customer.order = order.lines.Select(CheckpointSnapshot.Copy).ToList();
                    customer.remaining = (float)Math.Max(0, order.deadline - state.simulationTime);
                    customer.paid = order.status == OrderStatus.Complete; customer.timedOut = order.status == OrderStatus.Failed;
                    if (customer.paid || customer.timedOut) customer.phase = (int)CustomerAgent.State.Leaving;
                }
                if (owner.diner?.receipt>0)
                {
                    var diner = data.diners.Find(x => x.receipt == receipt);
                    if (diner == null && order.dinerPhase < 3) { diner = CheckpointSnapshot.Copy(owner.diner); data.diners.Add(diner); }
                    if (diner == null) continue;
                    diner.item=order.lines[0].id;diner.phase = order.dinerPhase; diner.remaining = (float)(diner.phase == 2 ? order.eatingRemaining : Math.Max(0, order.deadline - state.simulationTime)); diner.basket = Items(state, owner.id);
                }
            }
            return data;
        }
        internal static List<ItemAmount> Items(TransactionState state, string owner) => state.stacks.Where(x => x.owner == owner).GroupBy(x => x.item).Select(x => new ItemAmount(x.Key, x.Sum(a => a.quantity))).ToList();
    }
}
