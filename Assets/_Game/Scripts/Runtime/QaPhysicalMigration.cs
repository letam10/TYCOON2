using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator PhysicalMigrationAcceptance()
        {
            float originalScale = Time.timeScale;
            Time.timeScale = 0;
            yield return PhysicalMigrationReady();
            string originalPath = Path.GetFullPath(game.SavePath);
            string directory = Path.GetFullPath(game.QaDirectory).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            Check(game.IsQa && originalPath.StartsWith(directory, StringComparison.OrdinalIgnoreCase),
                "migration: fixture and starter checkpoint stay inside QA directory");
            game.Player.CanControl = false;
            game.SaveGame();
            game.Transactions.DrainCheckpoint();
            var original = SaveStore.Read(originalPath);
            Check(original?.transactionState != null, "migration: starter checkpoint preserved");
            string fixturePath = Path.Combine(directory, "physical-migration-live.json");
            string sourcePath = Path.Combine(directory, "physical-migration-legacy.json");
            var counter = game.Checkouts.First(x => x.ShopId == "farm");
            var fixture = PhysicalMigrationFixture(original, counter.Id);
            var command = new TransactionCommand
            {
                kind = TransactionKind.ContributePurchase,
                actor = "player",
                target = "carry10",
                quantity = 15,
                key = "qa-legacy-carry-contribution",
                effectId = "qa-legacy-carry-contribution-effect",
                expectedRevision = 0
            };
            try
            {
                game.Transactions.Detach();
                game.SavePath = fixturePath;
                File.WriteAllText(fixturePath + ".journal", "");
                SaveStore.Write(fixturePath, fixture);
                SaveStore.Write(sourcePath, fixture);
                // Ghi journal thật nhưng chưa checkpoint để mô phỏng lần thoát giữa hai lần lưu.
                using (var store = new GameplayTransactionStore(game))
                {
                    var legacy = new TransactionCore(fixture.transactionState, store,
                        () => fixture.transactionState.simulationTime);
                    Check(legacy.Execute(command).amount == 15, "migration: legacy journal spends wallet 15");
                }
                File.Copy(fixturePath + ".journal", sourcePath + ".journal", true);
                var disk = JsonUtility.FromJson<SaveData>(File.ReadAllText(fixturePath));
                Check(disk.version == 2 && disk.money == 500 && disk.transactionState.revision == 0,
                    "migration: disk checkpoint remains schema 2 wallet 500 before replay");
                var recovered = SaveStore.Read(fixturePath);
                Check(recovered.version == 2 && recovered.transactionState.schemaVersion == 2 &&
                    recovered.money == 485 && recovered.cashInSafe == 0 && recovered.cashInHand == 0,
                    "migration: real SaveStore replays schema 2 journal before cash conversion");
                Check(recovered.purchases.Single(x => x.id == "carry10").paid == 50,
                    "migration: journal contribution adds 15 to saved partial progress 35");
                game.LoadGame();
                var ready = PhysicalMigrationReady();
                while (ready.MoveNext()) yield return ready.Current;
                CheckPhysicalMigration(counter.Id, "first load");
                report.playerHeight = Bounds(game.Player.gameObject).size.y;
                // View bỏ lịch sử để nhẹ mỗi frame; kiểm chứng biên nhận phải dùng Snapshot đầy đủ.
                var migratedState = game.Transactions.Snapshot();
                string receipt = migratedState.receipts.Single().id;
                Check(receipt == "receipt:" + command.key, "migration: legacy command receipt keeps its exact ID");
                string migrated = JsonUtility.ToJson(migratedState);
                for (int pass = 1; pass <= 2; pass++)
                {
                    // Journal cũ vẫn còn: load phải bỏ qua revision đã có trong checkpoint schema 3.
                    Check(File.ReadAllText(fixturePath + ".journal").Length > 0,
                        "migration: reload " + pass + " includes checkpointed legacy journal");
                    game.LoadGame();
                    ready = PhysicalMigrationReady();
                    while (ready.MoveNext()) yield return ready.Current;
                    CheckPhysicalMigration(counter.Id, "reload " + pass);
                    Check(game.Transactions.Execute(command).id == receipt,
                        "migration: reload " + pass + " returns the same duplicate command receipt");
                    Check(JsonUtility.ToJson(game.Transactions.Snapshot()) == migrated,
                        "migration: reload " + pass + " duplicates no money goods progress or events");
                }
                game.SaveGame();
                game.Transactions.DrainCheckpoint();
                SaveStore.Write(Path.Combine(directory, "physical-migration-final.json"),
                    SaveStore.Read(fixturePath));
                Check(SaveStore.Read(fixturePath).transactionState.revision ==
                    game.Transactions.Snapshot().revision,
                    "migration: final checkpoint and retained journal recover the exact revision");
                Check(game.RuntimeErrors.Count == 0, "migration: no Player runtime errors");
            }
            finally
            {
                game.Transactions?.Detach();
                game.SavePath = originalPath;
                game.LoadGame();
                Time.timeScale = originalScale;
            }
            Time.timeScale = 0;
            yield return PhysicalMigrationReady();
            CheckRecoveredData(original, "migration starter restore");
            Check(game.Economy.CashInHand == original.cashInHand &&
                game.Economy.CashInSafe == original.cashInSafe,
                "migration: original QA starter cash restored");
            Time.timeScale = originalScale;
        }

        SaveData PhysicalMigrationFixture(SaveData original, string counter)
        {
            var data = TransactionCore.Copy(original);
            var state = data.transactionState;
            Check(state.crews.Count == 0 && state.crates.Count == 0 && state.truck == null &&
                state.stations.All(x => !x.running), "migration: isolated fresh starter has no active jobs");
            data.customers.Clear();
            data.diners.Clear();
            data.workers.Clear();
            state.owners.RemoveAll(x => x.kind == OwnerKind.Customer);
            state.stacks.Clear();
            state.reservations.Clear();
            state.orders.Clear();
            state.payments.Clear();
            state.receipts.Clear();
            state.outbox.Clear();
            state.consumers.Clear();
            state.purchases.Clear();
            state.legacyPaid.Clear();
            state.legacyLosses.Clear();
            state.schemaVersion = 2;
            state.revision = 0;
            state.money = 500;
            state.cashInHand = 0;
            state.cashInSafe = 0;
            state.revenue = 84;
            state.legacyRevenue = 30;
            state.legacyTransactions = 1;
            state.cashCollected = 30;
            state.legacyPaid.Add(7701);
            foreach (var cash in state.legacyCash) cash.amount = 0;
            state.unlocked.RemoveAll(x => x is "carry10" or "carry16" or "carry24");
            var player = state.owners.Single(x => x.id == "player");
            player.capacity = 6;
            state.stacks.Add(new ItemStackState
            {
                id = "qa-legacy-hand-carrots",
                item = "carrot",
                quantity = 5,
                owner = player.id,
                location = player.location
            });
            state.owners.Add(new OwnerState
            {
                id = "customer:7702",
                actor = "customer:7702",
                location = "customer:7702",
                kind = OwnerKind.Customer,
                capacity = 3
            });
            state.stacks.Add(new ItemStackState
            {
                id = "qa-legacy-delivered-carrots",
                item = "carrot",
                quantity = 3,
                owner = "customer:7702",
                location = "customer:7702"
            });
            state.orders.Add(new OrderRuntimeState
            {
                id = "order:7702",
                customer = "customer:7702",
                counter = counter,
                deadline = state.simulationTime + 90,
                status = OrderStatus.Complete,
                lines = new() { new OrderLine("carrot", 3, 18) { delivered = 3 } }
            });
            state.payments.Add(new PaymentState
            {
                id = "payment:order:7702",
                order = "order:7702",
                counter = counter,
                amount = 54
            });
            state.purchases.Add(new PurchaseRuntimeState
            {
                id = "carry10",
                definitionId = "carry10",
                definitionVersion = Definitions.Upgrade("carry10").version,
                contributed = 35
            });
            TransactionCore.Validate(state);
            return RuntimeTransactions.Project(state, data);
        }

        IEnumerator PhysicalMigrationReady()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            while (!game.NavigationReady || !game.CanSimulate || game.Transactions == null)
            {
                if (game.SaveBlocked || Time.realtimeSinceStartupAsDouble > deadline)
                    throw new Exception("Physical migration restore failed or exceeded 30 real seconds.");
                yield return null;
            }
            game.Player.CanControl = false;
        }

        void CheckPhysicalMigration(string counter, string stage)
        {
            var state = game.Transactions.Snapshot();
            var data = game.CaptureSaveData();
            Check(state.schemaVersion == 3 && state.money == 0 && state.cashInHand == 0 &&
                state.cashInSafe == 485 && game.Economy.CashInHand == 0 && game.Economy.CashInSafe == 485,
                "migration " + stage + ": recovered wallet 485 moves to safe exactly once");
            Check(game.Player.Carry.Total == 5 && game.Player.Carry.Count("carrot") == 5 &&
                state.stacks.Single(x => x.id == "qa-legacy-hand-carrots").quantity == 5 &&
                state.stacks.Single(x => x.id == "qa-legacy-delivered-carrots").quantity == 3 &&
                state.stacks.Sum(x => x.quantity) == 8,
                "migration " + stage + ": player owns 5 carrots and paid customer owns 3");
            Check(game.Player.Carry.Capacity == 12 && state.owners.Single(x => x.id == "player").capacity == 12,
                "migration " + stage + ": capacity derives from starter tier 6 to 12 without compounding");
            var purchase = state.purchases.Single(x => x.id == "carry10");
            Check(purchase.contributed == 50 && !purchase.complete && !state.unlocked.Contains("carry10") &&
                game.Contribution("carry10") == 50,
                "migration " + stage + ": carry10 remains partly funded at 50 of 100");
            Check(game.Checkouts.Single(x => x.Id == counter).Cash == 54 && game.Economy.PendingCash == 54 &&
                game.Economy.Revenue == 84 && game.Economy.CashCollected == 30 && game.Economy.Transactions == 2,
                "migration " + stage + ": counter cash 54 revenue 84 collected 30 and 2 sales retained");
            var order = state.orders.Single();
            var payment = state.payments.Single();
            Check(order.id == "order:7702" && order.status == OrderStatus.Complete &&
                order.lines.Single().delivered == 3 && payment.id == "payment:order:7702" &&
                payment.amount == 54 && !payment.collected &&
                data.receipts.OrderBy(x => x).SequenceEqual(new long[] { 7701, 7702 }),
                "migration " + stage + ": completed order payment and paid receipt IDs retained");
            Check(state.revision == 1 && state.receipts.Count == 1 && state.outbox.Count == 1,
                "migration " + stage + ": one recovered command receipt and event only");
        }
    }
}
