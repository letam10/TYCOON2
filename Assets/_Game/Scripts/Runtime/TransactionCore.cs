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
        TransactionState state;
        public CoreLifecycle Lifecycle { get; private set; }
        public Action<CommitBoundary> Fault { get; set; }
        public TransactionCore(TransactionState seed, ITransactionStore store = null, Func<double> clock = null)
        {
            this.store = store;
            this.clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d);
            state = Copy(store?.Read() ?? seed ?? throw new ArgumentNullException(nameof(seed)));
            Validate(state);
            if (store != null && store.Read() == null) store.Write(state);
        }
        public long Revision { get { lock (gate) return state.revision; } }
        public TransactionState Snapshot() { lock (gate) return Copy(state); }
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
                var receipt = state.receipts.Find(x => x.key == c.key);
                if (receipt != null)
                { Require(receipt.fingerprint == fingerprint, "key-conflict", "Key đã dùng với payload khác."); return Copy(receipt); }
                receipt = state.receipts.Find(x => x.effectId == c.effectId);
                if (receipt != null)
                { Require(receipt.effectFingerprint == effectFingerprint, "effect-conflict", "Effect đã dùng với payload khác."); return Copy(receipt); }
                Require(c.expectedRevision == state.revision, "stale", "State version đã thay đổi.");
                var draft = Copy(state);
                Expire(draft, clock());
                int amount = Apply(draft, c);
                draft.revision = checked(state.revision + 1);
                receipt = new TransactionReceipt {
                    id = "receipt:" + c.key, key = c.key, effectId = c.effectId,
                    fingerprint = fingerprint, effectFingerprint = effectFingerprint,
                    eventId = "event:" + c.key, revision = draft.revision, amount = amount
                };
                draft.receipts.Add(receipt);
                draft.outbox.Add(new TransactionEvent { id = receipt.eventId, receiptId = receipt.id,
                    effectId = c.effectId, revision = draft.revision, kind = c.kind.ToString() });
                Validate(draft);
                Fault?.Invoke(CommitBoundary.Prepared);
                try { store?.Write(draft); }
                catch
                {
                    // Có thể lỗi sau replace nhưng trước trả lời: đọc lại receipt trước khi cho retry.
                    try { var recovered = store.Read(); Validate(recovered); state = Copy(recovered); }
                    catch { Lifecycle = CoreLifecycle.RecoveryFailed; }
                    throw;
                }
                state = draft;
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
            var stock = s.stacks.Where(x => x.owner == owner.id).ToArray();
            var held = s.reservations.Where(x => x.status == ReservationStatus.Active && x.destination == owner.id).ToArray();
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
            s.stacks.Add(new ItemStackState { id = id, owner = owner, location = location.location, item = item, quantity = count });
        }
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
                Require(r.status == ReservationStatus.Active && r.holder == c.actor && r.source == c.source && r.destination == c.destination && r.item == c.item && r.quantity == c.quantity, "reservation", "Reservation không khớp hoặc hết hạn.");
                allocation = r.allocations; r.status = ReservationStatus.Used;
            }
            else allocation = Allocate(s, c.source, c.item, c.quantity);
            Require(Free(s, destination, c.item) >= c.quantity, "capacity", "Đích không đủ chỗ.");
            Consume(s, allocation); AddStack(s, "stack:" + c.effectId, destination.id, c.item, c.quantity); return c.quantity;
        }
        int Reserve(TransactionState s, TransactionCommand c)
        {
            Require(c.quantity > 0 && c.source != c.destination && double.IsFinite(c.expiresAt) && c.expiresAt > clock(), "reservation", "Reservation hoặc thời hạn không hợp lệ.");
            Require(!s.reservations.Any(x => x.id == c.target), "duplicate", "Reservation ID đã tồn tại.");
            var source = Owner(s, c.source); var destination = Owner(s, c.destination);
            WriteAccess(source, c.actor); WriteAccess(destination, c.actor);
            Require(source.kind != OwnerKind.Customer, "escrow", "Không reserve hàng đã giao cho khách.");
            Require(Free(s, destination, c.item) >= c.quantity, "capacity", "Đích không đủ chỗ.");
            s.reservations.Add(new ReservationState { id = c.target, holder = c.actor, source = c.source, destination = c.destination, item = c.item,
                quantity = c.quantity, expiresAt = c.expiresAt, allocations = Allocate(s, source.id, c.item, c.quantity) }); return c.quantity;
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
            Require(Owner(s, c.destination).kind == OwnerKind.Customer && Owner(s, c.source).kind == OwnerKind.Counter, "owner", "Sai owner đơn.");
            Require(c.lines.Count > 0 && c.lines.Select(x => x.id).Distinct().Count() == c.lines.Count && c.lines.All(x => Definitions.Item(x.id) != null && x.requested > 0 && x.delivered == 0 && x.unitPrice >= 0), "lines", "Dòng đơn không hợp lệ.");
            s.orders.Add(new OrderRuntimeState { id = c.target, customer = c.destination, counter = c.source, deadline = c.expiresAt, lines = c.lines.Select(Copy).ToList() }); return 1;
        }
        int Deliver(TransactionState s, TransactionCommand c)
        {
            var o = Order(s, c); var line = o.lines.Find(x => x.id == c.item);
            Require(line != null && c.quantity > 0 && c.quantity <= line.Remaining, "delivery", "Giao quá nhu cầu hoặc sai hàng.");
            var move = Copy(c); move.kind = TransactionKind.Transfer; move.destination = o.customer;
            Move(s, move, true); line.delivered += c.quantity; return c.quantity;
        }
        int CompleteOrder(TransactionState s, TransactionCommand c)
        {
            var o = Order(s, c, false); if (o.status == OrderStatus.Complete) return 0;
            Require(o.status == OrderStatus.Open && clock() < o.deadline && o.lines.All(x => x.Remaining == 0), "order-incomplete", "Chưa giao đủ hoặc đã hết hạn.");
            o.status = OrderStatus.Complete; return 1;
        }
        int FailOrder(TransactionState s, TransactionCommand c)
        {
            var o = Order(s, c, false); if (o.status == OrderStatus.Failed) return 0;
            Require(o.status == OrderStatus.Open, "order-closed", "Đơn đã hoàn tất.");
            o.status = OrderStatus.Failed;
            foreach (var r in s.reservations.Where(x => x.destination == o.customer && x.status == ReservationStatus.Active)) r.status = ReservationStatus.Released;
            return o.lines.Sum(x => checked(x.delivered * x.unitPrice));
        }
        int CreatePayment(TransactionState s, TransactionCommand c)
        {
            var o = Order(s, c, false); Require(o.status == OrderStatus.Complete, "order-incomplete", "Đơn chưa thành công.");
            if (s.payments.Any(x => x.order == o.id)) return 0;
            int amount = o.lines.Sum(x => checked(x.requested * x.unitPrice));
            s.payments.Add(new PaymentState { id = "payment:" + o.id, order = o.id, counter = o.counter, amount = amount });
            s.revenue = checked(s.revenue + amount); return amount;
        }
        static int Collect(TransactionState s, TransactionCommand c)
        {
            Player(s, c.actor); var payment = s.payments.Find(x => x.id == c.target);
            Require(payment != null, "payment", "Payment không tồn tại."); WriteAccess(Owner(s, payment.counter), c.actor);
            if (payment.collected) return 0;
            s.money = checked(s.money + payment.amount); payment.collected = true; return payment.amount;
        }
        static int Contribute(TransactionState s, TransactionCommand c)
        {
            Player(s, c.actor); var definition = Definitions.Upgrade(c.target);
            Require(definition != null && definition.kind != "legacy" && c.quantity > 0 && !s.unlocked.Contains(c.target), "purchase", "Purchase không khả dụng.");
            Require(string.IsNullOrEmpty(definition.requirement) || s.unlocked.Contains(definition.requirement), "requirement", "Thiếu điều kiện mở khóa.");
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
            return 1;
        }
        static StationRuntimeState Machine(TransactionState s, TransactionCommand c)
        {
            var m = s.stations.Find(x => x.id == c.target); Require(m != null, "station", "Station không tồn tại.");
            WriteAccess(Owner(s, m.input), c.actor); WriteAccess(Owner(s, m.output), c.actor); return m;
        }
        int StartMachine(TransactionState s, TransactionCommand c)
        {
            var m = Machine(s, c); var recipe = Array.Find(Definitions.Recipes, x => x.id == m.definitionId);
            Require(recipe != null && !m.running, "machine", "Máy đang chạy hoặc sai recipe.");
            Require(!string.IsNullOrWhiteSpace(c.secondary), "job", "Thiếu job ID.");
            Require(!s.jobIds.Contains(m.id + ":" + c.secondary), "job", "Job ID đã dùng.");
            Require(Free(s, Owner(s, m.output), recipe.output) >= recipe.yield, "capacity", "Đầu ra đầy.");
            foreach (var input in recipe.inputs) Consume(s, Allocate(s, m.input, input.id, input.count));
            m.jobId = c.secondary; m.reservationId = "output:" + m.id + ":" + c.secondary; m.operatorId = c.actor;
            s.jobIds.Add(m.id + ":" + c.secondary);
            s.reservations.Add(new ReservationState { id = m.reservationId, holder = c.actor, destination = m.output, item = recipe.output, quantity = recipe.yield, expiresAt = double.MaxValue });
            m.running = true; m.remaining = recipe.seconds; return 1;
        }
        static int AdvanceMachine(TransactionState s, TransactionCommand c)
        {
            var m = Machine(s, c); Require(m.running && m.jobId == c.secondary && m.operatorId == c.actor, "lease", "Sai job hoặc operator.");
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
            AddStack(s, "job-output:" + m.id + ":" + m.jobId, m.output, recipe.output, recipe.yield);
            m.running = false; m.batches++; m.workCount++; m.remaining = 0; return recipe.yield;
        }
        public static void Validate(TransactionState s)
        {
            if (s == null) throw new InvalidDataException("Thiếu transaction state.");
            Require(s.schemaVersion == 1 && s.catalogVersion == Definitions.Version && s.revision >= 0 && s.money >= 0 && s.revenue >= 0, "schema", "Version hoặc tiền không hợp lệ.");
            Unique(s.owners.Select(x => x.id)); Unique(s.stacks.Select(x => x.id)); Unique(s.reservations.Select(x => x.id));
            Unique(s.orders.Select(x => x.id)); Unique(s.payments.Select(x => x.id)); Unique(s.purchases.Select(x => x.id)); Unique(s.crews.Select(x => x.id)); Unique(s.stations.Select(x => x.id));
            Unique(s.receipts.Select(x => x.id)); Unique(s.receipts.Select(x => x.key)); Unique(s.receipts.Select(x => x.effectId)); Unique(s.outbox.Select(x => x.id)); Unique(s.unlocked);
            Unique(s.jobIds); Unique(s.consumers.Select(x => x.id)); Unique(s.receipts.Select(x => x.revision.ToString()));
            foreach (var consumer in s.consumers) Require(consumer.appliedEvents == s.outbox.Count(x => x.consumers.Contains(consumer.id)), "consumer", "Consumer projection lệch dedup.");
            foreach (var owner in s.owners)
            {
                Require(!string.IsNullOrWhiteSpace(owner.actor) && !string.IsNullOrWhiteSpace(owner.location) && owner.capacity >= 0 && Enum.IsDefined(typeof(OwnerKind), owner.kind), "owner", "Owner không hợp lệ.");
                Unique(owner.limits.Select(x => x.id)); Require(owner.limits.All(x => Definitions.Item(x.id) != null && x.count >= 0), "limits", "Giới hạn sai.");
                var items = s.stacks.Where(x => x.owner == owner.id).ToArray();
                var reserved = s.reservations.Where(x => x.status == ReservationStatus.Active && x.destination == owner.id).ToArray();
                Require((long)items.Sum(x => x.quantity) + reserved.Sum(x => x.quantity) <= owner.capacity, "capacity", "Owner vượt capacity.");
                var types = items.Select(x => x.item).Concat(reserved.Select(x => x.item)).Distinct().ToArray();
                Require(!owner.singleItem || types.Length <= 1, "single-item", "Người mang có nhiều loại hàng.");
                foreach (var limit in owner.limits) Require((long)items.Where(x => x.item == limit.id).Sum(x => x.quantity) + reserved.Where(x => x.item == limit.id).Sum(x => x.quantity) <= limit.count, "limits", "Vượt item limit.");
            }
            foreach (var stack in s.stacks)
            {
                var owner = Owner(s, stack.owner);
                Require(stack.quantity > 0 && Definitions.Item(stack.item) != null && stack.definitionVersion == 1 && stack.location == owner.location && Reserved(s, stack.id) <= stack.quantity, "stack", "Stack không hợp lệ.");
            }
            foreach (var r in s.reservations)
            {
                Require(!string.IsNullOrWhiteSpace(r.holder) && r.quantity > 0 && Definitions.Item(r.item) != null && double.IsFinite(r.expiresAt) && Enum.IsDefined(typeof(ReservationStatus), r.status), "reservation", "Reservation không hợp lệ.");
                Owner(s, r.destination);
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
            foreach (var crew in s.crews) Require(Definitions.Upgrade(crew.id)?.kind == "worker" && s.unlocked.Contains(crew.id) && crew.count is >= 1 and <= 3 && crew.speedLevel is >= 1 and <= 3 && crew.carryLevel is >= 1 and <= 3, "crew", "Crew sai.");
            foreach (var m in s.stations)
            {
                Owner(s, m.input); Owner(s, m.output); var recipe = Array.Find(Definitions.Recipes, x => x.id == m.definitionId);
                Require(recipe != null && m.definitionVersion == recipe.version && m.level >= 1 && m.batches >= 0 && m.workCount >= 0 && double.IsFinite(m.remaining) && m.remaining >= 0, "station", "Machine sai.");
                if (m.running) Require(s.reservations.Any(x => x.id == m.reservationId && x.status == ReservationStatus.Active && x.destination == m.output && x.item == recipe.output && x.quantity == recipe.yield), "station", "Job mất reservation.");
            }
            Require(s.receipts.Count == s.outbox.Count && s.receipts.Count == s.revision, "journal", "Mutation thiếu receipt hoặc outbox.");
            foreach (var r in s.receipts) Require(r.revision > 0 && r.revision <= s.revision && !string.IsNullOrEmpty(r.fingerprint) && !string.IsNullOrEmpty(r.effectFingerprint) && s.outbox.Any(e => e.id == r.eventId && e.receiptId == r.id && e.effectId == r.effectId && e.revision == r.revision), "journal", "Receipt không khớp event.");
        }
        static void Unique(IEnumerable<string> ids)
        { var set = new HashSet<string>(); foreach (var id in ids) Require(!string.IsNullOrWhiteSpace(id) && set.Add(id), "duplicate", "ID thiếu hoặc lặp: " + id); }
    }
}
