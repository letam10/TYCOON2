using System;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        [Test]
        public void StationViewReadsDoNotAllocatePerFrame()
        {
            int phase = crop.Phase;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
                phase += crop.Phase;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(phase, Is.Zero);
        }

        [Test]
        public void StationViewLookupFollowsCommittedProjection()
        {
            Assert.That(crop.Phase, Is.Zero);
            var command = game.Transactions.Command(TransactionKind.OperateProducer, "player", crop.Id);
            command.duration = 1;
            game.Transactions.Execute(command);
            var expected = ReadRuntimeView().stations.Find(x => x.id == crop.Id).progress;
            Assert.That(crop.Phase, Is.EqualTo(expected.phase));
            Assert.That(crop.Action, Is.EqualTo(expected.action));
            Assert.That(expected.phase != 0 || expected.action > 0, Is.True);
        }
    }
}
