using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        TransactionState ReadRuntimeView() => (TransactionState)typeof(RuntimeTransactions)
            .GetProperty("View", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game.Transactions);

        OrderRuntimeState ReadRuntimeOrder(long receipt) => (OrderRuntimeState)typeof(RuntimeTransactions)
            .GetMethod("Order", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(game.Transactions, new object[] { receipt });

        CustomerAgent AddOrderCustomer(long receipt)
        {
            var customer = Add<CustomerAgent>("OrderCustomer" + receipt);
            customer.Restore(new CustomerSave
            {
                receipt = receipt,
                shop = "farm",
                lane = counter.Id,
                phase = (int)CustomerAgent.State.Queue,
                remaining = 90,
                order = new() { new("carrot", 1, 10) }
            });
            return customer;
        }

        [Test]
        public void RuntimeOrderIndexFindsExactReceiptAndReusesCurrentProjection()
        {
            AddOrderCustomer(77);
            var order = ReadRuntimeOrder(77);
            Assert.That(order.id, Is.EqualTo("order:77"));
            Assert.That(ReadRuntimeOrder(77), Is.SameAs(order));
            Assert.That(ReadRuntimeOrder(770), Is.Null);
        }

        [Test]
        public void RuntimeOrderIndexRefreshesAfterNewCustomer()
        {
            AddOrderCustomer(77);
            var previous = ReadRuntimeOrder(77);
            AddOrderCustomer(78);
            Assert.That(ReadRuntimeOrder(78).id, Is.EqualTo("order:78"));
            Assert.That(ReadRuntimeOrder(77), Is.Not.SameAs(previous));
        }

        [Test]
        public void RuntimeOrderIndexPublishesDeliveredLinesWithoutMutatingOldView()
        {
            AddOrderCustomer(77);
            Inventory.Transfer(storage.Inventory, game.Player.Carry, "carrot", 1);
            var previous = ReadRuntimeOrder(77);
            Assert.That(game.Transactions.Deliver(77, game.Player.Carry), Is.EqualTo(1));
            Assert.That(ReadRuntimeOrder(77).lines[0].delivered, Is.EqualTo(1));
            Assert.That(previous.lines[0].delivered, Is.Zero);
        }
        [Test]
        public void RejectedCommandKeepsProjectionAndInventory()
        {
            var previousView = ReadRuntimeView();
            int previousInventory = storage.Inventory.Revision;
            var command = game.Transactions.Command(TransactionKind.Take, "player");
            command.source = "storage";
            command.destination = "player";
            command.item = "carrot";
            command.quantity = 1000;
            Assert.That(game.Transactions.TryExecute(command, out _), Is.False);
            Assert.That(ReadRuntimeView(), Is.SameAs(previousView));
            Assert.That(storage.Inventory.Revision, Is.EqualTo(previousInventory));
            Assert.That(storage.Inventory.Count("carrot"), Is.EqualTo(6));
            Assert.That(game.Player.Carry.Total, Is.Zero);
        }

        [Test]
        public void ReplayedReceiptDoesNotRebuildProjectionOrJournal()
        {
            var command = game.Transactions.Command(TransactionKind.Take, "player");
            command.source = "storage";
            command.destination = "player";
            command.item = "carrot";
            command.quantity = 1;
            game.Transactions.Execute(command);
            var previousView = ReadRuntimeView();
            long length = new FileInfo(game.SavePath + ".journal").Length;
            game.Transactions.Execute(command);
            Assert.That(ReadRuntimeView(), Is.SameAs(previousView));
            Assert.That(new FileInfo(game.SavePath + ".journal").Length, Is.EqualTo(length));
            Assert.That(game.Player.Carry.Count("carrot"), Is.EqualTo(1));
        }

        [Test]
        public void CommittedCommandPublishesFreshProjection()
        {
            var previousView = ReadRuntimeView();
            Inventory.Transfer(storage.Inventory, game.Player.Carry, "carrot", 1);
            Assert.That(ReadRuntimeView(), Is.Not.SameAs(previousView));
            Assert.That(ReadRuntimeView().revision, Is.EqualTo(previousView.revision + 1));
            Assert.That(storage.Inventory.Count("carrot"), Is.EqualTo(5));
            Assert.That(game.Player.Carry.Count("carrot"), Is.EqualTo(1));
        }
    }
}
