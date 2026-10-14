using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Tycoon
{
    [Serializable]
    public sealed class KeyboardBinding
    {
        public string id, path;
    }

    public static class GameKeyBindings
    {
        public static readonly (string id, string label, string key)[] Options =
        {
            ("forward", "Tiến", "w"), ("back", "Lùi", "s"),
            ("left", "Sang trái", "a"), ("right", "Sang phải", "d"),
            ("sprint", "Chạy nhanh", "leftShift"), ("withdraw", "Rút tiền", "r"),
            ("cycle", "Chọn hàng", "q"), ("save", "Lưu nhanh", "f5"),
            ("menu", "Menu", "escape"), ("map", "Bản đồ", "m")
        };

        public static string Path(string id)
        {
            var saved = GameSettingsStore.Current.keyBindings.Find(x => x.id == id);
            if (saved != null) return saved.path;
            foreach (var option in Options)
                if (option.id == id) return "<Keyboard>/" + option.key;
            throw new ArgumentException("Phím không tồn tại: " + id);
        }

        public static string Label(string id) => InputControlPath.ToHumanReadableString(Path(id),
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        public static bool Set(string id, string path, out string reason)
        {
            string reserved = path.ToLowerInvariant() switch
            {
                "<keyboard>/uparrow" => "forward", "<keyboard>/downarrow" => "back",
                "<keyboard>/leftarrow" => "left", "<keyboard>/rightarrow" => "right",
                "<keyboard>/rightshift" => "sprint", "<keyboard>/escape" => "menu", _ => null
            };
            if (reserved != null && reserved != id)
            { reason = "Phím này dành cho điều khiển dự phòng."; return false; }
            if (!path.StartsWith("<Keyboard>/", StringComparison.Ordinal)
                || InputSystem.FindControl(path) == null)
            { reason = "Chọn một phím bàn phím."; return false; }
            foreach (var option in Options)
                if (option.id != id && string.Equals(Path(option.id), path, StringComparison.OrdinalIgnoreCase))
                { reason = "Phím đã được dùng: " + GameLocalization.Translate(option.label); return false; }
            var bindings = GameSettingsStore.Current.keyBindings;
            bindings.RemoveAll(x => x.id == id);
            bindings.Add(new KeyboardBinding { id = id, path = path });
            reason = "";
            return true;
        }

        public static void Apply(InputAction action, int index, string id)
        {
            bool enabled = action.enabled;
            action.Disable();
            action.ApplyBindingOverride(index, Path(id));
            if (enabled) action.Enable();
        }

        public static void ApplyPlayer(PlayerController player)
        {
            if (player.Move == null) return;
            Apply(player.Move, 1, "forward"); Apply(player.Move, 2, "back");
            Apply(player.Move, 3, "left"); Apply(player.Move, 4, "right");
            Apply(player.Sprint, 0, "sprint"); Apply(player.Withdraw, 0, "withdraw");
            Apply(player.Cycle, 0, "cycle"); Apply(player.Save, 0, "save");
        }
    }
}
