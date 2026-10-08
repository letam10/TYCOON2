using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed partial class TransactionCoreTests
    {
        [Serializable]
        sealed class WorkforceJournalEntry
        {
            public long revision;
            public double time;
            public TransactionCommand command;
        }

        [Serializable]
        sealed class WorkforceJournalEnvelope
        {
            public string payload, sha256;
        }

        TransactionState ReplayWorkforceJournal(TransactionState seed, TransactionCommand command)
        {
            var data = new SaveData { transactionState = seed };
            SaveStore.Write(path, data);
            string payload = JsonUtility.ToJson(new WorkforceJournalEntry
            {
                revision = seed.revision + 1,
                time = now,
                command = command
            }).Replace(",\"workforceRevision\":0", "");
            File.WriteAllText(path + ".journal", JsonUtility.ToJson(new WorkforceJournalEnvelope
            {
                payload = payload,
                sha256 = (string)typeof(FileTransactionStore).GetMethod("Hash",
                    BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { payload })
            }) + "\n");
            return ((SaveData)typeof(GameplayTransactionStore).GetMethod("Recover",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { path, data }))
                .transactionState;
        }

        static TransactionState WorkforceMaximumSeed()
        {
            var seed = Seed();
            foreach (string id in new[] { "storage_processing", "storage_farm_shop" })
                seed.owners.Add(new OwnerState
                {
                    id = id,
                    kind = OwnerKind.Storage,
                    actor = "simulation",
                    location = id,
                    capacity = 96,
                    writers = new() { "player" }
                });
            return seed;
        }

        [Test]
        public void LegacyCountUpgradeReplaysBeforeClampingAndKeepsReceiptDedup()
        {
            var seed = Seed();
            seed.money = 10000;
            var crew = GameSession.CrewFor(Definitions.Upgrade("farmer"));
            crew.count = 2;
            seed.crews.Add(crew);
            seed.unlocked.Add(crew.id);
            var legacy = new TransactionCore(seed, clock: () => now, replaying: true);
            var command = Command(legacy, TransactionKind.UpgradeCrew, "old-count-upgrade", crew.id);
            command.secondary = "count";
            var receipt = legacy.Execute(command);
            var recovered = ReplayWorkforceJournal(seed, command);
            Assert.That(recovered.crews.Single().count, Is.EqualTo(3));
            Assert.That(recovered.money, Is.EqualTo(9400));
            var current = Core(recovered);
            Assert.That(current.Snapshot().crews.Single().count, Is.EqualTo(WorkforceRules.Limit(crew.id)));
            Assert.That(current.Execute(command).id, Is.EqualTo(receipt.id));
            Assert.That(current.Revision, Is.EqualTo(1));
        }

        [Test]
        public void LegacyMaximumJournalRestoresFullRosterThenMigratesExactlyOnce()
        {
            var seed = WorkforceMaximumSeed();
            var legacy = new TransactionCore(seed, clock: () => now, replaying: true);
            var command = Command(legacy, TransactionKind.GrantMaximum, "old-max-journal");
            var receipt = legacy.Execute(command);
            var recovered = ReplayWorkforceJournal(seed, command);
            Assert.That(recovered.crews.Sum(x => x.count), Is.EqualTo(78));
            var current = Core(recovered);
            Assert.That(current.Snapshot().crews.Sum(x => x.count), Is.EqualTo(39));
            Assert.That(current.Execute(command).id, Is.EqualTo(receipt.id));
            Assert.That(WorkforceMigration.Apply(current.Snapshot()), Is.False);
        }

        [Test]
        public void LegacyTruckJournalUsesOldSpeedAndPreservesTripProgress()
        {
            var legacy = new TransactionCore(WorkforceMaximumSeed(), clock: () => now, replaying: true);
            legacy.Execute(Command(legacy, TransactionKind.GrantMaximum, "legacy-truck-unlock"));
            var seed = legacy.Snapshot();
            var truck = seed.truck;
            truck.phase = "Travelling";
            truck.trip = "legacy-trip";
            truck.tripTarget = truck.destination;
            truck.path = new() { new RoutePoint { x = 0, z = 0 }, new RoutePoint { x = 0, z = 20 } };
            truck.distance = 20;
            truck.travelled = 0;
            var writer = new TransactionCore(seed, clock: () => now, replaying: true);
            var command = Command(writer, TransactionKind.TickTruck, "legacy-truck-tick");
            command.actor = "simulation";
            command.secondary = truck.trip;
            command.duration = 1;
            writer.Execute(command);
            var recovered = ReplayWorkforceJournal(seed, command);
            Assert.That(recovered.truck.travelled, Is.EqualTo(5.6).Within(.000001));
            Assert.That(recovered.truck.phase, Is.EqualTo("Travelling"));
            Assert.That(recovered.truck.trip, Is.EqualTo("legacy-trip"));
        }
    }
}
