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
        int incomingTotal;
        int capacity; bool singleItem;
        internal RuntimeTransactions Authority { get; private set; }
        public string OwnerId { get; private set; }
        public int Capacity { get => capacity; set { if(Authority!=null && value!=capacity)throw new InvalidOperationException("Capacity chỉ được cập nhật qua transaction."); capacity=value; } }
        public bool SingleItem { get => singleItem; set { if(Authority!=null && value!=singleItem)throw new InvalidOperationException("Carry mode chỉ được cập nhật qua transaction."); singleItem=value; } }
        public int Total { get; private set; }
        public int Revision { get; private set; }
        public int Free => Math.Max(0, Capacity - Total - incomingTotal);
        public Inventory(int capacity, bool singleItem = false) { Capacity = Math.Max(0, capacity); SingleItem = singleItem; }
        internal List<ItemAmount> Limits { get { var result=new List<ItemAmount>(); foreach(var pair in limits)result.Add(new(pair.Key,pair.Value));return result; } }
        internal void Bind(RuntimeTransactions authority,string owner) { Authority=authority;OwnerId=owner; }
        internal void Unbind() { Authority=null;OwnerId=null; }
        void RequirePrimitive() { if(Authority!=null)throw new InvalidOperationException("Inventory có owner chỉ được cập nhật qua transaction API."); }
        internal void Project(TransactionState state)
        {
            var owner=state.owners.Find(x=>x.id==OwnerId);
            capacity=owner.capacity;singleItem=owner.singleItem;amounts.Clear();limits.Clear();reserved.Clear();incoming.Clear();incomingTotal=0;Total=0;
            foreach(var stack in state.stacks)if(stack.owner==OwnerId){amounts[stack.item]=Count(stack.item)+stack.quantity;Total+=stack.quantity;}
            foreach(var limit in owner.limits)limits[limit.id]=limit.count;
            foreach(var reservation in state.reservations)if(reservation.status==ReservationStatus.Active)
            {
                if(reservation.source==OwnerId)reserved[reservation.item]=Reserved(reservation.item)+reservation.quantity;
                if(reservation.destination==OwnerId||reservation.relay==OwnerId){incoming[reservation.item]=ReservedSpace(reservation.item)+reservation.quantity;incomingTotal+=reservation.quantity;}
            }
            Revision=unchecked((int)state.revision);
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

    public sealed class Economy
    {
        RuntimeTransactions authority;
        int money,revenue,transactions,pendingCash;
        readonly HashSet<string> unlocked = new();
        public int Money { get => authority?.View.money ?? money; private set => money=value; }
        public int Revenue { get => authority?.View.revenue ?? revenue; private set => revenue=value; }
        public int Transactions { get => authority==null?transactions:authority.View.legacyTransactions+authority.View.payments.Count; private set => transactions=value; }
        public int PendingCash { get { if(authority==null)return pendingCash;int total=0;foreach(var p in authority.View.payments)if(!p.collected)total+=p.amount;foreach(var cash in authority.View.legacyCash)total+=cash.amount;return total; } private set => pendingCash=value; }
        public HashSet<string> Unlocked => authority==null?unlocked:new(authority.View.unlocked);
        internal void Bind(RuntimeTransactions runtime) => authority=runtime;
        internal void Unbind() => authority=null;
        void RequirePrimitive() { if(authority!=null)throw new InvalidOperationException("Economy chỉ được cập nhật qua transaction API."); }
        readonly HashSet<long> receipts = new();
        readonly Dictionary<long, LossRecord> losses = new();
        readonly HashSet<long> failedOrders = new();
        public List<long> ReceiptIds { get { if(authority==null)return new(receipts);var result=new List<long>(authority.View.legacyPaid);foreach(var p in authority.View.payments){long id=long.Parse(p.order.Substring(6));if(!result.Contains(id))result.Add(id);}return result; } }
        public List<LossRecord> Losses
        {
            get
            {
                var result = new List<LossRecord>();
                if(authority!=null)
                {
                    foreach(var loss in authority.View.legacyLosses)result.Add(TransactionCore.Copy(loss));
                    foreach(var order in authority.View.orders)if(order.status==OrderStatus.Failed&&!result.Exists(x=>x.receipt==long.Parse(order.id.Substring(6))))
                    {
                        var goods=RuntimeTransactions.Items(authority.View,order.customer);
                        if(goods.Exists(x=>x.count>0))result.Add(new LossRecord{receipt=long.Parse(order.id.Substring(6)),goods=goods});
                    }
                    return result;
                }
                foreach (var loss in losses.Values)
                {
                    var copy = new LossRecord { receipt = loss.receipt };
                    foreach (var good in loss.goods) copy.goods.Add(new(good.id, good.count));
                    result.Add(copy);
                }
                return result;
            }
        }
        public int LossCount => authority==null?losses.Count:Losses.Count;
        public int LostItems
        {
            get { int count = 0; foreach (var loss in Losses) foreach (var good in loss.goods) count += good.count; return count; }
        }
        public Economy(int money = 0) { Money = Math.Max(0, money); }
        public bool Has(string id) => string.IsNullOrEmpty(id) || Unlocked.Contains(id);
        public bool IsPaid(long receipt) => authority==null?receipts.Contains(receipt):ReceiptIds.Contains(receipt);
        public bool IsLost(long receipt) => authority==null?failedOrders.Contains(receipt):
            authority.View.legacyLosses.Exists(x=>x.receipt==receipt)||authority.View.orders.Exists(x=>x.status==OrderStatus.Failed&&long.Parse(x.id.Substring(6))==receipt);
        public bool TrySpend(int amount)
        {
            RequirePrimitive();
            if (amount < 0 || Money < amount) return false;
            Money -= amount; return true;
        }
        public bool Unlock(string id) { RequirePrimitive();return !string.IsNullOrEmpty(id) && Unlocked.Add(id); }
        public bool RecordPayment(long receiptId, int amount)
        {
            RequirePrimitive();
            if (receiptId <= 0 || amount <= 0 || receipts.Contains(receiptId) || failedOrders.Contains(receiptId)) return false;
            receipts.Add(receiptId); PendingCash += amount; Revenue += amount; Transactions++; return true;
        }
        public bool RecordLoss(long receiptId, Inventory goods)
        {
            RequirePrimitive();
            if (receiptId <= 0 || goods == null || receipts.Contains(receiptId) || failedOrders.Contains(receiptId)) return false;
            failedOrders.Add(receiptId);
            if(goods.Total>0)losses.Add(receiptId, new LossRecord { receipt = receiptId, goods = goods.Snapshot() });
            return true;
        }
        public int CollectCash(int amount)
        {
            RequirePrimitive();
            int collected = Math.Min(Math.Max(0, amount), PendingCash);
            PendingCash -= collected; Money += collected; return collected;
        }
        public void Restore(int money, int revenue, int transactions, IEnumerable<string> unlocked, int pendingCash = 0,
            IEnumerable<long> paidReceipts = null, IEnumerable<LossRecord> recordedLosses = null)
        {
            RequirePrimitive();
            Money = Math.Max(0, money); Revenue = Math.Max(0, revenue); Transactions = Math.Max(0, transactions);
            PendingCash = Math.Max(0, pendingCash);
            Unlocked.Clear(); receipts.Clear(); losses.Clear();failedOrders.Clear();
            if (unlocked != null) foreach (string id in unlocked) if (!string.IsNullOrEmpty(id)) Unlocked.Add(id);
            if (paidReceipts != null) foreach (long receipt in paidReceipts) receipts.Add(receipt);
            if (recordedLosses != null) foreach (var loss in recordedLosses)
            {
                if (loss == null || receipts.Contains(loss.receipt) || failedOrders.Contains(loss.receipt)) continue;
                var goods = new Inventory(int.MaxValue); goods.Restore(loss.goods); RecordLoss(loss.receipt, goods);
            }
        }
    }
}
