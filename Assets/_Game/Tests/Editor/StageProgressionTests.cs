using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class StageProgressionTests
    {
        [TestCase("stage09-crop-near-farm-shop.json")]
        [TestCase("stage09-livestock-near-farm-shop.json")]
        public void FarmShopCanOpenFromEitherLevelThreeRouteAtTheExactThreshold(string file)
        {
            var fixture=Read(file);var state=TransactionCoreTests.Seed();
            state.money=fixture.finalContribution;state.legacyTransactions=fixture.ordersBefore;
            if(fixture.stationFamily=="farm")state.unlocked.AddRange(new[]{"farm_level2","farm_level3"});
            else state.unlocked.AddRange(new[]{"milk_line","animal_level2","animal_level3"});
            state.purchases.Add(new PurchaseRuntimeState{id="farm_shop",definitionId="farm_shop",contributed=fixture.contributed});
            var upgrade=Definitions.Upgrade("farm_shop");
            Assert.That(ProgressionTracker.MissingRequirements(state,upgrade),Has.Some.Contains(fixture.ordersBefore+"/50"));
            Assert.That(state.money,Is.EqualTo(fixture.finalContribution));

            state.legacyTransactions=fixture.ordersAtThreshold;
            Assert.That(ProgressionTracker.MissingRequirements(state,upgrade),Is.Empty);
            state.unlocked=state.unlocked.Distinct().ToList();var core=new TransactionCore(state,null,()=>100);
            var contribute=new TransactionCommand{kind=TransactionKind.ContributePurchase,actor="player",target="farm_shop",
                key="stage09-top-up",effectId="stage09-top-up-effect",expectedRevision=core.Revision,quantity=fixture.finalContribution};
            Assert.That(core.Execute(contribute).amount,Is.EqualTo(fixture.finalContribution));
            var complete=new TransactionCommand{kind=TransactionKind.CompletePurchase,actor="player",target="farm_shop",
                key="stage09-complete",effectId="grant:farm_shop",expectedRevision=core.Revision};
            core.Execute(complete);
            Assert.That(core.Snapshot().unlocked,Does.Contain("farm_shop"));
            Assert.That(core.Snapshot().purchases.Single(x=>x.id=="farm_shop").complete,Is.True);
        }

        [Test] public void ProcessingUnlockRequiresFarmShopAnimalLevelThreeAndTwoHundredOrders()
        {
            var fixture=Read("stage10-near-processing.json");var state=TransactionCoreTests.Seed();
            state.money=fixture.finalContribution;state.legacyTransactions=fixture.ordersBefore;
            state.unlocked.AddRange(new[]{"farm_shop","milk_line","animal_level2","animal_level3"});
            state.purchases.Add(new PurchaseRuntimeState{id="mill",definitionId="mill",contributed=fixture.contributed});
            var upgrade=Definitions.Upgrade("mill");
            Assert.That(ProgressionTracker.MissingRequirements(state,upgrade),Has.Some.Contains(fixture.ordersBefore+"/200"));
            Assert.That(ProgressionTracker.MissingRequirements(state,upgrade).Any(x=>x=="Cần mở Mở rộng Farm Shop."),Is.False);

            state.unlocked.Remove("farm_shop");state.legacyTransactions=fixture.ordersAtThreshold;
            Assert.That(ProgressionTracker.MissingRequirements(state,upgrade),Has.Some.Contains("Cần mở Mở rộng Farm Shop."));
            state.unlocked.Add("farm_shop");
            Assert.That(ProgressionTracker.MissingRequirements(state,upgrade),Is.Empty);
            state.unlocked=state.unlocked.Distinct().ToList();var core=new TransactionCore(state,null,()=>100);
            var contribute=new TransactionCommand{kind=TransactionKind.ContributePurchase,actor="player",target="mill",
                key="stage10-top-up",effectId="stage10-top-up-effect",expectedRevision=core.Revision,quantity=fixture.finalContribution};
            Assert.That(core.Execute(contribute).amount,Is.EqualTo(fixture.finalContribution));
            core.Execute(new TransactionCommand{kind=TransactionKind.CompletePurchase,actor="player",target="mill",
                key="stage10-complete",effectId="grant:mill",expectedRevision=core.Revision});
            Assert.That(core.Snapshot().unlocked,Does.Contain("mill"));
            Assert.That(Definitions.Recipe("mill").output,Is.EqualTo("flour"));
            Assert.That(Definitions.Recipe("cheesemaker").output,Is.EqualTo("cheese"));
            Assert.That(Definitions.Recipe("saucemaker").output,Is.EqualTo("sauce"));
        }

        [Test] public void ProcessingConveyorTransitSurvivesReloadAndDeliversExactlyOnce()
        {
            var seed=TransactionCoreTests.Seed();seed.stacks.Add(new ItemStackState{id="farm-wheat",owner="storage",location="location:storage",item="wheat",quantity=4});
            var core=new TransactionCore(seed,null,()=>100);
            core.Execute(new TransactionCommand{kind=TransactionKind.Reserve,actor="simulation",target="belt:stage10:0",key="stage10-belt-reserve",effectId="stage10-belt-reserve-effect",
                expectedRevision=core.Revision,source="storage",destination="mill-input",secondary="conveyor",item="wheat",quantity=4,expiresAt=double.MaxValue});
            core.Execute(new TransactionCommand{kind=TransactionKind.Transfer,actor="simulation",key="stage10-belt-load",effectId="stage10-belt-load-effect",
                expectedRevision=core.Revision,source="storage",destination="conveyor",item="wheat",quantity=4,reservation="belt:stage10:0"});
            var state=core.Snapshot();
            Assert.That(state.stacks.Single(x=>x.owner=="conveyor").quantity,Is.EqualTo(4));
            Assert.That(state.reservations.Single(x=>x.id=="belt:stage10:0").status,Is.EqualTo(ReservationStatus.Active));
            var save=new SaveData{transactionVersion=SaveStore.CurrentTransactionVersion,transactionState=state,money=state.money};
            save.cash.Add(new CashSave{id="counter"});
            string directory=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"work","stage10-save-tests",Guid.NewGuid().ToString("N"));
            string path=Path.Combine(directory,"save-v2.json");
            try
            {
                SaveStore.Write(path,save);var loaded=SaveStore.Read(path);var repeated=SaveStore.Read(path);
                Assert.That(repeated.transactionState.stacks.Single(x=>x.owner=="conveyor").quantity,Is.EqualTo(4));
                Assert.That(repeated.transactionState.reservations.Single(x=>x.id=="belt:stage10:0").source,Is.EqualTo("conveyor"));
                var resumed=new TransactionCore(loaded.transactionState,null,()=>100);
                var deliver=new TransactionCommand{kind=TransactionKind.Transfer,actor="simulation",key="stage10-belt-deliver",effectId="stage10-belt-deliver-effect",
                    expectedRevision=resumed.Revision,source="conveyor",destination="mill-input",item="wheat",quantity=4,reservation="belt:stage10:0"};
                var receipt=resumed.Execute(deliver);Assert.That(resumed.Execute(deliver).id,Is.EqualTo(receipt.id));
                Assert.That(resumed.Snapshot().stacks.Count(x=>x.owner=="conveyor"),Is.Zero);
                Assert.That(resumed.Snapshot().stacks.Where(x=>x.owner=="mill-input"&&x.item=="wheat").Sum(x=>x.quantity),Is.EqualTo(12));
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }

        static StageFixture Read(string file)=>JsonUtility.FromJson<StageFixture>(File.ReadAllText(Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,"mod","test",file)));
        [Serializable] sealed class StageFixture
        {
            public string stationFamily;
            public int ordersBefore,ordersAtThreshold,contributed,finalContribution;
        }
    }
}
