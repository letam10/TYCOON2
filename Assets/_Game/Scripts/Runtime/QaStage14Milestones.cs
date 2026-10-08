using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable] sealed class MilestoneFixture{public float timeScale=6;public MilestoneCase[] cases;}
        [Serializable] sealed class MilestoneCase{public string id;public int wallet,orders,batches,bakeryOrders;public string[] owned;}
        [Serializable] sealed class MilestoneEvidence{public bool seededFixture=true;public List<MilestoneRow> results=new();}
        [Serializable] sealed class MilestoneRow{public string id;public long seedWallet,collected,beforePurchase,afterPurchase;public bool purchased;}
        void SeedMilestone(MilestoneCase f)
        {
            if(!game.IsQa||!Path.GetFullPath(game.SavePath).StartsWith(Path.GetFullPath(game.QaDirectory)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Preset chỉ được ghi vào save QA riêng.");
            game.Player.CanControl=false;game.Commerce.enabled=false;game.Restaurant.enabled=false;
            game.Commerce.ResetForLoad();game.Restaurant.ResetForLoad();game.PendingCustomers.Clear();game.PendingDiners.Clear();game.PendingWorkers.Clear();
            var data=game.CaptureSaveData();var seed=RuntimeTransactions.Migrate(game,data);
            seed.money=f.wallet;seed.revenue=seed.cashCollected=seed.legacyRevenue=0;seed.legacyTransactions=f.orders;
            seed.legacyCash.Clear();seed.legacyPaid.Clear();seed.legacyLosses.Clear();seed.stacks.Clear();seed.reservations.Clear();
            seed.orders.Clear();seed.payments.Clear();seed.purchases.Clear();seed.crews.Clear();seed.jobIds.Clear();seed.consumers.Clear();
            seed.unlocked=new(f.owned??Array.Empty<string>());seed.owners.RemoveAll(o=>o.kind is OwnerKind.Customer or OwnerKind.Worker or OwnerKind.Escrow);
            foreach(var o in seed.owners){if(o.kind==OwnerKind.Player)o.capacity=6;else o.writers=new(){"player"};}
            foreach(var s in seed.stations)
            {
                s.level=ProgressionTracker.StationLevel(seed,ProgressionTracker.Family(s));s.workCount=s.playerWorkCount=s.batches=s.playerBatches=0;
                s.running=false;s.remaining=0;s.operatorId=s.jobId=s.reservationId=s.escrow=null;s.operatorUntil=0;
                s.progress=new(){id=s.id,herd=3,cycle=s.item is "milk" or "egg" or "beef"?s.cycleSeconds:0};
                if(s.kind=="machine")s.batches=s.definitionId is "mill" or "cheesemaker" or "saucemaker"?f.batches:0;
            }
            if(f.bakeryOrders>0)
            {
                string customer="customer:history";seed.owners.Add(new OwnerState{id=customer,actor=customer,location=customer,kind=OwnerKind.Customer,capacity=0});
                string counter=game.Checkouts.First(c=>c.ShopId=="bakery").Id;
                for(int i=0;i<f.bakeryOrders;i++)
                {
                    string order=RuntimeTransactions.OrderId(100000+i);int price=game.ItemPrice("bread");
                    seed.orders.Add(new OrderRuntimeState{id=order,customer=customer,counter=counter,status=OrderStatus.Complete,lines=new(){new("bread",1,price){delivered=1}}});
                    seed.payments.Add(new PaymentState{id="payment:"+order,order=order,counter=counter,amount=price,collected=true});seed.revenue+=price;seed.cashCollected+=price;
                }
                seed.legacyTransactions=Math.Max(0,f.orders-f.bakeryOrders);
            }
            void Stock(string owner,string item,int count)
            {
                var o=seed.owners.First(x=>x.id==owner);seed.stacks.Add(new ItemStackState{id="fixture:"+owner+":"+item,owner=owner,
                    location=o.kind==OwnerKind.Storage?o.location+"/"+item:o.location,item=item,quantity=count});
            }
            if(f.id=="supermarket")foreach(var m in game.Machines.Where(m=>m.AreaId=="processing"))foreach(var input in m.Recipe.inputs)Stock(m.Id+"_input",input.id,input.count);
            if(f.id=="restaurant")foreach(var input in Definitions.Recipe("oven").inputs)Stock(game.Machines.First(m=>m.Recipe.id=="oven").Id+"_input",input.id,input.count);
            if(f.id=="restaurant")foreach(var input in Definitions.Recipe("kitchen").inputs)Stock("machine_kitchen_input",input.id,input.count);
            data.customers.Clear();data.diners.Clear();data.workers.Clear();data.events=new(){lastBreakBatch=seed.stations.Sum(s=>s.batches)};data.nextReceipt=200000;data.playerX=-15.5f;data.playerZ=-3.5f;
            TransactionCore.NormalizeState(seed);TransactionCore.Validate(seed);RuntimeTransactions.Project(seed,data);
            game.Transactions.Detach();string journal=game.SavePath+".journal";if(File.Exists(journal))File.Delete(journal);
            SaveStore.Write(game.SavePath,data);game.LoadGame();Check(!game.SaveBlocked,"load near-threshold fixture "+f.id);
            game.Player.CanControl=true;
        }
        IEnumerator MilestoneSale(string shop,string item,int quantity)
        {
            yield return FinalStash();yield return FinalGetItem(item,quantity);
            var counter=game.Checkouts.First(c=>c.ShopId==shop&&c.IsUnlocked);
            var root=new GameObject("MilestoneCustomer");root.transform.SetParent(game.transform);
            root.SetActive(false);var customer=root.AddComponent<CustomerAgent>();
            customer.Restore(new CustomerSave{receipt=game.NextReceipt++,shop=shop,lane=counter.Id,phase=(int)CustomerAgent.State.Queue,remaining=90,
                order=new(){new(item,quantity,game.ItemPrice(item))},x=counter.QueuePoint(0).x,z=counter.QueuePoint(0).z});
            customer.transform.position=counter.QueuePoint(customer);
            long money=game.Economy.Money,cash=counter.Cash;
            yield return Travel(FinalZone(counter,InteractionKind.Serve).Center);yield return FinalWait(()=>customer.Order.Paid,8,"giao đơn "+item);
            Check(game.Economy.Money==money&&counter.Cash>cash,"payment at counter before collection "+shop);
            yield return Travel(counter.CashZone.Center);yield return FinalWait(()=>counter.Cash==0,4,"thu payment "+shop);
            Check(game.Economy.Money>money,"real player collection reaches purchase boundary "+shop);
            yield return FinalStash();Destroy(root);
        }
        IEnumerator FinalMilestones()
        {
            yield return new WaitForSeconds(1);Check(game.NavigationReady,"milestone navigation ready");
            var fixture=JsonUtility.FromJson<MilestoneFixture>(File.ReadAllText(GameSession.Argument("--qa-fixture","")));Time.timeScale=fixture.timeScale;
            var evidence=new MilestoneEvidence();
            foreach(var f in fixture.cases)
            {
                SeedMilestone(f);yield return new WaitForSeconds(.2f);long collectedBefore=game.Economy.CashCollected;
                if(f.id=="supermarket")
                {
                    foreach(var m in game.Machines.Where(m=>m.AreaId=="processing"))
                    {
                        yield return Travel(FinalZone(m,InteractionKind.Operate).Center);yield return FinalWait(()=>m.Batches==50,15,"49→50 batches "+m.Recipe.id);
                    }
                }
                if(f.id=="restaurant")
                {
                    var oven=game.Machines.First(m=>m.Recipe.id=="oven");yield return Travel(FinalZone(oven,InteractionKind.Operate).Center);
                    yield return FinalWait(()=>oven.Batches==1,15,"real bread batch before Restaurant gate");yield return MilestoneSale("bakery","bread",1);
                    Check(game.Progression.SuccessfulOrdersAt("bakery")==60,"59→60 successful Bakery orders");
                }
                else yield return MilestoneSale("farm","carrot",2);
                long before=game.Economy.Money;Check(before>=Definitions.Upgrade(f.id).cost,"earned boundary cash "+f.id);
                yield return FinalBuy(f.id);Check(game.Economy.Has(f.id),"purchased "+f.id);
                evidence.results.Add(new(){id=f.id,seedWallet=f.wallet,collected=game.Economy.CashCollected-collectedBefore,beforePurchase=before,afterPurchase=game.Economy.Money,purchased=true});
                game.Transactions.Checkpoint();var restored=SaveStore.Read(game.SavePath);
                Check(restored.money==game.Economy.Money&&restored.unlocked.Contains(f.id),"purchase and wallet saved exactly "+f.id);
                if(f.id is "barn" or "milk_line")
                {
                    string product=f.id=="barn"?"beef":"milk";var animal=game.Producers.First(p=>p.ItemId==product);
                    yield return FinalGetItem("carrot",3);yield return Travel(FinalZone(animal,InteractionKind.Drop).Center);
                    yield return FinalWait(()=>animal.Feed==3,5,"cho ăn đủ tuyến "+product);yield return FinalStash();
                    yield return Travel(FinalZone(animal,InteractionKind.Operate).Center);yield return FinalWait(()=>animal.Cycle==0,35,"chăm chu kỳ "+product);
                    int herd=animal.Herd;yield return MilestoneSale("farm",product,1);
                    Check(product!="beef"||animal.Herd==herd-1,"meat consumes exactly one real animal");
                }
            }
            game.Restaurant.enabled=true;var kitchen=game.Machines.First(m=>m.Recipe.id=="kitchen");
            yield return Travel(FinalZone(kitchen,InteractionKind.Operate).Center);yield return FinalWait(()=>kitchen.Batches==1,20,"player cook after Restaurant purchase");
            yield return FinalGetItem("meal",1);yield return FinalWait(()=>game.Tables.Any(t=>t.NeedsMeal),20,"diner sits and orders");
            var servedTable=game.Tables.First(t=>t.NeedsMeal);long receipt=servedTable.Occupant.Receipt;
            yield return Travel(FinalZone(servedTable,InteractionKind.Serve).Center);yield return FinalWait(()=>!servedTable.NeedsMeal,5,"serve correct Restaurant table");
            yield return FinalWait(()=>servedTable.Cleaning>0,12,"eat and Restaurant payment");Check(servedTable.Cleaning>0,"dirty table remains unavailable before cleaning");
            yield return Travel(FinalZone(servedTable,InteractionKind.Operate).Center);yield return FinalWait(()=>servedTable.Cleaning==0,6,"player cleans table");
            yield return FinalCollect();game.Transactions.Checkpoint();
            Check(game.Transactions.Snapshot().payments.Any(p=>p.order==RuntimeTransactions.OrderId(receipt)&&p.collected),"Restaurant payment collected by player");
            long wallet=game.Economy.Money,total=game.Transactions.Snapshot().stacks.Sum(s=>s.quantity),collected=game.Economy.CashCollected;
            for(int i=0;i<3;i++)
            {
                game.LoadGame();Check(!game.SaveBlocked&&game.Economy.Money==wallet&&game.Economy.CashCollected==collected,"repeat load preserves wallet and collected cash "+i);
                Check(game.Transactions.Snapshot().stacks.Sum(s=>s.quantity)==total,"repeat load does not clone owned items "+i);
            }
            yield return FinalQuickRegression(fixture.cases.First(f=>f.id=="supermarket"));
            Time.timeScale=1;File.WriteAllText(Path.Combine(game.QaDirectory,"milestone-details.json"),JsonUtility.ToJson(evidence,true));
        }
        IEnumerator FinalQuickRegression(MilestoneCase preset)
        {
            SeedMilestone(preset);var machine=game.Machines.First(m=>m.Recipe.id=="mill");
            yield return Travel(FinalZone(machine,InteractionKind.Operate).Center);
            yield return FinalWait(()=>machine.Running&&machine.Remaining<5,5,"start interrupted machine");
            yield return WalkTo(FinalZone(machine,InteractionKind.Operate).Center+Vector3.back*2);
            float remaining=machine.Remaining;int input=machine.Input.Total;string job=game.Transactions.Station(machine.Id).jobId;
            yield return new WaitForSeconds(1);Check(Mathf.Abs(machine.Remaining-remaining)<.001f,"leaving machine preserves progress");
            game.Transactions.Checkpoint();game.LoadGame();
            Check(machine.Running&&Mathf.Abs(machine.Remaining-remaining)<.001f&&machine.Input.Total==input&&game.Transactions.Station(machine.Id).jobId==job,"load preserves running job input and progress");
            yield return Travel(FinalZone(machine,InteractionKind.Operate).Center);yield return FinalWait(()=>machine.Batches==50,12,"resume same machine job");
            var counter=game.Checkouts.First(c=>c.ShopId=="farm");
            yield return FinalGetItem("carrot",2);
            var root=new GameObject("PartialAbandonment");root.SetActive(false);root.transform.SetParent(game.transform);var partial=root.AddComponent<CustomerAgent>();
            partial.Restore(new CustomerSave{receipt=game.NextReceipt++,shop="farm",lane=counter.Id,phase=(int)CustomerAgent.State.Queue,remaining=90,order=new(){new("carrot",3,10)}});
            partial.transform.position=counter.QueuePoint(partial);yield return Travel(FinalZone(counter,InteractionKind.Serve).Center);
            yield return FinalWait(()=>partial.Basket.Total==2,5,"partial order receives exactly two carrots");
            long lossBefore=game.Economy.LostItems,cashBefore=counter.Cash;yield return WalkTo(counter.WorkPoint+Vector3.forward*3);
            yield return FinalWait(()=>partial.Order.RemainingPatience(Time.time)==0,100,"partial order patience expires");
            partial.Order.Expire(partial.Basket,game.Economy,Time.time);
            Check(partial.Order.TimedOut&&partial.Basket.Total==2&&game.Economy.LostItems==lossBefore+2&&counter.Cash==cashBefore,"timeout keeps received goods and records only actual loss");
            long failed=partial.Receipt;game.Transactions.Checkpoint();game.LoadGame();
            Check(!game.Transactions.Fail(failed)&&game.Economy.LostItems==lossBefore+2,"load failed order never records loss twice");Destroy(root);
            game.Restaurant.enabled=false;game.Restaurant.ResetForLoad();game.Commerce.enabled=false;game.Commerce.ResetForLoad();
            for(int i=0;i<30;i++)
            {
                var lane=CommerceDirector.FindLane("farm_shop");var person=new GameObject("Traffic_"+i);person.transform.SetParent(game.transform);
                Vector3 spawn=lane.transform.position+new Vector3((i%6-3)*.9f,0,-9-i/6*.8f);
                UnityEngine.AI.NavMesh.SamplePosition(spawn,out var hit,3,UnityEngine.AI.NavMesh.AllAreas);person.transform.position=hit.position;
                Art.Model(i%2==0?"customer":"customer_beach",Vector3.zero,person.transform).AddComponent<ActorView>();
                var customer=person.AddComponent<CustomerAgent>();customer.Initialize();customer.Begin("farm_shop",i);game.Commerce.Customers.Add(customer);
            }
            yield return new WaitForSeconds(15);
            Check(game.Commerce.ActiveCount==30,"30 real customers coexist without exceeding active cap");
            Check(game.Commerce.Customers.All(c=>c.Agent.isOnNavMesh),"30 customer navigation agents remain on NavMesh");
            Check(game.Commerce.Customers.All(c=>c.GetComponent<CustomerNavigationRecovery>().Repaths<=3),"customer repath remains bounded during congestion");
            game.Transactions.Checkpoint();TransactionCore.Validate(game.Transactions.Snapshot());
        }
    }
}
