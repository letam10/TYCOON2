using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CoreTests
    {
        [Test]
        public void FullWarehouseRetainsCarriedGoodsAndRecoversAfterRestock()
        {
            var warehouse=new Inventory(4);var carried=new Inventory(2);var shelf=new Inventory(2);
            warehouse.TryAdd("milk",4);carried.TryAdd("carrot",2);
            Assert.That(Inventory.Transfer(carried,warehouse,"carrot",2),Is.Zero);
            Assert.That(carried.Count("carrot"),Is.EqualTo(2));
            Assert.That(Inventory.Transfer(warehouse,shelf,"milk",2),Is.EqualTo(2));
            Assert.That(Inventory.Transfer(carried,warehouse,"carrot",2),Is.EqualTo(2));
            Assert.That(carried.Total,Is.Zero);Assert.That(warehouse.Total,Is.EqualTo(4));
            Assert.That(warehouse.Total+shelf.Total+carried.Total,Is.EqualTo(6));
        }
        [Test]
        public void DeferredCheckoutPaysOnlyOnceAndCashCollectionIsConserved()
        {
            var economy = new Economy(); var basket = new Inventory(3); basket.TryAdd("milk", 3);
            int price=3 * Definitions.Item("milk").price;
            Assert.That(economy.RecordPayment(41, price), Is.True);
            Assert.That(economy.Money, Is.Zero); Assert.That(economy.PendingCash, Is.EqualTo(3 * Definitions.Item("milk").price));
            Assert.That(economy.RecordPayment(41, price), Is.False);
            Assert.That(economy.CollectCash(1000), Is.EqualTo(3 * Definitions.Item("milk").price));
            Assert.That(economy.Money, Is.EqualTo(3 * Definitions.Item("milk").price)); Assert.That(economy.PendingCash, Is.Zero);
            Assert.That(economy.CollectCash(1000), Is.Zero);
        }
        [Test]
        public void DinerAndCashSaveUsesStableIdsAndRetainsPendingMeal()
        {
            var data=new SaveData{pendingCash=55,businessStage=5,restaurantMeals=2,bakerySales=3,nextReceipt=71};
            data.cash.Add(new CashSave{id="checkout_restaurant",amount=55});
            data.diners.Add(new DinerSave{table="table_2",receipt=70,phase=2,remaining=3.5f,basket=new System.Collections.Generic.List<ItemAmount>{new("meal",1)}});
            var restored=UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(data));
            Assert.That(restored.diners[0].table,Is.EqualTo("table_2"));Assert.That(restored.diners[0].receipt,Is.EqualTo(70));
            Assert.That(restored.diners[0].remaining,Is.EqualTo(3.5f));Assert.That(restored.diners[0].basket[0].id,Is.EqualTo("meal"));
            Assert.That(restored.cash[0].amount,Is.EqualTo(restored.pendingCash));Assert.That(restored.businessStage,Is.EqualTo(5));
        }
        [Test]
        public void InventoryRejectsInvalidInputWithoutMutation()
        {
            var inventory = new Inventory(4);
            Assert.IsFalse(inventory.TryAdd("missing", 1));
            Assert.IsFalse(inventory.TryAdd("carrot", -1));
            Assert.IsFalse(inventory.TryAdd("carrot", 0));
            Assert.IsTrue(inventory.TryAdd("carrot", 3));
            Assert.IsFalse(inventory.TryAdd("tomato", 2));
            Assert.IsFalse(inventory.TryRemove("carrot", 4));
            Assert.AreEqual(3, inventory.Total);
            Assert.AreEqual(3, inventory.Count("carrot"));
        }

        [Test]
        public void TransferConservesItemsAndHonorsCapacity()
        {
            var from = new Inventory(10); var to = new Inventory(3);
            from.TryAdd("carrot", 8); to.TryAdd("tomato", 1);
            Assert.AreEqual(2, Inventory.Transfer(from, to, "carrot", 20));
            Assert.AreEqual(6, from.Count("carrot"));
            Assert.AreEqual(2, to.Count("carrot"));
            Assert.AreEqual(1, to.Count("tomato"));
            Assert.AreEqual(0, Inventory.Transfer(from, to, "carrot", 1));
            Assert.AreEqual(0, Inventory.Transfer(from, from, "carrot", 2));
            Assert.AreEqual(9, from.Total + to.Total);
        }

        [Test]
        public void MissingRecipeInputDoesNotConsumeOtherIngredients()
        {
            var inventory = new Inventory(20);
            inventory.TryAdd("flour", 4); inventory.TryAdd("egg", 2);
            Assert.IsFalse(Definitions.Recipe("breadmixer").Consume(inventory));
            Assert.AreEqual(4, inventory.Count("flour"));
            Assert.AreEqual(2, inventory.Count("egg"));
            inventory.TryAdd("milk", 1);
            Assert.IsTrue(Definitions.Recipe("breadmixer").Consume(inventory));
            Assert.AreEqual(2, inventory.Count("flour"));
            Assert.AreEqual(1, inventory.Count("egg"));
            Assert.AreEqual(0, inventory.Count("milk"));
        }

        [Test]
        public void PurchaseEnforcesMoneyPrerequisitesAndSinglePayment()
        {
            var previous=GameSession.Instance;var root=new GameObject("PurchaseTest");root.SetActive(false);
            try
            {
                var game=root.AddComponent<GameSession>();GameSession.Instance=game;
                game.Player=root.AddComponent<PlayerController>();game.Economy=new Economy(500);
                game.Economy.Unlock("farm_level2");var upgrade=Definitions.Upgrade("barn");
                Assert.That(game.Contribute(Definitions.Upgrade("supermarket"),500),Is.Zero);
                Assert.That(game.Contribute(Definitions.Upgrade("farmer"),500),Is.Zero);
                Assert.That(game.Contribute(upgrade,200),Is.EqualTo(200));
                Assert.That(game.Economy.Has("barn"),Is.False);Assert.That(game.Economy.Money,Is.EqualTo(300));
                Assert.That(game.Contribute(upgrade,1000),Is.EqualTo(300));
                Assert.That(game.Economy.Has("barn"),Is.True);Assert.That(game.Economy.Money,Is.Zero);
                Assert.That(game.Contribute(upgrade,500),Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);GameSession.Instance=previous;}
        }

        [Test]
        public void ProgressionTrackerCombinesStationLevelsOrdersRecipesAndUnlocks()
        {
            var state=new TransactionState{legacyTransactions=49};
            var farm=new StationRuntimeState{id="field_carrot",kind="producer",item="carrot",area="farm",level=2};
            state.stations.Add(farm);
            var farmShop=Definitions.Upgrade("farm_shop");
            var missing=ProgressionTracker.MissingRequirements(state,farmShop);
            Assert.That(missing,Has.Some.Contains("Cây trồng 2/3"));Assert.That(missing,Has.Some.Contains("49/50"));
            farm.level=3;state.legacyTransactions=50;
            Assert.That(ProgressionTracker.MissingRequirements(state,farmShop),Is.Empty);
            state.unlocked.Add("farm_shop");state.legacyTransactions=200;
            state.stations.Add(new StationRuntimeState{id="animal",kind="producer",item="milk",area="farm",level=2});
            missing=ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("mill"));
            Assert.That(missing,Has.Some.Contains("Chăn nuôi cấp 3"));Assert.That(System.Array.TrueForAll(missing,x=>!x.Contains("200/200")),Is.True);
            state.stations[1].level=3;
            Assert.That(ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("mill")),Is.Empty);

            state.unlocked.Add("dairy");state.legacyTransactions=600;
            state.stations.Add(new StationRuntimeState{id="mill",kind="machine",definitionId="mill",area="processing",batches=49});
            state.stations.Add(new StationRuntimeState{id="cheesemaker",kind="machine",definitionId="cheesemaker",area="processing",batches=50});
            state.stations.Add(new StationRuntimeState{id="saucemaker",kind="machine",definitionId="saucemaker",area="processing",batches=50});
            missing=ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("supermarket"));
            Assert.That(missing,Has.Some.Contains("49/50"));
            state.stations[2].batches=50;
            Assert.That(ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("supermarket")),Is.Empty);
        }

        [Test]
        public void PaymentRetainsDeliveredItemsWithCustomerAndNeverPaysTwice()
        {
            var economy = new Economy(); var basket = new Inventory(12);
            basket.TryAdd("carrot", 2); basket.TryAdd("tomato", 1);
            int price=2*Definitions.Item("carrot").price+Definitions.Item("tomato").price;
            Assert.IsTrue(economy.RecordPayment(17, price));
            Assert.AreEqual(0, economy.Money);
            Assert.AreEqual(2*Definitions.Item("carrot").price+Definitions.Item("tomato").price, economy.Revenue);
            Assert.AreEqual(1, economy.Transactions);
            Assert.AreEqual(3, basket.Total);
            Assert.IsFalse(economy.RecordPayment(17, price));
            Assert.AreEqual(price,economy.PendingCash);
            Assert.AreEqual(3,basket.Total);
            Assert.AreEqual(price,economy.CollectCash(price));
            Assert.AreEqual(price,economy.Money);
        }

        [Test]
        public void SaveRoundTripPreservesProgressAndMakesNoBackup()
        {
            string root = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "work", "tests");
            string path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var inventory = new Inventory(30); inventory.TryAdd("cheese", 7);
                var original = new SaveData { money = 123, revenue = 450, transactions = 19, nextReceipt = 55, businessStage = 4, restaurantMeals = 3 };
                original.unlocked.Add("mill");
                original.inventories.Add(new InventorySave("storage", inventory));
                original.production.Add(new ProductionSave { id = "mill", running = true, remaining = 2.7f });
                original.transit.Add(new ItemAmount("bread", 2));
                SaveStore.Write(path, original);
                original.money = 128; SaveStore.Write(path, original);
                var loaded = SaveStore.Read(path);
                Assert.AreEqual(128, loaded.money);
                Assert.AreEqual(450, loaded.revenue);
                Assert.AreEqual(55, loaded.nextReceipt);
                Assert.AreEqual("mill", loaded.unlocked[0]);
                Assert.AreEqual(7, loaded.inventories[0].items[0].count);
                Assert.AreEqual(2.7f, loaded.production[0].remaining);
                Assert.AreEqual(2, loaded.transit[0].count);
                Assert.IsFalse(File.Exists(path + ".tmp"));
                Assert.IsFalse(File.Exists(path + ".bak"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
        }

        [Test]
        public void UnsupportedSaveVersionFailsInsteadOfResettingProgress()
        {
            string root = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "work", "tests");
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(path, "{\"version\":99,\"money\":777,\"unlocked\":[],\"inventories\":[]}");
                Assert.Throws<InvalidDataException>(() => SaveStore.Read(path));
                Assert.IsTrue(File.ReadAllText(path).Contains("777"));
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test]
        public void RestorePreservesOwnerStockEvenWhenCapacityWasReduced()
        {
            var inventory = new Inventory(3);
            inventory.Restore(new[] { new ItemAmount("carrot", 3), new ItemAmount("bread", 2) });
            Assert.AreEqual(5, inventory.Total);
            Assert.AreEqual(0, inventory.Free);
            Assert.IsFalse(inventory.TryAdd("egg", 1));
            Assert.AreEqual(2, inventory.Count("bread"));
        }
    }
}

