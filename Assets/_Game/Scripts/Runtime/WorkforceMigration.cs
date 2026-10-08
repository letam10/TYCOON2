using System.Linq;

namespace Tycoon
{
    public static class WorkforceMigration
    {
        public static bool Apply(TransactionState state)
        {
            if (state == null) return false;
            bool changed = false;
            foreach (var crew in state.crews)
            {
                int limit = WorkforceRules.Limit(crew.id);
                if (limit == 0 || crew.count <= limit) continue;
                crew.count = limit;
                changed = true;
            }
            for (int index = 0; index < state.owners.Count; index++)
            {
                var owner = state.owners[index];
                if (owner.kind != OwnerKind.Worker || owner.worker == null) continue;
                var crew = state.crews.Find(x => x.id == owner.worker.upgrade);
                if (crew == null) continue;
                bool retiring = owner.worker.retiring ||
                    WorkforceRules.IsExcess(owner.worker.id, owner.worker.upgrade);
                bool retired = retiring && !WorkforceRules.HasObligations(state, owner);
                int capacity = CarryLimits.Worker(crew, state.schemaVersion >= PhysicalCashRules.Version);
                if (capacity == owner.capacity && retiring == owner.worker.retiring &&
                    retired == owner.worker.retired) continue;
                // Không xóa owner/stack/reservation; giữ ID lịch sử và hàng đến khi giao xong.
                var updated = owner.ShallowCopy();
                updated.worker = CheckpointSnapshot.Copy(owner.worker);
                updated.capacity = capacity;
                updated.worker.retiring = retiring;
                updated.worker.retired = retired;
                state.owners[index] = updated;
                changed = true;
            }
            return changed;
        }

        public static bool Apply(SaveData data)
        {
            bool changed = Apply(data.transactionState);
            foreach (var crew in data.crews)
            {
                int limit = WorkforceRules.Limit(crew.id);
                if (limit == 0 || crew.count <= limit) continue;
                crew.count = limit;
                changed = true;
            }
            foreach (var worker in data.workers)
            {
                var owner = data.transactionState?.owners.Find(x => x.worker?.id == worker.id);
                bool retiring = owner?.worker.retiring ??
                    (worker.retiring || WorkforceRules.IsExcess(worker.id, worker.upgrade));
                bool retired = owner?.worker.retired ??
                    (retiring && worker.carry.All(x => x.count == 0) && worker.phase == 0 &&
                        string.IsNullOrEmpty(worker.reservation) && string.IsNullOrEmpty(worker.working));
                changed |= retiring != worker.retiring || retired != worker.retired;
                worker.retiring = retiring;
                worker.retired = retired;
            }
            return changed;
        }
    }
}
