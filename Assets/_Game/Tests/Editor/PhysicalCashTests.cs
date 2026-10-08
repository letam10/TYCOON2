using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class PhysicalCashTests
    {
        static TransactionState CashSeed(long hand = 0, long safe = 0)
        {
            var state = TransactionCoreTests.Seed();
            state.schemaVersion = PhysicalCashRules.Version;
            state.money = 0;
            state.cashInHand = hand;
            state.cashInSafe = safe;
            state.owners.Add(new OwnerState
            {
                id = PhysicalCashRules.SafeId,
                actor = "simulation",
                location = PhysicalCashRules.SafeId,
                kind = OwnerKind.Station
            });
            state.stations.Add(new StationRuntimeState
            {
                id = PhysicalCashRules.SafeId,
                kind = "purchase",
                input = PhysicalCashRules.SafeId,
                output = PhysicalCashRules.SafeId
            });
            CarryLimits.Apply(state);
            return state;
        }

        static TransactionCommand Command(TransactionCore core, TransactionKind kind, string key)
        {
            return new TransactionCommand
            {
                kind = kind,
                key = key,
                effectId = "effect:" + key,
                actor = "player",
                target = PhysicalCashRules.SafeId,
                expectedRevision = core.Revision
            };
        }

        static string Json(TransactionCore core) => JsonUtility.ToJson(core.Snapshot());

        [Test]
        public void DepositAndWithdrawMoveCashOnceAndRetainReceiptOnRetry()
        {
            var core = new TransactionCore(CashSeed(275, 900));
            var deposit = Command(core, TransactionKind.DepositCash, "deposit");
            var receipt = core.Execute(deposit);
            Assert.That(receipt.cashAmount, Is.EqualTo(275));
            Assert.That(core.Execute(deposit).id, Is.EqualTo(receipt.id));
            Assert.That(core.Snapshot().cashInHand, Is.Zero);
            Assert.That(core.Snapshot().cashInSafe, Is.EqualTo(1175));

            var withdraw = Command(core, TransactionKind.WithdrawCash, "withdraw");
            Assert.That(core.Execute(withdraw).cashAmount, Is.EqualTo(1175));
            Assert.That(core.Snapshot().cashInHand, Is.EqualTo(1175));
            Assert.That(core.Snapshot().cashInSafe, Is.Zero);
            Assert.That(core.Snapshot().money, Is.Zero);
        }

        [Test]
        public void HandsCannotHoldGoodsAndCashOrTransferCashWithGoods()
        {
            var seed = CashSeed(50, 100);
            seed.stacks.Add(new ItemStackState
            {
                id = "player-carrot",
                owner = "player",
                location = "location:player",
                item = "carrot",
                quantity = 1
            });
            Assert.That(Assert.Throws<TransactionRejectedException>(() => new TransactionCore(seed)).Code,
                Is.EqualTo("cash"));

            seed.cashInHand = 0;
            var core = new TransactionCore(seed);
            var withdraw = Command(core, TransactionKind.WithdrawCash, "blocked-withdraw");
            string before = Json(core);
            Assert.That(Assert.Throws<TransactionRejectedException>(() => core.Execute(withdraw)).Code,
                Is.EqualTo("hands"));
            Assert.That(Json(core), Is.EqualTo(before));

            var deposit = Command(core, TransactionKind.DepositCash, "blocked-deposit");
            Assert.That(Assert.Throws<TransactionRejectedException>(() => core.Execute(deposit)).Code,
                Is.EqualTo("hands"));
            Assert.That(Json(core), Is.EqualTo(before));
        }

        [Test]
        public void CashTransfersPreserveAmountsAboveIntMaximum()
        {
            const long amount = (long)int.MaxValue + 987654321L;
            var core = new TransactionCore(CashSeed(0, amount));
            var withdraw = Command(core, TransactionKind.WithdrawCash, "large-withdraw");
            Assert.That(core.Execute(withdraw).cashAmount, Is.EqualTo(amount));
            Assert.That(core.Snapshot().cashInHand, Is.EqualTo(amount));
            Assert.That(core.Snapshot().cashInSafe, Is.Zero);
        }

        [Test]
        public void DepositOverflowIsRejectedWithoutChangingState()
        {
            var core = new TransactionCore(CashSeed(1, long.MaxValue));
            var deposit = Command(core, TransactionKind.DepositCash, "overflow-deposit");
            string before = Json(core);
            Assert.That(Assert.Throws<TransactionRejectedException>(() => core.Execute(deposit)).Code,
                Is.EqualTo("cash-overflow"));
            Assert.That(Json(core), Is.EqualTo(before));
        }

        [Test]
        public void ContributionSpendsHandCashOnlyAndClampsToRemainingCost()
        {
            const long hand = 1000;
            const long safe = 5000;
            var core = new TransactionCore(CashSeed(hand, safe));
            var upgrade = Definitions.Upgrade("carry10");
            var contribute = Command(core, TransactionKind.ContributePurchase, "carry-contribution");
            contribute.target = upgrade.id;
            contribute.quantity = int.MaxValue;

            int spent = core.Execute(contribute).amount;
            Assert.That(spent, Is.EqualTo(upgrade.cost));
            Assert.That(core.Snapshot().cashInHand, Is.EqualTo(hand - spent));
            Assert.That(core.Snapshot().cashInSafe, Is.EqualTo(safe));
            Assert.That(core.Snapshot().purchases.Single(x => x.id == upgrade.id).contributed,
                Is.EqualTo(upgrade.cost));
        }

        [Test]
        public void MaximumGrantIsRepeatableAndPreservesCashAndStatistics()
        {
            var seed = CashSeed(37, 812);
            foreach (string id in new[] { "storage_processing", "storage_farm_shop" })
                seed.owners.Add(new OwnerState
                {
                    id = id, kind = OwnerKind.Storage, actor = "simulation", location = id,
                    capacity = 96, writers = new() { "player" }
                });
            seed.revenue = 246;
            seed.cashCollected = 135;
            seed.assistedCash = 79;
            var core = new TransactionCore(seed);
            core.Execute(Command(core, TransactionKind.GrantMaximum, "maximum-first"));
            var first = core.Snapshot();
            core.Execute(Command(core, TransactionKind.GrantMaximum, "maximum-repeat"));
            var second = core.Snapshot();

            Assert.That(second.maximumBuilt, Is.True);
            var expectedUnlocks = Definitions.Upgrades.Where(x => x.kind != "legacy").Select(x => x.id)
                .Concat(new[] { "feed_route", "crop_wheat", "crop_corn", "crop_soybean", "crop_tomato",
                    "crop_expansion3" }).Distinct();
            Assert.That(second.unlocked, Is.EquivalentTo(expectedUnlocks));
            Assert.That(second.cashInHand, Is.EqualTo(37));
            Assert.That(second.cashInSafe, Is.EqualTo(812));
            Assert.That(second.revenue, Is.EqualTo(246));
            Assert.That(second.cashCollected, Is.EqualTo(135));
            Assert.That(second.assistedCash, Is.EqualTo(79));
            first.revision = second.revision;
            first.receipts = second.receipts;
            first.outbox = second.outbox;
            Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
        }

        [Test]
        public void PhysicalCarryCapacityDerivesFromProgressionAndCrewTier()
        {
            var state = CashSeed();
            Assert.That(CarryLimits.Player(state), Is.EqualTo(12));
            state.unlocked.Add("carry10");
            Assert.That(CarryLimits.Player(state), Is.EqualTo(20));
            state.unlocked.Add("carry16");
            Assert.That(CarryLimits.Player(state), Is.EqualTo(32));
            state.unlocked.Add("carry24");
            Assert.That(CarryLimits.Player(state), Is.EqualTo(48));

            Assert.That(CarryLimits.Worker(new CrewState { role = "Farmer", carryLevel = 1 }), Is.EqualTo(18));
            Assert.That(CarryLimits.Worker(new CrewState { role = "Farmer", carryLevel = 2 }), Is.EqualTo(30));
            Assert.That(CarryLimits.Worker(new CrewState { role = "Farmer", carryLevel = 3 }), Is.EqualTo(48));
            Assert.That(CarryLimits.Worker(new CrewState { role = "Loader", carryLevel = 1 }), Is.EqualTo(18));
            Assert.That(CarryLimits.Worker(new CrewState { role = "Loader", carryLevel = 2 }), Is.EqualTo(36));
            Assert.That(CarryLimits.Worker(new CrewState { role = "Loader", carryLevel = 3 }), Is.EqualTo(54));
        }
    }
}
