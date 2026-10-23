using System;
using System.IO;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class GameSession
    {
        string freshTownJson;
        public int ActiveSaveSlot { get; private set; } = 1;
        public string SlotDirectory => IsQa
            ? QaDirectory
            : GameSaveSlots.RedesignDirectory(Application.persistentDataPath);
        public string SlotPath(int slot) => GameSaveSlots.PathFor(SlotDirectory, slot, IsQa);

        void CaptureFreshTown()
        {
            var data = CaptureSaveData();
            var state = RuntimeTransactions.Migrate(this, data);
            PhysicalCashMigration.Upgrade(state);
            // Snapshot mới đã ở layout hiện hành; không cho loader dịch như save legacy lần nữa.
            state.layoutRevision = TownLayout.Revision;
            RuntimeTransactions.UpgradeCounterOwners(state, this);
            freshTownJson = JsonUtility.ToJson(RuntimeTransactions.Project(state, data));
        }

        void InitializeSaveSlots()
        {
            ActiveSaveSlot = GameSaveSlots.ReadActive(SlotDirectory);
            SavePath = SlotPath(ActiveSaveSlot);
        }

        public bool SaveToSlot(int slot, out string message)
        {
            message = "";
            if (SaveBlocked || IsRestoring) { message = "Trò chơi chưa sẵn sàng để lưu."; return false; }
            try
            {
                SaveGame(); Transactions?.DrainCheckpoint();
                string target = SlotPath(slot);
                if (!string.Equals(target, SavePath, StringComparison.OrdinalIgnoreCase))
                {
                    using var lease = GameplayTransactionStore.AcquireLease(target);
                    var data = SaveStore.Read(SavePath);
                    // Khi người chơi xác nhận ghi đè, giữ bản checkpoint trước làm bản dự phòng.
                    if (File.Exists(target)) SaveStore.Write(target + ".previous.json", SaveStore.Read(target));
                    SaveStore.Write(target, data);
                    File.WriteAllText(target + ".journal", "");
                }
                message = "Đã lưu vào slot " + slot;
                Say(message); return true;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { message = "Không lưu được: " + error.Message; Say(message); return false; }
        }

        public bool OpenSaveSlot(int slot, bool createNew, out string message, bool replace = false)
        {
            string target = SlotPath(slot);
            if (IsRestoring || SaveBlocked)
            { message = "Trò chơi chưa sẵn sàng để đổi slot."; return false; }
            if (createNew && File.Exists(target) && !replace)
            { message = "Slot đã có save. Chọn một slot trống để chơi mới."; return false; }
            if (!createNew && !File.Exists(target))
            { message = "Slot này chưa có save."; return false; }
            string previous = SavePath;
            int previousSlot = ActiveSaveSlot;
            try
            {
                SaveGame(); Transactions?.DrainCheckpoint();
                if (createNew)
                {
                    if (File.Exists(target)) SaveStore.Write(target + ".previous.json", SaveStore.Read(target));
                    SaveStore.Write(target, JsonUtility.FromJson<SaveData>(freshTownJson));
                    File.WriteAllText(target + ".journal", "");
                }
                Transactions?.Detach(); Transactions = null;
                SavePath = target;
                LoadGame();
                if (SaveBlocked)
                {
                    SaveBlocked = false; SavePath = previous; LoadGame();
                    message = "Không tải được slot; đã trở lại save trước."; return false;
                }
                if (Transactions == null) InitializeTransactions();
                ActiveSaveSlot = slot;
                GameSaveSlots.Remember(SlotDirectory, slot);
                nextAutosave = Time.time + 60;
                message = (createNew ? "Đã tạo trò chơi mới ở slot " : "Đã tải slot ") + slot;
                Say(message); return true;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                ActiveSaveSlot = previousSlot;
                if (SavePath != previous) { SavePath = previous; LoadGame(); }
                message = "Không đổi được slot: " + error.Message; Say(message); return false;
            }
        }
    }
}
