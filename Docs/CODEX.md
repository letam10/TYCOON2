# TYCOON2 — đồ họa, model, tương tác và UI

Cập nhật: 06/10/2026, Asia/Bangkok. Tiếp tục trực tiếp dự án này; mục tiêu `/goal` đang active.

## Phạm vi lượt phát triển

Nâng cấp cách trình bày của toàn bộ town hiện có: Farm/Livestock, Farm Shop, Processing, Supermarket, Bakery và Restaurant. Giữ nền tảng ownership/transaction, progression, giá/gate, Save v2 và tài nguyên có sẵn. Không tạo project mới.

## Thay đổi đã triển khai

- Model procedural có hình dáng riêng cho ngô/đậu, cừu, hàng đóng chai, feed/len/sợi/vải/bột và năm món ăn; bảy loại máy có chi tiết riêng. Tái dùng mesh ít đỉnh và palette cream/wood/sage.
- Cảnh quan, sàn từng khu, bảng tên, hoa, cây, bàn ăn, kho và quầy thanh toán thống nhất. Đèn máy và món trên bàn đọc state/inventory thật.
- Ánh sáng ấm, shadow rõ hơn, nền dịu; grade dùng Volume profile runtime riêng. Không sửa QualitySettings có sẵn của người dùng.
- Player tăng tốc ngắn/dừng nhanh; WASD, phím mũi tên và gamepad; camera 55 độ, nhìn trước theo chuyển động, zoom chuột/cần phải giới hạn 12–27 m.
- Focus ring và progress/state theo thao tác; đất ẩm, bốn pha canh tác, dấu sẵn thu hoạch; âm thanh và hiệu ứng nhặt/đặt/thu tiền phân biệt.
- HUD gọn, safe area và reflow theo kích thước; ví, thực thu, doanh thu, thất thoát, sức mang, mục tiêu và hàng tại khu. Menu pause, quản lý đội, kho/SKU và tuyến xe có scroll/focus tay cầm.
- Sửa restore mở input dưới menu pause: chỉ cho phép điều khiển khi HUD ở màn chơi. Đã có ca QA tái hiện fail trước sửa.
- QA mới kiểm tra material, bounds nút, mở/đóng các bảng ở 720p/1080p/1440p, pause/load/resume; báo cáo frame theo độ phân giải và bộ nhớ.
- Harness traffic bật lại director khi restore actor; director bị tắt bởi fixture phải được chạy trước khi kiểm tra input/UI.

## Môi trường và kiểm chứng

Unity 6000.6.3f1, URP 17.6.0, Input System 1.19.0, Windows/Direct3D11.

- Baseline đầu lượt: EditMode 135/135 pass.
- Sau thay đổi: EditMode 138/138 pass, 0 fail/skip.
- Build Windows: Succeeded, 0 errors, 4 warnings, 117.877.355 bytes; báo cáo cuối tại `work/overnight-polish/Build/build-report.json`.
- Probe và các lượt Player xác nhận NVIDIA GeForce RTX 4060 Laptop GPU; có guard `--require-4060`, không fallback iGPU.
- Layout/UI ban đầu pass ba độ phân giải. Ca restore-pause tái hiện fail ở `work/overnight-polish/restore-repro/town-layout-report.json`.
- Lượt Player kết hợp trước sửa harness: các ca gameplay đi tới restaurant/logistics, 1.097 m, 1.020 thao tác, 3.077 material slots hợp lệ, 0 runtime errors; dừng ở UI vì fixture tắt director trước load.
- Lượt cuối: PASS 106/106 checks, 0 runtime errors, 3.107 material slots, 361 điểm reachable; 1.097,5 m và 1.008 thao tác. Output: `work/overnight-polish/player-verified/`.

## Bản chạy và bằng chứng

- Bản Windows để kiểm tra: `work/overnight-polish/Build/TYCOON2.exe`.
- Ảnh trước nâng cấp: `work/overnight-polish/before/`.
- Ảnh/báo cáo cuối: `work/overnight-polish/player-verified/`.
- `presentation-audit.json`: median/p95/max của frame unscaled, bỏ 3 giây khởi tạo; frame limiter, resize, save/load và thao tác QA đều có thể tạo spike. Không phải GPU time hay benchmark/soak dài hạn.
- Thử bằng CLI: `Tools/run_player.ps1 -Task Town -BuildPath <đường dẫn bản Windows>`.
- Preview dùng save QA riêng: chạy bản Windows với `--qa-preview --require-4060 --qa-output <thư mục QA mới> -force-d3d11 -force-device-index 0`.

## Git, dọn dẹp và tiếp tục

- Repository `letam10/TYCOON2`, branch `main`. Đã push `d168c40` (tương tác/camera/feedback) và `eb577d1` (model/cảnh quan/ánh sáng). Mốc UI/QA đang được chốt trước đợt sửa theo phản hồi mới.
- Giữ ngoài commit: QualitySettings có sẵn, raw ASSET/Downloaded, build, save/journal/log QA. Docs/CODEX.md đầu lượt là file trống; tài liệu lịch sử Docs/CODEX_HANDOFF.md được giữ.
- Trước khi bàn giao phải audit PID/parent/command line của các Unity/Player/helper do lượt này tạo, xác nhận hết rồi dọn file QA/diagnostic không cần thiết. Giữ build, report và ảnh được chỉ định làm sản phẩm/bằng chứng.
- Mục tiêu đang tiếp tục; chưa coi lượt fixture là chứng minh campaign ví 0 → Restaurant trong 180–240 phút hoặc nghiệm thu nghệ thuật của người dùng.

## Phản hồi tiếp theo — 06/10/2026

Người dùng yêu cầu UI chiếm ít màn hình hơn, tiếp tục nâng đồ họa/model và dùng icon vẽ theo vật/nghề trên ô nâng cấp thay vì đặt asset/model 3D. Tiêu chí lượt tiếp: HUD mặc định nhỏ, thông tin chi tiết mở khi cần; icon 2D có ánh xạ cho toàn bộ upgrade; giữ gameplay/giá/gate/save và kiểm tra lại Player trên RTX 4060.

Preview cũ có save QA riêng, PID 6188 và crash helper 1508. Computer Use đã xem được cửa sổ đúng nhưng guard phát hiện input người dùng nên phím/nút native chưa được nghiệm thu. Lượt tiếp sẽ đóng preview cũ để build/kiểm tra bản mới và audit lại tiến trình.
