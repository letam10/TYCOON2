using System;
using System.Linq;

namespace Tycoon
{
    public sealed partial class TransactionCore
    {
        static int TransferSafe(TransactionState state, TransactionCommand command, bool deposit)
        {
            Player(state, command.actor);
            Require(state.schemaVersion >= PhysicalCashRules.Version &&
                command.target == PhysicalCashRules.SafeId &&
                state.stations.Any(x => x.id == command.target), "safe", "Két không tồn tại.");
            Require(!PhysicalCashRules.HasGoods(state), "hands", "Cất hàng để lấy tiền.");
            long amount = deposit ? state.cashInHand : state.cashInSafe;
            if (amount == 0) return 0;
            if (deposit)
            {
                state.cashInSafe = PhysicalCashRules.Add(state.cashInSafe, amount);
                state.cashInHand = 0;
            }
            else
            {
                state.cashInHand = PhysicalCashRules.Add(state.cashInHand, amount);
                state.cashInSafe = 0;
            }
            return 1;
        }

        static long CashMoved(TransactionState before, TransactionState after, TransactionKind kind)
        {
            if (kind == TransactionKind.DepositCash) return before.cashInHand - after.cashInHand;
            if (kind == TransactionKind.WithdrawCash) return before.cashInSafe - after.cashInSafe;
            if (kind == TransactionKind.GrantModCash) return after.cashInSafe - before.cashInSafe;
            return Math.Abs(PhysicalCashRules.Hand(after) - PhysicalCashRules.Hand(before));
        }

        int GrantMaximum(TransactionState state, TransactionCommand command)
        {
            Player(state, command.actor);
            Require(DevelopmentAssistance.Enabled || !runtime, "assist", "Mod không khả dụng.");
            if (state.maximumBuilt)
            {
                if (!legacyWorkforceReplay) WorkforceMigration.Apply(state);
                return 0;
            }
            foreach (var definition in Definitions.Upgrades.Where(x => x.kind != "legacy"))
            {
                if (!state.unlocked.Contains(definition.id)) state.unlocked.Add(definition.id);
                var progress = state.purchases.Find(x => x.id == definition.id);
                if (progress == null)
                {
                    progress = new PurchaseRuntimeState
                    {
                        id = definition.id,
                        definitionId = definition.id,
                        definitionVersion = definition.version
                    };
                    state.purchases.Add(progress);
                }
                progress.contributed = definition.cost;
                progress.complete = true;
                if (definition.kind == "worker" && !state.crews.Any(x => x.id == definition.id))
                    state.crews.Add(GameSession.CrewFor(definition));
            }
            foreach (var crew in state.crews)
            {
                crew.speedLevel = 3;
                crew.count = legacyWorkforceReplay ? 3 : WorkforceRules.Limit(crew.id);
                crew.carryLevel = crew.role == "Cashier" ? 1 : 3;
            }
            state.maximumBuilt = true;
            EnsureTruck(state);
            UnlockCrops(state);
            RefreshProgression(state);
            return 1;
        }
    }
}
