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
    public sealed class RuntimeAuthorityTests
    {
        GameSession game, previous;
        StorageStation storage;
        CheckoutStation counter;
        MachineStation machine;
        ProductionStation crop;
        PurchasePad farmSpeedPad;
        string directory;
        T Add<T>(string id) where T : Component
        { var root = new GameObject(id); root.SetActive(false); root.transform.SetParent(game.transform); return root.AddComponent<T>(); }
        [SetUp] public void SetUp()
        {
            previous = GameSession.Instance;
            var root = new GameObject("RuntimeAuthorityTest"); root.SetActive(false);
            game = root.AddComponent<GameSession>(); GameSession.Instance = game;
            game.Player = Add<PlayerController>("Player"); game.Economy = new Economy(1000);
            storage = Add<StorageStation>("Storage"); storage.Id = "storage"; storage.Inventory = new Inventory(20); game.Stations.Add(storage);
            storage.Inventory.TryAdd("carrot", 6);
            counter = Add<CheckoutStation>("Counter"); counter.Id = "counter"; counter.ShopId = "farm"; counter.Inventory=new Inventory(36); game.Stations.Add(counter); game.Checkouts.Add(counter);
            machine = Add<MachineStation>("Machine"); machine.Id = "machine"; machine.Recipe = Definitions.Recipe("mill");
            machine.Inventory = new Inventory(3); machine.Input = new Inventory(8); machine.ConfigureInputLimits(); machine.Input.TryAdd("wheat", 8);
            game.Stations.Add(machine); game.Machines.Add(machine);
            crop = Add<ProductionStation>("Crop"); crop.Id = "crop"; crop.ItemId = "carrot"; crop.Inventory = new Inventory(2); game.Stations.Add(crop); game.Producers.Add(crop);
            farmSpeedPad=Add<PurchasePad>("pad_farm_speed2");farmSpeedPad.Id="pad_farm_speed2";farmSpeedPad.Upgrade=Definitions.Upgrade("farm_speed2");game.Stations.Add(farmSpeedPad);
            directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "work", "stage02-03", Guid.NewGuid().ToString("N"));
            game.SavePath = Path.Combine(directory, "save.json");
            game.InitializeTransactions();
        }
        [TearDown] public void TearDown()
        { game.Transactions?.Detach(); Object.DestroyImmediate(game.gameObject); GameSession.Instance = previous; if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        [Test] public void LiveViewsCannotMutateAuthorityAndTransferHasOneReceipt()
        {
            Assert.Throws<InvalidOperationException>(() => game.Player.Carry.TryAdd("milk", 1));
            Assert.Throws<InvalidOperationException>(() => game.Economy.TrySpend(10));
            Assert.Throws<InvalidOperationException>(() => crop.Phase = 3);
            var c = game.Transactions.Command(TransactionKind.Take, "player", key: "pickup", effect: "pickup-business");
            c.source = storage.Id; c.destination = "player"; c.item = "carrot"; c.quantity = 2;
            var first = game.Transactions.Execute(c); c.key = "retry-other-key";
            Assert.That(game.Transactions.Execute(c).id, Is.EqualTo(first.id));
            Assert.That(storage.Inventory.Count("carrot"), Is.EqualTo(4)); Assert.That(game.Player.Carry.Count("carrot"), Is.EqualTo(2));
            var saved = SaveStore.Read(game.SavePath); Assert.That(saved.transactionState.revision, Is.EqualTo(1));
            Assert.That(saved.transactionState.receipts.Count, Is.EqualTo(1)); Assert.That(saved.transactionState.outbox.Count, Is.EqualTo(1));
            Assert.That(saved.inventories.Find(x => x.id == "player").items[0].count, Is.EqualTo(2));
        }
        CustomerAgent Customer(long receipt, int count = 2)
        {
            var customer = Add<CustomerAgent>("Customer" + receipt);
            customer.Restore(new CustomerSave { receipt = receipt, shop = "farm", lane = counter.Id, phase = (int)CustomerAgent.State.Queue,
                remaining = 90, order = new() { new OrderLine("carrot", count, 10) } });
            customer.transform.position = counter.QueuePoint(customer); return customer;
        }
        [Test] public void RealCheckoutPartialsTimeoutAndCollectionRespectCoreOwnership()
        {
            var customer = Customer(7); Inventory.Transfer(storage.Inventory, game.Player.Carry, "carrot", 1);
            Assert.That(counter.Serve(game.Player.Carry), Is.True); Assert.That(customer.Basket.Total, Is.EqualTo(1));
            Assert.That(counter.Cash, Is.Zero); Assert.That(game.Transactions.Fail(7), Is.True);
            Assert.That(game.Transactions.Fail(7), Is.False); Assert.That(customer.Basket.Total, Is.EqualTo(1));
            Assert.That(game.Economy.LostItems, Is.EqualTo(1)); Assert.That(game.Economy.PendingCash, Is.Zero);
            Assert.That(game.Transactions.Transfer(customer.Basket, game.Player.Carry, "carrot", 1), Is.Zero);
            customer.Leave(); var paid = Customer(8, 1); Inventory.Transfer(storage.Inventory, game.Player.Carry, "carrot", 1);
            Assert.That(counter.Serve(game.Player.Carry), Is.True); Assert.That(paid.Order.Paid, Is.True); Assert.That(counter.Cash, Is.EqualTo(10));
            Assert.That(game.Transactions.Collect(counter.Id), Is.EqualTo(10)); Assert.That(game.Transactions.Collect(counter.Id), Is.Zero);
            Assert.That(game.Economy.Money, Is.EqualTo(1010)); Assert.That(game.Economy.Transactions, Is.EqualTo(1));
            TransactionCore.Validate(game.Transactions.Snapshot());
        }
        [Test] public void UnservedExpiredOrderRecordsZeroLoss()
        {
            Customer(9,3);
            Assert.That(game.Transactions.Fail(9),Is.True);
            Assert.That(game.Economy.IsLost(9),Is.True);Assert.That(game.Economy.LostItems,Is.Zero);Assert.That(game.Economy.LossCount,Is.Zero);
            Assert.That(game.Economy.IsPaid(9),Is.False);Assert.That(counter.Cash,Is.Zero);Assert.That(game.Economy.Money,Is.EqualTo(1000));
            Assert.That(game.Transactions.Fail(9),Is.False);
        }
        [Test] public void FarmCounterStockAndDirectServeKeepSeparateOwnersAndSettleOnce()
        {
            var customer=Customer(11,2);
            Assert.That(Inventory.Transfer(storage.Inventory,game.Player.Carry,"carrot",2),Is.EqualTo(2));
            Assert.That(Inventory.Transfer(game.Player.Carry,counter.Inventory,"carrot",1),Is.EqualTo(1));
            Assert.That(storage.Inventory.Count("carrot"),Is.EqualTo(4));
            Assert.That(counter.Inventory.Count("carrot"),Is.EqualTo(1));Assert.That(game.Player.Carry.Count("carrot"),Is.EqualTo(1));
            Assert.That(counter.Serve(counter.Inventory,game.Player.GetEntityId()),Is.True);
            Assert.That(customer.Basket.Count("carrot"),Is.EqualTo(1));Assert.That(customer.Order.Paid,Is.False);Assert.That(counter.Cash,Is.Zero);
            Assert.That(counter.Serve(game.Player.Carry,game.Player.GetEntityId()),Is.True);
            Assert.That(customer.Basket.Count("carrot"),Is.EqualTo(2));Assert.That(customer.Order.Paid,Is.True);
            Assert.That(game.Player.Carry.Total,Is.Zero);Assert.That(counter.Cash,Is.EqualTo(20));Assert.That(game.Economy.Money,Is.EqualTo(1000));
            Assert.That(game.Transactions.Collect(counter.Id),Is.EqualTo(20));Assert.That(game.Transactions.Collect(counter.Id),Is.Zero);
            Assert.That(game.Economy.Money,Is.EqualTo(1020));TransactionCore.Validate(game.Transactions.Snapshot());
        }
        [Test] public void MachineHandsOffOperatorAndRecoversJobReservationAndOutputOnce()
        {
            Assert.That(machine.Operate(.1f, game.Player.GetEntityId()), Is.True); string job = game.Transactions.Snapshot().stations.Find(x => x.id == machine.Id).jobId;
            machine.ReleaseOperator(game.Player.GetEntityId()); float remaining = machine.Remaining;
            game.SaveGame(); game.LoadGame(); Assert.That(game.SaveBlocked, Is.False); Assert.That(machine.Remaining, Is.EqualTo(remaining));
            Assert.That(machine.Input.Count("wheat"), Is.EqualTo(4)); Assert.That(machine.Inventory.ReservedSpace("flour"), Is.EqualTo(3));
            Assert.That(machine.Operate(6, game.Player.GetEntityId()), Is.True); Assert.That(machine.Inventory.Count("flour"), Is.EqualTo(3));
            var finish = game.Transactions.Command(TransactionKind.CompleteMachine, "player", machine.Id); finish.secondary = job;
            Assert.That(game.Transactions.Execute(finish).amount, Is.Zero); Assert.That(machine.Batches, Is.EqualTo(1));
        }
        [Test] public void PurchaseRecoveryAndCarryUpgradeAreSingleWriterAndPersisted()
        {
            var upgrade = Definitions.Upgrade("carry10"); Assert.That(game.Contribute(upgrade, 40), Is.EqualTo(40));
            game.SaveGame(); game.LoadGame(); Assert.That(game.SaveBlocked, Is.False); Assert.That(game.Contribution(upgrade.id), Is.EqualTo(40));
            Assert.That(game.Contribute(upgrade, 100), Is.EqualTo(60)); Assert.That(game.Player.Carry.Capacity, Is.EqualTo(10));
            Assert.That(game.Contribute(upgrade, 100), Is.Zero); Assert.That(game.Economy.Money, Is.EqualTo(900));
            game.LoadGame(); Assert.That(game.Player.Carry.Capacity, Is.EqualTo(10)); Assert.That(game.Purchases.Single().complete, Is.True);
        }
        [Test] public void FarmQualitySpeedAndCapacityPurchasesChangeOnlyTheirOwnAxis()
        {
            Assert.That(game.ItemPrice("carrot"),Is.EqualTo(10));Assert.That(crop.Level,Is.EqualTo(1));
            var speed=Definitions.Upgrade("farm_speed2");Assert.That(game.Contribute(speed,20),Is.EqualTo(20));
            Assert.That(game.Progression.Evaluate(speed).State,Is.EqualTo(PurchaseState.Contributing));Assert.That(farmSpeedPad.Prompt,Does.Contain("CONTRIBUTING"));
            game.SaveGame();game.LoadGame();Assert.That(game.Contribution(speed.id),Is.EqualTo(20));
            Assert.That(game.Contribute(speed,40),Is.EqualTo(40));Assert.That(game.Progression.AxisLevel("farm",UpgradeAxis.Speed),Is.EqualTo(2));Assert.That(farmSpeedPad.Prompt,Is.EqualTo("PURCHASED"));
            Assert.That(game.Progression.FarmGrowSeconds,Is.LessThan(2));Assert.That(game.ItemPrice("carrot"),Is.EqualTo(10));
            Assert.That(game.Player.Carry.Capacity,Is.EqualTo(6));Assert.That(crop.Level,Is.EqualTo(1));Assert.That(crop.Inventory.Capacity,Is.EqualTo(24));
            Assert.That(game.Contribute(Definitions.Upgrade("farm_value2"),250),Is.EqualTo(250));
            Assert.That(game.ItemPrice("carrot"),Is.EqualTo(13));Assert.That(game.Progression.AxisLevel("farm",UpgradeAxis.Speed),Is.EqualTo(2));
            Assert.That(crop.Inventory.Capacity,Is.EqualTo(24));Assert.That(crop.Level,Is.EqualTo(1));
            Assert.That(game.Contribute(Definitions.Upgrade("farm_capacity2"),150),Is.EqualTo(150));
            Assert.That(crop.Inventory.Capacity,Is.EqualTo(36));Assert.That(game.ItemPrice("carrot"),Is.EqualTo(13));
            Assert.That(crop.Level,Is.EqualTo(1));Assert.That(game.Player.Carry.Capacity,Is.EqualTo(6));
            Assert.That(game.Contribute(Definitions.Upgrade("farm_level2"),100),Is.EqualTo(100));
            Assert.That(crop.Level,Is.EqualTo(2));Assert.That(game.Progression.StationLevel("farm"),Is.EqualTo(2));
            Assert.That(game.ItemPrice("carrot"),Is.EqualTo(13));Assert.That(crop.Inventory.Capacity,Is.EqualTo(36));
            Assert.That(game.Contribute(Definitions.Upgrade("farm_level3"),300),Is.EqualTo(300));
            Assert.That(crop.Level,Is.EqualTo(3));Assert.That(game.Progression.StationLevel("farm"),Is.EqualTo(3));
            Assert.That(game.ItemPrice("carrot"),Is.EqualTo(13));Assert.That(game.Progression.AxisLevel("farm",UpgradeAxis.Speed),Is.EqualTo(2));
            Assert.That(game.Progression.Evaluate(Definitions.Upgrade("farm_shop")).Requirements,Has.Some.Contains("0/50"));
        }
        [TestCase(CommitBoundary.TemporaryFlushed, false)]
        [TestCase(CommitBoundary.Replaced, true)]
        public void GameplayEnvelopeCrashRetryNeverSplitsViewFromCore(CommitBoundary boundary, bool committed)
        {
            var store = new GameplayTransactionStore(game); var core = new TransactionCore(game.Transactions.Snapshot(), store, () => game.Transactions.Now);
            var c = game.Transactions.Command(TransactionKind.Take, "player", key: "crash"); c.source = storage.Id; c.destination = "player"; c.item = "carrot"; c.quantity = 1;
            store.Fault = at => { if (at == boundary) throw new IOException("interrupted gameplay commit"); };
            Assert.Throws<IOException>(() => core.Execute(c)); store.Fault = null;
            var saved = SaveStore.Read(game.SavePath); Assert.That(saved.inventories.Find(x => x.id == "player").items.Sum(x => x.count), Is.EqualTo(committed ? 1 : 0));
            core = new TransactionCore(null, store); var receipt = core.Execute(c); Assert.That(core.Execute(c).id, Is.EqualTo(receipt.id));
            Assert.That(core.Snapshot().stacks.Where(x => x.owner == "player").Sum(x => x.quantity), Is.EqualTo(1));
            Assert.That(File.Exists(game.SavePath + ".tmp"), Is.False);
        }
        [Test] public void InvalidCoreReferenceStopsGameplayWithoutRewritingFile()
        {
            string json = File.ReadAllText(game.SavePath); string invalid = json.Replace("\"owner\": \"storage\"", "\"owner\": \"missing\""); File.WriteAllText(game.SavePath, invalid);
            LogAssert.Expect(LogType.Exception, new Regex("Owner không tồn tại")); game.LoadGame();
            Assert.That(game.SaveBlocked, Is.True); Assert.That(game.Player.CanControl, Is.False);
            game.SaveGame(); Assert.That(File.ReadAllText(game.SavePath), Is.EqualTo(invalid));
        }
    }
}
