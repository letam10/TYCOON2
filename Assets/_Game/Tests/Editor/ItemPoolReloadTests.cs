using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class ItemPoolReloadTests
    {
        GameObject root;
        ItemPool pool;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("PoolReloadTest");
            pool = new ItemPool(root.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void DestroyedActorDoesNotPoisonReturnedVisuals()
        {
            var survivor = new GameObject("carrot");
            pool.Return(survivor);
            var actor = new GameObject("PreviousWorker");
            actor.transform.SetParent(root.transform);
            var oldVisual = new GameObject("carrot");
            oldVisual.transform.SetParent(actor.transform);
            pool.Return(oldVisual, false);
            Object.DestroyImmediate(actor);

            var restoredVisual = pool.Take("carrot", root.transform);
            Assert.That(restoredVisual, Is.SameAs(survivor));
            Assert.That(restoredVisual.activeSelf, Is.True);
            Assert.That(pool.Created, Is.Zero);
        }

        [Test]
        public void ReturningDestroyedVisualDoesNotBreakNextTake()
        {
            var survivor = new GameObject("carrot");
            pool.Return(survivor);
            var destroyed = new GameObject("carrot");
            Object.DestroyImmediate(destroyed);

            Assert.DoesNotThrow(() => pool.Return(destroyed));
            Assert.That(pool.Take("carrot", root.transform), Is.SameAs(survivor));
        }
    }
}
