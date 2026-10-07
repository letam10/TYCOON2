using System.Collections;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator CompletionCrewShortage()
        {
            yield return CompletionReset(new[] { "processor" }, "machine_mill");
            var mill = game.Machines.First(m => m.Id == "machine_mill");
            var farm = game.StorageFor("farm");
            var stock = game.StorageFor("processing");
            var holding = game.StorageFor("bakery");
            int wheat = CompletionItemTotal("wheat");
            // Giữ nguyên quyền sở hữu: chuyển lúa sang kho ngoài tuyến tìm nguyên liệu của thợ.
            foreach (var source in new[] { farm, stock })
            {
                int quantity = source.Inventory.Count("wheat");
                Check(game.Transactions.Transfer(source.Inventory, holding.Inventory, "wheat", quantity) == quantity,
                    "remove processor supply by transfer: " + source.Id);
            }
            yield return new WaitForSeconds(5);
            var worker = game.Workers.Single(w => w.UpgradeId == "processor");
            Check(mill.Batches == 0 && mill.Input.Count("wheat") == 0 && worker.Carry.Total == 0,
                "processor waits without manufacturing missing wheat");
            Check(CompletionItemTotal("wheat") == wheat, "shortage wait conserves every wheat stack");
            int required = mill.Recipe.inputs.Single().count;
            Check(game.Transactions.Transfer(holding.Inventory, stock.Inventory, "wheat", required) == required,
                "replenish source through actual owned transfer");
            yield return CompletionWait(
                () => mill.Batches == 1 && stock.Inventory.Count("flour") == mill.Recipe.yield,
                90, "waiting processor resumes loading and unloading after replenishment");
            Check(CompletionItemTotal("wheat") == wheat - required,
                "resumed processor consumes exactly one recipe of wheat");
            Check(worker.Deliveries >= 2, "shortage recovery includes worker pickup and finished output delivery");
            TransactionCore.Validate(game.Transactions.Snapshot());
        }

        IEnumerator CompletionCrewFullShelf()
        {
            yield return CompletionReset(new[] { "restocker_market" });
            var shelf = game.Shelves.First(s => s.ShopId == "market" && s.Accepts("carrot"));
            var storage = game.StorageFor("supermarket");
            var holding = game.StorageFor("farm");
            int total = CompletionItemTotal("carrot");
            int gap = shelf.Inventory.FreeFor("carrot");
            if (gap > 0)
            {
                Check(game.Transactions.Transfer(holding.Inventory, shelf.Inventory, "carrot", gap) == gap,
                    "fill destination carrot bin using owned stock");
            }
            Check(shelf.Inventory.FreeFor("carrot") == 0, "market destination bin starts full");
            int full = shelf.Inventory.Count("carrot");
            int supply = storage.Inventory.Count("carrot");
            Check(supply >= full && full > 0, "full destination case has sufficient independent source stock");
            yield return new WaitForSeconds(5);
            Check(shelf.Inventory.Count("carrot") == full && storage.Inventory.Count("carrot") == supply,
                "stocker waits while the destination carrot bin is full");
            Check(CompletionItemTotal("carrot") == total, "full destination wait loses no owned carrots");
            Check(game.Transactions.Transfer(shelf.Inventory, holding.Inventory, "carrot", full) == full,
                "free destination space through actual transfer");
            var worker = game.Workers.Single(w => w.UpgradeId == "restocker_market");
            yield return CompletionWait(() => worker.Carry.Count("carrot") > 0, 60,
                "stocker resumes and picks up reserved carrots after destination is cleared");
            var route = worker.Snapshot();
            var reservation = game.Transactions.Reservation(route.reservation);
            Check(reservation != null && reservation.status == ReservationStatus.Active &&
                reservation.destination == shelf.Inventory.OwnerId,
                "carried carrots retain the active reservation for their destination");
            int carried = worker.Carry.Count("carrot");
            Check(shelf.Inventory.ReservedSpace("carrot") == carried && shelf.Inventory.FreeFor("carrot") == 0,
                "in-transit carrots occupy all cleared destination space");
            Check(game.Transactions.Transfer(holding.Inventory, shelf.Inventory, "carrot", 1) == 0,
                "competing transfer cannot steal space reserved for carried goods");
            Check(worker.Carry.Count("carrot") == carried &&
                game.Transactions.Reservation(route.reservation).status == ReservationStatus.Active,
                "rejected competing deposit preserves worker carry and reservation");
            yield return CompletionWait(() => shelf.Inventory.Count("carrot") == full &&
                worker.Carry.Count("carrot") == 0, 60, "stocker delivers into its protected destination space");
            Check(game.Transactions.Reservation(route.reservation).status == ReservationStatus.Used,
                "successful worker delivery consumes its reservation exactly once");
            Check(storage.Inventory.Count("carrot") == supply - full && CompletionItemTotal("carrot") == total,
                "destination recovery conserves carrots across source, carry and shelf");
            TransactionCore.Validate(game.Transactions.Snapshot());
        }
    }
}
