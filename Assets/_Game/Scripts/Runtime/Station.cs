using System;
using UnityEngine;
namespace Tycoon
{
    [Serializable] public sealed class StationProgressSave
    {
        public string id; public int level=1,workCount,phase,herd=3,feed,batches,repairFee;public float remaining,action,cycle,breeding,repairRemaining;public bool running,broken,repairPaid;public float produced;
    }
    public abstract class Station:MonoBehaviour,IPlayerInteractionTarget
    {
        public string Id,Label;public string Requirement="",AreaId="farm";int level=1,workCount;public Vector3 InteractionPoint;
        protected RuntimeTransactions Authority=>GameSession.Instance?GameSession.Instance.Transactions:null;
        protected StationRuntimeState Runtime=>Authority?.Station(Id);
        public int Level { get=>Runtime?.level??level;set{if(Runtime!=null&&value!=Level)throw new InvalidOperationException("Station level chỉ được cập nhật qua transaction.");level=value;} }
        public int WorkCount { get=>Runtime?.workCount??workCount;set{if(Runtime!=null&&value!=WorkCount)throw new InvalidOperationException("Station work chỉ được cập nhật qua transaction.");workCount=value;} }
        public float InteractionRadius=1.15f;public bool PlayerUsesZones;
        public bool ContainsInteractionPoint(Vector3 position){if(Mathf.Abs(position.y-InteractionPoint.y)>1.2f)return false;position.y=InteractionPoint.y;return (position-InteractionPoint).sqrMagnitude<=InteractionRadius*InteractionRadius;}
        [NonSerialized]public Inventory Inventory;public TextMesh StatusLabel;protected EntityId operatorId;protected float operatorUntil;
        public bool IsUnlocked=>GameSession.Instance!=null&&GameSession.Instance.CanSimulate&&GameSession.Instance.Economy.Has(Requirement);
        public virtual string Prompt=>Label;
        public abstract bool Interact(PlayerController player,bool withdraw);
        protected bool Lease(EntityId actorId){if(operatorId!=actorId&&Time.time<operatorUntil)return false;operatorId=actorId;operatorUntil=Time.time+.08f;return true;}
        public void ReleaseOperator(EntityId actorId){if(Authority!=null){Authority.ReleaseOperator(this,actorId);return;}if(operatorId==actorId)operatorUntil=0;}
        public virtual bool Work(Inventory carrier,float delta,EntityId actorId)=>false;
        public virtual InteractionResult Perform(InteractionKind kind,InteractionContext context,float delta)=>StationPlayerActions.Execute(this,kind,context,delta);
        public virtual void EndInteraction(EntityId actor){ReleaseOperator(actor);if(this is PurchasePad pad)pad.StopContributing();}
        public virtual StationProgressSave CaptureProgress()=>new(){id=Id,level=Level,workCount=WorkCount};
        public virtual void RestoreProgress(StationProgressSave s){Level=s.level;WorkCount=s.workCount;operatorUntil=0;}
        public bool IsOperatedByOther(EntityId actorId)=>Authority!=null?Authority.OperatedByOther(this,actorId):operatorId!=actorId&&Time.time<operatorUntil;
        protected virtual void LateUpdate(){if(StatusLabel){StatusLabel.text=Prompt;StatusLabel.gameObject.SetActive(!PlayerUsesZones&&IsUnlocked&&GameSession.Instance.Player&&(GameSession.Instance.Player.transform.position-InteractionPoint).sqrMagnitude<36);}}
    }
    public sealed class StationZone:Station,IPlayerInteractionArea
    {
        public Station Target;public string Mode;
        float nextTransfer,operationDelta;
        public InteractionKind Kind=>Mode switch{"withdraw"=>InteractionKind.Pickup,"deposit"=>InteractionKind.Drop,"operate"=>InteractionKind.Operate,"serve"=>InteractionKind.Serve,"cash"=>InteractionKind.Collect,_=>throw new InvalidOperationException("Unknown interaction zone: "+Mode)};
        public Vector3 Center=>InteractionPoint;
        public bool Available=>this&&isActiveAndEnabled&&Target&&Target.IsUnlocked;
        public bool Contains(Vector3 point)=>ContainsInteractionPoint(point);
        public InteractionResult Perform(InteractionContext context,float delta)
        {
            if(!Available||context.Carry==null||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return InteractionResult.Waiting;
            if(context.Player&&!Contains(context.Player.transform.position))return InteractionResult.Reject("Hãy đứng trong vùng thao tác.");
            nextTransfer=Mathf.Max(0,nextTransfer-delta);
            if(nextTransfer>0)return InteractionResult.Waiting;
            if(Kind==InteractionKind.Operate||Kind==InteractionKind.Pickup&&Target is ProductionStation)
            {operationDelta+=delta;if(operationDelta<.1f)return InteractionResult.Waiting;delta=operationDelta;operationDelta=0;}
            int before=context.Carry.Total;
            var result=Target.Perform(Kind,context,delta);
            if(result.Worked&&(context.Carry.Total!=before||Kind is InteractionKind.Serve or InteractionKind.Collect))nextTransfer=.12f;
            return result;
        }
        public void Exit(EntityId actor){nextTransfer=operationDelta=0;if(Target)Target.EndInteraction(actor);}
        public override string Prompt=>!Target?"":Mode=="deposit"?"Đặt hàng • "+Target.Label:Mode=="withdraw"?"Lấy hàng • "+Target.Label:Mode=="serve"?"Giao hàng • "+Target.Label:Mode=="cash"?"Thu "+(Target as CheckoutStation)?.Cash+" xu":Target.Prompt;
        protected override void LateUpdate()
        {if(StatusLabel){StatusLabel.text=Prompt;StatusLabel.gameObject.SetActive(Available&&GameSession.Instance.Player&&Contains(GameSession.Instance.Player.transform.position));}}
        public override bool Interact(PlayerController player,bool withdraw)
        {
            if(player==null||!ContainsInteractionPoint(player.transform.position))return false;
            return Perform(new InteractionContext(player,Definitions.Items[GameSession.Instance.SelectedItem].id),Time.deltaTime).Worked;
        }
        public override bool Work(Inventory carrier,float delta,EntityId actorId)
        {
            return Perform(new InteractionContext(carrier,actorId,Definitions.Items[GameSession.Instance.SelectedItem].id),delta).Worked;
        }
    }
    public sealed class ProductionStation:Station
    {
        public string ItemId;public float Interval=5;public int Yield=1;public Transform[] Plants;
        float remaining,produced,action,cycle=30,breeding;int phase,herd=3,feed;float attendedUntil,tickDelta;
        public float Remaining {get=>Runtime?.progress.remaining??remaining;set{GuardState();remaining=value;}}
        public float Produced {get=>Runtime?.progress.produced??produced;private set{GuardState();produced=value;}}
        public int Phase {get=>Runtime?.progress.phase??phase;set{GuardState();phase=value;}}
        public int Herd {get=>Runtime?.progress.herd??herd;set{GuardState();herd=value;}}
        public int Feed {get=>Runtime?.progress.feed??feed;set{GuardState();feed=value;}}
        public float Action {get=>Runtime?.progress.action??action;set{GuardState();action=value;}}
        public float Cycle {get=>Runtime?.progress.cycle??cycle;set{GuardState();cycle=value;}}
        public float Breeding {get=>Runtime?.progress.breeding??breeding;set{GuardState();breeding=value;}}
        void GuardState(){if(Runtime!=null)throw new InvalidOperationException("Producer chỉ được cập nhật qua transaction.");}
        public bool Animal=>ItemId is "milk" or "egg" or "beef";
        public override string Prompt=>!IsUnlocked?Label+" • chưa mở":Animal?Label+" • đàn "+Herd+" • thức ăn "+Feed+" • "+(Feed==0?"Cần cà rốt":Herd==0?"Đang tái đàn":Cycle.ToString("0")+"s"):Label+" • "+(Phase==0?"Đứng gieo hạt":Phase==1?"Đứng tưới":Phase==2?"Đang lớn "+Remaining.ToString("0.0")+"s":"Đứng thu hoạch")+" • cấp "+Level;
        void Update(){Tick(Time.deltaTime);}
        public void Tick(float delta)
        {
            if(!IsUnlocked||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return;
            if(Authority!=null)
            {
                tickDelta+=delta;if(tickDelta>=.2f){Authority.TickProducer(this,tickDelta);tickDelta=0;}
                RefreshPlants();return;
            }
            if(!Animal)
            {
                if(Phase==2){Remaining=Mathf.Max(0,Remaining-delta);if(Remaining==0)Phase=3;}
                if(Plants!=null)foreach(var plant in Plants)if(plant)plant.localScale=Vector3.one*(Phase==0?.08f:Phase==1?.2f:Phase==2?Mathf.Lerp(1,.2f,Remaining/2):1);
                return;
            }
            if(Breeding>0){Breeding=Mathf.Max(0,Breeding-delta);if(Breeding==0)Herd++;}
            if(Herd==0||Feed==0)return;
            Cycle=Mathf.Max(0,Cycle-delta*(Time.time<attendedUntil?2:1));Remaining=Cycle;
        }
        public InteractionResult OperatePlayer(float delta,EntityId actor)
        {
            if(!IsUnlocked||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return InteractionResult.Waiting;
            if(Authority!=null)return Authority.StationAction(TransactionKind.OperateProducer,this,actor,delta)?InteractionResult.Success:InteractionResult.Reject(Authority.LastReason);
            if(!Animal&&Phase==3)return InteractionResult.Reject("Cây đã chín; hãy sang vùng Lấy hàng.");
            if(!Animal&&Phase==2)return InteractionResult.Reject("Cây đang lớn.");
            if(!Lease(actor))return InteractionResult.Reject("Trạm đang có người vận hành.");
            if(Animal)
            {
                if(Herd<3&&Breeding==0&&Feed>0){Feed--;Breeding=60;return InteractionResult.Success;}
                if(Feed==0)return InteractionResult.Reject("Hãy đặt cà rốt vào vùng Cho ăn.");
                if(Herd==0)return InteractionResult.Reject("Chuồng đang tái đàn.");
                attendedUntil=Time.time+.1f;
                return InteractionResult.Success;
            }
            Action+=delta;
            if(Action>=1){Action=0;if(Phase==0)Phase=1;else{Phase=2;Remaining=GameSession.Instance.Progression.FarmGrowSeconds;}}
            return InteractionResult.Success;
        }
        public InteractionResult PickupPlayer(Inventory carry,float delta,EntityId actor)
        {
            if(!IsUnlocked||carry==null||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return InteractionResult.Waiting;
            if(carry.FreeFor(ItemId)<1)return StationPlayerActions.CannotCarry(carry,ItemId);
            if(Authority!=null)return Authority.StationAction(TransactionKind.HarvestProducer,this,actor,delta,carry)?InteractionResult.Success:InteractionResult.Reject(Authority.LastReason);
            if(!Animal&&Phase!=3)return InteractionResult.Reject("Chưa đến lúc thu hoạch; hãy gieo và tưới ở vùng Làm việc.");
            if(Animal&&(Feed==0||Herd==0||Cycle>0))return InteractionResult.Reject("Chuồng chưa có sản phẩm để lấy.");
            if(!Lease(actor))return InteractionResult.Reject("Trạm đang có người vận hành.");
            Action+=delta;
            if(Action<1)return InteractionResult.Success;
            int amount=Animal&&ItemId=="beef"?Mathf.Min(3,carry.FreeFor(ItemId)):1;
            if(!carry.TryAdd(ItemId,amount))return StationPlayerActions.CannotCarry(carry,ItemId);
            Action=0;Produced+=amount;WorkCount++;
            if(Animal){if(ItemId=="beef")Herd--;Feed--;Cycle=ItemId=="beef"?45:30;}
            else Phase=0;
            return InteractionResult.Success;
        }
        public InteractionResult FeedPlayer(Inventory carry,EntityId actor)
        {
            if(!IsUnlocked||!Animal||carry==null)return InteractionResult.Waiting;
            if(Authority!=null)return Authority.StationAction(TransactionKind.FeedProducer,this,actor,carrier:carry)?InteractionResult.Success:InteractionResult.Reject(Authority.LastReason);
            if(Feed>=3)return InteractionResult.Reject("Máng ăn đã đầy.");
            if(carry.Available("carrot")==0)return InteractionResult.Reject("Chuồng chỉ nhận cà rốt làm thức ăn.");
            if(!Lease(actor))return InteractionResult.Reject("Trạm đang có người vận hành.");
            if(!carry.TryRemove("carrot",1))return InteractionResult.Waiting;
            Feed++;WorkCount++;return InteractionResult.Success;
        }
        public override void EndInteraction(EntityId actor)
        {
            if(operatorId==actor)attendedUntil=0;
            base.EndInteraction(actor);
        }
        public override bool Work(Inventory carrier,float delta,EntityId actorId)
        {
            if(Authority!=null)
            {
                if(!IsUnlocked||carrier==null||delta<=0||!float.IsFinite(delta))return false;
                if(Animal&&Feed<3&&carrier.Available("carrot")>0)return FeedPlayer(carrier,actorId).Worked;
                if(!Animal&&Phase==3||Animal&&Cycle==0&&Herd>0&&Feed>0)return PickupPlayer(carrier,delta,actorId).Worked;
                return OperatePlayer(delta,actorId).Worked;
            }
            if(!IsUnlocked||carrier==null||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta)||!Lease(actorId))return false;
            if(Animal)
            {
                if(Feed<3)
                {
                    // Thức ăn được giữ riêng tại chuồng; nhân viên lấy từ kho rồi đặt vào đây.
                    if(carrier.TryRemove("carrot",1)||(Inventory!=null&&Inventory.TryRemove("carrot",1))){Feed++;WorkCount++;return true;}
                }
                if(Herd<3&&Breeding==0&&Feed>0){Feed--;Breeding=60;return true;}
                if(Feed==0)return false;
                attendedUntil=Time.time+.1f;
                if(Herd==0||Cycle>0||carrier.FreeFor(ItemId)<=0)return false;
                Action+=delta;if(Action<1)return true;
                int amount=ItemId=="beef"?Mathf.Min(3,carrier.FreeFor(ItemId)):1;
                if(!carrier.TryAdd(ItemId,amount))return false;
                if(ItemId=="beef")Herd--;Feed--;Produced+=amount;WorkCount++;Action=0;Cycle=ItemId=="beef"?45:30;return true;
            }
            if(Phase==2){ReleaseOperator(actorId);return false;}
            if(Phase==3&&carrier.FreeFor(ItemId)<1)return false;
            Action+=delta;
            if(Action<1)return true;
            Action=0;
            if(Phase==0)Phase=1;
            else if(Phase==1){Phase=2;Remaining=GameSession.Instance.Progression.FarmGrowSeconds;}
            else if(carrier.TryAdd(ItemId,1)){Phase=0;Produced++;WorkCount++;}
            return true;
        }
        public override bool Interact(PlayerController player,bool withdraw)=>player&&ContainsInteractionPoint(player.transform.position)&&Work(player.Carry,Time.deltaTime,player.GetEntityId());
        void RefreshPlants(){if(Plants!=null)foreach(var plant in Plants)if(plant)plant.localScale=Vector3.one*(Phase==0?.08f:Phase==1?.2f:Phase==2?Mathf.Lerp(1,.2f,Remaining/2):1);}
        public override StationProgressSave CaptureProgress(){var s=base.CaptureProgress();s.phase=Phase;s.herd=Herd;s.feed=Feed;s.remaining=Remaining;s.action=Action;s.cycle=Cycle;s.breeding=Breeding;s.produced=Produced;return s;}
        public override void RestoreProgress(StationProgressSave s){base.RestoreProgress(s);Phase=s.phase;Herd=s.herd;Feed=s.feed;Remaining=s.remaining;Action=s.action;Cycle=s.cycle;Breeding=s.breeding;Produced=s.produced;attendedUntil=0;}
    }
    public sealed class StorageStation:Station
    {
        public override string Prompt=>Label+" • "+Inventory.Total+"/"+Inventory.Capacity+" • chọn vùng Lấy hoặc Đặt hàng";
        public override bool Interact(PlayerController player,bool withdraw)
        {
            if(!IsUnlocked)return false;
            if(withdraw)return Inventory.Transfer(Inventory,player.Carry,Definitions.Items[GameSession.Instance.SelectedItem].id,1)>0;
            foreach(var row in player.Carry.Snapshot())return Inventory.Transfer(player.Carry,Inventory,row.id,1)>0;
            return false;
        }
    }
    public sealed class ShelfStation:Station
    {
        public string[] AllowedItems;public string ShopId;
        public bool Accepts(string id)=>Array.IndexOf(AllowedItems,id)>=0;
        public override string Prompt=>Label+" • hàng dự trữ "+Inventory.Total+"/"+Inventory.Capacity;
        public override bool Interact(PlayerController player,bool withdraw)
        {
            if(!IsUnlocked)return false;
            foreach(var id in AllowedItems)if(Inventory.Transfer(withdraw?Inventory:player.Carry,withdraw?player.Carry:Inventory,id,1)>0){if(Authority==null)WorkCount++;return true;}
            return false;
        }
    }
    public sealed class PurchasePad:Station,IPlayerInteractionArea
    {
        public UpgradeDefinition Upgrade;float held;PurchaseState previousState;bool hasVisualState;
        public InteractionKind Kind=>InteractionKind.Purchase;
        public Vector3 Center=>InteractionPoint;
        public PurchaseEvaluation Evaluation=>GameSession.Instance.Progression.Evaluate(Upgrade);
        public bool Available=>this&&isActiveAndEnabled&&GameSession.Instance!=null&&Evaluation.CanContribute;
        public bool Contains(Vector3 point)=>ContainsInteractionPoint(point);
        public InteractionResult Perform(InteractionContext context,float delta)=>context.Player&&!Contains(context.Player.transform.position)?InteractionResult.Reject("Hãy đứng trong vùng mua."):Perform(Kind,context,delta);
        public void Exit(EntityId actor)=>EndInteraction(actor);
        public bool HoldToBuy(float delta)
        {
            var game=GameSession.Instance;
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta)||!game.CanPurchase(Upgrade,out _)||game.Economy.Money<=0){held=0;return false;}
            held+=delta*35;int amount=Mathf.FloorToInt(held);
            if(amount==0)return false;
            held-=amount;return game.Contribute(Upgrade,amount)>0;
        }
        public void StopContributing()=>held=0;
        public override string Prompt=>StatusText(Evaluation);
        public override bool Interact(PlayerController player,bool withdraw)=>!withdraw&&player&&ContainsInteractionPoint(player.transform.position)&&HoldToBuy(Time.deltaTime);
        protected override void LateUpdate()
        {
            var g=GameSession.Instance;var evaluation=Evaluation;bool visible=evaluation.State!=PurchaseState.Locked;
            if(!hasVisualState||previousState!=evaluation.State)
            {foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=visible;previousState=evaluation.State;hasVisualState=true;}
            if((g.Player.transform.position-InteractionPoint).sqrMagnitude>3)held=0;
            if(StatusLabel){bool nearby=(g.Player.transform.position-InteractionPoint).sqrMagnitude<36;StatusLabel.gameObject.SetActive(visible&&nearby);if(visible&&nearby)StatusLabel.text=StatusText(evaluation);}
        }
        static string StatusText(PurchaseEvaluation e)=>e.State switch
        {
            PurchaseState.Locked=>"LOCKED • "+string.Join(" ",e.Requirements),
            PurchaseState.Available=>"AVAILABLE • "+e.Contributed+"/"+e.Cost+" xu",
            PurchaseState.Contributing=>"CONTRIBUTING • "+e.Contributed+"/"+e.Cost+" xu",
            _=>"PURCHASED"
        };
    }
}


