using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    [Serializable] public sealed class WorkerSave
    {
        public string id, upgrade, source, destination, item, working, reservation;
        public float x,z;
        public int phase,count,deliveries;
        public long tableReceipt;
        public List<ItemAmount> carry=new();
    }
    public sealed class WorkerAgent : MonoBehaviour
    {
        public string WorkerId,UpgradeId,Role,Reason="Chờ việc";
        public NavMeshAgent Agent; public ActorView View;
        [NonSerialized] public Inventory Carry=new(6,true);
        public CheckoutStation Checkout; public Vector3 Anchor;
        public bool IsCashier=>Role=="Cashier";
        public int Deliveries;
        CrewState crew;
        Station source,destination,working;
        Inventory from,to;
        string item;
        int phase,count,cursor;
        float nextDecision,stuckTime,routeRetryAt;
        int stuckRepaths;
        Vector3 previous;
        bool heldSpace;
        string reservationId;
        float workDelta;
        long tableReceipt;
        TextMesh label;
        public string Diagnostic()=>WorkerId+" "+Reason+" phase="+phase+" item="+item+" carry="+Carry.Total+" source="+source?.Id+" destination="+destination?.Id;
        public void Initialize(UpgradeDefinition upgrade)
        {
            UpgradeId=upgrade.id;crew=GameSession.Instance.CrewStates.Find(x=>x.id==upgrade.id)??GameSession.CrewFor(upgrade);Role=crew.role;
            Agent=Navigation.Agent(gameObject);View=GetComponentInChildren<ActorView>();View.Initialize();
            var stackRoot=new GameObject("WorkerCarry");stackRoot.transform.SetParent(transform,false);stackRoot.transform.localPosition=new Vector3(0,.8f,-.4f);
            var stack=stackRoot.AddComponent<InventoryStack>();stack.Inventory=Carry;stack.Pool=GameSession.Instance.Pool;stack.Columns=stack.Rows=1;stack.Maximum=16;stack.Scale=.7f;
            label=Art.Label("",new Vector3(0,2.1f,0),transform,.2f,"#183D26").GetComponent<TextMesh>();
        }
        void Update()
        {
            var g=GameSession.Instance;if(!g.NavigationReady||!g.CanSimulate||!Agent.isOnNavMesh)return;
            if(Time.time<routeRetryAt){Reason="Đang chờ thông lối";return;}
            crew=g.CrewStates.Find(x=>x.id==UpgradeId)??crew;
            if(!g.StorageFor(crew.area)){Reason="Khu chưa có kho riêng";return;}
            Agent.speed=4.2f*(1+.2f*(crew.speedLevel-1));Carry.Capacity=crew.carryLevel==1?6:crew.carryLevel==2?10:crew.carryLevel==3?16:24;
            View.SetMotion(Agent.velocity.magnitude,Carry.Total>0);
            if(label){label.text=Reason;label.gameObject.SetActive((g.Player.transform.position-transform.position).sqrMagnitude<60);label.transform.rotation=Camera.main.transform.rotation;}
            float moved=(transform.position-previous).sqrMagnitude;
            if(Agent.hasPath&&Agent.remainingDistance>.6f&&moved<.0001f)stuckTime+=Time.deltaTime;
            else{stuckTime=0;if(moved>.0001f)stuckRepaths=0;}
            previous=transform.position;
            if(stuckTime>3)
            {
                if(stuckRepaths<3){Navigation.Go(Agent,Agent.destination,true);stuckRepaths++;stuckTime=0;return;}
                Agent.ResetPath();Reason="Lối bị chặn • đang chờ";routeRetryAt=Time.time+5;stuckRepaths=0;stuckTime=0;return;
            }
            if(phase==1||phase==2){Transport();return;}
            if(working)
            {
                if(!working.IsUnlocked){StopWorking();return;}
                if(Role=="Farmer"&&working is ProductionStation stockedCrop&&FarmStockEnough(stockedCrop,g.StorageFor(crew.area)))
                {StopWorking();Reason="Chờ nhu cầu • kho đã đủ loại hàng này";return;}
                if(working is ProductionStation waitingCrop&&!waitingCrop.Animal&&waitingCrop.Phase==2)
                {StopWorking();nextDecision=0;return;}
                if(working is MachineStation waitingMachine)
                {
                    if(waitingMachine.Broken){Reason="Máy hỏng • cần người chơi sửa";waitingMachine.ReleaseOperator(GetEntityId());Navigation.Go(Agent,waitingMachine.WaitingPoint);return;}
                    if(waitingMachine.Phase==MachinePhase.CompletedWaitingPickup)
                    {
                        var stock=g.StorageFor(crew.area);string output=waitingMachine.Recipe.output;
                        if(stock&&stock.Inventory.FreeFor(output)>0){waitingMachine.ReleaseOperator(GetEntityId());BeginMove(waitingMachine,stock,output,Mathf.Min(Carry.Capacity,waitingMachine.Inventory.Available(output)),false);return;}
                        Reason=stock?"Chờ kho còn chỗ cho "+Definitions.Item(output).label:"Khu chưa có kho nhận thành phẩm";
                        waitingMachine.ReleaseOperator(GetEntityId());return;
                    }
                    if(waitingMachine.Phase==MachinePhase.WaitingInput)
                    {
                        foreach(var input in waitingMachine.Recipe.inputs)
                        {
                            int missing=Mathf.Max(0,input.count-waitingMachine.Input.Available(input.id));if(missing==0)continue;
                            var supply=g.StorageFor(SupplyArea(input.id));
                            if(supply&&supply.Inventory.Available(input.id)>0)
                            {waitingMachine.ReleaseOperator(GetEntityId());BeginMove(supply,waitingMachine,input.id,Mathf.Min(missing,supply.Inventory.Available(input.id)),true);return;}
                            Reason="Chờ "+Definitions.Item(input.id).label+" trong kho "+SupplyArea(input.id);waitingMachine.ReleaseOperator(GetEntityId());return;
                        }
                        Reason=waitingMachine.WaitReason;waitingMachine.ReleaseOperator(GetEntityId());return;
                    }
                }
                Vector3 point=working.WorkPoint;
                if((transform.position-point).sqrMagnitude>1.3f){Navigation.Go(Agent,point);Reason="Đến "+working.Label;return;}
                Agent.ResetPath();
                if(working is MachineStation machine)
                {
                    Reason=machine.Phase==MachinePhase.Operating?"Đang vận hành":"Đang khởi động máy";
                    if(!machine.Broken&&machine.Operate(Time.deltaTime*(1+.2f*(crew.speedLevel-1)),GetEntityId()))View.Work(ActorView.WorkState(machine,InteractionKind.Operate));
                    return;
                }
                if(working is TableStation table)
                {
                    Reason=table.Cleaning>0?"Đang dọn bàn":table.NeedsMeal?"Đang phục vụ":"Chờ bàn";
                    table.Work(Carry,Time.deltaTime*(1+.2f*(crew.speedLevel-1)),GetEntityId());
                    View.Work(table.Cleaning>0?"Cleaning":"Serving");
                    if(!table.NeedsMeal&&table.Cleaning<=0)StopWorking();
                    return;
                }
                if(working is ProductionStation animal&&animal.Animal&&animal.Feed<ProductionStation.MaximumFeed&&
                    Carry.Available(animal.FeedItem)==0&&animal.Inventory.Available(animal.FeedItem)==0)
                {
                    var supply=g.StorageFor(animal.AreaId);int needed=ProductionStation.MaximumFeed-animal.Feed-animal.Inventory.Available(animal.FeedItem);
                    if(supply&&supply.Inventory.Available(animal.FeedItem)>0){animal.ReleaseOperator(GetEntityId());BeginMove(supply,animal,animal.FeedItem,Mathf.Min(needed,supply.Inventory.Available(animal.FeedItem)),false);}
                    else{Reason="Thiếu "+Definitions.Item(animal.FeedItem).label+" làm thức ăn";animal.ReleaseOperator(GetEntityId());Navigation.Go(Agent,animal.InteractionPoint+Vector3.left*2);}
                    return;
                }
                if(working is ProductionStation restock&&restock.Animal&&restock.Herd<ProductionStation.MaximumHerd&&restock.Feed>0&&restock.Breeding<=0)
                {var result=restock.RestockPlayer(GetEntityId());Reason=result.Worked?"Đang tái đàn":"Chờ tái đàn: "+result.Reason;return;}
                Reason=working is ProductionStation livestock&&livestock.Animal?"Đang chăm vật nuôi":"Đang làm việc";
                workDelta+=Time.deltaTime;if(workDelta<.1f)return;
                working.Work(Carry,workDelta*(1+.2f*(crew.speedLevel-1)),GetEntityId());workDelta=0;
                View.Work(ActorView.WorkState(working,InteractionKind.Operate));
                if(Carry.Total>0){StopWorking();BeginDrop(g.StorageFor(crew.area));}
                return;
            }
            if(Time.time<nextDecision)return;nextDecision=Time.time+.2f;
            if(IsCashier)
            {
                string shop=crew.area=="supermarket"?"market":crew.area;
                var counters=g.Checkouts.FindAll(x=>x.ShopId==shop&&x.IsUnlocked);
                if(counters.Count==0){Reason="Chờ quầy";return;}
                Checkout=counters[Math.Abs(Slot)%counters.Count];Anchor=Checkout.transform.position+new Vector3(0,0,1.4f);
                if(Carry.Total>0)
                {
                    var currentOrder=Checkout.FrontOrder;
                    bool needed=currentOrder!=null&&currentOrder.Receipt==tableReceipt;
                    if(needed)
                    {
                        needed=false;
                        foreach(var row in currentOrder.Lines)needed|=row.Remaining>0&&Carry.Available(row.id)>0;
                    }
                    if(!needed){BeginDrop(g.StorageFor(crew.area));return;}
                    if((transform.position-Checkout.WorkPoint).sqrMagnitude>1.3f){Navigation.Go(Agent,Checkout.WorkPoint);return;}
                    Agent.ResetPath();
                    if(Checkout.Serve(Carry)){Deliveries++;Reason="Đang phục vụ";View.Work("Cashier");}
                    else Reason="Chờ khách tại quầy";
                    return;
                }
                var order=Checkout.FrontOrder;
                if(order==null){Reason="Chờ khách tại quầy";return;}
                foreach(var row in order.Lines)
                {
                    int needed=row.requested-row.delivered;if(needed<=0)continue;
                    var shelf=g.Shelves.Find(x=>x.ShopId==shop&&x.IsUnlocked&&x.Inventory.Available(row.id)>0);
                    if(shelf){BeginMove(shelf,Checkout,row.id,needed,false);return;}
                }
                Reason="Chờ hàng tại kệ";
                return;
            }
            if(Carry.Total>0){BeginDrop(g.StorageFor(crew.area));return;}
            if(Role is "Farmer" or "AnimalWorker")
            {
                var list=g.Producers.FindAll(x=>x.IsUnlocked&&x.AreaId==crew.area&&(Role=="Farmer"?x.ItemId is "carrot" or "wheat" or "tomato":x.ItemId is "milk" or "egg" or "beef"));
                list.RemoveAll(x=>!x.Animal&&x.Phase==2||g.Workers.Exists(w=>w!=this&&w.working==x)||x.IsOperatedByOther(GetEntityId()));
                if(Role=="Farmer")
                {
                    var stock=g.StorageFor(crew.area);list.RemoveAll(p=>FarmStockEnough(p,stock)||stock.Inventory.FreeFor(p.ItemId)<p.HarvestQuantity);
                    list.Sort((a,b)=>stock.Inventory.Count(a.ItemId).CompareTo(stock.Inventory.Count(b.ItemId)));
                }
                if(list.Count>0){working=Role=="Farmer"?list[0]:list[(Math.Abs(Slot)+cursor++)%list.Count];Navigation.Go(Agent,working.WorkPoint);}
                else Reason="Chờ trạm sản xuất";
                return;
            }
            if(Role is "Processor" or "Baker" or "Cook")
            {
                var list=g.Machines.FindAll(x=>x.IsUnlocked&&x.AreaId==crew.area&&!g.Workers.Exists(w=>w!=this&&w.working==x));
                if(list.Count>0){working=list[Math.Abs(Slot)%list.Count];Navigation.Go(Agent,working.WorkPoint);}
                return;
            }
            if(Role=="Waiter")
            {
                var dirty=g.Tables.Find(x=>x.IsUnlocked&&x.AreaId==crew.area&&x.Cleaning>0&&!g.Workers.Exists(w=>w!=this&&w.working==x));
                if(dirty){working=dirty;Navigation.Go(Agent,dirty.WorkPoint);return;}
                var table=g.Tables.Find(x=>x.AreaId==crew.area&&x.NeedsMeal&&!g.Workers.Exists(w=>w!=this&&w.destination==x&&w.phase>0));
                if(table){var food=g.Machines.Find(x=>x.AreaId=="restaurant"&&x.Inventory.Available("meal")>0) as Station??g.StorageFor("restaurant");BeginMove(food,table,"meal",1,false);}
                return;
            }
            FindRoute();
        }
        int Slot { get { if(WorkerId!=null&&int.TryParse(WorkerId.Substring(WorkerId.LastIndexOf(':')+1),out var n))return n;return 0; } }
        static bool FarmStockEnough(ProductionStation crop,StorageStation storage)=>storage&&
            storage.Inventory.Count(crop.ItemId)+storage.Inventory.ReservedSpace(crop.ItemId)>=Mathf.Max(crop.HarvestQuantity*2,storage.SharedReserveThreshold(crop.ItemId)+6);
        void FindRoute()
        {
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
                    foreach(var s in g.Stations)if(s is StorageStation&&s.AreaId==SupplyArea(input.id)&&s.Inventory.Available(input.id)>0){supply=s;break;}
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
        static string SupplyArea(string sku)=>sku is "flour" or "cheese" or "sauce"?"processing":sku is "bread" or "cake"?"bakery":sku=="meal"?"restaurant":"farm";
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
        void OnDisable(){StopWorking();CancelTransport();}
        string InventoryId(Station s,Inventory inv)=>s==null?"":s.Id+(s is MachineStation m&&ReferenceEquals(m.Input,inv)?"_input":"");
        public WorkerSave Snapshot()=>new(){id=WorkerId,upgrade=UpgradeId,source=InventoryId(source,from),destination=InventoryId(destination,to),item=item,working=working?.Id,reservation=reservationId,tableReceipt=tableReceipt,x=transform.position.x,z=transform.position.z,phase=phase,count=count,deliveries=Deliveries,carry=Carry.Snapshot()};
        public void Restore(WorkerSave saved)
        {
            StopWorking();CancelTransport();
            if(Carry.Authority==null)Carry.Restore(saved.carry);Deliveries=saved.deliveries;item=saved.item;count=saved.count;phase=saved.phase;tableReceipt=saved.tableReceipt;reservationId=saved.reservation;
            var g=GameSession.Instance;
            source=g.Stations.Find(s=>s.Id==saved.source);destination=g.Stations.Find(s=>s.Id==saved.destination||s.Id+"_input"==saved.destination);
            from=source?.Inventory;to=destination is MachineStation m&&saved.destination.EndsWith("_input")?m.Input:destination?.Inventory;
            working=g.Stations.Find(s=>s.Id==saved.working);
            if(g.Transactions!=null)
            {
                if(phase is 1 or 2)
                {
                    if(destination is CheckoutStation)to=null;
                    if(destination is TableStation)to=null;
                    var reservation=g.Transactions.Reservation(reservationId);
                    heldSpace=reservation!=null&&reservation.status==ReservationStatus.Active;
                    if(!heldSpace){phase=0;count=0;reservationId=null;}
                }
                return;
            }
            if(phase is 1 or 2)
            {
                heldSpace=to!=null&&to.TryReserveSpace(item,count);
                if((!heldSpace&&!(destination is CheckoutStation))||(phase==1&&(from==null||!from.TryReserve(item,count)))){if(heldSpace)to.ReleaseSpace(item,count);phase=0;heldSpace=false;throw new System.IO.InvalidDataException("Không khôi phục được chỗ giữ cho tuyến vận chuyển "+WorkerId);}
            }
        }
    }
}

