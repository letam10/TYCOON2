using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    // Một writer, một khóa và một durable envelope cho toàn bộ thay đổi nghiệp vụ.
    public sealed class TransactionCore
    {
        readonly object gate = new();
        readonly ITransactionStore store;
        readonly Func<double> clock;
        readonly bool runtime;
        readonly Dictionary<string,TransactionReceipt> keys=new(),effects=new();
        TransactionState state;
        public CoreLifecycle Lifecycle { get; private set; }
        public Action<CommitBoundary> Fault { get; set; }
        public TransactionCore(TransactionState seed, ITransactionStore store = null, Func<double> clock = null, bool runtime = false)
        {
            this.store = store;
            this.runtime = runtime;
            this.clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d);
            state = NormalizeState(Copy(store?.Read() ?? seed ?? throw new ArgumentNullException(nameof(seed))));
            RefreshMachinePhases(state);
            Validate(state);
            IndexReceipts();
            if (store != null && store.Read() == null) store.Write(state);
        }
        public long Revision { get { lock (gate) return state.revision; } }
        public TransactionState Snapshot() { lock (gate) return Copy(state); }
        internal TransactionState RuntimeSnapshot(){lock(gate)return state.RuntimeCopy(false);}
        void IndexReceipts(){keys.Clear();effects.Clear();foreach(var r in state.receipts){keys.Add(r.key,r);effects.Add(r.effectId,r);}}
        internal static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        public TransactionReceipt Execute(TransactionCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            lock (gate)
            {
                Require(Lifecycle == CoreLifecycle.Ready, "recovery", "Core chưa phục hồi xong.");
                var c = Copy(command);
                Require(!string.IsNullOrWhiteSpace(c.key) && !string.IsNullOrWhiteSpace(c.effectId) && !string.IsNullOrWhiteSpace(c.actor), "identity", "Thiếu command, effect hoặc actor ID.");
                string fingerprint = Fingerprint(c, false), effectFingerprint = Fingerprint(c, true);
                keys.TryGetValue(c.key,out var receipt);
                if (receipt != null)
                { Require(receipt.fingerprint == fingerprint, "key-conflict", "Key đã dùng với payload khác."); return Copy(receipt); }
                effects.TryGetValue(c.effectId,out receipt);
                if (receipt != null)
                { Require(receipt.effectFingerprint == effectFingerprint, "effect-conflict", "Effect đã dùng với payload khác."); return Copy(receipt); }
                Require(c.expectedRevision == state.revision, "stale", "State version đã thay đổi.");
                var draft = runtime ? state.RuntimeCopy(true,c.kind==TransactionKind.AcknowledgeEvent?c.target:null,
                    c.kind is TransactionKind.RegisterOwner or TransactionKind.CompletePurchase or TransactionKind.UpgradeCrew) : Copy(state);
                Expire(draft, clock());
                int amount = Apply(draft, c);
                RefreshMachinePhases(draft);
                draft.simulationTime = clock();
                draft.revision = checked(state.revision + 1);
                receipt = new TransactionReceipt {
                    id = "receipt:" + c.key, key = c.key, effectId = c.effectId,
                    fingerprint = fingerprint, effectFingerprint = effectFingerprint,
                    eventId = "event:" + c.key, revision = draft.revision, amount = amount
                };
                draft.receipts.Add(receipt);
                draft.outbox.Add(new TransactionEvent { id = receipt.eventId, receiptId = receipt.id,
                    effectId = c.effectId, revision = draft.revision, kind = c.kind.ToString() });
                try
                {
                    if(!runtime)Validate(draft);
                    else Require(draft.money>=0&&draft.revenue>=0&&draft.cashCollected>=0&&draft.receipts.Count==draft.revision,"journal","Giao dịch runtime không hợp lệ.");
                    Fault?.Invoke(CommitBoundary.Prepared);
                    if(store is ICommandTransactionStore journal)journal.Write(draft,c);else store?.Write(draft);
                }
                catch
                {
                    // Lịch sử bất biến được dùng chung; rollback phần append khi chưa publish.
                    if(runtime){state.receipts.Remove(receipt);if(ReferenceEquals(state.outbox,draft.outbox))state.outbox.RemoveAt(state.outbox.Count-1);}
                    // Có thể lỗi sau replace nhưng trước trả lời: đọc lại receipt trước khi cho retry.
                    if(store!=null)
                        try { var recovered = store.Read(); Validate(recovered); state = Copy(recovered);IndexReceipts(); }
                        catch { Lifecycle = CoreLifecycle.RecoveryFailed; }
                    throw;
                }
                state = draft;
                keys.Add(receipt.key,receipt);effects.Add(receipt.effectId,receipt);
                Fault?.Invoke(CommitBoundary.Published);
                return Copy(receipt);
            }
        }
        public IReadOnlyList<TransactionEvent> PendingEvents(string consumer)
        { lock (gate) return state.outbox.Where(e => !e.consumers.Contains(consumer) && e.kind != nameof(TransactionKind.AcknowledgeEvent)).Select(Copy).ToArray(); }
        static string Fingerprint(TransactionCommand c, bool effect)
        {
            var payload = Copy(c);
            if (effect) { payload.key = null; payload.expectedRevision = 0; }
            return FileTransactionStore.Hash(JsonUtility.ToJson(payload));
        }
        int Apply(TransactionState s, TransactionCommand c)
        {
            switch (c.kind)
            {
                case TransactionKind.Take:
                case TransactionKind.Place:
                case TransactionKind.Transfer: return Move(s, c);
                case TransactionKind.Reserve: return Reserve(s, c);
                case TransactionKind.Release:
                    var reservation = Reservation(s, c.target);
                    Require(reservation.holder == c.actor, "authority", "Reservation thuộc actor khác.");
                    if (reservation.status != ReservationStatus.Active) return 0;
                    reservation.status = ReservationStatus.Released; return reservation.quantity;
                case TransactionKind.Split: return Split(s, c);
                case TransactionKind.Merge: return Merge(s, c);
                case TransactionKind.CreateOrder: return CreateOrder(s, c);
                case TransactionKind.DeliverOrder: return Deliver(s, c);
                case TransactionKind.CompleteOrder: return CompleteOrder(s, c);
                case TransactionKind.FailOrder: return FailOrder(s, c);
                case TransactionKind.CreatePayment: return CreatePayment(s, c);
                case TransactionKind.CollectPayment: return Collect(s, c);
                case TransactionKind.ContributePurchase: return Contribute(s, c);
                case TransactionKind.CompletePurchase: return CompletePurchase(s, c);
                case TransactionKind.StartMachine: return StartMachine(s, c);
                case TransactionKind.AdvanceMachine: return AdvanceMachine(s, c);
                case TransactionKind.CompleteMachine: return CompleteMachine(s, c);
                case TransactionKind.AcknowledgeEvent:
                    Require(c.actor == c.secondary && !string.IsNullOrWhiteSpace(c.secondary), "authority", "Sai consumer.");
                    var e = s.outbox.Find(x => x.id == c.target);
                    Require(e != null, "event", "Event không tồn tại.");
                    if (e.consumers.Contains(c.secondary)) return 0;
                    e.consumers.Add(c.secondary);
                    var consumer = s.consumers.Find(x => x.id == c.secondary);
                    if (consumer == null) { consumer = new ConsumerState { id = c.secondary }; s.consumers.Add(consumer); }
                    consumer.appliedEvents++; return 1;
                case TransactionKind.RegisterOwner:
                    Require(c.actor == "simulation" && c.owner != null, "authority", "Chỉ mô phỏng đăng ký owner.");
                    Require(!s.owners.Any(x => x.id == c.owner.id), "duplicate", "Owner đã tồn tại.");
                    s.owners.Add(Copy(c.owner));
                    if (c.owner.kind == OwnerKind.Worker)
                        foreach (var shared in s.owners.Where(x => x.kind is not (OwnerKind.Player or OwnerKind.Worker)))
                            if (!shared.writers.Contains(c.owner.actor)) shared.writers.Add(c.owner.actor);
                    return 1;
                case TransactionKind.OperateProducer: return OperateProducer(s, c);
                case TransactionKind.RestockProducer: return RestockProducer(s, c);
                case TransactionKind.TickProducer: return TickProducer(s, c);
                case TransactionKind.HarvestProducer: return HarvestProducer(s, c);
                case TransactionKind.FeedProducer: return FeedProducer(s, c);
                case TransactionKind.ReleaseOperator:
                    var station = Station(s, c.target);
                    if (station.operatorId != c.actor) return 0;
                    station.operatorId = null; station.operatorUntil = 0; return 1;
                case TransactionKind.BreakMachine:
                    Require(c.actor == "simulation", "authority", "Chỉ mô phỏng làm hỏng máy.");
                    var broken = Station(s, c.target); Require(broken.kind == "machine", "station", "Không phải máy.");
                    if (broken.progress.broken) return 0;
                    Require(!s.stations.Any(x=>x.kind=="machine"&&x.progress.broken), "breakdown", "Đã có một máy hỏng.");
                    broken.progress.broken = true; broken.progress.repairFee = Math.Clamp(c.quantity, 10, 100);
                    broken.progress.repairRemaining = 8; broken.progress.repairPaid = false; return 1;
                case TransactionKind.RepairMachine: return RepairMachine(s, c);
                case TransactionKind.UpgradeCrew: return UpgradeCrew(s, c);
                case TransactionKind.CleanTable: return CleanTable(s, c);
                case TransactionKind.AdvanceDiner: return AdvanceDiner(s, c);
                case TransactionKind.Checkpoint:
                    Require(c.actor == "simulation", "authority", "Chỉ mô phỏng ghi checkpoint."); return 0;
                default: throw new TransactionRejectedException("kind", "Transaction không hỗ trợ.");
            }
        }
        static void Require(bool condition, string code, string message)
        { if (!condition) throw new TransactionRejectedException(code, message); }
        static OwnerState Owner(TransactionState s, string id)
        { var owner = s.owners.Find(x => x.id == id); Require(owner != null, "owner", "Owner không tồn tại: " + id); return owner; }
        static void WriteAccess(OwnerState owner, string actor)
        { Require(actor == "simulation" || owner.actor == actor || owner.writers.Contains(actor), "authority", "Không có quyền cập nhật " + owner.id); }
        static void Player(TransactionState s, string actor)
        { Require(s.owners.Any(x => x.kind == OwnerKind.Player && x.actor == actor), "authority", "Chỉ player được dùng tiền."); }
        static ItemStackState Stack(TransactionState s, string id)
        { var stack = s.stacks.Find(x => x.id == id); Require(stack != null, "stack", "Stack không tồn tại: " + id); return stack; }
        static ReservationState Reservation(TransactionState s, string id)
        { var r = s.reservations.Find(x => x.id == id); Require(r != null, "reservation", "Reservation không tồn tại."); return r; }
        static int Reserved(TransactionState s, string stack) => s.reservations.Where(x => x.status == ReservationStatus.Active).Sum(x => x.allocations.Where(a => a.stack == stack).Sum(a => a.quantity));
        static int Free(TransactionState s, OwnerState owner, string item)
        {
            if(owner.accepts.Count>0&&!owner.accepts.Contains(item))return 0;
            var stock = s.stacks.Where(x => x.owner == owner.id).ToArray();
            var held = s.reservations.Where(x => x.status == ReservationStatus.Active && (x.destination == owner.id || x.relay == owner.id)).ToArray();
            if (owner.singleItem && (stock.Any(x => x.item != item) || held.Any(x => x.item != item))) return 0;
            int total = checked(stock.Sum(x => x.quantity) + held.Sum(x => x.quantity));
            int count = checked(stock.Where(x => x.item == item).Sum(x => x.quantity) + held.Where(x => x.item == item).Sum(x => x.quantity));
            int limit = owner.limits.Find(x => x.id == item)?.count ?? owner.capacity;
            return Math.Max(0, Math.Min(owner.capacity - total, limit - count));
        }
        static void Expire(TransactionState s, double now)
        { foreach (var r in s.reservations) if (r.status == ReservationStatus.Active && r.expiresAt <= now && !s.stations.Any(m => m.running && m.reservationId == r.id)) r.status = ReservationStatus.Expired; }
        static void AddStack(TransactionState s, string id, string owner, string item, int count)
        {
            Require(count > 0 && Definitions.Item(item) != null, "item", "Item hoặc lượng không hợp lệ.");
            Require(!s.stacks.Any(x => x.id == id), "duplicate", "Stack ID đã tồn tại.");
            var location = Owner(s, owner);
            s.stacks.Add(new ItemStackState { id = id, owner = owner, location = StackLocation(location,item), item = item, quantity = count });
        }
        static string StackLocation(OwnerState owner,string item) => owner.kind==OwnerKind.Storage?owner.location+"/"+item:owner.location;
        static string FeedItem(string livestock) => livestock is "milk" or "egg" or "beef"?"carrot":null;
        static List<StackAllocation> Allocate(TransactionState s, string owner, string item, int quantity)
        {
            var result = new List<StackAllocation>();
            foreach (var stack in s.stacks.Where(x => x.owner == owner && x.item == item).OrderBy(x => x.id))
            {
                int n = Math.Min(quantity, stack.quantity - Reserved(s, stack.id));
                if (n > 0) { result.Add(new StackAllocation { stack = stack.id, quantity = n }); quantity -= n; }
                if (quantity == 0) break;
            }
            Require(quantity == 0, "stock", "Không đủ hàng khả dụng."); return result;
        }
        static void Consume(TransactionState s, IEnumerable<StackAllocation> allocations)
        {
            foreach (var a in allocations) { var stack = Stack(s, a.stack); stack.quantity -= a.quantity; if (stack.quantity == 0) s.stacks.Remove(stack); }
        }
        static int Move(TransactionState s, TransactionCommand c, bool delivery = false)
        {
            Require(c.quantity > 0 && c.source != c.destination, "quantity", "Transfer cần lượng dương và hai owner khác nhau.");
            var source = Owner(s, c.source); var destination = Owner(s, c.destination);
            WriteAccess(source, c.actor); WriteAccess(destination, c.actor);
            Require(source.kind != OwnerKind.Customer && (destination.kind != OwnerKind.Customer || delivery), "escrow", "Hàng của khách chỉ được cập nhật qua đơn.");
            if (c.kind == TransactionKind.Take) Require(destination.actor == c.actor && destination.kind is OwnerKind.Player or OwnerKind.Worker, "direction", "Take phải vào người mang.");
            if (c.kind == TransactionKind.Place) Require(source.actor == c.actor && source.kind is OwnerKind.Player or OwnerKind.Worker, "direction", "Place phải từ người mang.");
            List<StackAllocation> allocation;
            if (!string.IsNullOrEmpty(c.reservation))
            {
                var r = Reservation(s, c.reservation);
                Require(r.status == ReservationStatus.Active && r.holder == c.actor && r.source == c.source && (r.destination == c.destination || r.relay == c.destination) && r.item == c.item && r.quantity == c.quantity, "reservation", "Reservation không khớp hoặc hết hạn.");
                allocation = r.allocations;
                bool relay = r.relay == c.destination;
                r.status = ReservationStatus.Used;
                Require(Free(s, destination, c.item) >= c.quantity, "capacity", "Đích không đủ chỗ.");
                Consume(s, allocation); AddStack(s, "stack:" + c.effectId, destination.id, c.item, c.quantity);
                if (relay)
                {
                    // Giữ chỗ ở đích suốt hai chặng; stock reservation theo hàng sang giỏ worker.
                    r.status = ReservationStatus.Active; r.source = destination.id; r.relay = null;
                    r.allocations = new() { new StackAllocation { stack = "stack:" + c.effectId, quantity = c.quantity } };
                }
                CountPlace(s, c); return c.quantity;
            }
            else allocation = Allocate(s, c.source, c.item, c.quantity);
            Require(Free(s, destination, c.item) >= c.quantity, "capacity", "Đích không đủ chỗ.");
            Consume(s, allocation); AddStack(s, "stack:" + c.effectId, destination.id, c.item, c.quantity);
            CountPlace(s, c); return c.quantity;
        }
        int Reserve(TransactionState s, TransactionCommand c)
        {
            Require(c.quantity > 0 && c.source != c.destination && double.IsFinite(c.expiresAt) && c.expiresAt > clock(), "reservation", "Reservation hoặc thời hạn không hợp lệ.");
            Require(!s.reservations.Any(x => x.id == c.target), "duplicate", "Reservation ID đã tồn tại.");
            var source = Owner(s, c.source); var destination = Owner(s, c.destination);
            WriteAccess(source, c.actor); WriteAccess(destination, c.actor);
            Require(source.kind != OwnerKind.Customer, "escrow", "Không reserve hàng đã giao cho khách.");
            Require(Free(s, destination, c.item) >= c.quantity, "capacity", "Đích không đủ chỗ.");
            if (!string.IsNullOrEmpty(c.secondary))
            {
                var relay = Owner(s, c.secondary); WriteAccess(relay, c.actor);
                bool workerRelay=relay.kind==OwnerKind.Worker&&relay.actor==c.actor;
                bool conveyorRelay=relay.kind==OwnerKind.Conveyor&&relay.actor=="simulation"&&c.actor=="simulation";
                Require((workerRelay||conveyorRelay)&&relay.id!=source.id&&relay.id!=destination.id&&Free(s,relay,c.item)>=c.quantity,
                    "capacity", "Khoang trung chuyển không đủ chỗ hoặc không thuộc actor.");
            }
            s.reservations.Add(new ReservationState { id = c.target, holder = c.actor, source = c.source, destination = c.destination, item = c.item,
                relay = c.secondary, quantity = c.quantity, expiresAt = c.expiresAt, allocations = Allocate(s, source.id, c.item, c.quantity) }); return c.quantity;
        }
        static int Split(TransactionState s, TransactionCommand c)
        {
            var stack = Stack(s, c.target); WriteAccess(Owner(s, stack.owner), c.actor);
            Require(c.quantity > 0 && c.quantity < stack.quantity, "quantity", "Split phải giữ hai stack dương.");
            int available = stack.quantity - Reserved(s, stack.id), reservedToMove = Math.Max(0, c.quantity - available);
            AddStack(s, c.secondary, stack.owner, stack.item, c.quantity); stack.quantity -= c.quantity;
            foreach (var r in s.reservations.Where(x => x.status == ReservationStatus.Active))
            {
                var a = r.allocations.Find(x => x.stack == stack.id); if (a == null || reservedToMove == 0) continue;
                int n = Math.Min(reservedToMove, a.quantity); a.quantity -= n; reservedToMove -= n;
                r.allocations.Add(new StackAllocation { stack = c.secondary, quantity = n }); if (a.quantity == 0) r.allocations.Remove(a);
            }
            return c.quantity;
        }
        static int Merge(TransactionState s, TransactionCommand c)
        {
            var a = Stack(s, c.target); var b = Stack(s, c.secondary); WriteAccess(Owner(s, a.owner), c.actor);
            Require(a != b && a.owner == b.owner && a.item == b.item && a.definitionVersion == b.definitionVersion, "merge", "Stack không tương thích.");
            int count = b.quantity; a.quantity = checked(a.quantity + count); s.stacks.Remove(b);
            foreach (var r in s.reservations.Where(x => x.status == ReservationStatus.Active))
            {
                var from = r.allocations.Find(x => x.stack == b.id); if (from == null) continue;
                var to = r.allocations.Find(x => x.stack == a.id);
                if (to == null) from.stack = a.id; else { to.quantity += from.quantity; r.allocations.Remove(from); }
            }
            return count;
        }
        OrderRuntimeState Order(TransactionState s, TransactionCommand c, bool open = true)
        {
            var o = s.orders.Find(x => x.id == c.target); Require(o != null, "order", "Order không tồn tại.");
            WriteAccess(Owner(s, o.counter), c.actor);
            if (open) Require(o.status == OrderStatus.Open && clock() < o.deadline, "order-closed", "Order đã đóng hoặc hết hạn."); return o;
        }
        int CreateOrder(TransactionState s, TransactionCommand c)
        {
            Require(c.actor == "simulation", "authority", "Chỉ mô phỏng tạo đơn.");
            Require(!s.orders.Any(x => x.id == c.target) && double.IsFinite(c.expiresAt) && c.expiresAt > clock(), "order", "Order ID hoặc hạn không hợp lệ.");
            if(!string.IsNullOrEmpty(c.owner?.id))
            {
                Require(c.owner.id==c.destination&&c.owner.kind==OwnerKind.Customer&&!s.owners.Any(x=>x.id==c.owner.id),"owner","Customer owner không hợp lệ.");
                s.owners.Add(Copy(c.owner));
            }
            Require(Owner(s, c.destination).kind == OwnerKind.Customer && Owner(s, c.source).kind == OwnerKind.Counter, "owner", "Sai owner đơn.");
            Require(c.lines.Count > 0 && c.lines.Select(x => x.id).Distinct().Count() == c.lines.Count && c.lines.All(x => Definitions.Item(x.id) != null && x.requested > 0 && x.delivered == 0 && x.unitPrice >= 0), "lines", "Dòng đơn không hợp lệ.");
            s.orders.Add(new OrderRuntimeState { id = c.target, customer = c.destination, counter = c.source, table = c.secondary, deadline = c.expiresAt, lines = c.lines.Select(Copy).ToList() }); return 1;
        }
        int Deliver(TransactionState s, TransactionCommand c)
        {
            var o = Order(s, c);int moved=0;
            var shipments=c.lines.Count==0?new List<OrderLine>{new(c.item,c.quantity,0)}:c.lines;
            Require(shipments.Count>0&&shipments.Select(x=>x.id).Distinct().Count()==shipments.Count&&
                (string.IsNullOrEmpty(c.reservation)||shipments.Count==1),"delivery","Danh sách hàng giao không hợp lệ.");
            foreach(var shipment in shipments)
            {
                var line=o.lines.Find(x=>x.id==shipment.id);
                Require(line!=null&&shipment.requested>0&&shipment.delivered==0&&shipment.requested<=line.Remaining,
                    "delivery","Giao quá nhu cầu hoặc sai hàng.");
                var move=Copy(c);move.kind=TransactionKind.Transfer;move.destination=o.customer;move.item=shipment.id;
                move.quantity=shipment.requested;move.lines.Clear();move.effectId=c.effectId+":"+shipment.id;
                Move(s,move,true);line.delivered+=shipment.requested;moved=checked(moved+shipment.requested);
            }
            if(c.actor=="player"&&o.lines.All(x=>x.Remaining==0))
            {
                var completed=s.stations.Find(x=>x.id==(string.IsNullOrEmpty(o.table)?o.counter:o.table));
                if(completed!=null)
                {completed.playerWorkCount++;if(!string.IsNullOrEmpty(o.table))completed.progress.playerServeCount++;}
            }
            return moved;
        }
        int CompleteOrder(TransactionState s, TransactionCommand c)
        {
            var o = Order(s, c, false); if (o.status == OrderStatus.Complete) return 0;
            Require(o.status == OrderStatus.Open && clock() < o.deadline && o.lines.All(x => x.Remaining == 0), "order-incomplete", "Chưa giao đủ hoặc đã hết hạn.");
            o.status = OrderStatus.Complete;
            if (!string.IsNullOrEmpty(o.table)) { o.dinerPhase = 2; o.eatingRemaining = 6; }
            return 1;
        }
        int FailOrder(TransactionState s, TransactionCommand c)
        {
            var o = Order(s, c, false); if (o.status == OrderStatus.Failed) return 0;
            Require(o.status == OrderStatus.Open, "order-closed", "Đơn đã hoàn tất.");
            o.status = OrderStatus.Failed;
            if (!string.IsNullOrEmpty(o.table)) o.dinerPhase = 3;
            foreach (var r in s.reservations.Where(x => x.destination == o.customer && x.status == ReservationStatus.Active)) r.status = ReservationStatus.Released;
            return o.lines.Sum(x => checked(x.delivered * x.unitPrice));
        }
        int CreatePayment(TransactionState s, TransactionCommand c)
        {
            var o = Order(s, c, false); Require(o.status == OrderStatus.Complete, "order-incomplete", "Đơn chưa thành công.");
            if(long.TryParse(o.id.Replace("order:",""),out long legacyReceipt)&&s.legacyPaid.Contains(legacyReceipt))return 0;
            if (s.payments.Any(x => x.order == o.id)) return 0;
            int amount = o.lines.Sum(x => checked(x.requested * x.unitPrice));
            s.payments.Add(new PaymentState { id = "payment:" + o.id, order = o.id, counter = o.counter, amount = amount });
            s.revenue = checked(s.revenue + amount);
            var station = s.stations.Find(x => x.id == o.counter); if (station != null) station.workCount++;
            return amount;
        }
        static int Collect(TransactionState s, TransactionCommand c)
        {
            Player(s, c.actor);
            if(c.target!=null&&c.target.StartsWith("legacy-cash:"))
            {
                string counter=c.target.Substring(12);WriteAccess(Owner(s,counter),c.actor);
                var cash=s.legacyCash.Find(x=>x.id==counter);if(cash==null||cash.amount==0)return 0;
                int amount=cash.amount;s.money=checked(s.money+amount);s.cashCollected=checked(s.cashCollected+amount);cash.amount=0;return amount;
            }
            var payment = s.payments.Find(x => x.id == c.target);
            Require(payment != null, "payment", "Payment không tồn tại."); WriteAccess(Owner(s, payment.counter), c.actor);
            if (payment.collected) return 0;
            s.money = checked(s.money + payment.amount);s.cashCollected=checked(s.cashCollected+payment.amount); payment.collected = true; return payment.amount;
        }
        static int Contribute(TransactionState s, TransactionCommand c)
        {
            Player(s, c.actor); var definition = Definitions.Upgrade(c.target);
            Require(definition != null && definition.kind != "legacy" && c.quantity > 0 && !s.unlocked.Contains(c.target), "purchase", "Purchase không khả dụng.");
            Require(string.IsNullOrEmpty(definition.requirement) || s.unlocked.Contains(definition.requirement), "requirement", "Thiếu điều kiện mở khóa.");
            ValidatePurchaseRequirements(s,definition);
            var purchase = s.purchases.Find(x => x.id == c.target);
            if (purchase == null) { purchase = new PurchaseRuntimeState { id = c.target, definitionId = c.target }; s.purchases.Add(purchase); }
            int amount = Math.Min(c.quantity, Math.Min(s.money, definition.cost - purchase.contributed));
            Require(amount > 0, "funds", "Không còn tiền hoặc purchase đã góp đủ.");
            s.money -= amount; purchase.contributed += amount; return amount;
        }
        static int CompletePurchase(TransactionState s, TransactionCommand c)
        {
            Player(s, c.actor); var purchase = s.purchases.Find(x => x.id == c.target); var definition = Definitions.Upgrade(c.target);
            Require(purchase != null && definition != null && purchase.definitionVersion == definition.version && purchase.contributed == definition.cost, "purchase-incomplete", "Purchase chưa góp đủ.");
            if (purchase.complete) return 0;
            purchase.complete = true; if (!s.unlocked.Contains(c.target)) s.unlocked.Add(c.target);
            if (definition.kind == "worker" && !s.crews.Any(x => x.id == c.target)) s.crews.Add(GameSession.CrewFor(definition));
            RefreshProgression(s);
            return 1;
        }
        static StationRuntimeState Machine(TransactionState s, TransactionCommand c)
        {
            var m = s.stations.Find(x => x.id == c.target); Require(m != null, "station", "Station không tồn tại.");
            Require(m.kind == "machine" && (string.IsNullOrEmpty(m.requirement) || s.unlocked.Contains(m.requirement)), "requirement", "Máy chưa mở.");
            WriteAccess(Owner(s, m.input), c.actor); WriteAccess(Owner(s, m.output), c.actor); return m;
        }
        int StartMachine(TransactionState s, TransactionCommand c)
        {
            var m = Machine(s, c); var recipe = Array.Find(Definitions.Recipes, x => x.id == m.definitionId);
            Require(recipe != null && !m.running && !m.progress.broken, "machine", "Máy đang chạy hoặc bị hỏng.");
            Lease(m, c.actor, clock());
            Require(!string.IsNullOrWhiteSpace(c.secondary), "job", "Thiếu job ID.");
            Require(!s.jobIds.Contains(m.id + ":" + c.secondary), "job", "Job ID đã dùng.");
            Require(m.machinePhase == MachinePhase.Ready, "machine", "Máy chưa sẵn sàng hoặc đang chờ lấy hàng.");
            Require(Free(s, Owner(s, m.output), recipe.output) >= recipe.yield, "capacity", "Đầu ra đầy.");
            m.escrow="escrow:"+m.id+":"+c.secondary;
            s.owners.Add(new OwnerState{id=m.escrow,actor="simulation",location=m.escrow,kind=OwnerKind.Escrow,capacity=recipe.inputs.Sum(x=>x.count)});
            foreach (var input in recipe.inputs)
            {
                Consume(s, Allocate(s, m.input, input.id, input.count));
                AddStack(s,"job-input:"+m.id+":"+c.secondary+":"+input.id,m.escrow,input.id,input.count);
            }
            m.jobId = c.secondary; m.reservationId = "output:" + m.id + ":" + c.secondary; m.operatorId = c.actor;
            s.jobIds.Add(m.id + ":" + c.secondary);
            s.reservations.Add(new ReservationState { id = m.reservationId, holder = c.actor, destination = m.output, item = recipe.output, quantity = recipe.yield, expiresAt = double.MaxValue });
            m.running = true; m.machinePhase = MachinePhase.Operating; m.remaining = recipe.seconds; return 1;
        }
        int AdvanceMachine(TransactionState s, TransactionCommand c)
        {
            var m = Machine(s, c); Require(m.running && !m.progress.broken && m.jobId == c.secondary, "lease", "Sai job hoặc máy đang hỏng.");
            Lease(m, c.actor, clock());
            Require(double.IsFinite(c.duration) && c.duration > 0, "duration", "Delta không hợp lệ.");
            m.remaining = Math.Max(0, m.remaining - c.duration); return 1;
        }
        static int CompleteMachine(TransactionState s, TransactionCommand c)
        {
            var m = Machine(s, c);
            if (!m.running && m.jobId == c.secondary) return 0;
            Require(m.running && m.jobId == c.secondary && m.operatorId == c.actor && m.remaining == 0, "machine", "Job chưa sẵn sàng.");
            var recipe = Array.Find(Definitions.Recipes, x => x.id == m.definitionId);
            var reservation = Reservation(s, m.reservationId); Require(reservation.status == ReservationStatus.Active, "reservation", "Mất chỗ đầu ra.");
            reservation.status = ReservationStatus.Used;
            Require(Free(s, Owner(s, m.output), recipe.output) >= recipe.yield, "capacity", "Đầu ra không còn chỗ.");
            if(!string.IsNullOrEmpty(m.escrow))s.stacks.RemoveAll(x=>x.owner==m.escrow);
            AddStack(s, "job-output:" + m.id + ":" + m.jobId, m.output, recipe.output, recipe.yield);
            m.running = false; m.machinePhase = MachinePhase.CompletedWaitingPickup; m.batches++; m.workCount++; if(c.actor=="player"){m.playerWorkCount++;m.playerBatches++;} m.remaining = 0; return recipe.yield;
        }
        static StationRuntimeState Station(TransactionState s, string id)
        { var m = s.stations.Find(x => x.id == id); Require(m != null, "station", "Station không tồn tại: " + id); return m; }
        static void CountPlace(TransactionState s, TransactionCommand c)
        {
            var station = s.stations.Find(x => x.output == c.destination || x.input == c.destination);
            if(station==null)return;
            if(c.kind==TransactionKind.Place)station.workCount++;
            if(c.actor=="player")station.playerWorkCount++;
        }
        static void Lease(StationRuntimeState m, string actor, double now)
        {
            Require(string.IsNullOrEmpty(m.operatorId) || m.operatorId == actor || now >= m.operatorUntil, "lease", "Trạm đang có người vận hành.");
            m.operatorId = actor; m.operatorUntil = now + .25;
        }
        StationRuntimeState Producer(TransactionState s, TransactionCommand c, bool lease = true)
        {
            var m = Station(s, c.target);
            Require(m.kind == "producer" && (string.IsNullOrEmpty(m.requirement) || s.unlocked.Contains(m.requirement)), "requirement", "Trạm chưa mở.");
            if (lease) { WriteAccess(Owner(s, m.output), c.actor); Lease(m, c.actor, clock()); }
            return m;
        }
        int OperateProducer(TransactionState s, TransactionCommand c)
        {
            var m = Producer(s, c); var p = m.progress;
            Require(double.IsFinite(c.duration) && c.duration > 0, "duration", "Delta không hợp lệ.");
            if (m.item is "milk" or "egg" or "beef")
            {
                Require(p.feed > 0, "feed", "Hãy đặt "+Definitions.Item(FeedItem(m.item)).label+" vào vùng Cho ăn.");
                return 1;
            }
            Require(p.phase < 2, "phase", p.phase == 2 ? "Cây đang lớn." : "Cây đã chín; hãy sang vùng Lấy hàng.");
            p.action += (float)c.duration;
            if (p.action >= 1) { p.action = 0; if (p.phase == 0) p.phase = 1; else { p.phase = 2; p.remaining = ProgressionTracker.FarmGrowDuration(s); } }
            return 1;
        }
        int RestockProducer(TransactionState s, TransactionCommand c)
        {
            var m=Producer(s,c);var p=m.progress;
            Require(m.item is "milk" or "egg" or "beef"&&p.herd<ProductionStation.MaximumHerd&&p.breeding==0,"restock","Đàn đã đủ hoặc đang tái đàn.");
            Require(p.feed>0,"feed","Cần thức ăn để bắt đầu tái đàn.");
            p.feed--;p.breeding=60;m.workCount++;if(c.actor=="player")m.playerWorkCount++;return 1;
        }
        int TickProducer(TransactionState s, TransactionCommand c)
        {
            Require(c.actor == "simulation" && double.IsFinite(c.duration) && c.duration > 0, "authority", "Tick không hợp lệ.");
            var m = Producer(s, c, false); var p = m.progress;
            if (m.item is not ("milk" or "egg" or "beef"))
            { if (p.phase == 2) { p.remaining = Math.Max(0, p.remaining - (float)c.duration); if (p.remaining == 0) p.phase = 3; } return 1; }
            if (p.breeding > 0) { p.breeding = Math.Max(0, p.breeding - (float)c.duration); if (p.breeding == 0) p.herd++; }
            if (p.herd > 0 && p.feed > 0) { p.cycle = Math.Max(0, p.cycle - (float)c.duration * (clock() < m.operatorUntil ? 2 : 1)); p.remaining = p.cycle; }
            return 1;
        }
        int HarvestProducer(TransactionState s, TransactionCommand c)
        {
            var m = Producer(s, c); var p = m.progress; var carrier = Owner(s, c.destination); WriteAccess(carrier, c.actor);
            Require(carrier.actor == c.actor && carrier.kind is OwnerKind.Player or OwnerKind.Worker, "authority", "Chỉ lấy vào giỏ người thao tác.");
            Require(double.IsFinite(c.duration) && c.duration > 0 && Free(s, carrier, m.item) > 0, "capacity", "Giỏ đầy hoặc đang mang loại khác.");
            bool animal = m.item is "milk" or "egg" or "beef";
            Require(animal ? p.herd > 0 && p.feed > 0 && p.cycle == 0 : p.phase == 3, "phase", "Chưa đến lúc thu hoạch.");
            p.action += (float)c.duration;
            if (p.action < 1) return 0;
            // Một lần lấy thịt luôn tiêu thụ đúng một con và tạo một đơn vị thịt.
            int count = 1;
            AddStack(s, "harvest:" + c.effectId, carrier.id, m.item, count);
            p.action = 0; p.produced += count; m.workCount++;if(c.actor=="player")m.playerWorkCount++;
            if (animal) { if (m.item == "beef") p.herd--; p.feed--; p.cycle = m.item == "beef" ? 45 : 30; } else p.phase = 0;
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
            Player(s, c.actor); var m = Machine(s, c); var p = m.progress;
            Require(p.broken && double.IsFinite(c.duration) && c.duration > 0, "repair", "Máy chưa cần sửa.");
            Lease(m, c.actor, clock());
            if (!p.repairPaid) { Require(s.money >= p.repairFee, "funds", "Không đủ tiền sửa máy."); s.money -= p.repairFee; p.repairPaid = true; }
            p.repairRemaining = Math.Max(0, p.repairRemaining - (float)c.duration);
            if (p.repairRemaining == 0) { p.broken = false; p.repairPaid = false; } return 1;
        }
        static int UpgradeCrew(TransactionState s, TransactionCommand c)
        {
            Player(s, c.actor); var crew = s.crews.Find(x => x.id == c.target);
            Require(crew != null && c.secondary is "speed" or "carry" or "count", "crew", "Không có nâng cấp đội.");
            Require(c.secondary != "carry" || crew.role != "Cashier", "crew", "Đội thu ngân không nâng sức mang.");
            int level = c.secondary == "speed" ? crew.speedLevel : c.secondary == "carry" ? crew.carryLevel : crew.count;
            int cost = checked(150 * level * level * (crew.area is "farm" or "farm_shop" ? 1 : 3));
            Require(level < 3 && s.money >= cost, "funds", "Không đủ tiền hoặc đội đã đạt cấp tối đa.");
            s.money -= cost; if (c.secondary == "speed") crew.speedLevel++; else if (c.secondary == "carry") crew.carryLevel++; else crew.count++;
            RefreshProgression(s); return cost;
        }
        static void RefreshProgression(TransactionState s)
        {
            foreach (var owner in s.owners)
            {
                if (owner.kind == OwnerKind.Player) owner.capacity = ProgressionTracker.AxisCapacity(s, "player", 6);
                if (owner.kind == OwnerKind.Worker && !string.IsNullOrEmpty(owner.worker?.upgrade))
                { var crew = s.crews.Find(x => x.id == owner.worker.upgrade); if (crew != null) owner.capacity = crew.carryLevel == 3 ? 16 : crew.carryLevel == 2 ? 10 : 6; }
                var station = s.stations.Find(x => x.id == owner.id);
                if (station?.kind == "producer" && station.area == "farm" && station.item is "carrot" or "tomato" or "wheat")
                    owner.capacity = ProgressionTracker.AxisCapacity(s, "farm", 24);
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
        internal static TransactionState NormalizeState(TransactionState s)
        {
            Require(s.schemaVersion is 1 or 2, "schema", "Version transaction chưa hỗ trợ.");
            // Save cũ có receipt Collect nhưng chưa có số liệu tiền thực thu.
            var collected=s.outbox.Where(x=>x.kind==nameof(TransactionKind.CollectPayment)).Select(x=>x.receiptId).ToHashSet();
            s.cashCollected=Math.Max(s.cashCollected,s.receipts.Where(x=>collected.Contains(x.id)).Sum(x=>x.amount));
            foreach(var owner in s.owners.Where(x=>x.kind==OwnerKind.Storage))
            {
                var station=s.stations.Find(x=>x.kind=="storage"&&(x.output==owner.id||x.id==owner.id));
                if(station!=null&&!string.IsNullOrEmpty(station.area))owner.location=station.area;
                foreach(var stack in s.stacks.Where(x=>x.owner==owner.id))stack.location=StackLocation(owner,stack.item);
            }
            s.schemaVersion=2;
            RefreshMachinePhases(s);
            return s;
        }
        static void RefreshMachinePhases(TransactionState s)
        {
            foreach(var machine in s.stations.Where(x=>x.kind=="machine"))
            {
                if(machine.running){machine.machinePhase=MachinePhase.Operating;continue;}
                var recipe=Definitions.Recipe(machine.definitionId);
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
            Require(s.schemaVersion == 2 && s.catalogVersion == Definitions.Version && s.revision >= 0 && s.money >= 0 && s.revenue >= 0 && s.cashCollected>=0 && double.IsFinite(s.simulationTime), "schema", "Version hoặc tiền không hợp lệ.");
            Unique(s.owners.Select(x => x.id)); Unique(s.stacks.Select(x => x.id)); Unique(s.reservations.Select(x => x.id));
            Unique(s.orders.Select(x => x.id)); Unique(s.payments.Select(x => x.id)); Unique(s.purchases.Select(x => x.id)); Unique(s.crews.Select(x => x.id)); Unique(s.stations.Select(x => x.id));
            Unique(s.receipts.Select(x => x.id)); Unique(s.receipts.Select(x => x.key)); Unique(s.receipts.Select(x => x.effectId)); Unique(s.outbox.Select(x => x.id)); Unique(s.unlocked);
            Unique(s.jobIds); Unique(s.consumers.Select(x => x.id)); Unique(s.receipts.Select(x => x.revision.ToString()));
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
                    Require(m.kind is "producer" or "storage" or "shelf" or "counter" or "table" or "purchase" or "conveyor", "station", "Station kind sai: " + m.id);
                    if (m.kind == "producer") Require(Definitions.Item(m.item) != null && m.progress.phase is >= 0 and <= 3 && m.progress.herd is >= 0 and <= 3 && m.progress.feed is >= 0 and <= 3 && float.IsFinite(m.progress.remaining) && m.progress.remaining >= 0 && float.IsFinite(m.progress.action) && m.progress.action >= 0 && float.IsFinite(m.progress.cycle) && m.progress.cycle >= 0 && float.IsFinite(m.progress.breeding) && m.progress.breeding >= 0, "station", "Producer state sai: " + m.id);
                    continue;
                }
                Require(recipe != null && m.definitionVersion == recipe.version, "station", "Recipe version sai: " + m.id);
                Require(Enum.IsDefined(typeof(MachinePhase),m.machinePhase)&&
                    (m.running?m.machinePhase==MachinePhase.Operating:m.machinePhase!=MachinePhase.Operating),"machine-phase","Machine phase không khớp job.");
                if (m.running) Require(s.reservations.Any(x => x.id == m.reservationId && x.status == ReservationStatus.Active && x.destination == m.output && x.item == recipe.output && x.quantity == recipe.yield), "station", "Job mất reservation.");
                if(m.running&&!string.IsNullOrEmpty(m.escrow))Require(Owner(s,m.escrow).kind==OwnerKind.Escrow&&recipe.inputs.All(i=>s.stacks.Where(x=>x.owner==m.escrow&&x.item==i.id).Sum(x=>x.quantity)==i.count),"escrow","Job thiếu nguyên liệu đang xử lý.");
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
