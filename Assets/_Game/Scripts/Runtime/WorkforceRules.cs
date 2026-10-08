using System;
using System.Collections.Generic;
using System.Linq;

namespace Tycoon
{
    public static class WorkforceRules
    {
        public const int MaximumActive = 39;
        public const int Version = 1;
        public const float Capability = 1.5f;
        public const TransactionKind RetireCommand = (TransactionKind)1000;
        static readonly Dictionary<string, int> Limits = BuildLimits();

        static Dictionary<string, int> BuildLimits()
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            int index = 0;
            foreach (var definition in Definitions.Upgrades)
            {
                if (definition.kind != "worker") continue;
                result.Add(definition.id, index++ % 2 == 0 ? 2 : 1);
            }
            return result;
        }

        public static int Limit(string upgrade) =>
            upgrade != null && Limits.TryGetValue(upgrade, out int limit) ? limit : 0;

        public static int ExpectedActive(TransactionState state) =>
            state?.crews.Sum(ActiveCount) ?? 0;

        public static int ActiveCount(CrewState crew) => Math.Min(crew.count, Limit(crew.id));

        public static float Productivity(int speedLevel) => Capability * (1 + .2f * (speedLevel - 1));

        public static float MovementSpeed(int speedLevel) => 4.2f * Productivity(speedLevel);

        public static bool IsExcess(string id, string upgrade)
        {
            if (id == null || upgrade == null || !id.StartsWith(upgrade + ":", StringComparison.Ordinal))
                return false;
            return int.TryParse(id.Substring(upgrade.Length + 1), out int slot) && slot >= Limit(upgrade);
        }

        public static bool HasObligations(TransactionState state, OwnerState owner)
        {
            return state.stacks.Any(x => x.owner == owner.id && x.quantity > 0) ||
                state.crates.Any(x => x.holder == owner.actor) ||
                state.reservations.Any(x => x.status == ReservationStatus.Active &&
                    (x.holder == owner.actor || x.source == owner.id ||
                        x.destination == owner.id || x.relay == owner.id)) ||
                state.stations.Any(x => x.operatorId == owner.actor && x.operatorUntil > state.simulationTime);
        }

        public static int PendingRetirements(TransactionState state) => state == null ? 0 :
            state.owners.Count(x => x.kind == OwnerKind.Worker && x.worker != null &&
                (x.worker.retiring || IsExcess(x.worker.id, x.worker.upgrade)) &&
                (!x.worker.retired || HasObligations(state, x)));
    }
}
