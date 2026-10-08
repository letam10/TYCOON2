using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed partial class TransactionCoreTests
    {
        string directory, path;
        double now;
        [SetUp] public void SetUp()
        {
            now = 100;
            directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "work", "stage02", Guid.NewGuid().ToString("N"));
            path = Path.Combine(directory, "transactions.json");
        }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        TransactionCore Core(TransactionState seed = null, FileTransactionStore store = null) => new(seed ?? Seed(), store, () => now);
        internal static TransactionState Seed()
        {
            var seed = new TransactionState { schemaVersion = 2, money = 600 }; seed.unlocked.Add("farm_level2");
            foreach (var row in new[] {
                ("player", OwnerKind.Player, "player", 6), ("worker", OwnerKind.Worker, "worker", 4),
                ("storage", OwnerKind.Storage, "simulation", 50), ("counter", OwnerKind.Counter, "simulation", 12),
                ("customer", OwnerKind.Customer, "customer", 3), ("conveyor", OwnerKind.Conveyor, "simulation", 4),
                ("mill-input", OwnerKind.Machine, "simulation", 20), ("mill-output", OwnerKind.Machine, "simulation", 3) })
                seed.owners.Add(new OwnerState { id = row.Item1, kind = row.Item2, actor = row.Item3, location = "location:" + row.Item1,
                    capacity = row.Item4, singleItem = row.Item2 == OwnerKind.Worker, writers = new() { "player", "worker" } });
            seed.stacks.Add(new ItemStackState { id = "stock", owner = "storage", location = "location:storage", item = "carrot", quantity = 10 });
            seed.stacks.Add(new ItemStackState { id = "wheat", owner = "mill-input", location = "location:mill-input", item = "wheat", quantity = 8 });
            seed.stations.Add(new StationRuntimeState { id = "mill", definitionId = "mill", input = "mill-input", output = "mill-output" });
            return seed;
        }
        static TransactionCommand Command(TransactionCore core, TransactionKind kind, string key, string target = null) => new() {
            kind = kind, key = key, effectId = "effect:" + key, actor = "player", expectedRevision = core.Revision, target = target,
            source = "storage", destination = "player", item = "carrot", quantity = 1
        };
        static int Count(TransactionCore core, string owner, string item = "carrot") => core.Snapshot().stacks.Where(x => x.owner == owner && x.item == item).Sum(x => x.quantity);
        static string Json(TransactionCore core) => JsonUtility.ToJson(core.Snapshot());
        static void Rejected(TransactionCore core, TransactionCommand c, string code)
        { string before = Json(core); Assert.That(Assert.Throws<TransactionRejectedException>(() => core.Execute(c)).Code, Is.EqualTo(code)); Assert.That(Json(core), Is.EqualTo(before)); }
        static void Journal(TransactionCore core, int commands)
        {
            var state = core.Snapshot(); TransactionCore.Validate(state);
            Assert.That(state.revision, Is.EqualTo(commands)); Assert.That(state.receipts.Count, Is.EqualTo(commands));
            Assert.That(state.outbox.Select(x => x.id).Distinct().Count(), Is.EqualTo(commands));
            Assert.That(state.receipts.Select(x => x.effectId).Distinct().Count(), Is.EqualTo(commands));
        }
        static void Order(TransactionCore core, string key = "order")
        {
            var c = Command(core, TransactionKind.CreateOrder, key, "order:1"); c.actor = "simulation";
            c.source = "counter"; c.destination = "customer"; c.expiresAt = 190;
            c.lines.Add(new OrderLine("carrot", 2, 10)); core.Execute(c);
        }
        static TransactionCommand Delivery(TransactionCore core, string key = "delivery", int quantity = 2)
        { var c = Command(core, TransactionKind.DeliverOrder, key, "order:1"); c.quantity = quantity; return c; }
        static void PaidOrder(TransactionCore core)
        { Order(core); core.Execute(Delivery(core)); core.Execute(Command(core, TransactionKind.CompleteOrder, "complete", "order:1")); core.Execute(Command(core, TransactionKind.CreatePayment, "payment", "order:1")); }
        [Test] public void DefinitionsAndReturnedSnapshotsCannotMutateAuthority()
        {
            var core = Core(); var state = core.Snapshot(); state.stacks[0].quantity = 999; state.money = -1;
            Assert.That(Count(core, "storage"), Is.EqualTo(10)); Assert.That(core.Snapshot().money, Is.EqualTo(600));
            var recipes = Definitions.Recipes; recipes[0] = null; var ingredients = Definitions.Recipe("mill").inputs; ingredients[0].count = 99;
            Assert.That(Definitions.Recipe("mill").inputs[0].count, Is.EqualTo(4)); Assert.That(Definitions.Recipes[0], Is.Not.Null);
        }
        [Test] public void TransactionSchemaOneMigratesTypedStorageLocationAndMachinePhase()
        {
            var seed=Seed();seed.schemaVersion=1;var core=Core(seed);var state=core.Snapshot();
            Assert.That(state.schemaVersion,Is.EqualTo(2));
            Assert.That(state.stacks.Single(x=>x.owner=="storage").location,Is.EqualTo("location:storage/carrot"));
            Assert.That(state.stations.Single().machinePhase,Is.EqualTo(MachinePhase.Ready));
            TransactionCore.Validate(state);
        }
        [Test] public void RetryReturnsDurableReceiptAndConflictingPayloadCannotMutate()
        {
            var core = Core(); var c = Command(core, TransactionKind.Take, "pickup"); var result = core.Execute(c);
            Assert.That(core.Execute(c).id, Is.EqualTo(result.id)); Assert.That(Count(core, "player"), Is.EqualTo(1));
            c.quantity = 2; Rejected(core, c, "key-conflict"); Journal(core, 1);
        }
        [Test] public void DifferentCommandSameEffectReturnsOriginalReceipt()
        {
            var core = Core(); var c = Command(core, TransactionKind.Transfer, "a"); var result = core.Execute(c);
            c.key = "b"; c.expectedRevision = core.Revision;
            Assert.That(core.Execute(c).id, Is.EqualTo(result.id)); c.actor = "worker"; Rejected(core, c, "effect-conflict"); Journal(core, 1);
        }
        [Test] public void ConcurrentActorsCompeteForVersionWithoutDoubleTransfer()
        {
            var core = Core(); var a = Command(core, TransactionKind.Take, "a"); a.quantity = 4;
            var b = Command(core, TransactionKind.Take, "b"); b.actor = "worker"; b.destination = "worker"; b.quantity = 4;
            int succeeded = 0, stale = 0;
            Parallel.ForEach(new[] { a, b }, c => { try { core.Execute(c); System.Threading.Interlocked.Increment(ref succeeded); }
                catch (TransactionRejectedException e) { if (e.Code != "stale") throw; System.Threading.Interlocked.Increment(ref stale); } });
            Assert.That(succeeded, Is.EqualTo(1)); Assert.That(stale, Is.EqualTo(1));
            Assert.That(core.Snapshot().stacks.Where(x => x.item == "carrot").Sum(x => x.quantity), Is.EqualTo(10)); Journal(core, 1);
        }
        [Test] public void AuthorityCapacityAndStaleFailuresLeaveNoPartialMutation()
        {
            var core = Core(); var c = Command(core, TransactionKind.Transfer, "invalid"); c.actor = "intruder"; Rejected(core, c, "authority");
            c.actor = "player"; c.quantity = 7; Rejected(core, c, "capacity"); c.quantity = 1; c.expectedRevision = 4; Rejected(core, c, "stale"); Journal(core, 0);
        }
        [Test] public void SplitMergePreservesReservationsAndTransferConsumesExactlyOnce()
        {
            var core = Core(); var reserve = Command(core, TransactionKind.Reserve, "reserve", "reservation:1"); reserve.quantity = 6; reserve.expiresAt = 120; core.Execute(reserve);
            var split = Command(core, TransactionKind.Split, "split", "stock"); split.quantity = 7; split.secondary = "split-stock"; core.Execute(split);
            Assert.That(core.Snapshot().reservations[0].allocations.Count, Is.EqualTo(2));
            var merge = Command(core, TransactionKind.Merge, "merge", "split-stock"); merge.secondary = "stock"; core.Execute(merge);
            var transfer = Command(core, TransactionKind.Transfer, "move"); transfer.quantity = 6; transfer.reservation = "reservation:1"; core.Execute(transfer);
            Assert.That(core.Execute(transfer).amount, Is.EqualTo(6)); Assert.That(Count(core, "player"), Is.EqualTo(6)); Assert.That(Count(core, "storage"), Is.EqualTo(4)); Journal(core, 4);
        }
        [Test] public void ReleaseDifferentCommandsFreesReservationOnlyOnce()
        {
            var core = Core(); var c = Command(core, TransactionKind.Reserve, "reserve", "r"); c.quantity = 6; c.expiresAt = 130; core.Execute(c);
            Assert.That(core.Execute(Command(core, TransactionKind.Release, "release1", "r")).amount, Is.EqualTo(6));
            Assert.That(core.Execute(Command(core, TransactionKind.Release, "release2", "r")).amount, Is.Zero);
            var take = Command(core, TransactionKind.Take, "take"); take.quantity = 6; core.Execute(take); Assert.That(Count(core, "player"), Is.EqualTo(6)); Journal(core, 4);
        }
        [Test] public void ReservationExpirySurvivesLoadAndCannotStealAnotherHolder()
        {
            var store = new FileTransactionStore(path); var core = Core(store: store); var c = Command(core, TransactionKind.Reserve, "reserve", "r"); c.expiresAt = 110; core.Execute(c);
            var release = Command(core, TransactionKind.Release, "steal", "r"); release.actor = "worker"; Rejected(core, release, "authority");
            now = 120; core = Core(store: store); var take = Command(core, TransactionKind.Transfer, "take"); take.reservation = "r"; Rejected(core, take, "reservation");
            Assert.That(core.Execute(Command(core, TransactionKind.Release, "release", "r")).amount, Is.Zero);
            Assert.That(core.Snapshot().reservations[0].status, Is.EqualTo(ReservationStatus.Expired)); Journal(core, 2);
        }
        [Test] public void PartialDeliveryHasNoPaymentAndFailureRetainsCustomerOwnership()
        {
            var core = Core(); Order(core); var delivery = Delivery(core, quantity: 1); core.Execute(delivery); core.Execute(delivery);
            Assert.That(core.Snapshot().payments, Is.Empty); Assert.That(Count(core, "customer"), Is.EqualTo(1));
            Rejected(core, Command(core, TransactionKind.CompleteOrder, "early", "order:1"), "order-incomplete");
            now = 190; core.Execute(Command(core, TransactionKind.FailOrder, "fail", "order:1"));
            Rejected(core, Delivery(core, "late", 1), "order-closed"); Rejected(core, Command(core, TransactionKind.CreatePayment, "pay", "order:1"), "order-incomplete");
            Assert.That(Count(core, "customer"), Is.EqualTo(1)); Assert.That(core.Snapshot().money, Is.EqualTo(600)); Journal(core, 3);
        }
        [Test] public void CompleteAndFailRaceHasOneTerminalOutcome()
        {
            var core = Core(); Order(core); core.Execute(Delivery(core));
            var complete = Command(core, TransactionKind.CompleteOrder, "complete", "order:1"); var fail = Command(core, TransactionKind.FailOrder, "fail", "order:1");
            Parallel.ForEach(new[] { complete, fail }, c => { try { core.Execute(c); } catch (TransactionRejectedException e) { Assert.That(e.Code, Is.EqualTo("stale")); } });
            var status = core.Snapshot().orders[0].status; Assert.That(status, Is.EqualTo(OrderStatus.Complete).Or.EqualTo(OrderStatus.Failed));
            if (status == OrderStatus.Complete) { core.Execute(Command(core, TransactionKind.CreatePayment, "pay", "order:1")); Rejected(core, Command(core, TransactionKind.FailOrder, "fail-again", "order:1"), "order-closed"); }
            else Rejected(core, Command(core, TransactionKind.CreatePayment, "pay", "order:1"), "order-incomplete");
            Assert.That(core.Snapshot().payments.Count, Is.EqualTo(status == OrderStatus.Complete ? 1 : 0)); TransactionCore.Validate(core.Snapshot());
        }
        [Test] public void PaymentAndCollectionHaveIndependentBusinessUniqueness()
        {
            var core = Core(); PaidOrder(core);
            Assert.That(core.Execute(Command(core, TransactionKind.CreatePayment, "again", "order:1")).amount, Is.Zero);
            var collect = Command(core, TransactionKind.CollectPayment, "collect", "payment:order:1"); collect.actor = "worker"; Rejected(core, collect, "authority");
            collect.actor = "player"; core.Execute(collect); core.Execute(collect);
            Assert.That(core.Execute(Command(core, TransactionKind.CollectPayment, "other-collect", collect.target)).amount, Is.Zero);
            Assert.That(core.Snapshot().money, Is.EqualTo(620)); Assert.That(core.Snapshot().revenue, Is.EqualTo(20)); Journal(core, 7);
        }
    }
}
