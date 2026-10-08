using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        [Test]
        public void RestoredClockStartsExactlyAtSavedTime()
        {
            var state = game.Transactions.Snapshot();
            state.simulationTime = .12345678901234;
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            Assert.That(game.Transactions.Now, Is.EqualTo(state.simulationTime));
        }
    }
}
