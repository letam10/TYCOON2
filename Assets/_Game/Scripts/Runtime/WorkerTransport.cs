using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed partial class WorkerAgent
    {
        static bool FarmStockEnough(ProductionStation crop,StorageStation storage)=>storage&&
            storage.Inventory.Count(crop.ItemId)+storage.Inventory.ReservedSpace(crop.ItemId)>=Mathf.Max(crop.HarvestQuantity*2,storage.SharedReserveThreshold(crop.ItemId)+6);
        void FindRoute()
        {
            using var timing = QaFrameProbe.Measure("WorkerTransport.FindRoute");
            var g=GameSession.Instance;var local=g.StorageFor(crew.area);
            if(Role=="Restocker")
            {
                var shelves=g.Shelves.FindAll(x=>x.AreaId==crew.area&&x.IsUnlocked);
                foreach(var shelf in shelves)foreach(var def in Definitions.Items)if(shelf.Accepts(def.id)&&SaleAvailable(local,def.id)>0&&shelf.Inventory.FreeFor(def.id)>0){BeginMove(local,shelf,def.id,Mathf.Min(Carry.Capacity,SaleAvailable(local,def.id)),false);return;}
                Reason="Chờ hàng trong kho khu";Navigation.Go(Agent,local.WaitingPoint);return;
            }
            foreach(var machine in g.Machines)
            {
                if(!machine.IsUnlocked||machine.AreaId!=crew.area)continue;
                foreach(var output in machine.Inventory.Snapshot())if(local.Inventory.FreeFor(output.id)>0){BeginMove(machine,local,output.id,Carry.Capacity,false);return;}
                foreach(var input in machine.Recipe.inputs)
                {
                    if(machine.Input.FreeFor(input.id)<=0)continue;
                    Station supply=null;
                    supply=SupplyFor(input.id);
                    if(supply){BeginMove(supply,machine,input.id,Carry.Capacity,true);return;}
                }
            }
            // Bán thành phẩm từ kho khu; đây là tuyến cố định của đội vận chuyển.
            foreach(var shelf in g.Shelves)if(shelf.AreaId==crew.area&&shelf.IsUnlocked)foreach(var def in Definitions.Items)
                if(shelf.Accepts(def.id))foreach(var s in g.Stations)if(s is StorageStation&&s.AreaId==SupplyArea(def.id)&&SaleAvailable(s,def.id)>0&&shelf.Inventory.FreeFor(def.id)>0){BeginMove(s,shelf,def.id,Mathf.Min(Carry.Capacity,SaleAvailable(s,def.id)),false);return;}
            Reason="Chờ nguyên liệu";Navigation.Go(Agent,local.WaitingPoint);
        }
        void BeginMove(Station pickup,Station drop,string sku,int requested,bool input)
        {
            if (Retiring) return;
            if(!pickup||!drop)return;
            tableReceipt=drop is TableStation table?table.Occupant?.Receipt??0:drop is CheckoutStation counter?counter.FrontOrder?.Receipt??0:0;
            source=pickup;destination=drop;from=pickup.Inventory;to=input?((MachineStation)drop).Input:drop.Inventory;item=sku;
            count=Mathf.Min(requested,Carry.FreeFor(sku),from.Available(sku),drop is CheckoutStation?requested:to.FreeFor(sku));
            if(count<=0){Reason=from.Available(sku)==0?"Thiếu nguyên liệu":"Đích đã đầy";return;}
            if(GameSession.Instance.Transactions!=null)
            {
                if(drop is CheckoutStation servingCounter)to=servingCounter.Queue[0].Basket;
                if(drop is TableStation servingTable)to=servingTable.Occupant.Basket;
                reservationId=GameSession.Instance.Transactions.Reserve(from,to,sku,count,GetEntityId(),Carry);
                if(reservationId==null)return;heldSpace=true;phase=1;Navigation.Go(Agent,pickup.WorkPoint);Reason="Đang lấy hàng";return;
            }
            if(to!=null&&!to.TryReserveSpace(sku,count))return;
            if(!from.TryReserve(sku,count)){to?.ReleaseSpace(sku,count);return;}
            heldSpace=to!=null;phase=1;Navigation.Go(Agent,pickup.WorkPoint);Reason="Đang lấy hàng";
        }
        void BeginDrop(Station drop)
        {
            if(!drop){Reason="Khu chưa có kho riêng";return;}
            var items=Carry.Snapshot();if(items.Count==0)return;item=items[0].id;source=null;destination=drop;from=Carry;to=drop.Inventory;
            count=Mathf.Min(Carry.Count(item),to.FreeFor(item));
            if(count<=0){Reason="Đích đã đầy";Navigation.Go(Agent,drop.WaitingPoint);return;}
            if(GameSession.Instance.Transactions!=null)
            {
                reservationId=GameSession.Instance.Transactions.Reserve(Carry,to,item,count,GetEntityId());
                if(reservationId==null)return;heldSpace=true;phase=2;Navigation.Go(Agent,drop.InteractionPoint);return;
            }
            heldSpace=to.TryReserveSpace(item,count);if(!heldSpace)return;phase=2;Navigation.Go(Agent,drop.InteractionPoint);
        }
        void Transport()
        {
            using var timing = QaFrameProbe.Measure("WorkerTransport.Transport");
            if(GameSession.Instance.Transactions!=null)
            {
                var r=GameSession.Instance.Transactions.Reservation(reservationId);
                if(r==null||r.status!=ReservationStatus.Active||r.expiresAt<=GameSession.Instance.Transactions.Now){CancelTransport();Reason="Chỗ giữ đã hết hạn";return;}
            }
            var target=phase==1?source:destination;
            if(!target||!target.IsUnlocked){CancelTransport();Reason="Đích không còn khả dụng";return;}
            if(destination is TableStation requestedTable&&(!requestedTable.NeedsMeal||requestedTable.Occupant.Receipt!=tableReceipt))
            {CancelTransport();Reason="Đơn bàn đã kết thúc";return;}
            if(destination is CheckoutStation requestedCounter&&requestedCounter.FrontOrder?.Receipt!=tableReceipt)
            {CancelTransport();Reason="Đơn quầy đã kết thúc";return;}
            if((transform.position-target.WorkPoint).sqrMagnitude>1.3f){Navigation.Go(Agent,target.WorkPoint);return;}
            Agent.ResetPath();
            if(phase==1)
            {
                int n=GameSession.Instance.Transactions!=null?GameSession.Instance.Transactions.Transfer(from,Carry,item,count,reservation:reservationId):Inventory.TransferReserved(from,Carry,item,count);
                if(n<count&&GameSession.Instance.Transactions==null){to?.ReleaseSpace(item,count-n);from.Release(item,count-n);}count=n;
                if(n==0){phase=0;heldSpace=false;Reason="Thiếu nguyên liệu";return;}
                View.Interact(true);
                phase=2;Navigation.Go(Agent,destination.WorkPoint);Reason="Đang vận chuyển";
            }
            else
            {
                int n;
                if(GameSession.Instance.Transactions!=null)
                {
                    if(destination is CheckoutStation counter)
                    {
                        var customer=counter.Queue.Count>0?counter.Queue[0]:null;
                        if(!customer||Vector3.Distance(customer.transform.position,counter.QueuePoint(customer))>.9f){Reason="Chờ khách tại quầy";return;}
                        n=GameSession.Instance.Transactions.Deliver(tableReceipt,Carry,reservationId);
                        if(customer.Order.Finished)customer.Leave();
                    }
                    else if(destination is TableStation table)n=GameSession.Instance.Transactions.Deliver(tableReceipt,Carry,reservationId);
                    else n=GameSession.Instance.Transactions.Transfer(Carry,to,item,count,reservation:reservationId);
                    if(n==0){Reason="Chờ đích nhận hàng";return;}
                    if(destination is CheckoutStation)View.Work("Cashier");else if(destination is TableStation)View.Work("Serving");else View.Interact(false);
                    heldSpace=false;phase=0;reservationId=null;Deliveries++;
                    if(destination is MachineStation)StopWorking();
                    if(destination is ProductionStation livestock&&livestock.Animal)working=livestock;return;
                }
                if(destination is CheckoutStation servingCounter)
                {
                    // Giao từ giỏ nhân viên sau khi đã đi tới đúng quầy và đúng đơn.
                    n=servingCounter.Serve(Carry)?count:0;
                    if(n==0){CancelTransport();Reason="Đơn quầy không nhận hàng";return;}
                }
                else if(destination is TableStation servingTable)
                {n=servingTable.DeliverMeal(Carry,tableReceipt)?1:0;if(n>0)to.ReleaseSpace(item,n);}
                else n=Inventory.TransferIntoReservedSpace(Carry,to,item,count);
                if(n==0){Reason="Đích đã đầy";return;}
                count-=n;if(count>0)return;
                heldSpace=false;phase=0;Deliveries++;destination.WorkCount++;
                if(destination is ProductionStation animal&&animal.Animal)working=animal;
            }
        }
        static string SupplyArea(string sku)=>sku is "flour" or "cheese" or "sauce" or "soy_sauce" or "bottled_milk" or "yarn" or "cloth"?"processing":sku is "bread" or "cake" or "bread_dough" or "cake_batter"?"bakery":sku is "meal" or "beef_soy" or "corn_soup" or "pasta" or "egg_sandwich" or "soy_vegetables"?"restaurant":"farm";
        Station SupplyFor(string item)
        {
            var g=GameSession.Instance;var local=g.StorageFor(crew.area);if(local&&local.Inventory.Available(item)>0)return local;
            var origin=g.StorageFor(SupplyArea(item));if(origin&&origin.Inventory.Available(item)>0)return origin;
            return g.Machines.FirstOrDefault(x=>x.IsUnlocked&&x.AreaId==crew.area&&x.Inventory.Available(item)>0);
        }
        static int SaleAvailable(Station source,string sku)
        {
            return source is StorageStation storage?storage.AvailableAboveReserve(sku):source.Inventory.Available(sku);
        }
        void StopWorking(){if(working)working.ReleaseOperator(GetEntityId());working=null;workDelta=0;}
        void CancelTransport()
        {
            if(GameSession.Instance&&GameSession.Instance.Transactions!=null)
            {
                GameSession.Instance.Transactions.Release(reservationId,GetEntityId());reservationId=null;
                heldSpace=false;phase=0;count=0;source=destination=null;from=to=null;return;
            }
            if(phase==1&&from!=null)from.Release(item,count);
            if(heldSpace&&to!=null)to.ReleaseSpace(item,count);
            heldSpace=false;phase=0;count=0;source=destination=null;from=to=null;
        }
    }
}
