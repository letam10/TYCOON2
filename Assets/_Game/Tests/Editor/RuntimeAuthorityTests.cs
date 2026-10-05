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
        StorageStation storage, shopStorage;
        CheckoutStation counter;
        MachineStation machine;
        ProductionStation crop, beef;
        ShelfStation livestockShelf;
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
            shopStorage=Add<StorageStation>("FarmShopStorage");shopStorage.Id="storage_farm_shop";shopStorage.AreaId="farm_shop";shopStorage.Inventory=new Inventory(20);game.Stations.Add(shopStorage);
            counter = Add<CheckoutStation>("Counter"); counter.Id = "counter"; counter.ShopId = "farm"; counter.Inventory=new Inventory(36); game.Stations.Add(counter); game.Checkouts.Add(counter);
            livestockShelf=Add<ShelfStation>("LivestockShelf");livestockShelf.Id="shelf_livestock";livestockShelf.ShopId="farm";livestockShelf.AreaId="farm_shop";livestockShelf.AllowedItems=new[]{"carrot","milk","egg","beef"};livestockShelf.Inventory=new Inventory(36);game.Stations.Add(livestockShelf);game.Shelves.Add(livestockShelf);
            beef=Add<ProductionStation>("BeefProducer");beef.Id="ranch_beef";beef.ItemId="beef";beef.AreaId="farm";beef.Requirement="barn";beef.Interval=45;beef.Cycle=0;beef.Inventory=new Inventory(36);game.Stations.Add(beef);game.Producers.Add(beef);
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
        [Test] public void ReservedSecondSkuCanBeDeliveredWithoutTouchingFirstLine()
        {
            var customer=Add<CustomerAgent>("MultiCustomer");customer.Restore(new CustomerSave{receipt=99,shop="farm",lane=counter.Id,
                phase=(int)CustomerAgent.State.Queue,remaining=90,order=new(){new("carrot",1,10),new("wheat",1,12)}});
            var worker=Add<WorkerAgent>("ReservationWorker");worker.WorkerId="second-sku";worker.UpgradeId="cashier";game.Transactions.BindWorker(worker);
            string reservation=game.Transactions.Reserve(machine.Input,customer.Basket,"wheat",1,worker.GetEntityId(),worker.Carry);
            Assert.That(reservation,Is.Not.Null);Assert.That(game.Transactions.Transfer(machine.Input,worker.Carry,"wheat",1,reservation:reservation),Is.EqualTo(1));
            Assert.That(game.Transactions.Deliver(99,worker.Carry,reservation),Is.EqualTo(1));
            Assert.That(customer.Order.Lines[0].delivered,Is.Zero);Assert.That(customer.Order.Lines[1].delivered,Is.EqualTo(1));
            Assert.That(customer.Basket.Count("wheat"),Is.EqualTo(1));TransactionCore.Validate(game.Transactions.Snapshot());
        }
        [Test] public void JournalReplayTrimsOnlyUncommittedTailAndCheckpointKeepsDedup()
        {
            Inventory.Transfer(storage.Inventory,game.Player.Carry,"carrot",2);
            File.AppendAllText(game.SavePath+".journal","{interrupted-tail");
            var saved=SaveStore.Read(game.SavePath);Assert.That(saved.transactionState.stacks.Where(s=>s.owner=="player").Sum(s=>s.quantity),Is.EqualTo(2));
            game.Transactions.Detach();game.InitializeTransactions(saved.transactionState);
            Assert.That(File.ReadAllText(game.SavePath+".journal"),Does.Not.Contain("interrupted-tail"));
            Inventory.Transfer(game.Player.Carry,storage.Inventory,"carrot",1);game.Transactions.Checkpoint();
            Assert.That(new FileInfo(game.SavePath+".journal").Length,Is.Zero);saved=SaveStore.Read(game.SavePath);
            Assert.That(saved.transactionState.revision,Is.EqualTo(2));Assert.That(saved.transactionState.receipts.Count,Is.EqualTo(2));
            Assert.That(saved.inventories.Single(i=>i.id=="player").items.Sum(i=>i.count),Is.EqualTo(1));
        }
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
            Assert.That(machine.Phase,Is.EqualTo(MachinePhase.Ready));
            Assert.That(machine.Operate(.1f, game.Player.GetEntityId()), Is.True); string job = game.Transactions.Snapshot().stations.Find(x => x.id == machine.Id).jobId;
            Assert.That(machine.Phase,Is.EqualTo(MachinePhase.Operating));
            machine.ReleaseOperator(game.Player.GetEntityId()); float remaining = machine.Remaining;
            Assert.That(machine.Phase,Is.EqualTo(MachinePhase.Operating));Assert.That(machine.HasOperator,Is.False);
            game.SaveGame(); game.LoadGame(); Assert.That(game.SaveBlocked, Is.False); Assert.That(machine.Remaining, Is.EqualTo(remaining));
            Assert.That(machine.Input.Count("wheat"), Is.EqualTo(4)); Assert.That(machine.Inventory.ReservedSpace("flour"), Is.EqualTo(3));
            Assert.That(machine.Operate(6, game.Player.GetEntityId()), Is.True); Assert.That(machine.Inventory.Count("flour"), Is.EqualTo(3));
            Assert.That(machine.Phase,Is.EqualTo(MachinePhase.CompletedWaitingPickup));
            var finish = game.Transactions.Command(TransactionKind.CompleteMachine, "player", machine.Id); finish.secondary = job;
            Assert.That(game.Transactions.Execute(finish).amount, Is.Zero); Assert.That(machine.Batches, Is.EqualTo(1));
            Assert.That(Inventory.Transfer(machine.Inventory,game.Player.Carry,"flour",3),Is.EqualTo(3));Assert.That(machine.Phase,Is.EqualTo(MachinePhase.Ready));
        }
        [Test] public void PassingPurchasePadDoesNotSpendAndStandingStillContinuesPartialContribution()
        {
            Assert.That(farmSpeedPad.HoldToBuy(.4f),Is.False);Assert.That(game.Economy.Money,Is.EqualTo(1000));farmSpeedPad.StopContributing();
            Assert.That(farmSpeedPad.HoldToBuy(.6f),Is.False);Assert.That(farmSpeedPad.HoldToBuy(.4f),Is.False);
            Assert.That(farmSpeedPad.HoldToBuy(.2f),Is.True);Assert.That(game.Contribution("farm_speed2"),Is.EqualTo(7));
            farmSpeedPad.StopContributing();Assert.That(game.Contribution("farm_speed2"),Is.EqualTo(7));
        }
        [Test] public void MeatBundleOpensFullRouteAndRestockStartsAtZeroPopulation()
        {
            var bundle=Definitions.Upgrade("barn");Assert.That(game.Contribute(bundle,500),Is.EqualTo(500));
            Assert.That(beef.IsUnlocked,Is.True);Assert.That(livestockShelf.IsUnlocked,Is.True);Assert.That(counter.AcceptsItem("beef"),Is.True);
            var customer=Add<CustomerAgent>("BeefCustomer");customer.Restore(new CustomerSave{receipt=game.NextReceipt++,shop="farm",lane=counter.Id,
                phase=(int)CustomerAgent.State.Queue,remaining=90,order=new(){new("beef",1,game.ItemPrice("beef"))}});customer.transform.position=counter.QueuePoint(customer);
            Assert.That(customer.Order.Lines.Single().id,Is.EqualTo("beef"));Assert.That(customer.Order.Lines.Single().unitPrice,Is.EqualTo(game.ItemPrice("beef")));
            int requested=customer.Order.Lines.Single().requested;
            for(int i=0;i<requested;i++)
            {
                Assert.That(Inventory.Transfer(storage.Inventory,game.Player.Carry,"carrot",1),Is.EqualTo(1));
                Assert.That(beef.FeedPlayer(game.Player.Carry,game.Player.GetEntityId()).Worked,Is.True);
                if(beef.Cycle>0)Assert.That(game.Transactions.TickProducer(beef,beef.Cycle),Is.True);
                Assert.That(beef.Cycle,Is.Zero);
                Assert.That(beef.PickupPlayer(game.Player.Carry,1,game.Player.GetEntityId()).Worked,Is.True);
                Assert.That(game.Player.Carry.Count("beef"),Is.EqualTo(1));
                Assert.That(counter.Serve(game.Player.Carry,game.Player.GetEntityId()),Is.True);
                Assert.That(customer.Basket.Count("beef"),Is.EqualTo(i+1));
                if(i+1<requested)Assert.That(customer.Order.Paid,Is.False);
            }
            Assert.That(customer.Order.Paid,Is.True);Assert.That(counter.Cash,Is.EqualTo(requested*game.ItemPrice("beef")));
            int expectedCash=counter.Cash;Assert.That(game.Transactions.Collect(counter.Id),Is.EqualTo(expectedCash));
            Assert.That(game.Economy.Money,Is.EqualTo(1000-500+expectedCash));
            while(beef.Herd>0)
            {
                Assert.That(Inventory.Transfer(storage.Inventory,game.Player.Carry,"carrot",1),Is.EqualTo(1));
                Assert.That(beef.FeedPlayer(game.Player.Carry,game.Player.GetEntityId()).Worked,Is.True);
                if(beef.Cycle>0)game.Transactions.TickProducer(beef,beef.Cycle);
                Assert.That(beef.PickupPlayer(game.Player.Carry,1,game.Player.GetEntityId()).Worked,Is.True);
                Assert.That(Inventory.Transfer(game.Player.Carry,storage.Inventory,"beef",1),Is.EqualTo(1));
            }
            Assert.That(beef.Herd,Is.Zero);
            Assert.That(Inventory.Transfer(storage.Inventory,game.Player.Carry,"carrot",1),Is.EqualTo(1));
            Assert.That(beef.FeedPlayer(game.Player.Carry,game.Player.GetEntityId()).Worked,Is.True);
            Assert.That(beef.RestockPlayer(game.Player.GetEntityId()).Worked,Is.True);
            Assert.That(beef.Herd,Is.Zero);Assert.That(beef.Breeding,Is.EqualTo(60));
            Assert.That(game.Transactions.TickProducer(beef,60),Is.True);Assert.That(beef.Herd,Is.EqualTo(1));
        }
        [Test] public void StorageTracksAreaAndItemLocationsAndShowsLiveReservations()
        {
            var initial=game.Transactions.Snapshot();Assert.That(initial.owners.Single(x=>x.id==storage.Id).location,Is.EqualTo("farm"));
            Assert.That(initial.stacks.Single(x=>x.owner==storage.Id&&x.item=="carrot").location,Is.EqualTo("farm/carrot"));
            Assert.That(Inventory.Transfer(storage.Inventory,game.Player.Carry,"carrot",1),Is.EqualTo(1));
            Assert.That(Inventory.Transfer(game.Player.Carry,shopStorage.Inventory,"carrot",1),Is.EqualTo(1));
            Assert.That(game.Transactions.Snapshot().stacks.Single(x=>x.owner==shopStorage.Id&&x.item=="carrot").location,Is.EqualTo("farm_shop/carrot"));
            var reservation=game.Transactions.Reserve(storage.Inventory,shopStorage.Inventory,"carrot",2,game.Player.GetEntityId());
            Assert.That(reservation,Is.Not.Null);Assert.That(storage.Inventory.Reserved("carrot"),Is.EqualTo(2));
            Assert.That(shopStorage.Inventory.ReservedSpace("carrot"),Is.EqualTo(2));Assert.That(shopStorage.ReservationSummary,Does.Contain("2 chờ nhận"));
            Assert.That(game.Transactions.Release(reservation,game.Player.GetEntityId()),Is.True);
            Assert.That(shopStorage.Inventory.IncomingTotal,Is.Zero);
        }
        [Test] public void FirstHireCountsOnlyPlayerJobsForItsRoleAndArea()
        {
            var state=game.Transactions.Snapshot();var cropState=state.stations.Single(x=>x.id==crop.Id);
            cropState.workCount=200;cropState.playerWorkCount=29;state.unlocked.Add("farm_level2");state.unlocked.Add("farm_level3");
            var reasons=ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("farmer"));
            Assert.That(reasons,Has.Some.Contains("29/30"));
            cropState.playerWorkCount=30;
            Assert.That(ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("farmer")),Is.Empty);
        }
        [Test] public void BakerHireCountsOnlyPlayerJobsAtItsBakeryArea()
        {
            var crew=GameSession.CrewFor(Definitions.Upgrade("cook_bakery"));
            Assert.That(crew.role,Is.EqualTo("Cook"));Assert.That(crew.area,Is.EqualTo("bakery"));
            var state=game.Transactions.Snapshot();var machineState=state.stations.Single(x=>x.id==machine.Id);
            machineState.area="bakery";machineState.level=3;machineState.workCount=100;machineState.playerWorkCount=100;machineState.batches=29;machineState.playerBatches=29;
            state.stations.Add(new StationRuntimeState{id="storage_bakery",kind="storage",area="bakery"});state.unlocked.Add("bakery");
            Assert.That(ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("cook_bakery")),Has.Some.Contains("29/30"));
            machineState.playerBatches=30;machineState.batches=30;
            Assert.That(ProgressionTracker.MissingRequirements(state,Definitions.Upgrade("cook_bakery")),Is.Empty);
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
            Assert.That(game.Progression.Evaluate(speed).State,Is.EqualTo(PurchaseState.Contributing));Assert.That(farmSpeedPad.Prompt,Does.Contain("ĐANG GÓP"));
            game.SaveGame();game.LoadGame();Assert.That(game.Contribution(speed.id),Is.EqualTo(20));
            Assert.That(game.Contribute(speed,40),Is.EqualTo(40));Assert.That(game.Progression.AxisLevel("farm",UpgradeAxis.Speed),Is.EqualTo(2));Assert.That(farmSpeedPad.Prompt,Is.EqualTo("ĐÃ MUA"));
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
            var seed=game.Transactions.Snapshot();game.Transactions.Detach();
            using var store = new GameplayTransactionStore(game); var core = new TransactionCore(seed, store, () => game.Transactions.Now,true);
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
