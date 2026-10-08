using System.Linq;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class PurchaseIconTests
    {
        [TestCase("milk_line","milk")][TestCase("egg_line","eggs")][TestCase("sheep_line","sheep")]
        [TestCase("farmer","farmer")][TestCase("processor","processor")][TestCase("cook_bakery","baker")]
        [TestCase("cook","cook")][TestCase("waiter","waiter")][TestCase("truck_bundle","driver")]
        [TestCase("repair_processing","repairer")][TestCase("carry10","basket")]
        [TestCase("mill_speed2","mill")][TestCase("storage_bakery_capacity2","crate")]
        public void PurchaseSubjectMatchesTheObjectOrProfession(string id,string subject)
        {Assert.That(DrawnPurchaseIcons.Subject(Definitions.Upgrade(id)),Is.EqualTo(subject));}

        [Test]public void EveryCurrentPurchaseHasASpecificDrawnIcon()
        {
            foreach(var upgrade in Definitions.Upgrades.Where(x=>x.kind!="legacy"))
            {
                Assert.That(DrawnPurchaseIcons.Subject(upgrade),Is.Not.EqualTo("upgrade"),upgrade.id);
                var pixels=DrawnPurchaseIcons.Draw(upgrade);
                Assert.That(pixels.Length,Is.EqualTo(DrawnPurchaseIcons.Size*DrawnPurchaseIcons.Size));
                Assert.That(pixels.Count(x=>x.a>0),Is.GreaterThan(4000),upgrade.id);
                Assert.That(pixels[0].a,Is.Zero,upgrade.id);
                Assert.That(pixels[DrawnPurchaseIcons.Size-1].a,Is.Zero,upgrade.id);
            }
        }
        [Test]public void FarmQualitySpeedCapacityAndLevelAreVisuallyDistinct()
        {
            var ids=new[]{"farm_value2","farm_speed2","farm_capacity2","farm_level2"};
            for(int i=0;i<ids.Length;i++)for(int j=i+1;j<ids.Length;j++)
                Assert.That(DrawnPurchaseIcons.Draw(Definitions.Upgrade(ids[i])).SequenceEqual(DrawnPurchaseIcons.Draw(Definitions.Upgrade(ids[j]))),Is.False,ids[i]+" and "+ids[j]);
            Assert.That(DrawnPurchaseIcons.Draw(Definitions.Upgrade("mill_speed2")).SequenceEqual(DrawnPurchaseIcons.Draw(Definitions.Upgrade("mill_speed3"))),Is.False);
            Assert.That(DrawnPurchaseIcons.Draw(Definitions.Upgrade("restocker")).SequenceEqual(DrawnPurchaseIcons.Draw(Definitions.Upgrade("loader_farm"))),Is.False);
        }
        [Test]
        public void IllustratedEdgesHavePartialCoverageInsteadOfHardPixelSteps()
        {
            var pixels = DrawnPurchaseIcons.Draw(Definitions.Upgrade("milk_line"));
            Assert.That(pixels.Count(p => p.a > 0 && p.a < 255), Is.GreaterThan(100));
            Assert.That(pixels.Distinct().Count(), Is.GreaterThan(100));
        }
    }
}
