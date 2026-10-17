using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator Redesign()
        {
            float limit=Time.realtimeSinceStartup+30;
            while(!game.NavigationReady){Check(Time.realtimeSinceStartup<limit,"Navigation startup");yield return null;}
            yield return new WaitForSeconds(1);
            Check(game.Economy.Money==0,"Fresh wallet is zero");
            Check(game.Player.Carry.Capacity==6&&game.Player.Carry.SingleItem,"Carry one SKU with six slots");
            Check(game.Producers.FindAll(x=>x.IsUnlocked&&x.ItemId=="carrot").Count==3,"Three free carrot plots");
            Check(!game.CanPurchase(Definitions.Upgrade("farmer"),out _),"No first hire before operational and upgrade gates");
            yield return Capture("01-starter.png");
            var crop=game.Producers.Find(x=>x.IsUnlocked&&x.ItemId=="carrot");
            yield return Travel(crop.InteractionPoint);
            limit=Time.realtimeSinceStartup+45;
            while(game.Player.Carry.Total<6){Check(Time.realtimeSinceStartup<limit,"Manual crop phases produce carry");yield return null;}
            yield return Capture("02-carry-six.png");
            var counter=game.Checkouts.Find(x=>x.ShopId=="farm"&&x.IsUnlocked);
            var service=game.Stations.Find(x=>x.Id==counter.Id+"_serve");
            yield return Travel(service?service.InteractionPoint:counter.InteractionPoint+Vector3.right*2);
            limit=Time.realtimeSinceStartup+100;
            while(game.Economy.Transactions==0){Check(Time.realtimeSinceStartup<limit,"Direct service completes a real order");yield return null;}
            Check(game.Economy.Money==0&&game.Economy.PendingCash>0,"Sale stays as physical cash");
            yield return Capture("03-first-sale.png");
            var collect=game.Stations.Find(x=>x.Id==counter.Id+"_cash");
            yield return Travel(collect?collect.InteractionPoint:counter.InteractionPoint);
            limit=Time.realtimeSinceStartup+10;
            while(game.Economy.Money==0){Check(Time.realtimeSinceStartup<limit,"Player collects cash");yield return null;}
            long balance=game.Economy.Money;
            var pad=game.Stations.Find(x=>x is PurchasePad p&&p.Upgrade.id=="barn");
            yield return Travel(pad.InteractionPoint);
            limit=Time.realtimeSinceStartup+15;
            while(game.Contribution("barn")==0){Check(Time.realtimeSinceStartup<limit,"Partial pad contributions");yield return null;}
            yield return Travel(pad.InteractionPoint+Vector3.left*3);
            int contributed=game.Contribution("barn");
            Check(contributed>0&&contributed<500&&!game.Economy.Has("barn"),"Partial purchase does not unlock");
            Check(game.Economy.Money+contributed==balance,"Wallet and contribution conserved");
            Keys(Key.F5);yield return null;Keys();yield return new WaitForSeconds(.2f);
            var saved=SaveStore.Read(game.SavePath);
            Check(saved.version==2&&saved.purchases.Exists(x=>x.id=="barn"&&x.paid==contributed),"Save v2 persists partial purchase");
            Check(saved.customers.Count>0&&saved.transit.Count==0,"Customers saved separately from warehouse");
            Check(game.RuntimeErrors.Count==0,"No runtime errors in starter loop");
            yield return Capture("04-partial-pad-save.png");
        }

        IEnumerator LivestockAndCrew()
        {
            Time.timeScale=4f;
            game.Commerce.enabled=false;
            float navigationDeadline=Time.realtimeSinceStartup+30;
            while(!game.NavigationReady&&Time.realtimeSinceStartup<navigationDeadline)yield return null;
            Check(game.NavigationReady&&game.Transactions!=null&&game.Transactions.Ready,"stage 06/07 scene, NavMesh and transaction core ready");
            Check(game.Economy.Money==0&&game.Player.Carry.Total==0,"livestock route begins from zero money and empty carry");
            Check(game.Progression.PlayerJobs(GameSession.CrewFor(Definitions.Upgrade("farmer")))==0,"first hire has no player jobs before work");
            Check(!game.CanPurchase(Definitions.Upgrade("farmer"),out _),"first employee stays locked before station level three and thirty self-done jobs");
            var counter=game.Checkouts.Find(x=>x.Id=="checkout_farm");
            Check(counter&&counter.AcceptsItem("beef"),"farm counter has the basic livestock sale route");
            Check(Definitions.Upgrade("barn").cost==500&&Definitions.Upgrade("milk_line").cost==1000&&Definitions.Upgrade("egg_line").cost==1000,"meat and milk/egg packages cost 500 and 1000");
            yield return EarnCarrotRevenue(counter,100);
            var farmLevelPad=game.Stations.Find(x=>x is PurchasePad p&&p.Upgrade.id=="farm_level2") as PurchasePad;
            Check(farmLevelPad&&game.Economy.Money>=100,"earned carrot sales cover farm level two prerequisite");
            long farmLevelBalance=game.Economy.Money;
            yield return Travel(PurchaseApproach(farmLevelPad));
            Check(farmLevelPad.Contains(game.Player.transform.position),"farm level two pad zone is reachable beside its collision footprint");
            float farmLevelDeadline=Time.realtimeSinceStartup+12;
            while(!game.Economy.Has("farm_level2")&&Time.realtimeSinceStartup<farmLevelDeadline){Keys(Key.E);yield return null;}
            Keys();Check(game.Economy.Has("farm_level2")&&game.Economy.Money==farmLevelBalance-100,"farm level two prerequisite is purchased once");
            yield return EarnCarrotRevenue(counter,500);
            long beforePurchase=game.Economy.Money;
            var pad=game.Stations.Find(x=>x is PurchasePad p&&p.Upgrade.id=="barn") as PurchasePad;
            Check(pad&&beforePurchase>=500,"earned carrot sales cover one complete meat route");
            yield return Travel(PurchaseApproach(pad));
            Check(pad.Contains(game.Player.transform.position),"purchase pad zone is reachable beside its collision footprint");
            float purchaseDeadline=Time.realtimeSinceStartup+12;
            while(!game.Economy.Has("barn")&&Time.realtimeSinceStartup<purchaseDeadline){Keys(Key.E);yield return null;}
            Keys();Check(game.Economy.Has("barn")&&game.Economy.Money==beforePurchase-500,"one barn purchase opens the complete meat route");
            var beef=game.Producers.Find(x=>x.Id=="ranch_beef");
            Check(beef&&beef.IsUnlocked&&beef.Herd==3&&beef.FeedItem=="animal_feed","barn unlock exposes animals and animal feed");
            var crops=game.Producers.FindAll(x=>x.ItemId=="carrot");int cropCursor=0;
            yield return PrepareAnimalFeed();
            var feed=FindZone(beef,InteractionKind.Drop);Check(feed,"livestock station has a distinct feed/drop zone");
            int feedBefore=beef.Feed;
            float feedDeadline=Time.realtimeSinceStartup+12;
            while(beef.Feed==0&&Time.realtimeSinceStartup<feedDeadline)
            {
                yield return Travel(feed.Center);yield return null;
            }
            Check(beef.Feed>feedBefore&&game.Player.Carry.Total==0,"player feeds the purchased herd with a real animal feed stack");
            float cycleDeadline=Time.realtimeSinceStartup+55;
            while(beef.Cycle>0&&Time.realtimeSinceStartup<cycleDeadline)yield return null;
            Check(beef.Cycle==0&&beef.Herd==3,"fed animal completes one real production cycle");
            yield return Travel(FindZone(beef,InteractionKind.Pickup).Center);
            float harvestDeadline=Time.realtimeSinceStartup+10;
            while(game.Player.Carry.Count("beef")==0&&Time.realtimeSinceStartup<harvestDeadline)yield return null;
            Check(game.Player.Carry.Count("beef")==1&&beef.Herd==2,"harvesting meat consumes one actual animal and carries its output");
            CreateFarmQaCustomer(counter,1001,"beef");
            Check(counter.Queue.Count>0,"farm customer enters FIFO after route unlock");
            var order=counter.Queue[0].Order;
            Check(order.Lines.Count==1&&order.Lines[0].id=="beef"&&order.Lines[0].requested==1,"livestock order requests one beef without a hidden purchase");
            Check(order.Lines[0].unitPrice==game.ItemPrice("beef"),"livestock order snapshots its unit price");
            long walletBeforeSale=game.Economy.Money;long cashBeforeSale=counter.Cash;
            var serve=FindZone(counter,InteractionKind.Serve);yield return Travel(serve.Center);
            float paymentDeadline=Time.realtimeSinceStartup+20;
            while(!order.Paid&&Time.realtimeSinceStartup<paymentDeadline)yield return null;
            Check(order.Paid&&counter.Cash==cashBeforeSale+game.ItemPrice("beef"),"real meat delivery creates deferred counter payment");
            Check(game.Economy.Money==walletBeforeSale,"payment stays out of wallet until player collects it");
            var collect=FindZone(counter,InteractionKind.Collect);yield return Travel(collect.Center);
            float collectDeadline=Time.realtimeSinceStartup+8;
            while(game.Economy.Money==walletBeforeSale&&Time.realtimeSinceStartup<collectDeadline)yield return null;
            Check(game.Economy.Money==walletBeforeSale+game.ItemPrice("beef"),"player Collect zone settles the livestock payment once");
            yield return Capture("farm-livestock-sale.png");

            game.Commerce.enabled=false;
            var farmStorage=game.StorageFor("farm");var shopStorage=game.StorageFor("farm_shop");
            if(game.Player.Carry.Total==0)yield return HarvestStarterCrop(crops[cropCursor++%crops.Count]);
            var storeDrop=FindZone(farmStorage,InteractionKind.Drop);yield return Travel(storeDrop.Center);
            Check(farmStorage.Inventory.Count("carrot")>0,"area warehouse stores a typed crop stack");
            Check(game.Transactions.Snapshot().stacks.Where(x=>x.owner==farmStorage.Id).All(x=>x.location=="farm/"+x.item),"warehouse locations key area and ItemType");
            Check(farmStorage.SharedReserveThreshold("animal_feed")>=1,"shared feed reserves protect animal feed from shelf restocking");
            string reservation=game.Transactions.Reserve(farmStorage.Inventory,shopStorage.Inventory,"carrot",1,game.Player.GetEntityId());
            Check(!string.IsNullOrEmpty(reservation)&&farmStorage.Inventory.Reserved("carrot")==1&&shopStorage.Inventory.ReservedSpace("carrot")==1,"source and destination capacity stay reserved together");
            Check(shopStorage.ReservationSummary.Contains("1 chờ nhận"),"reserved item and destination space are visible at storage");
            Check(game.Transactions.Release(reservation,game.Player.GetEntityId()),"released logistics reservations return available capacity");
            game.SaveGame();var save=SaveStore.Read(game.SavePath);
            Check(save!=null&&save.transactionState.schemaVersion>=2&&save.transactionState.stations.Any(x=>x.id==beef.Id&&x.progress.herd==beef.Herd),"livestock runtime state persists in transaction save");
            Check(game.RuntimeErrors.Count==0,"livestock and logistics loop has no runtime errors");
        }
        IEnumerator EarnCarrotRevenue(CheckoutStation counter,int target)
        {
            var service=FindZone(counter,InteractionKind.Serve);var collect=FindZone(counter,InteractionKind.Collect);
            var crops=game.Producers.FindAll(x=>x.ItemId=="carrot");int cursor=0;long collectedBefore=game.Economy.CashCollected;float deadline=Time.realtimeSinceStartup+600;
            while(game.Economy.CashCollected-collectedBefore<target)
            {
                Check(Time.realtimeSinceStartup<deadline,"earn the 500 xu route purchase through real carrot sales");
                if(counter.Queue.Count==0)
                {
                    if(game.Player.Carry.Total>0)yield return DepositAtCounter(counter);
                    while(counter.Inventory.Available("carrot")<3)
                    {
                        yield return HarvestStarterCrop(crops[cursor++%crops.Count]);
                        yield return DepositAtCounter(counter);
                    }
                    CreateFarmQaCustomer(counter,game.NextReceipt);
                }
                var order=counter.Queue[0].Order;
                Check(order.Lines.Count==1&&order.Lines[0].id=="carrot"&&order.Lines[0].unitPrice==10,"zero-money farm customers buy one-to-three base-price carrots");
                long walletBeforeOrder=game.Economy.Money,cashBeforeOrder=counter.Cash,lostBeforeOrder=game.Economy.LostItems;
                float orderDeadline=Time.realtimeSinceStartup+Mathf.Max(3,order.RemainingPatience(Time.time)/Time.timeScale+5);
                while(!order.Paid&&!order.TimedOut&&Time.realtimeSinceStartup<orderDeadline)
                {
                    if(game.Player.Carry.Available("carrot")==0&&counter.Inventory.Available("carrot")==0)
                    {
                        yield return HarvestStarterCrop(crops[cursor++%crops.Count]);
                        yield return DepositAtCounter(counter);
                    }
                    int delivered=order.Lines[0].delivered;yield return Travel(service.Center);
                    float responseDeadline=Time.realtimeSinceStartup+4;
                    while(order.Lines[0].delivered==delivered&&!order.Paid&&!order.TimedOut&&Time.realtimeSinceStartup<responseDeadline&&
                        (game.Player.Carry.Available("carrot")>0||counter.Inventory.Available("carrot")>0))yield return null;
                }
                if(order.TimedOut)
                {
                    int received=order.Lines.Sum(x=>x.delivered);
                    Check(!order.Paid&&game.Economy.Money==walletBeforeOrder&&counter.Cash==cashBeforeOrder,"expired customer receives no payment");
                    Check(game.Economy.LostItems-lostBeforeOrder==received,"only carrots received before timeout become loss");
                    continue;
                }
                Check(order.Paid,"carrot order completes before its patience expires");
                long balance=game.Economy.Money;
                var drop=FindZone(counter,InteractionKind.Drop);
                Check(drop,"counter exposes a separate Drop zone before payment collection");
                if(game.Player.Carry.Total>0)
                {
                    yield return Travel(drop.Center);
                    float dropDeadline=Time.realtimeSinceStartup+6;
                    while(game.Player.Carry.Total>0&&Time.realtimeSinceStartup<dropDeadline)yield return null;
                    Check(game.Player.Carry.Total==0,"remaining carrot carry is dropped before payment collection");
                }
                yield return Travel(collect.Center);
                float cashDeadline=Time.realtimeSinceStartup+6;
                while(game.Economy.Money==balance&&Time.realtimeSinceStartup<cashDeadline)yield return null;
                Check(game.Economy.Money>balance,"player collects each carrot payment at the counter");
            }
            if(game.Economy.CashInHand<target)
            {
                var safe=game.Stations.Find(x=>x is SafeStation) as SafeStation;
                Check(safe&&safe.Withdraw!=null,"starter safe has a withdraw zone before purchase");
                yield return Travel(safe.Withdraw.Center);
                float withdrawDeadline=Time.realtimeSinceStartup+8;
                while(game.Economy.CashInHand<target&&Time.realtimeSinceStartup<withdrawDeadline)yield return null;
            }
            Check(game.Economy.CashCollected-collectedBefore>=target&&game.Economy.CashInHand>=target,"carrot sales fund the livestock bundle from zero");
        }
        CustomerAgent CreateFarmQaCustomer(CheckoutStation counter,long sequence,string forcedItem=null)
        {
            var root=new GameObject("Stage67Customer_"+sequence);root.transform.SetParent(game.Commerce.transform);
            var model=Art.Model("customer",Vector3.zero,root.transform);model.AddComponent<ActorView>();
            var customer=root.AddComponent<CustomerAgent>();customer.Initialize();
            if(!UnityEngine.AI.NavMesh.SamplePosition(counter.QueuePoint(customer),out var point,3,UnityEngine.AI.NavMesh.AllAreas))throw new Exception("QA customer cannot reach farm FIFO.");
            root.transform.position=point.position;customer.Agent.Warp(point.position);
            if(string.IsNullOrEmpty(forcedItem))customer.Begin("farm",unchecked((int)sequence));
            else
            {
                var exit=TownLayout.Spawn((int)(sequence%3));
                customer.Restore(new CustomerSave{receipt=sequence,shop=counter.ShopId,lane=counter.Id,
                    phase=(int)CustomerAgent.State.Queue,queueIndex=counter.Queue.Count,remaining=90,
                    order=new(){new OrderLine(forcedItem,1,game.ItemPrice(forcedItem))},x=point.position.x,y=point.position.y,z=point.position.z,
                    exitX=exit.x,exitY=exit.y,exitZ=exit.z});
            }
            game.Commerce.Customers.Add(customer);return customer;
        }
        Vector3 PurchaseApproach(PurchasePad pad)
        {
            Vector3[] directions={Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
            foreach(Vector3 direction in directions)
            {
                Vector3 candidate=pad.InteractionPoint+direction*.72f;
                if(UnityEngine.AI.NavMesh.SamplePosition(candidate,out var hit,.35f,UnityEngine.AI.NavMesh.AllAreas)&&pad.Contains(hit.position))
                    return hit.position;
            }
            throw new InvalidOperationException("Không có NavMesh approach trong vùng purchase pad "+pad.Id);
        }
        IEnumerator PrepareAnimalFeed()
        {
            var feedmill=game.Machines.Find(x=>x.Id=="machine_feedmill");
            Check(feedmill&&feedmill.IsUnlocked,"feedmill route is unlocked before livestock feed");
            var input=FindZone(feedmill,InteractionKind.Drop);
            var output=FindZone(feedmill,InteractionKind.Pickup);
            Check(input&&output,"feedmill exposes separate input and output zones");
            foreach(string item in new[]{"wheat","corn","soybean"})
            {
                var crop=game.Producers.Find(x=>x.ItemId==item&&x.IsUnlocked);
                Check(crop,"feedmill has an unlocked "+item+" source crop");
                yield return HarvestStarterCrop(crop);
                yield return Travel(input.Center);
                float dropDeadline=Time.realtimeSinceStartup+8;
                while(game.Player.Carry.Total>0&&Time.realtimeSinceStartup<dropDeadline)yield return null;
                Check(game.Player.Carry.Total==0,"feedmill receives the complete "+item+" input stack");
            }
            float batchDeadline=Time.realtimeSinceStartup+20;
            while(feedmill.Inventory.Available("animal_feed")==0&&Time.realtimeSinceStartup<batchDeadline)yield return null;
            Check(feedmill.Inventory.Available("animal_feed")>0,"feedmill completes one animal feed batch");
            yield return Travel(output.Center);
            float pickupDeadline=Time.realtimeSinceStartup+8;
            while(game.Player.Carry.Available("animal_feed")==0&&Time.realtimeSinceStartup<pickupDeadline)yield return null;
            Check(game.Player.Carry.Available("animal_feed")>0,"player picks one animal feed stack");
        }
        StationZone FindZone(Station target,InteractionKind kind)=>game.Stations.Find(x=>x is StationZone zone&&zone.Target==target&&zone.Kind==kind) as StationZone;
    }
}
