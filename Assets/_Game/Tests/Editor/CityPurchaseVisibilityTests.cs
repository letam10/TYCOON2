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
    }
}
