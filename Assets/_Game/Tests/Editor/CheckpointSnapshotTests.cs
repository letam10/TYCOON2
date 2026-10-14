using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class CheckpointSnapshotTests
    {
        static readonly MethodInfo CopyMethod = typeof(TransactionState).Assembly
            .GetType("Tycoon.CheckpointSnapshot")
            .GetMethod("Copy", BindingFlags.Static | BindingFlags.NonPublic);

        // Test assembly gọi API internal qua reflection, không mở rộng API runtime.
        static T Copy<T>(T value) => (T)CopyMethod.MakeGenericMethod(typeof(T))
            .Invoke(null, new object[] { value });

        [Test]
        public void CopyPreservesRuntimeTypeEveryReceiptFieldAndLargeLongs()
        {
            var source = new TransactionReceipt
            {
                id = "receipt-1",
                key = "request-1",
                fingerprint = "request-fingerprint",
                effectId = "effect-1",
                effectFingerprint = "effect-fingerprint",
                eventId = "event-1",
                revision = long.MaxValue - 7,
                amount = int.MaxValue - 3,
                cashAmount = 9007199254740993L
            };

            object copied = Copy<object>(source);
            Assert.That(copied, Is.TypeOf<TransactionReceipt>());
            Assert.That(copied, Is.Not.SameAs(source));
            var snapshot = (TransactionReceipt)copied;
            Assert.That(snapshot.id, Is.EqualTo(source.id));
            Assert.That(snapshot.key, Is.EqualTo(source.key));
            Assert.That(snapshot.fingerprint, Is.EqualTo(source.fingerprint));
            Assert.That(snapshot.effectId, Is.EqualTo(source.effectId));
            Assert.That(snapshot.effectFingerprint, Is.EqualTo(source.effectFingerprint));
            Assert.That(snapshot.eventId, Is.EqualTo(source.eventId));
            Assert.That(snapshot.revision, Is.EqualTo(long.MaxValue - 7));
            Assert.That(snapshot.amount, Is.EqualTo(int.MaxValue - 3));
            Assert.That(snapshot.cashAmount, Is.EqualTo(9007199254740993L));

            source.key = "source-changed";
            source.revision = 0;
            snapshot.cashAmount = 0;
            Assert.That(snapshot.key, Is.EqualTo("request-1"));
            Assert.That(snapshot.revision, Is.EqualTo(long.MaxValue - 7));
            Assert.That(source.cashAmount, Is.EqualTo(9007199254740993L));
        }

        [Test]
        public void CopyKeepsNestedListsRecipeMetadataArraysAndNullsIndependentInBothDirections()
        {
            var batch = new RecipeBatchSnapshot
            {
                recipe = "test-recipe",
                output = "bread",
                initiator = "worker-1",
                version = 4,
                yield = 3,
                seconds = 12.5f,
                inputs = new[] { new ItemAmount("flour", 2), null, new ItemAmount("milk", 1) }
            };
            var source = new TransactionState
            {
                revision = long.MaxValue - 1,
                cashInSafe = 9007199254740993L,
                jobIds = null,
                owners = new List<OwnerState>
                {
                    new OwnerState
                    {
                        id = "owner-1",
                        writers = new List<string> { "player", null },
                        limits = new List<ItemAmount> { new ItemAmount("flour", 8), null },
                        customer = new CustomerSave
                        {
                            receipt = long.MaxValue - 2,
                            order = new List<OrderLine> { new OrderLine("bread", 3, 12) }
                        }
                    },
                    null
                },
                stations = new List<StationRuntimeState>
                {
                    new StationRuntimeState
                    {
                        id = "station-1",
                        recipeOptions = new List<string> { "test-recipe" },
                        batch = batch
                    },
                    new StationRuntimeState { id = "station-2", batch = null }
                }
            };

            var snapshot = Copy(source);
            var owner = snapshot.owners[0];
            var copiedBatch = snapshot.stations[0].batch;
            Assert.That(snapshot.revision, Is.EqualTo(source.revision));
            Assert.That(snapshot.cashInSafe, Is.EqualTo(source.cashInSafe));
            Assert.That(snapshot.owners, Is.Not.SameAs(source.owners));
            Assert.That(owner, Is.Not.SameAs(source.owners[0]));
            Assert.That(owner.customer, Is.Not.SameAs(source.owners[0].customer));
            Assert.That(owner.customer.order, Is.Not.SameAs(source.owners[0].customer.order));
            Assert.That(owner.customer.receipt, Is.EqualTo(long.MaxValue - 2));
            Assert.That(owner.customer.order[0].requested, Is.EqualTo(3));
            Assert.That(copiedBatch, Is.Not.SameAs(batch));
            Assert.That(copiedBatch.inputs, Is.Not.SameAs(batch.inputs));
            Assert.That(copiedBatch.inputs[0], Is.Not.SameAs(batch.inputs[0]));
            Assert.That(copiedBatch.recipe, Is.EqualTo("test-recipe"));
            Assert.That(copiedBatch.output, Is.EqualTo("bread"));
            Assert.That(copiedBatch.initiator, Is.EqualTo("worker-1"));
            Assert.That(copiedBatch.version, Is.EqualTo(4));
            Assert.That(copiedBatch.yield, Is.EqualTo(3));
            Assert.That(copiedBatch.seconds, Is.EqualTo(12.5f));
            Assert.That(snapshot.jobIds, Is.Null);
            Assert.That(snapshot.owners[1], Is.Null);
            Assert.That(owner.writers[1], Is.Null);
            Assert.That(owner.limits[1], Is.Null);
            Assert.That(copiedBatch.inputs[1], Is.Null);
            Assert.That(snapshot.stations[1].batch, Is.Null);

            source.owners[0].writers[0] = "source-writer";
            source.owners[0].limits[0].count = 99;
            source.owners[0].customer.order[0].requested = 99;
            source.stations[0].recipeOptions.Add("source-recipe");
            batch.inputs[0].count = 99;
            batch.recipe = "source-recipe";
            source.owners.Add(new OwnerState { id = "source-owner" });
            Assert.That(owner.writers[0], Is.EqualTo("player"));
            Assert.That(owner.limits[0].count, Is.EqualTo(8));
            Assert.That(owner.customer.order[0].requested, Is.EqualTo(3));
            Assert.That(snapshot.stations[0].recipeOptions, Is.EqualTo(new[] { "test-recipe" }));
            Assert.That(copiedBatch.inputs[0].count, Is.EqualTo(2));
            Assert.That(copiedBatch.recipe, Is.EqualTo("test-recipe"));
            Assert.That(snapshot.owners.Count, Is.EqualTo(2));

            owner.writers.Add("snapshot-writer");
            owner.limits[0].id = "snapshot-item";
            owner.customer.order[0].delivered = 2;
            snapshot.stations[0].recipeOptions[0] = "snapshot-recipe";
            copiedBatch.inputs[2].count = 88;
            copiedBatch.output = "snapshot-output";
            snapshot.stations.Clear();
            Assert.That(source.owners[0].writers.Count, Is.EqualTo(2));
            Assert.That(source.owners[0].limits[0].id, Is.EqualTo("flour"));
            Assert.That(source.owners[0].customer.order[0].delivered, Is.Zero);
            Assert.That(source.stations[0].recipeOptions[0], Is.EqualTo("test-recipe"));
            Assert.That(batch.inputs[2].count, Is.EqualTo(1));
            Assert.That(batch.output, Is.EqualTo("bread"));
            Assert.That(source.stations.Count, Is.EqualTo(2));
        }

        [Test]
        public void CopyPreservesEveryReceiptAndOrderInLargeHistoryWithoutSharingEntries()
        {
            const int count = 20000;
            var source = new List<TransactionReceipt>(count);
            for (int index = 0; index < count; index++)
            {
                source.Add(new TransactionReceipt
                {
                    id = "receipt-" + index,
                    revision = (long)int.MaxValue + index,
                    cashAmount = 9007199254740993L + index
                });
            }

            var snapshot = Copy(source);
            Assert.That(snapshot.Count, Is.EqualTo(count));
            for (int index = 0; index < count; index++)
            {
                Assert.That(snapshot[index], Is.Not.SameAs(source[index]));
                Assert.That(snapshot[index].id, Is.EqualTo("receipt-" + index));
                Assert.That(snapshot[index].revision, Is.EqualTo((long)int.MaxValue + index));
                Assert.That(snapshot[index].cashAmount, Is.EqualTo(9007199254740993L + index));
            }

            source[0].id = "source-changed";
            snapshot[count - 1].id = "snapshot-changed";
            source.RemoveAt(count / 2);
            Assert.That(snapshot[0].id, Is.EqualTo("receipt-0"));
            Assert.That(source[source.Count - 1].id, Is.EqualTo("receipt-" + (count - 1)));
            Assert.That(snapshot.Count, Is.EqualTo(count));
            snapshot.Clear();
            Assert.That(source.Count, Is.EqualTo(count - 1));
        }

        [Test]
        public void CopyPreservesNullStringAndValueTypes()
        {
            Assert.That(Copy<TransactionState>(null), Is.Null);
            string text = new string('x', 5);
            Assert.That(Copy(text), Is.SameAs(text));
            Assert.That(Copy(long.MaxValue), Is.EqualTo(long.MaxValue));
            Assert.That(Copy(new RecipeBatchSnapshot { inputs = null }).inputs, Is.Null);
            Assert.That(Copy(Array.Empty<ItemAmount>()), Is.Empty);
        }
    }
}
