using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class CheckpointHistoryTests
    {
        static readonly MethodInfo CopyMethod = typeof(TransactionCore).Assembly
            .GetType("Tycoon.CheckpointSnapshot")
            .GetMethod("Copy", BindingFlags.Static | BindingFlags.NonPublic);

        static T Copy<T>(T value) => (T)CopyMethod.MakeGenericMethod(typeof(T))
            .Invoke(null, new object[] { value });

        static TransactionState Capture(TransactionCore core) => (TransactionState)typeof(TransactionCore)
            .GetMethod("CaptureCheckpoint", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(core, null);

        [Test]
        public void ReceiptSchemaContainsOnlySupportedImmutableFields()
        {
            AssertSchema(typeof(TransactionReceipt), new Dictionary<string, Type>
            {
                ["id"] = typeof(string),
                ["key"] = typeof(string),
                ["fingerprint"] = typeof(string),
                ["effectId"] = typeof(string),
                ["effectFingerprint"] = typeof(string),
                ["eventId"] = typeof(string),
                ["revision"] = typeof(long),
                ["amount"] = typeof(int),
                ["cashAmount"] = typeof(long)
            });
        }

        [Test]
        public void EventSchemaHasOnlyConsumersAsMutableField()
        {
            AssertSchema(typeof(TransactionEvent), new Dictionary<string, Type>
            {
                ["id"] = typeof(string),
                ["receiptId"] = typeof(string),
                ["effectId"] = typeof(string),
                ["kind"] = typeof(string),
                ["revision"] = typeof(long),
                ["consumers"] = typeof(List<string>)
            });
        }

        [Test]
        public void ReceiptHistoryPreservesAllFieldsNullsOrderAndTwoWayIsolation()
        {
            var first = Receipt(1);
            var last = Receipt(2);
            var source = new List<TransactionReceipt> { first, null, first, last };
            var snapshot = Copy(source);
            Assert.That(snapshot, Is.Not.SameAs(source));
            Assert.That(snapshot.Count, Is.EqualTo(4));
            Assert.That(snapshot[1], Is.Null);
            AssertFields(first, snapshot[0]);
            AssertFields(first, snapshot[2]);
            AssertFields(last, snapshot[3]);
            Assert.That(snapshot[2], Is.Not.SameAs(snapshot[0]));

            first.key = "live-key";
            first.cashAmount = 0;
            snapshot[3].effectFingerprint = "snapshot-effect";
            snapshot[3].revision = 0;
            source.RemoveAt(1);
            Assert.That(snapshot[0].key, Is.EqualTo("key-1"));
            Assert.That(snapshot[0].cashAmount, Is.EqualTo(9007199254740994L));
            Assert.That(last.effectFingerprint, Is.EqualTo("effect-fingerprint-2"));
            Assert.That(last.revision, Is.EqualTo(long.MaxValue - 2));
            Assert.That(snapshot.Count, Is.EqualTo(4));
            snapshot.Clear();
            Assert.That(source.Count, Is.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void StandaloneEventPreservesAllFieldsAndConsumerStates(int consumerState)
        {
            var source = Event(1);
            if (consumerState == 0) source.consumers = null;
            if (consumerState == 1) source.consumers.Clear();
            var snapshot = (TransactionEvent)Copy<object>(source);
            AssertFields(source, snapshot);
            if (source.consumers == null) return;
            Assert.That(snapshot.consumers, Is.Not.SameAs(source.consumers));
            source.consumers.Add("live");
            snapshot.consumers.Add("snapshot");
            Assert.That(snapshot.consumers, Does.Not.Contain("live"));
            Assert.That(source.consumers, Does.Not.Contain("snapshot"));
            source.kind = "live-kind";
            snapshot.receiptId = "snapshot-receipt";
            Assert.That(snapshot.kind, Is.EqualTo("kind-1"));
            Assert.That(source.receiptId, Is.EqualTo("receipt-1"));
        }

        [Test]
        public void LargeEventHistoryPreservesOrderNullsAndTwoWayIsolation()
        {
            const int count = 20000;
            var source = new List<TransactionEvent>(count);
            for (int index = 0; index < count; index++)
                source.Add(index % 7 == 0 ? null : Event(index));
            var snapshot = Copy(source);
            Assert.That(snapshot.Count, Is.EqualTo(count));
            for (int index = 0; index < count; index++)
            {
                if (source[index] == null)
                {
                    Assert.That(snapshot[index], Is.Null);
                    continue;
                }
                AssertFields(source[index], snapshot[index]);
                Assert.That(snapshot[index].consumers, Is.Not.SameAs(source[index].consumers));
            }

            source[1].consumers[0] = "live";
            source[1].revision = 0;
            snapshot[2].consumers.Clear();
            snapshot[2].id = "snapshot";
            source.RemoveAt(0);
            Assert.That(snapshot[1].consumers[0], Is.EqualTo("first"));
            Assert.That(snapshot[1].revision, Is.EqualTo(long.MaxValue - 1));
            Assert.That(source[1].consumers.Count, Is.EqualTo(4));
            Assert.That(source[1].id, Is.EqualTo("event-2"));
            Assert.That(snapshot.Count, Is.EqualTo(count));
            snapshot.Clear();
            Assert.That(source.Count, Is.EqualTo(count - 1));
        }

        [Test]
        public void NullAndEmptyHistoryListsKeepTheirOriginalMeaning()
        {
            Assert.That(Copy<List<TransactionReceipt>>(null), Is.Null);
            Assert.That(Copy<List<TransactionEvent>>(null), Is.Null);
            Assert.That(Copy<TransactionReceipt>(null), Is.Null);
            Assert.That(Copy<TransactionEvent>(null), Is.Null);
            var source = new TransactionState();
            var snapshot = Copy(source);
            Assert.That(snapshot.receipts, Is.Empty.And.Not.SameAs(source.receipts));
            Assert.That(snapshot.outbox, Is.Empty.And.Not.SameAs(source.outbox));
            source.receipts = null;
            source.outbox = null;
            snapshot = Copy(source);
            Assert.That(snapshot.receipts, Is.Null);
            Assert.That(snapshot.outbox, Is.Null);
        }

        [Test]
        public void CapturedHistoryRemainsIsolatedFromLiveAcknowledgementAndLaterCommits()
        {
            var core = new TransactionCore(TransactionCoreTests.Seed(), runtime: true);
            var command = new TransactionCommand
            {
                key = "checkpoint",
                effectId = "checkpoint-effect",
                actor = "simulation",
                kind = TransactionKind.Checkpoint,
                expectedRevision = core.Revision
            };
            var receipt = core.Execute(command);
            var snapshot = Capture(core);
            snapshot.receipts[0].key = "snapshot-key";
            snapshot.outbox[0].kind = "snapshot-kind";
            snapshot.outbox[0].consumers.Add("snapshot-consumer");
            Assert.That(core.Execute(command).id, Is.EqualTo(receipt.id));
            core.Execute(new TransactionCommand
            {
                key = "ack",
                effectId = "ack-effect",
                actor = "live-consumer",
                secondary = "live-consumer",
                kind = TransactionKind.AcknowledgeEvent,
                expectedRevision = core.Revision,
                target = receipt.eventId
            });
            var live = Capture(core);
            Assert.That(live.receipts[0].key, Is.EqualTo("checkpoint"));
            Assert.That(live.outbox[0].kind, Is.EqualTo(nameof(TransactionKind.Checkpoint)));
            Assert.That(live.outbox[0].consumers, Is.EqualTo(new[] { "live-consumer" }));
            Assert.That(snapshot.outbox[0].consumers, Is.EqualTo(new[] { "snapshot-consumer" }));
            Assert.That(snapshot.receipts.Count, Is.EqualTo(1));
            Assert.That(snapshot.outbox.Count, Is.EqualTo(1));
            snapshot.receipts.Clear();
            snapshot.outbox.Clear();
            Assert.That(Capture(core).receipts.Count, Is.EqualTo(2));
            Assert.That(Capture(core).outbox.Count, Is.EqualTo(2));
            TransactionCore.Validate(live);
        }

        static TransactionReceipt Receipt(int index) => new()
        {
            id = "receipt-" + index,
            key = "key-" + index,
            fingerprint = "fingerprint-" + index,
            effectId = "effect-" + index,
            effectFingerprint = "effect-fingerprint-" + index,
            eventId = "event-" + index,
            revision = long.MaxValue - index,
            amount = int.MaxValue - index,
            cashAmount = 9007199254740993L + index
        };

        static TransactionEvent Event(int index) => new()
        {
            id = "event-" + index,
            receiptId = "receipt-" + index,
            effectId = "effect-" + index,
            kind = "kind-" + index,
            revision = long.MaxValue - index,
            consumers = new List<string> { "first", null, "first", "last" }
        };

        static void AssertSchema(Type type, Dictionary<string, Type> expected)
        {
            Assert.That(type.IsSealed, Is.True);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Trường mới phải được xét lại, tránh vô tình chia sẻ tham chiếu mutable trong fast path.
            Assert.That(fields.Select(field => field.Name), Is.EquivalentTo(expected.Keys));
            foreach (var field in fields)
                Assert.That(field.FieldType, Is.EqualTo(expected[field.Name]), field.Name);
        }

        static void AssertFields<T>(T source, T snapshot)
        {
            Assert.That(snapshot, Is.Not.SameAs(source));
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
                Assert.That(field.GetValue(snapshot), Is.EqualTo(field.GetValue(source)), field.Name);
        }
    }
}
