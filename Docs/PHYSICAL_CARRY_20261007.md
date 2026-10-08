# Tiền mặt, cầm hàng và UI — 08/10/2026

## Thao tác mới

- Tiền bán hàng nằm ở quầy; đứng tại vùng thu để cầm lên tay.
- Tay giữ một loại hàng hoặc tiền. Cất/giao hết hàng trước khi lấy tiền.
- Két miễn phí ở khu khởi đầu: vùng GỬI bên trái, RÚT bên phải.
- Mỗi lần vào vùng két chuyển toàn bộ số tiền; đứng yên không lặp giao dịch.
- Mọi chi phí lấy từ tiền trên tay. Tiền trong két cần rút trước khi sử dụng.
- Mod game mở hai dòng: Mod tiền và Mod xây dựng tối đa.
- Mod tiền thêm 999.999 vào két. Mod tối đa lưu vào tiến trình hiện tại.
- Ô nâng cấp bắt đầu góp sau 0,25 giây, tăng dần đến 5 lần tốc độ sau 2,5 giây.
- Rời ô giữ nguyên số đã góp. Cấp tiếp theo chờ hiệu ứng 0,6 giây.
- Ô ở cấp cuối ẩn hình ảnh và ngừng tương tác.
- Tại bến xe: mang hàng đến đóng kiện, đứng vùng LẤY KIỆN rồi vùng GIAO KIỆN.

## Sức mang

| Đối tượng | Các mức |
|---|---|
| Người chơi | 12 / 20 / 32 / 48 |
| Nhân viên thường | 12 / 20 / 32 |
| Bốc xếp | 12 / 24 / 36 |
| Tiền | Số nguyên 64 bit, không giới hạn theo sức mang |

Tiền dùng số bó model hữu hạn. Kiện tính số vật phẩm bên trong vào sức mang.

## Cấu trúc triển khai

- `PhysicalCashRules`: điều kiện tay, cộng/trừ tiền và kiểm tra tràn số.
- `PhysicalCashTransactions`: gửi/rút két và mod tối đa.
- `PhysicalCashMigration`: chuyển ví cũ vào két một lần sau phục hồi journal.
- `CarryLimits`: suy ra sức mang từ cấp đã sở hữu, không nhân đôi khi load.
- `SafeStation`: két và hai vùng thao tác độc lập.
- `CarryPresentation`, `HandItem*`, `InventoryStack`: model và chồng hàng.
- `PurchasePad*`, `UpgradeModel*`: góp tiền, phản hồi và thay đổi theo cấp.
- `ItemIconAtlas*`, `OrderBubble*`: icon vẽ 2D, tiến độ giao và viền kiên nhẫn.
- `GameHud*`, `HudNoticeBuffer`: menu mod, HUD tiền và thông báo gọn.
- `TruckLogisticsPhysical`, `TruckCargoPresentation`: cầm kiện và vùng bến xe.

## Save và giao dịch

- Save/transaction mới dùng phiên bản 3; giữ nguyên ID cũ.
- `cashInHand`, `cashInSafe`, doanh thu và tổng tiền thu dùng `long`.
- Save cũ được phục hồi journal theo schema cũ trước khi chuyển số dư vào két.
- Receipt/effect ID cũ được giữ lại để nhận ra lệnh đã thực hiện.
- Animation chạy sau giao dịch thành công và không cập nhật tiền/hàng.
- Save của người chơi và các thay đổi có sẵn trong worktree được giữ nguyên.

## Kiểm chứng

- EditMode: 243/243 đạt, gồm dữ liệu đội, kho rỗng và nguyên liệu đã giữ chỗ.
- Build Windows thành công tại `work/physical-carry-20261007/Release`.
- Player dùng RTX 4060 Laptop và Direct3D 11, đo hiệu năng ở 1920×1080.
- Lượt Player cuối đạt 220 kiểm tra, không có lỗi runtime, gồm điều kiện FPS.
- Migration Player đạt 117 kiểm tra: journal cũ, giữ hàng, tiền quầy, góp dở và load lại.
- Player mở trong tiến trình mới đọc checkpoint tối đa thành công, không có lỗi runtime.
- Đã chụp 27 loại hàng ở số lượng 1, 24 và 48; tiền 5 tỷ và kiện cầm trên tay.
- UI 720p, 1080p, 1440p kiểm tra bằng cửa sổ native đúng kích thước.
- UI 4K kiểm tra bằng RenderTexture 3840×2160 có HUD và bong bóng thật.
- Windows giới hạn kích thước cửa sổ 4K; chưa xác nhận cửa sổ native 4K.
- Ảnh gốc được giữ nguyên; bảng ảnh tổng hợp chỉ phục vụ xem nhanh.
- Video nâng cấp chứa 24 khung hình Player, 1920×1080, 10 FPS.
- Bộ bằng chứng có 102 ảnh gốc cùng ảnh trước thay đổi và bảng ảnh tổng hợp.

