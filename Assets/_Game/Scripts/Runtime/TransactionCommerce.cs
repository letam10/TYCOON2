using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class TransactionCore
    {
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
                long amount=cash.amount;PhysicalCashRules.CreditHand(s,amount);s.cashCollected=checked(s.cashCollected+amount);cash.amount=0;return (int)Math.Min(int.MaxValue,amount);
            }
            var payment = s.payments.Find(x => x.id == c.target);
            Require(payment != null, "payment", "Payment không tồn tại."); WriteAccess(Owner(s, payment.counter), c.actor);
            if (payment.collected) return 0;
            PhysicalCashRules.CreditHand(s,payment.amount);s.cashCollected=checked(s.cashCollected+payment.amount); payment.collected = true; return payment.amount;
        }
        int Contribute(TransactionState s, TransactionCommand c)
        {
            Player(s, c.actor); var definition = Definitions.Upgrade(c.target);
            // Chỉ replay lệnh đã ghi của pad vừa nghỉ dùng; gameplay mới không thể góp thêm.
            bool retiredReplay = replaying && c.target == "dairy";
            Require(definition != null && (definition.kind != "legacy" || retiredReplay) &&
                c.quantity > 0 && !s.unlocked.Contains(c.target), "purchase", "Purchase không khả dụng.");
            Require(string.IsNullOrEmpty(definition.requirement) || s.unlocked.Contains(definition.requirement), "requirement", "Thiếu điều kiện mở khóa.");
            if (!retiredReplay) ValidatePurchaseRequirements(s,definition);
            var purchase = s.purchases.Find(x => x.id == c.target);
            if (purchase == null) { purchase = new PurchaseRuntimeState { id = c.target, definitionId = c.target }; s.purchases.Add(purchase); }
            int amount = (int)Math.Min(c.quantity, Math.Min(PhysicalCashRules.Hand(s), definition.cost - purchase.contributed));
            Require(amount > 0, "funds", "Không còn tiền hoặc purchase đã góp đủ.");
            PhysicalCashRules.Spend(s, amount); purchase.contributed += amount; return amount;
        }
        int CompletePurchase(TransactionState s, TransactionCommand c)
        {
            Player(s, c.actor); var purchase = s.purchases.Find(x => x.id == c.target); var definition = Definitions.Upgrade(c.target);
            Require(purchase != null && definition != null && purchase.definitionVersion == definition.version && purchase.contributed == definition.cost, "purchase-incomplete", "Purchase chưa góp đủ.");
            if (purchase.complete) return 0;
            purchase.complete = true; if (!s.unlocked.Contains(c.target)) s.unlocked.Add(c.target);
            if (definition.kind == "worker" && !s.crews.Any(x => x.id == c.target)) s.crews.Add(GameSession.CrewFor(definition));
            if(c.target=="truck_bundle")EnsureTruck(s);
            RefreshProgression(s);
            return 1;
        }
    }
}
