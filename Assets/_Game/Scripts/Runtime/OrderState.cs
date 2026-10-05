using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon
{
    [Serializable]
    public sealed class OrderLine
    {
        public string id;
        public int requested, delivered, unitPrice;
        public int Remaining => Math.Max(0, requested - delivered);
        public OrderLine() { }
        public OrderLine(string id, int requested, int unitPrice)
        { this.id = id; this.requested = requested; this.unitPrice = unitPrice; }
        public OrderLine Copy() => new(id, requested, unitPrice) { delivered = delivered };
    }

    [Serializable]
    public sealed class CustomerSave
    {
        public long receipt;
        public string shop, lane;
        public int phase, queueIndex, approachStep;
        public float remaining, x, y, z, exitX, exitY, exitZ;
        public bool paid, timedOut;
        public List<OrderLine> order = new();
        public List<ItemAmount> basket = new();
    }

    public sealed class OrderState
    {
        public const float Patience = 90;
        public long Receipt { get; private set; }
        RuntimeTransactions authority;
        readonly List<OrderLine> lines = new();
        float deadline; bool paid,timedOut;
        public List<OrderLine> Lines => authority==null?lines:authority.Order(Receipt).lines.ConvertAll(x=>x.Copy());
        public float Deadline { get => authority==null?deadline:(float)authority.Order(Receipt).deadline; private set => deadline=value; }
        public bool Paid { get => authority==null?paid:authority.Order(Receipt).status==OrderStatus.Complete; private set => paid=value; }
        public bool TimedOut { get => authority==null?timedOut:authority.Order(Receipt).status==OrderStatus.Failed; private set => timedOut=value; }
        internal void Bind(RuntimeTransactions runtime) { if(runtime.Order(Receipt)==null)throw new System.IO.InvalidDataException("Order reference không tồn tại: "+Receipt);authority=runtime; }
        public bool Finished => Paid || TimedOut;
        public bool Complete => Lines.Count > 0 && Lines.TrueForAll(x => x.Remaining == 0);
        public int TotalPrice { get { int total = 0; foreach (var line in Lines) total += line.requested * line.unitPrice; return total; } }
        public int RemainingQuantity { get { int total = 0; foreach (var line in Lines) total += line.Remaining; return total; } }
        public OrderState(long receipt, IEnumerable<OrderLine> lines, float queueJoinedAt, float patience = Patience)
        {
            Receipt = receipt; Deadline = queueJoinedAt + Math.Max(0, patience);
            if (lines != null) foreach (var line in lines)
                if (line != null && line.requested > 0 && line.unitPrice > 0 && !string.IsNullOrEmpty(line.id) && Definitions.Item(line.id) != null) Lines.Add(line.Copy());
        }
        public float RemainingPatience(float now) => authority==null?Math.Max(0,Deadline-now):(float)Math.Max(0,authority.Order(Receipt).deadline-authority.Now);
        public bool Expire(Inventory received, Economy economy, float now)
        {
            if(authority!=null)return !Finished&&authority.Now>=authority.Order(Receipt).deadline&&authority.Fail(Receipt);
            if (Finished || now < Deadline || economy == null) return false;
            if (economy.IsPaid(Receipt)) { Paid = true; return false; }
            TimedOut = true; economy.RecordLoss(Receipt, received); return true;
        }
        public int Deliver(Inventory source, Inventory received, Economy economy, float now, EntityId? actingActor = null)
        {
            if(authority!=null)return authority.Deliver(Receipt,source,actingActor:actingActor);
            if (Finished || economy == null || received == null) return 0;
            if (economy.IsPaid(Receipt)) { Paid = true; return 0; }
            if (economy.IsLost(Receipt)) { TimedOut = true; return 0; }
            // Hạn chờ thắng khi giao và hết giờ cùng khung hình; không nhận thêm hàng sau hạn.
            if (Expire(received, economy, now)) return 0;
            int moved = 0;
            foreach (var line in Lines)
            {
                int amount = Inventory.Transfer(source, received, line.id, line.Remaining);
                line.delivered += amount; moved += amount;
            }
            if (Complete) Paid = economy.RecordPayment(Receipt, TotalPrice);
            return moved;
        }
        public List<OrderLine> SnapshotLines()
        {
            var copy = new List<OrderLine>(); foreach (var line in Lines) copy.Add(line.Copy()); return copy;
        }
        public static OrderState Restore(CustomerSave saved, float now)
        {
            return new OrderState(saved.receipt, saved.order, now, saved.remaining) { Paid = saved.paid, TimedOut = saved.timedOut };
        }
    }
}
