using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class RetiredPurchaseJournalTests
    {
        [Serializable]
        sealed class Entry
        {
            public long revision;
            public double time;
            public TransactionCommand command;
        }

        [Serializable]
        sealed class Envelope { public string payload, sha256; }

        [TestCase(200, false)]
        [TestCase(1800, false)]
        [TestCase(1800, true)]
        public void RetiredDairyJournalReplaysOnceButNewContributionIsRejected(int amount, bool complete)
        {
            string directory = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "save.json");
            Directory.CreateDirectory(directory);
            try
            {
                var seed = TransactionCoreTests.Seed();
                seed.money = 5000;
                seed.unlocked.Add("mill");
                var core = new TransactionCore(seed, null, () => 100);
                SaveStore.Write(path, new SaveData
                {
                    money = seed.money,
                    transactionVersion = SaveStore.CurrentTransactionVersion,
                    transactionState = core.Snapshot()
                });
                var command = new TransactionCommand
                {
                    kind = TransactionKind.ContributePurchase,
                    actor = "player", target = "dairy", quantity = amount,
                    key = "old-dairy-contribution", effectId = "old-dairy-effect", expectedRevision = 0
                };
                Assert.Throws<TransactionRejectedException>(() => core.Execute(command));
                AppendEntry(path, 1, command);
                if (complete)
                {
                    AppendEntry(path, 2, new TransactionCommand
                    {
                        kind = TransactionKind.CompletePurchase,
                        actor = "player", target = "dairy",
                        key = "old-dairy-completion", effectId = "old-dairy-complete-effect", expectedRevision = 1
                    });
                }
                var restored = SaveStore.Read(path);
                Assert.That(restored.money, Is.EqualTo(seed.money - amount));
                Assert.That(restored.purchases.Single(p => p.id == "dairy").paid, Is.EqualTo(amount));
                Assert.That(restored.transactionState.purchases.Single(p => p.id == "dairy").complete,
                    Is.EqualTo(complete));
                Assert.That(restored.unlocked.Contains("dairy"), Is.EqualTo(complete));
                SaveStore.Write(path, restored);
                Assert.That(SaveStore.Read(path).money, Is.EqualTo(restored.money));
                Assert.That(SaveStore.Read(path).transactionState.revision, Is.EqualTo(complete ? 2 : 1));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        static void AppendEntry(string path, long revision, TransactionCommand command)
        {
            string payload = JsonUtility.ToJson(new Entry { revision = revision, time = 100, command = command });
            using var hash = SHA256.Create();
            string checksum = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(payload)))
                .Replace("-", "");
            File.AppendAllText(path + ".journal",
                JsonUtility.ToJson(new Envelope { payload = payload, sha256 = checksum }) + "\n");
        }
    }
}
