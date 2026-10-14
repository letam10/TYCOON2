using System.Collections.Generic;

namespace Tycoon
{
    public static partial class GameLocalization
    {
        static void AddMenus(Dictionary<string, string> d)
        {
            d["Bản đồ"] = "Map";
            d["BẢN ĐỒ THÀNH PHỐ"] = "CITY MAP";
            d["Chọn điểm vàng để dịch chuyển • mũi tên trắng là bạn"] = "Choose a gold point to travel • the white arrow is you";
            d["Dịch chuyển miễn phí, giữ nguyên hàng và tiền."] = "Free travel keeps your carried goods and cash.";
            d["TYCOON • Nông trại"] = "TYCOON • Farm";
            d["Công viên"] = "Park";
            d["Nhà ga"] = "Rail station";
            d["Bãi biển"] = "Beach";
            d["Bến cảng"] = "Harbor";
            d["Tùy chỉnh phím"] = "Key bindings";
            d["TÙY CHỈNH PHÍM"] = "KEY BINDINGS";
            d["Chọn thao tác rồi nhấn phím mới • Esc hủy • tự lưu"] = "Choose an action and press a key • Esc cancels • saves automatically";
            d["Phím mũi tên và gamepad vẫn dùng được."] = "Arrow keys and gamepad remain available.";
            d["Tiến"] = "Forward"; d["Lùi"] = "Back";
            d["Sang trái"] = "Left"; d["Sang phải"] = "Right";
            d["Chạy nhanh"] = "Sprint"; d["Rút tiền"] = "Withdraw cash";
            d["Chọn hàng"] = "Select item"; d["Lưu nhanh"] = "Quick save";
            d["Di chuyển"] = "Move"; d["Lưu"] = "Save"; d["Tải"] = "Load";
            d["Khôi phục mặc định"] = "Restore defaults";
            d["Slot lưu • Chơi mới"] = "Save slots • New game";
            d["SLOT LƯU TRÒ CHƠI"] = "SAVE SLOTS";
            d["Mỗi slot là một tiến trình riêng • slot 1 giữ save cũ"] = "Each slot is separate • slot 1 keeps your existing save";
            d["Chơi mới"] = "New game"; d["Trống"] = "Empty";
            d["Cài đặt"] = "Settings";
            d["CÀI ĐẶT"] = "SETTINGS";
            d["Đồ họa, ngôn ngữ và tốc độ khung hình"] = "Graphics, language and frame rate";
            d["Chất lượng"] = "Quality";
            d["Thấp"] = "Low";
            d["Cân bằng"] = "Balanced";
            d["Cao"] = "High";
            d["Độ phân giải"] = "Resolution";
            d["Toàn màn hình"] = "Fullscreen";
            d["Đồng bộ màn hình (VSync)"] = "Display sync (VSync)";
            d["Giới hạn FPS"] = "FPS limit";
            d["Tỷ lệ dựng hình"] = "Render scale";
            d["Ngôn ngữ"] = "Language";
            d["Hiển thị FPS"] = "Show FPS";
            d["Bật / tắt"] = "On / off";
            d["Áp dụng"] = "Apply";
            d["Thay đổi được lưu khi bấm Áp dụng."] = "Press Apply to save your changes.";
            d["Đã lưu cài đặt."] = "Settings saved.";
            d["Đóng"] = "Close";
            d["Quay lại"] = "Back";
            d["Tiếp tục"] = "Continue";
            d["TIỀN TRÊN TAY"] = "CASH IN HAND";
            d["TIỀN TRONG KÉT"] = "CASH IN SAFE";
            d["BƯỚC TIẾP THEO"] = "NEXT OBJECTIVE";
            d["ĐỨNG GẦN • TỰ ĐỘNG"] = "STAND NEARBY • AUTOMATIC";
            d["Đổi món"] = "Recipe";
            d["Xem kho / chọn hàng"] = "Stock / select item";
            d["Kho"] = "Stock";
            d["Xe tải"] = "Truck";
            d["Thông tin"] = "Info";
            d["TỔNG QUAN KINH DOANH"] = "BUSINESS OVERVIEW";
            d["Mục tiêu, doanh thu và kho gần bạn"] = "Objectives, revenue and nearby stock";
            d["Từ nông trại đến đế chế kinh doanh"] = "From a farm to a business empire";
            d["Thông tin kinh doanh"] = "Business information";
            d["Kho • chọn hàng"] = "Stock • select item";
            d["Vận chuyển xe tải"] = "Truck transport";
            d["Quản lý đội"] = "Manage crew";
            d["Lưu trò chơi"] = "Save game";
            d["Lưu & thoát"] = "Save & quit";
            d["Trò chơi đang tạm dừng.\nEsc / nút Back để tiếp tục."] =
                "Game paused.\nPress Esc / Back to continue.";
            d["ĐỘI NGŨ"] = "CREW";
            d["Vai trò, khu vực và nâng cấp cho từng đội"] = "Roles, areas and upgrades for each crew";
            d["KHO HÀNG"] = "WAREHOUSE";
            d["Chọn mặt hàng muốn lấy • giữ chỗ và hàng đang đến được cập nhật riêng"] =
                "Select an item to collect • reserved and incoming stock are shown separately";
            d["VẬN CHUYỂN XE TẢI"] = "TRUCK TRANSPORT";
            d["Chọn hai kho khác nhau để điều phối tuyến hàng"] = "Choose two different warehouses for a route";
            d["Đổi kho nguồn"] = "Change source";
            d["Đổi kho đích"] = "Change destination";
            d["Gửi xe"] = "Dispatch truck";
            d["Bật / tắt tự động"] = "Toggle automatic routes";
            d["Bật / tắt tuyến tự động"] = "Toggle automatic routes";
            d["Trạm cấp 3 và 30 công việc tự làm sẽ mở điểm thuê đội."] =
                "Reach station level 3 and complete 30 jobs yourself to unlock crew hiring.";
            d["Bạn có thể tự sản xuất, vận chuyển và phục vụ."] = "You can produce, transport and serve yourself.";
            d["Nâng cấp áp dụng đúng nghề và khu • tiền được trừ ngay khi chọn."] =
                "Upgrades apply to this role and area • payment is immediate.";
            d["Không dùng sức mang"] = "No carry upgrade";
            d["Một tài xế / xe"] = "One driver per truck";
            d["Tốc độ"] = "Speed";
            d["Sức mang"] = "Carry capacity";
            d["Số người"] = "Crew size";
            d["TỐI ĐA"] = "MAX";
            d["Thêm người"] = "Add worker";
            d["Đã nâng đội"] = "Crew upgraded";
            d["Không đủ xu hoặc nâng cấp đã tối đa."] = "Not enough coins, or the upgrade is already at maximum.";
            d["Chưa có kho hoặc mặt hàng đã mở."] = "No warehouse or item has been unlocked yet.";
            d["Giữ chỗ"] = "Reserved";
            d["Đang đến"] = "Incoming";
            d["Sức chứa kho"] = "Warehouse capacity";
            d["Nguồn"] = "Source";
            d["Đích"] = "Destination";
            d["Đỗ tại bến"] = "Parked at depot";
            d["Đang chất hàng"] = "Loading cargo";
            d["Đang vận chuyển"] = "Travelling";
            d["Chờ dỡ hàng"] = "Waiting to unload";
            d["Tự động"] = "Automatic";
            d["BẬT"] = "ON";
            d["TẮT"] = "OFF";
            d["Xe tải + tài xế: mở khu Chế biến, kho cấp 3 và tự chở hàng 30 chuyến."] =
                "Truck + driver: unlock Processing, warehouse level 3 and complete 30 delivery trips yourself.";
            d["Góp tiền tại điểm mua xe bên kho Chế biến."] = "Pay at the truck purchase pad by Processing storage.";
            d["Nông trại & cửa hàng"] = "Farm & shop";
            d["Nhà hàng & tiệm bánh"] = "Restaurant & bakery";
            d["Nông trại"] = "Farm";
            d["Cửa hàng nông sản"] = "Farm shop";
            d["Chế biến"] = "Processing";
            d["Siêu thị"] = "Supermarket";
            d["Tiệm bánh"] = "Bakery";
            d["Nhà hàng"] = "Restaurant";
            d["Nông dân"] = "Farmer";
            d["Chăm vật nuôi"] = "Animal worker";
            d["Xếp hàng"] = "Restocker";
            d["Bán hàng"] = "Cashier";
            d["Đầu bếp / thợ bánh"] = "Cook / baker";
            d["Phục vụ"] = "Waiter";
            d["Vận chuyển"] = "Transporter";
            d["Kỹ thuật viên sửa chữa"] = "Repair technician";
            d["Bốc hàng"] = "Loader";
            d["Tài xế"] = "Driver";
            d["nhân viên"] = "workers";
            d["người"] = "workers";
            d["đơn"] = "orders";
            d["Đang cầm"] = "Holding";
            d["Đặt vào két để mang hàng"] = "Deposit cash in the safe to carry goods";
            d["Đang mang thùng hàng"] = "Carrying a crate";
            d["Đặt thùng tại kho đích"] = "Drop the crate at its destination warehouse";
            d["Tay trống"] = "Empty hands";
            d["Chọn"] = "Selected";
            d["Đóng menu mod để tiếp tục."] = "Close the mod menu to continue.";
            d["Gameplay đang tạm dừng • mở menu để kiểm tra lưu game."] =
                "Gameplay is paused • open the menu to check the save.";
            d["Thu tiền tại cọc tiền"] = "Collect the cash pile";
            d["Dừng 0,25 giây gần vật thể để bắt đầu"] = "Stand still near an object for 0.25 seconds to begin";
            d["Cần trái: di chuyển    RB: chọn hàng    Start: lưu    Back: menu"] =
                "Left stick: move    RB: select item    Start: save    Back: menu";
            d["WASD: di chuyển    Shift: chạy (tay trống)    Dừng 0,25 giây: thao tác    Q: chọn hàng    F5: lưu    Esc: menu"] =
                "WASD: move    Shift: run (empty hands)    Stand still 0.25s: interact    Q: select item    F5: save    Esc: menu";
            d["Chưa thu"] = "Uncollected";
            d["Doanh thu"] = "Revenue";
            d["Đã thu"] = "Collected";
            d["Thất thoát"] = "Lost";
            d["KHO"] = "STOCK";
            d["Mở Kho để xem toàn bộ hàng"] = "Open Stock to see all items";
            d["giữ"] = "reserved";
            d["đến"] = "incoming";
            d["Sức chứa"] = "Capacity";
            d["Chuẩn bị"] = "Preparing";
            d["xu"] = "coins";
            d["món"] = "items";
            d["cấp"] = "level";
            d["Mod tiền"] = "Add cash";
            d["Mod xây dựng tối đa"] = "Maximize buildings";
            d["Đã nâng tối đa và lưu trò chơi."] = "All upgrades applied and game saved.";
            d["Đã cộng 999.999 xu vào két và lưu."] = "Added 999,999 coins to the safe and saved.";
            d["Chưa áp dụng được mod."] = "Could not apply the mod.";
        }
    }
}
