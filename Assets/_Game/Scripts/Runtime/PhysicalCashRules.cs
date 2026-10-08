using System;
using System.Linq;

namespace Tycoon
{
    public static class PhysicalCashRules
    {
        public const int Version = 3;
        public const string SafeId = "safe_starter";

        public static long Hand(TransactionState state)
        {
            return state.schemaVersion >= Version ? state.cashInHand : state.money;
        }

        public static bool HasGoods(TransactionState state, string actor = "player")
        {
            return state.stacks.Any(x => x.owner == actor && x.quantity > 0) ||
                state.crates.Any(x => x.holder == actor);
        }

        internal static long Add(long balance, long amount)
        {
            if (amount < 0 || balance < 0 || balance > long.MaxValue - amount)
                throw new TransactionRejectedException("cash-overflow", "Số tiền vượt giới hạn lưu trữ.");
            return balance + amount;
        }

        internal static void CreditHand(TransactionState state, long amount)
        {
            if (state.schemaVersion >= Version)
            {
                if (HasGoods(state))
                    throw new TransactionRejectedException("hands", "Cất hàng để lấy tiền.");
                state.cashInHand = Add(state.cashInHand, amount);
            }
            else state.money = Add(state.money, amount);
        }

        internal static void Spend(TransactionState state, long amount)
        {
            if (amount < 0 || Hand(state) < amount)
                throw new TransactionRejectedException("funds", "Rút thêm tiền tại két.");
            if (state.schemaVersion >= Version) state.cashInHand -= amount;
            else state.money -= amount;
        }

        public static void Validate(TransactionState state)
        {
            if (state.schemaVersion < Version) return;
            if (state.money != 0 || state.cashInHand < 0 || state.cashInSafe < 0 ||
                state.cashInHand > 0 && HasGoods(state))
                throw new TransactionRejectedException("cash", "Trạng thái tiền hoặc tay không hợp lệ.");
        }

        internal static void Guard(TransactionState state, TransactionCommand command)
        {
            if (state.schemaVersion < Version || command.actor != "player") return;
            if (state.cashInHand > 0 && command.kind is TransactionKind.Take or
                TransactionKind.HarvestProducer or TransactionKind.ClaimCrate)
                throw new TransactionRejectedException("hands", "Gửi tiền vào két để lấy hàng.");
            if (state.cashInHand > 0 && command.destination == "player")
                throw new TransactionRejectedException("hands", "Gửi tiền vào két để lấy hàng.");
        }
    }
}
