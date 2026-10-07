using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tycoon
{
    public sealed partial class GameSession
    {
        bool RemoveRetiredPurchasePads(SaveData data)
        {
            var retired = new HashSet<string>();
            foreach (var upgrade in Definitions.Upgrades)
            {
                string id = "pad_" + upgrade.id;
                if (upgrade.kind == "legacy" && !Stations.Exists(station => station.Id == id))
                    retired.Add(id);
            }

            var state = data.transactionState;
            if (state != null)
            {
                // Không loại bỏ owner cũ nếu còn giữ dữ liệu giao dịch.
                foreach (var owner in state.owners.Where(owner => retired.Contains(owner.id)))
                {
                    bool holdsData = owner.capacity != 0 || state.stacks.Exists(stack => stack.owner == owner.id);
                    holdsData |= state.reservations.Exists(reservation =>
                        reservation.source == owner.id || reservation.destination == owner.id);
                    holdsData |= state.orders.Exists(order => order.counter == owner.id || order.table == owner.id);
                    if (holdsData)
                        throw new InvalidDataException("Station reference không còn tồn tại: " + owner.id);
                }
            }

            // Chỉ bỏ trạng thái hình thức của các ô đã được catalog đánh dấu legacy.
            bool changed = data.stationStates.RemoveAll(progress =>
                progress != null && retired.Contains(progress.id)) > 0;
            if (state != null)
            {
                changed |= state.stations.RemoveAll(station => retired.Contains(station.id)) > 0;
                changed |= state.owners.RemoveAll(owner => retired.Contains(owner.id)) > 0;
            }
            return changed;
        }
    }
}
