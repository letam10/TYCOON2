using System.Linq;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class TownRedesignTests
    {
        [Test] public void AllLiteralRuntimeModelKeysExistInCatalogOrTownArt()
        {
            var catalog=UnityEngine.Resources.Load<GameCatalog>("GameCatalog");Assert.That(catalog,Is.Not.Null);
            string root=System.IO.Path.Combine(UnityEngine.Application.dataPath,"_Game/Scripts/Runtime");
            foreach(string file in System.IO.Directory.GetFiles(root,"*.cs"))
                foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(file),"Art\\.Model\\(\"([^\"]+)\""))
                    Assert.That(catalog.Model(match.Groups[1].Value)||TownArt.Supports(match.Groups[1].Value),Is.True,file+" model "+match.Groups[1].Value);
        }
        static TransactionCommand C(TransactionCore core,TransactionKind kind,string key,string target=null)=>new(){kind=kind,key=key,effectId="effect:"+key,actor="player",expectedRevision=core.Revision,target=target};
        [Test] public void MachineCapacitySpeedAndQualityAreSeparateTransactions()
        {
            var seed=TransactionCoreTests.Seed();seed.money=5000;seed.unlocked.Add("mill");seed.stations[0].kind="machine";seed.stations[0].area="processing";
            var core=new TransactionCore(seed);
            foreach(string id in new[]{"mill_capacity2","mill_speed2","mill_value2"})
            {var c=C(core,TransactionKind.ContributePurchase,"pay:"+id,id);c.quantity=600;core.Execute(c);core.Execute(C(core,TransactionKind.CompletePurchase,"buy:"+id,id));}
            var s=core.Snapshot();Assert.That(s.owners.Single(x=>x.id=="mill-input").capacity,Is.EqualTo(54));Assert.That(s.owners.Single(x=>x.id=="mill-output").capacity,Is.EqualTo(54));
            Assert.That(ProgressionTracker.StationLevel(s,"mill"),Is.EqualTo(1));Assert.That(ProgressionTracker.AxisLevel(s,"mill",UpgradeAxis.Speed),Is.EqualTo(2));Assert.That(ProgressionTracker.AxisLevel(s,"mill",UpgradeAxis.QualityValue),Is.EqualTo(2));Assert.That(s.revenue,Is.Zero);
        }
        [Test] public void AssistanceOpensFeedWithoutGrantingFarmLevels()
        {var seed=TransactionCoreTests.Seed();seed.unlocked.Clear();var core=new TransactionCore(seed);core.Execute(C(core,TransactionKind.GrantAssistance,"support"));Assert.That(core.Snapshot().unlocked,Does.Contain("feed_route"));Assert.That(core.Snapshot().unlocked,Does.Not.Contain("farm_level2"));}
        [Test] public void AutonomousBatchContinuesWithoutOperatorAndCreditsInitiatorOnce()
        {
            var seed=TransactionCoreTests.Seed();seed.stations[0].autonomous=true;seed.stations[0].lastInputActor="player";
            var core=new TransactionCore(seed,null,()=>100);
            var start=C(core,TransactionKind.StartMachine,"auto-start","mill");start.actor="simulation";start.secondary="auto-job";core.Execute(start);
            core=new TransactionCore(core.Snapshot(),null,()=>101);
            var advance=C(core,TransactionKind.AdvanceMachine,"auto-advance","mill");advance.actor="simulation";advance.secondary="auto-job";advance.duration=6;core.Execute(advance);
            var finish=C(core,TransactionKind.CompleteMachine,"auto-finish","mill");finish.actor="simulation";finish.secondary="auto-job";core.Execute(finish);core.Execute(finish);
            var s=core.Snapshot();Assert.That(s.stations[0].playerBatches,Is.EqualTo(1));Assert.That(s.stacks.Where(x=>x.owner=="mill-output").Sum(x=>x.quantity),Is.EqualTo(3));
            Assert.That(s.stacks.Where(x=>x.owner=="mill-input").Sum(x=>x.quantity),Is.EqualTo(4));TransactionCore.Validate(s);
        }
        [Test] public void BakeryRecipeUsesRealMixerOutput()
        {
            Assert.That(Definitions.Recipe("oven").inputs[0].id,Is.EqualTo("bread_dough"));
            Assert.That(Definitions.Recipe("cakeoven").inputs[0].id,Is.EqualTo("cake_batter"));
            Assert.That(Definitions.Recipe("feedmill").inputs.Select(x=>x.id),Is.EquivalentTo(new[]{"wheat","corn","soybean"}));
        }
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
            var seed=TransactionCoreTests.Seed();seed.contentVersion=1;seed.unlocked.Remove("farm_level2");
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
