using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        const BindingFlags CheckpointFields = BindingFlags.Instance | BindingFlags.NonPublic;

        GameplayTransactionStore CheckpointStore() => (GameplayTransactionStore)typeof(RuntimeTransactions)
            .GetField("store", CheckpointFields).GetValue(game.Transactions);

        void BeforeCheckpoint(Action callback)
        {
            var writer = typeof(GameplayTransactionStore).GetField("checkpoint", CheckpointFields)
                .GetValue(CheckpointStore());
            writer.GetType().GetField("BeforeWrite", CheckpointFields).SetValue(writer, callback);
        }

        TransactionCommand CheckpointTake(string key)
        {
            var command = game.Transactions.Command(TransactionKind.Take, "player", key: key, effect: key);
            command.source = storage.Id;
            command.destination = "player";
            command.item = "carrot";
            command.quantity = 1;
            return command;
        }

        [Test]
        public void BackgroundCheckpointRetainsLaterJournalAndReceiptDedup()
        {
            using var started = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            BeforeCheckpoint(() =>
            {
                started.Set();
                if (!release.Wait(5000)) throw new TimeoutException("Checkpoint test latch timed out.");
            });
            var first = CheckpointTake("checkpoint-before");
            game.Transactions.Execute(first);
            game.Events.untilRush = 123;
            game.Transactions.Checkpoint();
            try
            {
                Assert.That(started.Wait(5000), Is.True);
                game.Events.untilRush = 999;
                var later = CheckpointTake("checkpoint-after");
                var receipt = game.Transactions.Execute(later);
                game.Transactions.Checkpoint();
                using (var journal = new StreamReader(new FileStream(game.SavePath + ".journal",
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    Assert.That(journal.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                        Is.EqualTo(2));
                release.Set();
                var recovered = CheckpointStore().Read();
                var checkpoint = JsonUtility.FromJson<SaveData>(File.ReadAllText(game.SavePath));
                Assert.That(checkpoint.transactionState.revision, Is.EqualTo(1));
                Assert.That(checkpoint.events.untilRush, Is.EqualTo(123));
                Assert.That(recovered.revision, Is.EqualTo(2));
                var replay = new TransactionCore(recovered);
                Assert.That(replay.Execute(later).id, Is.EqualTo(receipt.id));
                Assert.That(replay.Revision, Is.EqualTo(2));
                Assert.That(replay.Snapshot().stacks.Where(x => x.owner == "player").Sum(x => x.quantity),
                    Is.EqualTo(2));
            }
            finally
            {
                release.Set();
            }
        }

        [Test]
        public void DisposeWaitsForCheckpointBeforeReleasingLeaseAndReopenDeduplicates()
        {
            using var started = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            BeforeCheckpoint(() =>
            {
                started.Set();
                if (!release.Wait(5000)) throw new TimeoutException("Checkpoint test latch timed out.");
            });
            var command = CheckpointTake("checkpoint-dispose");
            var receipt = game.Transactions.Execute(command);
            game.Transactions.Checkpoint();
            Assert.That(started.Wait(5000), Is.True);
            string lockPath = game.SavePath + ".lock";
            var unlock = Task.Run(() =>
            {
                try
                {
                    Assert.Throws<IOException>(() =>
                    {
                        using var competing = new FileStream(lockPath, FileMode.Open,
                            FileAccess.ReadWrite, FileShare.None);
                    });
                }
                finally
                {
                    release.Set();
                }
            });
            game.Transactions.Detach();
            unlock.GetAwaiter().GetResult();
            Assert.That(new FileInfo(game.SavePath + ".journal").Length, Is.Zero);
            using var reopened = new GameplayTransactionStore(game);
            var core = new TransactionCore(null, reopened);
            Assert.That(core.Execute(command).id, Is.EqualTo(receipt.id));
            Assert.That(core.Revision, Is.EqualTo(1));
        }

        [Test]
        public void CompactCheckpointRoundTripsAllSaveFieldsThroughExistingReader()
        {
            game.Transactions.Execute(CheckpointTake("checkpoint-json"));
            game.Events.breakdownMachine = "escaped \"quoted\" text\nTiếng Việt";
            game.Transactions.Checkpoint();
            CheckpointStore().Read();
            string compact = File.ReadAllText(game.SavePath);
            Assert.That(compact, Does.Not.Contain("\n"));
            var original = SaveStore.Read(game.SavePath);
            string roundTrip = Path.Combine(directory, "round-trip.json");
            SaveStore.Write(roundTrip, original);
            var restored = SaveStore.Read(roundTrip);
            Assert.That(JsonUtility.ToJson(restored), Is.EqualTo(JsonUtility.ToJson(original)));
            Assert.That(restored.transactionVersion, Is.EqualTo(3));
            Assert.That(restored.events.breakdownMachine, Is.EqualTo(game.Events.breakdownMachine));
        }

        [Test]
        public void CheckpointCopiesRecipeArraysBeforeBackgroundSerialization()
        {
            using var started = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            BeforeCheckpoint(() =>
            {
                started.Set();
                if (!release.Wait(5000)) throw new TimeoutException("Checkpoint test latch timed out.");
            });
            var state = game.Transactions.Snapshot();
            var batch = RecipeBatchSnapshot.From(Definitions.Recipe("mill"), "player");
            state.stations.Find(x => x.id == machine.Id).batch = batch;
            int originalCount = batch.inputs[0].count;
            CheckpointStore().Checkpoint(state);
            try
            {
                Assert.That(started.Wait(5000), Is.True);
                batch.inputs[0].count = 999;
                release.Set();
                var restored = CheckpointStore().Read();
                Assert.That(restored.stations.Find(x => x.id == machine.Id).batch.inputs[0].count,
                    Is.EqualTo(originalCount));
            }
            finally
            {
                release.Set();
            }
        }

        [Test]
        public void CashCommandAfterCheckpointUsesMonotonicJournalTime()
        {
            var state = game.Transactions.Snapshot();
            state.simulationTime = game.Transactions.Now + 10;
            CheckpointStore().Checkpoint(state);
            var command = game.Transactions.Command(TransactionKind.GrantModCash, "player",
                key: "checkpoint-cash", effect: "checkpoint-cash");
            var receipt = game.Transactions.Execute(command);
            var restored = CheckpointStore().Read();
            var disk = JsonUtility.FromJson<SaveData>(File.ReadAllText(game.SavePath));
            Assert.That(restored.simulationTime, Is.GreaterThanOrEqualTo(disk.transactionState.simulationTime));
            Assert.That(restored.cashInSafe, Is.EqualTo(1000L + DevelopmentAssistance.Grant));
            Assert.That(new TransactionCore(restored).Execute(command).id, Is.EqualTo(receipt.id));
        }

        [Test]
        public void BackgroundFailureIsObservedAndLeaseReleasedOnlyAfterDrain()
        {
            var store = CheckpointStore();
            BeforeCheckpoint(() => throw new IOException("injected background failure"));
            game.Transactions.Execute(CheckpointTake("checkpoint-failure"));
            game.Transactions.Checkpoint();
            Assert.Throws<IOException>(() => store.Read());
            Assert.Throws<IOException>(() => store.Dispose());
            Assert.That(SaveStore.Read(game.SavePath).transactionState.revision, Is.EqualTo(1));
            using var reopened = new GameplayTransactionStore(game);
            Assert.That(reopened.Read().revision, Is.EqualTo(1));
        }

        [TestCase(CommitBoundary.TemporaryFlushed, 0)]
        [TestCase(CommitBoundary.Replaced, 1)]
        public void InjectedCheckpointFaultStaysSynchronousAndJournalRecovers(
            CommitBoundary boundary, long snapshotRevision)
        {
            var store = CheckpointStore();
            game.Transactions.Execute(CheckpointTake("checkpoint-fault"));
            store.Fault = at =>
            {
                if (at == boundary) throw new IOException("injected checkpoint interruption");
            };
            try
            {
                Assert.Throws<IOException>(() => game.Transactions.Checkpoint());
                var checkpoint = JsonUtility.FromJson<SaveData>(File.ReadAllText(game.SavePath));
                Assert.That(checkpoint.transactionState.revision, Is.EqualTo(snapshotRevision));
                Assert.That(SaveStore.Read(game.SavePath).transactionState.revision, Is.EqualTo(1));
                Assert.That(File.Exists(game.SavePath + ".tmp"), Is.False);
            }
            finally
            {
                store.Fault = null;
            }
        }
    }
}
