using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator CompletionSave()
        {
            yield return CompletionReset(new[] { "processor", "repair_processing" },
                "machine_mill", "machine_cheesemaker");
            foreach (var worker in game.Workers) worker.enabled = false;
            var farm = game.StorageFor("farm");
            var processing = game.StorageFor("processing");
            var machine = game.Machines.First(m => m.Id == "machine_mill");
            var player = game.Player.Carry;
            Check(game.Transactions.Transfer(farm.Inventory, player, "carrot", 1) == 1,
                "Manual job picks real source stock");
            Check(game.Transactions.Transfer(player, processing.Inventory, "carrot", 1) == 1,
                "Manual job delivers to a different owner");
            Check(game.Contribute(Definitions.Upgrade("truck_bundle"), 5000) == 5000,
                "Thirty hand jobs unlock the truck package");
            Check(game.Transactions.Transfer(farm.Inventory, player, "carrot", 3) == 3, "Pick three cargo items");
            var pack = game.Transactions.Command(TransactionKind.PackCrate, "player", "crate:completion");
            pack.source = "player";
            pack.item = "carrot";
            pack.quantity = 3;
            game.Transactions.Execute(pack);
            game.Transactions.Execute(game.Transactions.Command(TransactionKind.LoadCrate,
                "player", "crate:completion"));
            Check(game.Logistics.Select(processing.Id, game.StorageFor("farm_shop").Id, false),
                "Select actual truck source and destination");
            Check(game.Transactions.Transfer(processing.Inventory, machine.Input, "wheat", 4) == 4,
                "Load saved production batch");
            yield return CompletionWait(() => machine.Running, 5, "Saved machine has input escrow");
            machine.BreakDown(40);
            var repair = game.Transactions.Command(TransactionKind.RepairMachine, "player", machine.Id);
            repair.duration = 2;
            game.Transactions.Execute(repair);
            machine.enabled = false;
            var counter = game.Checkouts.First(c => c.ShopId == "farm");
            var partial = TownCustomer(counter, "carrot", 3);
            game.Transactions.Transfer(farm.Inventory, player, "carrot", 2);
            Check(game.Transactions.Deliver(partial.Receipt, player) == 2, "Save fixture delivers a partial order");
            var paid = TownCustomer(game.Checkouts.First(c => c.ShopId == "farm_shop"), "tomato", 1);
            game.Transactions.Transfer(farm.Inventory, player, "tomato", 1);
            Check(game.Transactions.Deliver(paid.Receipt, player) == 1, "Save fixture leaves payment at its counter");
            var failed = TownCustomer(counter, "wheat", 2, .3f);
            game.Transactions.Transfer(farm.Inventory, player, "wheat", 1);
            Check(game.Transactions.Deliver(failed.Receipt, player) == 1, "Customer owns one item before timeout");
            yield return new WaitForSeconds(.5f);
            Check(game.Transactions.Order(failed.Receipt).status == OrderStatus.Failed,
                "Partial abandonment fails before save");
            game.Contribute(Definitions.Upgrade("carry10"), 35);
            CompletionDiner("corn_soup");
            var processor = game.Workers.First(w => w.UpgradeId == "processor");
            processor.enabled = true;
            yield return CompletionWait(() => processor.Snapshot().phase == 2 && processor.Carry.Total > 0,
                40, "Worker holds actual goods and destination reservation during save");
            Check(game.Logistics.Dispatch(), "Reserve destination before truck departure");
            yield return CompletionWait(() => game.Transactions.View.truck.travelled > .1, 5,
                "Truck moves before checkpoint");
            game.Events.warning = 9;
            game.Events.untilRush = 480;
            Time.timeScale = 0;
            game.Player.CanControl = false;
            string pausedState = JsonUtility.ToJson(game.Transactions.Snapshot());
            yield return new WaitForSecondsRealtime(.25f);
            Check(JsonUtility.ToJson(game.Transactions.Snapshot()) == pausedState,
                "Pause freezes jobs, orders, reservations and machine state");
            game.SaveGame();
            var expected = SaveStore.Read(game.SavePath);
            string snapshot = Path.Combine(game.QaDirectory, "completion-midflight.json");
            SaveStore.Write(snapshot, expected);
            Check(expected.transactionState.truck.phase == "Travelling" &&
                expected.transactionState.truck.travelled > 0, "Save includes a moving truck with owned cargo");
            for (int attempt = 0; attempt < 3; attempt++)
            {
                game.Commerce.enabled = true;
                game.Restaurant.enabled = true;
                game.LoadGame();
                yield return RecoveryReady();
                game.Player.CanControl = false;
                CompletionCompare(expected, "Repeated load " + (attempt + 1));
            }
            Time.timeScale = 1;
        }

        IEnumerator CompletionRelaunch()
        {
            yield return RecoveryReady();
            Time.timeScale = 0;
            game.Player.CanControl = false;
            string source = GameSession.Argument("--qa-completion-from", "");
            Check(File.Exists(source), "Relaunch source exists");
            var expected = SaveStore.Read(source);
            game.Transactions.Detach();
            SaveStore.Write(game.SavePath, expected);
            File.Delete(game.SavePath + ".journal");
            game.Milestone = 1;
            game.LoadGame();
            yield return RecoveryReady();
            foreach (var machine in game.Machines) machine.enabled = false;
            game.Player.CanControl = false;
            CompletionCompare(expected, "Separate process");
            yield return CompletionResume(expected);
            Check(game.RuntimeErrors.Count == 0, "Relaunch has no runtime errors");
            yield return Capture("restored.png");
        }

        IEnumerator CompletionResume(SaveData expected)
        {
            game.Commerce.enabled = false;
            game.Restaurant.enabled = false;
            foreach (var producer in game.Producers) producer.enabled = false;
            var job = expected.workers.First(w => w.phase == 2 && w.count > 0);
            var worker = game.Workers.Single(w => w.WorkerId == job.id);
            int deliveries = worker.Deliveries;
            Time.timeScale = 3;
            yield return CompletionWait(() => worker.Deliveries > deliveries, 60,
                "Restored worker completes its reserved delivery");
            Check(game.Transactions.Reservation(job.reservation).status == ReservationStatus.Used,
                "Restored delivery consumes its original reservation once");
            worker.enabled = false;
            var machine = game.Machines.Single(m => m.Id == "machine_mill");
            yield return CompletionWait(() => !machine.Broken, 35,
                "Repairer resumes saved partial repair without resetting progress");
            yield return CompletionWait(() => game.Transactions.View.truck.phase == "WaitingUnload", 80,
                "Restored truck reaches its destination with cargo");
            var truck = game.Transactions.View.truck;
            var box = game.Transactions.View.crates.Single(c => c.holder == truck.id);
            int quantity = game.Transactions.View.stacks.Where(s => s.owner == box.id).Sum(s => s.quantity);
            var destination = game.Stations.OfType<StorageStation>().Single(s => s.Id == truck.current);
            int before = destination.Inventory.Count(box.item);
            var unload = game.Transactions.Command(TransactionKind.UnloadCrate, "player", box.id);
            game.Transactions.Execute(unload);
            game.Transactions.Execute(unload);
            Check(destination.Inventory.Count(box.item) == before + quantity,
                "Restored cargo unloads once even when the command repeats");
            TransactionCore.Validate(game.Transactions.Snapshot());
            Time.timeScale = 0;
        }

        void CompletionCompare(SaveData expected, string label)
        {
            var actual = game.Transactions.Snapshot();
            var saved = expected.transactionState;
            Check(actual.money == saved.money && actual.revenue == saved.revenue &&
                actual.cashCollected == saved.cashCollected && actual.assistedCash == saved.assistedCash,
                label + " preserves wallet, counter revenue and collected cash");
            Check(CompletionItems(actual) == CompletionItems(saved),
                label + " preserves every item owner, location and quantity");
            Check(CompletionOrders(actual) == CompletionOrders(saved),
                label + " preserves partial, failed, paid orders and price snapshots");
            Check(JsonUtility.ToJson(new PaymentList { items = actual.payments.ToArray() }) ==
                JsonUtility.ToJson(new PaymentList { items = saved.payments.ToArray() }),
                label + " does not pay or collect twice");
            Check(CompletionReservations(actual) == CompletionReservations(saved),
                label + " preserves source, relay and output reservations");
            Check(string.Join(";", actual.purchases.Select(p => p.id + ":" + p.contributed + ":" + p.complete)) ==
                string.Join(";", saved.purchases.Select(p => p.id + ":" + p.contributed + ":" + p.complete)),
                label + " preserves partial purchase contributions");
            foreach (var station in saved.stations.Where(s => s.kind is "machine" or "producer"))
            {
                var restored = actual.stations.Single(s => s.id == station.id);
                Check(restored.running == station.running && restored.remaining == station.remaining &&
                    restored.batches == station.batches &&
                    JsonUtility.ToJson(restored.progress) == JsonUtility.ToJson(station.progress),
                    label + " preserves production, herd and repair " + station.id);
            }
            Check(JsonUtility.ToJson(actual.truck) == JsonUtility.ToJson(saved.truck),
                label + " preserves truck position on route");
            Check(JsonUtility.ToJson(game.Events) == JsonUtility.ToJson(expected.events),
                label + " preserves event countdown without offline progression");
            foreach (var worker in expected.workers)
            {
                var restored = game.Workers.Single(w => w.WorkerId == worker.id).Snapshot();
                // JsonUtility có thể khôi phục ID chưa gán thành null thay vì chuỗi rỗng.
                Check(restored.phase == worker.phase && (restored.reservation ?? "") == (worker.reservation ?? "") &&
                    (restored.destination ?? "") == (worker.destination ?? "") && restored.count == worker.count,
                    label + " preserves worker job " + worker.id);
            }
            Check(game.Economy.Losses.Count == expected.losses.Count &&
                game.Economy.Losses.Sum(l => l.goods.Sum(i => i.count)) ==
                expected.losses.Sum(l => l.goods.Sum(i => i.count)), label + " does not record loss twice");
            TransactionCore.Validate(actual);
        }

        [Serializable]
        sealed class PaymentList { public PaymentState[] items; }

        static string CompletionItems(TransactionState state)
        {
            return string.Join(";", state.stacks.OrderBy(s => s.id)
                .Select(s => s.id + ":" + s.owner + ":" + s.location + ":" + s.item + ":" + s.quantity));
        }

        static string CompletionOrders(TransactionState state)
        {
            return string.Join(";", state.orders.OrderBy(o => o.id).Select(o => JsonUtility.ToJson(o)));
        }

        static string CompletionReservations(TransactionState state)
        {
            return string.Join(";", state.reservations.Where(r => r.status == ReservationStatus.Active)
                .OrderBy(r => r.id).Select(r => JsonUtility.ToJson(r)));
        }
    }
}
