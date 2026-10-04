using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CommerceV2Tests
    {
        GameSession game;
        GameSession previousSession;
        CheckoutStation lane;

        T Component<T>(string name) where T : Component
        {
            var root = new GameObject(name); root.SetActive(false); root.transform.SetParent(game.transform);
            return root.AddComponent<T>();
        }

        [SetUp]
        public void SetUp()
        {
            previousSession = GameSession.Instance;
            var root = new GameObject("CommerceTestSession"); root.SetActive(false);
            game = root.AddComponent<GameSession>(); GameSession.Instance = game;
            lane = Component<CheckoutStation>("Checkout"); lane.Id = "checkout_farm"; lane.ShopId = "farm";
            game.Checkouts.Add(lane);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(game.gameObject);
            GameSession.Instance = previousSession;
        }

        CustomerAgent Customer(long receipt, int queueIndex, float remaining, params OrderLine[] lines)
        {
            var customer = Component<CustomerAgent>("Customer");
            customer.Restore(new CustomerSave
            {
                receipt = receipt, shop = "farm", lane = lane.Id, phase = (int)CustomerAgent.State.Queue,
                queueIndex = queueIndex, remaining = remaining, order = new List<OrderLine>(lines)
            });
            customer.transform.position = lane.QueuePoint(customer);
            return customer;
        }

        [Test]
        public void GeneratedOrdersHaveOneToThreeUnlockedProductsAndNinetySeconds()
        {
            var shelf = Component<ShelfStation>("Shelf"); shelf.ShopId = "farm";
            shelf.Inventory = new Inventory(20); shelf.AllowedItems = new[] { "carrot", "tomato", "milk", "beef" };
            game.Shelves.Add(shelf);
            foreach (string id in shelf.AllowedItems)
            {
                var producer = Component<ProductionStation>(id); producer.ItemId = id;
                producer.Requirement = id == "beef" ? "barn" : ""; game.Producers.Add(producer);
            }
            var customer = Component<CustomerAgent>("Customer");
            for (int i = 0; i < 20; i++)
            {
                customer.Begin("farm", i);
                Assert.That(customer.Order.Lines.Count, Is.InRange(1, 3));
                Assert.That(customer.Order.RemainingPatience(Time.time), Is.EqualTo(90).Within(.1f));
                var seen = new HashSet<string>();
                foreach (var line in customer.Order.Lines)
                {
                    Assert.That(line.id, Is.Not.EqualTo("beef")); Assert.That(seen.Add(line.id), Is.True);
                    Assert.That(line.requested, Is.EqualTo(1)); Assert.That(line.delivered, Is.Zero);
                    Assert.That(line.unitPrice, Is.EqualTo(game.ItemPrice(line.id)));
                }
                Assert.That(lane.Queue.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void SharedDeliveryKeepsPartialGoodsAndPaysOnceOnlyAfterCompletion()
        {
            var customer = Customer(10, 0, 90, new OrderLine("carrot", 1, 10), new OrderLine("tomato", 1, 20));
            var playerSource = new Inventory(5); playerSource.TryAdd("carrot", 1); playerSource.TryAdd("milk", 1);
            int openingMoney = game.Economy.Money;
            Assert.That(lane.Serve(playerSource), Is.True);
            Assert.That(customer.Basket.Count("carrot"), Is.EqualTo(1));
            Assert.That(playerSource.Count("milk"), Is.EqualTo(1));
            Assert.That(lane.Queue, Does.Contain(customer));
            Assert.That(game.Economy.PendingCash, Is.Zero); Assert.That(game.Economy.Transactions, Is.Zero);
            var workerSource = new Inventory(2); workerSource.TryAdd("tomato", 1);
            Assert.That(lane.Serve(workerSource), Is.True);
            Assert.That(customer.Basket.Total, Is.EqualTo(2));
            Assert.That(lane.Queue, Is.Empty); Assert.That(customer.Current, Is.EqualTo(CustomerAgent.State.Leaving));
            Assert.That(lane.Cash, Is.EqualTo(30)); Assert.That(game.Economy.PendingCash, Is.EqualTo(30));
            Assert.That(game.Economy.Money, Is.EqualTo(openingMoney));
            Assert.That(lane.Serve(workerSource), Is.False); Assert.That(game.Economy.Transactions, Is.EqualTo(1));
            var player = Component<PlayerController>("Player");
            Assert.That(lane.Interact(player, true), Is.True);
            Assert.That(game.Economy.Money, Is.EqualTo(openingMoney + 30));
            Assert.That(lane.Cash, Is.Zero); Assert.That(game.Economy.PendingCash, Is.Zero);
            Assert.That(lane.Interact(player, true), Is.False);
        }

        [Test]
        public void CheckoutServesOnlyFrontCustomerAfterTheyReachTheCounter()
        {
            var first = Customer(20, 0, 90, new OrderLine("milk", 1, 12));
            var second = Customer(21, 1, 90, new OrderLine("carrot", 1, 10));
            var source = new Inventory(5); source.TryAdd("carrot", 1);
            Assert.That(lane.FrontOrder, Is.SameAs(first.Order));
            Assert.That(lane.Serve(source), Is.False); Assert.That(second.Basket.Total, Is.Zero);
            source.TryAdd("milk", 1); first.transform.position = Vector3.one * 10;
            Assert.That(lane.Serve(source), Is.False); Assert.That(source.Count("milk"), Is.EqualTo(1));
            first.transform.position = lane.QueuePoint(first);
            Assert.That(lane.Serve(source), Is.True);
            Assert.That(lane.FrontOrder, Is.SameAs(second.Order)); Assert.That(second.Basket.Total, Is.Zero);
            second.transform.position = lane.QueuePoint(second);
            Assert.That(lane.Serve(source), Is.True);
            Assert.That(game.Economy.Transactions, Is.EqualTo(2)); Assert.That(lane.Cash, Is.EqualTo(22));
        }

        [Test]
        public void ExpiredPartialOrderKeepsGoodsWithoutPaymentOrWarehouseRefund()
        {
            var customer = Customer(30, 0, 0, new OrderLine("carrot", 1, 10) { delivered = 1 }, new OrderLine("tomato", 1, 20));
            customer.Basket.TryAdd("carrot", 1);
            var storage = Component<StorageStation>("Warehouse"); storage.Inventory = new Inventory(10); game.Storage = storage;
            var source = new Inventory(5); source.TryAdd("tomato", 1);
            int openingMoney = game.Economy.Money;
            Assert.That(lane.Serve(source), Is.False);
            Assert.That(customer.Order.TimedOut, Is.True); Assert.That(lane.Queue, Is.Empty);
            Assert.That(customer.Basket.Count("carrot"), Is.EqualTo(1)); Assert.That(source.Count("tomato"), Is.EqualTo(1));
            Assert.That(storage.Inventory.Total, Is.Zero); Assert.That(game.Economy.IsLost(30), Is.True);
            Assert.That(game.Economy.Losses[0].goods[0].count, Is.EqualTo(1));
            Assert.That(game.Economy.Transactions, Is.Zero); Assert.That(game.Economy.PendingCash, Is.Zero);
            Assert.That(game.Economy.Money, Is.EqualTo(openingMoney));
            Assert.That(customer.Order.Expire(customer.Basket, game.Economy, Time.time), Is.False);
            Assert.That(game.Economy.LostItems, Is.EqualTo(1));
        }

        [Test]
        public void CustomerSnapshotRoundTripPreservesQueueGoodsOrderPriceAndRemainingTime()
        {
            var first = Customer(40, 0, 60, new OrderLine("milk", 1, 12));
            var second = Customer(41, 1, 47.5f, new OrderLine("carrot", 1, 15) { delivered = 1 }, new OrderLine("tomato", 1, 26));
            second.Basket.TryAdd("carrot", 1);
            var firstSaved = first.Snapshot();
            var saved = JsonUtility.FromJson<CustomerSave>(JsonUtility.ToJson(second.Snapshot()));
            Assert.That(saved.queueIndex, Is.EqualTo(1));
            lane.Queue.Clear();
            second.Restore(saved); first.Restore(firstSaved);
            Assert.That(lane.Queue[0], Is.SameAs(first)); Assert.That(lane.Queue[1], Is.SameAs(second));
            Assert.That(second.Receipt, Is.EqualTo(41)); Assert.That(second.Lane, Is.SameAs(lane));
            Assert.That(second.Order.RemainingPatience(Time.time), Is.EqualTo(47.5f).Within(.1f));
            Assert.That(second.Order.Lines[0].delivered, Is.EqualTo(1));
            Assert.That(second.Order.Lines[1].Remaining, Is.EqualTo(1));
            Assert.That(second.Order.TotalPrice, Is.EqualTo(41)); Assert.That(second.Basket.Count("carrot"), Is.EqualTo(1));
            saved.order[0].delivered = 0; saved.basket[0].count = 99;
            Assert.That(second.Order.Lines[0].delivered, Is.EqualTo(1));
            Assert.That(second.Basket.Total, Is.EqualTo(1)); Assert.That(game.Economy.PendingCash, Is.Zero);
        }

        [Test]
        public void RestoringPaidCustomerDoesNotRequeueOrPayAgain()
        {
            var customer = Customer(50, 0, 90, new OrderLine("carrot", 1, 10));
            var source = new Inventory(2); source.TryAdd("carrot", 1);
            lane.Serve(source);
            var saved = customer.Snapshot(); customer.Restore(saved);
            Assert.That(customer.Current, Is.EqualTo(CustomerAgent.State.Leaving)); Assert.That(lane.Queue, Is.Empty);
            Assert.That(customer.Basket.Count("carrot"), Is.EqualTo(1));
            Assert.That(lane.Cash, Is.EqualTo(10)); Assert.That(game.Economy.Transactions, Is.EqualTo(1));
            Assert.That(lane.Serve(source), Is.False);
        }
    }
}



