using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class UpgradeVisualTests
    {
        GameSession game;
        GameSession previous;

        [SetUp]
        public void SetUp()
        {
            previous = GameSession.Instance;
            var root = new GameObject("UpgradeVisualTests");
            root.SetActive(false);
            game = root.AddComponent<GameSession>();
            game.Economy = new Economy(0);
            GameSession.Instance = game;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(game.gameObject);
            GameSession.Instance = previous;
        }

        [Test]
        public void EditModeRefreshBuildsCurrentTierImmediatelyAndDoesNotDuplicateParts()
        {
            var root = new GameObject("Farm");
            root.transform.SetParent(game.transform, false);
            var station = root.AddComponent<ProductionStation>();
            game.Stations.Add(station);
            UpgradeModelView.RefreshAll(game, false);
            game.Economy.Unlocked.Add("farm_level3");
            game.Economy.Unlocked.Add("farm_speed3");
            UpgradeModelView.RefreshAll(game, true);
            var view = root.GetComponent<UpgradeModelView>();
            Assert.That(view.Tier, Is.EqualTo(3));
            Assert.That(view.SpeedLevel, Is.EqualTo(3));
            var parts = root.transform.Find("PersistentUpgradeDetails");
            Assert.That(parts, Is.Not.Null);
            Assert.That(parts.GetComponentsInChildren<Transform>(true)
                .Count(x => x.name == "SprinklerHead"), Is.EqualTo(6));
            int count = parts.childCount;
            UpgradeModelView.RefreshAll(game, true);
            typeof(UpgradeModelView).GetMethod("BuildPendingVisual",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            Assert.That(root.transform.Find("PersistentUpgradeDetails"), Is.SameAs(parts));
            Assert.That(parts.childCount, Is.EqualTo(count));
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(parts.localScale, Is.EqualTo(Vector3.one));
            Assert.That(game.GetComponent<UpgradeVisualQueue>(), Is.Null);
        }

        [Test]
        public void CrewRefreshReflectsAllAxesAndKeepsTheSharedSingleMesh()
        {
            var root = new GameObject("Worker");
            root.transform.SetParent(game.transform, false);
            var worker = root.AddComponent<WorkerAgent>();
            worker.UpgradeId = "farmer";
            var crew = new CrewState { id = "farmer", count = 2, carryLevel = 2, speedLevel = 2 };
            game.CrewStates.Add(crew);
            game.Workers.Add(worker);
            UpgradeModelView.RefreshAll(game, true);
            crew.count = 3;
            crew.carryLevel = 3;
            crew.speedLevel = 3;
            UpgradeModelView.RefreshAll(game, true);
            var view = root.GetComponent<UpgradeModelView>();
            Assert.That(view.Tier, Is.EqualTo(3));
            Assert.That(view.CapacityLevel, Is.EqualTo(3));
            Assert.That(view.SpeedLevel, Is.EqualTo(3));
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(filters.Length, Is.EqualTo(1));
            Assert.That(filters[0].sharedMesh.triangles.Length / 3, Is.EqualTo(72));
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(filters[0].transform.localScale, Is.EqualTo(Vector3.one));
        }
    }
}
