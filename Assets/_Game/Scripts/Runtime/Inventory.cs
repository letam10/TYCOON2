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
        public int Capacity { get; set; }
        public bool SingleItem { get; set; }
        public int Total { get; private set; }
        public int Revision { get; private set; }
        public int Free => Math.Max(0, Capacity - Total - incomingTotal);
        public Inventory(int capacity, bool singleItem = false) { Capacity = Math.Max(0, capacity); SingleItem = singleItem; }
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
        public void SetLimit(string id, int limit) { if (!string.IsNullOrEmpty(id)) limits[id] = Math.Max(0, limit); }
        public bool TryReserve(string id, int count)
        {
            if (count <= 0 || Available(id) < count) return false;
            reserved[id] = Reserved(id) + count; return true;
        }
        public void Release(string id, int count)
        {
            if (id != null && count > 0) reserved[id] = Math.Max(0, Reserved(id) - count);
        }
        public bool TryReserveSpace(string id, int count)
        {
            if (count <= 0 || FreeFor(id) < count) return false;
            incoming[id] = ReservedSpace(id) + count; incomingTotal += count; return true;
        }
        public void ReleaseSpace(string id, int count)
        {
            if (id == null || count <= 0) return;
            int released = Math.Min(count, ReservedSpace(id));
            incoming[id] = ReservedSpace(id) - released; incomingTotal -= released;
        }
        public void ClearReservations() { reserved.Clear(); incoming.Clear(); incomingTotal = 0; }
        public bool TryAdd(string id, int count)
        {
            if (count <= 0 || FreeFor(id) < count) return false;
            amounts[id] = Count(id) + count; Total += count; Revision++; return true;
        }
        public bool TryRemove(string id, int count)
        {
            if (count <= 0 || Available(id) < count) return false;
            amounts[id] = Count(id) - count; Total -= count; Revision++; return true;
        }
        public static int Transfer(Inventory from, Inventory to, string id, int count)
        {
            if (from == null || to == null || from == to || count <= 0) return 0;
            int amount = Math.Min(count, Math.Min(from.Available(id), to.FreeFor(id)));
            if (amount <= 0 || !to.TryAdd(id, amount)) return 0;
            from.TryRemove(id, amount); return amount;
        }
        public static int TransferReserved(Inventory from, Inventory to, string id, int count)
        {
            if (from == null || to == null || from == to || count <= 0) return 0;
            int amount = Math.Min(count, Math.Min(from.Reserved(id), to.FreeFor(id)));
            if (amount <= 0 || !to.TryAdd(id, amount)) return 0;
            from.Release(id, amount); from.TryRemove(id, amount); return amount;
        }
        public static int TransferIntoReservedSpace(Inventory from, Inventory to, string id, int count)
        {
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
            amounts.Clear(); ClearReservations(); Total = 0;
            if (values != null) foreach (var value in values)
            {
                if (value == null || value.count <= 0 || string.IsNullOrEmpty(value.id) || Definitions.Item(value.id) == null) continue;
                if (SingleItem && Total > Count(value.id)) throw new ArgumentException("Giỏ hàng chỉ được chứa một loại sản phẩm.");
                amounts[value.id] = Count(value.id) + value.count; Total += value.count;
            }
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
        public int Money { get; private set; }
        public int Revenue { get; private set; }
        public int Transactions { get; private set; }
        public int PendingCash { get; private set; }
        public readonly HashSet<string> Unlocked = new();
        readonly HashSet<long> receipts = new();
        readonly Dictionary<long, LossRecord> losses = new();
        public List<long> ReceiptIds => new(receipts);
        public List<LossRecord> Losses
        {
            get
            {
                var result = new List<LossRecord>();
                foreach (var loss in losses.Values)
                {
                    var copy = new LossRecord { receipt = loss.receipt };
                    foreach (var good in loss.goods) copy.goods.Add(new(good.id, good.count));
                    result.Add(copy);
                }
                return result;
            }
        }
        public int LossCount => losses.Count;
        public int LostItems
        {
            get { int count = 0; foreach (var loss in losses.Values) foreach (var good in loss.goods) count += good.count; return count; }
        }
        public Economy(int money = 0) { Money = Math.Max(0, money); }
        public bool Has(string id) => string.IsNullOrEmpty(id) || Unlocked.Contains(id);
        public bool IsPaid(long receipt) => receipts.Contains(receipt);
        public bool IsLost(long receipt) => losses.ContainsKey(receipt);
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Money < amount) return false;
            Money -= amount; return true;
        }
        public bool Unlock(string id) => !string.IsNullOrEmpty(id) && Unlocked.Add(id);
        public bool RecordPayment(long receiptId, int amount)
        {
            if (receiptId <= 0 || amount <= 0 || receipts.Contains(receiptId) || losses.ContainsKey(receiptId)) return false;
            receipts.Add(receiptId); PendingCash += amount; Revenue += amount; Transactions++; return true;
        }
        public bool RecordLoss(long receiptId, Inventory goods)
        {
            if (receiptId <= 0 || goods == null || receipts.Contains(receiptId) || losses.ContainsKey(receiptId)) return false;
            losses.Add(receiptId, new LossRecord { receipt = receiptId, goods = goods.Snapshot() }); return true;
        }
        public int CollectCash(int amount)
        {
            int collected = Math.Min(Math.Max(0, amount), PendingCash);
            PendingCash -= collected; Money += collected; return collected;
        }
        public void Restore(int money, int revenue, int transactions, IEnumerable<string> unlocked, int pendingCash = 0,
            IEnumerable<long> paidReceipts = null, IEnumerable<LossRecord> recordedLosses = null)
        {
            Money = Math.Max(0, money); Revenue = Math.Max(0, revenue); Transactions = Math.Max(0, transactions);
            PendingCash = Math.Max(0, pendingCash);
            Unlocked.Clear(); receipts.Clear(); losses.Clear();
            if (unlocked != null) foreach (string id in unlocked) if (!string.IsNullOrEmpty(id)) Unlocked.Add(id);
            if (paidReceipts != null) foreach (long receipt in paidReceipts) receipts.Add(receipt);
            if (recordedLosses != null) foreach (var loss in recordedLosses)
            {
                if (loss == null || receipts.Contains(loss.receipt) || losses.ContainsKey(loss.receipt)) continue;
                var goods = new Inventory(int.MaxValue); goods.Restore(loss.goods); RecordLoss(loss.receipt, goods);
            }
        }
    }
}
