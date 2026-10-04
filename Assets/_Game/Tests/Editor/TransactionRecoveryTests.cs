using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed partial class TransactionCoreTests
    {
        [Test] public void TwoCoreInstancesCannotOverwriteCommittedRevision()
        {
            var store = new FileTransactionStore(path); var first = Core(store: store); var second = Core(store: new FileTransactionStore(path));
            var a = Command(first, TransactionKind.Take, "a"); var b = Command(second, TransactionKind.Take, "b");
            first.Execute(a); Assert.Throws<IOException>(() => second.Execute(b));
            Assert.That(Count(second, "player"), Is.EqualTo(1)); b.expectedRevision = second.Revision; second.Execute(b);
            Assert.That(Count(Core(store: store), "player"), Is.EqualTo(2)); Journal(second, 2);
            Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(1));
        }
        [Test] public void GenericTransferCannotReturnDeliveredCustomerGoods()
        {
            var core = Core(); Order(core); core.Execute(Delivery(core, quantity: 1));
            var takeBack = Command(core, TransactionKind.Take, "take-back"); takeBack.source = "customer";
            Rejected(core, takeBack, "escrow"); Assert.That(Count(core, "customer"), Is.EqualTo(1));
        }
        [Test] public void PartialPurchaseAndGrantRecoverWithoutDoubleSpending()
        {
            var store = new FileTransactionStore(path); var core = Core(store: store);
            var contribute = Command(core, TransactionKind.ContributePurchase, "pay1", "barn"); contribute.quantity = 200; core.Execute(contribute);
            core = Core(store: store); core.Execute(contribute); Assert.That(core.Snapshot().money, Is.EqualTo(400));
            Rejected(core, Command(core, TransactionKind.CompletePurchase, "early", "barn"), "purchase-incomplete");
            contribute = Command(core, TransactionKind.ContributePurchase, "pay2", "barn"); contribute.quantity = 500; core.Execute(contribute);
            core.Execute(Command(core, TransactionKind.CompletePurchase, "grant", "barn")); core = Core(store: store);
            Assert.That(core.Execute(Command(core, TransactionKind.CompletePurchase, "grant2", "barn")).amount, Is.Zero);
            Assert.That(core.Snapshot().money, Is.EqualTo(100)); Assert.That(core.Snapshot().purchases[0].contributed, Is.EqualTo(500));
            Assert.That(core.Snapshot().unlocked.Count(x => x == "barn"), Is.EqualTo(1)); Journal(core, 4);
        }
        [Test] public void MachineCrashRecoveryConsumesAndOutputsOncePerJob()
        {
            var store = new FileTransactionStore(path); var core = Core(store: store);
            var start = Command(core, TransactionKind.StartMachine, "start", "mill"); start.secondary = "job1"; core.Execute(start); core.Execute(start);
            Assert.That(Count(core, "mill-input", "wheat"), Is.EqualTo(4)); core = Core(store: store);
            var progress = Command(core, TransactionKind.AdvanceMachine, "advance", "mill"); progress.secondary = "job1"; progress.duration = 6; core.Execute(progress);
            var finish = Command(core, TransactionKind.CompleteMachine, "finish", "mill"); finish.secondary = "job1"; core.Execute(finish); core = Core(store: store); core.Execute(finish);
            var again = Command(core, TransactionKind.CompleteMachine, "again", "mill"); again.secondary = "job1"; core.Execute(again);
            Assert.That(Count(core, "mill-output", "flour"), Is.EqualTo(3)); Assert.That(core.Snapshot().stations[0].batches, Is.EqualTo(1));
            start.key = "new-key"; start.effectId = "new-start"; start.expectedRevision = core.Revision; Rejected(core, start, "job"); Journal(core, 4);
        }
        [Test] public void ConveyorSaveLoadConservesEveryStackOwnerAndLocation()
        {
            var store = new FileTransactionStore(path); var core = Core(store: store); var c = Command(core, TransactionKind.Transfer, "belt"); c.destination = "conveyor"; c.quantity = 4; core.Execute(c);
            core = Core(store: store); core.Execute(c); var state = core.Snapshot(); Assert.That(Count(core, "conveyor"), Is.EqualTo(4));
            Assert.That(state.stacks.Sum(x => x.quantity), Is.EqualTo(18)); Assert.That(state.stacks.All(x => state.owners.Any(o => o.id == x.owner && o.location == x.location)), Is.True); Journal(core, 1);
        }
        [TestCase(CommitBoundary.Prepared, false)]
        [TestCase(CommitBoundary.TemporaryFlushed, false)]
        [TestCase(CommitBoundary.Replaced, true)]
        [TestCase(CommitBoundary.Published, true)]
        public void CrashAtEachBoundaryRecoversWholeCommit(CommitBoundary boundary, bool committed)
        {
            var store = new FileTransactionStore(path); var core = Core(store: store); var c = Command(core, TransactionKind.Take, "crash");
            Action<CommitBoundary> fault = at => { if (at == boundary) throw new IOException("simulated interruption"); };
            store.Fault = fault; core.Fault = fault; Assert.Throws<IOException>(() => core.Execute(c)); store.Fault = null;
            core = Core(store: store); Journal(core, committed ? 1 : 0); Assert.That(Count(core, "player"), Is.EqualTo(committed ? 1 : 0));
            var receipt = core.Execute(c); Assert.That(core.Execute(c).id, Is.EqualTo(receipt.id)); Journal(core, 1);
            Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(1));
        }
        [Test] public void OutboxRedeliveryCommitsConsumerEffectAndDedupTogether()
        {
            var store = new FileTransactionStore(path); var core = Core(store: store); core.Execute(Command(core, TransactionKind.Take, "take"));
            var message = core.PendingEvents("hud").Single(); var ack = Command(core, TransactionKind.AcknowledgeEvent, "ack", message.id); ack.actor = ack.secondary = "hud";
            store.Fault = boundary => { if (boundary == CommitBoundary.Replaced) throw new IOException("publish interrupted"); };
            Assert.Throws<IOException>(() => core.Execute(ack)); store.Fault = null; core = Core(store: store);
            core.Execute(ack); ack.key = "ack-new"; ack.effectId = "ack-new-effect"; ack.expectedRevision = core.Revision; core.Execute(ack);
            Assert.That(core.PendingEvents("hud"), Is.Empty); Assert.That(core.Snapshot().consumers.Single().appliedEvents, Is.EqualTo(1)); Journal(core, 3);
        }
        [Test] public void InvalidSnapshotAndChecksumBlockRecoveryWithoutRepairingAssets()
        {
            var seed = Seed(); seed.stacks[0].owner = "missing";
            Assert.That(Assert.Throws<TransactionRejectedException>(() => Core(seed)).Code, Is.EqualTo("owner"));
            var store = new FileTransactionStore(path); var core = Core(store: store); File.WriteAllText(path, "{\"payload\":\"bad\",\"sha256\":\"wrong\"}");
            string before = File.ReadAllText(path); Assert.Throws<InvalidDataException>(() => Core(store: store)); Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            store.Fault = b => { if (b == CommitBoundary.TemporaryFlushed) throw new IOException("disk failed"); };
            Assert.Throws<InvalidDataException>(() => core.Execute(Command(core, TransactionKind.Take, "take")));
            Assert.That(core.Lifecycle, Is.EqualTo(CoreLifecycle.RecoveryFailed)); Rejected(core, Command(core, TransactionKind.Take, "after"), "recovery");
        }
        [TestCase(TransactionKind.DeliverOrder)]
        [TestCase(TransactionKind.CreatePayment)]
        [TestCase(TransactionKind.CollectPayment)]
        [TestCase(TransactionKind.ContributePurchase)]
        [TestCase(TransactionKind.CompletePurchase)]
        public void BusinessMutationCrashAfterCommitCannotRepeat(TransactionKind kind)
        {
            var store = new FileTransactionStore(path); var core = Core(store: store);
            if (kind is TransactionKind.DeliverOrder or TransactionKind.CreatePayment or TransactionKind.CollectPayment) Order(core);
            if (kind is TransactionKind.CreatePayment or TransactionKind.CollectPayment) { core.Execute(Delivery(core)); core.Execute(Command(core, TransactionKind.CompleteOrder, "complete", "order:1")); }
            if (kind == TransactionKind.CollectPayment) core.Execute(Command(core, TransactionKind.CreatePayment, "payment", "order:1"));
            if (kind == TransactionKind.CompletePurchase) { var pay = Command(core, TransactionKind.ContributePurchase, "pay", "barn"); pay.quantity = 500; core.Execute(pay); }
            var command = kind == TransactionKind.DeliverOrder ? Delivery(core) : Command(core, kind, "crash-business", kind == TransactionKind.CollectPayment ? "payment:order:1" : kind is TransactionKind.ContributePurchase or TransactionKind.CompletePurchase ? "barn" : "order:1");
            store.Fault = b => { if (b == CommitBoundary.Replaced) throw new IOException("lost response"); };
            Assert.Throws<IOException>(() => core.Execute(command)); store.Fault = null; core = Core(store: store);
            string before = Json(core); var result = core.Execute(command); Assert.That(Json(core), Is.EqualTo(before));
            command.key = "second-command"; command.expectedRevision = core.Revision;
            Assert.That(core.Execute(command).id, Is.EqualTo(result.id)); Assert.That(Json(core), Is.EqualTo(before));
        }
    }
}
