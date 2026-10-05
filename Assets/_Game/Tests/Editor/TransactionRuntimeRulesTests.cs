using System.Linq;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed partial class TransactionCoreTests
    {
        [Test]public void CrewUpgradesAreIsolatedByRoleAndArea()
        {
            var seed=Seed();seed.money=1000;seed.unlocked.Add("farmer");seed.unlocked.Add("animal_worker");
            seed.crews.Add(GameSession.CrewFor(Definitions.Upgrade("farmer")));
            seed.crews.Add(GameSession.CrewFor(Definitions.Upgrade("animal_worker")));
            var core=Core(seed);
            var speed=Command(core,TransactionKind.UpgradeCrew,"farmer-speed","farmer");speed.secondary="speed";core.Execute(speed);
            var count=Command(core,TransactionKind.UpgradeCrew,"animal-count","animal_worker");count.secondary="count";core.Execute(count);
            var crews=core.Snapshot().crews;
            Assert.That(crews.Single(x=>x.id=="farmer").speedLevel,Is.EqualTo(2));
            Assert.That(crews.Single(x=>x.id=="farmer").count,Is.EqualTo(1));
            Assert.That(crews.Single(x=>x.id=="animal_worker").speedLevel,Is.EqualTo(1));
            Assert.That(crews.Single(x=>x.id=="animal_worker").count,Is.EqualTo(2));
        }
        [Test] public void RelayReservationFollowsWorkerAndHoldsFinalCapacityUntilPlacement()
        {
            var core=Core();var reserve=Command(core,TransactionKind.Reserve,"route","route:1");reserve.actor="worker";
            reserve.destination="counter";reserve.secondary="worker";reserve.quantity=4;reserve.expiresAt=190;core.Execute(reserve);
            var take=Command(core,TransactionKind.Take,"take-route");take.actor="worker";take.destination="worker";take.quantity=4;take.reservation="route:1";
            core.Execute(take);core.Execute(take);
            var state=core.Snapshot();Assert.That(state.reservations.Single().source,Is.EqualTo("worker"));
            Assert.That(state.reservations.Single().status,Is.EqualTo(ReservationStatus.Active));Assert.That(Count(core,"storage"),Is.EqualTo(6));Assert.That(Count(core,"worker"),Is.EqualTo(4));
            var place=Command(core,TransactionKind.Place,"place-route");place.actor="worker";place.source="worker";place.destination="counter";place.quantity=4;place.reservation="route:1";
            core.Execute(place);core.Execute(place);Assert.That(Count(core,"worker"),Is.Zero);Assert.That(Count(core,"counter"),Is.EqualTo(4));
            Assert.That(core.Snapshot().reservations.Single().status,Is.EqualTo(ReservationStatus.Used));Journal(core,3);
        }
        [Test]public void CarryUpgradeChainChangesOwnerCapacitySixTenSixteenTwentyFour()
        {
            var seed=Seed();seed.money=5000;seed.owners.Find(x=>x.id=="player").singleItem=true;seed.stacks.Find(x=>x.id=="stock").quantity=24;var core=Core(seed);
            Assert.That(core.Snapshot().owners.Find(x=>x.id=="player").capacity,Is.EqualTo(6));
            foreach(var row in new[]{("carry10",10),("carry16",16),("carry24",24)})
            {
                var contribute=Command(core,TransactionKind.ContributePurchase,"contribute:"+row.Item1,row.Item1);contribute.quantity=Definitions.Upgrade(row.Item1).cost;
                core.Execute(contribute);core.Execute(contribute);var complete=Command(core,TransactionKind.CompletePurchase,"grant:"+row.Item1,row.Item1);core.Execute(complete);core.Execute(complete);
                Assert.That(core.Snapshot().owners.Find(x=>x.id=="player").capacity,Is.EqualTo(row.Item2));
            }
            var take=Command(core,TransactionKind.Take,"carry24");take.quantity=24;core.Execute(take);
            Assert.That(Count(core,"player"),Is.EqualTo(24));Assert.That(core.Snapshot().money,Is.EqualTo(1800));Journal(core,7);
        }
        [Test]public void MachineIngredientsHaveEscrowOwnerThroughoutInterruptedProcessing()
        {
            var core=Core();var start=Command(core,TransactionKind.StartMachine,"start-escrow","mill");start.secondary="job-escrow";core.Execute(start);
            var state=core.Snapshot();var machine=state.stations.Single();Assert.That(state.owners.Single(x=>x.id==machine.escrow).kind,Is.EqualTo(OwnerKind.Escrow));
            Assert.That(state.stacks.Where(x=>x.owner==machine.escrow).Sum(x=>x.quantity),Is.EqualTo(4));
            Assert.That(state.stacks.Sum(x=>x.quantity),Is.EqualTo(18));
            var advance=Command(core,TransactionKind.AdvanceMachine,"advance-escrow","mill");advance.secondary=machine.jobId;advance.duration=6;core.Execute(advance);
            var finish=Command(core,TransactionKind.CompleteMachine,"finish-escrow","mill");finish.secondary=machine.jobId;core.Execute(finish);core.Execute(finish);
            Assert.That(core.Snapshot().stacks.Any(x=>x.owner==machine.escrow),Is.False);Assert.That(Count(core,"mill-output","flour"),Is.EqualTo(3));Journal(core,3);
        }
    }
}
