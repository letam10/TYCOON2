namespace Tycoon
{
    public static class PhysicalCashMigration
    {
        public static bool Upgrade(SaveData data)
        {
            bool changed = data.version < PhysicalCashRules.Version;
            if (data.transactionState != null)
            {
                changed |= Upgrade(data.transactionState);
                RuntimeTransactions.Project(data.transactionState, data);
            }
            else if (changed)
            {
                data.cashInSafe = checked(data.cashInSafe + data.money);
                data.money = 0;
                data.cashInHand = 0;
                data.version = PhysicalCashRules.Version;
            }
            return changed;
        }

        public static bool Upgrade(TransactionState state)
        {
            if (state.schemaVersion >= PhysicalCashRules.Version) return false;
            // Chỉ gọi sau recovery journal cũ; tiền cũ vào két để giữ nguyên hàng đang cầm.
            state.cashInSafe = checked(state.cashInSafe + state.money);
            state.cashInHand = 0;
            state.money = 0;
            state.schemaVersion = PhysicalCashRules.Version;
            CarryLimits.Apply(state);
            PhysicalCashRules.Validate(state);
            return true;
        }
    }
}
