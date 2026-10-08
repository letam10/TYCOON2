using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    [Serializable] public sealed class WorkerSave
    {
        public string id, upgrade, source, destination, item, working, reservation;
        public float x,z;
        public int phase,count,deliveries;
        public bool retiring, retired;
        public long tableReceipt;
        public List<ItemAmount> carry=new();
    }
    public sealed partial class WorkerAgent : MonoBehaviour
    {
        public string WorkerId,UpgradeId,Role,Reason="Chờ việc";
        public NavMeshAgent Agent; public ActorView View;
        [NonSerialized] public Inventory Carry=new(6,true);
        public CheckoutStation Checkout; public Vector3 Anchor;
        public bool IsCashier=>Role=="Cashier";
        public int Deliveries;
        CrewState crew;
        StorageStation areaStorage;
        float movementSpeed;
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
        public string Diagnostic()=>WorkerId+" "+Reason+" phase="+phase+" item="+item+" carry="+Carry.Total+" source="+source?.Id+" destination="+destination?.Id;
        public void Initialize(UpgradeDefinition upgrade)
        {
            UpgradeId = upgrade.id;
            crew = GameSession.Instance.ReadCrew(upgrade.id) ?? GameSession.CrewFor(upgrade);
            Role = crew.role;
            Agent=Navigation.Agent(gameObject);View=GetComponentInChildren<ActorView>();View.Initialize();
            CarryPresentation.AttachWorker(this);
        }
        void Update()
        {
            using var frameProbe = QaFrameProbe.Measure("WorkerAgent.Update");
            var g=GameSession.Instance;if(!g.NavigationReady||!g.CanSimulate||Time.deltaTime<=0)return;
            if (Retiring && Role == "Driver") { TickRetirement(); return; }
            if(Role=="Driver")
            {
                if(g.Logistics?.Vehicle){if(Agent.enabled)Navigation.Suspend(Agent);transform.SetParent(g.Logistics.Vehicle,false);transform.localPosition=new(0,.45f,1.5f);transform.localRotation=Quaternion.identity;Reason=g.Logistics.Reason;
                    if(View.Animator.HasState(0,Animator.StringToHash("Sit"))&&!View.Animator.GetCurrentAnimatorStateInfo(0).IsName("Sit"))View.Animator.CrossFadeInFixedTime("Sit",.2f);}
                return;
            }
            if(!Agent.isOnNavMesh)return;
            if(Time.time<routeRetryAt){Reason="Đang chờ thông lối";return;}
            crew=g.ReadCrew(UpgradeId)??crew;
            if (!areaStorage) areaStorage = g.StorageFor(crew.area);
            if (!areaStorage) { Reason = "Khu chưa có kho riêng"; return; }
            float nextSpeed = WorkforceRules.MovementSpeed(crew.speedLevel);
            if (movementSpeed != nextSpeed) { movementSpeed = nextSpeed; Agent.speed = nextSpeed; }
            int capacity = CarryLimits.Worker(crew);
            if (Carry.Capacity != capacity) Carry.Capacity = capacity;
            View.SetMotion(Navigation.ActualSpeed(Agent),Role=="Loader"?g.Logistics.HasCargo(this):Carry.Total>0);
            float moved=(transform.position-previous).sqrMagnitude;
            if(Agent.hasPath&&Agent.remainingDistance>.6f&&moved<.0001f)stuckTime+=Time.deltaTime;
            else{stuckTime=0;if(moved>.0001f)stuckRepaths=0;}
            previous=transform.position;
            if(stuckTime>3)
            {
                if(stuckRepaths<3){Navigation.Go(Agent,Agent.destination,true);stuckRepaths++;stuckTime=0;return;}
                Agent.ResetPath();Reason="Lối bị chặn • đang chờ";routeRetryAt=Time.time+5;stuckRepaths=0;stuckTime=0;return;
            }
            if (Retiring) { TickRetirement(); return; }
            if(Role=="Loader"){g.Logistics.Loader(this,crew);return;}
            if(phase==1||phase==2){Transport();return;}
            if(working)
            {
                if(!working.IsUnlocked){StopWorking();return;}
                if(Role=="Repairer" && working is MachineStation brokenMachine)
                {
                    if(!brokenMachine.Broken){StopWorking();return;}
                    if(g.Player.ActiveInteraction is ProximityTarget p && p.Target==brokenMachine && p.Kind==InteractionKind.Repair)
                    {Reason="Người chơi đang sửa • nhường chỗ";brokenMachine.ReleaseOperator(GetEntityId());Navigation.Go(Agent,brokenMachine.WaitingPoint);return;}
                    if((transform.position-brokenMachine.WorkPoint).sqrMagnitude>1.3f){Navigation.Go(Agent,brokenMachine.WorkPoint);Reason="Đến sửa "+brokenMachine.Label;return;}
                    if (Agent.hasPath) Agent.ResetPath();
                    if (!brokenMachine.RepairPaid && g.Economy.CashInHand < brokenMachine.RepairFee)
                    {
                        Reason = "Rút tiền tại két";
                        return;
                    }
                    bool repaired=brokenMachine.Repair(g.Economy,Time.deltaTime*WorkforceRules.Capability,GetEntityId());
                    Reason=repaired?"Đang sửa • "+RepairTiming.Employee(crew.speedLevel)+"s":g.Transactions.LastReason;
                    if(repaired)View.Work(ActorView.WorkState(brokenMachine,InteractionKind.Repair));return;
                }
                if(Role=="Farmer"&&working is ProductionStation stockedCrop&&FarmStockEnough(stockedCrop,g.StorageFor(crew.area)))
                {StopWorking();Reason="Chờ nhu cầu • kho đã đủ loại hàng này";return;}
                if(working is ProductionStation waitingCrop&&!waitingCrop.Animal&&waitingCrop.Phase==2)
                {StopWorking();nextDecision=0;return;}
                if(working is MachineStation waitingMachine)
                {
                    if(waitingMachine.Broken){Reason="Máy hỏng • chờ kỹ thuật viên";Navigation.Go(Agent,waitingMachine.WaitingPoint);StopWorking();return;}
                    if(waitingMachine.Inventory.Total>0)
                    {
                        var stock=g.StorageFor(crew.area);string output=waitingMachine.Inventory.Snapshot().FirstOrDefault(x=>waitingMachine.Inventory.Available(x.id)>0&&stock.Inventory.FreeFor(x.id)>0)?.id;
                        if(stock&&output!=null){waitingMachine.ReleaseOperator(GetEntityId());BeginMove(waitingMachine,stock,output,Mathf.Min(Carry.Capacity,waitingMachine.Inventory.Available(output)),false);return;}
                        Reason=stock?"Chờ kho / hàng đã được giữ chỗ":"Khu chưa có kho nhận thành phẩm";
                        StopWorking();return;
                    }
                    if(waitingMachine.Phase==MachinePhase.WaitingInput)
                    {
                        foreach(var input in waitingMachine.Recipe.inputs)
                        {
                            int missing=Mathf.Max(0,input.count-waitingMachine.Input.Available(input.id));if(missing==0)continue;
                            var supply=SupplyFor(input.id);
                            if(supply&&supply.Inventory.Available(input.id)>0)
                            {waitingMachine.ReleaseOperator(GetEntityId());BeginMove(supply,waitingMachine,input.id,Mathf.Min(missing,supply.Inventory.Available(input.id)),true);return;}
                            Reason="Chờ "+Definitions.Item(input.id).label+" trong kho "+GameHud.AreaLabel(SupplyArea(input.id));StopWorking();return;
                        }
                        Reason=waitingMachine.WaitReason;waitingMachine.ReleaseOperator(GetEntityId());return;
                    }
                }
                Vector3 point=working.WorkPoint;
                if((transform.position-point).sqrMagnitude>1.3f){Navigation.Go(Agent,point);Reason="Đến "+working.Label;return;}
                if (Agent.hasPath) Agent.ResetPath();
                if(working is MachineStation machine)
                {
                    Reason=machine.Phase==MachinePhase.Operating?"Đang vận hành":"Đang khởi động máy";
                    StopWorking();Reason="Máy tự chạy • tìm việc nạp/lấy hàng";
                    return;
                }
                if(working is TableStation table)
                {
                    Reason=table.Cleaning>0?"Đang dọn bàn":table.NeedsMeal?"Đang phục vụ":"Chờ bàn";
                    table.Work(Carry,Time.deltaTime*WorkforceRules.Productivity(crew.speedLevel),GetEntityId());
                    View.Work(table.Cleaning>0?"Cleaning":"Serving");
                    if(!table.NeedsMeal&&table.Cleaning<=0)StopWorking();
                    return;
                }
                if(working is ProductionStation animal&&animal.Animal&&animal.Feed<ProductionStation.MaximumFeed&&
                    Carry.Available(animal.FeedItem)==0&&animal.Inventory.Available(animal.FeedItem)==0)
                {
                    var supply=g.StorageFor(animal.AreaId);int needed=ProductionStation.MaximumFeed-animal.Feed-animal.Inventory.Available(animal.FeedItem);
                    if(supply&&supply.Inventory.Available(animal.FeedItem)>0){animal.ReleaseOperator(GetEntityId());BeginMove(supply,animal,animal.FeedItem,Mathf.Min(needed,supply.Inventory.Available(animal.FeedItem)),false);}
                    else
                    {
                        // Chăm vật nuôi gồm chuẩn bị thức ăn; lấy/nạp đều đi qua reservation chung.
                        var mixer=g.Machines.Find(x=>x.Id=="machine_feedmill"&&x.IsUnlocked);
                        if(mixer&&!mixer.Broken&&mixer.Inventory.Available(animal.FeedItem)>0){animal.ReleaseOperator(GetEntityId());BeginMove(mixer,animal,animal.FeedItem,Mathf.Min(needed,mixer.Inventory.Available(animal.FeedItem)),false);return;}
                        if(mixer&&!mixer.Broken&&!mixer.Running)
                            foreach(var input in mixer.Recipe.inputs)
                            {int missing=input.count-mixer.Input.Available(input.id);if(missing>0&&supply&&supply.Inventory.Available(input.id)>0){animal.ReleaseOperator(GetEntityId());BeginMove(supply,mixer,input.id,Mathf.Min(missing,supply.Inventory.Available(input.id)),true);return;}}
                        Reason=mixer&&mixer.Broken?"Máy thức ăn hỏng • chờ kỹ thuật viên":mixer&&mixer.Running?"Chờ mẻ thức ăn hoàn tất":"Thiếu nguyên liệu thức ăn";animal.ReleaseOperator(GetEntityId());Navigation.Go(Agent,animal.WaitingPoint);
                    }
                    return;
                }
                if(working is ProductionStation restock&&restock.Animal&&restock.Herd<ProductionStation.MaximumHerd&&restock.Feed>0&&restock.Breeding<=0)
                {var result=restock.RestockPlayer(GetEntityId());Reason=result.Worked?"Đang tái đàn":"Chờ tái đàn: "+result.Reason;return;}
                Reason=working is ProductionStation livestock&&livestock.Animal?"Đang chăm vật nuôi":"Đang làm việc";
                workDelta+=Time.deltaTime;if(workDelta<.1f)return;
                working.Work(Carry,workDelta*WorkforceRules.Productivity(crew.speedLevel),GetEntityId());workDelta=0;
                View.Work(ActorView.WorkState(working,InteractionKind.Operate));
                if(Carry.Total>0){StopWorking();BeginDrop(g.StorageFor(crew.area));}
                return;
            }
            if(Time.time<nextDecision)return;nextDecision=Time.time+.2f/WorkforceRules.Capability;
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
                    if (Agent.hasPath) Agent.ResetPath();
                    if(Checkout.Serve(Carry)){Deliveries++;Reason="Đang phục vụ";View.Work("Cashier");}
                    else Reason="Chờ khách tại quầy";
                    return;
                }
                var order=Checkout.FrontOrder;
                if(order==null){Reason="Chờ khách tại quầy";return;}
                if(order.Lines.Any(x=>x.Remaining>0&&Checkout.Inventory.Available(x.id)>0))
                {
                    if((transform.position-Checkout.WorkPoint).sqrMagnitude>1.3f){Navigation.Go(Agent,Checkout.WorkPoint);Reason="Đến phục vụ hàng tại quầy";return;}
                    if (Agent.hasPath) Agent.ResetPath();
                    Reason=Checkout.Serve(Checkout.Inventory,GetEntityId())?"Đang phục vụ":"Chờ khách tại quầy";
                    View.Work("Cashier");
                    return;
                }
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
            if(Role=="Repairer")
            {
                working=g.Machines.Find(m=>m.IsUnlocked&&m.Broken&&m.AreaId==crew.area&&!g.Workers.Exists(w=>w!=this&&w.working==m));
                if(working)Navigation.Go(Agent,working.WorkPoint);else{Reason="Chờ máy cần sửa trong khu";Navigation.Go(Agent,g.StorageFor(crew.area).WaitingPoint);}
                return;
            }
            if(Role is "Farmer" or "AnimalWorker")
            {
                var list=g.Producers.FindAll(x=>x.IsUnlocked&&x.AreaId==crew.area&&(Role=="Farmer"?!x.Animal:x.Animal));
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
                for(int i=0;i<list.Count;i++)
                {
                    var m=list[(Math.Abs(Slot)+cursor+i)%list.Count];var stock=g.StorageFor(crew.area);
                    bool output=m.Inventory.Snapshot().Any(x=>m.Inventory.Available(x.id)>0&&stock.Inventory.FreeFor(x.id)>0);
                    bool input=!m.Broken&&!m.Running&&m.Recipe.inputs.Any(x=>m.Input.Available(x.id)<x.count&&m.Input.FreeFor(x.id)>0&&SupplyFor(x.id));
                    if(m.Broken||!output&&!input)continue;
                    working=m;cursor=(cursor+i+1)%list.Count;Navigation.Go(Agent,m.WorkPoint);return;
                }
                Reason=list.Any(x=>x.Broken)?"Máy hỏng • chờ kỹ thuật viên":list.Any(x=>x.Inventory.Total>0)?"Chờ kho còn chỗ":"Chờ nguyên liệu / máy tự chạy";Navigation.Go(Agent,g.StorageFor(crew.area).WaitingPoint);
                return;
            }
            if(Role=="Waiter")
            {
                var dirty=g.Tables.Find(x=>x.IsUnlocked&&x.AreaId==crew.area&&x.Cleaning>0&&!g.Workers.Exists(w=>w!=this&&w.working==x));
                if(dirty){working=dirty;Navigation.Go(Agent,dirty.WorkPoint);return;}
                var table=g.Tables.Find(x=>x.AreaId==crew.area&&x.NeedsMeal&&!g.Workers.Exists(w=>w!=this&&w.destination==x&&w.phase>0));
                if(table){string wanted=table.Occupant.WantedItem;var food=g.Machines.Find(x=>x.AreaId=="restaurant"&&x.Inventory.Available(wanted)>0) as Station??g.StorageFor("restaurant");BeginMove(food,table,wanted,1,false);}
                return;
            }
            FindRoute();
        }
        int Slot { get { if(WorkerId!=null&&int.TryParse(WorkerId.Substring(WorkerId.LastIndexOf(':')+1),out var n))return n;return 0; } }
        void OnDisable()
        {
            if (Retired) return;
            StopWorking();
            CancelTransport();
        }
    }
}

