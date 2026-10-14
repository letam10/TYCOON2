using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class GameHud
    {
        GameObject saveSlotsPanel;
        ScreenMode slotsReturnMode;
        Text slotsMessage;
        readonly List<(int slot, Text label, Button load)> slotRows = new();
        int confirmSlot;
        bool confirmNew;

        void BuildSaveSlots()
        {
            saveSlotsPanel = Modal("SaveSlotsPanel");
            var board = ManagementBoard(saveSlotsPanel.transform, "SLOT LƯU TRÒ CHƠI",
                "Mỗi slot là một tiến trình riêng • slot 1 giữ save cũ");
            for (int i = 1; i <= GameSaveSlots.Count; i++)
            {
                int slot = i;
                float top = .77f - (i - 1) * .175f;
                var row = Panel("SaveSlot_" + i, board.transform, Card);
                Rect((RectTransform)row.transform, new(.04f, top - .14f), new(.96f, top), Vector2.zero, Vector2.zero);
                var label = Text("SlotInfo", row.transform, "", 22, TextAnchor.MiddleLeft, Ink);
                Rect(label.rectTransform, new(.025f, .14f), new(.48f, .88f), Vector2.zero, Vector2.zero);
                label.resizeTextForBestFit = true; label.resizeTextMinSize = 16;
                var save = Button(row.transform, "Lưu", () => SaveSlotAction(slot, false));
                save.name = "SaveToSlot_" + i;
                Rect((RectTransform)save.transform, new(.51f, .19f), new(.64f, .81f), Vector2.zero, Vector2.zero);
                var load = Button(row.transform, "Tải", () => LoadSlotAction(slot));
                load.name = "LoadSlot_" + i;
                Rect((RectTransform)load.transform, new(.66f, .19f), new(.79f, .81f), Vector2.zero, Vector2.zero);
                var create = Button(row.transform, "Chơi mới", () => SaveSlotAction(slot, true));
                create.name = "NewSlot_" + i;
                Rect((RectTransform)create.transform, new(.81f, .19f), new(.975f, .81f), Vector2.zero, Vector2.zero);
                slotRows.Add((slot, label, load));
            }
            slotsMessage = Text("SlotsMessage", board.transform, "", 19, TextAnchor.MiddleCenter, Muted);
            Rect(slotsMessage.rectTransform, new(.04f, .17f), new(.96f, .24f), Vector2.zero, Vector2.zero);
            FooterButton(board.transform, "Đóng", CloseSaveSlots).name = "CloseSaveSlots";
            saveSlotsPanel.SetActive(false);
        }

        public void OpenSaveSlots()
        {
            slotsReturnMode = mode;
            confirmSlot = 0;
            ShowScreen(ScreenMode.Saves);
            RefreshSaveSlots();
            SetText(slotsMessage, "Lưu nhanh dùng slot đang chơi: " + game.ActiveSaveSlot);
        }

        public void CloseSaveSlots()
        {
            confirmSlot = 0;
            ShowScreen(slotsReturnMode);
        }

        void SaveSlotAction(int slot, bool create)
        {
            bool exists = File.Exists(game.SlotPath(slot));
            bool needsConfirmation = exists && (create || slot != game.ActiveSaveSlot);
            if (needsConfirmation && (confirmSlot != slot || confirmNew != create))
            {
                confirmSlot = slot; confirmNew = create;
                SetText(slotsMessage, "Bấm lại " + (create ? "Chơi mới" : "Lưu") + " ở slot " + slot
                    + " để ghi đè. Save trước sẽ được giữ dự phòng.");
                return;
            }
            confirmSlot = 0;
            bool ok = create ? game.OpenSaveSlot(slot, true, out string message, needsConfirmation)
                : game.SaveToSlot(slot, out message);
            SetText(slotsMessage, message);
            RefreshSaveSlots();
            if (ok && create) ShowScreen(ScreenMode.Play);
        }

        void LoadSlotAction(int slot)
        {
            confirmSlot = 0;
            if (game.OpenSaveSlot(slot, false, out string message)) ShowScreen(ScreenMode.Play);
            else SetText(slotsMessage, message);
        }

        void RefreshSaveSlots()
        {
            foreach (var row in slotRows)
            {
                string path = game.SlotPath(row.slot);
                bool exists = File.Exists(path);
                string description = exists ? File.GetLastWriteTime(path).ToString("dd/MM/yyyy HH:mm") : "Trống";
                row.label.text = "Slot " + row.slot + (row.slot == game.ActiveSaveSlot ? " • đang chơi" : "")
                    + "\n" + GameLocalization.Translate(description);
                row.load.interactable = exists;
            }
        }
    }
}
