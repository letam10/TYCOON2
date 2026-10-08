using System.IO;

namespace Tycoon
{
    public sealed partial class WorkerAgent
    {
        string InventoryId(Station station, Inventory inventory) => station == null ? "" :
            station.Id + (station is MachineStation machine && ReferenceEquals(machine.Input, inventory)
                ? "_input" : "");

        public WorkerSave Snapshot() => new()
        {
            id = WorkerId,
            upgrade = UpgradeId,
            source = InventoryId(source, from),
            destination = InventoryId(destination, to),
            item = item,
            working = working?.Id,
            reservation = reservationId,
            tableReceipt = tableReceipt,
            x = transform.position.x,
            z = transform.position.z,
            phase = phase,
            count = count,
            deliveries = Deliveries,
            carry = Carry.Snapshot(),
            retiring = Retiring,
            retired = Retired
        };

        public void Restore(WorkerSave saved)
        {
            StopWorking();
            CancelTransport();
            Retiring = saved.retiring || WorkforceRules.IsExcess(WorkerId, UpgradeId);
            Retired = saved.retired;
            if (Carry.Authority == null) Carry.Restore(saved.carry);
            Deliveries = saved.deliveries;
            item = saved.item;
            count = saved.count;
            phase = saved.phase;
            tableReceipt = saved.tableReceipt;
            reservationId = saved.reservation;
            var game = GameSession.Instance;
            source = game.Stations.Find(x => x.Id == saved.source);
            destination = game.Stations.Find(x => x.Id == saved.destination || x.Id + "_input" == saved.destination);
            from = source?.Inventory;
            to = destination is MachineStation machine && saved.destination.EndsWith("_input")
                ? machine.Input : destination?.Inventory;
            working = game.Stations.Find(x => x.Id == saved.working);
            if (game.Transactions != null)
            {
                if (phase is 1 or 2)
                {
                    if (destination is CheckoutStation or TableStation) to = null;
                    var reservation = game.Transactions.Reservation(reservationId);
                    heldSpace = reservation != null && reservation.status == ReservationStatus.Active;
                    if (!heldSpace)
                    {
                        phase = 0;
                        count = 0;
                        reservationId = null;
                    }
                }
                return;
            }
            if (phase is not (1 or 2)) return;
            heldSpace = to != null && to.TryReserveSpace(item, count);
            if ((!heldSpace && destination is not CheckoutStation) ||
                (phase == 1 && (from == null || !from.TryReserve(item, count))))
            {
                if (heldSpace) to.ReleaseSpace(item, count);
                phase = 0;
                heldSpace = false;
                throw new InvalidDataException("Không khôi phục được chỗ giữ cho tuyến vận chuyển " + WorkerId);
            }
        }
    }
}
