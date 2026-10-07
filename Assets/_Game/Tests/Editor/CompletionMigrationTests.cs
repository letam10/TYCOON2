using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class CompletionMigrationTests
    {
        [Test]
        public void ExistingMachinesReconcileChangedCatalogWithoutNewStationIds()
        {
            var previous = GameSession.Instance;
            var root = new GameObject("Catalog migration regression");
            root.SetActive(false);
            var game = root.AddComponent<GameSession>();
            GameSession.Instance = game;
            try
            {
                var child = new GameObject("mill");
                child.transform.SetParent(root.transform);
                var machine = child.AddComponent<MachineStation>();
                machine.Id = "mill";
                machine.Requirement = "mill";
                machine.Recipe = Definitions.Recipe("mill");
                machine.Inventory = new Inventory(24);
                machine.ConfigureInputLimits();
                game.Machines.Add(machine);
                game.Stations.Add(machine);
                var state = TransactionCoreTests.Seed();
                var saved = state.stations.Single(s => s.id == "mill");
                saved.requirement = "dairy";
                saved.autonomous = false;
                saved.recipeOptions.Clear();
                // Thế giới dùng cùng stable ID: không được chỉ migrate khi xuất hiện trạm mới.
                state.owners.Add(new OwnerState
                {
                    id = "mill", actor = "simulation", location = "mill", capacity = 24,
                    kind = OwnerKind.Machine, writers = new() { "player" }
                });
                state.owners.Add(new OwnerState
                {
                    id = "mill_input", actor = "simulation", location = "mill_input", capacity = 36,
                    kind = OwnerKind.Machine, writers = new() { "player" }
                });
                var stockBefore = state.stacks.Sum(s => s.quantity);
                Assert.That(Reconcile(state, game), Is.True);
                Assert.That(saved.requirement, Is.EqualTo("mill"));
                Assert.That(saved.autonomous, Is.True);
                Assert.That(saved.recipeOptions, Is.EquivalentTo(new[] { "mill" }));
                Assert.That(state.stacks.Sum(s => s.quantity), Is.EqualTo(stockBefore));
                Assert.That(Reconcile(state, game), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                GameSession.Instance = previous;
            }
        }

        static bool Reconcile(TransactionState state, GameSession game)
        {
            return (bool)typeof(RuntimeTransactions).GetMethod("ReconcileWorld",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { state, game });
        }

        [Test]
        public void ProcessingIncludesBasicMachinesWithoutAnotherPurchase()
        {
            Assert.That(Definitions.Upgrade("dairy").kind, Is.EqualTo("legacy"));
            Assert.That(Definitions.Upgrade("supermarket").requirement, Is.EqualTo("mill"));
            var state = TransactionCoreTests.Seed();
            state.unlocked.Add("mill");
            state.legacyTransactions = 600;
            foreach (string recipe in new[] { "mill", "cheesemaker", "saucemaker" })
            {
                var station = state.stations.Find(s => s.definitionId == recipe);
                if (station == null)
                {
                    station = new StationRuntimeState { id = recipe, kind = "machine", definitionId = recipe };
                    state.stations.Add(station);
                }
                station.batches = 50;
            }
            Assert.That(ProgressionTracker.MissingRequirements(state, Definitions.Upgrade("supermarket")), Is.Empty);
        }
    }
}
