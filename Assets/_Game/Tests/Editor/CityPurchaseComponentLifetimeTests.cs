using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed partial class RuntimeAuthorityTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void CompletedPadSkipsDestroyedCachedComponents(bool destroyCollider)
        {
            RestoreFarmSpeed(2);
            var temporary = new GameObject("TemporaryPadDecoration", typeof(BoxCollider), typeof(MeshRenderer));
            temporary.transform.SetParent(farmSpeedPad.transform, false);
            farmSpeedPad.InitializeIcon();
            Component removed = destroyCollider ? temporary.GetComponent<Collider>() :
                temporary.GetComponent<Renderer>();
            Object.DestroyImmediate(removed);

            var state = game.Transactions.Snapshot();
            state.unlocked.Add("farm_speed3");
            game.Transactions.Detach();
            game.InitializeTransactions(state, false);
            var refresh = typeof(PurchasePad).GetMethod("RefreshDisplay", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(() => refresh.Invoke(farmSpeedPad, null), Throws.Nothing);
            Assert.That(farmSpeedPad.gameObject.activeSelf, Is.False);
            Assert.That(farmSpeedPad.Available, Is.False);
        }
    }
}
