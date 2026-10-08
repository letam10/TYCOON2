# TYCOON2

Trò chơi quản lý thành phố, trang trại, sản xuất, cửa hàng và nhà hàng.
Unity 6000.6.3f1, URP 17.6.0; Windows, tiếng Việt, chơi đơn offline.

## Chơi bản Windows

Giải nén toàn bộ gói Windows rồi chạy `TYCOON2.exe`.
Giữ thư mục `TYCOON2_Data`, `MonoBleedingEdge` và các DLL bên cạnh chương trình.
Hướng dẫn và bằng chứng mới nhất: [CITY_EXPANSION_20261008.md](Docs/CITY_EXPANSION_20261008.md).

| Thao tác | Điều khiển |
| --- | --- |
| Di chuyển | WASD, phím mũi tên hoặc cần trái gamepad |
| Chạy khi tay trống | Shift hoặc nhấn cần trái |
| Tự thao tác | Dừng 0,25 giây gần vật thể |
| Chọn hàng để lấy từ kho | Q hoặc RB |
| Lưu | F5 hoặc Start |
| Menu | Esc hoặc Back |
| Zoom | Cuộn chuột hoặc cần phải |

Tay chỉ giữ một loại hàng hoặc tiền. Giao/cất hết hàng trước khi lấy tiền;
gửi tiền vào két hoặc tiêu hết trước khi lấy hàng mới.
Két miễn phí nằm trong khu khởi đầu, có hai vùng gửi và rút riêng.
Mọi khoản chi dùng tiền đang cầm; tiền trong két phải rút trước.

`Mod game` mở hai lựa chọn: cộng 999.999 xu vào két hoặc xây/nâng toàn bộ tới cấp tối đa.
Mod tối đa được lưu vào tiến trình. Doanh thu vẫn chỉ ghi từ hoạt động bán hàng thật.
Hàng cầm xếp 3 cột × 2 hàng mỗi tầng; cây trồng dài nằm ngang trên bàn và kho.
Đội ngũ tối đa 39 người thuộc 26 nghề, năng lực mỗi người tăng 1,5 lần.
NPC theo vỉa hè/đường chính và có thể lên xuống xe buýt, xe khách, taxi ở bến.

## Mở source

Cài đúng Unity qua Unity Hub, mở thư mục dự án, mở `Assets/_Game/Scenes/Tycoon.unity` và Play.
Repo dùng Git LFS cho asset; chạy `git lfs pull` sau khi clone.
Save cũ được phục hồi journal rồi chuyển tiền ví sang két và vị trí sang bản đồ mới đúng một lần.

## Kiểm tra tại máy phát triển

```powershell
.\Tools\run_unity.ps1 -Task Tests
.\Tools\run_unity.ps1 -Task Build -BuildPath 'work/city-expansion-20261008/Release/TYCOON2.exe'
.\Tools\run_city_play.ps1 -Visible
```

Lệnh cuối chơi bằng gamepad ảo qua hệ thống input/UI thật, simulation 1x và save riêng.
Không sử dụng benchmark hoặc tạo inventory fixture để đo hiệu năng.
Kết quả, ảnh và timeline nằm trong `work/city-expansion-20261008/ordinary-play-*`.
