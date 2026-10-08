using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        BalanceFixture balance;
        double finalStarted,nextTrace,nextCheckpoint;
        string finalTask="Bắt đầu";
        bool finalResume;
        string finalSourceRun;
        double FinalSeconds=>game.Transactions.Now-finalStarted;
        readonly HashSet<string> finalMilestones=new();
        readonly string[] finalPurchaseOrder={"farm_speed2","carry10","farm_level2","farm_level3","farmer","farm_capacity2",
            "farm_speed3","farm_capacity3","farm_value2","farm_value3","farm_shop","barn","milk_line","egg_line","animal_level2","animal_level3",
            "animal_worker","counter_level2","counter_level3","cashier","transport_farm_shop","mill","dairy","mill_level2","mill_level3","processor",
            "carry16","supermarket","bakery","oven_level2","oven_level3","cook_bakery","transport_bakery","cashier_bakery","restaurant"};
        void FinalPulse()
        {
            if(balance==null)return;
            if(File.Exists(Path.Combine(game.QaDirectory,"stop-request.txt"))){game.Transactions.Checkpoint();throw new OperationCanceledException("Lượt QA đã lưu và dừng theo yêu cầu kiểm tra.");}
            double seconds=FinalSeconds;
            if(seconds>balance.maximumMinutes*60)throw new Exception("Progression vượt giới hạn "+balance.maximumMinutes+" phút: "+finalTask);
            if(game.SaveBlocked||game.RuntimeErrors.Count>0)throw new Exception("Runtime dừng: "+string.Join("; ",game.RuntimeErrors));
            if(Time.timeAsDouble>=nextTrace)
            {
                nextTrace=Time.timeAsDouble+balance.traceSeconds;
                string row=string.Join(",",seconds.ToString("F2",CultureInfo.InvariantCulture),game.Economy.Money,game.Economy.CashCollected,
                    game.Economy.PendingCash,game.Economy.Revenue,game.Economy.LostItems,game.Progression.SuccessfulOrders,
                    game.Progression.RecipeBatches("mill"),game.Progression.RecipeBatches("cheesemaker"),game.Progression.RecipeBatches("saucemaker"),game.Progression.SuccessfulOrdersAt("bakery"),finalTask.Replace(',',' '));
                File.AppendAllText(Path.Combine(game.QaDirectory,"cash-progression.csv"),row+"\n");
                File.WriteAllText(Path.Combine(game.QaDirectory,"progress-status.txt"),row+"\n"+string.Join(",",game.Economy.Unlocked));
            }
            if(Time.timeAsDouble>=nextCheckpoint){nextCheckpoint=Time.timeAsDouble+60;game.Transactions.Checkpoint();}
        }
        IEnumerator FinalWait(Func<bool> done,float seconds,string label,bool required=true)
        {
            double until=Time.timeAsDouble+seconds;
            while(!done()&&Time.timeAsDouble<until){FinalPulse();yield return null;}
            if(required&&!done())throw new Exception("Không hoàn tất "+label+": "+game.Player.InteractionReason);
        }
        IEnumerator FinalSelect(string item)
        {
            int index=Array.FindIndex(Definitions.Items,x=>x.id==item);Check(index>=0,"SKU "+item);
            for(int i=0;game.SelectedItem!=index&&i<Definitions.Items.Length;i++){Keys(Key.Q);yield return null;Keys();yield return null;}
        }
        IEnumerator FinalStash()
        {
            if(game.Player.Carry.Total==0)yield break;
            string item=game.Player.Carry.Snapshot()[0].id;
            var destination=game.Stations.Where(s=>s is StorageStation&&s.IsUnlocked&&s.Inventory.FreeFor(item)>=game.Player.Carry.Total)
                .OrderBy(s=>Vector3.SqrMagnitude(s.transform.position-game.Player.transform.position)).FirstOrDefault();
            Check(destination!=null,"còn kho nhận "+item);
            yield return Travel(FinalPoint(destination,InteractionKind.Drop));
            yield return FinalWait(()=>game.Player.Carry.Total==0,8,"cất "+item);
        }
        IEnumerator FinalGetItem(string item,int wanted,int depth=0)
        {
            if(depth>6)throw new Exception("Recipe loop "+item);
            finalTask="Lấy "+item;FinalPulse();
            if(game.Player.Carry.Total>0&&game.Player.Carry.Count(item)==0)yield return FinalStash();
            wanted=Math.Min(wanted,game.Player.Carry.Capacity);
            int attempts=0;
            while(game.Player.Carry.Count(item)<wanted&&attempts++<wanted+8)
            {
                FinalPulse();
                if(game.Player.Carry.Total>0&&game.Player.Carry.Count(item)==0)yield return FinalStash();
                var source=game.Stations.Where(s=>s is StorageStation or ShelfStation or MachineStation&&s.IsUnlocked&&s.Inventory!=null&&s.Inventory.Available(item)>0)
                    .OrderBy(s=>Vector3.SqrMagnitude(s.transform.position-game.Player.transform.position)).FirstOrDefault();
                if(source)
                {
                    yield return FinalSelect(item);yield return Travel(FinalPoint(source,InteractionKind.Pickup));
                    yield return FinalWait(()=>game.Player.Carry.Count(item)>=wanted||source.Inventory.Available(item)==0,8,"lấy kho "+item,false);
                    if(game.Player.Carry.Count(item)>0)yield break;
                }
                var producer=game.Producers.Where(p=>p.IsUnlocked&&p.ItemId==item&&!p.IsOperatedByOther(game.Player.GetEntityId()))
                    .OrderByDescending(p=>p.Animal?p.Cycle==0:p.Phase==3).ThenBy(p=>Vector3.SqrMagnitude(p.transform.position-game.Player.transform.position)).FirstOrDefault();
                if(producer)
                {
                    if(!producer.Animal&&game.Player.Carry.Count(item)>0&&game.Player.Carry.FreeFor(item)<producer.HarvestQuantity)yield break;
                    if(producer.Animal)
                    {
                        if(game.Player.Carry.Count(item)>0&&producer.Feed==0)yield break;
                        if(producer.Feed==0)
                        {
                            yield return FinalGetItem(producer.FeedItem,3,depth+1);
                            yield return Travel(FinalPoint(producer,InteractionKind.Drop));
                            yield return FinalWait(()=>producer.Feed>0,4,"cho ăn "+item);
                            yield return FinalStash();
                        }
                        if(producer.Herd==0&&producer.Breeding==0)
                        {yield return Travel(FinalPoint(producer,InteractionKind.Restock));yield return FinalWait(()=>producer.Breeding>0,4,"tái đàn "+item);}
                        yield return Travel(FinalPoint(producer,InteractionKind.Operate));
                        yield return FinalWait(()=>producer.Cycle==0&&producer.Herd>0,90,"chăm "+item,false);
                    }
                    else if(producer.Phase<2)
                    {
                        yield return Travel(FinalPoint(producer,InteractionKind.Operate));
                        yield return FinalWait(()=>producer.Phase>=2,8,"gieo/tưới "+item);
                    }
                    yield return Travel(FinalPoint(producer,InteractionKind.Pickup));
                    int before=game.Player.Carry.Count(item);
                    yield return FinalWait(()=>game.Player.Carry.Count(item)>before,12,"thu hoạch "+item,false);
                    if(producer.Animal&&game.Player.Carry.Count(item)>=wanted)yield break;
                    continue;
                }
                var machine=game.Machines.FirstOrDefault(m=>m.IsUnlocked&&m.Recipe.output==item);
                Check(machine!=null,"route có thể sản xuất "+item);
                if(game.Player.Carry.Count(item)>0)yield break;
                yield return FinalMakeBatch(machine,depth+1);
            }
            Check(game.Player.Carry.Count(item)>0,"hàng thật để tiếp tục "+item);
        }
        IEnumerator FinalMakeBatch(MachineStation machine,int depth=0)
        {
            finalTask="Chế biến "+machine.Recipe.id;FinalPulse();
            yield return FinalStash();
            if(machine.Broken)yield return FinalRepair(machine);
            if(machine.Inventory.Available(machine.Recipe.output)>0)
            {yield return FinalGetItem(machine.Recipe.output,machine.Inventory.Available(machine.Recipe.output),depth);yield return FinalStash();}
            foreach(var input in machine.Recipe.inputs)
            {
                int tries=0;
                while(machine.Input.Available(input.id)<input.count&&!machine.Running&&tries++<10)
                {
                    yield return FinalGetItem(input.id,input.count-machine.Input.Available(input.id),depth);
                    yield return Travel(FinalPoint(machine,InteractionKind.Drop));
                    yield return FinalWait(()=>game.Player.Carry.Total==0||machine.Input.FreeFor(input.id)==0,8,"cấp "+input.id);
                    yield return FinalStash();
                }
            }
            int before=machine.Batches;
            yield return Travel(FinalPoint(machine,InteractionKind.Operate));
            yield return FinalWait(()=>machine.Batches>before,35,"vận hành "+machine.Id,false);
            if(machine.Inventory.Available(machine.Recipe.output)>0)
            {yield return FinalGetItem(machine.Recipe.output,machine.Recipe.yield,depth);yield return FinalStash();}
        }
        IEnumerator FinalRepair(MachineStation machine)
        {
            if(!machine.Broken)yield break;
            finalTask="Sửa "+machine.Id;FinalPulse();
            if(!machine.RepairPaid&&game.Economy.Money<machine.RepairFee)yield return FinalCollect();
            Check(machine.RepairPaid||game.Economy.Money>=machine.RepairFee,"tiền sửa máy từ cash thực thu");
            yield return Travel(FinalPoint(machine,InteractionKind.Repair));
            yield return FinalWait(()=>!machine.Broken,12,"sửa "+machine.Id);
        }
        IEnumerator FinalCollect()
        {
            foreach(var counter in game.Checkouts.Where(c=>c.IsUnlocked&&c.Cash>0).ToArray())
            {yield return Travel(counter.CollectionPoint);yield return FinalWait(()=>counter.Cash==0,3,"thu tiền "+counter.Id);}
        }
        IEnumerator FinalBuy(string id)
        {
            var pad=game.Stations.OfType<PurchasePad>().First(p=>p.Upgrade.id==id);
            Check(game.CanPurchase(pad.Upgrade,out _)&&game.Economy.Money>=pad.Upgrade.cost-game.Contribution(id),"đủ điều kiện và cash thật mua "+id);
            finalTask="Mua "+id;FinalPulse();yield return FinalStash();yield return Travel(pad.Center);
            yield return FinalWait(()=>game.Economy.Has(id)||game.Economy.Money==0,18,"purchase "+id,false);
            if(game.Economy.Has(id)&&finalMilestones.Add(id))File.AppendAllText(Path.Combine(game.QaDirectory,"purchase-milestones.csv"),
                string.Join(",",id,FinalSeconds.ToString("F2",CultureInfo.InvariantCulture),game.Economy.CashCollected,game.Economy.Money,game.Progression.SuccessfulOrders)+"\n");
            // Rời pad qua input thật để đóng interaction lease.
            yield return WalkTo(pad.Center+Vector3.back*1.9f);
        }
        IEnumerator FinalServiceRound()
        {
            var counters=game.Checkouts.Where(c=>c.IsUnlocked&&c.ShopId!="restaurant"&&c.FrontOrder!=null).ToArray();
            var counter=counters.OrderByDescending(c=>c.ShopId=="bakery"&&game.Progression.SuccessfulOrdersAt("bakery")<60?10000:0)
                .ThenByDescending(c=>c.FrontOrder.TotalPrice).FirstOrDefault();
            if(!counter){yield return FinalGetItem("carrot",game.Player.Carry.Capacity);yield return FinalStash();yield break;}
            var order=counter.FrontOrder;long receipt=order.Receipt;
            foreach(var line in order.Lines.Where(l=>l.Remaining>0).ToArray())
            {
                if(counter.FrontOrder?.Receipt!=receipt)break;
                if(counter.Inventory.Available(line.id)>0)
                {yield return FinalStash();yield return Travel(FinalPoint(counter,InteractionKind.Serve));yield return new WaitForSeconds(1);if(order.Finished)break;}
                if(order.Finished)break;
                yield return FinalGetItem(line.id,line.Remaining);
                yield return Travel(FinalPoint(counter,InteractionKind.Serve));
                yield return FinalWait(()=>order.Finished||game.Player.Carry.Total==0||!order.Lines.Any(l=>l.Remaining>0&&game.Player.Carry.Available(l.id)>0),5,"giao "+receipt,false);
            }
            yield return FinalStash();
        }
        IEnumerator FinalPlayToRestaurant(BalanceFixture settings)
        {
            balance=settings;finalStarted=finalResume?1:game.Transactions.Now;
            File.WriteAllText(Path.Combine(game.QaDirectory,"cash-progression.csv"),"seconds,wallet,collected,pending,revenue,lostItems,orders,flourBatches,cheeseBatches,sauceBatches,bakeryOrders,action\n");
            File.WriteAllText(Path.Combine(game.QaDirectory,"purchase-milestones.csv"),"purchase,seconds,collected,wallet,orders\n");
            while(!game.Economy.Has("restaurant"))
            {
                FinalPulse();
                if(game.Economy.PendingCash>0)yield return FinalCollect();
                var broken=game.Machines.FirstOrDefault(m=>m.Broken);
                if(broken&&game.Economy.Money>=broken.RepairFee)yield return FinalRepair(broken);
                string purchase=finalPurchaseOrder.FirstOrDefault(id=>FinalPurchaseAllowed(id)&&!game.Economy.Has(id)&&game.CanPurchase(Definitions.Upgrade(id),out _)&&
                    game.Economy.Money>=Definitions.Upgrade(id).cost-game.Contribution(id));
                if(purchase!=null){yield return FinalBuy(purchase);continue;}
                bool upgraded=false;
                foreach(var crew in game.CrewStates.Where(c=>c.role is "Farmer" or "AnimalWorker" or "Processor" or "Cook").ToArray())
                    foreach(string kind in new[]{"count","speed"})
                    {
                        int level=kind=="count"?crew.count:kind=="speed"?crew.speedLevel:crew.carryLevel;
                        int cost=game.CrewUpgradeCost(crew.id,kind);
                        if(level<3&&game.Economy.Money>=cost+200&&game.UpgradeCrew(crew.id,kind)){upgraded=true;break;}
                    }
                if(upgraded){yield return null;continue;}
                if(game.Economy.Has("mill")&&!game.Economy.Has("supermarket"))
                {
                    var recipe=game.Machines.Where(m=>m.IsUnlocked&&m.AreaId=="processing"&&m.Batches<50)
                        .OrderBy(m=>m.Batches).FirstOrDefault();
                    if(recipe&&game.Economy.PendingCash<150){yield return FinalMakeBatch(recipe);continue;}
                }
                if(game.Economy.Has("bakery")&&!game.Economy.Has("cook_bakery")&&game.Progression.PlayerJobs(GameSession.CrewFor(Definitions.Upgrade("cook_bakery")))<30)
                {yield return FinalMakeBatch(game.Machines.First(m=>m.Recipe.id=="oven"));continue;}
                yield return FinalServiceRound();
            }
            yield return FinalStash();
            var kitchen=game.Machines.First(m=>m.Recipe.id=="kitchen");yield return FinalMakeBatch(kitchen);yield return FinalGetItem("meal",1);
            var table=game.Tables.FirstOrDefault(t=>t.NeedsMeal);
            yield return FinalWait(()=>game.Tables.Any(t=>t.NeedsMeal),30,"khách ngồi bàn");table=game.Tables.First(t=>t.NeedsMeal);
            long diner=table.Occupant.Receipt;
            yield return Travel(FinalPoint(table,InteractionKind.Serve));yield return FinalWait(()=>!table.NeedsMeal,5,"serve đúng bàn");
            yield return FinalWait(()=>table.Cleaning>0,12,"ăn và payment");
            yield return Travel(FinalPoint(table,InteractionKind.Operate));yield return FinalWait(()=>table.Cleaning==0,5,"player clean");
            yield return FinalCollect();game.Transactions.Checkpoint();
            Check(game.Progression.PlayerJobs(GameSession.CrewFor(Definitions.Upgrade("cook")))>0,"player cook trước hire Restaurant");
            var snapshot=game.Transactions.Snapshot();Check(snapshot.payments.Any(p=>p.order==RuntimeTransactions.OrderId(diner)&&p.collected),"Restaurant payment thực thu");
            TransactionCore.Validate(snapshot);
            double minutes=FinalSeconds/60;
            File.WriteAllText(Path.Combine(game.QaDirectory,"balance-result.json"),JsonUtility.ToJson(new BalanceResult{minutes=minutes,cashCollected=game.Economy.CashCollected,orders=game.Progression.SuccessfulOrders,zeroStart=true,noStateGrants=true,sourceRun=finalSourceRun},true));
            Check(minutes>=settings.targetMinimumMinutes&&minutes<=settings.targetMaximumMinutes,"cash collected progression within 180–240 minutes: "+minutes);
            Time.timeScale=1;
        }
        [Serializable] sealed class BalanceResult{public double minutes;public long cashCollected;public int orders;public bool zeroStart,noStateGrants;public string sourceRun;}
        bool FinalPurchaseAllowed(string id)
        {
            if(!game.Economy.Has("farm_shop"))return id is "farm_speed2" or "carry10" or "farm_level2" or "farm_level3" or "farmer" or "farm_capacity2" or "farm_speed3" or "farm_value2" or "farm_shop";
            if(id=="barn")return game.Economy.Has("supermarket");
            return true;
        }
    }
}
