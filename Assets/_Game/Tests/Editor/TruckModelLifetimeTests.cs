using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class TruckModelLifetimeTests
    {
        const string CrateId = "crate:lifetime";
        const string WorkerId = "loader:0";
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        GameSession previous;
        GameCatalog previousCatalog;
        GameSession game;
        TruckLogistics logistics;
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            previous = GameSession.Instance;
            previousCatalog = Art.Catalog;
            Art.Catalog = Resources.Load<GameCatalog>("GameCatalog");
            root = new GameObject("TruckModelLifetimeTest");
            root.SetActive(false);
            game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            game.Player = Add<PlayerController>("Player");
            game.Economy = new Economy(0);
            logistics = Add<TruckLogistics>("Logistics");
            typeof(TruckLogistics).GetProperty(nameof(TruckLogistics.Vehicle))
                .SetValue(logistics, Add<Transform>("Vehicle"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            GameSession.Instance = previous;
            Art.Catalog = previousCatalog;
        }

        [TestCase("dock:storage_processing")]
        [TestCase("truck:starter")]
        [TestCase("player")]
        public void DestroyedCachedCrateRebuildsOnceWithoutChangingOwnership(string holder)
        {
            Initialize(holder);
            Present();
            var oldModel = Models()[CrateId];
            var parent = holder == "player" ? game.Player.transform :
                holder == "truck:starter" ? logistics.Vehicle : logistics.transform;
            Assert.That(oldModel.transform.parent, Is.SameAs(parent));
            string before = JsonUtility.ToJson(game.Transactions.Snapshot());
            Object.DestroyImmediate(oldModel);

            Assert.DoesNotThrow(Present);

            AssertRebuilt(oldModel, parent, before);
        }

        [Test]
        public void ReplacingWorkerRebuildsItsDestroyedCrateUnderTheNewWorker()
        {
            Initialize(RuntimeTransactions.WorkerOwner(WorkerId));
            var worker = AddWorker();
            Present();
            var oldModel = Models()[CrateId];
            Assert.That(oldModel.transform.parent, Is.SameAs(worker.transform));
            game.Workers.Remove(worker);
            Object.DestroyImmediate(worker.gameObject);
            Assert.That(oldModel == null, Is.True);
            var replacement = AddWorker();
            string before = JsonUtility.ToJson(game.Transactions.Snapshot());

            Assert.DoesNotThrow(Present);

            AssertRebuilt(oldModel, replacement.transform, before);
        }

        void AssertRebuilt(GameObject oldModel, Transform holder, string before)
        {
            var rebuilt = Models()[CrateId];
            Assert.That(rebuilt, Is.Not.SameAs(oldModel));
            Assert.That(rebuilt != null, Is.True);
            Assert.That(rebuilt.transform.parent, Is.SameAs(holder));
            Assert.That(rebuilt.GetComponentsInChildren<TextMesh>(true).Single().text, Is.EqualTo("×6"));
            Present();
            Assert.That(Models()[CrateId], Is.SameAs(rebuilt));
            Assert.That(Models().Count, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<Transform>(true).Count(t => t.name == CrateId), Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(game.Transactions.Snapshot()), Is.EqualTo(before));
            TransactionCore.Validate(game.Transactions.Snapshot());
        }

        void Initialize(string holder)
        {
            var state = new TransactionState { schemaVersion = 3 };
            state.unlocked.AddRange(new[] { "truck_bundle", "loader_processing" });
            state.crews.Add(GameSession.CrewFor(Definitions.Upgrade("loader_processing")));
            state.owners.Add(new OwnerState
            {
                id = "player", actor = "player", location = "player", kind = OwnerKind.Player, capacity = 12
            });
            state.owners.Add(new OwnerState
            {
                id = "storage_processing", actor = "simulation", location = "processing",
                kind = OwnerKind.Storage, capacity = 72
            });
            string actor = RuntimeTransactions.WorkerOwner(WorkerId);
            state.owners.Add(new OwnerState
            {
                id = actor, actor = actor, location = actor, kind = OwnerKind.Worker,
                capacity = 12, singleItem = true,
                worker = new WorkerSave { id = WorkerId, upgrade = "loader_processing" }
            });
            state.owners.Add(new OwnerState
            {
                id = CrateId, actor = "simulation", location = holder + "/" + CrateId,
                kind = OwnerKind.Crate, capacity = 6, singleItem = true
            });
            state.crates.Add(new CargoCrateState
            {
                id = CrateId, item = "carrot", holder = holder, source = "storage_processing"
            });
            state.stacks.Add(new ItemStackState
            {
                id = "cargo:lifetime", owner = CrateId, location = holder + "/" + CrateId,
                item = "carrot", quantity = 6
            });
            game.InitializeTransactions(state, persist: false);
        }

        WorkerAgent AddWorker()
        {
            var worker = Add<WorkerAgent>("Loader");
            worker.WorkerId = WorkerId;
            worker.UpgradeId = "loader_processing";
            game.Transactions.BindWorker(worker);
            game.Workers.Add(worker);
            return worker;
        }

        T Add<T>(string name) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            return typeof(T) == typeof(Transform) ? child.transform as T : child.AddComponent<T>();
        }

        Dictionary<string, GameObject> Models() => (Dictionary<string, GameObject>)typeof(TruckLogistics)
            .GetField("visuals", PrivateInstance).GetValue(logistics);

        void Present() => typeof(TruckLogistics).GetMethod("LateUpdate", PrivateInstance).Invoke(logistics, null);
    }
}
