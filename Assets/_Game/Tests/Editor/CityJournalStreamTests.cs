using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        [Test]
        public void CheckpointThenAppendRecoversEachCommittedCommandExactlyOnce()
        {
            Assert.That(Inventory.Transfer(storage.Inventory, game.Player.Carry, "carrot", 2), Is.EqualTo(2));
            game.Transactions.Checkpoint();
            Assert.That(new FileInfo(game.SavePath + ".journal").Length, Is.GreaterThan(0));
            var command = game.Transactions.Command(TransactionKind.Take, "player",
                key: "after-checkpoint", effect: "after-checkpoint");
            command.source = storage.Id;
            command.destination = "player";
            command.item = "carrot";
            command.quantity = 1;
            var receipt = game.Transactions.Execute(command);
            game.Transactions.Detach();
            var recovered = SaveStore.Read(game.SavePath).transactionState;
            Assert.That(recovered.revision, Is.EqualTo(2));
            Assert.That(recovered.receipts.Count, Is.EqualTo(2));
            Assert.That(recovered.stacks.Where(x => x.owner == "player").Sum(x => x.quantity), Is.EqualTo(3));
            game.InitializeTransactions(recovered);
            Assert.That(game.Transactions.Execute(command).id, Is.EqualTo(receipt.id));
            Assert.That(game.Player.Carry.Count("carrot"), Is.EqualTo(3));
            Assert.That(game.Transactions.Snapshot().revision, Is.EqualTo(2));
            game.Transactions.Checkpoint();
            game.Transactions.Detach();
            Assert.That(new FileInfo(game.SavePath + ".journal").Length, Is.Zero);
        }

        [Test]
        public void JournalAllowsReadersButRejectsAnotherWriterUntilDetached()
        {
            Inventory.Transfer(storage.Inventory, game.Player.Carry, "carrot", 1);
            string path = game.SavePath + ".journal";
            using (var reader = new StreamReader(new FileStream(path, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite)))
                Assert.That(reader.ReadToEnd(), Is.Not.Empty);
            Assert.Throws<IOException>(() =>
            {
                using var competing = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            });
            game.Transactions.Detach();
            using var released = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            Assert.That(released.Length, Is.GreaterThan(0));
        }
    }
}
