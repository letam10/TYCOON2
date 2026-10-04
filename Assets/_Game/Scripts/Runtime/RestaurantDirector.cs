using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed class RestaurantDirector : MonoBehaviour
    {
        public readonly List<DinerAgent> Diners = new();
        readonly Queue<DinerAgent> pool = new();
        bool restored;
        float nextSpawn;
        int index;
        void Update()
        {
            var game=GameSession.Instance;
            if(!game.NavigationReady || !game.CanSimulate || !game.Economy.Has("restaurant") || game.Milestone==0)return;
            if(!restored)
            {
                foreach(var saved in new List<DinerSave>(game.PendingDiners))
                {
                    var table=game.Tables.Find(x=>x.Id==saved.table);
                    if(table&&Spawn(table,saved))game.PendingDiners.Remove(saved);
                }
                restored=game.PendingDiners.Count==0;
                if(!restored)return;
            }
            if(Time.time<nextSpawn)return;nextSpawn=Time.time+(game.RushActive?3:8);
            var available=game.Tables.Find(x=>x.Occupant==null && x.Cleaning<=0);
            if(available && game.Commerce.Customers.Count+Diners.Count<30)Spawn(available,null);
        }
        bool Spawn(TableStation table,DinerSave saved)
        {
            DinerAgent diner;
            Vector3 point=saved==null?new Vector3(-20+(index++%5),.02f,25):new Vector3(saved.x,.02f,saved.z);
            if(!NavMesh.SamplePosition(point,out var hit,3,NavMesh.AllAreas))return false;
            if(pool.Count>0){diner=pool.Dequeue();diner.transform.position=hit.position;diner.gameObject.SetActive(true);diner.Agent.Warp(hit.position);}
            else
            {
                var root=new GameObject("Diner");root.transform.SetParent(transform);root.transform.position=hit.position;
                var model=Art.Model(index%2==0?"customer":"customer_beach",Vector3.zero,root.transform);model.AddComponent<ActorView>();
                diner=root.AddComponent<DinerAgent>();diner.Initialize();
            }
            diner.Begin(table,saved);Diners.Add(diner);return true;
        }
        public void Recycle(DinerAgent diner){if(!Diners.Remove(diner))return;diner.gameObject.SetActive(false);pool.Enqueue(diner);}
        public void ResetForLoad(){foreach(var diner in Diners.ToArray())Recycle(diner);restored=false;nextSpawn=Time.time;}
    }
    public sealed class TableStation : Station
    {
        public DinerAgent Occupant;
        public Vector3 Seat;
        float cleaning;
        public float Cleaning {get=>Runtime?.progress.remaining??cleaning;set{if(Runtime!=null)throw new System.InvalidOperationException("Table chỉ được cập nhật qua transaction.");cleaning=value;}}
        public bool NeedsMeal=>Occupant && Occupant.Phase==1 && Occupant.PatienceLeft>0;
        public override string Prompt=>Cleaning>0?Label+" • đứng dọn bàn "+Cleaning.ToString("0.0")+"s":NeedsMeal?Label+" • phục vụ món ăn":Label+" • "+(Occupant?"Đang có khách":"Trống");
        public override bool Work(Inventory carrier,float delta,EntityId actorId)
        {
            if(!IsUnlocked||carrier==null||delta<=0)return false;
            if(Authority!=null)return Cleaning>0&&Occupant==null?Authority.StationAction(TransactionKind.CleanTable,this,actorId,delta):DeliverMeal(carrier);
            if(!Lease(actorId))return false;
            if(Cleaning>0&&Occupant==null)
            {
                Cleaning=Mathf.Max(0,Cleaning-delta);
                if(Cleaning==0)WorkCount++; return true;
            }
            return DeliverMeal(carrier);
        }
        public bool DeliverMeal(Inventory carrier,long expectedReceipt=0)=>IsUnlocked&&Occupant&&(expectedReceipt==0||Occupant.Receipt==expectedReceipt)&&Occupant.ReceiveMeal(carrier);
        public override bool Interact(PlayerController player,bool withdraw)=>!withdraw&&player&&ContainsInteractionPoint(player.transform.position)&&Work(player.Carry,Time.deltaTime,player.GetEntityId());
        public override StationProgressSave CaptureProgress(){var state=base.CaptureProgress();state.remaining=Cleaning;return state;}
        public override void RestoreProgress(StationProgressSave state){base.RestoreProgress(state);Cleaning=state.remaining;Occupant=null;}
        protected override void LateUpdate(){if(StatusLabel)StatusLabel.text=Label+"\n"+(Cleaning>0?"Dọn bàn":Occupant?(Occupant.Phase==1?"Gọi món":Occupant.Phase==2?"Đang ăn":"Đã đặt bàn"):"Trống");}
    }
    public sealed class DinerAgent : MonoBehaviour
    {
        public NavMeshAgent Agent;
        public ActorView View;
        public TableStation Table;
        [System.NonSerialized] public Inventory Basket=new(1);
        public long Receipt;
        int phase;float remaining;
        OrderRuntimeState Runtime=>GameSession.Instance?.Transactions?.Order(Receipt);
        public int Phase {get=>Runtime?.dinerPhase??phase;set{if(Runtime!=null)throw new System.InvalidOperationException("Diner chỉ được cập nhật qua transaction.");phase=value;}}
        public float Remaining {get=>Runtime==null?remaining:Phase==2?(float)Runtime.eatingRemaining:(float)System.Math.Max(0,Runtime.deadline-GameSession.Instance.Transactions.Now);set{if(Runtime!=null)throw new System.InvalidOperationException("Diner chỉ được cập nhật qua transaction.");remaining=value;}}
        public int Price;
        float lastTickTime;
        public float PatienceLeft=>Phase<2?Runtime!=null?Remaining:Mathf.Max(0,Remaining-Mathf.Max(0,Time.time-lastTickTime)):0;
        public void Initialize(){Agent=Navigation.Agent(gameObject);View=GetComponentInChildren<ActorView>();View.Initialize();}
        public void Begin(TableStation table,DinerSave saved)
        {
            Table=table;if(Basket.Authority!=null)Basket.Unbind();Basket.Restore(saved?.basket);Receipt=saved?.receipt??GameSession.Instance.NextReceipt++;
            phase=saved?.phase??0;remaining=saved?.remaining??90;Price=saved?.price??GameSession.Instance.ItemPrice("meal");lastTickTime=Time.time;
            if(GameSession.Instance.Transactions!=null)GameSession.Instance.Transactions.BindDiner(this,saved??Snapshot(),saved==null);
            if(Phase<3)table.Occupant=this;
            if(Agent){if(Phase<3)Navigation.Go(Agent,table.Seat);else Navigation.Go(Agent,new Vector3(-20,0,25));}
        }
        public bool ReceiveMeal(Inventory carrier)
        {
            if(Phase!=1||Table==null||Table.Occupant!=this||carrier==null)return false;
            if(PatienceLeft<=0){FailOrder();return false;}
            if(Runtime!=null)return GameSession.Instance.Transactions.Deliver(Receipt,carrier)>0;
            if(Inventory.Transfer(carrier,Basket,"meal",1)!=1)return false;
            // Nhận món và bắt đầu ăn là cùng một giao dịch, tránh giao trùng trong một frame.
            Phase=2;Remaining=6;lastTickTime=Time.time;return true;
        }
        void LeaveTable(bool dirty)
        {
            if(Runtime==null)Phase=3;
            if(Table&&Table.Occupant==this){Table.Occupant=null;if(dirty&&Runtime==null)Table.Cleaning=3;}
            if(Agent)Navigation.Go(Agent,new Vector3(-20,0,25));
        }
        void FailOrder(){if(Runtime!=null)GameSession.Instance.Transactions.Fail(Receipt);else GameSession.Instance.Economy.RecordLoss(Receipt,Basket);LeaveTable(Basket.Total>0);}
        public void Tick(float delta)
        {
            if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return;
            var game=GameSession.Instance;
            if(!game.CanSimulate)return;
            if(Runtime!=null)
            {
                if(Phase<2&&PatienceLeft<=0){FailOrder();return;}
                if(Phase==1&&Table.Inventory.Count("meal")>0)ReceiveMeal(Table.Inventory);
                if(Phase==2)
                {
                    var command=game.Transactions.Command(TransactionKind.AdvanceDiner,"simulation",RuntimeTransactions.OrderId(Receipt));command.duration=delta;
                    game.Transactions.TryExecute(command,out _);if(Phase==3)LeaveTable(true);
                }
                return;
            }
            lastTickTime=Time.time;
            if(Phase<2)
            {
                Remaining=Mathf.Max(0,Remaining-delta);
                if(Remaining<=0){FailOrder();return;}
            }
            if(Phase==1&&Table.Inventory.Count("meal")>0)ReceiveMeal(Table.Inventory);
            else if(Phase==2)
            {
                Remaining=Mathf.Max(0,Remaining-delta);
                if(Remaining>0)return;
                var bank=game.Checkouts.Find(x=>x.ShopId=="restaurant");
                if(!bank)return;
                if(game.Economy.RecordPayment(Receipt,Price))
                {
                    bank.Cash+=Price;game.RestaurantMeals++;Table.WorkCount++;
                    if(game.Feedback)game.Feedback.PlaySale();
                }
                Basket.Restore(null);LeaveTable(true);
            }
        }
        void Update()
        {
            var game=GameSession.Instance;View.SetMotion(Agent.velocity.magnitude,Basket.Total>0);Tick(Time.deltaTime);
            if(Phase==0&&Navigation.Arrived(Agent))
            {
                if(Runtime!=null){var command=game.Transactions.Command(TransactionKind.AdvanceDiner,"simulation",RuntimeTransactions.OrderId(Receipt));command.secondary="seat";game.Transactions.TryExecute(command,out _);}else Phase=1;
                transform.rotation=Quaternion.LookRotation(Table.transform.position-transform.position);
            }
            else if(Phase==3&&Navigation.Arrived(Agent))game.Restaurant.Recycle(this);
            if(Phase is 1 or 2 && View.Animator.HasState(0,Animator.StringToHash("Sit")) && !View.Animator.IsInTransition(0) && !View.Animator.GetCurrentAnimatorStateInfo(0).IsName("Sit"))View.Animator.CrossFadeInFixedTime("Sit",.2f);
        }
        public DinerSave Snapshot()=>new(){table=Table.Id,receipt=Receipt,phase=Phase,remaining=Phase<2?PatienceLeft:Remaining,price=Price,x=transform.position.x,z=transform.position.z,basket=Basket.Snapshot()};
    }
}


