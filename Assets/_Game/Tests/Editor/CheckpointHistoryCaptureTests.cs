using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class CheckpointHistoryCaptureTests
    {
        static readonly Type CaptureType = typeof(TransactionCore).Assembly
            .GetType("Tycoon.CheckpointHistoryCapture", true);
        static readonly MethodInfo CreateMethod = CaptureType
            .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly MethodInfo MaterializeMethod = CaptureType
            .GetMethod("Materialize", BindingFlags.Instance | BindingFlags.NonPublic);

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void NullAndEmptyListsKeepTheirMeaning(bool nullReceipts, bool nullEvents)
        {
            var receipts = nullReceipts ? null : new List<TransactionReceipt>();
            var events = nullEvents ? null : new List<TransactionEvent>();
            var capture = Create(receipts, events);
            var state = Materialize(capture);
            Assert.That(state.receipts, nullReceipts ? Is.Null : Is.Empty);
            Assert.That(state.outbox, nullEvents ? Is.Null : Is.Empty);
            if (!nullReceipts)
                Assert.That(state.receipts, Is.Not.SameAs(receipts));
            if (!nullEvents)
                Assert.That(state.outbox, Is.Not.SameAs(events));
        }

        [Test]
        public void AllFieldsNullsOrderAndDuplicateEntriesArePreserved()
        {
            var receipt = Receipt(1);
            var transactionEvent = Event(1);
            var receipts = new List<TransactionReceipt> { receipt, null, receipt, new TransactionReceipt() };
            var events = new List<TransactionEvent> { transactionEvent, null, transactionEvent,
                new TransactionEvent { consumers = null }, new TransactionEvent() };
            var state = Materialize(Create(receipts, events));
            AssertHistories(receipts, events, state);
            Assert.That(state.receipts[0], Is.Not.SameAs(state.receipts[2]));
            Assert.That(state.outbox[0], Is.Not.SameAs(state.outbox[2]));
            Assert.That(state.outbox[0].consumers, Is.Not.SameAs(state.outbox[2].consumers));
            state.outbox[0].consumers[0] = "changed duplicate";
            Assert.That(state.outbox[2].consumers[0], Is.EqualTo("first"));
        }

        [TestCase(9007199254740993L)]
        [TestCase(long.MaxValue)]
        [TestCase(long.MinValue)]
        public void MoneyAndRevisionRemainExactInt64(long value)
        {
            var receipt = Receipt(1);
            receipt.cashAmount = value;
            receipt.revision = value;
            var transactionEvent = Event(1);
            transactionEvent.revision = value;
            var state = Materialize(Create(new() { receipt }, new() { transactionEvent }));
            Assert.That(state.receipts[0].cashAmount, Is.EqualTo(value));
            Assert.That(state.receipts[0].revision, Is.EqualTo(value));
            Assert.That(state.outbox[0].revision, Is.EqualTo(value));
        }

        [Test]
        public void SourceMutationsBeforeWorkerMaterializationCannotChangeCapture()
        {
            var receipt = Receipt(1);
            var transactionEvent = Event(1);
            var receipts = new List<TransactionReceipt> { receipt };
            var events = new List<TransactionEvent> { transactionEvent };
            var capture = Create(receipts, events);
            MutateFields(receipt);
            MutateFields(transactionEvent);
            transactionEvent.consumers[0] = "live";
            transactionEvent.consumers.Clear();
            transactionEvent.consumers = null;
            receipts.Clear();
            events.Clear();

            var state = Task.Run(() => Materialize(capture)).GetAwaiter().GetResult();
            AssertHistories(new() { Receipt(1) }, new() { Event(1) }, state);
        }

        [Test]
        public void RepeatedWorkerOutputsAndSourceAreIndependentlyMutable()
        {
            var receipt = Receipt(1);
            var transactionEvent = Event(1);
            var receipts = new List<TransactionReceipt> { receipt };
            var events = new List<TransactionEvent> { transactionEvent };
            var capture = Create(receipts, events);
            var outputs = Task.WhenAll(Task.Run(() => Materialize(capture)), Task.Run(() => Materialize(capture)))
                .GetAwaiter().GetResult();
            var first = outputs[0];
            var second = outputs[1];
            Assert.That(first.receipts, Is.Not.SameAs(second.receipts));
            Assert.That(first.outbox, Is.Not.SameAs(second.outbox));
            Assert.That(first.receipts[0], Is.Not.SameAs(second.receipts[0]));
            Assert.That(first.outbox[0], Is.Not.SameAs(second.outbox[0]));
            Assert.That(first.outbox[0].consumers, Is.Not.SameAs(second.outbox[0].consumers));
            MutateFields(first.receipts[0]);
            MutateFields(first.outbox[0]);
            first.outbox[0].consumers.Clear();
            first.receipts.Clear();
            first.outbox.Clear();
            AssertHistories(new() { Receipt(1) }, new() { Event(1) }, second);
            AssertFields(Receipt(1), receipt);
            AssertFields(Event(1), transactionEvent);

            MutateFields(receipt);
            MutateFields(transactionEvent);
            transactionEvent.consumers.Clear();
            AssertHistories(new() { Receipt(1) }, new() { Event(1) }, second);
            MutateFields(second.receipts[0]);
            second.outbox[0].consumers.Add("second output");
            AssertHistories(new() { Receipt(1) }, new() { Event(1) }, Materialize(capture));
        }

        [Test]
        public void MaterializeReplacesHistoryAndPreservesOwnedWorld()
        {
            var capture = Create(new() { Receipt(1) }, new() { Event(1) });
            var owned = new TransactionState { revision = 73, money = 9007199254740993L };
            var owners = owned.owners;
            owned.unlocked.Add("farm");
            owned.receipts.Add(Receipt(2));
            owned.outbox.Add(Event(2));
            MaterializeMethod.Invoke(capture, new object[] { owned });
            AssertHistories(new() { Receipt(1) }, new() { Event(1) }, owned);
            Assert.That(owned.revision, Is.EqualTo(73));
            Assert.That(owned.money, Is.EqualTo(9007199254740993L));
            Assert.That(owned.owners, Is.SameAs(owners));
            Assert.That(owned.unlocked, Is.EqualTo(new[] { "farm" }));
        }

        [Test]
        public void TwentyThousandRecordsPreserveEveryFieldAndConsumerOffset()
        {
            const int count = 20000;
            var receipts = new List<TransactionReceipt>(count);
            var events = new List<TransactionEvent>(count);
            for (int index = 0; index < count; index++)
            {
                receipts.Add(index % 7 == 0 ? null : Receipt(index));
                var transactionEvent = index % 11 == 0 ? null : Event(index);
                if (transactionEvent != null && index % 3 == 0)
                    transactionEvent.consumers = null;
                if (transactionEvent != null && index % 3 == 1)
                    transactionEvent.consumers.Clear();
                events.Add(transactionEvent);
            }
            var capture = Create(receipts, events);
            var state = Task.Run(() => Materialize(capture)).GetAwaiter().GetResult();
            AssertHistories(receipts, events, state);
            receipts[1].cashAmount = 0;
            events[2].consumers[0] = "live";
            state.receipts[2].id = "output";
            state.outbox[5].consumers.Clear();
            Assert.That(state.receipts[1].cashAmount, Is.EqualTo(9007199254740994L));
            Assert.That(state.outbox[2].consumers[0], Is.EqualTo("first"));
            Assert.That(receipts[2].id, Is.EqualTo("receipt-2"));
            Assert.That(events[5].consumers.Count, Is.EqualTo(4));
            var repeated = Materialize(capture);
            Assert.That(repeated.receipts[2].id, Is.EqualTo("receipt-2"));
            Assert.That(repeated.outbox[5].consumers.Count, Is.EqualTo(4));
        }

        static object Create(List<TransactionReceipt> receipts, List<TransactionEvent> events) =>
            CreateMethod.Invoke(null, new object[] { receipts, events });

        static TransactionState Materialize(object capture)
        {
            var result = new TransactionState();
            MaterializeMethod.Invoke(capture, new object[] { result });
            return result;
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

        static void AssertHistories(List<TransactionReceipt> receipts, List<TransactionEvent> events,
            TransactionState state)
        {
            Assert.That(state.receipts.Count, Is.EqualTo(receipts.Count));
            Assert.That(state.outbox.Count, Is.EqualTo(events.Count));
            for (int index = 0; index < receipts.Count; index++)
                AssertFields(receipts[index], state.receipts[index]);
            for (int index = 0; index < events.Count; index++)
                AssertFields(events[index], state.outbox[index]);
        }

        static void AssertFields<T>(T source, T output) where T : class
        {
            if (source == null)
            {
                Assert.That(output, Is.Null);
                return;
            }
            Assert.That(output, Is.Not.Null.And.Not.SameAs(source));
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var expected = field.GetValue(source);
                var actual = field.GetValue(output);
                Assert.That(actual, Is.EqualTo(expected), field.Name);
                if (expected is List<string>)
                    Assert.That(actual, Is.Not.SameAs(expected));
            }
        }

        static void MutateFields<T>(T value)
        {
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(string))
                    field.SetValue(value, "changed");
                else if (field.FieldType == typeof(long))
                    field.SetValue(value, 0L);
                else if (field.FieldType == typeof(int))
                    field.SetValue(value, 0);
            }
        }
    }
}
