using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorkforceRulesTests
    {
        [Test]
        public void MaximumHasAllTwentySixProfessionsWithThirteenPairs()
        {
            var definitions = Definitions.Upgrades.Where(x => x.kind == "worker").ToArray();
            Assert.That(definitions.Length, Is.EqualTo(26));
            Assert.That(definitions.Count(x => WorkforceRules.Limit(x.id) == 2), Is.EqualTo(13));
            Assert.That(definitions.Count(x => WorkforceRules.Limit(x.id) == 1), Is.EqualTo(13));
            Assert.That(definitions.Sum(x => WorkforceRules.Limit(x.id)), Is.EqualTo(39));
            Assert.That(WorkforceRules.Limit("truck_bundle"), Is.EqualTo(1));
            var state = new TransactionState();
            foreach (var definition in definitions)
            {
                var crew = GameSession.CrewFor(definition);
                crew.count = 3;
                state.crews.Add(crew);
            }
            Assert.That(WorkforceRules.ExpectedActive(state), Is.EqualTo(WorkforceRules.MaximumActive));
            Assert.That(WorkforceMigration.Apply(state), Is.True);
            Assert.That(state.crews.Sum(x => x.count), Is.EqualTo(39));
            Assert.That(WorkforceMigration.Apply(state), Is.False);
        }

        [TestCase("Farmer", 1, 18)]
        [TestCase("Farmer", 2, 30)]
        [TestCase("Farmer", 3, 48)]
        [TestCase("Loader", 1, 18)]
        [TestCase("Loader", 2, 36)]
        [TestCase("Loader", 3, 54)]
        public void CapacityIsExactlyOneAndAHalfPhysicalBaseline(string role, int level, int expected)
        {
            var crew = new CrewState { role = role, carryLevel = level };
            Assert.That(CarryLimits.Worker(crew), Is.EqualTo(expected));
            Assert.That(CarryLimits.Worker(crew), Is.EqualTo(CarryLimits.Worker(crew, false) * 3));
        }

        [Test]
        public void LegacySeventyEightWorkerSaveRetainsCargoAndExactlyThirtyNineRetirements()
        {
            var state = new TransactionState { schemaVersion = 3 };
            foreach (var definition in Definitions.Upgrades.Where(x => x.kind == "worker"))
            {
                var crew = GameSession.CrewFor(definition);
                crew.count = 3;
                state.crews.Add(crew);
                for (int slot = 0; slot < 3; slot++)
                {
                    string id = definition.id + ":" + slot;
                    string ownerId = RuntimeTransactions.WorkerOwner(id);
                    state.owners.Add(new OwnerState
                    {
                        id = ownerId, actor = ownerId, kind = OwnerKind.Worker,
                        worker = new WorkerSave { id = id, upgrade = definition.id }
                    });
                    state.stacks.Add(new ItemStackState
                    {
                        id = "stack:" + id, owner = ownerId, item = "carrot", quantity = 1
                    });
                }
            }
            Assert.That(WorkforceMigration.Apply(state), Is.True);
            Assert.That(state.owners.Count, Is.EqualTo(78));
            Assert.That(state.stacks.Sum(x => x.quantity), Is.EqualTo(78));
            Assert.That(WorkforceRules.ExpectedActive(state), Is.EqualTo(39));
            Assert.That(WorkforceRules.PendingRetirements(state), Is.EqualTo(39));
            var restored = JsonUtility.FromJson<TransactionState>(JsonUtility.ToJson(state));
            Assert.That(WorkforceMigration.Apply(restored), Is.False);
            Assert.That(restored.owners.Count(x => x.worker.retiring), Is.EqualTo(39));
            Assert.That(restored.stacks.Sum(x => x.quantity), Is.EqualTo(78));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void SpeedAndProductivityScaleOnceFromUnchangedTier(int level)
        {
            float baseline = 1 + .2f * (level - 1);
            Assert.That(WorkforceRules.Productivity(level), Is.EqualTo(baseline * 1.5f).Within(.00001));
            Assert.That(WorkforceRules.MovementSpeed(level), Is.EqualTo(4.2f * baseline * 1.5f).Within(.00001));
        }

        [Test]
        public void RepeatedMigrationDoesNotChangePlayerCapsPricesOrRecipeYields()
        {
            var state = new TransactionState { schemaVersion = 3 };
            var prices = Definitions.Items.Select(x => x.price).ToArray();
            var yields = Definitions.Recipes.Select(x => x.yield).ToArray();
            var upgrades = new[] { "", "carry10", "carry16", "carry24" };
            var caps = new[] { 12, 20, 32, 48 };
            for (int index = 0; index < upgrades.Length; index++)
            {
                state.unlocked.Add(upgrades[index]);
                WorkforceMigration.Apply(state);
                WorkforceMigration.Apply(state);
                Assert.That(CarryLimits.Player(state), Is.EqualTo(caps[index]));
            }
            Assert.That(Definitions.Items.Select(x => x.price), Is.EqualTo(prices));
            Assert.That(Definitions.Recipes.Select(x => x.yield), Is.EqualTo(yields));
        }

        [Test]
        public void MaximumModAndReloadUseLimitsWithoutRepeatedScaling()
        {
            var seed = TransactionCoreTests.Seed();
            seed.schemaVersion = 3;
            seed.money = 0;
            foreach (string id in new[] { "storage_processing", "storage_farm_shop" })
                seed.owners.Add(new OwnerState
                {
                    id = id, kind = OwnerKind.Storage, actor = "simulation", location = id, capacity = 96
                });
            var core = new TransactionCore(seed);
            Grant(core, "maximum-1");
            var first = core.Snapshot();
            core = new TransactionCore(first);
            Grant(core, "maximum-2");
            var second = core.Snapshot();
            Assert.That(first.crews.Sum(x => x.count), Is.EqualTo(39));
            Assert.That(second.crews.Sum(x => x.count), Is.EqualTo(39));
            Assert.That(second.crews.All(x => x.count == WorkforceRules.Limit(x.id)), Is.True);
            Assert.That(JsonUtility.ToJson(second.crews[0]), Is.EqualTo(JsonUtility.ToJson(first.crews[0])));
            Assert.That(second.stacks.Sum(x => x.quantity), Is.EqualTo(first.stacks.Sum(x => x.quantity)));
        }

        static void Grant(TransactionCore core, string key)
        {
            core.Execute(new TransactionCommand
            {
                actor = "player", kind = TransactionKind.GrantMaximum, key = key,
                effectId = key, expectedRevision = core.Revision
            });
        }
    }
}
