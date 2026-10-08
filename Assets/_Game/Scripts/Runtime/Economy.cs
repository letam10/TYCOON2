using System;
using System.Collections.Generic;

namespace Tycoon
{
    public sealed class Economy
    {
        long cashCollected, cashInSafe;
        public long CashCollected=>authority?.View.cashCollected??cashCollected;
        RuntimeTransactions authority;
        long money,revenue,pendingCash;
        int transactions;
        readonly HashSet<string> unlocked = new();
        readonly HashSet<string> projectedUnlocked = new();
        TransactionState unlockView;
        public long Money { get => CashInHand; private set => money=value; }
        public long CashInHand => authority==null?money:PhysicalCashRules.Hand(authority.View);
        public long CashInSafe => authority?.View.cashInSafe??cashInSafe;
        public void RestoreCash(long hand,long safe) { RequirePrimitive();money=hand;cashInSafe=safe; }
        public long Revenue { get => authority?.View.revenue ?? revenue; private set => revenue=value; }
        public int Transactions { get => authority==null?transactions:authority.View.legacyTransactions+authority.View.payments.Count; private set => transactions=value; }
        public long PendingCash { get { if(authority==null)return pendingCash;long total=0;foreach(var p in authority.View.payments)if(!p.collected)total+=p.amount;foreach(var cash in authority.View.legacyCash)total+=cash.amount;return total; } private set => pendingCash=value; }
        public HashSet<string> Unlocked => authority==null?unlocked:new(authority.View.unlocked);
        internal void Bind(RuntimeTransactions runtime)
        {
            authority = runtime;
            unlockView = null;
        }
        internal void Unbind()
        {
            authority = null;
            unlockView = null;
        }
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
        public Economy(long money = 0) { Money = Math.Max(0, money); }
        public bool Has(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            if (authority == null) return unlocked.Contains(id);
            var current = authority.View;
            if (!ReferenceEquals(unlockView, current))
            {
                // Cache đọc theo projection; không đưa bộ mutable này cho code bên ngoài.
                projectedUnlocked.Clear();
                foreach (string item in current.unlocked) projectedUnlocked.Add(item);
                unlockView = current;
            }
            return projectedUnlocked.Contains(id);
        }
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
        public long CollectCash(long amount)
        {
            RequirePrimitive();
            long collected = Math.Min(Math.Max(0, amount), PendingCash);
            PendingCash -= collected; Money += collected;cashCollected+=collected; return collected;
        }
        public void Restore(long money, long revenue, int transactions, IEnumerable<string> unlocked, long pendingCash = 0,
            IEnumerable<long> paidReceipts = null, IEnumerable<LossRecord> recordedLosses = null,long collectedCash=0)
        {
            RequirePrimitive();
            Money = Math.Max(0, money); Revenue = Math.Max(0, revenue); Transactions = Math.Max(0, transactions);
            cashCollected=Math.Max(0,collectedCash);
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
