using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        void RestoreFarmSpeed(int highest)
        {
            var state = game.Transactions.Snapshot();
            for (int level = 2; level <= highest; level++)
            {
                string id = "farm_speed" + level;
                if (!state.unlocked.Contains(id)) state.unlocked.Add(id);
            }
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            farmSpeedPad.gameObject.AddComponent<BoxCollider>();
            var price = new GameObject("TestPrice", typeof(TextMesh));
            price.transform.SetParent(farmSpeedPad.transform, false);
            farmSpeedPad.gameObject.SetActive(true);
            farmSpeedPad.InitializeIcon();
        }

        [Test]
        public void LoadedFinalPadDisappearsWithItsInteraction()
        {
            RestoreFarmSpeed(3);
            Assert.That(farmSpeedPad.Upgrade.id, Is.EqualTo("farm_speed3"));
            Assert.That(farmSpeedPad.gameObject.activeSelf, Is.False);
            Assert.That(farmSpeedPad.GetComponentsInChildren<Renderer>(true).All(x => !x.enabled), Is.True);
            Assert.That(farmSpeedPad.GetComponentsInChildren<Collider>(true).All(x => !x.enabled), Is.True);
            Assert.That(farmSpeedPad.Available, Is.False);
        }

        [Test]
        public void LoadedIntermediatePadKeepsNextUpgrade()
        {
            RestoreFarmSpeed(2);
            Assert.That(farmSpeedPad.Upgrade.id, Is.EqualTo("farm_speed3"));
            Assert.That(farmSpeedPad.gameObject.activeSelf, Is.True);
            Assert.That(farmSpeedPad.Evaluation.State, Is.Not.EqualTo(PurchaseState.Purchased));
        }

        [Test]
        public void LoadingEarlierTierReactivatesPreviouslyCompletedPad()
        {
            RestoreFarmSpeed(3);
            Assert.That(farmSpeedPad.gameObject.activeSelf, Is.False);
            var earlier = game.Transactions.Snapshot();
            earlier.unlocked.Remove("farm_speed3");
            game.Transactions.Detach();
            game.InitializeTransactions(earlier, false);
            farmSpeedPad.RestoreAfterLoad();
            Assert.That(farmSpeedPad.gameObject.activeSelf, Is.True);
            Assert.That(farmSpeedPad.Upgrade.id, Is.EqualTo("farm_speed3"));
            Assert.That(farmSpeedPad.Evaluation.State, Is.Not.EqualTo(PurchaseState.Purchased));
        }

        [Test]
        public void FinalUpgradeDoesNotDrawAnIconBeforeHiding()
        {
            RestoreFarmSpeed(2);
            var image = farmSpeedPad.GetComponentInChildren<SpriteRenderer>(true).sprite;
            RestoreFarmSpeed(3);
            Assert.That(farmSpeedPad.gameObject.activeSelf, Is.False);
            Assert.That(farmSpeedPad.GetComponentInChildren<SpriteRenderer>(true).sprite, Is.SameAs(image));
        }

        [Test]
        public void PurchasedUpgradeDoesNotDisplayMissingTrainingRequirements()
        {
            var state = game.Transactions.Snapshot();
            var upgrade = Definitions.Upgrade("cook");
            Assert.That(ProgressionTracker.MissingRequirements(state, upgrade), Is.Not.Empty);
            state.unlocked.Add(upgrade.id);
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            var evaluation = game.Progression.Evaluate(upgrade);
            Assert.That(evaluation.State, Is.EqualTo(PurchaseState.Purchased));
            Assert.That(evaluation.Requirements, Is.Empty);
            Assert.That(evaluation.CanContribute, Is.False);
        }

        [Test]
        public void InitiallyHiddenDuplicateKeepsItsLayoutAnchor()
        {
            var duplicate = Add<PurchasePad>("DuplicateFarmSpeed");
            duplicate.Upgrade = Definitions.Upgrade("farm_speed3");
            game.Stations.Add(duplicate);
            var price = new GameObject("TestPrice", typeof(TextMesh));
            price.transform.SetParent(duplicate.transform, false);
            duplicate.InitializeIcon();
            Assert.That(duplicate.transform.Find("PurchaseIcon2D"), Is.Not.Null);
            Assert.That(duplicate.gameObject.activeSelf, Is.False);
        }
    }
}
