using System.Reflection;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class CityInventoryProjectionTests
    {
        static readonly MethodInfo ProjectMethod = typeof(Inventory).GetMethod("Project",
            BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo BindMethod = typeof(Inventory).GetMethod("Bind",
            BindingFlags.Instance | BindingFlags.NonPublic);

        static Inventory InventoryFor(string owner, int capacity = 20)
        {
            var inventory = new Inventory(capacity);
            BindMethod.Invoke(inventory, new object[] { null, owner });
            return inventory;
        }

        static void Project(Inventory inventory, TransactionState state) =>
            ProjectMethod.Invoke(inventory, new object[] { state });

        static TransactionState Seed()
        {
            var state = new TransactionState();
            foreach (string id in new[] { "source", "destination", "relay", "unrelated" })
                state.owners.Add(new OwnerState { id = id, capacity = 20 });
            state.stacks.Add(new ItemStackState { owner = "source", item = "carrot", quantity = 8 });
            state.stacks.Add(new ItemStackState { owner = "source", item = "wheat", quantity = 4 });
            return state;
        }

        static ReservationState Reservation(string item = "carrot", int quantity = 3) => new()
        {
            id = "reserved",
            source = "source",
            destination = "destination",
            relay = "relay",
            item = item,
            quantity = quantity,
            status = ReservationStatus.Active
        };

        [Test]
        public void CommittedTakeLeavesOtherInventoryRevisionUnchanged()
        {
            var core = new TransactionCore(TransactionCoreTests.Seed());
            var storage = InventoryFor("storage");
            var mill = InventoryFor("mill-input");
            Project(storage, core.Snapshot());
            Project(mill, core.Snapshot());
            int storageRevision = storage.Revision;
            int millRevision = mill.Revision;
            core.Execute(new TransactionCommand
            {
                kind = TransactionKind.Take,
                key = "projection-take",
                effectId = "effect:projection-take",
                actor = "player",
                expectedRevision = core.Revision,
                source = "storage",
                destination = "player",
                item = "carrot",
                quantity = 2
            });
            Project(storage, core.Snapshot());
            Project(mill, core.Snapshot());
            Assert.That(storage.Revision, Is.EqualTo(storageRevision + 1));
            Assert.That(storage.Count("carrot"), Is.EqualTo(8));
            Assert.That(mill.Revision, Is.EqualTo(millRevision));
            Assert.That(mill.Count("wheat"), Is.EqualTo(8));
        }

        [Test]
        public void UnrelatedOwnerChangesAndRepeatedProjectionKeepRevision()
        {
            var state = Seed();
            var source = InventoryFor("source");
            var unrelated = InventoryFor("unrelated");
            Project(source, state);
            Project(unrelated, state);
            int sourceRevision = source.Revision;
            int unrelatedRevision = unrelated.Revision;
            state.revision++;
            state.stacks.Add(new ItemStackState { owner = "unrelated", item = "carrot", quantity = 2 });
            Project(source, state);
            Project(unrelated, state);
            Assert.That(source.Revision, Is.EqualTo(sourceRevision));
            Assert.That(unrelated.Revision, Is.EqualTo(unrelatedRevision + 1));
            Assert.That(source.Total, Is.EqualTo(12));
            Assert.That(unrelated.Count("carrot"), Is.EqualTo(2));
            Project(unrelated, state);
            Assert.That(unrelated.Revision, Is.EqualTo(unrelatedRevision + 1));
            state.revision = long.MaxValue;
            state.money++;
            Project(source, state);
            Assert.That(source.Revision, Is.EqualTo(sourceRevision));
        }

        [Test]
        public void SameTotalWithDifferentSkuCountsChangesRevisionAtIdenticalGlobalRevision()
        {
            var state = Seed();
            var inventory = InventoryFor("source");
            Project(inventory, state);
            int revision = inventory.Revision;
            var loaded = Seed();
            loaded.stacks[0].quantity = 4;
            loaded.stacks[1].quantity = 8;
            Project(inventory, loaded);
            Assert.That(inventory.Revision, Is.EqualTo(revision + 1));
            Assert.That(inventory.Total, Is.EqualTo(12));
            Assert.That(inventory.Count("carrot"), Is.EqualTo(4));
            Assert.That(inventory.Count("wheat"), Is.EqualTo(8));
            loaded.stacks.RemoveAt(0);
            Project(inventory, loaded);
            Assert.That(inventory.Revision, Is.EqualTo(revision + 2));
            Assert.That(inventory.Count("carrot"), Is.Zero);
        }

        [Test]
        public void SplitStacksAndEquivalentReservationRecordsDoNotChangeInventory()
        {
            var state = Seed();
            state.reservations.Add(Reservation());
            var inventory = InventoryFor("source");
            Project(inventory, state);
            int revision = inventory.Revision;
            state.stacks[0].quantity = 5;
            state.stacks.Add(new ItemStackState { owner = "source", item = "carrot", quantity = 3 });
            state.stacks.Reverse();
            state.reservations[0].quantity = 1;
            state.reservations.Add(Reservation(quantity: 2));
            state.revision++;
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(revision));
            Assert.That(inventory.Count("carrot"), Is.EqualTo(8));
            Assert.That(inventory.Available("carrot"), Is.EqualTo(5));
        }

        [TestCase(ReservationStatus.Used)]
        [TestCase(ReservationStatus.Released)]
        [TestCase(ReservationStatus.Expired)]
        public void ReservationLifecycleUpdatesSourceDestinationAndRelay(ReservationStatus inactive)
        {
            var state = Seed();
            var source = InventoryFor("source");
            var destination = InventoryFor("destination");
            var relay = InventoryFor("relay");
            foreach (var inventory in new[] { source, destination, relay }) Project(inventory, state);
            int sourceRevision = source.Revision;
            state.reservations.Add(Reservation());
            foreach (var inventory in new[] { source, destination, relay }) Project(inventory, state);
            Assert.That(source.Revision, Is.EqualTo(sourceRevision + 1));
            Assert.That(source.ReservedTotal, Is.EqualTo(3));
            Assert.That(source.Available("carrot"), Is.EqualTo(5));
            foreach (var inventory in new[] { destination, relay })
            {
                Assert.That(inventory.Revision, Is.EqualTo(1));
                Assert.That(inventory.ReservedSpace("carrot"), Is.EqualTo(3));
                Assert.That(inventory.IncomingTotal, Is.EqualTo(3));
                Assert.That(inventory.Free, Is.EqualTo(17));
            }
            state.reservations[0].status = inactive;
            foreach (var inventory in new[] { source, destination, relay })
            {
                int revision = inventory.Revision;
                Project(inventory, state);
                Assert.That(inventory.Revision, Is.EqualTo(revision + 1));
                Assert.That(inventory.ReservedTotal, Is.Zero);
                Assert.That(inventory.IncomingTotal, Is.Zero);
                Project(inventory, state);
                Assert.That(inventory.Revision, Is.EqualTo(revision + 1));
            }
        }

        [TestCase("source")]
        [TestCase("destination")]
        [TestCase("relay")]
        public void ReservationSkuChangeIsDetectedEvenWhenTotalsMatch(string owner)
        {
            var state = Seed();
            state.reservations.Add(Reservation());
            var inventory = InventoryFor(owner);
            Project(inventory, state);
            int revision = inventory.Revision;
            state.reservations[0].item = "wheat";
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(revision + 1));
            Assert.That(inventory.Reserved("carrot"), Is.Zero);
            Assert.That(inventory.ReservedSpace("carrot"), Is.Zero);
            Assert.That(inventory.Reserved("wheat"), Is.EqualTo(owner == "source" ? 3 : 0));
            Assert.That(inventory.ReservedSpace("wheat"), Is.EqualTo(owner == "source" ? 0 : 3));
        }

        [Test]
        public void RelayChangingToSourceReleasesIncomingSpaceAndReservesExistingGoods()
        {
            var state = Seed();
            state.reservations.Add(Reservation());
            var inventory = InventoryFor("relay");
            Project(inventory, state);
            int revision = inventory.Revision;
            state.reservations[0].source = "relay";
            state.reservations[0].relay = null;
            state.stacks.Add(new ItemStackState { owner = "relay", item = "carrot", quantity = 3 });
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(revision + 1));
            Assert.That(inventory.Total, Is.EqualTo(3));
            Assert.That(inventory.ReservedTotal, Is.EqualTo(3));
            Assert.That(inventory.Available("carrot"), Is.Zero);
            Assert.That(inventory.IncomingTotal, Is.Zero);
            Assert.That(inventory.Free, Is.EqualTo(17));
        }

        [Test]
        public void CapacitySingleItemAndExplicitZeroLimitRemainObservable()
        {
            var state = Seed();
            var inventory = InventoryFor("destination");
            var owner = state.owners[1];
            Project(inventory, state);
            int revision = inventory.Revision;
            owner.capacity = 10;
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(++revision));
            Assert.That(inventory.Free, Is.EqualTo(10));
            owner.singleItem = true;
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(++revision));
            Assert.That(inventory.SingleItem, Is.True);
            state.reservations.Add(Reservation());
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(++revision));
            Assert.That(inventory.SingleItem, Is.True);
            Assert.That(inventory.FreeFor("wheat"), Is.Zero);
            Assert.That(inventory.FreeFor("carrot"), Is.EqualTo(7));
            owner.limits.Add(new ItemAmount("carrot", 5));
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(++revision));
            Assert.That(inventory.FreeFor("carrot"), Is.EqualTo(2));
            owner.limits[0].count = 0;
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(++revision));
            Assert.That(inventory.FreeFor("carrot"), Is.Zero);
            owner.limits.Clear();
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(++revision));
            Assert.That(inventory.FreeFor("carrot"), Is.EqualTo(7));
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void ProjectionPreservesPrimitiveZeroEntriesAndLargeExactQuantities()
        {
            var state = Seed();
            var inventory = InventoryFor("destination");
            Assert.That(inventory.TryAdd("carrot", 1), Is.True);
            Assert.That(inventory.TryReserve("carrot", 1), Is.True);
            inventory.Release("carrot", 1);
            Assert.That(inventory.TryRemove("carrot", 1), Is.True);
            Assert.That(inventory.TryReserveSpace("wheat", 1), Is.True);
            inventory.ReleaseSpace("wheat", 1);
            int revision = inventory.Revision;
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(revision));
            state.owners[1].capacity = int.MaxValue;
            state.stacks.Add(new ItemStackState { owner = "destination", item = "carrot", quantity = 16777217 });
            Project(inventory, state);
            Assert.That(inventory.Count("carrot"), Is.EqualTo(16777217));
            state.stacks[2].quantity++;
            Project(inventory, state);
            Assert.That(inventory.Revision, Is.EqualTo(revision + 2));
            Assert.That(inventory.Count("carrot"), Is.EqualTo(16777218));
        }
    }
}
