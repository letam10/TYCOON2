using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tycoon.Tests
{
    public sealed class SaveWorldCompatibilityTests
    {
        static readonly string[] RetiredIds =
        {
            "pad_farmer_2", "pad_carry_upgrade", "pad_farm_upgrade",
            "pad_worker_upgrade", "pad_machine_upgrade"
        };
        GameSession game;
        GameSession previous;
        StorageStation storage;
        ProductionStation crop;
        CheckoutStation counter;
        string directory;

        T Add<T>(string id) where T : Component
        {
            var root = new GameObject(id);
            root.SetActive(false);
            root.transform.SetParent(game.transform);
            return root.AddComponent<T>();
        }

        [SetUp]
        public void SetUp()
        {
            previous = GameSession.Instance;
            var root = new GameObject("SaveWorldCompatibility");
            root.SetActive(false);
            game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            game.Economy = new Economy(500);
            game.Player = Add<PlayerController>("Player");
            storage = AddStorage("storage_farm");
            crop = Add<ProductionStation>("Crop");
            crop.Id = "crop";
            crop.ItemId = "carrot";
            crop.Inventory = new Inventory(24);
            crop.Phase = 2;
            crop.Remaining = 1.25f;
            crop.WorkCount = 7;
            crop.PlayerWorkCount = 4;
            game.Stations.Add(crop);
            game.Producers.Add(crop);
            counter = Add<CheckoutStation>("Counter");
            counter.Id = "checkout";
            counter.ShopId = "farm";
            game.Stations.Add(counter);
            game.Checkouts.Add(counter);
            game.Player.Carry.TryAdd("carrot", 2);
            storage.Inventory.TryAdd("wheat", 3);
            game.Economy.RecordPayment(41, 12);
            game.Economy.CollectCash(12);
            game.Economy.RecordPayment(42, 10);
            counter.Cash = 10;
            game.Economy.Unlock("farmer");
            game.CrewStates.Add(GameSession.CrewFor(Definitions.Upgrade("farmer")));
            game.Purchases.Add(new PurchaseProgress
            {
                id = "carry10", paid = 35, total = Definitions.Upgrade("carry10").cost
            });
            directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "work", "save-recovery", "tests", Guid.NewGuid().ToString("N"));
            game.SavePath = Path.Combine(directory, "save.json");
        }

        StorageStation AddStorage(string id)
        {
            var station = Add<StorageStation>(id);
            station.Id = id;
            station.AreaId = "farm";
            station.Inventory = new Inventory(24);
            game.Stations.Add(station);
            return station;
        }

        [TearDown]
        public void TearDown()
        {
            game.Transactions?.Detach();
            Object.DestroyImmediate(game.gameObject);
            GameSession.Instance = previous;
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        SaveData LegacySave()
        {
            var data = game.CaptureSaveData();
            foreach (string id in RetiredIds)
                data.stationStates.Add(new StationProgressSave { id = id });
            return data;
        }

        SaveData TransactionSave()
        {
            var data = game.CaptureSaveData();
            var state = RuntimeTransactions.Migrate(game, data);
            foreach (string id in RetiredIds)
            {
                AddRetiredState(state, id);
                data.stationStates.Add(new StationProgressSave { id = id });
            }
            data.transactionState = state;
            data.transactionVersion = SaveStore.CurrentTransactionVersion;
            return data;
        }

        static void AddRetiredState(TransactionState state, string id)
        {
            state.owners.Add(new OwnerState
            {
                id = id, actor = "simulation", location = id, kind = OwnerKind.Station
            });
            state.stations.Add(new StationRuntimeState
            {
                id = id, input = id, output = id, definitionId = id.Substring(4),
                kind = "purchase", progress = new StationProgressSave { id = id }
            });
        }

        void AssertPreserved()
        {
            Assert.That(game.SaveBlocked, Is.False);
            Assert.That(game.Economy.Money, Is.EqualTo(512));
            Assert.That(game.Economy.Revenue, Is.EqualTo(22));
            Assert.That(game.Economy.CashCollected, Is.EqualTo(12));
            Assert.That(game.Economy.PendingCash, Is.EqualTo(10));
            Assert.That(game.Economy.Transactions, Is.EqualTo(2));
            Assert.That(game.Economy.ReceiptIds, Is.EquivalentTo(new long[] { 41, 42 }));
            Assert.That(counter.Cash, Is.EqualTo(10));
            Assert.That(game.Player.Carry.Count("carrot"), Is.EqualTo(2));
            Assert.That(storage.Inventory.Count("wheat"), Is.EqualTo(3));
            Assert.That(crop.Phase, Is.EqualTo(2));
            Assert.That(crop.Remaining, Is.EqualTo(1.25f));
            Assert.That(crop.WorkCount, Is.EqualTo(7));
            Assert.That(crop.PlayerWorkCount, Is.EqualTo(4));
            Assert.That(game.Purchases.Single().paid, Is.EqualTo(35));
            Assert.That(game.CrewStates.Single().id, Is.EqualTo("farmer"));
            Assert.That(game.Economy.Has("farmer"), Is.True);
            Assert.That(game.NextReceipt, Is.EqualTo(43));
        }

        [Test]
        public void LegacyRetiredPadsLoadTwiceWithoutLosingGoodsMoneyOrProgress()
        {
            SaveStore.Write(game.SavePath, LegacySave());
            var added = AddStorage("new_storage");
            game.Player.Carry.TryRemove("carrot", 2);
            storage.Inventory.TryRemove("wheat", 3);
            crop.Phase = 0;
            crop.Remaining = 0;
            crop.WorkCount = 0;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                game.LoadGame();
                AssertPreserved();
                Assert.That(added.Inventory.Total, Is.Zero);
                Assert.That(added.WorkCount, Is.Zero);
                game.SaveGame();
            }
            Assert.That(SaveStore.Read(game.SavePath).stationStates
                .Any(s => RetiredIds.Contains(s.id)), Is.False);
        }

        [Test]
        public void EmptyRetiredTransactionOwnersLoadTwiceAndPreserveAssets()
        {
            SaveStore.Write(game.SavePath, TransactionSave());
            AddStorage("new_storage");
            for (int attempt = 0; attempt < 2; attempt++)
            {
                game.LoadGame();
                AssertPreserved();
                var state = game.Transactions.Snapshot();
                Assert.That(state.owners.Any(o => RetiredIds.Contains(o.id)), Is.False);
                Assert.That(state.stations.Any(s => RetiredIds.Contains(s.id)), Is.False);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnknownStationBlocksLoadWithoutRewritingSave(bool transactions)
        {
            var data = transactions ? TransactionSave() : game.CaptureSaveData();
            if (transactions)
            {
                AddRetiredState(data.transactionState, "pad_unknown_upgrade");
                data.stationStates.Add(new StationProgressSave { id = "pad_unknown_upgrade" });
            }
            else
                data.stationStates.Add(new StationProgressSave { id = "pad_unknown_upgrade" });
            AssertRejectedUnchanged(data);
        }

        [TestCase("capacity")]
        [TestCase("goods")]
        [TestCase("reservation")]
        public void RetiredOwnerWithStateCannotBeDiscarded(string kind)
        {
            var data = TransactionSave();
            var state = data.transactionState;
            string id = RetiredIds[0];
            if (kind != "reservation")
                state.owners.Single(o => o.id == id).capacity = 1;
            if (kind == "goods")
                state.stacks.Add(new ItemStackState
                {
                    id = "retired-goods", owner = id, location = id, item = "carrot", quantity = 1
                });
            if (kind == "reservation")
                state.reservations.Add(new ReservationState
                {
                    id = "retired-reservation", holder = "player", source = "player", destination = id,
                    item = "carrot", quantity = 1, expiresAt = 100, status = ReservationStatus.Released
                });
            AssertRejectedUnchanged(data);
        }

        void AssertRejectedUnchanged(SaveData data)
        {
            SaveStore.Write(game.SavePath, data);
            byte[] original = File.ReadAllBytes(game.SavePath);
            // Trạm mới buộc đường reconcile chạy; save lỗi vẫn phải giữ nguyên từng byte.
            AddStorage("new_storage");
            LogAssert.Expect(LogType.Exception, new Regex(".+"));
            game.LoadGame();
            Assert.That(game.SaveBlocked, Is.True);
            Assert.That(game.Economy.Money, Is.EqualTo(512));
            Assert.That(game.Player.Carry.Count("carrot"), Is.EqualTo(2));
            game.SaveGame();
            Assert.That(File.ReadAllBytes(game.SavePath), Is.EqualTo(original));
            Assert.That(File.Exists(game.SavePath + ".tmp"), Is.False);
        }
    }
}
