using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Stage11Tests
    {
        [Test] public void SupermarketUsesNearThresholdRequirementsAndOnlyUnlockedProductionRoutes()
        {
            var fixture=Stage11To13Fixtures.Read<ThresholdFixture>("stage11-near-supermarket.json");
            var state=TransactionCoreTests.Seed();state.money=fixture.finalContribution;state.legacyTransactions=fixture.successfulOrdersBefore;
            state.unlocked.AddRange(new[]{"farm_shop","mill","dairy","milk_line"});
            state.purchases.Add(new PurchaseRuntimeState{id="supermarket",definitionId="supermarket",contributed=fixture.contributed});
            state.stations.Single(x=>x.definitionId=="mill").batches=fixture.recipeBatchesBefore;
            Stage11To13Fixtures.Machine(state,"cheesemaker","dairy",fixture.recipeBatchesBefore);
            Stage11To13Fixtures.Machine(state,"saucemaker","dairy",fixture.recipeBatchesBefore);
            Stage11To13Fixtures.Producer(state,"wheat","farm_shop");
            Stage11To13Fixtures.Producer(state,"tomato","farm_shop");
            Stage11To13Fixtures.Producer(state,"milk","milk_line");

            var upgrade=Definitions.Upgrade("supermarket");
            var missing=ProgressionTracker.MissingRequirements(state,upgrade);
            Assert.That(upgrade.cost,Is.EqualTo(fixture.cost));
            Assert.That(missing,Has.Some.Contains("599/600"));
            Assert.That(missing.Count(x=>x.Contains("49/50")),Is.EqualTo(3));
            Assert.That(ProgressionTracker.CanProduce(state,"flour"),Is.True);
            Assert.That(ProgressionTracker.CanProduce(state,"cheese"),Is.True);
            Assert.That(ProgressionTracker.CanProduce(state,"sauce"),Is.True);
            Assert.That(ProgressionTracker.CanProduce(state,"bread"),Is.False);
            Assert.That(CustomerAgent.MaximumOrderItemTypes("market"),Is.EqualTo(2));

            state.legacyTransactions=fixture.successfulOrdersAtThreshold;
            state.stations.Where(x=>x.definitionId is "mill" or "cheesemaker" or "saucemaker").ToList()
                .ForEach(x=>x.batches=fixture.recipeBatchesAtThreshold);
            Assert.That(ProgressionTracker.MissingRequirements(state,upgrade),Is.Empty);
            Assert.That(state.money,Is.EqualTo(fixture.finalContribution));

            var purchaseState=TransactionCoreTests.Seed();purchaseState.money=fixture.finalContribution;purchaseState.legacyTransactions=fixture.successfulOrdersAtThreshold;
            purchaseState.unlocked.AddRange(new[]{"farm_shop","mill","dairy","milk_line"});
            purchaseState.purchases.Add(new PurchaseRuntimeState{id="supermarket",definitionId="supermarket",contributed=fixture.contributed});
            purchaseState.stations.Single(x=>x.definitionId=="mill").batches=fixture.recipeBatchesAtThreshold;
            AddMachine(purchaseState,"cheesemaker",fixture.recipeBatchesAtThreshold);
            AddMachine(purchaseState,"saucemaker",fixture.recipeBatchesAtThreshold);
            var core=new TransactionCore(purchaseState,null,()=>100);
            var contribute=new TransactionCommand{kind=TransactionKind.ContributePurchase,actor="player",target="supermarket",key="stage11-top-up",
                effectId="stage11-top-up-effect",expectedRevision=core.Revision,quantity=fixture.finalContribution};
            Assert.That(core.Execute(contribute).amount,Is.EqualTo(fixture.finalContribution));
            var complete=new TransactionCommand{kind=TransactionKind.CompletePurchase,actor="player",target="supermarket",key="stage11-complete",
                effectId="grant:supermarket",expectedRevision=core.Revision};
            var completion=core.Execute(complete);long revision=core.Revision;
            Assert.That(core.Execute(complete).id,Is.EqualTo(completion.id));
            Assert.That(core.Revision,Is.EqualTo(revision));Assert.That(core.Snapshot().unlocked,Does.Contain("supermarket"));
        }

        [Test] public void MultiItemOrderDeliveryCommitsAtomicallyAndRetryDoesNotDuplicate()
        {
            var state=TransactionCoreTests.Seed();
            state.stacks.Add(new ItemStackState{id="player-carrot",owner="player",location="location:player",item="carrot",quantity=1});
            state.stacks.Add(new ItemStackState{id="player-wheat",owner="player",location="location:player",item="wheat",quantity=1});
            var core=new TransactionCore(state,null,()=>100);
            core.Execute(new TransactionCommand{kind=TransactionKind.CreateOrder,actor="simulation",key="multi-order",effectId="multi-order-effect",
                expectedRevision=core.Revision,target="order:multi",source="counter",destination="customer",expiresAt=200,
                lines=new(){new OrderLine("carrot",1,10),new OrderLine("wheat",1,12)}});
            var deliver=new TransactionCommand{kind=TransactionKind.DeliverOrder,actor="player",key="multi-deliver",effectId="multi-deliver-effect",
                expectedRevision=core.Revision,target="order:multi",source="player",lines=new(){new OrderLine("carrot",1,0),new OrderLine("wheat",1,0)}};
            long revision=core.Revision;var receipt=core.Execute(deliver);
            Assert.That(core.Revision,Is.EqualTo(revision+1));
            Assert.That(core.Execute(deliver).id,Is.EqualTo(receipt.id));
            var snapshot=core.Snapshot();
            Assert.That(snapshot.stacks.Where(x=>x.owner=="customer").Sum(x=>x.quantity),Is.EqualTo(2));
            Assert.That(snapshot.orders.Single(x=>x.id=="order:multi").lines.All(x=>x.Remaining==0),Is.True);
            Assert.That(snapshot.payments,Is.Empty);
        }

        static void AddMachine(TransactionState state,string recipe,int batches)
        {
            string input=recipe+"-input",output=recipe+"-output";
            foreach(string id in new[]{input,output})state.owners.Add(new OwnerState{id=id,kind=OwnerKind.Machine,actor="simulation",location="location:"+id,capacity=24,writers=new(){"player","worker"}});
            state.stations.Add(new StationRuntimeState{id="machine_"+recipe,definitionId=recipe,kind="machine",requirement="dairy",
                area="processing",input=input,output=output,batches=batches});
        }

        [Serializable] sealed class ThresholdFixture
        { public int successfulOrdersBefore,successfulOrdersAtThreshold,recipeBatchesBefore,recipeBatchesAtThreshold,contributed,finalContribution,cost; }
    }
}
