using System;
using UnityEngine;
namespace Tycoon
{
    [Serializable] public sealed class StationProgressSave
    {
        public string id; public int level=1,workCount,phase,herd=3,feed,batches,repairFee;public float remaining,action,cycle,breeding,repairRemaining;public bool running,broken,repairPaid;public float produced;
    }
    public abstract class Station:MonoBehaviour
    {
        public string Id,Label;public string Requirement="",AreaId="farm";public int Level=1,WorkCount;public Vector3 InteractionPoint;
        public float InteractionRadius=1.15f;public bool PlayerUsesZones;
        public bool ContainsInteractionPoint(Vector3 position){position.y=InteractionPoint.y;return (position-InteractionPoint).sqrMagnitude<=InteractionRadius*InteractionRadius;}
        [NonSerialized]public Inventory Inventory;public TextMesh StatusLabel;protected EntityId operatorId;protected float operatorUntil;
        public bool IsUnlocked=>GameSession.Instance!=null&&GameSession.Instance.Economy.Has(Requirement);
        public virtual string Prompt=>Label;
        public abstract bool Interact(PlayerController player,bool withdraw);
        protected bool Lease(EntityId actorId){if(operatorId!=actorId&&Time.time<operatorUntil)return false;operatorId=actorId;operatorUntil=Time.time+.08f;return true;}
        public void ReleaseOperator(EntityId actorId){if(operatorId==actorId)operatorUntil=0;}
        public virtual bool Work(Inventory carrier,float delta,EntityId actorId)=>false;
        public virtual StationProgressSave CaptureProgress()=>new(){id=Id,level=Level,workCount=WorkCount};
        public virtual void RestoreProgress(StationProgressSave s){Level=s.level;WorkCount=s.workCount;operatorUntil=0;}
        public bool IsOperatedByOther(EntityId actorId)=>operatorId!=actorId&&Time.time<operatorUntil;
        protected virtual void LateUpdate(){if(StatusLabel){StatusLabel.text=Prompt;StatusLabel.gameObject.SetActive(IsUnlocked&&GameSession.Instance.Player&&(GameSession.Instance.Player.transform.position-InteractionPoint).sqrMagnitude<36);}}
    }
    public sealed class StationZone:Station
    {
        public Station Target;public string Mode;
        float nextTransfer;
        public override string Prompt=>!Target?"":Mode=="deposit"?"Cất hàng • "+Target.Label:Mode=="withdraw"?"Lấy "+Definitions.Items[GameSession.Instance.SelectedItem].label+" • "+Target.Label:Mode=="serve"?"Giao đơn • "+(Target as CheckoutStation)?.FrontOrder?.RemainingQuantity+" còn thiếu":Mode=="cash"?"Thu "+(Target as CheckoutStation)?.Cash+" xu":Target.Prompt;
        public override bool Interact(PlayerController player,bool withdraw)
        {
            if(!Target||!Target.IsUnlocked||player==null||!ContainsInteractionPoint(player.transform.position))return false;
            if(Mode=="cash"&&Target is CheckoutStation cashDesk)return cashDesk.CollectCash(player)>0;
            return Work(player.Carry,Time.deltaTime,player.GetEntityId());
        }
        public override bool Work(Inventory carrier,float delta,EntityId actorId)
        {
            if(!Target||!Target.IsUnlocked||carrier==null)return false;
            if(Mode=="cash")return false;
            if(Mode=="operate"&&Target is MachineStation operating)return operating.Broken?operating.Repair(GameSession.Instance.Economy,delta,actorId):operating.Operate(delta,actorId);
            if(Time.time<nextTransfer)return false;
            if(Mode=="serve"&&Target is CheckoutStation counter){nextTransfer=Time.time+.15f;return counter.Serve(carrier);}
            if(Mode=="deposit")
            {
                foreach(var row in carrier.Snapshot())
                {
                    if(Target is ShelfStation shelf&&!shelf.Accepts(row.id))continue;
                    if(Target is MachineStation inputMachine&&!Array.Exists(inputMachine.Recipe.inputs,x=>x.id==row.id))continue;
                    Inventory destination=Target is MachineStation machine?machine.Input:Target.Inventory;
                    if(destination==null)return false;
                    nextTransfer=Time.time+.12f;
                    bool moved=Inventory.Transfer(carrier,destination,row.id,1)>0;
                    if(moved)Target.WorkCount++;
                    return moved;
                }
                return false;
            }
            if(Mode=="withdraw")
            {
                string item=Target is MachineStation outputMachine?outputMachine.Recipe.output:Definitions.Items[GameSession.Instance.SelectedItem].id;
                if(Target.Inventory==null)return false;
                nextTransfer=Time.time+.12f;
                return Inventory.Transfer(Target.Inventory,carrier,item,1)>0;
            }
            return Target.Work(carrier,delta,actorId);
        }
    }
    public sealed class ProductionStation:Station
    {
        public string ItemId;public float Interval=5,Remaining;public int Yield=1;public Transform[] Plants;public float Produced{get;private set;}
        public int Phase,Herd=3,Feed;public float Action,Cycle=30,Breeding;float attendedUntil;
        public bool Animal=>ItemId is "milk" or "egg" or "beef";
        public override string Prompt=>!IsUnlocked?Label+" • chưa mở":Animal?Label+" • đàn "+Herd+" • thức ăn "+Feed+" • "+(Feed==0?"Cần cà rốt":Herd==0?"Đang tái đàn":Cycle.ToString("0")+"s"):Label+" • "+(Phase==0?"Đứng gieo hạt":Phase==1?"Đứng tưới":Phase==2?"Đang lớn "+Remaining.ToString("0.0")+"s":"Đứng thu hoạch")+" • cấp "+Level;
        void Update(){Tick(Time.deltaTime);}
        public void Tick(float delta)
        {
            if(!IsUnlocked||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return;
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
        public override bool Work(Inventory carrier,float delta,EntityId actorId)
        {
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
            else if(Phase==1){Phase=2;Remaining=2;}
            else if(carrier.TryAdd(ItemId,1)){Phase=0;Produced++;WorkCount++;}
            return true;
        }
        public override bool Interact(PlayerController player,bool withdraw)=>player&&ContainsInteractionPoint(player.transform.position)&&Work(player.Carry,Time.deltaTime,player.GetEntityId());
        public override StationProgressSave CaptureProgress(){var s=base.CaptureProgress();s.phase=Phase;s.herd=Herd;s.feed=Feed;s.remaining=Remaining;s.action=Action;s.cycle=Cycle;s.breeding=Breeding;s.produced=Produced;return s;}
        public override void RestoreProgress(StationProgressSave s){base.RestoreProgress(s);Phase=s.phase;Herd=s.herd;Feed=s.feed;Remaining=s.remaining;Action=s.action;Cycle=s.cycle;Breeding=s.breeding;Produced=s.produced;attendedUntil=0;}
    }
    public sealed class StorageStation:Station
    {
        public override string Prompt=>Label+" • "+Inventory.Total+"/"+Inventory.Capacity+" • cất hàng / R lấy "+Definitions.Items[GameSession.Instance.SelectedItem].label;
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
            foreach(var id in AllowedItems)if(Inventory.Transfer(withdraw?Inventory:player.Carry,withdraw?player.Carry:Inventory,id,1)>0){WorkCount++;return true;}
            return false;
        }
    }
    public sealed class PurchasePad:Station
    {
        public UpgradeDefinition Upgrade;float held;
        public bool HoldToBuy(float delta)
        {
            var game=GameSession.Instance;
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta)||!game.CanPurchase(Upgrade,out _)||game.Economy.Money<=0){held=0;return false;}
            held+=delta*35;int amount=Mathf.FloorToInt(held);
            if(amount==0)return false;
            held-=amount;return game.Contribute(Upgrade,amount)>0;
        }
        public void StopContributing()=>held=0;
        public override string Prompt=>Upgrade.label+" • "+GameSession.Instance.Contribution(Upgrade.id)+"/"+Upgrade.cost+" xu";
        public override bool Interact(PlayerController player,bool withdraw)=>!withdraw&&player&&ContainsInteractionPoint(player.transform.position)&&HoldToBuy(Time.deltaTime);
        protected override void LateUpdate()
        {
            var g=GameSession.Instance;bool available=g.CanPurchase(Upgrade,out _);
            foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=available;
            if((g.Player.transform.position-InteractionPoint).sqrMagnitude>3)held=0;
            if(StatusLabel){StatusLabel.text=Prompt;StatusLabel.gameObject.SetActive(available&&(g.Player.transform.position-InteractionPoint).sqrMagnitude<36);}
        }
    }
}


