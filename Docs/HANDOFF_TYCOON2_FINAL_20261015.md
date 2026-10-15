# TYCOON2 final handoff — 2026-10-15

## Trạng thái bàn giao

- Workspace: `D:\GAME\TYCOON2`
- Branch: `main`
- Commit trước khi chốt: `1ddaeb2121014c867a0609d6b2f530b3fe296ce5`
- Remote: `https://github.com/letam10/TYCOON2.git`
- Build bàn giao: `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe`
- Build size đã xác nhận: `667136` bytes
- Không chạy benchmark, soak test hoặc test thời lượng dài.

## Thay đổi trong lượt chốt

1. `Assets/_Game/Scripts/Runtime/CityTrafficSignalVisual.cs`
   - Đèn giao thông dùng URP Lit và fallback Standard khi shader bị strip.
   - Giữ nguyên màu đỏ/vàng/xanh, emission, light chớp và trạng thái dừng xe/băng đường.
   - Foundation smoke đã xác nhận không còn material shader lỗi ở `SignalLamp_*`.
2. `Assets/_Game/Scripts/Runtime/QaCampaign.cs`
   - Manual selling dùng cà rốt starter, khách QA có receipt duy nhất và order 1 củ xác định.
   - Một đơn dở được giữ lại để mẻ thu hoạch kế tiếp phục vụ tiếp; tiền được collect, gửi safe rồi withdraw.
   - Helper mua upgrade giữ E đến khi hoàn tất hoặc hết 8 giây, mô phỏng thao tác người chơi.
3. `Tools/run_player.ps1`
   - Dọn đúng bộ QA slot hiện tại: `qa-save-v2`, journal/previous và các save slot.
   - Kiểm tra đường dẫn nằm trong output directory và không xóa save ngoài phạm vi.
4. `Assets/_Game/Scripts/Runtime/CityAccessPaths.cs` và test smoothing đã có trong commit nền `1ddaeb2`.
   - Tuyến corridor cong có endpoint lock, finite guard và fallback.

## Kiểm chứng mới

### Focused edit mode

Lệnh:\n+
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\run_unity.ps1 -Task Tests -TestFilter 'Tycoon.CityAccessPathsSmoothTests'
```

Kết quả XML mới tại `QA\editmode-results.xml`: `tests=4 passed=4 failed=0 skipped=0`.

### Foundation runtime smoke

Lệnh:\n+
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\run_player.ps1 -Task Foundation -BuildPath 'D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe'
```

Kết quả mới tại `QA\Evidence\foundation\foundation-report.json`:

- `passed=true`
- `runtimeErrors=0`
- Player exit code `0`
- Đã kiểm: sprint, carry animation, carry stack, drop/pickup, conservation, không sinh tiền từ timer, F5 save, warehouse stock, atomic save, rig bounds.

### Vertical campaign

Lệnh đã chạy:\n+
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\run_player.ps1 -Task Vertical -BuildPath 'D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe'
```

Manual selling đã PASS trong report: thu hoạch, checkout, collect cash, deposit safe và withdraw đủ tiền. Full vertical dừng ở `purchased farmer` vì progression hiện hành yêu cầu worker có trạm cấp 3 và 30 lượt việc; save mới chưa thỏa gate này. Đây là giới hạn kiểm chứng campaign, không phải lỗi shader hay lỗi manual selling. Không hạ gate để làm xanh QA.

## Build và cách chạy

Build source đã chạy bằng:\n+
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\run_unity.ps1 -Task Build -BuildPath 'work\tycoon2-final-20261015\Release\TYCOON2.exe'
```

Artifact đã copy sang:\n+
```text
D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe
D:\GAME\TYCOON2\Builds\Windows\TYCOON2_Data\
```

Chạy thử: mở `TYCOON2.exe` trong thư mục `Builds\Windows`. Foundation smoke đã chạy đúng binary này.

## Cleanup và phạm vi file

- Đã khôi phục 88 `.fbx.meta` bị Unity import thay đổi ngoài phạm vi.
- Đã khôi phục `PLAN.md`.
- Không xóa các asset tải xuống trong `ASSET\Downloaded\`; đây là dữ liệu art đã có trong workspace.
- Không stage evidence ảnh/report sinh trong QA nếu không cần cho source commit.
- Các file `qa-save-v2*` và slot QA được runner dọn theo whitelist trong output directory.

## Việc còn cần người nhận kiểm tra thủ công

- Mở build và duyệt visual toàn bản đồ, các model, text label, biển, đường cong, rừng và cutaway mái nhà.
- Chơi qua progression worker để tạo đủ station level và 30 player jobs trước khi chạy full Vertical campaign.
- Xác nhận cảm quan traffic signal, pedestrian crossing và tương tác khu vực trên máy người nhận.

## Quy trình tiếp nhận

1. Pull commit mới nhất từ `origin/main`.
2. Chạy `TYCOON2.exe` trong `Builds\Windows`.
3. Nếu cần chạy QA nhanh, dùng lệnh Foundation ở trên; không dùng Vertical như benchmark.
4. Đọc report tại `QA\Evidence\foundation\foundation-report.json` và file này cùng `QA\FINAL_HANDOFF_REPORT_20261015.md`.
