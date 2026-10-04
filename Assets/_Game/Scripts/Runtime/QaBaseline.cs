using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tycoon
{
    // Chỉ chạy bằng --qa-baseline, dùng scene và foundation hiện có.
    public sealed class QaBaseline:MonoBehaviour
    {
        readonly List<string> checks=new();
        string failure;
        Keyboard keyboard;
        IEnumerator Start()
        {
            var routine=Run();
            while(true)
            {
                bool next;
                try{next=routine.MoveNext();}
                catch(Exception error){failure=error.ToString();break;}
                if(!next)break;
                yield return routine.Current;
            }
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            var game=GameSession.Instance;
            if(game.RuntimeErrors.Count>0)failure=failure??string.Join("\n",game.RuntimeErrors);
            var report=new Report{result=failure==null?"Passed":"Failed",unity=Application.unityVersion,
                utc=DateTime.UtcNow.ToString("o"),checks=checks.ToArray(),failure=failure,errors=game.RuntimeErrors.ToArray()};
            File.WriteAllText(Path.Combine(game.QaDirectory,"baseline-results.json"),JsonUtility.ToJson(report,true));
            Debug.Log("BASELINE_"+report.result.ToUpperInvariant()+" "+checks.Count+" checks "+(failure??""));
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(failure==null?0:2);
#else
            Application.Quit(failure==null?0:2);
#endif
        }
        void Check(bool success,string label)
        {
            if(!success)throw new InvalidOperationException(label);
            checks.Add(label);
        }
        IEnumerator Run()
        {
            var game=GameSession.Instance;game.Commerce.enabled=false;game.Restaurant.enabled=false;
            float until=Time.realtimeSinceStartup+30;
            while(!game.NavigationReady&&Time.realtimeSinceStartup<until)yield return null;
            Check(game.NavigationReady,"NavMesh built in existing scene");
            var ids=new HashSet<string>();
            foreach(var station in game.Stations)Check(ids.Add(station.Id),"Stable ID "+station.Id);
            Check(game.StorageFor("unknown")==null&&game.StorageFor("farm_shop")==game.Storage,"Warehouses have explicit area owners");
            game.Player.Controller.enabled=false;game.Player.transform.position=new Vector3(5,.05f,2);game.Player.Controller.enabled=true;
            keyboard=InputSystem.AddDevice<Keyboard>();Vector3 start=game.Player.transform.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));yield return new WaitForSeconds(.35f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check((game.Player.transform.position-start).sqrMagnitude>.02f,"Input System moves player in PlayMode");
            game.Player.enabled=false;
            game.Economy.Unlock("mill");
            var machine=game.Machines.Find(m=>m.Recipe.id=="mill");machine.Input.TryAdd("wheat",4);
            Check(machine.Operate(.1f,game.Player.GetEntityId()),"Machine starts with explicit operator");
            machine.ReleaseOperator(game.Player.GetEntityId());float remaining=machine.Remaining;
            yield return new WaitForSeconds(.3f);
            Check(machine.Running&&Mathf.Approximately(machine.Remaining,remaining)&&machine.Inventory.Total==0,"Machine does not run unattended");
            var counter=game.Checkouts.Find(c=>c.ShopId=="farm"&&c.IsUnlocked);
            var shelf=game.Shelves.Find(s=>s.ShopId=="farm"&&s.Accepts("carrot")&&s.IsUnlocked);
            Check(counter&&shelf,"Existing counter and shelf available");shelf.Inventory.TryAdd("carrot",3);
            var customer=Customer(game,counter,1);until=Time.realtimeSinceStartup+15;
            while(Vector3.Distance(customer.transform.position,counter.QueuePoint(customer))>.75f&&Time.realtimeSinceStartup<until)yield return null;
            Check(Vector3.Distance(customer.transform.position,counter.QueuePoint(customer))<=.75f&&customer.Agent.isOnNavMesh,"Customer navigates to queue");
            Check(!counter.Serve(game.Player.Carry)&&shelf.Inventory.Count("carrot")==3,"Empty carrier cannot consume shelf stock");
            int wallet=game.Economy.Money;int transactions=game.Economy.Transactions;
            game.CrewStates.Add(new CrewState{id="cashier",role="Cashier",area="farm_shop"});
            until=Time.realtimeSinceStartup+20;bool carried=false;
            while(!customer.Order.Paid&&Time.realtimeSinceStartup<until)
            {
                carried|=game.Workers.Exists(w=>w.Carry.Count("carrot")>0);
                yield return null;
            }
            Check(carried,"Cashier physically carries shelf goods");
            Check(customer.Order.Paid&&customer.Basket.Count("carrot")==1&&shelf.Inventory.Count("carrot")==2,"Cashier delivers own goods at counter");
            int price=customer.Order.TotalPrice;
            Check(game.Economy.Money==wallet&&counter.Cash==price&&game.Economy.Transactions==transactions+1,"Payment creates deferred counter cash once");
            game.Player.Controller.enabled=false;game.Player.transform.position=counter.CashZone.InteractionPoint;game.Player.Controller.enabled=true;
            Check(counter.CollectCash(game.Player)==price&&counter.CollectCash(game.Player)==0,"Cash collection is explicit and idempotent");
            Check(game.Economy.Money==wallet+price&&game.Economy.PendingCash==0,"Counter cash moves to wallet once");
            Check(!game.Economy.RecordPayment(customer.Receipt,price),"Receipt cannot pay twice");
            long receipt=customer.Receipt;int total=Owned(game,"carrot");game.SaveGame();
            Check(File.Exists(game.SavePath)&&!File.Exists(game.SavePath+".tmp"),"Atomic save leaves no temporary file");
            game.LoadGame();game.LoadGame();
            Check(!game.SaveBlocked&&Owned(game,"carrot")==total&&game.Economy.Money==wallet+price,"Repeated load preserves goods and money");
            Check(game.Commerce.Customers.Count==0&&game.PendingCustomers.Exists(c=>c.receipt==receipt)&&
                game.Workers.Count==0&&game.PendingWorkers.Count==1,"Load resets old actors and keeps pending owners");
            Check(game.NextReceipt>receipt,"Receipt sequence stays monotonic");
            game.SaveGame();var saved=SaveStore.Read(game.SavePath);
            Check(saved.customers.Exists(c=>c.receipt==receipt)&&saved.workers.Count==1,"Immediate resave retains unspawned owners");
            Check(machine.Running&&Mathf.Approximately(machine.Remaining,remaining)&&machine.Input.Count("wheat")==0,"Machine restores once from station state");
            yield return null;
            Check(game.Workers.Count==1&&game.PendingWorkers.Count==0,"Saved worker resumes once");
            game.Commerce.enabled=true;yield return null;game.Commerce.enabled=false;
            Check(game.Commerce.Customers.Exists(c=>c.Receipt==receipt)&&game.PendingCustomers.Count==0,"Saved customer restores through pool");
            var order=new OrderState(game.NextReceipt++,new[]{new OrderLine("milk",2,10)},0,1);
            var goods=new Inventory(2);goods.TryAdd("milk",1);int stock=game.Storage.Inventory.Total;
            Check(order.Expire(goods,game.Economy,2)&&game.Economy.IsLost(order.Receipt),"Expired partial order records loss");
            Check(!order.Expire(goods,game.Economy,3)&&!game.Economy.RecordPayment(order.Receipt,10),"Expired receipt cannot settle twice");
            Check(game.Storage.Inventory.Total==stock&&goods.Total==1,"Timeout never refunds delivered goods");
        }
        CustomerAgent Customer(GameSession game,CheckoutStation lane,int requested)
        {
            Vector3 point=lane.QueuePoint(null)+Vector3.back*2;
            if(!NavMesh.SamplePosition(point,out var hit,3,NavMesh.AllAreas))throw new InvalidOperationException("No queue approach on NavMesh");
            var root=new GameObject("BaselineCustomer");root.transform.SetParent(game.Commerce.transform);root.transform.position=hit.position;
            Art.Model("customer",Vector3.zero,root.transform).AddComponent<ActorView>();
            var customer=root.AddComponent<CustomerAgent>();customer.Initialize();var exit=game.Storage.InteractionPoint;
            customer.Restore(new CustomerSave{receipt=game.NextReceipt++,shop=lane.ShopId,lane=lane.Id,phase=(int)CustomerAgent.State.Queue,
                remaining=120,x=hit.position.x,y=hit.position.y,z=hit.position.z,exitX=exit.x,exitY=exit.y,exitZ=exit.z,
                order=new List<OrderLine>{new("carrot",requested,game.ItemPrice("carrot"))}});
            game.Commerce.Customers.Add(customer);return customer;
        }
        static int Owned(GameSession game,string id)
        {
            int total=game.Player.Carry.Count(id);
            foreach(var station in game.Stations){total+=station.Inventory?.Count(id)??0;if(station is MachineStation machine)total+=machine.Input.Count(id);}
            foreach(var worker in game.Workers)total+=worker.Carry.Count(id);
            foreach(var customer in game.Commerce.Customers)total+=customer.Basket.Count(id);
            foreach(var worker in game.PendingWorkers)foreach(var item in worker.carry)if(item.id==id)total+=item.count;
            foreach(var customer in game.PendingCustomers)foreach(var item in customer.basket)if(item.id==id)total+=item.count;
            return total;
        }
        [Serializable] sealed class Report{public string result,unity,utc,failure;public string[] checks,errors;}
    }
}
