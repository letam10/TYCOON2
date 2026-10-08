using System;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        [Test]
        public void UnlockLookupDoesNotAllocatePerFrame()
        {
            game.Economy.Has("farmer");
            long before = GC.GetAllocatedBytesForCurrentThread();
            bool result = false;
            for (int i = 0; i < 1000; i++)
                result |= game.Economy.Has("farmer");
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(result, Is.EqualTo(ReadRuntimeView().unlocked.Contains("farmer")));
        }

        [Test]
        public void ContributionLookupDoesNotCopyEveryPurchase()
        {
            game.Contribution("player_capacity2");
            long before = GC.GetAllocatedBytesForCurrentThread();
            int result = 0;
            for (int i = 0; i < 1000; i++)
                result += game.Contribution("player_capacity2");
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            int expected = ReadRuntimeView().purchases.Find(x => x.id == "player_capacity2")?.contributed ?? 0;
            Assert.That(result, Is.EqualTo(expected * 1000));
        }

        [Test]
        public void UnlockLookupUsesReplacementProjectionAndKeepsCopiesIndependent()
        {
            var state = game.Transactions.Snapshot();
            if (!state.unlocked.Contains("farm_level2")) state.unlocked.Add("farm_level2");
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            var copy = game.Economy.Unlocked;
            copy.Remove("farm_level2");
            Assert.That(game.Economy.Has("farm_level2"), Is.True);
            Assert.That(game.Economy.Has("missing-unlock"), Is.False);
            Assert.That(game.Economy.Has(null), Is.True);
            Assert.That(game.Economy.Has(""), Is.True);
        }
    }
}
