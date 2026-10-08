using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        [Test]
        public void EmptyStockRemainsUnavailableWhenMachineNeedsIngredients()
        {
            storage.AreaId = "farm";
            var state = game.Transactions.Snapshot();
            state.stacks.RemoveAll(x => x.owner == machine.Input.OwnerId);
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            Assert.That(storage.SharedReserveThreshold("wheat"), Is.GreaterThan(0));
            Assert.That(storage.Inventory.Available("wheat"), Is.Zero);
            Assert.That(storage.AvailableAboveReserve("wheat"), Is.Zero);
        }

        [Test]
        public void StockAboveReserveExcludesMachineNeedsAndWorkerReservations()
        {
            storage.AreaId = "farm";
            var state = game.Transactions.Snapshot();
            var wheat = state.stacks.Find(x => x.owner == machine.Input.OwnerId && x.item == "wheat");
            wheat.owner = storage.Inventory.OwnerId;
            wheat.location = "farm/wheat";
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            int reserve = storage.SharedReserveThreshold("wheat");
            Assert.That(reserve, Is.GreaterThan(0));
            Assert.That(storage.AvailableAboveReserve("wheat"), Is.EqualTo(Mathf.Max(0, 8 - reserve)));
            string reservation = game.Transactions.Reserve(
                storage.Inventory, machine.Input, "wheat", 2, game.Player.GetEntityId());
            Assert.That(reservation, Is.Not.Null.And.Not.Empty);
            Assert.That(storage.Inventory.Available("wheat"), Is.EqualTo(6));
            Assert.That(storage.AvailableAboveReserve("wheat"), Is.EqualTo(Mathf.Max(0, 6 - reserve)));
        }
    }
}
