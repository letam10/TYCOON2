using System.Collections.Generic;

namespace Tycoon
{
    public static partial class GameLocalization
    {
        static void AddFeatureMenus(Dictionary<string, string> entries)
        {
            entries["đang chơi"] = "active";
            entries["Lưu nhanh dùng slot đang chơi"] = "Quick save uses the active slot";
            entries["Nhấn phím mới, hoặc Esc để hủy."] = "Press a new key, or Esc to cancel.";
            entries["Đã hủy đổi phím."] = "Key change cancelled.";
            entries["Đã lưu phím mới."] = "New key saved.";
            entries["Đã khôi phục phím mặc định."] = "Default keys restored.";
            entries["Phím này dành cho điều khiển dự phòng."] = "This key is reserved for fallback controls.";
            entries["Chọn một phím bàn phím."] = "Choose a keyboard key.";
            entries["Phím đã được dùng"] = "Key already in use";
            entries["Bản đồ chưa sẵn sàng."] = "The map is not ready yet.";
            entries["Điểm dịch chuyển đang bị chặn."] = "This travel destination is blocked.";
            entries["Đã đến"] = "Arrived at";
            entries["Trò chơi chưa sẵn sàng để lưu."] = "The game is not ready to save yet.";
            entries["Đã lưu vào slot"] = "Saved to slot";
            entries["Không lưu được"] = "Could not save";
            entries["Trò chơi chưa sẵn sàng để đổi slot."] = "The game is not ready to switch slots yet.";
            entries["Slot đã có save. Chọn một slot trống để chơi mới."] =
                "This slot already has a save. Choose an empty slot for a new game.";
            entries["Slot này chưa có save."] = "This slot has no save yet.";
            entries["Không tải được slot; đã trở lại save trước."] =
                "Could not load the slot; restored the previous save.";
            entries["Đã tạo trò chơi mới ở slot"] = "Started a new game in slot";
            entries["Đã tải slot"] = "Loaded slot";
            entries["Không đổi được slot"] = "Could not switch slots";
            // Dịch cả cụm trước số slot để giữ đúng thứ tự từ trong thông báo xác nhận.
            entries["Bấm lại Chơi mới ở slot"] = "Press New game again in slot";
            entries["Bấm lại Lưu ở slot"] = "Press Save again in slot";
            entries["để ghi đè. Save trước sẽ được giữ dự phòng."] =
                "to overwrite. The previous save will be kept as a backup.";
        }
    }
}
