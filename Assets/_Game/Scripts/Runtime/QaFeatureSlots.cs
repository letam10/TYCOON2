using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable] sealed class FeatureSaveExpected
        {
            public long cash;
            public string slotOneHash;
            public float x, z;
        }

        IEnumerator FeatureSlotsAcceptance()
        {
            Check(DevelopmentAssistance.ApplyCashOnly(game), "QA uses real money mod to distinguish saved towns");
            Check(DevelopmentAssistance.ApplyMaximum(game), "QA creates real completed businesses before testing new-game reset");
            yield return CompletionWait(() => game.Workers.Count == game.CrewStates.Sum(x => x.count)
                && game.Workers.Count > 0, 30, "Completed town restores every purchased employee");
            long cash = game.Economy.CashInSafe;
            yield return FeatureKey(Key.F6);
            yield return CompletionWait(() => !game.Transactions.CheckpointPending, 5, "Rebound F6 finishes checkpoint");
            Check(SaveStore.Read(game.SavePath).cashInSafe == cash, "Rebound quick-save writes current slot");
            game.Hud.OpenSaveSlots();
            FeatureClick("SaveToSlot_3"); yield return null;
            Check(SaveStore.Read(game.SlotPath(3)).cashInSafe == cash, "UI saves a copy into independent slot 3");
            string oneHash = FeatureHash(game.SlotPath(1));
            yield return Capture("save-slots.png");
            FeatureClick("NewSlot_2");
            yield return CompletionWait(() => !game.IsRestoring && game.Player.CanControl, 20, "New slot restores fresh world");
            Check(game.ActiveSaveSlot == 2 && game.Economy.CashInSafe == 0 && game.Economy.CashInHand == 0,
                "Fresh slot has independent starting money");
            Check(Vector3.Distance(game.Player.transform.position, CityDistricts.PlayerSpawn) < .4f,
                "New slot spawns directly in front of starter TYCOON");
            Check(game.Workers.Count == 0 && game.BusinessStage == 1 && game.Player.Carry.Total == 0,
                "New slot clears old progression, employees and carried goods");
            Check(FeatureHash(game.SlotPath(1)) == oneHash, "Creating slot 2 leaves original slot 1 unchanged");
            game.Hud.OpenSaveSlots(); FeatureClick("LoadSlot_3");
            yield return CompletionWait(() => !game.IsRestoring && game.Player.CanControl, 20, "Slot 3 restores saved progress");
            Check(game.ActiveSaveSlot == 3 && game.Economy.CashInSafe == cash, "Loading slot 3 restores its exact money");
            Check(game.BusinessStage == 5 && game.Workers.Count > 0,
                "Slot 3 restores completed businesses and employees after a fresh town");
            Check(FeatureHash(game.SlotPath(1)) == oneHash, "Loading another slot leaves legacy slot 1 unchanged");
            Check(SaveStore.Read(game.SlotPath(2)).cashInSafe == 0, "Slot 2 keeps its independent new-game save");
            game.Hud.OpenSaveSlots(); FeatureClick("NewSlot_3"); yield return null;
            Check(game.Economy.CashInSafe == cash && game.ActiveSaveSlot == 3,
                "Overwrite new-game button requires a second confirmation");
            FeatureClick("CloseSaveSlots");
            game.Hud.OpenSaveSlots();
            FeatureClick("SaveToSlot_2");
            yield return null;
            Check(SaveStore.Read(game.SlotPath(2)).cashInSafe == 0, "Copy overwrite requires confirmation");
            FeatureClick("SaveToSlot_2");
            yield return null;
            Check(SaveStore.Read(game.SlotPath(2)).cashInSafe == cash, "Confirmed copy overwrites only slot 2");
            Check(SaveStore.Read(game.SlotPath(2) + ".previous.json").cashInSafe == 0,
                "Copy overwrite retains the previous fresh checkpoint");
            FeatureClick("NewSlot_2");
            yield return null;
            FeatureClick("NewSlot_2");
            yield return CompletionWait(() => !game.IsRestoring && game.Player.CanControl, 20,
                "Confirmed new-game overwrite restores fresh slot");
            Check(game.Economy.CashInSafe == 0 && game.BusinessStage == 1, "Confirmed new game resets its chosen slot");
            Check(SaveStore.Read(game.SlotPath(2) + ".previous.json").cashInSafe == cash,
                "New-game overwrite retains the completed town checkpoint");
            game.Hud.OpenSaveSlots();
            FeatureClick("LoadSlot_3");
            yield return CompletionWait(() => !game.IsRestoring && game.Player.CanControl, 20,
                "Slot 3 restores after confirmed overwrite of another slot");
            Check(game.Economy.CashInSafe == cash && FeatureHash(game.SlotPath(1)) == oneHash,
                "Confirmed overwrite preserves slot 3 and the original slot 1");
            game.SaveGame(); yield return CompletionWait(() => !game.Transactions.CheckpointPending, 5, "Final slot checkpoint finishes");
            File.WriteAllText(Path.Combine(game.QaDirectory, "feature-save-expected.json"), JsonUtility.ToJson(new FeatureSaveExpected
            {
                cash = cash, slotOneHash = oneHash, x = game.Player.transform.position.x, z = game.Player.transform.position.z
            }, true));
        }

        IEnumerator FeatureSlotsRelaunch()
        {
            yield return CompletionWait(() => game.NavigationReady && !game.IsRestoring && game.Player.CanControl,
                30, "Separate process restores selected save slot");
            var expected = JsonUtility.FromJson<FeatureSaveExpected>(File.ReadAllText(
                Path.Combine(game.QaDirectory, "feature-save-expected.json")));
            Check(game.ActiveSaveSlot == 3 && game.SavePath == game.SlotPath(3), "Selected slot persists across a new game process");
            Check(game.Economy.CashInSafe == expected.cash, "Separate process restores exact slot money");
            Check(game.BusinessStage == 5 && game.Workers.Count > 0, "Separate process restores completed town progression");
            Check(Mathf.Abs(game.Player.transform.position.x - expected.x) < .01f
                && Mathf.Abs(game.Player.transform.position.z - expected.z) < .01f, "Separate process restores saved player position");
            Check(FeatureHash(game.SlotPath(1)) == expected.slotOneHash, "Relaunch preserves original legacy save");
            Check(GameKeyBindings.Path("map") == "<Keyboard>/p" && GameKeyBindings.Path("forward") == "<Keyboard>/i"
                && GameKeyBindings.Path("save") == "<Keyboard>/f6", "Custom keys survive native process relaunch");
            yield return FeatureKey(Key.P);
            Check(game.Hud.MapOpen, "Restored custom key opens map in a new process");
            yield return Capture("relaunch-map.png");
            yield return FeatureKey(Key.P);
            Check(game.RuntimeErrors.Count == 0, "Separate-process feature verification has no runtime errors");
        }

        static string FeatureHash(string path)
        {
            var saved = SaveStore.Read(path);
            // Đổi slot có checkpoint cuối của slot hiện tại; chỉ bỏ thời gian ghi, vẫn so toàn bộ tiến trình.
            saved.savedAt = "";
            using var sha = SHA256.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(saved));
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }
    }
}
