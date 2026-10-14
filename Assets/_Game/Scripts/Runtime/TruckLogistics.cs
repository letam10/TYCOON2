using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Tycoon
{
    public sealed partial class TruckLogistics:MonoBehaviour
    {
        GameSession game=>GameSession.Instance;
        TruckRuntimeState State=>game.Transactions?.View.truck;
        public Transform Vehicle {get;private set;}
        public string Reason {get;private set;}="Chưa mua xe tải";
        readonly Dictionary<string,GameObject> visuals=new();
        float elapsedTick;
        readonly Dictionary<string,float> handling=new();
        public bool HasCargo(WorkerAgent worker)=>game.Transactions.View.crates.Any(x=>x.holder==game.Transactions.Actor(worker.GetEntityId()));
        public bool Select(string source,string destination,bool repeat)
        {
            var c=game.Transactions.Command(TransactionKind.SelectTruckRoute,"player");c.source=source;c.destination=destination;c.quantity=repeat?1:0;
            bool ok=game.Transactions.TryExecute(c,out _);Reason=ok?"Đã chọn tuyến":game.Transactions.LastReason;return ok;
        }
        public bool Dispatch(string actor="player")
        {
            if(State==null)return false;string target=State.current!=State.source?State.source:State.destination;
            var c=game.Transactions.Command(TransactionKind.DispatchTruck,actor);c.path=TruckRoutes.Path(State.current,target);
            bool ok=game.Transactions.TryExecute(c,out _);Reason=ok?"Xe đang đi":game.Transactions.LastReason;return ok;
        }
        bool Act(TransactionKind kind,string actor,string box=null,string source=null,string item=null,int count=0)
        {
            var c=game.Transactions.Command(kind,actor,box);c.source=source;c.item=item;c.quantity=count;
            bool ok=game.Transactions.TryExecute(c,out _);if(!ok)Reason=game.Transactions.LastReason;return ok;
        }
        public InteractionResult Interact(CargoDock dock,InteractionContext context)
        {
            if(State==null)return InteractionResult.Reject("Cần mua xe + tài xế tại Processing.");
            if(State.current!=dock.WarehouseId||State.phase=="Travelling")return InteractionResult.Reject("Xe chưa ở bến này.");
            var crates=game.Transactions.View.crates;
            if(game.Transactions.View.schemaVersion>=3)return PhysicalInteract(dock,context);
            if(State.phase=="WaitingUnload")
            {var box=crates.FirstOrDefault(x=>x.holder==State.id);return box==null?InteractionResult.Reject("Nhân viên đang dỡ hàng."):new(Act(TransactionKind.UnloadCrate,"player",box.id));}
            if(context.Carry.Total>0)
            {var item=context.Carry.Snapshot()[0];return new(Act(TransactionKind.PackCrate,"player","crate:"+System.Guid.NewGuid().ToString("N"),"player",item.id,Mathf.Min(6,item.count)));}
            var ready=crates.FirstOrDefault(x=>x.holder=="dock:"+dock.WarehouseId);
            return ready!=null?new(Act(TransactionKind.LoadCrate,"player",ready.id)):InteractionResult.Reject("Mang hàng tới bến để đóng thùng; chọn tuyến và gửi xe trên HUD.");
        }
        public void Loader(WorkerAgent worker,CrewState crew)
        {
            using var timing = QaFrameProbe.Measure("TruckLogistics.Loader");
            if(State==null||State.phase=="Travelling"||game.StorageFor(crew.area)?.Id!=State.current){worker.Reason="Chờ xe tại bến của khu";return;}
            string actor=game.Transactions.Actor(worker.GetEntityId());var crates=game.Transactions.View.crates;
            var held=crates.FirstOrDefault(x=>x.holder==actor);Vector3 dock=TruckRoutes.Dock(State.current);
            Vector3 pickupPoint = HandlingPoint(worker, dock, true);
            if(held!=null&&(worker.transform.position-pickupPoint).sqrMagnitude<1.2f)
            {
                int carried=crates.Where(x=>x.holder==actor).Sum(x=>game.Transactions.View.stacks.Where(s=>s.owner==x.id).Sum(s=>s.quantity));
                var extra=crates.FirstOrDefault(x=>x.item==held.item&&(held.task=="load"?x.holder=="dock:"+State.current:x.holder==State.id)&&carried+game.Transactions.View.stacks.Where(s=>s.owner==x.id).Sum(s=>s.quantity)<=worker.Carry.Capacity);
                if(extra!=null){worker.Agent.ResetPath();if(Act(TransactionKind.ClaimCrate,actor,extra.id))return;}
            }
            Vector3 goal=held==null?pickupPoint:HandlingPoint(worker, dock, false);
            if((worker.transform.position-goal).sqrMagnitude>1.2f){Navigation.Go(worker.Agent,goal);worker.Reason=held==null?"Đến lấy thùng":"Mang thùng đến xe/kho";return;}
            worker.Agent.ResetPath();
            float elapsed = handling.TryGetValue(actor, out float time) ? time : 0;
            elapsed += Time.deltaTime * WorkforceRules.Productivity(crew.speedLevel);
            handling[actor] = elapsed;
            if(elapsed<.7f){worker.Reason="Đang bốc hàng";return;}handling[actor]=0;
            if(held!=null){Act(held.task=="load"?TransactionKind.LoadCrate:TransactionKind.UnloadCrate,actor,held.id);worker.View.Interact(false);return;}
            var ready=crates.FirstOrDefault(x=>State.phase=="WaitingUnload"?x.holder==State.id:x.holder=="dock:"+State.current);
            if(ready!=null){Act(TransactionKind.ClaimCrate,actor,ready.id);worker.View.Interact(true);return;}
            if(State.current==State.source&&State.phase is "Idle" or "Loading"&&crates.Count(x=>x.holder==State.id)<6)
            {
                var stock=game.StorageFor(crew.area);
                foreach(var item in Definitions.Items)
                {
                    int available=stock.AvailableAboveReserve(item.id);var target=game.Stations.Find(x=>x.Id==State.destination) as StorageStation;
                    if(available<=0||!target||target.Inventory.FreeFor(item.id)<=0)continue;
                    if(Act(TransactionKind.PackCrate,actor,"crate:"+System.Guid.NewGuid().ToString("N"),stock.Id,item.id,Mathf.Min(6,available)))return;
                }
            }
            worker.Reason=Reason="Chờ hàng khả dụng hoặc chỗ trên xe";
        }
        void Update()
        {
            using var updateProbe = QaFrameProbe.Measure("TruckLogistics.1.Update");

            if(State==null)return;
            if(Vehicle==null){Vehicle=BuildTruck();Vehicle.position=TruckRoutes.Position(State);}
            if(!game.CanSimulate)return;
            elapsedTick+=Time.deltaTime;if(elapsedTick<.2f)return;float elapsed=elapsedTick;elapsedTick=0;
            if(State.phase=="Travelling")
            {
                var point=TruckRoutes.Position(State);var next=State.Copy();next.travelled=System.Math.Min(next.distance,next.travelled+3);var direction=TruckRoutes.Position(next)-point;
                if(direction.sqrMagnitude>.01f&&People().Any(p=>Vector3.Dot(p-point,direction.normalized)>0&&Vector3.Dot(p-point,direction.normalized)<4&&Vector3.Cross(p-point,direction.normalized).magnitude<1.5f))
                {Reason="Chờ nhường đường tại giao lộ / người qua đường";return;}
                var c=game.Transactions.Command(TransactionKind.TickTruck,"simulation");c.secondary=State.trip;c.duration=elapsed;game.Transactions.TryExecute(c,out _);Reason="Đến kho "+GameHud.AreaLabel(game.Stations.Find(x=>x.Id==State.tripTarget)?.AreaId??"");
            }
            else if(State.repeat&&State.phase is "Idle" or "Loading")
            {
                int count=game.Transactions.View.crates.Count(x=>x.holder==State.id);
                if(State.current!=State.source||count>0&&!game.Transactions.View.crates.Any(x=>x.task=="load"))Dispatch("simulation");
            }
        }
        void LateUpdate()
        {
            using var lateUpdateProbe = QaFrameProbe.Measure("TruckLogistics.1.LateUpdate");
            if(State==null||Vehicle==null)return;
            UpdateCargoPresentation();
            var position=TruckRoutes.Position(State);var direction=position-Vehicle.position;
            if(direction.sqrMagnitude>.005f)Vehicle.rotation=Quaternion.RotateTowards(Vehicle.rotation,Quaternion.LookRotation(direction),150*Time.deltaTime);
            Vehicle.position = Vector3.MoveTowards(Vehicle.position, position,
                6 * WorkforceRules.Capability * Time.deltaTime);
            AnimateTruckWheels();
            var crates=game.Transactions.View.crates;
            foreach(var removed in visuals.Keys.Where(id=>!crates.Any(x=>x.id==id)).ToArray()){Destroy(visuals[removed]);visuals.Remove(removed);}
            int cargo=0;var ground=new Dictionary<string,int>();var hands=new Dictionary<string,int>();
            foreach(var box in crates)
            {
                // Load có thể hủy nhân viên và model con; crate vẫn thuộc transaction.
                if (!visuals.TryGetValue(box.id, out var model) || !model)
                {
                    model=new GameObject(box.id);model.transform.SetParent(transform);HandItemModels.Build("crate",model.transform);
                    var icon=new GameObject("CrateItemIcon").AddComponent<SpriteRenderer>();icon.transform.SetParent(model.transform,false);icon.transform.localPosition=new(0,.25f,.44f);icon.transform.localScale=Vector3.one*.45f;icon.sprite=ItemIconAtlas.Get(box.item);
                    int quantity = game.Transactions.View.stacks.Where(x => x.owner == box.id).Sum(x => x.quantity);
                    Art.Label("×" + quantity, new(0, .75f, 0), model.transform, .14f);
                    visuals[box.id] = model;
                }
                model.transform.localScale = Vector3.one *
                    (box.holder != State.id && !box.holder.StartsWith("dock:") ? .8f : 1);
                if (box.holder == State.id)
                {
                    model.transform.SetParent(Vehicle, false);
                    model.transform.localPosition = new((cargo % 2 - .5f) * .70f, .928f,
                        -.14f - cargo / 2 * .68f);
                    cargo++;
                }
                else if(box.holder.StartsWith("dock:")){int n=ground.TryGetValue(box.holder,out int value)?value:0;ground[box.holder]=n+1;model.transform.SetParent(transform);model.transform.position=TruckRoutes.Dock(box.holder.Substring(5))+new Vector3(-1.7f,.1f+n*.7f,0);}
                else if(box.holder=="player")
                {
                    int n = hands.TryGetValue("player", out int count) ? count : 0;
                    hands["player"] = n + 1;
                    model.transform.SetParent(game.Player.transform, false);
                    model.transform.localPosition = HeldCratePosition(n, .9f);
                }
                else
                {
                    var worker = game.Workers.Find(w => game.Transactions.Actor(w.GetEntityId()) == box.holder);
                    if (worker)
                    {
                        int n = hands.TryGetValue(box.holder, out int value) ? value : 0;
                        hands[box.holder] = n + 1;
                        model.transform.SetParent(worker.transform, false);
                        model.transform.localPosition = HeldCratePosition(n, .8f);
                    }
                }
            }
        }
        static Vector3 HeldCratePosition(int index, float height) =>
            new((index % 2 - .5f) * .54f, height + index / 6 * .35f, 1.13f + (index / 2 % 3 - 1) * .47f);
        IEnumerable<Vector3> People()
        {
            yield return game.Player.transform.position;
            foreach(var c in game.Commerce.Customers)if(c&&c.gameObject.activeInHierarchy)yield return c.transform.position;
            foreach(var d in game.Restaurant.Diners)if(d&&d.gameObject.activeInHierarchy)yield return d.transform.position;
            foreach(var w in game.Workers)if(w&&w.Role!="Driver")yield return w.transform.position;
        }
    }
}
