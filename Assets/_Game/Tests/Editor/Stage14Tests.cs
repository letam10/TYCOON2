using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class Stage14Tests
    {
        [Serializable] sealed class Boundaries {public Purchase[] purchases;public int activeCustomers,partialRequested,partialDelivered,patienceSeconds;}
        [Serializable] sealed class Purchase {public string id;public int wallet,payment,cost;}
        static IEnumerable<string> PurchaseIds=>new[]{"barn","milk_line","farm_shop","mill","supermarket","bakery","restaurant"};
        static TransactionCommand Command(TransactionCore core,TransactionKind kind,string key,string target=null,string actor="player")=>
            new(){kind=kind,key=key,effectId="effect:"+key,actor=actor,target=target,expectedRevision=core.Revision};
        static TransactionState ReadySeed(string id,int wallet)
        {
            var s=TransactionCoreTests.Seed();s.money=wallet;s.legacyTransactions=600;
            s.unlocked.AddRange(new[]{"farm_level3","animal_level3","oven_level3","farm_shop","barn","milk_line","egg_line","mill","dairy","supermarket","bakery"}.Where(x=>x!=id));
            foreach(string recipe in new[]{"mill","cheesemaker","saucemaker"})
            {
                var machine=s.stations.FirstOrDefault(m=>m.definitionId==recipe);
                if(machine==null){machine=new StationRuntimeState{id=recipe,definitionId=recipe,input="mill-input",output="mill-output",area="processing"};s.stations.Add(machine);}
                machine.batches=50;
            }
            s.stations.Add(new StationRuntimeState{id="counter",kind="counter",area="bakery",input="counter",output="counter"});
            for(int i=1;i<=60;i++)
            {
                string order="order:"+i;
                s.orders.Add(new OrderRuntimeState{id=order,customer="customer",counter="counter",deadline=90,status=OrderStatus.Complete,lines=new(){new("carrot",1,10){delivered=1}}});
                s.payments.Add(new PaymentState{id="payment:"+order,order=order,counter="counter",amount=10,collected=true});
            }
            return s;
        }
        [TestCaseSource(nameof(PurchaseIds))]
        public void NearBoundaryRealPaymentThenPartialPurchaseIsIdempotent(string id)
        {
            var f=Stage11To13Fixtures.Read<Boundaries>("stage14-boundaries.json").purchases.Single(x=>x.id==id);
            var core=new TransactionCore(ReadySeed(id,f.wallet),null,()=>100,true);
            Assert.That(Definitions.Upgrade(id).cost,Is.EqualTo(f.cost));
            var contribute=Command(core,TransactionKind.ContributePurchase,"partial",id);contribute.quantity=f.wallet;
            core.Execute(contribute);Assert.That(core.Snapshot().money,Is.Zero);
            var order=Command(core,TransactionKind.CreateOrder,"create","order:61","simulation");order.source="counter";order.destination="customer";order.expiresAt=190;order.lines=new(){new("carrot",2,10)};core.Execute(order);
            var delivery=Command(core,TransactionKind.DeliverOrder,"deliver","order:61");delivery.source="storage";delivery.item="carrot";delivery.quantity=2;core.Execute(delivery);
            core.Execute(Command(core,TransactionKind.CompleteOrder,"complete","order:61"));core.Execute(Command(core,TransactionKind.CreatePayment,"payment","order:61"));
            Assert.That(core.Snapshot().money,Is.Zero);Assert.That(core.Snapshot().cashCollected,Is.Zero,"Tạo payment chưa phải tiền thực thu");
            var collect=Command(core,TransactionKind.CollectPayment,"collect","payment:order:61");core.Execute(collect);core.Execute(collect);
            var state=core.Snapshot();Assert.That(state.money,Is.EqualTo(f.payment));Assert.That(state.cashCollected,Is.EqualTo(f.payment));
            // Load nhiều lần vẫn tiếp tục phần tiền đã góp, giữ nguyên receipt/effect.
            for(int i=0;i<3;i++)core=new TransactionCore(core.Snapshot(),null,()=>100,true);
            var topup=Command(core,TransactionKind.ContributePurchase,"topup",id);topup.quantity=f.cost;core.Execute(topup);core.Execute(topup);
            var unlock=Command(core,TransactionKind.CompletePurchase,"unlock",id);var receipt=core.Execute(unlock);long revision=core.Revision;
            Assert.That(core.Execute(unlock).id,Is.EqualTo(receipt.id));Assert.That(core.Revision,Is.EqualTo(revision));
            Assert.That(core.Snapshot().money,Is.EqualTo(10));Assert.That(core.Snapshot().cashCollected,Is.EqualTo(20));TransactionCore.Validate(core.Snapshot());
        }
        [Test] public void RuntimeDraftRollbackAndLateEffectsPreserveOriginalState()
        {
            var core=new TransactionCore(TransactionCoreTests.Seed(),null,()=>100,true);
            var take=Command(core,TransactionKind.Take,"take");take.source="storage";take.destination="player";take.item="carrot";take.quantity=2;
            core.Fault=at=>{if(at==CommitBoundary.Prepared)throw new InvalidOperationException("interrupted");};
            Assert.Throws<InvalidOperationException>(()=>core.Execute(take));core.Fault=null;
            Assert.That(core.Revision,Is.Zero);TransactionCore.Validate(core.Snapshot());core.Execute(take);
            for(int i=0;i<120;i++)core.Execute(Command(core,TransactionKind.Checkpoint,"tick"+i,actor:"simulation"));
            core=new TransactionCore(core.Snapshot(),null,()=>100,true);take.key="late-retry";take.expectedRevision=core.Revision;
            Assert.That(core.Execute(take).amount,Is.EqualTo(2));Assert.That(core.Snapshot().stacks.Where(s=>s.owner=="player").Sum(s=>s.quantity),Is.EqualTo(2));TransactionCore.Validate(core.Snapshot());
        }
        [TestCase(false)] [TestCase(true)] public void PaymentTimeoutSameTickHasOneOutcomeAndNoRepeatLoss(bool beforeDeadline)
        {
            double now=100;var core=new TransactionCore(TransactionCoreTests.Seed(),null,()=>now,true);
            var order=Command(core,TransactionKind.CreateOrder,"create","order:1","simulation");order.source="counter";order.destination="customer";order.expiresAt=190;order.lines=new(){new("carrot",3,10)};core.Execute(order);
            var delivery=Command(core,TransactionKind.DeliverOrder,"partial","order:1");delivery.source="storage";delivery.item="carrot";delivery.quantity=1;core.Execute(delivery);
            now=beforeDeadline?189.999:190;delivery=Command(core,TransactionKind.DeliverOrder,"remaining","order:1");delivery.source="storage";delivery.item="carrot";delivery.quantity=2;
            if(beforeDeadline)
            {
                core.Execute(delivery);core.Execute(Command(core,TransactionKind.CompleteOrder,"complete","order:1"));core.Execute(Command(core,TransactionKind.CreatePayment,"pay","order:1"));
                Assert.Throws<TransactionRejectedException>(()=>core.Execute(Command(core,TransactionKind.FailOrder,"fail","order:1","simulation")));
                Assert.That(core.Snapshot().payments.Single().amount,Is.EqualTo(30));
            }
            else
            {
                Assert.Throws<TransactionRejectedException>(()=>core.Execute(delivery));core.Execute(Command(core,TransactionKind.FailOrder,"fail","order:1","simulation"));
                for(int i=0;i<3;i++){core=new TransactionCore(core.Snapshot(),null,()=>now,true);Assert.That(core.Execute(Command(core,TransactionKind.FailOrder,"again"+i,"order:1","simulation")).amount,Is.Zero);}
                Assert.That(core.Snapshot().payments,Is.Empty);Assert.That(core.Snapshot().stacks.Where(s=>s.owner=="customer").Sum(s=>s.quantity),Is.EqualTo(1));
                Assert.That(core.Snapshot().stacks.Where(s=>s.owner=="storage"&&s.item=="carrot").Sum(s=>s.quantity),Is.EqualTo(9));
            }
            TransactionCore.Validate(core.Snapshot());
        }
        [Test] public void ThirtyCustomerReservationsShareCapacityWithoutDuplicateDelivery()
        {
            var f=Stage11To13Fixtures.Read<Boundaries>("stage14-boundaries.json");var state=TransactionCoreTests.Seed();state.stacks.First(s=>s.id=="stock").quantity=f.activeCustomers;
            var core=new TransactionCore(state,null,()=>100,true);
            for(int i=0;i<f.activeCustomers;i++)
            {
                var order=Command(core,TransactionKind.CreateOrder,"create"+i,"order:"+i,"simulation");order.source="counter";order.destination="customer:"+i;order.expiresAt=190;
                order.owner=new OwnerState{id=order.destination,actor=order.destination,location=order.destination,kind=OwnerKind.Customer,capacity=3,writers=new(){"player","worker"}};
                order.lines=new(){new("carrot",1,10)};core.Execute(order);
                var reserve=Command(core,TransactionKind.Reserve,"reserve"+i,"r:"+i);reserve.source="storage";reserve.destination=order.destination;reserve.item="carrot";reserve.quantity=1;reserve.expiresAt=200;core.Execute(reserve);
            }
            var snapshot=core.Snapshot();Assert.That(snapshot.reservations.Count(r=>r.status==ReservationStatus.Active),Is.EqualTo(30));
            var extra=Command(core,TransactionKind.Reserve,"extra","r:extra");extra.source="storage";extra.destination="player";extra.item="carrot";extra.quantity=1;extra.expiresAt=200;
            Assert.That(Assert.Throws<TransactionRejectedException>(()=>core.Execute(extra)).Code,Is.EqualTo("stock"));
            for(int i=0;i<30;i++)
            {
                var deliver=Command(core,TransactionKind.DeliverOrder,"deliver"+i,"order:"+i);deliver.source="storage";deliver.item="carrot";deliver.quantity=1;deliver.reservation="r:"+i;
                core.Execute(deliver);core.Execute(deliver);
            }
            snapshot=core.Snapshot();Assert.That(snapshot.stacks.Where(s=>s.owner.StartsWith("customer:")).Sum(s=>s.quantity),Is.EqualTo(30));
            Assert.That(snapshot.stacks.Any(s=>s.owner=="storage"),Is.False);TransactionCore.Validate(snapshot);
        }
    }
}
