using System.Linq;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class TownRedesignTests
    {
        static TransactionCommand C(TransactionCore core,TransactionKind kind,string key,string target=null)=>new(){kind=kind,key=key,effectId="effect:"+key,actor="player",expectedRevision=core.Revision,target=target};
        [Test] public void AssistanceRetriesOnceAndDoesNotBecomeSalesOrCollectedCash()
        {
            var core=new TransactionCore(TransactionCoreTests.Seed());var command=C(core,TransactionKind.GrantAssistance,"assist");
            core.Execute(command);core.Execute(command);
            var s=core.Snapshot();Assert.That(s.money,Is.EqualTo(600+999999));Assert.That(s.assistedCash,Is.EqualTo(999999));
            Assert.That(s.cashCollected,Is.Zero);Assert.That(s.revenue,Is.Zero);Assert.That(s.payments,Is.Empty);
            Assert.That(s.unlocked,Does.Contain("restaurant"));Assert.That(s.unlocked,Does.Not.Contain("farm_level3"));
            core.Execute(C(core,TransactionKind.GrantAssistance,"assist2"));Assert.That(core.Snapshot().assistedCash,Is.EqualTo(1999998));
        }
        [Test] public void FarmTierTwoOpensFeedCropsWithoutOpeningTomatoOrChangingActiveYield()
        {
            var seed=TransactionCoreTests.Seed();seed.contentVersion=1;
            seed.stations.Add(new(){id="plot",kind="producer",item="carrot",input="storage",output="storage",batchYield=2,progress=new(){herd=0}});
            var core=new TransactionCore(seed);
            var sow=C(core,TransactionKind.OperateProducer,"sow","plot");sow.duration=1;core.Execute(sow);
            var pay=C(core,TransactionKind.ContributePurchase,"farm-pay","farm_level2");pay.quantity=100;core.Execute(pay);
            core.Execute(C(core,TransactionKind.CompletePurchase,"farm-buy","farm_level2"));
            var s=core.Snapshot();Assert.That(s.unlocked,Does.Contain("crop_wheat"));Assert.That(s.unlocked,Does.Contain("crop_corn"));Assert.That(s.unlocked,Does.Contain("crop_soybean"));
            Assert.That(s.unlocked,Does.Not.Contain("crop_tomato"));Assert.That(s.stations.Single(x=>x.id=="plot").progress.cycleYield,Is.EqualTo(2));
        }
    }
}
