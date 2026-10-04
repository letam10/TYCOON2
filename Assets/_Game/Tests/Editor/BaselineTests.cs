using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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
        }
        [TearDown] public void TearDown(){Object.DestroyImmediate(game.gameObject);GameSession.Instance=previous;}
        [Test] public void UnknownAreaHasNoSharedWarehouseFallback()
        {
            var farm=Add<StorageStation>("Farm");farm.Id="farm";farm.AreaId="farm";farm.Inventory=new Inventory(10);
            var market=Add<StorageStation>("Market");market.Id="market";market.AreaId="supermarket";market.Inventory=new Inventory(10);
            game.Storage=farm;game.Stations.Add(farm);game.Stations.Add(market);
            farm.Inventory.TryAdd("carrot",3);
            Assert.That(game.StorageFor("farm_shop"),Is.SameAs(farm));
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
            Assert.That(game.Economy.Losses.Count,Is.EqualTo(1));
            Assert.That(game.Economy.Losses[0].goods,Is.Empty);
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
