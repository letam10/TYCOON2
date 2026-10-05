using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tycoon.Tests
{
    public sealed class BaselineTests
    {
        GameSession game,previous;
        T Add<T>(string id) where T:Component
        {
            var root=new GameObject(id);root.SetActive(false);root.transform.SetParent(game.transform);
            return root.AddComponent<T>();
        }
        [SetUp] public void SetUp()
        {
            previous=GameSession.Instance;
            var root=new GameObject("Baseline");root.SetActive(false);
            game=root.AddComponent<GameSession>();GameSession.Instance=game;
            game.Economy=new Economy(500);
            game.Player=Add<PlayerController>("Player");
            game.SavePath=Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath,"..")),"work","stage01","editor",System.Guid.NewGuid()+".json");
        }
        [TearDown] public void TearDown()
        {
            if(File.Exists(game.SavePath))File.Delete(game.SavePath);
            if(File.Exists(game.SavePath+".tmp"))File.Delete(game.SavePath+".tmp");
            Object.DestroyImmediate(game.gameObject);GameSession.Instance=previous;
        }
        [Test] public void InvalidRestorePreservesInventoryAndReservations()
        {
            var stock=new Inventory(10,true);stock.TryAdd("carrot",3);stock.TryReserve("carrot",1);
            Assert.Throws<InvalidDataException>(()=>stock.Restore(new[]{new ItemAmount("milk",1),new ItemAmount("egg",1)}));
            Assert.That(stock.Count("carrot"),Is.EqualTo(3));Assert.That(stock.Available("carrot"),Is.EqualTo(2));
        }
        [Test] public void SaveRejectsDuplicateOwners()
        {
            var save=new SaveData();
            save.inventories.Add(new InventorySave("player",new Inventory(6)));
            save.inventories.Add(new InventorySave("player",new Inventory(6)));
            Assert.Throws<InvalidDataException>(()=>SaveStore.Write(game.SavePath,save));
            Assert.That(File.Exists(game.SavePath),Is.False);
        }
        [Test] public void SaveRejectsReceiptThatIsBothPaidAndLost()
        {
            var save=new SaveData();save.receipts.Add(31);save.losses.Add(new LossRecord{receipt=31});
            Assert.Throws<InvalidDataException>(()=>SaveStore.Write(game.SavePath,save));
        }
        [Test] public void SaveRejectsCashMismatch()
        {
            var save=new SaveData{pendingCash=10};save.cash.Add(new CashSave{id="checkout",amount=20});
            Assert.Throws<InvalidDataException>(()=>SaveStore.Write(game.SavePath,save));
        }
        [Test] public void InvalidOwnerBlocksAutosaveAndLeavesOriginalFileAndGameUnchanged()
        {
            game.Player.Carry.TryAdd("carrot",2);
            var save=new SaveData{money=1};save.inventories.Add(new InventorySave("unknown",new Inventory(6)));
            SaveStore.Write(game.SavePath,save);string original=File.ReadAllText(game.SavePath);
            LogAssert.Expect(LogType.Exception,new Regex("Owner không còn tồn tại"));
            game.LoadGame();game.SaveGame();
            Assert.That(game.SaveBlocked,Is.True);Assert.That(game.Economy.Money,Is.EqualTo(500));
            Assert.That(game.Player.Carry.Count("carrot"),Is.EqualTo(2));
            Assert.That(File.ReadAllText(game.SavePath),Is.EqualTo(original));
        }
        [Test] public void RepeatedLoadPreservesCashGoodsAndMonotonicReceipt()
        {
            var counter=Add<CheckoutStation>("Checkout");counter.Id="checkout";counter.ShopId="farm";
            game.Stations.Add(counter);game.Checkouts.Add(counter);
            game.Player.Carry.TryAdd("carrot",2);
            Assert.That(game.Economy.RecordPayment(42,10),Is.True);counter.Cash=10;
            game.SaveGame();game.Player.Carry.TryRemove("carrot",2);game.Economy.CollectCash(10);
            Assert.That(File.Exists(game.SavePath),Is.True);
            game.LoadGame();game.LoadGame();
            Assert.That(game.SaveBlocked,Is.False);Assert.That(game.Player.Carry.Count("carrot"),Is.EqualTo(2));
            Assert.That(game.Economy.Money,Is.EqualTo(500));Assert.That(game.Economy.PendingCash,Is.EqualTo(10));
            Assert.That(counter.Cash,Is.EqualTo(10));Assert.That(game.Economy.Transactions,Is.EqualTo(1));
            Assert.That(game.NextReceipt,Is.EqualTo(43));Assert.That(File.Exists(game.SavePath+".tmp"),Is.False);
        }
        [Test] public void ResaveRetainsOwnersWaitingForSpawn()
        {
            game.PendingCustomers.Add(new CustomerSave{receipt=43,shop="farm",lane="checkout",remaining=20,
                order=new List<OrderLine>{new("carrot",1,10){delivered=1}},basket=new List<ItemAmount>{new("carrot",1)}});
            game.PendingWorkers.Add(new WorkerSave{id="cashier:0",upgrade="cashier",carry=new List<ItemAmount>{new("milk",1)}});
            game.SaveGame();var save=SaveStore.Read(game.SavePath);
            Assert.That(save.customers.Count,Is.EqualTo(1));Assert.That(save.customers[0].basket[0].count,Is.EqualTo(1));
            Assert.That(save.workers.Count,Is.EqualTo(1));Assert.That(save.workers[0].carry[0].id,Is.EqualTo("milk"));
        }
        [Test] public void SaveIncludesSeparateMachineInputOwner()
        {
            var machine=Add<MachineStation>("Mill");machine.Id="mill";machine.Recipe=Definitions.Recipe("mill");
            machine.Input=new Inventory(8);machine.Inventory=new Inventory(8);machine.Input.TryAdd("wheat",4);
            game.Stations.Add(machine);game.SaveGame();var save=SaveStore.Read(game.SavePath);
            Assert.That(save.inventories.Count,Is.EqualTo(3));
            Assert.That(save.inventories.Find(i=>i.id=="mill_input").items[0].count,Is.EqualTo(4));
            Assert.That(save.inventories.Find(i=>i.id=="mill").items,Is.Empty);
        }
        [Test] public void SaveLoadValidatesPersistentStationsButNotActionZones()
        {
            var storage=Add<StorageStation>("Storage");storage.Id="storage_farm";storage.AreaId="farm";storage.Inventory=new Inventory(10);
            var zone=Add<StationZone>("WithdrawZone");zone.Id="storage_farm_withdraw";zone.Target=storage;zone.Mode="withdraw";
            game.Stations.Add(storage);game.Stations.Add(zone);
            storage.Inventory.TryAdd("carrot",2);
            game.SaveGame();game.LoadGame();
            Assert.That(game.SaveBlocked,Is.False);
            Assert.That(storage.Inventory.Count("carrot"),Is.EqualTo(2));
        }
        [Test] public void UnknownAreaHasNoSharedWarehouseFallback()
        {
            var farm=Add<StorageStation>("Farm");farm.Id="farm";farm.AreaId="farm";farm.Inventory=new Inventory(10);
            var shop=Add<StorageStation>("FarmShop");shop.Id="farm_shop";shop.AreaId="farm_shop";shop.Inventory=new Inventory(10);
            var market=Add<StorageStation>("Market");market.Id="market";market.AreaId="supermarket";market.Inventory=new Inventory(10);
            game.Storage=farm;game.Stations.Add(farm);game.Stations.Add(shop);game.Stations.Add(market);
            farm.Inventory.TryAdd("carrot",3);
            Assert.That(game.StorageFor("farm"),Is.SameAs(farm));
            Assert.That(game.StorageFor("farm_shop"),Is.SameAs(shop));
            Assert.That(game.StorageFor("farm_shop"),Is.Not.SameAs(farm));
            Assert.That(game.StorageFor("supermarket"),Is.SameAs(market));
            Assert.That(game.StorageFor("unknown"),Is.Null);
            Assert.That(market.Inventory.Count("carrot"),Is.Zero);
        }
        [Test] public void EmptyCarrierCannotBuyFromShelfAtCheckout()
        {
            var lane=Add<CheckoutStation>("Checkout");lane.Id="checkout";lane.ShopId="farm";game.Checkouts.Add(lane);
            var shelf=Add<ShelfStation>("Shelf");shelf.ShopId="farm";shelf.Inventory=new Inventory(10);
            shelf.Inventory.TryAdd("carrot",3);game.Shelves.Add(shelf);
            var customer=Add<CustomerAgent>("Customer");
            customer.Restore(new CustomerSave{receipt=21,shop="farm",lane=lane.Id,phase=(int)CustomerAgent.State.Queue,
                remaining=90,order=new List<OrderLine>{new("carrot",1,10)}});
            customer.transform.position=lane.QueuePoint(customer);
            game.Player.transform.position=lane.InteractionPoint;
            Assert.That(lane.Interact(game.Player,false),Is.False);
            Assert.That(customer.Basket.Total,Is.Zero);
            Assert.That(shelf.Inventory.Count("carrot"),Is.EqualTo(3));
            Assert.That(game.Economy.Transactions,Is.Zero);
        }
        [Test] public void EmptyExpiredOrderIsTerminalAndCannotLaterPay()
        {
            var order=new OrderState(22,new[]{new OrderLine("carrot",1,10)},0,1);
            Assert.That(order.Expire(new Inventory(2),game.Economy,2),Is.True);
            Assert.That(game.Economy.IsLost(22),Is.True);
            Assert.That(game.Economy.RecordPayment(22,10),Is.False);
            Assert.That(game.Economy.Losses,Is.Empty);
            Assert.That(game.Economy.LostItems,Is.Zero);
        }
        [Test] public void ReceiptCannotProducePaymentAndLoss()
        {
            var goods=new Inventory(2);goods.TryAdd("milk",1);
            Assert.That(game.Economy.RecordPayment(23,12),Is.True);
            Assert.That(game.Economy.RecordLoss(23,goods),Is.False);
            Assert.That(game.Economy.RecordPayment(23,12),Is.False);
            Assert.That(game.Economy.Transactions,Is.EqualTo(1));
            Assert.That(goods.Count("milk"),Is.EqualTo(1));
        }
        [Test] public void InvalidReceiptHasNoFinancialEffects()
        {
            Assert.That(game.Economy.RecordPayment(0,10),Is.False);
            Assert.That(game.Economy.RecordPayment(-1,10),Is.False);
            Assert.That(game.Economy.RecordLoss(0,new Inventory(2)),Is.False);
            Assert.That(game.Economy.PendingCash,Is.Zero);
        }
        [Test] public void LegacyPurchaseCannotSpendOrUnlock()
        {
            foreach(var upgrade in Definitions.Upgrades)
                if(upgrade.kind=="legacy")
                {
                    Assert.That(game.Contribute(upgrade,5000),Is.Zero);
                    Assert.That(game.Economy.Has(upgrade.id),Is.False);
                }
            Assert.That(game.Economy.Money,Is.EqualTo(500));
            Assert.That(game.Purchases,Is.Empty);
        }
        [Test] public void WorkerCannotBePurchasedWithoutItsOwnWarehouse()
        {
            game.Economy.Unlock("mill");
            var processor=Definitions.Upgrade("processor");
            int money=game.Economy.Money;
            Assert.That(game.CanPurchase(processor,out var reason),Is.False);
            Assert.That(reason,Does.Contain("chưa có kho riêng"));
            Assert.That(game.Contribute(processor,processor.cost),Is.Zero);
            Assert.That(game.Economy.Money,Is.EqualTo(money));
            Assert.That(game.Economy.Has(processor.id),Is.False);
        }
        [Test] public void RunningMachineStillNeedsAnOperatorToAdvance()
        {
            var machine=Add<MachineStation>("Machine");
            machine.Recipe=Definitions.Recipe("mill");machine.Input=new Inventory(4);machine.Inventory=new Inventory(4);
            machine.Input.TryAdd("wheat",4);
            Assert.That(machine.Operate(.1f,game.Player.GetEntityId()),Is.True);
            float remaining=machine.Remaining;Assert.That(machine.Running,Is.True);
            machine.ReleaseOperator(game.Player.GetEntityId());
            Assert.That(machine.Operate(float.PositiveInfinity,game.Player.GetEntityId()),Is.False);
            Assert.That(machine.Operate(float.NaN,game.Player.GetEntityId()),Is.False);
            Assert.That(machine.Remaining,Is.EqualTo(remaining));
        }
    }
}
