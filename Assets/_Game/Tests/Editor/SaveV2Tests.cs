using NUnit.Framework;
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
    }
}
