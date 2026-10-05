using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable] sealed class TownFixture { public int wallet,handJobs,repairJobs;public float timeScale=3;public string[] owned;public TownStock[] stock; }
        [Serializable] sealed class TownStock {public string owner,item;public int quantity;}
        Vector3 Near(Station station)=>station is PurchasePad?station.transform.position:station is CargoDock?station.transform.position+Vector3.left*1.7f:station is ShelfStation?station.transform.position+Vector3.left*1.4f:station is ProductionStation p&&!p.Animal?station.transform.position+Vector3.back*2.6f:station is CheckoutStation?station.transform.position+new Vector3(-1.7f,0,-.3f):station.transform.position+Vector3.back*(station is MachineStation or TableStation or ProductionStation?1.65f:2);
        IEnumerator TownWait(Func<bool> ready,float seconds,string label)
        {float end=Time.time+seconds;while(!ready()){if(Time.time>end)throw new Exception(label+" • "+game.Player.InteractionReason+" • "+game.Player.transform.position);yield return null;}}
        void SeedTown(TownFixture f)
        {
            SeedMilestone(new(){id="town",wallet=f.wallet,owned=f.owned,orders=600});
            var data=game.CaptureSaveData();var s=game.Transactions.Snapshot();
            s.transportJobs.Clear();s.crates.Clear();s.truck=null;
            for(int i=0;i<f.handJobs;i++)s.transportJobs.Add(new(){id="fixture:hand:"+i,source="storage_farm",destination="storage_processing",area="processing",kind="hand",item="carrot",quantity=6});
            var feed=s.stations.First(x=>x.id=="machine_feedmill");feed.progress.playerRepairCount=f.repairJobs;
            foreach(var row in f.stock)
            {var owner=s.owners.First(x=>x.id==row.owner);s.stacks.Add(new(){id="fixture:stock:"+row.owner+":"+row.item,owner=owner.id,location=owner.location+"/"+row.item,item=row.item,quantity=row.quantity});}
            TransactionCore.NormalizeState(s);TransactionCore.Validate(s);RuntimeTransactions.Project(s,data);
            game.Transactions.Detach();File.Delete(game.SavePath+".journal");SaveStore.Write(game.SavePath,data);game.LoadGame();
            Check(!game.SaveBlocked,"near-threshold fixture restores into isolated QA save");
        }
        IEnumerator TownGet(string sku,int count,StorageStation storage)
        {
            Check(game.Player.Carry.Total==0,"empty carry before picking "+sku);
            game.SelectedItem=Array.FindIndex(Definitions.Items,x=>x.id==sku);game.Player.StopInteraction();
            yield return Travel(Near(storage));yield return TownWait(()=>game.Player.Carry.Count(sku)>=count,4,"pickup "+sku);
            // Đi ra trước khi đổi mục tiêu để không tự lấy thêm cùng lượt đứng.
            yield return Travel(Near(storage)+Vector3.back*1.5f);
        }
        IEnumerator TownDrop(Station target)
        {yield return Travel(Near(target));yield return TownWait(()=>game.Player.Carry.Total==0,8,"drop "+target.Id);yield return Travel(Near(target)+Vector3.back*1.5f);}
        CustomerAgent TownCustomer(CheckoutStation counter,string item,int quantity,float patience=90)
        {
            var root=new GameObject("FixtureCustomer");root.transform.SetParent(game.transform);root.transform.position=counter.QueuePoint(0);
            var model=Art.Model("customer",Vector3.zero,root.transform);model.AddComponent<ActorView>();var customer=root.AddComponent<CustomerAgent>();customer.Initialize();
            var exit=TownLayout.Spawn(0);customer.Restore(new(){receipt=game.NextReceipt++,shop=counter.ShopId,lane=counter.Id,phase=(int)CustomerAgent.State.Queue,remaining=patience,order=new(){new(item,quantity,game.ItemPrice(item))},x=root.transform.position.x,z=root.transform.position.z,exitX=exit.x,exitZ=exit.z});
            game.Commerce.Customers.Add(customer);return customer;
        }
        IEnumerator TownRedesign(bool layoutOnly)
        {
            yield return new WaitForSeconds(1);Check(game.NavigationReady,"town NavMesh ready");
            bool fixtureOnly=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--qa-town-fixture-only");
            if(fixtureOnly)File.WriteAllText(Path.Combine(game.QaDirectory,"fixture-note.txt"),"Chạy dữ liệu mod/test sát ngưỡng; bỏ lượt hồi phục ví 0 đã được kiểm tra riêng. Không chứng minh full progression hoặc balance 180–240 phút.");
            if(!layoutOnly&&!fixtureOnly)
            {
                Check(game.Economy.Money==0&&game.Player.Carry.Total==0,"new game starts from zero");
                var plot=game.Producers.First(x=>x.Id=="field_carrot");
                yield return Travel(Near(plot));yield return TownWait(()=>game.Player.Carry.Count("carrot")>=6,30,"auto sow water grow harvest");
                yield return null;Check(game.Player.GetComponentInChildren<InventoryStack>().VisibleCount==game.Player.Carry.Total,"carry models match owned quantity");
                yield return Capture("01-zero-cash-farm.png");
                var counter=game.Checkouts.First(x=>x.ShopId=="farm");yield return Travel(Near(counter));
                yield return TownWait(()=>counter.Cash>0,55,"natural approaching FIFO customer completes order");
                Check(game.Economy.Money==0,"completed delivery creates counter cash before wallet");
                yield return Travel(counter.CollectionPoint);yield return TownWait(()=>game.Economy.Money>0,4,"player collects real payment");
                Check(game.Economy.CashCollected==game.Economy.Money,"zero-cash recovery loop earns collected cash");
                if(game.Player.Carry.Total>0)yield return TownDrop(game.Storage);
                int wallet=game.Economy.Money,revenue=game.Economy.Revenue,collected=game.Economy.CashCollected,orders=game.Economy.Transactions;
                var support=game.Hud.GetComponentsInChildren<Button>().First(b=>b.name=="Hỗ trợ +999.999 / mở khu");support.onClick.Invoke();
                Check(game.Economy.Money==wallet+999999&&game.Economy.Revenue==revenue&&game.Economy.CashCollected==collected&&game.Economy.Transactions==orders,"support click grants exactly 999999 without revenue or progression");
                Check(game.Machines.All(m=>m.IsUnlocked)&&game.Producers.All(p=>p.IsUnlocked),"support opens every basic production route including feed mixer");
            }
            var f=JsonUtility.FromJson<TownFixture>(File.ReadAllText(GameSession.Argument("--qa-fixture","")));SeedTown(f);Time.timeScale=f.timeScale;
            yield return TownLayoutChecks();
            if(layoutOnly){yield return TownScreens();yield break;}
            var source=game.StorageFor("farm");var processing=game.StorageFor("processing");
            Check(game.Progression.Evaluate(Definitions.Upgrade("truck_bundle")).Requirements.Any(x=>x.Contains("29/30")),"truck gate displays 29/30 hand jobs");
            yield return TownGet("carrot",6,source);yield return TownDrop(processing);
            Check(game.Transactions.View.transportJobs.Count(x=>x.kind=="hand"&&x.area=="processing")==30,"one delivered carry trip crosses 29 to 30 jobs");
            var sale=game.Checkouts.First(x=>x.ShopId=="farm");var buyer=TownCustomer(sale,"carrot",1);
            yield return TownGet("carrot",1,processing);yield return Travel(Near(sale));yield return TownWait(()=>buyer.Order.Paid,8,"threshold sale");
            yield return Travel(sale.CollectionPoint);yield return TownWait(()=>game.Economy.Money>=5000,4,"4990 reaches 5000 through collection");
            if(game.Player.Carry.Total>0)yield return TownDrop(source);
            var pad=game.Stations.OfType<PurchasePad>().First(x=>x.Upgrade.id=="truck_bundle");yield return Travel(Near(pad));yield return TownWait(()=>game.Economy.Has("truck_bundle"),15,"purchase truck package");
            Check(game.Transactions.View.truck!=null&&game.CrewStates.Any(c=>c.role=="Driver"),"truck package includes truck and driver");
            Check(game.Logistics.Select(processing.Id,game.StorageFor("farm_shop").Id,false),"choose explicit truck warehouses");
            yield return TownGet("carrot",3,processing);var dock=game.Stations.OfType<CargoDock>().First(d=>d.WarehouseId==processing.Id);
            yield return Travel(Near(dock));yield return TownWait(()=>game.Transactions.View.crates.Any(c=>c.holder==game.Transactions.View.truck.id),8,"pack and load real cargo at dock");
            int cargo=game.Transactions.View.stacks.Where(x=>game.Transactions.View.crates.Any(c=>c.id==x.owner)).Sum(x=>x.quantity);
            int items=game.Transactions.View.stacks.Where(x=>x.item=="carrot").Sum(x=>x.quantity);Check(game.Logistics.Dispatch(),"dispatch reserves destination");
            yield return new WaitForSeconds(3);game.Player.CanControl=false;game.SaveGame();var saved=SaveStore.Read(game.SavePath);double distance=saved.transactionState.truck.travelled;
            for(int i=0;i<3;i++){game.LoadGame();game.Player.CanControl=false;Check(Math.Abs(game.Transactions.View.truck.travelled-distance)<.01,"repeated load preserves truck progress "+i);Check(game.Transactions.View.stacks.Where(x=>x.item=="carrot").Sum(x=>x.quantity)==items,"repeated load does not clone cargo "+i);}
            File.Copy(game.SavePath,Path.Combine(game.QaDirectory,"truck-in-transit-v2.json"),true);game.Player.CanControl=true;
            yield return TownWait(()=>game.Transactions.View.truck.phase=="WaitingUnload",75,"truck reaches destination");
            yield return new WaitForSeconds(1);
            var destination=game.StorageFor("farm_shop");int stock=destination.Inventory.Count("carrot");dock=game.Stations.OfType<CargoDock>().First(d=>d.WarehouseId==destination.Id);
            yield return Travel(Near(dock));yield return TownWait(()=>game.Transactions.View.crates.Count==0,8,"unload at destination");
            Check(destination.Inventory.Count("carrot")==stock+cargo&&game.Transactions.View.stacks.Where(x=>x.item=="carrot").Sum(x=>x.quantity)==items,"unload conserves actual cargo");
            yield return Capture("02-truck-unload.png");
            DevelopmentAssistance.Apply(game);
            var feed=game.Machines.First(x=>x.Id=="machine_feedmill");
            foreach(var input in feed.Recipe.inputs){yield return TownGet(input.id,input.count,source);yield return TownDrop(feed);}
            yield return TownWait(()=>feed.Batches>0,12,"three-ingredient feed auto production");Check(feed.Inventory.Count("animal_feed")>=3,"feed output has real machine owner");
            var breakCommand=game.Transactions.Command(TransactionKind.BreakMachine,"simulation",feed.Id);breakCommand.quantity=10;Check(game.Transactions.TryExecute(breakCommand,out _),"breakdown keeps machine state");
            yield return Travel(Near(feed));float began=Time.time;yield return TownWait(()=>feed.RepairPaid,2,"repair fee charged once");
            yield return new WaitForSeconds(1);float progress=game.Transactions.Station(feed.Id).progress.repairProgress;int money=game.Economy.Money;
            yield return Travel(Near(feed)+Vector3.back*2);game.SaveGame();game.LoadGame();yield return Travel(Near(feed));yield return TownWait(()=>!feed.Broken,8,"repair resumes after leaving and load");
            Check(progress>0&&game.Economy.Money==money&&game.Transactions.Station(feed.Id).progress.playerRepairCount==30,"repair normalized progress fee and 29 to 30 solo jobs");
            Check(Time.time-began>=5,"player repair consumes five simulation seconds including preserved pause");
            Check(game.Progression.Evaluate(Definitions.Upgrade("repair_farm")).Requirements.Length==0,"repairer hire gate opens after solo repair job 30");
            yield return Travel(Near(feed)+Vector3.back*2);
            if(game.Player.Carry.Total>0)yield return TownDrop(source);
            yield return TownRestaurant();yield return TownTraffic();yield return TownScreens();
            game.Player.CanControl=false;game.SaveGame();TransactionCore.Validate(game.Transactions.Snapshot());
        }
        IEnumerator TownRestaurant()
        {
            var kitchen=game.Machines.First(m=>m.Id=="machine_kitchen");var storage=game.StorageFor("restaurant");
            foreach(string recipe in Definitions.KitchenRecipes.Where(x=>x!="kitchen"))
            {
                Check(game.Transactions.SelectRecipe(kitchen,recipe),"select distinct restaurant recipe "+recipe);
                foreach(var input in kitchen.Recipe.inputs){yield return TownGet(input.id,input.count,storage);yield return TownDrop(kitchen);}
                yield return TownWait(()=>kitchen.Inventory.Count(kitchen.Recipe.output)>0,20,"automatic cook "+recipe);
                string item=kitchen.Recipe.output;int before=game.Economy.Money;
                yield return Travel(Near(kitchen));yield return TownWait(()=>game.Player.Carry.Count(item)>0,5,"carry correct dish");
                yield return Travel(Near(kitchen)+Vector3.back*2);
                var table=game.Tables.First(t=>t.Occupant==null&&t.Cleaning<=0);
                var root=new GameObject("FixtureDiner");root.transform.SetParent(game.transform);root.transform.position=table.Seat;
                var model=Art.Model("customer",Vector3.zero,root.transform);model.AddComponent<ActorView>();var diner=root.AddComponent<DinerAgent>();diner.Initialize();
                var seed=new DinerSave{receipt=game.NextReceipt++,table=table.Id,item=item,phase=1,remaining=90,price=game.ItemPrice(item),x=table.Seat.x,z=table.Seat.z};
                game.Transactions.BindDiner(diner,seed,true);diner.Begin(table,seed);game.Restaurant.Diners.Add(diner);
                yield return Travel(Near(table));yield return TownWait(()=>diner.Phase>=2,6,"serve correct table "+item);
                if(game.Player.Carry.Total>0)yield return TownDrop(storage);
                yield return TownWait(()=>table.Cleaning>0,12,"eat payment dirty table "+item);
                Check(game.Economy.Money==before,"restaurant payment waits for player collection");
                yield return Travel(Near(table));yield return TownWait(()=>table.Cleaning<=0,12,"player cleans table");
                var bank=game.Checkouts.First(c=>c.ShopId=="restaurant");yield return Travel(bank.CollectionPoint);yield return TownWait(()=>game.Economy.Money>before,5,"restaurant collect "+item);
            }
        }
        IEnumerator TownLayoutChecks()
        {
            yield return null;var layout=new LayoutReport();
            void Point(string id,Vector3 point)
            {var path=new NavMeshPath();if(!NavMesh.SamplePosition(point,out var hit,.65f,NavMesh.AllAreas)||!NavMesh.CalculatePath(game.Player.transform.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)layout.failures.Add(id+" "+point);else layout.checkedPoints.Add(id);}
            foreach(var s in game.Stations.Where(x=>x is not ConveyorStation)){Point(s.Id,Near(s));if(s is not PurchasePad){Point(s.Id+":work",s.WorkPoint);Point(s.Id+":wait",s.WaitingPoint);}if(s is CheckoutStation c){Point(c.Id+":cash",c.CollectionPoint);for(int i=0;i<15;i++)Point(c.Id+":queue:"+i,c.QueuePoint(i));}if(s is TableStation t)Point(s.Id+":seat",t.Seat);}
            for(int i=0;i<3;i++)Point("offscreen spawn "+i,TownLayout.Spawn(i));
            File.WriteAllText(Path.Combine(game.QaDirectory,"town-layout-details.json"),JsonUtility.ToJson(layout,true));Check(layout.failures.Count==0,"all stations pads work wait queues reachable: "+string.Join("; ",layout.failures));
            Check(!game.Stations.Any(x=>x is StationZone),"no action floor tiles remain");
            Check(TownLayout.Spawn(0).x>58+24,"customer spawn beyond player boundary and camera framing");
        }
        IEnumerator TownTraffic()
        {
            var counter=game.Checkouts.First(c=>c.ShopId=="farm");int index=0;
            while(game.Commerce.ActiveCount<30)
            {
                var root=new GameObject("FixtureApproachingCustomer");root.transform.SetParent(game.transform);root.transform.position=TownLayout.Spawn(index)+Vector3.left*(index/3*.7f);index++;
                var model=Art.Model("customer",Vector3.zero,root.transform);model.AddComponent<ActorView>();var customer=root.AddComponent<CustomerAgent>();customer.Initialize();customer.Begin(counter.ShopId,index,true);game.Commerce.Customers.Add(customer);
            }
            game.Commerce.enabled=true;yield return new WaitForSeconds(15);
            Check(game.Commerce.ActiveCount<=30&&game.Commerce.Customers.All(c=>c.Agent.isOnNavMesh),"short 30-customer traffic remains bounded on NavMesh");
            Check(game.Commerce.Customers.All(c=>c.GetComponent<CustomerNavigationRecovery>().Repaths<=3),"customer repaths bounded during short traffic case");
            game.Commerce.enabled=false;game.SaveGame();var before=SaveStore.Read(game.SavePath);int approaching=before.customers.Count(c=>c.phase==(int)CustomerAgent.State.Approaching);
            game.LoadGame();Check(!game.SaveBlocked&&game.PendingCustomers.Count(c=>c.phase==(int)CustomerAgent.State.Approaching)==approaching,"save restores approaching customers before queue patience begins");
        }
        IEnumerator TownScreens()
        {
            game.Player.StopInteraction();game.Player.CanControl=false;float speed=Time.timeScale;Time.timeScale=0;
            game.ToastUntil=0;var canvas=game.Hud.GetComponentInChildren<Canvas>();canvas.enabled=false;
            game.CameraRig.Overview=true;game.CameraRig.Snap();yield return Capture("03-town-overview.png");game.CameraRig.Overview=false;
            foreach(var point in new[]{new Vector3(-13,0,15),new Vector3(36,0,9),new Vector3(40,0,33),new Vector3(11,0,38),new Vector3(-20,0,39)})
            {game.CameraRig.FocusOffset=point-game.Player.transform.position;game.CameraRig.Distance=25;game.CameraRig.Snap();yield return Capture("area-"+point.x+".png");}
            game.CameraRig.FocusOffset=new(2,.8f,3);game.CameraRig.Target=game.Player.transform;game.CameraRig.Snap();
            canvas.enabled=true;
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1440)})
            {Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);Canvas.ForceUpdateCanvases();Check(Screen.width==size.x&&Screen.height==size.y,"UI resolution "+size);yield return Capture("ui-"+size.x+".png");}
            Time.timeScale=speed;game.Player.CanControl=true;
        }
        IEnumerator TownRelaunch()
        {
            yield return new WaitForSeconds(1);var source=SaveStore.Read(GameSession.Argument("--qa-town-load-from",""));Check(source!=null,"relaunch source save verified");
            game.Commerce.enabled=false;game.Restaurant.enabled=false;game.Transactions.Detach();SaveStore.Write(game.SavePath,source);game.LoadGame();game.Player.CanControl=false;
            Check(!game.SaveBlocked,"relaunch restore before input");var s=game.Transactions.Snapshot();
            Check(s.money==source.money&&s.cashCollected==source.cashCollected,"relaunch exact wallet and collected cash");
            Check(s.stacks.Sum(x=>x.quantity)==source.transactionState.stacks.Sum(x=>x.quantity),"relaunch exact inventory including customers crates and machine escrow");
            Check(s.truck?.travelled==source.transactionState.truck?.travelled&&s.reservations.Count(x=>x.status==ReservationStatus.Active)==source.transactionState.reservations.Count(x=>x.status==ReservationStatus.Active),"relaunch restores cargo position and reservation");
            yield return Capture("relaunch-owned-cargo.png");
        }
    }
}
