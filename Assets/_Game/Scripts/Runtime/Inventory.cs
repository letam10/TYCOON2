using System;
using System.Collections.Generic;

namespace Tycoon
{
    public sealed class Inventory
    {
        readonly Dictionary<string, int> amounts = new();
        readonly Dictionary<string, int> limits = new();
        readonly Dictionary<string, int> reserved = new();
        readonly Dictionary<string, int> incoming = new();
        readonly Dictionary<string, int> projected = new();
        readonly Dictionary<string, int> projectedIncoming = new();
        int incomingTotal;
        int capacity; bool singleItem;
        internal RuntimeTransactions Authority { get; private set; }
        public string OwnerId { get; private set; }
        public int Capacity { get => capacity; set { if(Authority!=null && value!=capacity)throw new InvalidOperationException("Capacity chỉ được cập nhật qua transaction."); capacity=value; } }
        public bool SingleItem { get => singleItem; set { if(Authority!=null && value!=singleItem)throw new InvalidOperationException("Carry mode chỉ được cập nhật qua transaction."); singleItem=value; } }
        public int Total { get; private set; }
        public int Revision { get; private set; }
        public int ReservedTotal { get { int total=0;foreach(var amount in reserved.Values)total+=amount;return total; } }
        public int IncomingTotal => incomingTotal;
        public int Free => Math.Max(0, Capacity - Total - incomingTotal);
        public Inventory(int capacity, bool singleItem = false) { Capacity = Math.Max(0, capacity); SingleItem = singleItem; }
        internal List<ItemAmount> Limits { get { var result=new List<ItemAmount>(); foreach(var pair in limits)result.Add(new(pair.Key,pair.Value));return result; } }
        internal void Bind(RuntimeTransactions authority,string owner) { Authority=authority;OwnerId=owner; }
        internal void Unbind() { Authority=null;OwnerId=null; }
        void RequirePrimitive() { if(Authority!=null)throw new InvalidOperationException("Inventory có owner chỉ được cập nhật qua transaction API."); }
        internal void Project(TransactionState state)
        {
            var owner = state.owners.Find(x => x.id == OwnerId);
            bool changed = capacity != owner.capacity || singleItem != owner.singleItem;
            capacity = owner.capacity;
            singleItem = owner.singleItem;
            int total = 0;
            foreach (var stack in state.stacks)
                if (stack.owner == OwnerId)
                {
                    AccumulateProjection(projected, stack.item, stack.quantity);
                    total += stack.quantity;
                }
            changed |= ApplyProjection(amounts, projected) || Total != total;
            Total = total;
            foreach (var limit in owner.limits) projected[limit.id] = limit.count;
            changed |= ApplyProjection(limits, projected, true);
            total = 0;
            foreach (var reservation in state.reservations)
                if (reservation.status == ReservationStatus.Active)
                {
                    if (reservation.source == OwnerId)
                        AccumulateProjection(projected, reservation.item, reservation.quantity);
                    if (reservation.destination != OwnerId && reservation.relay != OwnerId) continue;
                    AccumulateProjection(projectedIncoming, reservation.item, reservation.quantity);
                    total += reservation.quantity;
                }
            changed |= ApplyProjection(reserved, projected);
            changed |= ApplyProjection(incoming, projectedIncoming) || incomingTotal != total;
            incomingTotal = total;
            if (changed) Revision++;
        }
        static void AccumulateProjection(Dictionary<string, int> values, string item, int quantity)
        {
            values.TryGetValue(item, out int value);
            values[item] = value + quantity;
        }
        static bool ApplyProjection(Dictionary<string, int> target, Dictionary<string, int> next,
            bool explicitZero = false)
        {
            // So sánh từng SKU; khóa lượng 0 của inventory primitive tương đương không có hàng.
            bool changed = false;
            foreach (var pair in target)
                if ((explicitZero && !next.ContainsKey(pair.Key)) ||
                    pair.Value != (next.TryGetValue(pair.Key, out int value) ? value : 0)) changed = true;
            foreach (var pair in next)
                if ((explicitZero && !target.ContainsKey(pair.Key)) ||
                    pair.Value != (target.TryGetValue(pair.Key, out int value) ? value : 0)) changed = true;
            if (changed)
            {
                target.Clear();
                foreach (var pair in next) target.Add(pair.Key, pair.Value);
            }
            next.Clear();
            return changed;
        }
        public int Count(string id) => id != null && amounts.TryGetValue(id, out int count) ? count : 0;
        public int Reserved(string id) => id != null && reserved.TryGetValue(id, out int count) ? count : 0;
        public int ReservedSpace(string id) => id != null && incoming.TryGetValue(id, out int count) ? count : 0;
        public int Available(string id) => Math.Max(0, Count(id) - Reserved(id));
        public int FreeFor(string id)
        {
            if (string.IsNullOrEmpty(id) || Definitions.Item(id) == null) return 0;
            if (SingleItem && Total + incomingTotal > Count(id) + ReservedSpace(id)) return 0;
            int limit = limits.TryGetValue(id, out int value) ? value : int.MaxValue;
            return Math.Min(Free, Math.Max(0, limit - Count(id) - ReservedSpace(id)));
        }
        public void SetLimit(string id, int limit) { if(Authority!=null && limits.TryGetValue(id,out int old)&&old==limit)return;RequirePrimitive();if (!string.IsNullOrEmpty(id)) limits[id] = Math.Max(0, limit); }
        public bool TryReserve(string id, int count)
        {
            RequirePrimitive();
            if (count <= 0 || Available(id) < count) return false;
            reserved[id] = Reserved(id) + count; return true;
        }
        public void Release(string id, int count)
        {
            RequirePrimitive();
            if (id != null && count > 0) reserved[id] = Math.Max(0, Reserved(id) - count);
        }
        public bool TryReserveSpace(string id, int count)
        {
            RequirePrimitive();
            if (count <= 0 || FreeFor(id) < count) return false;
            incoming[id] = ReservedSpace(id) + count; incomingTotal += count; return true;
        }
        public void ReleaseSpace(string id, int count)
        {
            RequirePrimitive();
            if (id == null || count <= 0) return;
            int released = Math.Min(count, ReservedSpace(id));
            incoming[id] = ReservedSpace(id) - released; incomingTotal -= released;
        }
        public void ClearReservations() { RequirePrimitive();reserved.Clear(); incoming.Clear(); incomingTotal = 0; }
        public bool TryAdd(string id, int count)
        {
            RequirePrimitive();
            if (count <= 0 || FreeFor(id) < count) return false;
            amounts[id] = Count(id) + count; Total += count; Revision++; return true;
        }
        public bool TryRemove(string id, int count)
        {
            RequirePrimitive();
            if (count <= 0 || Available(id) < count) return false;
            amounts[id] = Count(id) - count; Total -= count; Revision++; return true;
        }
        public static int Transfer(Inventory from, Inventory to, string id, int count)
        {
            if (from == null || to == null || from == to || count <= 0) return 0;
            if(from.Authority!=null||to.Authority!=null)
            {
                if(from.Authority==null||from.Authority!=to.Authority)throw new InvalidOperationException("Transfer không được trộn inventory authoritative và primitive.");
                return from.Authority.Transfer(from,to,id,count);
            }
            int amount = Math.Min(count, Math.Min(from.Available(id), to.FreeFor(id)));
            if (amount <= 0 || !to.TryAdd(id, amount)) return 0;
            from.TryRemove(id, amount); return amount;
        }
        public static int TransferReserved(Inventory from, Inventory to, string id, int count)
        {
            from?.RequirePrimitive();to?.RequirePrimitive();
            if (from == null || to == null || from == to || count <= 0) return 0;
            int amount = Math.Min(count, Math.Min(from.Reserved(id), to.FreeFor(id)));
            if (amount <= 0 || !to.TryAdd(id, amount)) return 0;
            from.Release(id, amount); from.TryRemove(id, amount); return amount;
        }
        public static int TransferIntoReservedSpace(Inventory from, Inventory to, string id, int count)
        {
            from?.RequirePrimitive();to?.RequirePrimitive();
            if (from == null || to == null || from == to || count <= 0) return 0;
            int amount = Math.Min(count, Math.Min(from.Available(id), to.ReservedSpace(id)));
            if (amount <= 0) return 0;
            to.ReleaseSpace(id, amount);
            int moved = Transfer(from, to, id, amount);
            if (moved < amount) to.TryReserveSpace(id, amount - moved);
            return moved;
        }
        public bool AddIntoReservedSpace(string id, int count)
        {
            RequirePrimitive();
            if (count <= 0 || ReservedSpace(id) < count) return false;
            ReleaseSpace(id, count);
            if (TryAdd(id, count)) return true;
            TryReserveSpace(id, count); return false;
        }
        public List<ItemAmount> Snapshot()
        {
            var result = new List<ItemAmount>();
            foreach (var item in Definitions.Items) if (Count(item.id) > 0) result.Add(new(item.id, Count(item.id)));
            return result;
        }
        public void Restore(IEnumerable<ItemAmount> values)
        {
            RequirePrimitive();
            var restored=new Dictionary<string,int>();long total=0;
            if (values != null) foreach (var value in values)
            {
                if(value==null||value.count<=0||string.IsNullOrEmpty(value.id)||Definitions.Item(value.id)==null||restored.ContainsKey(value.id))
                    throw new System.IO.InvalidDataException("Danh sách hàng không hợp lệ.");
                restored.Add(value.id,value.count);total+=value.count;
            }
            if(total>int.MaxValue||(SingleItem&&restored.Count>1))throw new System.IO.InvalidDataException("Giỏ hàng không hợp lệ.");
            // Kiểm tra toàn bộ trước khi thay thế để không làm mất hàng khi save bị lỗi.
            amounts.Clear();ClearReservations();Total=(int)total;
            foreach(var value in restored)amounts.Add(value.Key,value.Value);
            // Giữ nguyên lượng hàng trong save khi giới hạn kho đã thay đổi.
            Revision++;
        }
    }

    [Serializable]
    public sealed class LossRecord
    {
        public long receipt;
        public List<ItemAmount> goods = new();
    }

}
