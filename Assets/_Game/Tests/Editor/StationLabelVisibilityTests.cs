using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CountingLabelStation : Station
    {
        public int Reads;
        public string Content = "Trạm";
        public override string Prompt
        {
            get
            {
                Reads++;
                return Content;
            }
        }
        public override bool Interact(PlayerController player, bool withdraw) => false;
        public void RefreshLabel() => base.LateUpdate();
    }

    public sealed class StationLabelVisibilityTests
    {
        GameObject root;
        GameSession game;
        GameSession previous;
        CountingLabelStation station;
        StationZone zone;

        T Add<T>(string name) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child.AddComponent<T>();
        }

        [SetUp]
        public void SetUp()
        {
            previous = GameSession.Instance;
            root = new GameObject("LabelVisibilityTest");
            root.SetActive(false);
            game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            game.Player = Add<PlayerController>("Player");
            station = Add<CountingLabelStation>("Station");
            station.StatusLabel = Add<TextMesh>("StationLabel");
        }

        [TearDown]
        public void TearDown()
        {
            if (zone) Object.DestroyImmediate(zone.gameObject);
            Object.DestroyImmediate(root);
            GameSession.Instance = previous;
        }

        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, true)]
        public void HiddenStationNeverFormatsItsPrompt(bool zones, bool locked, bool distant)
        {
            station.PlayerUsesZones = zones;
            station.Requirement = locked ? "unowned" : "";
            game.Player.transform.position = distant ? Vector3.right * 10 : Vector3.zero;
            for (int i = 0; i < 100; i++) station.RefreshLabel();
            Assert.That(station.Reads, Is.Zero);
            Assert.That(station.StatusLabel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ReturningToStationShowsCurrentTextInTheSameRefresh()
        {
            station.Content = "Đang làm";
            station.RefreshLabel();
            Assert.That(station.StatusLabel.text, Is.EqualTo("Đang làm"));
            game.Player.transform.position = Vector3.right * 10;
            station.RefreshLabel();
            int reads = station.Reads;
            station.Content = "Đã hoàn tất";
            station.RefreshLabel();
            Assert.That(station.Reads, Is.EqualTo(reads));
            game.Player.transform.position = Vector3.zero;
            station.RefreshLabel();
            Assert.That(station.StatusLabel.gameObject.activeSelf, Is.True);
            Assert.That(station.StatusLabel.text, Is.EqualTo("Đã hoàn tất"));
        }

        [Test]
        public void LockedZoneDoesNotFormatItsTargetPrompt()
        {
            station.Requirement = "unowned";
            zone = Add<StationZone>("Zone");
            zone.Target = station;
            zone.Mode = "operate";
            zone.StatusLabel = Add<TextMesh>("ZoneLabel");
            RefreshZone(zone);
            Assert.That(station.Reads, Is.Zero);
            Assert.That(zone.StatusLabel.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ZoneReentryShowsCurrentTargetPrompt()
        {
            zone = Add<StationZone>("Zone");
            zone.transform.SetParent(null);
            zone.Target = station;
            zone.Mode = "operate";
            zone.StatusLabel = Add<TextMesh>("ZoneLabel");
            RefreshZone(zone);
            Assert.That(zone.StatusLabel.text, Is.EqualTo("Trạm"));
            game.Player.transform.position = Vector3.right * 3;
            RefreshZone(zone);
            int reads = station.Reads;
            station.Content = "Có hàng mới";
            RefreshZone(zone);
            Assert.That(station.Reads, Is.EqualTo(reads));
            game.Player.transform.position = Vector3.zero;
            RefreshZone(zone);
            Assert.That(zone.StatusLabel.gameObject.activeSelf, Is.True);
            Assert.That(zone.StatusLabel.text, Is.EqualTo("Có hàng mới"));
        }

        static void RefreshZone(StationZone zone) => typeof(StationZone)
            .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(zone, null);
    }
}
