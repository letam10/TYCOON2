using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

namespace Tycoon
{
    // Nghiệm thu 02/03 trong scene thật: hàng/tiền chỉ đến từ trồng, giao và thu tiền.
    public sealed class QaStages : MonoBehaviour
    {
        readonly List<string> checks = new();
        Keyboard keyboard;
        Gamepad gamepad;
        string failure;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode editorInput;
#endif
        IEnumerator Start()
        {
#if UNITY_EDITOR
            editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var run=Run();
            while(true)
            {
                bool next;try{next=run.MoveNext();}catch(Exception error){failure=error.ToString();break;}
                if(!next)break;yield return run.Current;
            }
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(gamepad!=null)InputSystem.RemoveDevice(gamepad);
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
#endif
            var game=GameSession.Instance;
            if(game.RuntimeErrors.Count>0)failure??=string.Join("\n",game.RuntimeErrors);
            var state=game.Transactions?.Snapshot();
            var report=new Report{result=failure==null?"Passed":"Failed",unity=Application.unityVersion,utc=DateTime.UtcNow.ToString("o"),
                checks=checks.ToArray(),errors=game.RuntimeErrors.ToArray(),failure=failure,revision=state?.revision??-1,
                receipts=state?.receipts.Count??0,events=state?.outbox.Count??0,graphics=SystemInfo.graphicsDeviceName};
            File.WriteAllText(Path.Combine(game.QaDirectory,"stage23-results.json"),JsonUtility.ToJson(report,true));
            Debug.Log("STAGE23_"+report.result.ToUpperInvariant()+" "+checks.Count+" checks "+(failure??""));
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(failure==null?0:2);
#else
            Application.Quit(failure==null?0:2);
#endif
        }
        void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);checks.Add(name);}
        void At(GameSession game,Vector3 point)
        {game.Player.StopInteraction();game.Player.Controller.enabled=false;game.Player.transform.position=point+Vector3.up*.05f;game.Player.Controller.enabled=true;game.CameraRig.Snap();}
        StationZone Zone(GameSession game,Station target,InteractionKind kind)=>game.Stations.OfType<StationZone>().Single(x=>x.Target==target&&x.Kind==kind);
        void Harvest(GameSession game,ProductionStation crop)
        {
            At(game,Zone(game,crop,InteractionKind.Operate).Center);
            if(crop.Phase==0)Check(game.Player.InteractAtCurrentPosition(1.05f)&&crop.Phase==1,"Auto sow in Operate zone");
            if(crop.Phase==1)Check(game.Player.InteractAtCurrentPosition(1.05f)&&crop.Phase==2,"Auto water in Operate zone");
            crop.Tick(2.1f);Check(crop.Phase==3,"Crop grows through authoritative tick");
            At(game,Zone(game,crop,InteractionKind.Pickup).Center);int before=game.Player.Carry.Total;
            Check(game.Player.InteractAtCurrentPosition(1.05f)&&game.Player.Carry.Total==before+1,"Auto harvest changes real owned carry");
        }
        CustomerAgent Customer(GameSession game,CheckoutStation lane,int count)
        {
            Vector3 point=lane.QueuePoint(null)+Vector3.back*2;
            if(!NavMesh.SamplePosition(point,out var hit,3,NavMesh.AllAreas))throw new InvalidOperationException("No customer approach");
            var root=new GameObject("Stage23Customer");root.transform.SetParent(game.Commerce.transform);root.transform.position=hit.position;
            Art.Model("customer",Vector3.zero,root.transform).AddComponent<ActorView>();var customer=root.AddComponent<CustomerAgent>();customer.Initialize();
            customer.Restore(new CustomerSave{receipt=game.NextReceipt++,shop=lane.ShopId,lane=lane.Id,phase=(int)CustomerAgent.State.Queue,
                remaining=90,x=hit.position.x,y=hit.position.y,z=hit.position.z,exitX=point.x,exitY=point.y,exitZ=point.z,
                order=new(){new OrderLine("carrot",count,game.ItemPrice("carrot"))}});
            game.Commerce.Customers.Add(customer);return customer;
        }
        IEnumerator Arrive(CheckoutStation lane,CustomerAgent customer)
        {
            float until=Time.realtimeSinceStartup+20;
            while(Vector3.Distance(customer.transform.position,lane.QueuePoint(customer))>.8f&&Time.realtimeSinceStartup<until)yield return null;
            Check(Vector3.Distance(customer.transform.position,lane.QueuePoint(customer))<=.8f,"Customer reaches physical FIFO counter");
        }
        IEnumerator Run()
        {
            var game=GameSession.Instance;game.Commerce.enabled=game.Restaurant.enabled=false;
            float until=Time.realtimeSinceStartup+30;
            while(!game.NavigationReady&&Time.realtimeSinceStartup<until)yield return null;
            Check(game.NavigationReady&&game.Transactions!=null&&game.Transactions.Ready,"Existing scene, NavMesh and runtime core ready");
            Check(game.Economy.Money==0&&game.Player.Carry.Total==0,"Fresh zero-money, empty-carry start");
            Check(!Camera.main.orthographic&&Mathf.Abs(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.x,55))<.01f,"Perspective pitch 55 degrees");
            foreach(var target in game.Stations.Where(x=>x is not StationZone).ToArray())
            {
                var zones=game.Stations.OfType<StationZone>().Where(x=>x.Target==target).ToArray();
                for(int i=0;i<zones.Length;i++)for(int j=i+1;j<zones.Length;j++)
                    Check(Vector3.Distance(zones[i].Center,zones[j].Center)>zones[i].InteractionRadius+zones[j].InteractionRadius,"Separate "+target.Id+" "+zones[i].Kind+"/"+zones[j].Kind);
            }
            keyboard=InputSystem.AddDevice<Keyboard>();gamepad=InputSystem.AddDevice<Gamepad>();At(game,new Vector3(5,0,2));
            Vector3 start=game.Player.transform.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return new WaitForSeconds(.25f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check(Vector3.Dot(game.Player.transform.position-start,PlayerController.CameraRelativeDirection(Vector2.up,Camera.main.transform))>.1f,"WASD follows camera in PlayMode");
            start=game.Player.transform.position;InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=Vector2.right});yield return new WaitForSeconds(.25f);
            InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return null;
            Check(Vector3.Dot(game.Player.transform.position-start,PlayerController.CameraRelativeDirection(Vector2.right,Camera.main.transform))>.1f,"Gamepad moves along camera right");
            game.Player.enabled=false;var crop=game.Producers.Find(x=>x.Id=="field_carrot");
            At(game,Zone(game,crop,InteractionKind.Operate).Center);game.Player.InteractAtCurrentPosition(.3f);float action=crop.Action;
            At(game,new Vector3(5,0,2));game.Player.InteractAtCurrentPosition(1);
            Check(Mathf.Approximately(crop.Action,action)&&game.Player.ActiveInteraction==null,"Leaving zone immediately stops work");
            for(int i=0;i<6;i++)Harvest(game,crop);
            Check(game.Player.Carry.Total==6&&game.Player.Carry.Capacity==6,"Default carry capacity six");
            var stack=game.Player.GetComponentInChildren<InventoryStack>();stack.Refresh();
            Check(stack.VisibleCount==6&&Enumerable.Range(0,6).All(i=>stack.VisibleItemId(i)=="carrot"),"3D stack matches all six actual carrots");
            if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null)
            {game.CameraRig.Snap();yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(game.QaDirectory,"carry-six.png"));yield return null;}
            At(game,Zone(game,game.Storage,InteractionKind.Pickup).Center);game.SelectedItem=Array.FindIndex(Definitions.Items,x=>x.id=="milk");
            Check(!game.Player.InteractAtCurrentPosition(.13f)&&game.Player.InteractionReason.Contains("loại khác")&&game.Player.Carry.Total==6,"Wrong SKU rejection preserves carry and shows Vietnamese reason");
            game.SelectedItem=0;At(game,Zone(game,crop,InteractionKind.Pickup).Center);
            Check(!game.Player.InteractAtCurrentPosition(.13f)&&game.Player.InteractionReason.Contains("đầy")&&game.Player.Carry.Total==6,"Full carry rejection preserves quantity");
            At(game,Zone(game,game.Storage,InteractionKind.Drop).Center);for(int i=0;i<6;i++)game.Player.InteractAtCurrentPosition(.13f);
            Check(game.Player.Carry.Total==0&&game.Storage.Inventory.Count("carrot")==6,"Auto drop into specific warehouse conserves goods");
            var counter=game.Checkouts.Find(x=>x.Id=="checkout_farm");var customer=Customer(game,counter,2);yield return Arrive(counter,customer);
            At(game,Zone(game,game.Storage,InteractionKind.Pickup).Center);game.Player.InteractAtCurrentPosition(.13f);
            At(game,Zone(game,counter,InteractionKind.Serve).Center);game.Player.InteractAtCurrentPosition(.13f);
            Check(customer.Basket.Total==1&&counter.Cash==0&&!customer.Order.Paid,"Partial delivery keeps customer ownership without payment");
            game.Transactions.Fail(customer.Receipt);customer.Leave();Check(customer.Basket.Total==1&&game.Economy.LostItems==1,"Failed partial order retains goods and records one loss");
            for(int sale=0;sale<10;sale++)
            {
                if(game.Storage.Inventory.Count("carrot")>0){At(game,Zone(game,game.Storage,InteractionKind.Pickup).Center);game.Player.InteractAtCurrentPosition(.13f);}else Harvest(game,crop);
                customer=Customer(game,counter,1);yield return Arrive(counter,customer);
                At(game,Zone(game,counter,InteractionKind.Serve).Center);Check(game.Player.InteractAtCurrentPosition(.13f)&&customer.Order.Paid,"Full order creates payment through Serve zone");
                int cash=counter.Cash;game.Transactions.Settle(customer.Receipt);Check(counter.Cash==cash,"Payment settlement is idempotent");
                At(game,Zone(game,counter,InteractionKind.Collect).Center);Check(game.Player.InteractAtCurrentPosition(.13f)&&counter.Cash==0,"Separate Collect zone moves deferred cash to wallet");
                int money=game.Economy.Money;game.Player.InteractAtCurrentPosition(.13f);Check(game.Economy.Money==money,"Collection cannot execute twice");
            }
            Check(game.Economy.Money==100&&game.Economy.Transactions==10,"Revenue earned from ten real deliveries");
            var pad=game.Stations.OfType<PurchasePad>().Single(x=>x.Upgrade.id=="carry10");At(game,pad.Center);
            Check(game.Player.InteractAtCurrentPosition(1)&&game.Contribution("carry10")==35&&!game.Economy.Has("carry10"),"Purchase uses partial contribution");
            game.SaveGame();game.LoadGame();Check(!game.SaveBlocked&&game.Contribution("carry10")==35&&game.Economy.Money==65,"Load preserves partial purchase and wallet");
            At(game,pad.Center);Check(game.Player.InteractAtCurrentPosition(2)&&game.Economy.Has("carry10")&&game.Player.Carry.Capacity==10,"Purchase completes once and upgrades capacity to ten");
            int paid=game.Contribution("carry10");game.Player.InteractAtCurrentPosition(2);Check(game.Contribution("carry10")==paid&&game.Economy.Money==0,"Completed purchase cannot charge twice");
            var state=game.Transactions.Snapshot();TransactionCore.Validate(state);
            Check(state.receipts.Count==state.revision&&state.outbox.Count==state.revision,"All live mutations have receipt and outbox in the same save");
            Check(state.stacks.All(x=>state.owners.Any(o=>o.id==x.owner&&o.location==x.location)),"Every stack has one valid owner and location");
            game.SaveGame();Check(SaveStore.Read(game.SavePath).transactionVersion==1,"One durable gameplay envelope retains the core");
        }
        [Serializable]sealed class Report{public string result,unity,utc,failure,graphics;public string[]checks,errors;public long revision;public int receipts,events;}
    }
}
