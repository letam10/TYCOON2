using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class TransactionCore
    {
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
            if(s.schemaVersion>=3&&owner.kind==OwnerKind.Player&&(s.cashInHand>0||s.crates.Any(x=>x.holder==owner.actor)))return 0;
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
        static string FeedItem(string livestock) => Definitions.IsAnimal(livestock)?"animal_feed":null;
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
        static int Move(TransactionState s, TransactionCommand c, bool delivery = false,bool cargo=false)
        {
            Require(c.quantity > 0 && c.source != c.destination, "quantity", "Transfer cần lượng dương và hai owner khác nhau.");
            var source = Owner(s, c.source); var destination = Owner(s, c.destination);
            Require(cargo || source.kind!=OwnerKind.Crate&&destination.kind!=OwnerKind.Crate,"cargo","Thùng hàng chỉ chuyển qua API cargo.");
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
    }
}
