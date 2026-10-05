using System;
using System.Linq;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class CargoRedesignTests
    {
        static TransactionState Seed()
        {
            var s=TransactionCoreTests.Seed();s.unlocked.AddRange(new[]{"mill","farm_shop","storage_processing_level2","storage_processing_level3"});
            foreach(string area in new[]{"processing","farm_shop"})
            {
                string id="storage_"+area;s.owners.Add(new(){id=id,actor="simulation",location=area,kind=OwnerKind.Storage,capacity=72,writers=new(){"player","worker"}});
                s.stations.Add(new(){id=id,kind="storage",area=area,input=id,output=id,level=3});
            }
            return s;
        }
        static TransactionCommand C(TransactionCore core,TransactionKind kind,string key,string target=null)=>new(){kind=kind,actor="player",key=key,effectId="effect:"+key,expectedRevision=core.Revision,target=target};
        static void BuyTruck(TransactionCore core)
        {
            var pay=C(core,TransactionKind.ContributePurchase,"truck-pay","truck_bundle");pay.quantity=5000;core.Execute(pay);
            core.Execute(C(core,TransactionKind.CompletePurchase,"truck-buy","truck_bundle"));
        }
        [Test] public void SnapshotRoundTripBeforePurchaseDoesNotInventTruck()
        {var core=new TransactionCore(Seed());Assert.That(core.Snapshot().truck,Is.Null);Assert.That(new TransactionCore(core.Snapshot()).Snapshot().truck,Is.Null);}
        [Test] public void ThirtyHandJobsCountsBatchesNotIndividualTransfers()
        {
            var seed=Seed();for(int i=0;i<29;i++)seed.transportJobs.Add(new(){id="job"+i,kind="hand",area="processing",item="carrot",quantity=6});
            var core=new TransactionCore(seed,null,()=>100);
            Assert.That(ProgressionTracker.MissingRequirements(core.Snapshot(),Definitions.Upgrade("truck_bundle")),Has.Some.Contains("29/30"));
            var take=C(core,TransactionKind.Take,"take");take.source="storage";take.destination="player";take.item="carrot";take.quantity=6;core.Execute(take);
            var drop=C(core,TransactionKind.Place,"drop1");drop.source="player";drop.destination="storage_processing";drop.item="carrot";drop.quantity=3;core.Execute(drop);
            Assert.That(core.Snapshot().transportJobs.Count,Is.EqualTo(29));
            drop=C(core,TransactionKind.Place,"drop2");drop.source="player";drop.destination="storage_processing";drop.item="carrot";drop.quantity=3;core.Execute(drop);core.Execute(drop);
            var state=core.Snapshot();Assert.That(state.transportJobs.Count,Is.EqualTo(30));Assert.That(state.transportJobs.Last().quantity,Is.EqualTo(6));
            Assert.That(ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("truck_bundle")),Is.Empty);
        }
        [Test] public void TruckCargoReservesDestinationAndReloadCannotCloneItems()
        {
            var seed=Seed();seed.money=5000;for(int i=0;i<30;i++)seed.transportJobs.Add(new(){id="job"+i,kind="hand",area="processing",item="carrot",quantity=1});
            var core=new TransactionCore(seed,null,()=>100);BuyTruck(core);Assert.That(core.Snapshot().crews.Single(x=>x.id=="truck_bundle").role,Is.EqualTo("Driver"));
            var take=C(core,TransactionKind.Take,"take");take.source="storage";take.destination="player";take.item="carrot";take.quantity=6;core.Execute(take);
            var pack=C(core,TransactionKind.PackCrate,"pack","box");pack.source="player";pack.item="carrot";pack.quantity=6;core.Execute(pack);core.Execute(pack);
            var load=C(core,TransactionKind.LoadCrate,"load","box");core.Execute(load);
            var dispatch=C(core,TransactionKind.DispatchTruck,"send");dispatch.path=new(){new(0,0),new(20,0)};core.Execute(dispatch);core.Execute(dispatch);
            Assert.That(core.Snapshot().reservations.Single(x=>x.id.StartsWith("cargo-reserve:")).quantity,Is.EqualTo(6));
            var tick=C(core,TransactionKind.TickTruck,"move-half");tick.actor="simulation";tick.secondary="effect:send";tick.duration=2.5;core.Execute(tick);
            var saved=core.Snapshot();Assert.That(saved.truck.travelled,Is.EqualTo(10));
            for(int i=0;i<3;i++){core=new TransactionCore(saved,null,()=>100);Assert.That(core.Snapshot().crates.Count,Is.EqualTo(1));}
            tick=C(core,TransactionKind.TickTruck,"move-end");tick.actor="simulation";tick.secondary="effect:send";tick.duration=2.5;core.Execute(tick);
            var unload=C(core,TransactionKind.UnloadCrate,"unload","box");core.Execute(unload);core.Execute(unload);
            var result=core.Snapshot();Assert.That(result.stacks.Where(x=>x.owner=="storage_farm_shop"&&x.item=="carrot").Sum(x=>x.quantity),Is.EqualTo(6));
            Assert.That(result.stacks.Where(x=>x.item=="carrot").Sum(x=>x.quantity),Is.EqualTo(10));Assert.That(result.crates,Is.Empty);Assert.That(result.truck.completedTrips,Is.EqualTo(1));TransactionCore.Validate(result);
        }
        [Test] public void LoaderClaimCannotBeExecutedByTwoWorkersOrWrongArea()
        {
            var seed=Seed();seed.unlocked.AddRange(new[]{"truck_bundle","loader_processing"});seed.crews.Add(new(){id="truck_bundle",role="Driver",area="processing"});seed.crews.Add(new(){id="loader_processing",role="Loader",area="processing"});
            var worker=seed.owners.First(x=>x.id=="worker");worker.worker=new(){id="loader:0",upgrade="loader_processing"};worker.capacity=6;
            seed.owners.Add(new(){id="worker2",actor="worker2",location="worker2",kind=OwnerKind.Worker,capacity=6,singleItem=true,worker=new(){id="loader:1",upgrade="loader_processing"}});
            var core=new TransactionCore(seed,null,()=>100);
            var take=C(core,TransactionKind.Take,"take");take.source="storage";take.destination="player";take.item="carrot";take.quantity=6;core.Execute(take);
            var pack=C(core,TransactionKind.PackCrate,"pack","box");pack.source="player";pack.item="carrot";pack.quantity=6;core.Execute(pack);
            var claim=C(core,TransactionKind.ClaimCrate,"claim","box");claim.actor="worker";core.Execute(claim);core.Execute(claim);
            claim=C(core,TransactionKind.ClaimCrate,"claim2","box");claim.actor="worker2";Assert.Throws<TransactionRejectedException>(()=>core.Execute(claim));
            var load=C(core,TransactionKind.LoadCrate,"load","box");load.actor="worker";core.Execute(load);core.Execute(load);
            Assert.That(core.Snapshot().crates.Single().holder,Is.EqualTo(core.Snapshot().truck.id));Assert.That(core.Snapshot().stacks.Single(x=>x.owner=="box").quantity,Is.EqualTo(6));
        }
        [Test] public void FullDestinationRejectsDispatchWithoutLosingCargo()
        {
            var seed=Seed();seed.unlocked.Add("truck_bundle");seed.crews.Add(new(){id="truck_bundle",role="Driver",area="processing"});
            seed.owners.Single(x=>x.id=="storage_farm_shop").capacity=1;
            var core=new TransactionCore(seed,null,()=>100);
            var take=C(core,TransactionKind.Take,"take");take.source="storage";take.destination="player";take.item="carrot";take.quantity=6;core.Execute(take);
            var pack=C(core,TransactionKind.PackCrate,"pack","box");pack.source="player";pack.item="carrot";pack.quantity=6;core.Execute(pack);
            core.Execute(C(core,TransactionKind.LoadCrate,"load","box"));var send=C(core,TransactionKind.DispatchTruck,"send");send.path=new(){new(0,0),new(20,0)};
            Assert.That(Assert.Throws<TransactionRejectedException>(()=>core.Execute(send)).Code,Is.EqualTo("capacity"));
            Assert.That(core.Snapshot().truck.phase,Is.EqualTo("Loading"));Assert.That(core.Snapshot().stacks.Single(x=>x.owner=="box").quantity,Is.EqualTo(6));
        }
    }
}
