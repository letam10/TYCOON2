using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tycoon
{
    public sealed partial class QaDriver : MonoBehaviour
    {
        GameSession game;
        Keyboard keyboard;
        Gamepad gamepad;
        QaReport report = new();
        string failure;
        readonly List<InputDevice> physicalDevices = new();
        IEnumerator Start()
        {
            game = GameSession.Instance;
            // Chỉ cô lập input trong tiến trình QA để phím người dùng không làm sai phép thử.
            foreach(var device in InputSystem.devices)
                if(device.enabled && (device is Keyboard || device is Gamepad)) { physicalDevices.Add(device); InputSystem.DisableDevice(device); }
            keyboard = InputSystem.AddDevice<Keyboard>("QA_Keyboard");
            gamepad = InputSystem.AddDevice<Gamepad>("QA_Gamepad");
            report.startedUtc = DateTime.UtcNow.ToString("o");
            report.graphicsDevice = SystemInfo.graphicsDeviceName;
            var routines = new Stack<IEnumerator>();
            var arguments = Environment.GetCommandLineArgs();
            bool load = Array.Exists(arguments,x=>x=="--qa-load"), full = Array.Exists(arguments,x=>x=="--qa-progression");
            bool stage45=Array.Exists(arguments,x=>x=="--qa-stage45");bool stage67=Array.Exists(arguments,x=>x=="--qa-stage67");
            bool art = Array.Exists(arguments,x=>x=="--qa-art");
            bool redesign=Array.Exists(arguments,x=>x=="--qa-v2");
            string mode = redesign ? "redesign" : art ? "visual" : load ? "load" : stage67?"stage67":stage45?"stage45":full ? "progression" : game.Milestone == 0 ? "foundation" : "vertical";
            bool resume = Array.Exists(arguments,x=>x=="--qa-resume");
            routines.Push(redesign ? Redesign() : art ? VisualInspection() : load ? LoadCheck() : stage67?LivestockAndCrew():stage45?FarmStarterAndPurchase():game.Milestone == 0 ? Foundation() : (full || resume) ? Progression(resume) : Vertical());
            while (routines.Count > 0)
            {
                object yielded = null; bool running = false;
                try { running = routines.Peek().MoveNext(); if (running) yielded = routines.Peek().Current; }
                catch (Exception error) { failure = error.ToString(); }
                if (failure != null) break;
                if (!running) { routines.Pop(); continue; }
                if (yielded is IEnumerator child) { routines.Push(child); continue; }
                yield return yielded;
            }
            Keys();
            if (failure != null) { WriteState("failure-state.txt"); yield return Capture("failure.png"); }
            report.failure = failure ?? "";
            report.runtimeErrors = game.RuntimeErrors.ToArray();
            report.passed = failure == null && game.RuntimeErrors.Count == 0;
            report.distanceWalked = game.Player.DistanceWalked;
            report.interactions = game.Player.SuccessfulInteractions;
            report.finishedUtc = DateTime.UtcNow.ToString("o");
            File.WriteAllText(Path.Combine(game.QaDirectory, mode + "-report.json"), JsonUtility.ToJson(report, true));
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad);
            foreach(var device in physicalDevices) if(device.added) InputSystem.EnableDevice(device);
            Debug.Log(mode.ToUpperInvariant()+"_QA " + (report.passed ? "PASS" : "FAIL " + report.failure));
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(report.passed ? 0 : 2);
#else
            Application.Quit(report.passed ? 0 : 2);
#endif
        }
        IEnumerator Foundation()
        {
            yield return new WaitForSeconds(1);
            Check(game.Player.Carry.Total == 0, "clean carry");
            Check(game.Economy.Money == 0 && game.Economy.Transactions == 0, "clean economy");
            var view = game.Player.View;
            Check(view.Animator != null && view.Animator.avatar != null && view.Animator.avatar.isValid, "valid player rig");
            foreach (string state in new[] { "Idle", "Walk", "Run", "Carry", "CarryWalk", "Pickup", "Drop" })
                Check(view.Animator.HasState(0, Animator.StringToHash(state)), "animation " + state);
            foreach (var renderer in FindObjectsByType<Renderer>())
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || material.shader.name.Contains("Error"))
                        throw new Exception("Invalid material: " + renderer.name);
                    report.materialSlotsChecked++;
                }
            Check(report.materialSlotsChecked > 0, "all rendered material slots valid");
            yield return Capture("01-foundation.png");
            var begin = game.Player.transform.position;
            Keys(Key.W, Key.LeftShift);
            yield return new WaitForSeconds(1.2f);
            Check(view.State == "Run", "keyboard sprint animation");
            Keys(); yield return new WaitForSeconds(.2f);
            Check((game.Player.transform.position - begin).magnitude > 5, "keyboard movement");
            begin = game.Player.transform.position;
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
            yield return new WaitForSeconds(.8f);
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return new WaitForSeconds(.2f);
            Check(game.Player.transform.position.x - begin.x > 2, "gamepad movement");
            var carrot = game.Producers.Find(x => x.ItemId == "carrot");
            yield return Travel(carrot.InteractionPoint);
            float harvestDeadline = Time.realtimeSinceStartup + carrot.Interval * 3;
            while (carrot.Inventory.Count("carrot") < 6)
            {
                if (Time.realtimeSinceStartup > harvestDeadline) throw new Exception("Crop production did not replenish harvest stock.");
                yield return null;
            }
            Keys(Key.E); yield return new WaitForSeconds(2.0f); Keys();
            yield return new WaitForSeconds(.5f);
            Check(game.Player.Carry.Count("carrot") >= 6, "harvest through keyboard");
            Check(view.State == "Carry", "carry animation");
            var carryStack = game.Player.GetComponentInChildren<InventoryStack>();
            Check(carryStack.VisibleCount == game.Player.Carry.Total, "physical carry stack");
            yield return Capture("02-carry.png");
            int harvested = game.Player.Carry.Count("carrot");
            yield return Travel(game.Storage.InteractionPoint);
            Keys(Key.E); yield return new WaitForSeconds(harvested * .25f + .5f); Keys();
            yield return new WaitForSeconds(.3f);
            Check(game.Player.Carry.Total == 0, "drop all through keyboard");
            Check(game.Storage.Inventory.Count("carrot") == harvested, "storage conserves stock");
            Keys(Key.R); yield return new WaitForSeconds(.70f); Keys();
            yield return new WaitForSeconds(.2f);
            int picked = game.Player.Carry.Count("carrot");
            Check(picked > 0 && picked < harvested, "withdraw stock");
            var shelf = game.Shelves[0];
            yield return Travel(shelf.InteractionPoint);
            Keys(Key.E); yield return new WaitForSeconds(picked * .25f + .3f); Keys();
            yield return new WaitForSeconds(.3f);
            Check(game.Player.Carry.Total == 0 && shelf.Inventory.Count("carrot") == picked, "stock shelf");
            Check(game.Storage.Inventory.Count("carrot") + shelf.Inventory.Count("carrot") == harvested, "global transfer conservation");
            Check(game.Economy.Money == 0 && game.Economy.Transactions == 0, "no money from timers");
            Keys(Key.F5); yield return new WaitForSeconds(.2f); Keys();
            yield return new WaitForSeconds(.3f);
            var save = SaveStore.Read(game.SavePath);
            Check(save != null && save.money == 0, "save via F5");
            Check(save.inventories.Exists(x => x.id == "storage" && x.items.Exists(y => y.id == "carrot" && y.count == harvested - picked)), "saved warehouse stock");
            Check(!File.Exists(game.SavePath + ".bak") && !File.Exists(game.SavePath + ".tmp"), "atomic save no backup");
            yield return Capture("03-farm-shop.png");
            game.CameraRig.Overview = true; yield return new WaitForSeconds(1);
            yield return Capture("04-overview.png");
            report.playerHeight = Bounds(game.Player.gameObject).size.y;
            Check(report.playerHeight > 1.1f && report.playerHeight < 2.8f, "rig scale and motion bounds");
        }
        IEnumerator VisualInspection()
        {
            yield return new WaitForSeconds(1);
            Check(game.Economy.Money==0,"art inspection never grants money");
            yield return Capture("01-larger-farm.png");
            foreach(string key in new[]{"player","customer","customer_beach","user_cow","user_chicken"})
            {
                var actor=Art.Model(key,new Vector3(-38,.04f,4),game.transform);
                game.CameraRig.Target=actor.transform;game.CameraRig.FocusOffset=new Vector3(0,Art.ModelSize(key).y*.5f,0);game.CameraRig.Pitch=22;game.CameraRig.Yaw=180;game.CameraRig.Distance=key.Contains("chicken")?4:6;game.CameraRig.Snap();
                var animator=actor.GetComponentInChildren<Animator>();Check(animator&&animator.avatar&&animator.avatar.isValid,"valid rig "+key);
                var bones=actor.GetComponentInChildren<SkinnedMeshRenderer>().bones;
                foreach(string state in key.StartsWith("user_")?new[]{"Idle","Walk"}:new[]{"Idle","Walk","Run","Carry","CarryWalk","Pickup","Drop","Sit"})
                {
                    Check(animator.HasState(0,Animator.StringToHash(state)),key+" clip "+state);
                    animator.Play(state,0,0);yield return new WaitForSeconds(.2f);
                    var previous=new Vector3[bones.Length];for(int i=0;i<bones.Length;i++)previous[i]=bones[i].position;
                    float deadline=Time.realtimeSinceStartup+.7f;float maximum=0;
                    while(Time.realtimeSinceStartup<deadline)
                    {
                        yield return null;
                        for(int i=0;i<bones.Length;i++){maximum=Mathf.Max(maximum,Vector3.Distance(previous[i],bones[i].position));previous[i]=bones[i].position;}
                    }
                    Check(maximum<.4f,"smooth bones "+key+" "+state);
                    var bounds=Bounds(actor);Check(float.IsFinite(bounds.size.y)&&bounds.size.y>.25f&&bounds.size.y<3,"finite pose bounds "+key+" "+state);
                    if(state is "Idle" or "Walk" or "Carry" or "Sit")yield return Capture(key+"-"+state+".png");
                }
                Destroy(actor);
            }
            game.CameraRig.Target=game.Player.transform;game.CameraRig.FocusOffset=new Vector3(2,.8f,3);game.CameraRig.Pitch=55;game.CameraRig.Yaw=40;game.CameraRig.Distance=24;game.CameraRig.Snap();
            var animals=game.GetComponentsInChildren<AnimalMotion>();var starts=new Vector3[animals.Length];
            for(int i=0;i<animals.Length;i++)starts[i]=animals[i].transform.localPosition;
            yield return new WaitForSeconds(4);
            int moved=0;foreach(var animal in animals)
            {
                int index=System.Array.IndexOf(animals,animal);if(Vector3.Distance(starts[index],animal.transform.localPosition)>.05f)moved++;
                Check(Vector3.Distance(animal.Origin,animal.transform.localPosition)<.21f,"animal stays within its cell");
            }
            Check(moved>8,"farm animals really move");
            yield return Capture("02-animal-motion.png");
        }
        IEnumerator Travel(Vector3 target)
        {
            var path = new UnityEngine.AI.NavMeshPath();
            if (game.NavigationReady && UnityEngine.AI.NavMesh.CalculatePath(game.Player.transform.position, target, UnityEngine.AI.NavMesh.AllAreas, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                var route=new List<string>{"from="+game.Player.transform.position+" to="+target+" status="+path.status};
                foreach(var corner in path.corners)route.Add("corner="+corner);
                File.WriteAllLines(Path.Combine(game.QaDirectory,"navigation-route.txt"),route);
                for (int i = 1; i < path.corners.Length; i++) yield return WalkTo(path.corners[i]);
            }
            else yield return WalkTo(target);
            Keys();InputSystem.QueueStateEvent(gamepad,new GamepadState()); yield return new WaitForSeconds(.2f);
        }
        IEnumerator WalkTo(Vector3 target)
        {
            float end = Time.realtimeSinceStartup + 25;
            while (Vector2.Distance(new Vector2(target.x, target.z), new Vector2(game.Player.transform.position.x, game.Player.transform.position.z)) > .25f)
            {
                if (Time.realtimeSinceStartup > end) throw new Exception("Movement timeout: " + target + " from " + game.Player.transform.position);
                Vector3 direction = target - game.Player.transform.position; direction.y = 0; direction.Normalize();
                Vector3 forward = Camera.main.transform.forward; forward.y = 0; forward.Normalize();
                Vector3 right = Camera.main.transform.right; right.y = 0; right.Normalize();
                float x = Vector3.Dot(direction, right), y = Vector3.Dot(direction, forward);
                // Stick liên tục theo góc NavMesh; WASD tám hướng có thể cắt góc tường.
                Keys();InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=new Vector2(x,y)});yield return null;
            }
            Keys();InputSystem.QueueStateEvent(gamepad,new GamepadState()); yield return new WaitForSeconds(.2f);
        }
        IEnumerator Vertical()
        {
            yield return new WaitForSeconds(1);
            Check(game.NavigationReady, "NavMesh ready");
            foreach(var station in game.Stations)
                if(station is PurchasePad)
                {
                    var path=new UnityEngine.AI.NavMeshPath();
                    Check(UnityEngine.AI.NavMesh.SamplePosition(station.InteractionPoint,out var hit,.45f,UnityEngine.AI.NavMesh.AllAreas)&&Vector3.Distance(hit.position,station.InteractionPoint)<.45f,"pad on walkable ground "+station.Id);
                    Check(UnityEngine.AI.NavMesh.CalculatePath(game.Player.transform.position,hit.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete,"pad reachable "+station.Id);
                }
            Check(game.Economy.Money == 0 && game.Economy.Transactions == 0, "fresh save zero money");
            yield return new WaitForSeconds(4);
            Check(game.Economy.Revenue == 0, "production alone creates no money");
            yield return Capture("01-farm-reference.png");
            foreach (string id in new[] { "farmer", "restocker", "cashier" })
            { yield return ManualEarn(Definitions.Upgrade(id).cost); yield return Buy(id); }
            Check(game.Workers.Count == 3, "three physical workers hired");
            yield return Earn(Definitions.Upgrade("mill").cost);
            yield return Buy("mill");
            yield return Earn(Definitions.Upgrade("processor").cost);
            yield return Buy("processor");
            var machine = game.Machines.Find(x => x.Recipe.id == "mill");
            float end = Time.realtimeSinceStartup + 100;
            while (machine.Batches == 0 && Time.realtimeSinceStartup < end) yield return new WaitForSeconds(1);
            Check(machine.Batches > 0, "wheat actually consumed and flour produced");
            Check(game.Workers.Find(x => x.Role == "Farmer").Deliveries > 0 && game.Workers.Find(x => x.Role == "Restocker").Deliveries > 0, "automatic inventory transport");
            yield return Travel(machine.InteractionPoint);
            yield return Capture("04-processing.png");
            yield return Earn(Definitions.Upgrade("machine_upgrade").cost);
            yield return Buy("machine_upgrade");
            int money = game.Economy.Money;
            Keys(Key.E); yield return new WaitForSeconds(.4f); Keys();
            Check(game.Economy.Money == money, "pad does not charge twice");
            Keys(Key.F5); yield return new WaitForSeconds(.1f); Keys();
            yield return new WaitForSeconds(.2f);
            var save = SaveStore.Read(game.SavePath);
            Check(save.unlocked.Contains("mill") && save.unlocked.Contains("processor") && save.unlocked.Contains("machine_upgrade"), "saved unlocks and upgrade");
            Check(save.production.Exists(x => x.id == machine.Id), "saved production progress");
            Check(save.inventories.Exists(x => x.id == machine.Id + "_input"), "saved recipe input inventory");
            Check(save.pendingCash == SumSavedCash(save), "saved unpaid cash conservation");
            Check(game.Economy.PendingCash == SumCash(), "live unpaid cash conservation");
            game.CameraRig.Overview = true; yield return new WaitForSeconds(1);
            yield return Capture("05-vertical-overview.png");
            Check(game.Economy.Transactions > 0 && game.Economy.Revenue > 0, "revenue from real customers");
        }
        IEnumerator FarmStarterAndPurchase()
        {
            yield return new WaitForSeconds(1);
            Check(game.NavigationReady&&game.Transactions!=null&&game.Transactions.Ready,"fresh scene and transaction core ready");
            Check(game.Economy.Money==0&&game.Economy.Transactions==0&&game.Player.Carry.Total==0,"new game starts with zero cash and empty carry");
            var crops=game.Producers.FindAll(x=>x.ItemId=="carrot");
            Check(crops.Count==3&&crops.TrueForAll(x=>x.IsUnlocked),"three accessible starter carrot plots");
            Check(game.Producers.FindAll(x=>x.ItemId is "tomato" or "wheat").TrueForAll(x=>!x.IsUnlocked),"other crop routes remain locked");
            Check(game.ItemPrice("carrot")==10,"starter carrot has base price ten");
            Check(game.ObjectiveText.Contains("LOCKED")&&game.ObjectiveText.Contains("50"),"HUD objective exposes the next gate before its pad appears");
            Check(game.Progression.Evaluate(Definitions.Upgrade("farm_speed2")).State==PurchaseState.Available,"unlocked purchase is available");

            foreach(var crop in crops)yield return HarvestStarterCrop(crop);
            Check(game.Player.Carry.Total==3&&game.Player.Carry.Count("carrot")==3,"three harvested carrots belong to player carry");
            var counter=game.Checkouts.Find(x=>x.Id=="checkout_farm");
            Check(counter.Inventory!=null&&counter.AcceptsItem("carrot"),"farm counter owns a carrot stock location");
            yield return DepositAtCounter(counter);
            int conserved=game.Transactions.Snapshot().stacks.Where(x=>x.item=="carrot").Sum(x=>x.quantity);
            Check(game.Player.Carry.Total==0&&conserved==3,"walking through serve and dropping at counter conserves every carrot owner");
            Check(counter.Cash>0&&game.Economy.Money==0,"direct customer delivery leaves payment at counter until collection");
            int openingMoney=game.Economy.Money;
            yield return ServeAndCollect(counter);
            Check(game.Economy.Transactions>0&&game.Economy.Money>openingMoney,"player collects real farm sales");
            yield return Capture("farm-starter-sale.png");

            var purchase=Definitions.Upgrade("farm_capacity2");var pad=game.Stations.Find(x=>x is PurchasePad p&&p.Upgrade==purchase) as PurchasePad;
            int initialWallet=game.Economy.Money;
            Check(initialWallet>0&&initialWallet<purchase.cost&&pad!=null,"starter revenue can make a partial purchase");
            foreach(var purchasePad in game.Stations.FindAll(x=>x is PurchasePad).ConvertAll(x=>(PurchasePad)x))
                Check(OutsideConstruction(purchasePad.InteractionPoint),"purchase pad outside construction footprint "+purchasePad.Id);
            Check(game.Progression.Evaluate(purchase).State==PurchaseState.Available,"capacity upgrade starts available");
            yield return Travel(pad.InteractionPoint);
            float contributionDeadline=Time.realtimeSinceStartup+5;
            while(game.Economy.Money>0&&!game.Economy.Has(purchase.id)&&Time.realtimeSinceStartup<contributionDeadline)yield return null;
            Check(game.Economy.Money==0&&game.Contribution(purchase.id)==initialWallet&&!game.Economy.Has(purchase.id),"purchase consumes only available money and retains partial contribution");
            Check(game.Progression.Evaluate(purchase).State==PurchaseState.Contributing,"purchase displays contributing state");
            game.SaveGame();game.LoadGame();
            Check(!game.SaveBlocked&&game.Contribution(purchase.id)==initialWallet&&game.Economy.Money==0,"partial purchase and wallet survive save/load");
            Check(game.Progression.Evaluate(purchase).State==PurchaseState.Contributing,"contributing status survives load");

            int savedContribution=game.Contribution(purchase.id);
            yield return HarvestStarterCrop(crops[0]);
            Check(game.Contribution(purchase.id)==savedContribution&&game.Progression.Evaluate(purchase).State==PurchaseState.Contributing,"leaving purchase zone stops further contribution");
            Check(!game.Economy.Has(purchase.id)&&game.Progression.AxisLevel("farm",UpgradeAxis.Capacity)==1,"partial purchase has no premature effect");
            Check(game.ItemPrice("carrot")==10&&game.Player.Carry.Capacity==6&&crops.TrueForAll(x=>x.Inventory.Capacity==24&&x.Level==1),"quality, speed and capacity stay unchanged until purchase completes");
            Check(game.Player.Carry.Count("carrot")>0&&game.Progression.FarmGrowSeconds==2,"production continues after a partial purchase");
            yield return Travel(pad.InteractionPoint);yield return new WaitForSeconds(.3f);
            Check(game.Contribution(purchase.id)==savedContribution&&game.Economy.Has(purchase.id)==false,"empty wallet cannot advance a saved contribution");
        }

        IEnumerator HarvestStarterCrop(ProductionStation crop)
        {
            int before=game.Player.Carry.Count("carrot");
            var operate=game.Stations.Find(x=>x is StationZone z&&z.Target==crop&&z.Kind==InteractionKind.Operate) as StationZone;
            var pickup=game.Stations.Find(x=>x is StationZone z&&z.Target==crop&&z.Kind==InteractionKind.Pickup) as StationZone;
            Check(operate&&pickup,"crop has distinct work and harvest zones");
            if(crop.Phase!=3)
            {
                yield return Travel(operate.InteractionPoint);
                float grown=Time.realtimeSinceStartup+10;
                while(crop.Phase!=3&&Time.realtimeSinceStartup<grown)yield return null;
                Check(crop.Phase==3,"free sow, water and grow complete through Operate zone");
            }
            yield return Travel(pickup.InteractionPoint);
            float harvested=Time.realtimeSinceStartup+8;
            while(game.Player.Carry.Count("carrot")==before&&Time.realtimeSinceStartup<harvested)yield return null;
            Check(game.Player.Carry.Count("carrot")==before+1,"Pickup zone transfers one grown carrot to player");
        }

        IEnumerator DepositAtCounter(CheckoutStation counter)
        {
            var zone=game.Stations.Find(x=>x is StationZone z&&z.Target==counter&&z.Kind==InteractionKind.Drop) as StationZone;
            Check(zone,"checkout has a separate Drop zone");yield return Travel(zone.InteractionPoint);
            float until=Time.realtimeSinceStartup+8;while(game.Player.Carry.Total>0&&Time.realtimeSinceStartup<until)yield return null;
            Check(game.Player.Carry.Total==0,"counter drop completes without discarding carry");
        }

        IEnumerator ServeAndCollect(CheckoutStation counter)
        {
            float queueUntil=Time.realtimeSinceStartup+35;
            while(counter.Queue.Count==0&&Time.realtimeSinceStartup<queueUntil)yield return null;
            Check(counter.Queue.Count>0,"farm customer enters FIFO queue");
            var customer=counter.Queue[0];var order=customer.Order;
            int cashBefore=counter.Cash;
            Check(order.Lines.Count==1&&order.Lines[0].id=="carrot"&&order.Lines[0].requested is >=1 and <=3,"farm customer orders one to three carrots");
            Check(order.Lines[0].unitPrice==10&&order.RemainingPatience(Time.time)<=90&&order.RemainingPatience(Time.time)>0,"order snapshots ten xu and starts a ninety-second patience window");
            float patienceDeadline=Time.realtimeSinceStartup+order.RemainingPatience(Time.time)-2;
            while(counter.Inventory.Count("carrot")<order.RemainingQuantity&&Time.realtimeSinceStartup<patienceDeadline)
            {
                yield return HarvestStarterCrop(game.Producers.Find(x=>x.Id=="field_carrot"));
                yield return DepositAtCounter(counter);
            }
            Check(counter.Inventory.Count("carrot")>=order.RemainingQuantity,"counter stock covers the order before patience expires");
            yield return Travel(game.Stations.Find(x=>x is StationZone z&&z.Target==counter&&z.Kind==InteractionKind.Serve).InteractionPoint);
            float servedUntil=Time.realtimeSinceStartup+Mathf.Max(5,order.RemainingPatience(Time.time));
            while(!order.Paid&&!order.TimedOut&&Time.realtimeSinceStartup<servedUntil)yield return null;
            Check(order.Paid&&!order.TimedOut,"FIFO service completes the order before timeout");
            int amount=order.TotalPrice;Check(amount==order.Lines[0].requested*10,"payment uses the order price snapshot");
            Check(counter.Cash>=cashBefore+amount,"completed order creates deferred counter payment");
            var collection=game.Stations.Find(x=>x is StationZone z&&z.Target==counter&&z.Kind==InteractionKind.Collect) as StationZone;
            Check(collection,"checkout has a separate Collect zone");int wallet=game.Economy.Money;int pending=counter.Cash;
            yield return Travel(collection.InteractionPoint);
            float collectedUntil=Time.realtimeSinceStartup+5;while(game.Economy.Money==wallet&&Time.realtimeSinceStartup<collectedUntil)yield return null;
            Check(game.Economy.Money==wallet+pending&&counter.Cash==0,"only player Collect transfers payment to wallet");
        }

        static bool OutsideConstruction(Vector3 p)
        {
            return !Inside(p,new Vector2(7,6),new Vector2(22,20))&&!Inside(p,new Vector2(28,7),new Vector2(18,18))&&
                !Inside(p,new Vector2(28,33),new Vector2(22,22))&&!Inside(p,new Vector2(4,38),new Vector2(20,20))&&
                !Inside(p,new Vector2(-20,39),new Vector2(22,20));
        }
        static bool Inside(Vector3 p,Vector2 center,Vector2 size)=>Mathf.Abs(p.x-center.x)<size.x*.5f&&Mathf.Abs(p.z-center.y)<size.y*.5f;
        int SumCash() { int sum = 0; foreach (var lane in game.Checkouts) sum += lane.Cash; return sum; }
        static int SumSavedCash(SaveData save) { int sum=0;foreach(var lane in save.cash)sum+=lane.amount;return sum; }
        IEnumerator Progression(bool resume = false)
        {
            if (!resume) yield return Vertical();
            else { yield return new WaitForSeconds(1); Check(game.Economy.Has("machine_upgrade"), "resume uses earned vertical save"); }
            foreach(string id in new[]{"farmer_2","barn","animal_worker","farm_upgrade","worker_upgrade","carry_upgrade","dairy","supermarket","restocker_market","cashier_market","bakery","cook_bakery","restaurant","cook","waiter"})
            {
                if(game.Economy.Has(id)) continue;
                yield return Earn(Definitions.Upgrade(id).cost);yield return Buy(id);
                if(id=="supermarket")
                {
                    float deadline=Time.realtimeSinceStartup+40;
                    while(game.Commerce.PeakCustomers<30&&Time.realtimeSinceStartup<deadline)yield return new WaitForSeconds(1);
                    Check(game.Commerce.PeakCustomers>=30,"30 customers active");
                    yield return Travel(game.Checkouts.Find(x=>x.ShopId=="market").InteractionPoint);yield return Capture("06-supermarket.png");
                }
            }
            Check(game.Workers.Exists(x=>x.Role=="Cook") && game.Workers.Exists(x=>x.Role=="Waiter"),"Cook and Waiter hired");
            yield return Travel(game.Checkouts.Find(x=>x.ShopId=="bakery").InteractionPoint);
            float end=Time.realtimeSinceStartup+300;
            while((game.RestaurantMeals<2||game.BakerySales<2||game.Machines.Exists(x=>x.Batches==0)) && Time.realtimeSinceStartup<end){Keys(Key.E);yield return null;}
            Keys();Check(game.RestaurantMeals>=2,"ordered meals cooked served eaten and paid");Check(game.BakerySales>=2,"bakery customers bought bread or cake");
            foreach(var machine in game.Machines)Check(machine.Batches>0,"recipe operated "+machine.Recipe.id);
            foreach(var shelf in game.Shelves)Check(shelf.Inventory.Total<=shelf.Inventory.Capacity,"shelf capacity "+shelf.Id);
            yield return Capture("07-bakery.png");yield return Travel(game.Tables[1].InteractionPoint);yield return Capture("08-restaurant.png");
            yield return Benchmark();
            Keys(Key.F5);yield return new WaitForSeconds(.2f);Keys();yield return new WaitForSeconds(.2f);
            var save=SaveStore.Read(game.SavePath);
            Check(save.businessStage==5&&save.restaurantMeals>=2&&save.bakerySales>=2,"saved full business progression");Check(save.pendingCash==SumSavedCash(save),"full pending cash conserved");
            game.CameraRig.Overview=true;yield return new WaitForSeconds(1);yield return Capture("09-all-businesses.png");
        }
        IEnumerator Benchmark()
        {
            yield return Travel(game.Checkouts.Find(x=>x.ShopId=="market").InteractionPoint);
            game.CameraRig.Overview=false;yield return new WaitForSeconds(2);
            var camera=Camera.main;var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=2};target.Create();
            var previous=camera.targetTexture;camera.targetTexture=target;
            int renders=0;Action<UnityEngine.Rendering.ScriptableRenderContext,Camera> handler=(c,cam)=>{if(cam==camera)renders++;};
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=handler;
            yield return new WaitForSeconds(5);
            var times=new List<float>();var gpu=new List<double>();var cpu=new List<double>();var timing=new FrameTiming[1];ulong stamp=0;int maxCustomers=0,minCustomers=1000;
            float deadline=Time.realtimeSinceStartup+30;int beginRenders=renders;
            while(Time.realtimeSinceStartup<deadline)
            {
                FrameTimingManager.CaptureFrameTimings();yield return null;
                int customers=game.Commerce.Customers.Count+game.Restaurant.Diners.Count;maxCustomers=Mathf.Max(maxCustomers,customers);minCustomers=Mathf.Min(minCustomers,customers);
                if(customers<30)continue;times.Add(Time.unscaledDeltaTime*1000);
                if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].frameStartTimestamp!=stamp)
                {stamp=timing[0].frameStartTimestamp;if(timing[0].gpuFrameTime>0)gpu.Add(timing[0].gpuFrameTime);if(timing[0].cpuFrameTime>0)cpu.Add(timing[0].cpuFrameTime);}
            }
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=handler;camera.targetTexture=previous;target.Release();Destroy(target);
            Check(renders-beginRenders>300,"benchmark rendered actual camera frames");Check(times.Count>=300,"benchmark samples with thirty customers");
            times.Sort();float total=0;foreach(float t in times)total+=t;double gpuTotal=0,cpuTotal=0;foreach(double t in gpu)gpuTotal+=t;foreach(double t in cpu)cpuTotal+=t;
            var result=new PerformanceReport{graphicsDevice=SystemInfo.graphicsDeviceName,width=1920,height=1080,samples=times.Count,renderedFrames=renders-beginRenders,averageFps=1000*times.Count/total,p95Milliseconds=times[(int)(times.Count*.95f)],minimumCustomers=minCustomers,maximumCustomers=maxCustomers,gpuSamples=gpu.Count,gpuAverageMilliseconds=gpu.Count>0?gpuTotal/gpu.Count:0,cpuAverageMilliseconds=cpu.Count>0?cpuTotal/cpu.Count:0};
            File.WriteAllText(Path.Combine(game.QaDirectory,"performance.json"),JsonUtility.ToJson(result,true));Check(result.averageFps>=60,"1080p average sixty FPS with thirty customers");
        }
        [Serializable] public sealed class PerformanceReport
        {
            public string graphicsDevice;public int width,height,samples,renderedFrames,minimumCustomers,maximumCustomers,gpuSamples;public float averageFps,p95Milliseconds;public double gpuAverageMilliseconds,cpuAverageMilliseconds;
        }
        IEnumerator ManualEarn(int amount)
        {
            var source = game.Producers.Find(x => x.ItemId == "milk");
            var shelf = game.Shelves.Find(x => x.Accepts("milk"));
            var lane = game.Checkouts[0];
            float end = Time.realtimeSinceStartup + 200;
            while (game.Economy.Money < amount)
            {
                Check(Time.realtimeSinceStartup < end, "manual selling within deadline");
                yield return Travel(source.InteractionPoint);
                while (game.Player.Carry.Count("milk") < game.Player.Carry.Capacity && Time.realtimeSinceStartup < end) { Keys(Key.E); yield return null; }
                Keys();
                Check(game.Player.Carry.Count("milk") > 0, "manual harvest uses production inventory");
                if (report.screenshots.Count < 2) yield return Capture("02-tall-carry.png");
                yield return Travel(shelf.InteractionPoint);
                while (game.Player.Carry.Total > 0 && Time.realtimeSinceStartup < end) { Keys(Key.E); yield return null; }
                Keys();
                yield return Travel(lane.InteractionPoint);
                float wait = Time.realtimeSinceStartup + 20;
                Keys(Key.E);
                while ((lane.Queue.Count > 0 || shelf.Inventory.Total > 0) && Time.realtimeSinceStartup < wait) yield return null;
                yield return new WaitForSeconds(.5f); Keys();
                Check(game.Economy.Transactions > 0, "customer checkout paid");
            }
            yield return Capture("03-customer-checkout.png");
        }
        IEnumerator Earn(int amount)
        {
            var lane = game.Checkouts[0];
            yield return Travel(lane.InteractionPoint);
            float end = Time.realtimeSinceStartup + 240;
            int transactions=game.Economy.Transactions;float stalled=Time.realtimeSinceStartup;bool captured=false;
            while (game.Economy.Money < amount && Time.realtimeSinceStartup < end)
            {
                Keys(Key.E);
                if(game.Economy.Transactions!=transactions){transactions=game.Economy.Transactions;stalled=Time.realtimeSinceStartup;captured=false;}
                if(!captured && Time.realtimeSinceStartup-stalled>45){captured=true;WriteState("stalled-state.txt");yield return Capture("stalled.png");}
                yield return null;
            }
            Keys(); Check(game.Economy.Money >= amount, "earned " + amount + " from automation");
        }
        IEnumerator Buy(string id)
        {
            var pad = (PurchasePad)game.Stations.Find(x => x.Id == "pad_" + id);
            yield return Travel(pad.InteractionPoint);
            int money = game.Economy.Money;
            Keys(Key.E); yield return new WaitForSeconds(.35f); Keys();
            Check(game.Economy.Has(id), "purchased " + id);
            Check(game.Economy.Money == money - pad.Upgrade.cost, "exact cost " + id);
            yield return new WaitForSeconds(.2f);
            Keys(Key.F5);yield return new WaitForSeconds(.1f);Keys();
        }
        IEnumerator LoadCheck()
        {
            yield return new WaitForSeconds(1);
            var save = SaveStore.Read(game.SavePath);
            Check(save != null && game.Economy.Money == save.money, "restart loaded wallet");
            Check(game.Economy.PendingCash == SumCash(), "restart retained cash");
            foreach (string id in save.unlocked) Check(game.Economy.Has(id), "restart unlocked " + id);
            Check(game.NextReceipt >= save.nextReceipt, "receipt IDs advance across restart");
            foreach (var amount in save.transit) Check(game.Storage.Inventory.Count(amount.id) >= amount.count, "transit conserved " + amount.id);
            yield return Capture("restart-loaded.png");
        }
        IEnumerator Capture(string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;
            string path = Path.Combine(game.QaDirectory, name);
            yield return null;
            var target = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            try
            {
                var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target };
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(Camera.main, request);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Destroy(pixels);
            }
            report.screenshots.Add(name);
        }
        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        void WriteState(string name)
        {
            var lines=new List<string>{"UTC="+DateTime.UtcNow.ToString("o"),"timeScale="+Time.timeScale+" money="+game.Economy.Money+" pending="+game.Economy.PendingCash+" transactions="+game.Economy.Transactions,"player="+game.Player.transform.position+" nearest="+game.NearestStation(game.Player.transform.position)?.Id+" input="+game.Player.Use.IsPressed(),"unlocked="+string.Join(",",game.Economy.Unlocked)};
            foreach(var station in game.Stations)if(station.Inventory!=null)lines.Add(station.Id+" total="+station.Inventory.Total+" "+JsonUtility.ToJson(new InventorySave(station.Id,station.Inventory)));
            foreach(var machine in game.Machines)lines.Add(machine.Id+" input="+JsonUtility.ToJson(new InventorySave(machine.Id,machine.Input))+" running="+machine.Running+" remaining="+machine.Remaining+" batches="+machine.Batches);
            foreach(var worker in game.Workers)lines.Add(worker.Diagnostic());
            foreach(var lane in game.Checkouts)lines.Add(lane.Id+" cash="+lane.Cash+" queue="+lane.Queue.Count);
            foreach(var customer in game.Commerce.Customers)lines.Add(customer.Diagnostic());
            File.WriteAllLines(Path.Combine(game.QaDirectory,name),lines);
        }
        void Check(bool condition, string label)
        {
            if (!condition) throw new Exception("QA failed: " + label);
            report.checks.Add(label);
        }
        static Bounds Bounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        [Serializable]
        public sealed class QaReport
        {
            public bool passed;
            public string startedUtc, finishedUtc, graphicsDevice, failure;
            public float distanceWalked, playerHeight;
            public int interactions;
            public int materialSlotsChecked;
            public List<string> checks = new();
            public List<string> screenshots = new();
            public string[] runtimeErrors;
        }
    }
}
