using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class OrderBubbleTerminalTests
    {
        GameSession game;
        GameSession previous;
        CheckoutStation checkout;

        T Component<T>(string name) where T : Component
        {
            var root = new GameObject(name);
            root.SetActive(false);
            root.transform.SetParent(game.transform);
            return root.AddComponent<T>();
        }

        [SetUp]
        public void SetUp()
        {
            previous = GameSession.Instance;
            var root = new GameObject("TerminalFeedbackTest");
            root.SetActive(false);
            game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            checkout = Component<CheckoutStation>("Checkout");
            checkout.Id = "checkout_farm";
            checkout.ShopId = "farm";
            game.Checkouts.Add(checkout);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(game.gameObject);
            GameSession.Instance = previous;
        }

        CustomerAgent Customer()
        {
            var customer = Component<CustomerAgent>("Customer");
            customer.Restore(new CustomerSave
            {
                receipt = 100,
                shop = "farm",
                lane = checkout.Id,
                phase = (int)CustomerAgent.State.Queue,
                remaining = 90,
                order = new List<OrderLine> { new("carrot", 2, 10) }
            });
            return customer;
        }

        DinerAgent Diner(float patience)
        {
            checkout.ShopId = "restaurant";
            var table = Component<TableStation>("Table");
            table.Id = "table_terminal";
            table.Inventory = new Inventory(3);
            var diner = Component<DinerAgent>("Diner");
            diner.Begin(table, new DinerSave
            {
                receipt = 200,
                table = table.Id,
                item = "meal",
                price = 600,
                phase = 1,
                remaining = patience
            });
            return diner;
        }

        [Test]
        public void CheckoutCompletionShowsCheckWithoutRepeatingPaymentOrExtendingFeedback()
        {
            var customer = Customer();
            var goods = new Inventory(2);
            goods.TryAdd("carrot", 2);
            Assert.That(customer.Order.Deliver(goods, customer.Basket, game.Economy, Time.time), Is.EqualTo(2));
            customer.Leave();
            var feedback = customer.GetComponent<OrderBubbleTerminalFeedback>();
            Assert.That(feedback.Outcome, Is.EqualTo(OrderBubbleTerminalOutcome.Completed));
            Assert.That(feedback.IsPlaying, Is.True);
            Assert.That(checkout.Queue, Is.Empty);
            feedback.Advance(.3f);
            customer.Leave();
            feedback.Advance(.31f);
            Assert.That(feedback.IsPlaying, Is.False);
            Assert.That(customer.Current, Is.EqualTo(CustomerAgent.State.Leaving));
            Assert.That(game.Economy.Transactions, Is.EqualTo(1));
            Assert.That(game.Economy.Revenue, Is.EqualTo(20));
            Assert.That(customer.Basket.Count("carrot"), Is.EqualTo(2));
        }

        [Test]
        public void CheckoutTimeoutShowsDisappointmentAndKeepsSettledGoodsUnchanged()
        {
            var customer = Customer();
            var goods = new Inventory(1);
            goods.TryAdd("carrot", 1);
            customer.Order.Deliver(goods, customer.Basket, game.Economy, Time.time);
            customer.Order.Expire(customer.Basket, game.Economy, Time.time + 100);
            int retained = customer.Basket.Total;
            int lost = game.Economy.LostItems;
            customer.Leave();
            var feedback = customer.GetComponent<OrderBubbleTerminalFeedback>();
            Assert.That(feedback.Outcome, Is.EqualTo(OrderBubbleTerminalOutcome.Disappointed));
            feedback.Advance(.59f);
            Assert.That(feedback.IsPlaying, Is.True);
            feedback.Advance(.02f);
            Assert.That(feedback.IsPlaying, Is.False);
            Assert.That(customer.Basket.Total, Is.EqualTo(retained));
            Assert.That(game.Economy.LostItems, Is.EqualTo(lost));
            Assert.That(game.Economy.Transactions, Is.Zero);
        }

        [Test]
        public void DinerCompletionShowsCheckAndSettlesOnlyOnce()
        {
            var diner = Diner(90);
            var goods = new Inventory(1);
            goods.TryAdd("meal", 1);
            Assert.That(diner.ReceiveMeal(goods), Is.True);
            diner.Tick(7);
            var feedback = diner.GetComponent<OrderBubbleTerminalFeedback>();
            Assert.That(feedback.Outcome, Is.EqualTo(OrderBubbleTerminalOutcome.Completed));
            Assert.That(diner.Phase, Is.EqualTo(3));
            Assert.That(diner.Table.Occupant, Is.Null);
            Assert.That(diner.Table.Cleaning, Is.EqualTo(3));
            diner.Tick(7);
            feedback.Advance(.61f);
            Assert.That(feedback.IsPlaying, Is.False);
            Assert.That(game.Economy.Transactions, Is.EqualTo(1));
            Assert.That(checkout.Cash, Is.EqualTo(600));
            Assert.That(diner.Basket.Total, Is.Zero);
        }

        [Test]
        public void DinerTimeoutShowsDisappointmentWithoutPayment()
        {
            var diner = Diner(0);
            diner.Tick(.1f);
            var feedback = diner.GetComponent<OrderBubbleTerminalFeedback>();
            Assert.That(feedback.Outcome, Is.EqualTo(OrderBubbleTerminalOutcome.Disappointed));
            Assert.That(diner.Phase, Is.EqualTo(3));
            feedback.Advance(.61f);
            Assert.That(feedback.IsPlaying, Is.False);
            Assert.That(game.Economy.Transactions, Is.Zero);
            Assert.That(checkout.Cash, Is.Zero);
            Assert.That(diner.Table.Occupant, Is.Null);
        }

        [Test]
        public void DisableCallbackReturnsTerminalViewToPoolWithNoOutcomeOnReuse()
        {
            var actor = new GameObject("ActiveFeedbackActor");
            try
            {
                var feedback = actor.AddComponent<OrderBubbleTerminalFeedback>();
                var source = OrderBubbleLayer.Acquire(actor.transform);
                source.SetSingle("carrot", 1, 2, .2f);
                var shown = source;
                Assert.That(feedback.Show(ref source, false), Is.True);
                Assert.That(source, Is.Null);
                Assert.That(shown.HasContent, Is.True);
                Assert.That(shown.LineCount, Is.Zero);
                actor.SetActive(false);
                // EditMode không chạy vòng đời MonoBehaviour; Player QA kiểm tra SetActive thật.
                typeof(OrderBubbleTerminalFeedback).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(feedback, null);
                Assert.That(feedback.IsPlaying, Is.False);
                Assert.That(feedback.Outcome, Is.EqualTo(OrderBubbleTerminalOutcome.None));
                var reused = OrderBubbleLayer.Acquire(game.transform);
                Assert.That(reused, Is.SameAs(shown));
                Assert.That(reused.HasContent, Is.False);
                Assert.That(reused.TerminalOutcome, Is.EqualTo(OrderBubbleTerminalOutcome.None));
                Assert.That(reused.PatienceFraction, Is.EqualTo(1));
                reused.SetSingle("meal", 0, 1, .8f);
                Assert.That(reused.LineCount, Is.EqualTo(1));
                OrderBubbleLayer.Release(ref reused);
            }
            finally
            {
                Object.DestroyImmediate(actor);
            }
        }

        [Test]
        public void NewCheckoutVisitClearsPendingTerminalFeedback()
        {
            var customer = Customer();
            customer.Order.Expire(customer.Basket, game.Economy, Time.time + 100);
            customer.Leave();
            var feedback = customer.GetComponent<OrderBubbleTerminalFeedback>();
            Assert.That(feedback.IsPlaying, Is.True);
            customer.Restore(new CustomerSave
            {
                receipt = 101,
                shop = "farm",
                lane = checkout.Id,
                phase = (int)CustomerAgent.State.Queue,
                remaining = 90,
                order = new List<OrderLine> { new("carrot", 1, 10) }
            });
            Assert.That(feedback.IsPlaying, Is.False);
            Assert.That(feedback.Outcome, Is.EqualTo(OrderBubbleTerminalOutcome.None));
            Assert.That(customer.Current, Is.EqualTo(CustomerAgent.State.Queue));
        }

        [Test]
        public void NewDinerVisitClearsPendingTerminalFeedback()
        {
            var diner = Diner(0);
            diner.Tick(.1f);
            var feedback = diner.GetComponent<OrderBubbleTerminalFeedback>();
            Assert.That(feedback.IsPlaying, Is.True);
            diner.Begin(diner.Table, new DinerSave
            {
                receipt = 201,
                table = diner.Table.Id,
                item = "meal",
                price = 600,
                phase = 1,
                remaining = 90
            });
            Assert.That(feedback.IsPlaying, Is.False);
            Assert.That(feedback.Outcome, Is.EqualTo(OrderBubbleTerminalOutcome.None));
            Assert.That(diner.Phase, Is.EqualTo(1));
        }
    }
}
