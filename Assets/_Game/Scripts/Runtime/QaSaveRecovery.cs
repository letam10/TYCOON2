using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator SaveRecovery()
        {
            Time.timeScale = 0;
            string source = GameSession.Argument("--qa-recovery-source", "");
            var original = SaveStore.Read(source);
            Check(original != null, "legacy source fixture exists");
            yield return RecoveryReady();
            Check(game.Player.CanControl, "Play restores player control");
            game.Player.CanControl = false;
            CheckRecoveredData(original, "legacy");
            Check(!game.Transactions.Snapshot().stations.Any(station =>
                Definitions.Upgrades.Any(upgrade => upgrade.kind == "legacy" && "pad_" + upgrade.id == station.id)),
                "retired pads removed from transaction bookkeeping");
            yield return Capture("01-legacy-save-restored.png");

            game.SaveGame();
            var migrated = game.CaptureSaveData();
            for (int pass = 0; pass < 2; pass++)
            {
                game.LoadGame();
                yield return RecoveryReady();
                game.Player.CanControl = false;
                CheckRecoveredData(migrated, "reload " + (pass + 1));
            }

            Check(game.Player.Carry.Total == 0, "recovered starter has empty carry");
            Time.timeScale = 1;
            game.Player.CanControl = true;
            var plot = game.Producers.First(station => station.Id == "field_carrot");
            yield return Travel(Near(plot));
            yield return TownWait(() => game.Player.Carry.Count("carrot") >= 6, 35, "recovered starter harvest");
            var counter = game.Checkouts.First(station => station.ShopId == "farm");
            long startingMoney = game.Economy.Money;
            yield return Travel(Near(counter));
            yield return TownWait(() => counter.Cash > 0, 55, "recovered starter serves natural customer");
            Check(game.Economy.Money == startingMoney, "delivery pays counter before wallet");
            yield return Travel(counter.CollectionPoint);
            yield return TownWait(() => game.Economy.Money > startingMoney, 5, "recovered starter collects cash");
            Check(game.Economy.CashCollected > original.cashCollected, "restored save earns real collected cash");

            Time.timeScale = 0;
            game.Player.CanControl = false;
            game.SaveGame();
            var earned = game.CaptureSaveData();
            game.LoadGame();
            yield return RecoveryReady();
            game.Player.CanControl = false;
            CheckRecoveredData(earned, "earned cash reload");
            yield return Capture("02-recovered-starter-cash.png");
            Time.timeScale = 1;
        }

        IEnumerator RecoveryReady()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 30;
            while (!game.NavigationReady || game.IsRestoring)
            {
                Check(!game.SaveBlocked, "restore does not block gameplay");
                if (Time.realtimeSinceStartupAsDouble > deadline)
                    throw new Exception("Restore did not resume within 30 seconds.");
                yield return null;
            }
            Check(!game.SaveBlocked && game.CanSimulate && game.Transactions != null,
                "save restores into a ready simulation");
        }

        void CheckRecoveredData(SaveData expected, string label)
        {
            var actual = game.CaptureSaveData();
            Check(actual.money == expected.money && actual.revenue == expected.revenue &&
                actual.cashCollected == expected.cashCollected && actual.pendingCash == expected.pendingCash &&
                actual.transactions == expected.transactions, label + " preserves money and accounting");
            Check(actual.unlocked.OrderBy(id => id).SequenceEqual(expected.unlocked.OrderBy(id => id)),
                label + " preserves unlocks");
            Check(actual.receipts.OrderBy(id => id).SequenceEqual(expected.receipts.OrderBy(id => id)),
                label + " preserves paid receipts");
            foreach (var inventory in expected.inventories)
            {
                var restored = actual.inventories.Find(owner => owner.id == inventory.id);
                Check(restored != null && InventorySignature(restored) == InventorySignature(inventory),
                    label + " preserves inventory " + inventory.id);
            }
        }

        static string InventorySignature(InventorySave inventory)
        {
            return string.Join(";", inventory.items.OrderBy(item => item.id)
                .Select(item => item.id + ":" + item.count));
        }
    }
}
