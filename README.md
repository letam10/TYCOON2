# TYCOON2

Trò chơi quản lý thành phố, trang trại, sản xuất, cửa hàng và nhà hàng.
Unity 6000.6.3f1, URP 17.6.0; Windows, tiếng Việt/English, chơi đơn offline.

## Chơi bản Windows

Giải nén toàn bộ gói Windows rồi chạy `TYCOON2.exe`.
Giữ thư mục `TYCOON2_Data`, các DLL và thư mục kèm theo bên cạnh chương trình.
Hướng dẫn điều khiển hiện hành nằm ngay bên dưới.
Bản bàn giao V21 và bằng chứng hiện hành: [CODEX_HANDOFF.md](Docs/CODEX_HANDOFF.md).

| Thao tác | Điều khiển |
| --- | --- |
| Di chuyển | WASD, phím mũi tên hoặc cần trái gamepad |
| Chạy 10 m/s khi tay trống | Giữ Shift trái/phải hoặc nhấn cần trái |
| Tự thao tác | Dừng 0,25 giây gần vật thể |
| Chọn hàng để lấy từ kho | Q hoặc RB |
| Lưu | F5 hoặc Start |
| Menu | Esc hoặc Back |
| Zoom | Cuộn chuột hoặc cần phải |
| Bản đồ lớn / dịch chuyển | M, bấm minimap hoặc Cài đặt → Bản đồ |
| Đổi phím | Cài đặt → Tùy chỉnh phím |
| 3 slot lưu / chơi mới | Menu hoặc Cài đặt → Slot lưu • Chơi mới |

Minimap nằm dưới bên trái, theo vị trí và hướng nhân vật. Bản đồ lớn tạm dừng game;
bấm điểm vàng hoặc tên điểm để dịch chuyển. Game mới xuất hiện trước quầy nông trại TYCOON.
Slot 1 giữ save cũ; slot 2/3 độc lập. Lưu nhanh dùng slot đang chơi. Ghi đè cần xác nhận
lần nữa và giữ checkpoint trước trong file `.previous.json` của slot đó.
Đổi phím được tự lưu; Esc hủy lúc chọn phím, nút Khôi phục mặc định bỏ các phím tùy chỉnh.

Tay chỉ giữ một loại hàng hoặc tiền. Giao/cất hết hàng trước khi lấy tiền;
gửi tiền vào két hoặc tiêu hết trước khi lấy hàng mới.
Két miễn phí nằm trong khu khởi đầu, có hai vùng gửi và rút riêng.
Mọi khoản chi dùng tiền đang cầm; tiền trong két phải rút trước.

`Mod game` mở hai lựa chọn: cộng 999.999 xu vào két hoặc xây/nâng toàn bộ tới cấp tối đa.
Mod tối đa được lưu vào tiến trình. Doanh thu vẫn chỉ ghi từ hoạt động bán hàng thật.
Hàng cầm xếp 2 cột × 3 hàng mỗi tầng; cây trồng dài nằm ngang trên bàn và kho.
Chỉ trứng, sữa và chai/lọ dễ vỡ dùng vỉ, khay hoặc két khi cầm; một món hiện đúng một món trong khay.
Thành phố tăng gấp đôi chiều rộng và chiều dài; V11 có13khu cảnh quan và nước ripple/bọt nhẹ.
Đội ngũ tối đa 39 người thuộc 26 nghề, năng lực mỗi người tăng 1,5 lần.
NPC theo vỉa hè/đường chính, lên xuống xe buýt, xe khách, taxi và các phương tiện đường phố.
Hai đường ray và nhà ga có hai sân; dùng cầu và dốc đi bộ để qua phía bên kia.
Bàn tròn có hai ghế, bàn vuông có ghế dài, khu quầy có ghế ngồi sát mép bàn.
Nút **Cài đặt** đổi đồ họa, độ phân giải, ngôn ngữ, đồng bộ màn hình và bật/tắt thanh FPS.
Bờ biển có mép cát cong nhẹ, vùng cát ướt, nước xanh với caustic, bọt ven bờ và sóng nhỏ.
Kiểm chứng hiện hành và giới hạn nằm trong `Docs/CODEX_HANDOFF.md`; các báo cáo V12 là lịch sử.

## Mở source

Cài đúng Unity qua Unity Hub, mở thư mục dự án, mở `Assets/_Game/Scenes/Tycoon.unity` và Play.
Repo dùng Git LFS cho asset; chạy `git lfs pull` sau khi clone.
Save cũ được phục hồi journal rồi chuyển tiền ví sang két và vị trí sang bản đồ mới đúng một lần.

## Kiểm tra tại máy phát triển

```powershell
.\Tools\run_unity.ps1 -Task Tests
.\Tools\run_unity.ps1 -Task Build
.\Tools\run_world_features.ps1
# Dùng đúng thư mục output vừa được lệnh trên trả về để kiểm mở lại game:
.\Tools\run_world_features.ps1 -Load -OutputPath 'work/world-map-20261014/player-<timestamp>'
.\Tools\run_completion.ps1 -Case All -BuildPath Builds/Windows/TYCOON2.exe
```

Các lệnh Player dùng input/UI thật và save QA riêng. Đây là kiểm chức năng ngắn;
không dùng fixture để khẳng định cân bằng campaign hoặc hiệu năng benchmark.
Lượt map/slot/rebind có ảnh và report dưới `work/world-map-20261014/player-*`.
