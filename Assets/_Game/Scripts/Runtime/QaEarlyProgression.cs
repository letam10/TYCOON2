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
    public sealed partial class QaDriver
    {
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
            long money = game.Economy.Money;
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
            long openingMoney=game.Economy.Money;
            yield return ServeAndCollect(counter);
            Check(game.Economy.Transactions>0&&game.Economy.Money>openingMoney,"player collects real farm sales");
            yield return Capture("farm-starter-sale.png");

            var purchase=Definitions.Upgrade("farm_capacity2");var pad=game.Stations.Find(x=>x is PurchasePad p&&p.Upgrade==purchase) as PurchasePad;
            long initialWallet=game.Economy.Money;
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
            long cashBefore=counter.Cash;
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
            Check(collection,"checkout has a separate Collect zone");long wallet=game.Economy.Money;long pending=counter.Cash;
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
        long SumCash() { long sum = 0; foreach (var lane in game.Checkouts) sum += lane.Cash; return sum; }
        static long SumSavedCash(SaveData save) { long sum=0;foreach(var lane in save.cash)sum+=lane.amount;return sum; }
    }
}
