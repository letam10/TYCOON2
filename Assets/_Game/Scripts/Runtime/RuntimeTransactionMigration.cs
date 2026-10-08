using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class RuntimeTransactions
    {
        public static TransactionState Migrate(GameSession game, SaveData data)
        {
            var s = new TransactionState { contentVersion=data.contentVersion,money = data.money, revenue = data.revenue, cashCollected=data.cashCollected, legacyRevenue = data.revenue,
                legacyTransactions = data.transactions, legacyPaid = data.receipts, legacyLosses = data.losses, legacyCash = data.cash,
                simulationTime = Time.timeAsDouble, unlocked = data.unlocked, crews = data.crews };
            void Owner(string id, Inventory inventory, OwnerKind kind, string actor = "simulation", List<ItemAmount> items = null, string location = null)
            {
                var owner=new OwnerState { id = id, actor = actor, kind = kind, location = location??id,
                    capacity = inventory.Capacity, singleItem = inventory.SingleItem, limits = inventory.Limits,
                    writers = kind == OwnerKind.Player ? new() : new() { "player" } };
                s.owners.Add(owner);
                foreach (var item in items ?? inventory.Snapshot()) s.stacks.Add(new ItemStackState {
                    id = "migrated:" + id + ":" + item.id, item = item.id, quantity = item.count, owner = id,
                    location = kind==OwnerKind.Storage?owner.location+"/"+item.id:owner.location });
            }
            Owner("player", game.Player.Carry, OwnerKind.Player, "player");
            foreach (var station in game.Stations.Where(x => x is not StationZone))
            {
                OwnerKind kind = station is StorageStation ? OwnerKind.Storage : station is MachineStation ? OwnerKind.Machine : station is CheckoutStation ? OwnerKind.Counter : station is ConveyorStation ? OwnerKind.Conveyor : OwnerKind.Station;
                Owner(station.Id, station.Inventory ?? new Inventory(0), kind, location:station is StorageStation?station.AreaId:null);
                if(station is ShelfStation shelf)s.owners[^1].accepts=new(shelf.AllowedItems);
                if(station is CheckoutStation checkout)s.owners[^1].accepts=new(checkout.AcceptedItems);
                if(station is MachineStation machine)
                {
                    s.owners[^1].accepts=machine.Options.Select(id=>Definitions.Recipe(id).output).Distinct().ToList();Owner(station.Id + "_input", machine.Input, OwnerKind.Machine);
                    s.owners[^1].accepts=machine.Options.SelectMany(id=>Definitions.Recipe(id).inputs).Select(x=>x.id).Concat(machine.Input.Snapshot().Select(x=>x.id)).Distinct().ToList();
                }
                var progress = station.CaptureProgress();
                var m = new StationRuntimeState { id = station.Id, area = station.AreaId, requirement = station.Requirement, level = progress.level, workCount = progress.workCount, playerWorkCount=progress.playerWorkCount,
                    input = station is MachineStation ? station.Id + "_input" : station.Id, output = station.Id, progress = progress,
                    kind = station is MachineStation ? "machine" : station is ProductionStation ? "producer" : station is StorageStation ? "storage" : station is CargoDock?"dock":station is ShelfStation ? "shelf" : station is CheckoutStation ? "counter" : station is TableStation ? "table" : station is ConveyorStation ? "conveyor" : "purchase",
                    definitionId = station is MachineStation machine2 ? machine2.Recipe.id : station is PurchasePad pad ? pad.Upgrade.id : station.Id,
                    item = (station as ProductionStation)?.ItemId,batchYield=(station as ProductionStation)?.Yield??1,cycleSeconds=(station as ProductionStation)?.Interval??0, running = progress.running, remaining = station is MachineStation ? progress.remaining : 0, batches = progress.batches,playerBatches=progress.playerBatches };
                s.stations.Add(m);
                if(station is MachineStation auto){m.autonomous=true;m.recipeOptions=new(auto.Options);m.definitionVersion=auto.Recipe.version;}
                if (m.running)
                {
                    var recipe = data.contentVersion<2?Definitions.LegacyRecipe(m.definitionId):Definitions.Recipe(m.definitionId);m.batch=RecipeBatchSnapshot.From(recipe,"simulation"); m.jobId = "migrated-job:" + m.id + ":" + m.batches;
                    m.reservationId = "output:" + m.jobId; s.jobIds.Add(m.id + ":" + m.jobId);
                    s.reservations.Add(new ReservationState { id = m.reservationId, holder = "simulation", destination = m.output, item = recipe.output, quantity = recipe.yield, expiresAt = double.MaxValue });
                    m.escrow="escrow:"+m.jobId;
                    Owner(m.escrow,new Inventory(recipe.inputs.Sum(x=>x.count)),OwnerKind.Escrow,items:recipe.inputs.ToList());
                }
            }
            foreach (var purchase in data.purchases) s.purchases.Add(new PurchaseRuntimeState { id = purchase.id, definitionId = purchase.id, contributed = purchase.paid, complete = data.unlocked.Contains(purchase.id) });
            foreach (var worker in data.workers)
            {
                var crew = s.crews.Find(x => x.id == worker.upgrade); int capacity = crew.carryLevel == 3 ? 16 : crew.carryLevel == 2 ? 10 : 6;
                Owner(WorkerOwner(worker.id), new Inventory(capacity, true), OwnerKind.Worker, WorkerOwner(worker.id), worker.carry);
                s.owners[^1].worker = worker;
            }
            foreach (var shared in s.owners.Where(x => x.kind is not (OwnerKind.Player or OwnerKind.Worker))) shared.writers.AddRange(s.owners.Where(x => x.kind == OwnerKind.Worker).Select(x => x.actor));
            foreach (var customer in data.customers)
            {
                if(customer.phase==(int)CustomerAgent.State.Approaching)continue;
                Owner(CustomerId(customer.receipt), new Inventory(Math.Max(3, customer.order.Sum(x => x.requested))), OwnerKind.Customer, CustomerId(customer.receipt), customer.basket);
                s.owners[^1].customer = customer;
                s.orders.Add(new OrderRuntimeState { id = OrderId(customer.receipt), customer = CustomerId(customer.receipt), counter = customer.lane,
                    deadline = s.simulationTime + customer.remaining, lines = customer.order, status = customer.paid ? OrderStatus.Complete : customer.timedOut ? OrderStatus.Failed : OrderStatus.Open });
            }
            foreach (var diner in data.diners)
            {
                if(diner.phase==4)continue;
                Owner(CustomerId(diner.receipt), new Inventory(1), OwnerKind.Customer, CustomerId(diner.receipt), diner.basket); s.owners[^1].diner = diner;
                s.orders.Add(new OrderRuntimeState { id = OrderId(diner.receipt), customer = CustomerId(diner.receipt), counter = game.Checkouts.Find(x => x.ShopId == "restaurant").Id,
                    table = diner.table, dinerPhase = diner.phase, eatingRemaining = diner.phase == 2 ? diner.remaining : 0,
                    deadline = s.simulationTime + diner.remaining, lines = new() { new OrderLine(diner.item??"meal", 1, diner.price) { delivered = diner.phase >= 2 ? 1 : 0 } },
                    status = diner.phase >= 2 ? OrderStatus.Complete : OrderStatus.Open });
            }
            foreach(var worker in data.workers.Where(x=>x.phase is 1 or 2))
            {
                string source=worker.phase==2?WorkerOwner(worker.id):worker.source;
                string destination=worker.destination;
                var order=s.orders.Find(x=>x.id==OrderId(worker.tableReceipt));
                if(order!=null&&(destination==order.counter||destination==order.table))destination=order.customer;
                var allocations=new List<StackAllocation>();int remaining=worker.count;
                foreach(var stack in s.stacks.Where(x=>x.owner==source&&x.item==worker.item))
                {
                    int held=s.reservations.Where(x=>x.status==ReservationStatus.Active).Sum(x=>x.allocations.Where(a=>a.stack==stack.id).Sum(a=>a.quantity));
                    int amount=Math.Min(remaining,stack.quantity-held);if(amount>0){allocations.Add(new(){stack=stack.id,quantity=amount});remaining-=amount;}
                }
                if(remaining!=0)throw new InvalidDataException("Không khôi phục được stock reservation worker: "+worker.id);
                worker.reservation="migrated-route:"+worker.id;
                s.reservations.Add(new ReservationState{id=worker.reservation,holder=WorkerOwner(worker.id),source=source,destination=destination,
                    relay=worker.phase==1?WorkerOwner(worker.id):null,item=worker.item,quantity=worker.count,expiresAt=s.simulationTime+120,allocations=allocations});
            }
            if(data.version>=3){s.schemaVersion=3;s.money=0;s.cashInHand=data.cashInHand;s.cashInSafe=data.cashInSafe;CarryLimits.Apply(s);}
            TransactionCore.Validate(s); return s;
        }

        // Thêm owner/trạm mới của phiên bản gameplay mà không reset các owner đã lưu.
    }
}
