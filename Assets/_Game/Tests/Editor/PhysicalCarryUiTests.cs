using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class PhysicalCarryUiTests
    {
        [Test]
        public void ModMenuBlocksPlayerControlUntilClosed()
        {
            var root = new GameObject("ModMenuTest");
            try
            {
                var hud = root.AddComponent<GameHud>();
                var field = typeof(GameHud).GetField("modOpen",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.That(hud.AllowsPlayerControl, Is.True);
                field.SetValue(hud, true);
                Assert.That(hud.IsModMenuOpen, Is.True);
                Assert.That(hud.AllowsPlayerControl, Is.False);
                field.SetValue(hud, false);
                Assert.That(hud.AllowsPlayerControl, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EveryCatalogItemHasItsOwnCachedDrawnSprite()
        {
            var sprites = new HashSet<Sprite>();
            foreach (var item in Definitions.Items)
            {
                var sprite = ItemIconAtlas.Get(item.id);
                Assert.That(sprite, Is.Not.Null, item.id);
                Assert.That(sprite.name, Is.EqualTo("ItemIcon_" + item.id));
                Assert.That(sprite.texture.width, Is.EqualTo(256));
                Assert.That(ItemIconAtlas.Get(item.id), Is.SameAs(sprite));
                Assert.That(sprites.Add(sprite), Is.True, item.id + " reuses another item icon");
            }
            Assert.That(sprites.Count, Is.EqualTo(27));
            Assert.That(ItemIconAtlas.Get("not_an_item"), Is.Null);
        }

        [Test]
        public void NoticesHaveAtMostTwoLinesAndABoundedLength()
        {
            string compact = HudNoticeBuffer.Compact(new string('A', 300) + "\nsecond\nthird");
            Assert.That(compact.Split('\n').Length, Is.LessThanOrEqualTo(2));
            Assert.That(compact.Replace("\n", "").Length, Is.LessThanOrEqualTo(HudNoticeBuffer.MaximumCharacters));
            Assert.That(compact.EndsWith("…"), Is.True);
            Assert.That(HudNoticeBuffer.Compact("  Lưu\n\n  thành công  "), Is.EqualTo("Lưu thành công"));
        }

        [Test]
        public void RepeatedNoticesDoNotExtendTheVisibleDuration()
        {
            var notices = new HudNoticeBuffer();
            Assert.That(notices.Push("Đã lưu trò chơi", 0), Is.True);
            Assert.That(notices.Push("Đã lưu trò chơi", 2), Is.False);
            notices.Tick(3);
            Assert.That(notices.Current, Is.Empty);
            Assert.That(notices.Push("Đã lưu trò chơi", 4), Is.False);
            Assert.That(notices.Push("Đã lưu trò chơi", 7), Is.True);
        }

        [Test]
        public void RapidDistinctNoticesKeepOnlyTheLatestPendingMessages()
        {
            var notices = new HudNoticeBuffer();
            for (int i = 0; i < 10; i++) notices.Push("Thông báo " + i, i * .01f);
            Assert.That(notices.PendingCount, Is.EqualTo(HudNoticeBuffer.MaximumPending));
            notices.Tick(3);
            Assert.That(notices.Current, Is.EqualTo("Thông báo 7"));
            Assert.That(notices.Push("Thông báo 8", 3), Is.False);
        }

        [Test]
        public void ReusedBubbleClearsLinesAndRestoresPatience()
        {
            var root = new GameObject("BubbleTest", typeof(RectTransform));
            var first = new GameObject("FirstCustomer");
            var second = new GameObject("NextDiner");
            try
            {
                var view = new OrderBubbleView(root.transform);
                view.Bind(first.transform);
                view.SetLines(new List<OrderLine>
                {
                    new("carrot", 3, 10) { delivered = 2 },
                    new("milk", 1, 35)
                }, .15f);
                Assert.That(view.LineCount, Is.EqualTo(2));
                Assert.That(view.PatienceFraction, Is.EqualTo(.15f).Within(.001f));
                view.Reset();
                Assert.That(view.Target, Is.Null);
                Assert.That(view.LineCount, Is.Zero);
                Assert.That(view.Rect.gameObject.activeSelf, Is.False);
                Assert.That(view.PatienceFraction, Is.EqualTo(1));
                view.Bind(second.transform);
                view.SetSingle("corn_soup", 0, 1, .8f);
                Assert.That(view.Target, Is.SameAs(second.transform));
                Assert.That(view.LineCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void PatienceUsesGreenYellowAndRedBands()
        {
            var green = OrderBubbleView.PatienceColor(.8f);
            var yellow = OrderBubbleView.PatienceColor(.4f);
            var red = OrderBubbleView.PatienceColor(.1f);
            Assert.That(green.g, Is.GreaterThan(green.r));
            Assert.That(yellow.r, Is.GreaterThan(yellow.g));
            Assert.That(yellow.g, Is.GreaterThan(yellow.b));
            Assert.That(red.r, Is.GreaterThan(red.g * 2));
        }
    }
}
