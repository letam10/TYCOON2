using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class PhysicalCashRecoveryTests
    {
        [Serializable]
        sealed class Entry
        {
            public long revision;
            public double time;
            public TransactionCommand command;
        }

        [Serializable]
        sealed class Envelope
        {
            public string payload;
            public string sha256;
        }

        [Test]
        public void LegacyJournalReplaysBeforeOneTimeCashMigrationAndKeepsReceiptDedup()
        {
            string directory = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "save.json");
            Directory.CreateDirectory(directory);
            try
            {
                var seed = TransactionCoreTests.Seed();
                seed.money = 500;
                var core = new TransactionCore(seed, null, () => 100);
                var save = new SaveData
                {
                    version = 2,
                    money = seed.money,
                    transactionVersion = seed.schemaVersion,
                    transactionState = core.Snapshot()
                };
                SaveStore.Write(path, save);
                var command = new TransactionCommand
                {
                    kind = TransactionKind.GrantModCash,
                    actor = "player",
                    key = "legacy-cash-grant",
                    effectId = "legacy-cash-grant-effect",
                    expectedRevision = 0
                };
                AppendEntry(path, 1, command);

                var recovered = SaveStore.Read(path);
                Assert.That(recovered.transactionState.schemaVersion, Is.EqualTo(2));
                Assert.That(recovered.money, Is.EqualTo(500 + 999999));
                Assert.That(recovered.transactionState.receipts.Count, Is.EqualTo(1));

                Assert.That(PhysicalCashMigration.Upgrade(recovered), Is.True);
                Assert.That(recovered.version, Is.EqualTo(3));
                Assert.That(recovered.money, Is.Zero);
                Assert.That(recovered.cashInHand, Is.Zero);
                Assert.That(recovered.cashInSafe, Is.EqualTo(500 + 999999));
                Assert.That(PhysicalCashMigration.Upgrade(recovered), Is.False);

                var migratedCore = new TransactionCore(recovered.transactionState, null, () => 100);
                Assert.That(migratedCore.Execute(command).id, Is.EqualTo("receipt:legacy-cash-grant"));
                Assert.That(migratedCore.Snapshot().cashInSafe, Is.EqualTo(500 + 999999));
                Assert.That(migratedCore.Snapshot().receipts.Count, Is.EqualTo(1));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        static void AppendEntry(string path, long revision, TransactionCommand command)
        {
            string payload = JsonUtility.ToJson(new Entry { revision = revision, time = 100, command = command });
            File.AppendAllText(path + ".journal", JsonUtility.ToJson(new Envelope
            {
                payload = payload,
                sha256 = Hash(payload)
            }) + "\n");
        }

        static string Hash(string value)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
    }
}
