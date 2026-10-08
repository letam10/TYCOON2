using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        [TestCase("farmer", 1, 18)]
        [TestCase("farmer", 2, 30)]
        [TestCase("farmer", 3, 48)]
        [TestCase("loader_processing", 1, 18)]
        [TestCase("loader_processing", 2, 36)]
        [TestCase("loader_processing", 3, 54)]
        public void NewlyBoundWorkerUsesPhysicalCapacityBeforeFirstUpdate(string id, int level, int expected)
        {
            var state = game.Transactions.Snapshot();
            var crew = GameSession.CrewFor(Definitions.Upgrade(id));
            crew.carryLevel = level;
            state.crews.RemoveAll(x => x.id == id);
            state.crews.Add(crew);
            if (!state.unlocked.Contains(id)) state.unlocked.Add(id);
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            var worker = Add<WorkerAgent>("PhysicalCapacityWorker");
            worker.WorkerId = "physical-capacity";
            worker.UpgradeId = id;
            game.Transactions.BindWorker(worker);
            Assert.That(worker.Carry.Capacity, Is.EqualTo(expected));
            Assert.DoesNotThrow(() => worker.Carry.Capacity = CarryLimits.Worker(crew));
        }
    }
}
