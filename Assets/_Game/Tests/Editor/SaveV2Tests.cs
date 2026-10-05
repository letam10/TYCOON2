using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;
namespace Tycoon.Tests
{
    public sealed class SaveV2Tests
    {
        [Test] public void AbandonedPartiallyDeliveredOrderSurvivesReloadWithoutRefund()
        {
            var economy=new Economy();var source=new Inventory(6,true);source.TryAdd("carrot",2);var basket=new Inventory(10);
            var order=new OrderState(101,new[]{new OrderLine("carrot",3,10)},0);
            Assert.That(order.Deliver(source,basket,economy,10),Is.EqualTo(2));
            Assert.That(order.Expire(basket,economy,90),Is.True);
            var save=new SaveData{pendingCash=economy.PendingCash,losses=economy.Losses};
            save.customers.Add(new CustomerSave{receipt=101,order=order.SnapshotLines(),remaining=0,timedOut=true,phase=3,basket=basket.Snapshot()});
            var restored=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            var bank=new Economy();bank.Restore(restored.money,restored.revenue,restored.transactions,restored.unlocked,restored.pendingCash,restored.receipts,restored.losses);
            var held=new Inventory(10);held.Restore(restored.customers[0].basket);
            var resumed=OrderState.Restore(restored.customers[0],200);
            Assert.That(held.Count("carrot"),Is.EqualTo(2));Assert.That(bank.LostItems,Is.EqualTo(2));Assert.That(bank.Money,Is.Zero);
            Assert.That(resumed.Expire(held,bank,300),Is.False);Assert.That(bank.LossCount,Is.EqualTo(1));
        }
        [Test] public void ReceiptAndPendingCashSurviveRoundTripAndCannotPayAgain()
        {
            var bank=new Economy();Assert.That(bank.RecordPayment(9,30),Is.True);
            var save=new SaveData{pendingCash=bank.PendingCash,revenue=bank.Revenue,transactions=bank.Transactions,receipts=bank.ReceiptIds};
            var read=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));var restored=new Economy();
            restored.Restore(0,read.revenue,read.transactions,read.unlocked,read.pendingCash,read.receipts,read.losses);
            Assert.That(restored.RecordPayment(9,30),Is.False);Assert.That(restored.CollectCash(100),Is.EqualTo(30));Assert.That(restored.CollectCash(100),Is.Zero);
        }
        [Test] public void DestinationReservationPreventsAnotherActorTakingWorkerSpace()
        {
            var carried=new Inventory(3,true);carried.TryAdd("carrot",3);var target=new Inventory(3);
            Assert.That(target.TryReserveSpace("carrot",3),Is.True);Assert.That(target.TryAdd("milk",1),Is.False);
            Assert.That(Inventory.TransferIntoReservedSpace(carried,target,"carrot",3),Is.EqualTo(3));Assert.That(carried.Total,Is.Zero);
            Assert.That(target.Total,Is.EqualTo(3));
        }
        [Test] public void InitialEconomyCatalogMatchesApprovedStarterPackages()
        {
            Assert.That(Definitions.Item("carrot").price,Is.EqualTo(10));Assert.That(Definitions.Upgrade("barn").cost,Is.EqualTo(500));
            Assert.That(Definitions.Upgrade("milk_line").cost,Is.EqualTo(1000));Assert.That(Definitions.Upgrade("egg_line").cost,Is.EqualTo(1000));
        }
        [Test] public void TransactionSaveV2SurvivesRepeatedReadsWithoutCloningOwnersOrMoney()
        {
            var fixture=JsonUtility.FromJson<SaveFixture>(File.ReadAllText(FixturePath("stage08-save-v2-seed.json")));
            var state=new TransactionCore(TransactionCoreTests.Seed()).Snapshot();
            var save=new SaveData{transactionVersion=SaveStore.CurrentTransactionVersion,transactionState=state,money=fixture.money};
            save.cash.Add(new CashSave{id="counter",amount=0});
            string path=TemporarySavePath();
            try
            {
                SaveStore.Write(path,save);
                var first=SaveStore.Read(path);var second=SaveStore.Read(path);
                Assert.That(first.transactionVersion,Is.EqualTo(SaveStore.CurrentTransactionVersion));
                Assert.That(first.transactionState.stacks.Find(x=>x.owner=="storage"&&x.item=="carrot").quantity,Is.EqualTo(fixture.storageCarrots));
                Assert.That(second.transactionState.stacks.Find(x=>x.owner=="storage"&&x.item=="carrot").quantity,Is.EqualTo(fixture.storageCarrots));
                Assert.That(second.money,Is.EqualTo(fixture.money));Assert.That(second.transactionState.revision,Is.Zero);
            }
            finally{DeleteTemporarySave(path);}
        }
        [Test] public void TransactionVersionOneIsNormalizedAndUpgradedToVersionTwo()
        {
            var state=TransactionCoreTests.Seed();state.schemaVersion=1;
            var save=new SaveData{transactionVersion=1,transactionState=state,money=state.money};save.cash.Add(new CashSave{id="counter"});
            string path=TemporarySavePath();
            try
            {
                SaveStore.Write(path,save);var read=SaveStore.Read(path);
                Assert.That(read.transactionVersion,Is.EqualTo(SaveStore.CurrentTransactionVersion));
                Assert.That(read.transactionState.schemaVersion,Is.EqualTo(2));
            }
            finally{DeleteTemporarySave(path);}
        }
        static string FixturePath(string name)=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"mod","test",name);
        static string TemporarySavePath()=>Path.Combine(Directory.GetParent(Application.dataPath).FullName,"work","stage08-save-tests",Guid.NewGuid().ToString("N"),"save-v2.json");
        static void DeleteTemporarySave(string path)
        {string directory=Path.GetDirectoryName(path);if(Directory.Exists(directory))Directory.Delete(directory,true);}
        [Serializable]sealed class SaveFixture{public int money,storageCarrots;}
    }
}
