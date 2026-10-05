using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Stage12Tests
    {
        [Test] public void BakeryAndRestaurantGatesAndPlayerTrainingUseTheirOwnProgress()
        {
            var bakeryFixture=Stage11To13Fixtures.Read<ThresholdFixture>("stage12-near-bakery.json");
            var state=TransactionCoreTests.Seed();state.money=bakeryFixture.finalContribution;
            state.purchases.Add(new PurchaseRuntimeState{id="bakery",definitionId="bakery",contributed=bakeryFixture.contributed});
            Assert.That(Definitions.Upgrade("bakery").cost,Is.EqualTo(bakeryFixture.cost));
            Assert.That(ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("bakery")),Has.Some.Contains("Supermarket"));
            state.unlocked.AddRange(new[]{"supermarket","farm_shop","mill","milk_line","egg_line","bakery"});
            Stage11To13Fixtures.Producer(state,"wheat","farm_shop");
            Stage11To13Fixtures.Producer(state,"milk","milk_line");
            Stage11To13Fixtures.Producer(state,"egg","egg_line");
            Stage11To13Fixtures.Machine(state,"oven","bakery",area:"bakery");
            Stage11To13Fixtures.Machine(state,"cakeoven","bakery",area:"bakery");
            Assert.That(ProgressionTracker.CanProduce(state,"bread"),Is.True);
            Assert.That(ProgressionTracker.CanProduce(state,"cake"),Is.True);

            var bakeryPurchase=TransactionCoreTests.Seed();bakeryPurchase.money=bakeryFixture.finalContribution;bakeryPurchase.unlocked.Add("supermarket");
            bakeryPurchase.purchases.Add(new PurchaseRuntimeState{id="bakery",definitionId="bakery",contributed=bakeryFixture.contributed});
            var bakeryCore=new TransactionCore(bakeryPurchase,null,()=>100);
            var bakeryContribution=new TransactionCommand{kind=TransactionKind.ContributePurchase,actor="player",target="bakery",key="stage12-bakery-top-up",
                effectId="stage12-bakery-top-up-effect",expectedRevision=bakeryCore.Revision,quantity=bakeryFixture.finalContribution};
            Assert.That(bakeryCore.Execute(bakeryContribution).amount,Is.EqualTo(bakeryFixture.finalContribution));
            var bakeryCompletion=new TransactionCommand{kind=TransactionKind.CompletePurchase,actor="player",target="bakery",key="stage12-bakery-complete",
                effectId="grant:bakery",expectedRevision=bakeryCore.Revision};
            bakeryCore.Execute(bakeryCompletion);Assert.That(bakeryCore.Snapshot().unlocked,Does.Contain("bakery"));

            var restaurantFixture=Stage11To13Fixtures.Read<RestaurantFixture>("stage12-near-restaurant.json");
            state.unlocked.Add("oven_level2");
            state.stations.Add(new StationRuntimeState{id="checkout_bakery",kind="counter",area="bakery"});
            for(int i=0;i<restaurantFixture.successfulBakeryOrdersBefore;i++)state.payments.Add(new PaymentState{id="p"+i,order="o"+i,counter="checkout_bakery",amount=90});
            var restaurant=Definitions.Upgrade("restaurant");
            Assert.That(restaurant.cost,Is.EqualTo(restaurantFixture.cost));
            Assert.That(restaurantFixture.contributed+restaurantFixture.finalContribution,Is.EqualTo(restaurant.cost));
            var missing=ProgressionTracker.MissingRequirements(state,restaurant);
            Assert.That(missing,Has.Some.Contains("59/60"));
            state.payments.Add(new PaymentState{id="p59",order="o59",counter="checkout_bakery",amount=90});
            Assert.That(ProgressionTracker.MissingRequirements(state,restaurant),Has.Some.Contains("2/3"));
            state.unlocked.Add("oven_level3");
            Assert.That(ProgressionTracker.MissingRequirements(state,restaurant),Is.Empty);

            var restaurantPurchase=BuildRestaurantPurchase(restaurantFixture);
            var restaurantCore=new TransactionCore(restaurantPurchase,null,()=>100);
            var restaurantContribution=new TransactionCommand{kind=TransactionKind.ContributePurchase,actor="player",target="restaurant",key="stage12-restaurant-top-up",
                effectId="stage12-restaurant-top-up-effect",expectedRevision=restaurantCore.Revision,quantity=restaurantFixture.finalContribution};
            Assert.That(restaurantCore.Execute(restaurantContribution).amount,Is.EqualTo(restaurantFixture.finalContribution));
            var restaurantCompletion=new TransactionCommand{kind=TransactionKind.CompletePurchase,actor="player",target="restaurant",key="stage12-restaurant-complete",
                effectId="grant:restaurant",expectedRevision=restaurantCore.Revision};
            restaurantCore.Execute(restaurantCompletion);Assert.That(restaurantCore.Snapshot().unlocked,Does.Contain("restaurant"));

            state.unlocked.AddRange(new[]{"restaurant","kitchen_level2","kitchen_level3"});
            Stage11To13Fixtures.Machine(state,"kitchen","restaurant",area:"restaurant",playerJobs:30);
            state.stations.Add(new StationRuntimeState{id="table_0",kind="table",area="restaurant",playerWorkCount=30});
            state.stations.Add(new StationRuntimeState{id="storage_restaurant",kind="storage",area="restaurant"});
            var cook=Definitions.Upgrade("cook");
            Assert.That(ProgressionTracker.MissingRequirements(state,cook),Has.Some.Contains("tự phục vụ"));
            Assert.That(ProgressionTracker.MissingRequirements(state,cook),Has.Some.Contains("tự dọn"));
            var table=state.stations.Single(x=>x.id=="table_0");table.progress.playerServeCount=1;table.progress.playerCleanCount=1;
            Assert.That(ProgressionTracker.MissingRequirements(state,cook),Is.Empty);
            Assert.That(ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("waiter")),Is.Empty);
        }

        static TransactionState BuildRestaurantPurchase(RestaurantFixture fixture)
        {
            var state=TransactionCoreTests.Seed();state.money=fixture.finalContribution;
            state.unlocked.AddRange(new[]{"bakery","oven_level2","oven_level3"});
            state.purchases.Add(new PurchaseRuntimeState{id="restaurant",definitionId="restaurant",contributed=fixture.contributed});
            state.owners.Add(new OwnerState{id="checkout_bakery",kind=OwnerKind.Counter,actor="simulation",location="location:checkout_bakery",capacity=30,writers=new(){"player","worker"}});
            state.stations.Add(new StationRuntimeState{id="checkout_bakery",kind="counter",area="bakery",input="checkout_bakery",output="checkout_bakery"});
            for(int i=0;i<fixture.successfulBakeryOrdersAtThreshold;i++)
            {
                string order="order:bakery:"+i,customer="customer:bakery:"+i;
                state.owners.Add(new OwnerState{id=customer,kind=OwnerKind.Customer,actor=customer,location="location:"+customer,capacity=3,writers=new(){"player","worker"}});
                state.orders.Add(new OrderRuntimeState{id=order,customer=customer,counter="checkout_bakery",deadline=200,status=OrderStatus.Complete,
                    lines=new(){new OrderLine("bread",1,90){delivered=1}}});
                state.payments.Add(new PaymentState{id="payment:"+order,order=order,counter="checkout_bakery",amount=90});
            }
            return state;
        }

        [Serializable] sealed class ThresholdFixture { public int contributed,finalContribution,cost; }
        [Serializable] sealed class RestaurantFixture { public int successfulBakeryOrdersBefore,successfulBakeryOrdersAtThreshold,ovenLevelBefore,ovenLevelAtThreshold,contributed,finalContribution,cost; }
    }
}
