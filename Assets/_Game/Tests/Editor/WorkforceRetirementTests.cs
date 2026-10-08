using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed partial class TransactionCoreTests
    {
        static TransactionState WorkforceSeed()
        {
            var seed = Seed();
            var crew = GameSession.CrewFor(Definitions.Upgrade("farmer"));
            crew.count = 3;
            crew.carryLevel = 3;
            seed.crews.Add(crew);
            seed.unlocked.Add(crew.id);
            seed.owners.Add(new OwnerState
            {
                id = "worker:farmer:2", actor = "worker:farmer:2", location = "worker:farmer:2",
                kind = OwnerKind.Worker, capacity = 16, singleItem = true,
                worker = new WorkerSave { id = "farmer:2", upgrade = "farmer", phase = 2 }
            });
            seed.owners.Find(x => x.id == "storage").writers.Add("worker:farmer:2");
            seed.stacks.Add(new ItemStackState
            {
                id = "retiring-cargo", owner = "worker:farmer:2", location = "worker:farmer:2",
                item = "carrot", quantity = 7
            });
            return seed;
        }

        [Test]
        public void MigrationPreservesCargoReservationProgressAndReceiptIdentity()
        {
            var core = Core(WorkforceSeed());
            var reserve = Command(core, TransactionKind.Reserve, "retiring-reserve", "retiring-r");
            reserve.actor = "worker:farmer:2";
            reserve.source = reserve.actor;
            reserve.destination = "storage";
            reserve.quantity = 7;
            reserve.expiresAt = 250;
            core.Execute(reserve);
            var state = core.Snapshot();
            state.crews[0].count = 3;
            state.stations[0].progress.action = .42f;
            var receipt = JsonUtility.ToJson(state.receipts[0]);
            var reservation = JsonUtility.ToJson(state.reservations[0]);
            var stacks = state.stacks.Select(x => JsonUtility.ToJson(x)).ToArray();
            var progress = JsonUtility.ToJson(state.stations[0]);
            Assert.That(WorkforceMigration.Apply(state), Is.True);
            Assert.That(WorkforceMigration.Apply(state), Is.False);
            Assert.That(state.crews[0].count, Is.EqualTo(2));
            var worker = state.owners.Find(x => x.id == "worker:farmer:2").worker;
            Assert.That(worker.retiring, Is.True);
            Assert.That(worker.retired, Is.False);
            Assert.That(WorkforceRules.PendingRetirements(state), Is.EqualTo(1));
            Assert.That(state.stacks.Select(x => JsonUtility.ToJson(x)), Is.EqualTo(stacks));
            Assert.That(JsonUtility.ToJson(state.receipts[0]), Is.EqualTo(receipt));
            Assert.That(JsonUtility.ToJson(state.reservations[0]), Is.EqualTo(reservation));
            Assert.That(JsonUtility.ToJson(state.stations[0]), Is.EqualTo(progress));
            var restored = JsonUtility.FromJson<TransactionState>(JsonUtility.ToJson(state));
            Assert.That(WorkforceMigration.Apply(restored), Is.False);
            Assert.That(WorkforceRules.PendingRetirements(restored), Is.EqualTo(1));
        }

        [Test]
        public void RetirementRejectsCargoThenDepositsAndCommitsOnceAcrossReload()
        {
            var seed = WorkforceSeed();
            WorkforceMigration.Apply(seed);
            var core = Core(seed);
            var retire = Command(core, WorkforceRules.RetireCommand, "retire", "worker:farmer:2");
            retire.actor = "simulation";
            Rejected(core, retire, "cargo");
            var drop = Command(core, TransactionKind.Place, "retiring-drop");
            drop.actor = "worker:farmer:2";
            drop.source = drop.actor;
            drop.destination = "storage";
            drop.quantity = 7;
            core.Execute(drop);
            int goods = core.Snapshot().stacks.Sum(x => x.quantity);
            retire.expectedRevision = core.Revision;
            var receipt = core.Execute(retire);
            Assert.That(core.Execute(retire).id, Is.EqualTo(receipt.id));
            core = Core(core.Snapshot());
            Assert.That(core.Execute(retire).id, Is.EqualTo(receipt.id));
            var after = core.Snapshot();
            Assert.That(after.owners.Find(x => x.id == "worker:farmer:2").worker.retired, Is.True);
            Assert.That(WorkforceRules.PendingRetirements(after), Is.Zero);
            Assert.That(after.stacks.Sum(x => x.quantity), Is.EqualTo(goods));
            Assert.That(Count(core, "storage"), Is.EqualTo(17));
        }

        [Test]
        public void EmptyWorkerWithLiveReservationCannotRetire()
        {
            var seed = WorkforceSeed();
            seed.stacks.RemoveAll(x => x.id == "retiring-cargo");
            var core = Core(seed);
            var reserve = Command(core, TransactionKind.Reserve, "pickup-reserve", "pickup-r");
            reserve.actor = "worker:farmer:2";
            reserve.destination = "worker:farmer:2";
            reserve.expiresAt = 250;
            core.Execute(reserve);
            var retire = Command(core, WorkforceRules.RetireCommand, "retire-blocked", "worker:farmer:2");
            retire.actor = "simulation";
            Rejected(core, retire, "cargo");
        }

        [Test]
        public void HeldCrateKeepsLoaderAliveEvenWithEmptyCarryInventory()
        {
            var state = WorkforceSeed();
            state.stacks.RemoveAll(x => x.id == "retiring-cargo");
            var loader = GameSession.CrewFor(Definitions.Upgrade("loader_farm"));
            loader.count = 3;
            state.crews.Clear();
            state.crews.Add(loader);
            state.crates.Add(new CargoCrateState { id = "held-crate", holder = "worker:farmer:2" });
            var owner = state.owners.Find(x => x.id == "worker:farmer:2");
            owner.worker.id = "loader_farm:2";
            owner.worker.upgrade = loader.id;
            Assert.That(WorkforceRules.HasObligations(state, owner), Is.True);
            WorkforceMigration.Apply(state);
            Assert.That(state.owners.Find(x => x.id == owner.id).worker.retired, Is.False);
        }

        [TestCase("farmer", 2)]
        [TestCase("restocker", 1)]
        public void CrewCountUpgradeStopsAtProfessionLimit(string id, int limit)
        {
            var seed = Seed();
            seed.money = 10000;
            var crew = GameSession.CrewFor(Definitions.Upgrade(id));
            crew.count = limit;
            seed.crews.Add(crew);
            seed.unlocked.Add(id);
            var core = Core(seed);
            var upgrade = Command(core, TransactionKind.UpgradeCrew, "crew-over-limit", id);
            upgrade.secondary = "count";
            Rejected(core, upgrade, "funds");
        }
    }
}
