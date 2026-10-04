using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    // Component Unity chỉ đọc projection và gửi command; core là writer của tài sản/state.
    public sealed class RuntimeTransactions
    {
        readonly GameSession game;
        readonly TransactionCore core;
        readonly Dictionary<Inventory, string> inventories = new();
        readonly Dictionary<EntityId, string> actors = new();
        readonly GameplayTransactionStore store;
        TransactionState view;
        readonly double origin, restoredTime;
        public bool Ready => core.Lifecycle == CoreLifecycle.Ready && !game.SaveBlocked;
        public long Revision => core.Revision;
        public double Now => restoredTime + Time.timeAsDouble - origin;
        public string LastReason { get; private set; } = "";
        internal TransactionState View => view;
        public TransactionState Snapshot() => core.Snapshot();
        public RuntimeTransactions(GameSession game, TransactionState state, bool persist = true)
        {
            this.game = game; origin = Time.timeAsDouble; restoredTime = state.simulationTime;
            store = persist ? new GameplayTransactionStore(game) : null;
            core = new TransactionCore(state, store, () => Now); view = core.Snapshot();
            actors.Add(game.Player.GetEntityId(), "player");
            Bind(game.Player.Carry, "player"); game.Economy.Bind(this);
            foreach (var station in game.Stations.Where(x => x is not StationZone))
            {
                if (station.Inventory != null) Bind(station.Inventory, station.Id);
                if (station is MachineStation machine) Bind(machine.Input, station.Id + "_input");
            }
        }
        public TransactionCommand Command(TransactionKind kind, string actor, string target = null, string key = null, string effect = null)
        {
            key ??= Guid.NewGuid().ToString("N");
            return new() { kind = kind, actor = actor, target = target, key = key,
                effectId = effect ?? "effect:" + key, expectedRevision = Revision };
        }
        public TransactionReceipt Execute(TransactionCommand command)
        {
            if (!Ready) throw new TransactionRejectedException("recovery", "Gameplay chưa phục hồi xong.");
            try { return core.Execute(command); }
            finally { Refresh(); }
        }
        public bool TryExecute(TransactionCommand command, out int amount)
        {
            amount = 0; LastReason = "";
            try { amount = Execute(command).amount; return true; }
            catch (TransactionRejectedException error) { LastReason = error.Message; return false; }
            catch (IOException error) { game.BlockRecovery(error); LastReason = "Không ghi được giao dịch; gameplay đã dừng."; return false; }
        }
        internal StationRuntimeState Station(string id) => view.stations.Find(x => x.id == id);
        internal OrderRuntimeState Order(long receipt) => view.orders.Find(x => x.id == OrderId(receipt));
        public static string OrderId(long receipt) => "order:" + receipt;
        public static string CustomerId(long receipt) => "customer:" + receipt;
        public static string WorkerOwner(string id) => "worker:" + id;
        public string Actor(EntityId actor) => actors.TryGetValue(actor, out var id) ? id : throw new TransactionRejectedException("authority", "Actor chưa đăng ký.");
        public string OwnerId(Inventory inventory) => inventories.TryGetValue(inventory, out var id) ? id : throw new TransactionRejectedException("owner", "Inventory chưa có owner.");
        string CarrierActor(Inventory from, Inventory to)
        {
            var a = view.owners.Find(x => x.id == OwnerId(from)); var b = view.owners.Find(x => x.id == OwnerId(to));
            return a.kind is OwnerKind.Player or OwnerKind.Worker ? a.actor : b.kind is OwnerKind.Player or OwnerKind.Worker ? b.actor : "simulation";
        }
        public int Transfer(Inventory from, Inventory to, string item, int requested, string key = null, string effect = null, string reservation = null)
        {
            int count = string.IsNullOrEmpty(reservation) ? Math.Min(requested, Math.Min(from.Available(item), to.FreeFor(item))) : requested;
            if (count <= 0) return 0;
            var source = view.owners.Find(x => x.id == OwnerId(from)); var destination = view.owners.Find(x => x.id == OwnerId(to));
            var kind = source.kind is OwnerKind.Player or OwnerKind.Worker ? TransactionKind.Place : destination.kind is OwnerKind.Player or OwnerKind.Worker ? TransactionKind.Take : TransactionKind.Transfer;
            var c = Command(kind, CarrierActor(from, to), key: key, effect: effect);
            c.source = source.id; c.destination = destination.id; c.item = item; c.quantity = count; c.reservation = reservation;
            return TryExecute(c, out int moved) ? moved : 0;
        }
        public string Reserve(Inventory from, Inventory to, string item, int count, EntityId actor, Inventory relay = null)
        {
            string id = "reservation:" + Guid.NewGuid().ToString("N");
            var c = Command(TransactionKind.Reserve, Actor(actor), id);
            c.source = OwnerId(from); c.destination = OwnerId(to); c.item = item; c.quantity = count;
            c.secondary = relay == null ? null : OwnerId(relay); c.expiresAt = Now + 120;
            return TryExecute(c, out _) ? id : null;
        }
        public bool Release(string reservation, EntityId actor)
        {
            if (string.IsNullOrEmpty(reservation)) return false;
            return TryExecute(Command(TransactionKind.Release, Actor(actor), reservation), out _);
        }
        internal ReservationState Reservation(string id) => view.reservations.Find(x => x.id == id);
        public bool StationAction(TransactionKind kind, Station station, EntityId actor, float delta = 0, Inventory carrier = null)
        {
            var c = Command(kind, Actor(actor), station.Id); c.duration = delta;
            if (carrier != null) { c.source = OwnerId(carrier); c.destination = OwnerId(carrier); }
            return TryExecute(c, out _);
        }
        public bool TickProducer(ProductionStation station, float delta)
        {
            var p = Station(station.Id).progress;
            if (!station.Animal && p.phase != 2 || station.Animal && p.breeding <= 0 && (p.feed == 0 || p.herd == 0 || p.cycle == 0)) return false;
            var c = Command(TransactionKind.TickProducer, "simulation", station.Id); c.duration = delta;
            return TryExecute(c, out _);
        }
        public bool OperateMachine(MachineStation machine, float delta, EntityId actor)
        {
            var m = Station(machine.Id); string actorId = Actor(actor);
            if (!m.running)
            {
                var start = Command(TransactionKind.StartMachine, actorId, machine.Id);
                start.secondary = "job:" + Guid.NewGuid().ToString("N");
                if (!TryExecute(start, out _)) return false;
                m = Station(machine.Id);
            }
            var advance = Command(TransactionKind.AdvanceMachine, actorId, machine.Id);
            advance.secondary = m.jobId; advance.duration = delta * (1 + .15f * (machine.Level - 1));
            if (!TryExecute(advance, out _)) return false;
            m = Station(machine.Id);
            if (m.remaining == 0)
            {
                var finish = Command(TransactionKind.CompleteMachine, actorId, machine.Id, effect: "finish:" + m.id + ":" + m.jobId);
                finish.secondary = m.jobId; return TryExecute(finish, out _);
            }
            return true;
        }
        public void ReleaseOperator(Station station, EntityId actor)
        {
            if (!Ready || !actors.TryGetValue(actor, out var id) || Station(station.Id)?.operatorId != id) return;
            TryExecute(Command(TransactionKind.ReleaseOperator, id, station.Id), out _);
        }
        public bool OperatedByOther(Station station, EntityId actor)
        { var m = Station(station.Id); return m != null && m.operatorId != null && m.operatorId != Actor(actor) && Now < m.operatorUntil; }
        void Bind(Inventory inventory, string owner)
        {
            if (!view.owners.Any(x => x.id == owner)) throw new InvalidDataException("Owner không tồn tại: " + owner);
            inventories[inventory] = owner; inventory.Bind(this, owner); inventory.Project(view);
        }
        public void BindWorker(WorkerAgent worker, WorkerSave saved = null)
        {
            string id = WorkerOwner(worker.WorkerId);
            var crew=view.crews.Find(x=>x.id==worker.UpgradeId);
            int capacity=crew==null||crew.carryLevel==1?6:crew.carryLevel==2?10:16;
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
                CreateOrder(saved.receipt, counter.Id, id, new() { new OrderLine("meal", 1, saved.price) }, saved.remaining, saved.table,owner);
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
        public int Deliver(long receipt, Inventory source, string reservation = null)
        {
            var o = Order(receipt); if (o == null || o.status != OrderStatus.Open) return 0;
            if (Now >= o.deadline) { Fail(receipt); return 0; }
            int moved = 0;
            foreach (var line in o.lines.ToArray())
            {
                int quantity = Math.Min(line.Remaining, source.Available(line.id));
                if (reservation != null)
                { var r=Reservation(reservation);quantity=r!=null&&r.status==ReservationStatus.Active&&r.item==line.id?Math.Min(line.Remaining,r.quantity):0; }
                if (quantity == 0) continue;
                var c = Command(TransactionKind.DeliverOrder, view.owners.Find(x => x.id == OwnerId(source)).actor, o.id);
                c.source = OwnerId(source); c.item = line.id; c.quantity = quantity; c.reservation = reservation;
                if (TryExecute(c, out int delivered)) moved += delivered;
            }
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
        public int Collect(string counter)
        {
            int total = 0;
            foreach (var payment in view.payments.Where(x => x.counter == counter && !x.collected).ToArray())
                if (TryExecute(Command(TransactionKind.CollectPayment, "player", payment.id, effect: "collect:" + payment.id), out int amount)) total += amount;
            if((view.legacyCash.Find(x=>x.id==counter)?.amount??0)>0&&TryExecute(Command(TransactionKind.CollectPayment,"player","legacy-cash:"+counter,effect:"collect-legacy:"+counter),out int legacy))total+=legacy;
            return total;
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
        { if (store != null) store.Checkpoint(core.Snapshot()); }
        void Refresh()
        {
            view = core.Snapshot();
            foreach (var inventory in inventories.Keys) inventory.Project(view);
        }
        public void Detach()
        { foreach (var inventory in inventories.Keys) inventory.Unbind(); game.Economy.Unbind(); }

        public static TransactionState Migrate(GameSession game, SaveData data)
        {
            var s = new TransactionState { money = data.money, revenue = data.revenue, legacyRevenue = data.revenue,
                legacyTransactions = data.transactions, legacyPaid = data.receipts, legacyLosses = data.losses, legacyCash = data.cash,
                simulationTime = Time.timeAsDouble, unlocked = data.unlocked, crews = data.crews };
            void Owner(string id, Inventory inventory, OwnerKind kind, string actor = "simulation", List<ItemAmount> items = null)
            {
                s.owners.Add(new OwnerState { id = id, actor = actor, kind = kind, location = id,
                    capacity = inventory.Capacity, singleItem = inventory.SingleItem, limits = inventory.Limits,
                    writers = kind == OwnerKind.Player ? new() : new() { "player" } });
                foreach (var item in items ?? inventory.Snapshot()) s.stacks.Add(new ItemStackState {
                    id = "migrated:" + id + ":" + item.id, item = item.id, quantity = item.count, owner = id, location = id });
            }
            Owner("player", game.Player.Carry, OwnerKind.Player, "player");
            foreach (var station in game.Stations.Where(x => x is not StationZone))
            {
                OwnerKind kind = station is StorageStation ? OwnerKind.Storage : station is MachineStation ? OwnerKind.Machine : station is CheckoutStation ? OwnerKind.Counter : OwnerKind.Station;
                Owner(station.Id, station.Inventory ?? new Inventory(0), kind);
                if(station is ShelfStation shelf)s.owners[^1].accepts=new(shelf.AllowedItems);
                if(station is MachineStation machine)
                {
                    s.owners[^1].accepts=new(){machine.Recipe.output};Owner(station.Id + "_input", machine.Input, OwnerKind.Machine);
                    s.owners[^1].accepts=machine.Recipe.inputs.Select(x=>x.id).ToList();
                }
                var progress = station.CaptureProgress();
                var m = new StationRuntimeState { id = station.Id, area = station.AreaId, requirement = station.Requirement, level = progress.level, workCount = progress.workCount,
                    input = station is MachineStation ? station.Id + "_input" : station.Id, output = station.Id, progress = progress,
                    kind = station is MachineStation ? "machine" : station is ProductionStation ? "producer" : station is StorageStation ? "storage" : station is ShelfStation ? "shelf" : station is CheckoutStation ? "counter" : station is TableStation ? "table" : "purchase",
                    definitionId = station is MachineStation machine2 ? machine2.Recipe.id : station is PurchasePad pad ? pad.Upgrade.id : station.Id,
                    item = (station as ProductionStation)?.ItemId, running = progress.running, remaining = station is MachineStation ? progress.remaining : 0, batches = progress.batches };
                s.stations.Add(m);
                if (m.running)
                {
                    var recipe = Definitions.Recipe(m.definitionId); m.jobId = "migrated-job:" + m.id + ":" + m.batches;
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
                Owner(CustomerId(customer.receipt), new Inventory(Math.Max(3, customer.order.Sum(x => x.requested))), OwnerKind.Customer, CustomerId(customer.receipt), customer.basket);
                s.owners[^1].customer = customer;
                s.orders.Add(new OrderRuntimeState { id = OrderId(customer.receipt), customer = CustomerId(customer.receipt), counter = customer.lane,
                    deadline = s.simulationTime + customer.remaining, lines = customer.order, status = customer.paid ? OrderStatus.Complete : customer.timedOut ? OrderStatus.Failed : OrderStatus.Open });
            }
            foreach (var diner in data.diners)
            {
                Owner(CustomerId(diner.receipt), new Inventory(1), OwnerKind.Customer, CustomerId(diner.receipt), diner.basket); s.owners[^1].diner = diner;
                s.orders.Add(new OrderRuntimeState { id = OrderId(diner.receipt), customer = CustomerId(diner.receipt), counter = game.Checkouts.Find(x => x.ShopId == "restaurant").Id,
                    table = diner.table, dinerPhase = diner.phase, eatingRemaining = diner.phase == 2 ? diner.remaining : 0,
                    deadline = s.simulationTime + diner.remaining, lines = new() { new OrderLine("meal", 1, diner.price) { delivered = diner.phase >= 2 ? 1 : 0 } },
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
            TransactionCore.Validate(s); return s;
        }
        internal static SaveData Project(TransactionState state, SaveData data)
        {
            data.transactionState = TransactionCore.Copy(state); data.transactionVersion=1;data.money = state.money; data.revenue = state.revenue;
            data.transactions = state.legacyTransactions + state.payments.Count;
            data.unlocked = new(state.unlocked); data.crews = state.crews.Select(TransactionCore.Copy).ToList();
            data.purchases = state.purchases.Select(x => new PurchaseProgress { id = x.id, paid = x.contributed, total = Definitions.Upgrade(x.id).cost, complete = x.complete }).ToList();
            data.cash = state.owners.Where(x => x.kind == OwnerKind.Counter).Select(x => new CashSave { id = x.id,
                amount = state.payments.Where(p => p.counter == x.id && !p.collected).Sum(p => p.amount) + (state.legacyCash.Find(p => p.id == x.id)?.amount ?? 0) }).ToList();
            data.pendingCash = data.cash.Sum(x => x.amount);
            data.receipts = state.legacyPaid.Concat(state.payments.Select(x => long.Parse(x.order.Substring(6)))).Distinct().ToList();
            data.losses = state.legacyLosses.Select(TransactionCore.Copy).ToList();
            foreach (var order in state.orders.Where(x => x.status == OrderStatus.Failed))
                if (!data.losses.Any(x => x.receipt == long.Parse(order.id.Substring(6)))) data.losses.Add(new LossRecord {
                    receipt = long.Parse(order.id.Substring(6)), goods = Items(state, order.customer) });
            data.inventories = state.owners.Where(x => x.kind is not (OwnerKind.Worker or OwnerKind.Customer) && (x.id == "player" || data.inventories.Any(i => i.id == x.id)))
                .Select(x => new InventorySave(x.id, new Inventory(0)) { items = Items(state, x.id) }).ToList();
            data.stationStates = state.stations.Select(x => { var p = TransactionCore.Copy(x.progress); p.id = x.id; p.level = x.level; p.workCount = x.workCount;
                if (x.kind == "machine") { p.remaining = (float)x.remaining; p.running = x.running; p.batches = x.batches; } return p; }).ToList();
            foreach (var owner in state.owners.Where(x => x.kind == OwnerKind.Worker && !string.IsNullOrEmpty(x.worker?.id)))
            {
                var worker = data.workers.Find(x => x.id == owner.worker.id) ?? TransactionCore.Copy(owner.worker);
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
                    if (customer == null && order.status == OrderStatus.Open) { customer = TransactionCore.Copy(owner.customer); data.customers.Add(customer); }
                    if (customer == null) continue;
                    customer.basket = Items(state, owner.id); customer.order = order.lines.Select(TransactionCore.Copy).ToList();
                    customer.remaining = (float)Math.Max(0, order.deadline - state.simulationTime);
                    customer.paid = order.status == OrderStatus.Complete; customer.timedOut = order.status == OrderStatus.Failed;
                    if (customer.paid || customer.timedOut) customer.phase = (int)CustomerAgent.State.Leaving;
                }
                if (owner.diner?.receipt>0)
                {
                    var diner = data.diners.Find(x => x.receipt == receipt);
                    if (diner == null && order.dinerPhase < 3) { diner = TransactionCore.Copy(owner.diner); data.diners.Add(diner); }
                    if (diner == null) continue;
                    diner.phase = order.dinerPhase; diner.remaining = (float)(diner.phase == 2 ? order.eatingRemaining : Math.Max(0, order.deadline - state.simulationTime)); diner.basket = Items(state, owner.id);
                }
            }
            return data;
        }
        internal static List<ItemAmount> Items(TransactionState state, string owner) => state.stacks.Where(x => x.owner == owner).GroupBy(x => x.item).Select(x => new ItemAmount(x.Key, x.Sum(a => a.quantity))).ToList();
    }
    // Save scene + mutation/receipt/dedup/outbox dùng chung một lần replace, không có file core rời.
    public sealed class GameplayTransactionStore : ITransactionStore
    {
        readonly GameSession game;
        public Action<CommitBoundary> Fault { get; set; }
        public GameplayTransactionStore(GameSession game) { this.game = game; }
        public TransactionState Read() => SaveStore.Read(game.SavePath)?.transactionState;
        public void Write(TransactionState state) => Write(state, false);
        public void Checkpoint(TransactionState state) => Write(state, true);
        void Write(TransactionState state, bool checkpoint)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(game.SavePath)));
            using var lease = new FileStream(game.SavePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            var previous = Read();
            if (previous != null && previous.revision != state.revision - (checkpoint ? 0 : 1) || previous == null && state.revision != 0)
                throw new IOException("Save revision đã thay đổi; cần recovery trước khi retry.");
            var data = RuntimeTransactions.Project(state, game.CaptureSaveData());
            SaveStore.Write(game.SavePath, data, Fault);
        }
    }
}
