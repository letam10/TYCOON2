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
            var core=new TransactionCore(state,null,()=>100);
            var contribute=new TransactionCommand{kind=TransactionKind.ContributePurchase,actor="player",target="farm_shop",
                key="stage09-top-up",effectId="stage09-top-up-effect",expectedRevision=core.Revision,quantity=fixture.finalContribution};
            Assert.That(core.Execute(contribute).amount,Is.EqualTo(fixture.finalContribution));
            var complete=new TransactionCommand{kind=TransactionKind.CompletePurchase,actor="player",target="farm_shop",
                key="stage09-complete",effectId="grant:farm_shop",expectedRevision=core.Revision};
            core.Execute(complete);
            Assert.That(core.Snapshot().unlocked,Does.Contain("farm_shop"));
            Assert.That(core.Snapshot().purchases.Single(x=>x.id=="farm_shop").complete,Is.True);
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
