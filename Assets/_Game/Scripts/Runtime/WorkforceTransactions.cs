namespace Tycoon
{
    public sealed partial class TransactionCore
    {
        static string WorkforceFingerprint(string json) => json
            .Replace(",\"retiring\":false", "")
            .Replace(",\"retired\":false", "")
            .Replace(",\"workforceRevision\":0", "");

        static int RetireWorkforce(TransactionState state, TransactionCommand command)
        {
            Require(command.actor == "simulation", "authority", "Chỉ mô phỏng cho nhân viên nghỉ việc.");
            int index = state.owners.FindIndex(x => x.id == command.target && x.kind == OwnerKind.Worker);
            Require(index >= 0, "worker", "Nhân viên không tồn tại.");
            var owner = state.owners[index];
            Require(owner.worker != null &&
                WorkforceRules.IsExcess(owner.worker.id, owner.worker.upgrade), "worker", "Đội vẫn cần nhân viên.");
            Require(!WorkforceRules.HasObligations(state, owner), "cargo", "Nhân viên còn hàng hoặc chỗ giữ.");
            if (owner.worker.retired) return 0;
            // Bản nháp không được sửa WorkerSave dùng chung với state trước commit.
            var updated = owner.ShallowCopy();
            updated.worker = CheckpointSnapshot.Copy(owner.worker);
            updated.worker.retiring = true;
            updated.worker.retired = true;
            updated.worker.phase = 0;
            updated.worker.count = 0;
            updated.worker.working = null;
            updated.worker.reservation = null;
            state.owners[index] = updated;
            return 1;
        }
    }

    public sealed partial class RuntimeTransactions
    {
        internal bool RetireWorkforce(WorkerAgent worker)
        {
            string owner = WorkerOwner(worker.WorkerId);
            var command = Command(WorkforceRules.RetireCommand, "simulation", owner,
                effect: "retire-workforce:" + owner);
            if (!TryExecute(command, out _)) return false;
            inventories.Remove(worker.Carry);
            actors.Remove(worker.GetEntityId());
            worker.Carry.Unbind();
            return true;
        }
    }
}
