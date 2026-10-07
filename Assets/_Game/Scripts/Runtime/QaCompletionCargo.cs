using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator CompletionCargo()
        {
            yield return CompletionReset(new[] { "loader_processing", "loader_farm_shop" });
            // Thu nhỏ riêng ngăn sữa trong fixture để tạo kho đầy bằng hàng đã có chủ.
            float speed = Time.timeScale;
            Time.timeScale = 0;
            var fixture = game.Transactions.Snapshot();
            var destinationOwner = fixture.owners.Single(o => o.id == "storage_farm_shop");
            destinationOwner.limits.Single(l => l.id == "milk").count = 6;
            CompletionWriteFixture(fixture, game.CaptureSaveData());
            yield return RecoveryReady();
            game.Commerce.enabled = false;
            game.Restaurant.enabled = false;
            game.Player.CanControl = false;
            foreach (var producer in game.Producers) producer.enabled = false;
            foreach (var machine in game.Machines) machine.enabled = false;
            Time.timeScale = speed;

            var farm = game.StorageFor("farm");
            var source = game.StorageFor("processing");
            var destination = game.StorageFor("farm_shop");
            var holding = game.StorageFor("bakery");
            var totals = Definitions.Items.ToDictionary(i => i.id, i => CompletionItemTotal(i.id));
            var manifest = source.Inventory.Snapshot().ToDictionary(i => i.id, i => i.count);
            Check(manifest.Count == 5 && manifest.Values.All(q => q == 6),
                "source contains five complete six item cargo batches");
            var destinationBefore = manifest.Keys.ToDictionary(i => i, i => destination.Inventory.Count(i));
            Check(game.Transactions.View.transportJobs.Count(j => j.kind == "hand") == 29,
                "cargo fixture begins one real hand delivery below truck purchase gate");
            Check(game.Transactions.Transfer(farm.Inventory, game.Player.Carry, "carrot", 1) == 1,
                "cargo prerequisite takes an owned carrot");
            Check(game.Transactions.Transfer(game.Player.Carry, source.Inventory, "carrot", 1) == 1,
                "cargo prerequisite completes the thirtieth hand delivery");
            Check(game.Contribute(Definitions.Upgrade("truck_bundle"), 5000) == 5000,
                "truck and driver purchased through actual contribution");
            Check(game.Transactions.Transfer(source.Inventory, farm.Inventory, "carrot", 1) == 1,
                "return prerequisite carrot before observing five cargo SKUs");
            Check(game.Logistics.Select(source.Id, destination.Id, false),
                "select explicit processing to farm shop cargo route");
            Check(game.UpgradeCrew("loader_processing", "count"),
                "purchase second source loader through crew upgrade transaction");
            yield return CompletionWait(() => game.Workers.Count(w => w.UpgradeId == "loader_processing") == 2 &&
                game.Workers.Any(w => w.Role == "Driver"), 5, "source loader pair and purchased driver spawn");
            yield return CompletionWait(() => game.Logistics.Vehicle && game.Workers.Any(w =>
                w.Role == "Driver" && w.transform.parent == game.Logistics.Vehicle && !w.Agent.enabled),
                5, "driver Update seats driver in the actual truck");

            var packed = new HashSet<string>();
            var claimed = new HashSet<string>();
            bool capacityHonored = true;
            var loaders = game.Workers.Where(w => w.UpgradeId == "loader_processing")
                .Select(w => game.Transactions.Actor(w.GetEntityId())).ToHashSet();
            yield return CompletionWait(() =>
            {
                var state = game.Transactions.View;
                foreach (var box in state.crates)
                {
                    if (box.holder == "dock:" + source.Id) packed.Add(box.id);
                    if (box.task == "load" && loaders.Contains(box.holder)) claimed.Add(box.id);
                }
                capacityHonored &= state.crates.Count(c => c.holder == state.truck.id || c.task == "load") <= 6;
                return source.Inventory.Total == 0 && state.crates.Count == manifest.Count &&
                    state.crates.All(c => c.holder == state.truck.id && c.task == null);
            }, 90, "real loaders pack claim carry and load every source batch");
            Check(capacityHonored, "concurrent loader claims respect six truck slots");
            Check(packed.Count > 0 && claimed.Count == manifest.Count,
                "observe autonomous packing at dock and each crate in source loader custody");
            foreach (var row in manifest)
            {
                var box = game.Transactions.View.crates.Single(c => c.item == row.Key);
                Check(game.Transactions.View.stacks.Where(s => s.owner == box.id).Sum(s => s.quantity) == row.Value,
                    "loaded crate retains exact owned quantity: " + row.Key);
            }
            CompletionCargoConserved(totals, "loaded cargo");

            Check(game.Transactions.Transfer(holding.Inventory, destination.Inventory, "milk", 6) == 6,
                "fill destination milk bin with independently owned bakery stock");
            Check(destination.Inventory.FreeFor("milk") == 0, "destination milk bin is full before departure");
            int activeBefore = game.Transactions.View.reservations.Count(r => r.status == ReservationStatus.Active);
            var crateIds = game.Transactions.View.crates.Select(c => c.id).ToHashSet();
            Check(!game.Logistics.Dispatch(), "dispatch refuses a full destination bin");
            Check(game.Transactions.View.truck.phase == "Loading" && destination.Inventory.IncomingTotal == 0 &&
                game.Transactions.View.reservations.Count(r => r.status == ReservationStatus.Active) == activeBefore,
                "failed multi SKU dispatch rolls back all destination reservations");
            Check(game.Logistics.Select(source.Id, destination.Id, true), "enable automatic driver route retries");
            yield return new WaitForSeconds(4);
            Check(game.Transactions.View.truck.phase == "Loading" &&
                crateIds.SetEquals(game.Transactions.View.crates.Select(c => c.id)) &&
                game.Transactions.View.crates.All(c => c.holder == game.Transactions.View.truck.id),
                "automatic dispatch waits with every crate still owned by the truck");
            CompletionCargoConserved(totals, "full destination refusal");
            Check(game.Transactions.Transfer(destination.Inventory, holding.Inventory, "milk", 6) == 6,
                "release destination capacity using an actual transfer");
            yield return CompletionWait(() => game.Transactions.View.truck.phase == "Travelling" &&
                game.Transactions.View.truck.travelled > 0, 10,
                "automatic dispatch resumes and truck Update advances real travel");
            var reservations = game.Transactions.View.crates.Select(c => c.reservation).ToArray();
            Check(reservations.All(id => !string.IsNullOrEmpty(id)) &&
                reservations.Distinct().Count() == manifest.Count,
                "each travelling crate owns a distinct destination reservation");
            foreach (var row in manifest)
            {
                Check(destination.Inventory.ReservedSpace(row.Key) == row.Value,
                    "departure reserves exact destination quantity: " + row.Key);
            }
            Check(game.Transactions.Transfer(holding.Inventory, destination.Inventory, "milk", 1) == 0,
                "competing deposit cannot steal the travelling milk crate destination space");
            CompletionCargoConserved(totals, "reserved travelling cargo");

            var unloadActors = game.Workers.Where(w => w.UpgradeId == "loader_farm_shop")
                .Select(w => game.Transactions.Actor(w.GetEntityId())).ToHashSet();
            var unloadedClaims = new HashSet<string>();
            bool arrived = false;
            int completed = game.Transactions.View.truck.completedTrips;
            yield return CompletionWait(() =>
            {
                var state = game.Transactions.View;
                arrived |= state.truck.current == destination.Id && state.truck.phase == "WaitingUnload";
                foreach (var box in state.crates)
                {
                    if (box.task == "unload" && unloadActors.Contains(box.holder)) unloadedClaims.Add(box.id);
                }
                return state.truck.completedTrips == completed + 1 && state.crates.Count == 0;
            }, 120, "truck follows its route and destination loader unloads every owned crate");
            Check(arrived && unloadedClaims.SetEquals(crateIds),
                "destination loader claims every crate after actual truck arrival");
            foreach (var row in manifest)
            {
                Check(destination.Inventory.Count(row.Key) == destinationBefore[row.Key] + row.Value,
                    "destination receives each cargo SKU exactly once: " + row.Key);
            }
            Check(reservations.All(id => game.Transactions.Reservation(id).status == ReservationStatus.Used) &&
                destination.Inventory.IncomingTotal == 0,
                "unloading consumes every destination reservation and leaves no reserved space");
            Check(!game.Transactions.View.owners.Any(o => crateIds.Contains(o.id)) &&
                !game.Transactions.View.stacks.Any(s => crateIds.Contains(s.owner)),
                "completed trip removes empty crate owners without orphaned cargo stacks");
            Check(source.Inventory.Total == 0, "cargo source stays empty after destination delivery");
            CompletionCargoConserved(totals, "completed cargo trip");
            TransactionCore.Validate(game.Transactions.Snapshot());
        }

        void CompletionCargoConserved(Dictionary<string, int> totals, string phase)
        {
            foreach (var row in totals)
            {
                Check(CompletionItemTotal(row.Key) == row.Value, phase + " conserves SKU " + row.Key);
            }
        }
    }
}
