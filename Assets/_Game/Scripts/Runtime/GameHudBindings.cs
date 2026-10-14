using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class GameHud
    {
        GameObject bindingsPanel;
        Text bindingsMessage;
        string capturingBinding;
        readonly List<(string id, string label, Button button)> bindingButtons = new();

        void BuildBindings()
        {
            bindingsPanel = Modal("BindingsPanel");
            var board = ManagementBoard(bindingsPanel.transform, "TÙY CHỈNH PHÍM",
                "Chọn thao tác rồi nhấn phím mới • Esc hủy • tự lưu");
            for (int i = 0; i < GameKeyBindings.Options.Length; i++)
            {
                var option = GameKeyBindings.Options[i];
                var button = Button(board.transform, option.label, () => BeginBinding(option.id));
                button.name = "Bind_" + option.id;
                int column = i / 5, row = i % 5;
                float left = .05f + column * .47f, top = .77f - row * .103f;
                Rect((RectTransform)button.transform, new(left, top - .077f), new(left + .43f, top), Vector2.zero, Vector2.zero);
                bindingButtons.Add((option.id, option.label, button));
            }
            bindingsMessage = Text("BindingsMessage", board.transform, "", 18, TextAnchor.MiddleCenter, Muted);
            Rect(bindingsMessage.rectTransform, new(.04f, .16f), new(.96f, .22f), Vector2.zero, Vector2.zero);
            var reset = Button(board.transform, "Khôi phục mặc định", ResetBindings);
            reset.name = "ResetBindings";
            Rect((RectTransform)reset.transform, new(.13f, .035f), new(.47f, .12f), Vector2.zero, Vector2.zero);
            var close = Button(board.transform, "Đóng", CloseBindings);
            close.name = "CloseBindings";
            Rect((RectTransform)close.transform, new(.53f, .035f), new(.87f, .12f), Vector2.zero, Vector2.zero);
            bindingsPanel.SetActive(false);
        }

        public void OpenBindings()
        {
            ShowScreen(ScreenMode.Bindings);
            RefreshBindingButtons();
            SetText(bindingsMessage, "Phím mũi tên và gamepad vẫn dùng được.");
            Select(bindingButtons[0].button);
        }

        public void CloseBindings()
        {
            capturingBinding = null;
            ShowScreen(ScreenMode.Settings);
        }

        void BeginBinding(string id)
        {
            capturingBinding = id;
            SetText(bindingsMessage, "Nhấn phím mới, hoặc Esc để hủy.");
            if (UnityEngine.EventSystems.EventSystem.current)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        bool UpdateBindingCapture()
        {
            if (capturingBinding == null) return false;
            if (Keyboard.current == null) return true;
            foreach (var key in Keyboard.current.allKeys)
            {
                if (!key.wasPressedThisFrame) continue;
                if (key.keyCode == Key.Escape)
                { capturingBinding = null; SetText(bindingsMessage, "Đã hủy đổi phím."); return true; }
                if (!GameKeyBindings.Set(capturingBinding, "<Keyboard>/" + key.name, out string reason))
                { SetText(bindingsMessage, reason); return true; }
                capturingBinding = null;
                GameSettingsStore.SaveAndApply();
                RefreshBindingButtons();
                SetText(bindingsMessage, "Đã lưu phím mới.");
                return true;
            }
            return true;
        }

        void ResetBindings()
        {
            capturingBinding = null;
            GameSettingsStore.Current.keyBindings.Clear();
            GameSettingsStore.SaveAndApply();
            RefreshBindingButtons();
            SetText(bindingsMessage, "Đã khôi phục phím mặc định.");
        }

        void RefreshBindingButtons()
        {
            foreach (var row in bindingButtons)
                row.button.GetComponentInChildren<Text>().text = GameLocalization.Translate(row.label)
                    + "  :  " + GameKeyBindings.Label(row.id);
        }
    }
}
