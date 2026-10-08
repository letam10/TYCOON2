using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon.Tests
{
    public sealed class CityUiMotionTests
    {
        [Test]
        public void PatienceKeepsExactTimeWithoutRebuildingForSubpixelChanges()
        {
            var root = new GameObject("BubbleTest", typeof(RectTransform));
            try
            {
                var view = new OrderBubbleView(root.transform);
                view.Bind(root.transform);
                view.SetSingle("carrot", 1, 3, .8f);
                var ring = view.Rect.Find("Patience").GetComponent<Image>();
                float drawn = ring.fillAmount;
                view.SetSingle("carrot", 1, 3, .8001f);
                Assert.That(view.PatienceFraction, Is.EqualTo(.8001f));
                Assert.That(ring.fillAmount, Is.EqualTo(drawn));
                view.SetSingle("carrot", 1, 3, .24f);
                Assert.That(ring.color, Is.EqualTo(OrderBubbleView.PatienceColor(.24f)));
                Assert.That(Mathf.Abs(ring.fillAmount - .24f), Is.LessThan(1f / 512));
                view.Reset();
                view.Bind(root.transform);
                view.SetSingle("milk", 0, 2, 1);
                Assert.That(view.Rect.Find("OrderLine0").gameObject.activeSelf, Is.True);
                Assert.That(view.Rect.Find("OrderLine1").gameObject.activeSelf, Is.False);
                Assert.That(view.PatienceFraction, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FeedbackBurstReusesItsObjectsAcrossRepeatedCollections()
        {
            var root = new GameObject("BurstTest");
            try
            {
                var feedback = root.AddComponent<GameFeedback>();
                feedback.Burst(Vector3.zero);
                var burst = root.GetComponentInChildren<PurchaseBurst>();
                int children = burst.transform.childCount;
                burst.Advance(.5f);
                for (int i = 0; i < 10; i++)
                {
                    feedback.Burst(Vector3.one);
                    Assert.That(root.GetComponentInChildren<PurchaseBurst>(), Is.SameAs(burst));
                    Assert.That(burst.transform.childCount, Is.EqualTo(children));
                    burst.Advance(.5f);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TransferReusesModelAndSleepsWhenTheModelReturnsToAStack()
        {
            var previous = GameSession.Instance;
            var root = new GameObject("TransferTest");
            var game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            game.enabled = false;
            game.Pool = new ItemPool(root.transform);
            try
            {
                CarryTransferVisual.Play("carrot", Vector3.zero, Vector3.one, root.transform);
                var flight = root.GetComponentInChildren<CarryTransferVisual>();
                var model = flight.gameObject;
                flight.Advance(.5f);
                Assert.That(model.activeSelf, Is.False);
                Assert.That(flight.enabled, Is.False);
                int created = game.Pool.Created;
                CarryTransferVisual.Play("carrot", Vector3.one, Vector3.zero, root.transform);
                Assert.That(root.GetComponentInChildren<CarryTransferVisual>(), Is.SameAs(flight));
                Assert.That(game.Pool.Created, Is.EqualTo(created));
                flight.Advance(.5f);
                var stacked = game.Pool.Take("carrot", root.transform);
                Assert.That(stacked, Is.SameAs(model));
                stacked.transform.localPosition = new Vector3(0, 2, 0);
                flight.Advance(1);
                Assert.That(stacked.transform.localPosition, Is.EqualTo(new Vector3(0, 2, 0)));
                Assert.That(stacked.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                GameSession.Instance = previous;
            }
        }
    }
}
