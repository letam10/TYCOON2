using System.Linq;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class RepairRedesignTests
    {
        static TransactionCommand Cmd(TransactionCore core,TransactionKind kind,string key,string actor="player",double delta=0)=>new(){kind=kind,key=key,effectId="effect:"+key,actor=actor,target="mill",expectedRevision=core.Revision,duration=delta,quantity=20};
        static TransactionState Seed(int speed=1)
        {
            var s=TransactionCoreTests.Seed();s.stations[0].area="processing";
            s.unlocked.Add("repair_processing");s.crews.Add(new(){id="repair_processing",role="Repairer",area="processing",speedLevel=speed});
            s.owners.Single(x=>x.id=="worker").worker=new(){id="tech",upgrade="repair_processing"};return s;
        }
        [TestCase(1,30)] [TestCase(2,20)] [TestCase(3,10)]
        public void SpecialistUsesExactTimeAndPaysOneFee(int level,int seconds)
        {
            double now=100;var core=new TransactionCore(Seed(level),null,()=>now);
            core.Execute(Cmd(core,TransactionKind.BreakMachine,"break","simulation"));
            core.Execute(Cmd(core,TransactionKind.RepairMachine,"half","worker",seconds/2.0));
            var saved=core.Snapshot();Assert.That(saved.stations[0].progress.repairProgress,Is.EqualTo(.5).Within(.001));
            now+=1;core=new TransactionCore(saved,null,()=>now);var finish=Cmd(core,TransactionKind.RepairMachine,"finish","worker",seconds/2.0);
            core.Execute(finish);core.Execute(finish);Assert.That(core.Snapshot().money,Is.EqualTo(580));Assert.That(core.Snapshot().stations[0].progress.broken,Is.False);
        }
        [Test] public void PlayerTakesOverWithoutResetAndOwnRepairTakesFiveSeconds()
        {
            double now=100;var core=new TransactionCore(Seed(),null,()=>now);core.Execute(Cmd(core,TransactionKind.BreakMachine,"break","simulation"));
            core.Execute(Cmd(core,TransactionKind.RepairMachine,"worker-half","worker",15));
            core.Execute(Cmd(core,TransactionKind.RepairMachine,"player-finish","player",2.5));
            Assert.That(core.Snapshot().money,Is.EqualTo(580));Assert.That(core.Snapshot().stations[0].progress.broken,Is.False);
            core.Execute(Cmd(core,TransactionKind.BreakMachine,"break2","simulation"));core.Execute(Cmd(core,TransactionKind.RepairMachine,"player-only","player",5));
            Assert.That(core.Snapshot().stations[0].progress.playerRepairCount,Is.EqualTo(1));
        }
        [Test] public void WorkerOfAnotherRoleCannotRepair()
        {
            var s=Seed();s.crews.Clear();s.unlocked.Remove("repair_processing");s.unlocked.Add("processor");s.crews.Add(new(){id="processor",role="Processor",area="processing"});s.owners.Single(x=>x.id=="worker").worker.upgrade="processor";
            var core=new TransactionCore(s,null,()=>100);core.Execute(Cmd(core,TransactionKind.BreakMachine,"break","simulation"));
            Assert.That(Assert.Throws<TransactionRejectedException>(()=>core.Execute(Cmd(core,TransactionKind.RepairMachine,"wrong","worker",30))).Code,Is.EqualTo("authority"));
            Assert.That(core.Snapshot().money,Is.EqualTo(600));
        }
    }
}
