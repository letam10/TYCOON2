using System;
using System.IO;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        TransactionState DeferredCapture(out object history)
        {
            var core = (TransactionCore)typeof(RuntimeTransactions)
                .GetField("core", CheckpointFields).GetValue(game.Transactions);
            var arguments = new object[] { null };
            var state = (TransactionState)typeof(TransactionCore)
                .GetMethod("CaptureDeferredCheckpoint", CheckpointFields).Invoke(core, arguments);
            history = arguments[0];
            return state;
        }

        static void MaterializeHistory(object history, TransactionState state)
        {
            history.GetType().GetMethod("Materialize", CheckpointFields).Invoke(history, new object[] { state });
        }

        [Test]
        public void DeferredCaptureKeepsHistoryAndWorldAtSameRevisionAfterLaterAcknowledgment()
        {
            var command = CheckpointTake("deferred-before");
            var receipt = game.Transactions.Execute(command);
            var before = game.Transactions.Snapshot();
            var captured = DeferredCapture(out var history);
            Assert.That(captured.receipts, Is.Null);
            Assert.That(captured.outbox, Is.Null);
            var acknowledgment = game.Transactions.Command(TransactionKind.AcknowledgeEvent,
                "deferred-consumer", receipt.eventId, key: "deferred-after", effect: "deferred-after");
            acknowledgment.secondary = "deferred-consumer";
            game.Transactions.Execute(acknowledgment);
            MaterializeHistory(history, captured);
            // Snapshot đã qua một vòng JSON; so sánh cùng ranh giới để tránh lệch chữ số cuối của double.
            var serializedCapture = JsonUtility.FromJson<TransactionState>(JsonUtility.ToJson(captured));
            Assert.That(JsonUtility.ToJson(serializedCapture), Is.EqualTo(JsonUtility.ToJson(before)));
            Assert.That(game.Transactions.Snapshot().outbox[0].consumers, Does.Contain("deferred-consumer"));
            captured.outbox[0].consumers.Add("captured-only");
            Assert.That(game.Transactions.Snapshot().outbox[0].consumers, Does.Not.Contain("captured-only"));
            Assert.That(new TransactionCore(captured).Execute(command).id, Is.EqualTo(receipt.id));
        }

        [Test]
        public void DeferredWriterMaterializesBeforeCallbackAndRetainsBackgroundFailure()
        {
            game.Transactions.Execute(CheckpointTake("deferred-writer"));
            var captured = DeferredCapture(out var history);
            var owned = new SaveData { transactionState = captured };
            var writerType = typeof(TransactionCore).Assembly.GetType("Tycoon.CheckpointWriter", true);
            var writer = Activator.CreateInstance(writerType, CheckpointFields, null,
                new object[] { Path.Combine(directory, "deferred-writer.json") }, null);
            int callerThread = Thread.CurrentThread.ManagedThreadId;
            int workerThread = 0;
            int receiptCount = -1;
            int eventCount = -1;
            writerType.GetField("BeforeWrite", CheckpointFields).SetValue(writer, (Action)(() =>
            {
                workerThread = Thread.CurrentThread.ManagedThreadId;
                receiptCount = owned.transactionState.receipts.Count;
                eventCount = owned.transactionState.outbox.Count;
                throw new IOException("deferred injected failure");
            }));
            writerType.GetMethod("StartCaptured", CheckpointFields)
                .Invoke(writer, new object[] { owned, history });
            var observe = writerType.GetMethod("Observe", CheckpointFields);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var failure = Assert.Throws<TargetInvocationException>(() =>
                    observe.Invoke(writer, new object[] { true }));
                Assert.That(failure.InnerException, Is.TypeOf<IOException>());
                Assert.That(failure.InnerException.InnerException.Message, Is.EqualTo("deferred injected failure"));
            }
            Assert.That(workerThread, Is.Not.EqualTo(callerThread));
            Assert.That(receiptCount, Is.EqualTo(1));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(writerType.GetProperty("HasWritten", CheckpointFields).GetValue(writer), Is.False);
            Assert.That(File.Exists(Path.Combine(directory, "deferred-writer.json")), Is.False);
            Assert.That(writerType.GetMethod("Start", CheckpointFields), Is.Not.Null);
            Assert.That(typeof(TransactionCore).GetMethod("CaptureCheckpoint", CheckpointFields), Is.Not.Null);
        }
    }
}
