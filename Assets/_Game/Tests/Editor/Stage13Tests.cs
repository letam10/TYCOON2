using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Stage13Tests
    {
        GameObject root;GameSession game;

        [SetUp] public void SetUp()
        {
            root=new GameObject("Stage13 test session");game=root.AddComponent<GameSession>();GameSession.Instance=game;
        }

        [TearDown] public void TearDown()
        {
            GameSession.Instance=null;if(root)UnityEngine.Object.DestroyImmediate(root);
        }

        [Test] public void RushRequiresCrewAndBreakdownWarningAndRepairResumeAreSaved()
        {
            var fixture=Stage11To13Fixtures.Read<EventFixture>("stage13-events-seed.json");
            game.Events.untilRush=0;game.TickEvents(.1f);
            Assert.That(game.Events.warning,Is.Zero,"Rush must wait until an employee team exists.");
            game.CrewStates.Add(new CrewState{id="restocker_market",role="Restocker",area="supermarket"});
            game.TickEvents(.1f);Assert.That(game.Events.warning,Is.EqualTo(fixture.warningSeconds));
            game.TickEvents(fixture.warningSeconds);Assert.That(game.Events.rushRemaining,Is.EqualTo(90));
            game.TickEvents(10);Assert.That(game.Events.rushRemaining,Is.EqualTo(80).Within(.001f));
            var commerce=new GameObject("CommerceDirector test");commerce.transform.SetParent(root.transform);
            Assert.That(commerce.AddComponent<CommerceDirector>().MaximumActive,Is.EqualTo(30));

            game.Economy=new Economy(200);game.Economy.Unlock("mill");
            var machineObject=new GameObject("machine_mill");machineObject.transform.SetParent(root.transform);
            var machine=machineObject.AddComponent<MachineStation>();machine.Id="machine_mill";machine.Label="Mill";machine.Requirement="mill";
            machine.Recipe=Definitions.Recipe("mill");machine.Inventory=new Inventory(12);machine.Level=2;machine.Running=true;machine.Remaining=4;
            machine.Batches=fixture.batchesBeforeBreakdownWarning;game.Machines.Add(machine);
            game.TickEvents(.1f);Assert.That(game.Events.breakdownWarning,Is.Zero);
            machine.Batches=fixture.batchesAtBreakdownWarning;game.TickEvents(.1f);
            Assert.That(game.Events.breakdownMachine,Is.EqualTo(machine.Id));
            machine.EndInteraction(default);game.TickEvents(fixture.warningSeconds);
            Assert.That(machine.Broken,Is.True);Assert.That(machine.Running,Is.True);Assert.That(machine.Remaining,Is.EqualTo(4));
            Assert.That(machine.RepairRemaining,Is.EqualTo(fixture.repairSeconds));
            Assert.That(machine.RepairFee,Is.InRange(fixture.minimumRepairFee,fixture.maximumRepairFee));
            Assert.That(StationPlayerActions.Supports(machine,InteractionKind.Repair),Is.True);
            var repairZone=new GameObject("RepairZone").AddComponent<StationZone>();repairZone.Mode="repair";
            Assert.That(repairZone.Kind,Is.EqualTo(InteractionKind.Repair));
            Assert.That(machine.Work(new Inventory(6),1,default),Is.False,"Generic operation must not repair a broken machine.");

            var context=new InteractionContext(new Inventory(6),default,"");
            Assert.That(StationPlayerActions.Execute(machine,InteractionKind.Repair,context,2).Worked,Is.True);
            Assert.That(game.Economy.Money,Is.EqualTo(160));
            float remaining=machine.RepairRemaining;machine.EndInteraction(default);
            Assert.That(machine.RepairRemaining,Is.EqualTo(remaining));
            for(int i=0;i<3;i++)StationPlayerActions.Execute(machine,InteractionKind.Repair,context,2);
            Assert.That(machine.Broken,Is.False);Assert.That(game.Economy.Money,Is.EqualTo(160));
            Assert.That(machine.Running,Is.True);Assert.That(machine.Remaining,Is.EqualTo(4));

            string directory=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"work","stage13-save-test");
            string path=Path.Combine(directory,"save-v2.json");
            try
            {
                game.Events.breakdownMachine=machine.Id;game.Events.breakdownWarning=7;game.Events.rushRemaining=23;
                SaveStore.Write(path,new SaveData{events=game.Events});var loaded=SaveStore.Read(path);
                Assert.That(loaded.events.breakdownMachine,Is.EqualTo(machine.Id));
                Assert.That(loaded.events.breakdownWarning,Is.EqualTo(7));Assert.That(loaded.events.rushRemaining,Is.EqualTo(23));
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }

        [Test] public void TransactionalRepairChargesOnceAndKeepsRunningMachineProgressAcrossReload()
        {
            var state=TransactionCoreTests.Seed();state.money=200;
            state.owners.Add(new OwnerState{id="cheese-input",kind=OwnerKind.Machine,actor="simulation",location="location:cheese-input",capacity=24,writers=new(){"player","worker"}});
            state.owners.Add(new OwnerState{id="cheese-output",kind=OwnerKind.Machine,actor="simulation",location="location:cheese-output",capacity=24,writers=new(){"player","worker"}});
            state.stations.Add(new StationRuntimeState{id="machine_cheesemaker",kind="machine",definitionId="cheesemaker",input="cheese-input",output="cheese-output"});
            var core=new TransactionCore(state,null,()=>100);
            var start=CoreCommand(core,TransactionKind.StartMachine,"player","machine-start","mill");start.secondary="repair-test";core.Execute(start);
            var running=core.Snapshot().stations.Single(x=>x.id=="mill");Assert.That(running.remaining,Is.EqualTo(6));
            var breakdown=CoreCommand(core,TransactionKind.BreakMachine,"simulation","machine-break","mill");breakdown.quantity=40;core.Execute(breakdown);
            var secondBreak=CoreCommand(core,TransactionKind.BreakMachine,"simulation","machine-break-second","machine_cheesemaker");secondBreak.quantity=20;
            Assert.That(Assert.Throws<TransactionRejectedException>(()=>core.Execute(secondBreak)).Code,Is.EqualTo("breakdown"));

            var repair=CoreCommand(core,TransactionKind.RepairMachine,"player","repair-one","mill");repair.duration=3;core.Execute(repair);
            var mid=core.Snapshot();Assert.That(mid.money,Is.EqualTo(160));
            Assert.That(mid.stations.Single(x=>x.id=="mill").progress.repairRemaining,Is.EqualTo(2).Within(.001));
            Assert.That(mid.stations.Single(x=>x.id=="mill").remaining,Is.EqualTo(6));
            core=new TransactionCore(mid,null,()=>100);
            repair=CoreCommand(core,TransactionKind.RepairMachine,"player","repair-finish","mill");repair.duration=2;core.Execute(repair);
            var resumed=core.Snapshot();var machine=resumed.stations.Single(x=>x.id=="mill");
            Assert.That(resumed.money,Is.EqualTo(160));Assert.That(machine.progress.broken,Is.False);
            Assert.That(machine.running,Is.True);Assert.That(machine.remaining,Is.EqualTo(6));
            Assert.That(resumed.stacks.Single(x=>x.owner.StartsWith("escrow:")&&x.item=="wheat").quantity,Is.EqualTo(4));
        }

        static TransactionCommand CoreCommand(TransactionCore core,TransactionKind kind,string actor,string key,string target)
            =>new(){kind=kind,actor=actor,key=key,effectId="effect:"+key,target=target,expectedRevision=core.Revision};

        [Serializable] sealed class EventFixture
        { public int batchesBeforeBreakdownWarning,batchesAtBreakdownWarning,warningSeconds,repairSeconds,minimumRepairFee,maximumRepairFee; }
    }
}
