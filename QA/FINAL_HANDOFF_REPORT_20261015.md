# TYCOON2 QA final report — 2026-10-15

## Kết luận ngắn

Bản Windows đã build từ source hiện tại và Foundation runtime smoke PASS trên GPU NVIDIA GeForce RTX 4060 Laptop GPU. Focused corridor test PASS 4/4. Full Vertical campaign không được đánh dấu PASS vì dừng đúng progression gate `purchased farmer`; report giữ nguyên lỗi để bàn giao trung thực.

## Evidence

| Hạng mục | Lệnh | Kết quả | Evidence |
|---|---|---|---|
| Curved corridor | `run_unity.ps1 -Task Tests -TestFilter Tycoon.CityAccessPathsSmoothTests` | 4 passed, 0 failed, 0 skipped | `QA/editmode-results.xml` |
| Windows build | `run_unity.ps1 -Task Build -BuildPath work\\tycoon2-final-20261015\\Release\\TYCOON2.exe` | Unity exit 0 | `work\\tycoon2-final-20261015\\Release` |
| Foundation smoke | `run_player.ps1 -Task Foundation -BuildPath D:\\GAME\\TYCOON2\\Builds\\Windows\\TYCOON2.exe` | Player exit 0, `passed=true`, `runtimeErrors=0` | `QA/Evidence/foundation/foundation-report.json` |
| Vertical campaign | `run_player.ps1 -Task Vertical -BuildPath D:\\GAME\\TYCOON2\\Builds\\Windows\\TYCOON2.exe` | Manual selling PASS; full campaign stops at worker gate | `QA/Evidence/vertical/vertical-report.json` |

## Đã sửa trong lượt chốt

- Traffic lamp shader fallback: URP Lit trước, Standard sau; tránh `Hidden/InternalErrorShader` trong Windows build.
- Manual QA sales loop: receipt duy nhất, order xác định 1 cà rốt, xử lý order dở, thu/gửi/rút tiền.
- QA purchase helper: giữ E tới khi purchase hoàn tất hoặc deadline 8 giây.
- Player runner: whitelist chính xác tên QA save slot và journal/previous, có kiểm tra path.

## Giới hạn kiểm chứng

Foundation smoke không chứng minh toàn bộ city art, mọi interaction khu vực, full worker progression hoặc cảm quan trên mọi máy. Vertical campaign cần save có đủ station level và 30 player jobs theo luật progression hiện tại; không hạ luật để làm test xanh. Không benchmark, soak hoặc performance campaign được chạy theo yêu cầu.

## Artifact bàn giao

```text
D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe
D:\GAME\TYCOON2\Builds\Windows\TYCOON2_Data\
```

Build executable đã xác nhận tồn tại, kích thước 667136 bytes. Dùng `Docs/HANDOFF_TYCOON2_FINAL_20261015.md` làm tài liệu tiếp nhận chính.

## Git handoff

Source đang ở branch `main`, remote `https://github.com/letam10/TYCOON2.git`. Chỉ stage source QA/traffic/runner và hai tài liệu handoff/report; không stage asset tải xuống hoặc evidence sinh tự động ngoài phạm vi.