## Hiệu năng

- Cảnh đo có 78 nhân viên hoạt động, 30 khách và người chơi cầm 48 món.
- Số liệu ban đầu: 25,67 FPS, CPU chính 38,79 ms và GPU 1,72 ms.
- Đo riêng giao dịch cho thấy chúng không phải chi phí chính.
- Mỗi nhân viên từng copy JSON cả danh sách 26 đội ở mỗi frame.
- `FindCrew` hiện chỉ copy trực tiếp sáu trường của đội cần dùng.
- Kho có tồn khả dụng bằng 0 trả về ngay, tránh quét toàn bộ nhu cầu sản xuất.
- Animator dùng `CullUpdateTransforms`; trạng thái tiếp tục chạy khi model ngoài khung nhìn.
- Hai probe tắt mặc định; chỉ bật trong cửa sổ đo QA, không ghi file theo từng frame.
- Mục tiêu nghiệm thu là FPS trung bình ít nhất 60 trên cửa sổ đo 10 giây.
- Báo cáo giữ P95 và frame chậm nhất để thể hiện mức giật còn lại.
- Bộ đếm draw call/GC không có dữ liệu trong release được ghi rõ là không khả dụng.
- Lượt đầy đủ: 87,95 FPS, CPU chính 11,36 ms, GPU 1,71 ms.
- P95 là 21,68 ms; frame chậm nhất 28,80 ms. Chưa đo soak dài hoặc mọi frame đạt 60 FPS.
- Lượt đo nhanh riêng đạt 87,14 FPS; chọn tuyến hàng giảm từ 1.329 ms xuống 230 ms/10 giây.
- Số liệu trên thuộc fixture đông khách chạy thật trong Player, không phải số liệu Editor.

## Đường dẫn bằng chứng cuối

- Chức năng, model, UI và hiệu năng: `work/physical-carry-20261007/player-20261007T184304484Z`.
- Migration: `work/physical-carry-20261007/migration-20261007T184623211Z`.
- Đo nhanh: `work/physical-carry-20261007/player-physicalperformance-20261007T184213561Z`.
- Mở lại Player: `work/physical-carry-20261007/player-physicalload-20261007T184708246Z`.
- EditMode: `QA/editmode-results.xml`, 243 đạt và 0 lỗi.
- Build: `work/physical-carry-20261007/Release/build-report.json`.

## Chạy lại kiểm tra

Các lệnh dưới đây chạy từ thư mục dự án bằng PowerShell.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_unity.ps1 -Task Tests
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_player.ps1 `
    -Task Physical -Visible `
    -BuildPath work/physical-carry-20261007/Release/TYCOON2.exe
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_player.ps1 `
    -Task PhysicalMigration -Visible `
    -BuildPath work/physical-carry-20261007/Release/TYCOON2.exe
```

`PhysicalPerformance` nhận thêm `-LoadPath` trỏ tới checkpoint tối đa của lượt `Physical`.
Mỗi lượt QA có thư mục và save riêng. Ảnh từng model sử dụng fixture có ghi rõ trong mã QA.
Vòng chơi đầu, thao tác mod, thu/gửi/rút tiền và phục vụ bàn đi qua actor cùng input của Player.

## Bản giao

- Giải nén toàn bộ `TYCOON2-Windows.zip` rồi chạy `TYCOON2.exe`.
- `TYCOON2-Source.zip` chứa Assets, Packages, ProjectSettings, Tools và Docs.
- Mở source bằng Unity 6000.6.3f1, scene `Assets/_Game/Scenes/Tycoon.unity`.
- `TYCOON2-Evidence.zip` chứa ảnh gốc, video, báo cáo chức năng, migration và hiệu năng.
- `SHA256SUMS.txt` và `delivery-manifest.json` dùng đối chiếu bản giao.
- Bản chơi bình thường dùng save trong `Application.persistentDataPath`.
- Source nằm riêng với thư mục Release; các thay đổi có sẵn trong worktree được giữ lại.
