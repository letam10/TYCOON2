# TYCOON2 — đồ họa, model, tương tác và UI

Cập nhật: 06/10/2026, Asia/Bangkok. Dự án D:\GAME\TYCOON2, GitHub letam10/TYCOON2, branch main. Mục tiêu /goal đang active để tiếp tục qua đêm.

## Bản chạy và cách kiểm tra

- Bản Windows hiện tại: D:\GAME\TYCOON2\work\cash-only-mod\Build\TYCOON2.exe; mốc UI trước tại work/compact-presentation/Build/.
- Ảnh cuối: work/compact-presentation/presentation-final/; gameplay tổng hợp: work/compact-presentation/player-full/.
- WASD/phím mũi tên/gamepad để di chuyển; dừng 0,25 giây gần vật thể để thao tác; Q chọn hàng; F5 lưu; Esc menu; cuộn chuột/cần phải zoom 12–27 m.
- Kho, xe tải và thông tin kinh doanh mở bằng nút nhỏ góc phải. Mục tiêu, doanh thu và tồn kho xem trong bảng Thông tin.

## Thay đổi đã triển khai

- HUD mặc định chỉ có ví nhỏ, giỏ, hàng nút và hướng dẫn tương tác. Các bảng lớn mở khi cần, hỗ trợ safe area/reflow. Thông tin/pause/đội dừng simulation và khóa điều khiển; kho/xe khóa điều khiển. Đóng bảng chỉ mở input khi game hợp lệ.
- Ô nâng cấp dùng sprite 2D tự vẽ đúng vật/nghề, không đặt model/asset 3D thu nhỏ. Huy hiệu phân biệt chất lượng/tốc độ/sức chứa/cấp, tự đổi cấp 2→3. Sprite/texture cũ được giải phóng. Giá vừa dải nền; tên/điều kiện/tiến độ mua hiện trong HUD khi chọn ô để không đè icon.
- Nhãn trạm gần người chơi tối đa hai dòng, giới hạn 192–210 px. HUD vẫn đọc Prompt đầy đủ. Bubble khách hàng không có TextMesh trực tiếp vẫn billboard được.
- Model riêng cho cây/hàng, cừu, feed/len/sợi/vải/bột, năm món và bảy máy; thêm chi tiết máy, hạt ngô dùng mesh gộp chia sẻ. Giữ model thư viện hiện có.
- Piston/vòi/con thoi/bàn đạp/mẻ trộn chuyển động theo Operating thật; hơi nóng chỉ khi máy hoạt động; nguyên liệu/đầu ra đọc inventory. Cây lớn nghiêng nhẹ, không ghi phase/timer/inventory hoặc scale cây.
- Cảnh quan/sàn từng khu, bảng tên, hoa/cây, kho/quầy/bàn ăn đồng bộ palette. Đèn/progress máy, món ăn, đất ẩm và dấu thu hoạch đọc state thật.
- Ánh sáng ấm/shadow, backdrop dịu, grade runtime riêng. Player tăng tốc ngắn/dừng nhanh; camera 55 độ/nhìn trước/zoom; focus/progress ring và âm thanh/hiệu ứng nhặt/đặt/thu tiền phân biệt.
- Giữ ownership/transaction, giá/gate/progression và Save v2. Không tạo project mới; QualitySettings và raw ASSET/Downloaded có sẵn được giữ ngoài commit.

## Kiểm chứng

Unity 6000.6.3f1, URP 17.6.0, Input System 1.19.0; Windows/Direct3D11. Probe riêng và guard --require-4060 xác nhận NVIDIA GeForce RTX 4060 Laptop GPU trước lượt render.

| Kiểm tra | Kết quả |
| --- | --- |
| EditMode | 153/153 pass, 0 fail/skip; QA/editmode-results.xml |
| Build cuối | Succeeded, 0 errors, 2 warnings, 117.909.115 bytes; work/compact-presentation/Build/build-report.json |
| Gameplay tổng hợp | PASS 313 checks, 0 runtime errors, 2.411 material slots, 1.122,9 m và 1.042 thao tác; player-full/town-redesign-report.json |
| Trình bày cuối sau sửa nhỏ | PASS 252 checks, 0 runtime errors; presentation-final/town-layout-report.json |
| HUD mặc định | Khoảng 4,82% tại 1280×720, 1920×1080, 2560×1440; hợp vùng Image lấy mẫu 160×90, không cộng trùng; presentation-final/hud-coverage.json |
| Nhãn gần luống | 192/210/210 px tại ba độ phân giải |

QA dùng keyboard/gamepad/chuột tổng hợp của Input System. Nút được click qua EventSystem, không gọi trực tiếp onClick. Đã kiểm tra pause/đội/thông tin/kho/xe ở ba độ phân giải, save/load dưới pause và resume; mọi ô nâng cấp có sprite vẽ và không còn model con. Đã xem ảnh thực của HUD, bảng đội và cận icon máy.

Gameplay có starter ví 0, giao hàng/thu tiền, xe tải giữ ownership qua repeated load, sửa máy, năm món nhà hàng và traffic ngắn 30 khách. Phần sau dùng fixture gần ngưỡng; chưa chứng minh campaign ví 0→Restaurant đầy đủ trong 180–240 phút hoặc phiên dài xuyên đêm. Bằng chứng gameplay 313 ca trước tinh chỉnh trình bày cuối được giữ riêng; lượt 252 ca bổ sung cho bản build cuối.

player-full/presentation-audit.json: managed 29 MiB, allocated 360 MiB; 1080p p95 khoảng 6,061 ms. Frame có limiter/resize/capture/lưu QA, không phải GPU time/benchmark 165 FPS/soak dài. Chưa coi đây là nghiệm thu nghệ thuật của người dùng hoặc xác nhận input vật lý native.

## Git và tiếp tục qua đêm

- Đã push đợt đầu: d168c40 (tương tác/camera), eb577d1 (town/model/ánh sáng), 57293d5 (UI/QA ban đầu).
- Đã push theo phản hồi: 24593be (HUD gọn + icon vẽ), 0a4fd32 (model + chuyển động máy/cây). Mốc QA/tài liệu 3c1924b đã push.
- Heartbeat tycoon2-qua-m đã ACTIVE, gắn cuộc chat này, tiếp tục mỗi hai giờ. Prompt yêu cầu kết thúc công việc qua đêm lúc 07:00 ngày 07/10/2026 giờ Bangkok và tắt lịch; chỉ thông báo khi có thay đổi có ý nghĩa/lỗi/cần người dùng.
- Ưu tiên nghiệm thu gameplay liên tục, máy/cây operating/idle/pause/restore và phiên dài; tiếp tục nâng theo bằng chứng. GPU vừa/nặng phải preflight RTX 4060, chờ theo AGENTS, không polling mỗi phút.

## Dọn dẹp

- Các Unity/Player/probe của đợt này đã thoát tự nhiên. Preview cũ PID 6188 và helper 1508 đã đóng. Audit hiện tại không có Player hoặc Unity cần giữ.
- Hai file .rsp tại work/interaction-icon-check-a17b09/ đã dọn. Bốn compiler output Tycoon.Runtime.dll/.pdb và Tycoon.Tests.dll/.pdb còn lại: duyệt tự động chặn xóa, lý do blocked by policy; không có tiến trình dùng. Kiểm tra/dọn lại khi chính sách cho phép.
- Ký hiệu Burst work/compact-presentation/Build/TYCOON2_BackUpThisFolder_ButDontShipItWithYourGame/x86_64/lib_burst_generated.pdb cũng bị chặn xóa. Đây là ký hiệu do Unity tự sinh, không phải sao lưu nguồn chủ động; không cần để chơi.
- Giữ build/report/ảnh chỉ định làm sản phẩm và bằng chứng. Không dọn hàng loạt work/cache có sẵn hoặc nguồn/asset người dùng.

## Mod game chỉ cộng tiền — 06/10/2026

- Theo yêu cầu mới, nút trên HUD là Mod game. Mỗi click hợp lệ cộng đúng 999.999 vào ví; không mở khu/cây, thay upgrade/progression, cấp đồ/worker, đổi máy/đặt chỗ hoặc cộng vào doanh thu/thực thu/thống kê hỗ trợ. Sổ giao dịch vẫn ghi receipt/revision để lưu và chống cộng trùng.
- Lệnh GrantModCash được thêm cuối enum để giữ mã giao dịch cũ. Chỉ player được gọi; vượt giới hạn int của ví bị từ chối toàn bộ. Lệnh riêng không chạy expiry/refresh pha máy/đồng hồ mô phỏng. GrantAssistance cũ chỉ được giữ cho QA/compatibility, không còn gắn vào nút HUD.
- Code đã push: 2073421 (feat: add cash-only mod game button).
- EditMode 158/158 pass: so toàn bộ state trừ tiền/sổ giao dịch, đặt chỗ quá hạn không bị mod đổi, replay/load chỉ cộng một lần, click mới cộng lần tiếp theo, giới hạn ví và quyền player.
- Build cuối: Succeeded, 0 errors, 2 warnings, 117.910.651 bytes. Bản dùng để kiểm tra hiện tại: D:\GAME\TYCOON2\work\cash-only-mod\Build\TYCOON2.exe.
- Player trên RTX 4060/D3D11 PASS 256 checks, 0 runtime errors. Click qua EventSystem khi đóng băng simulation xác nhận cộng đúng 999.999, toàn bộ state gameplay khác giữ nguyên và số dư HUD đúng. Ảnh đã xem: work/cash-only-mod/player-final/mod-game-cash-only.png; report: work/cash-only-mod/player-final/town-layout-report.json.
- Nút Mod game vẫn theo cấu hình TYCOON_DISABLE_ASSIST hiện có; bản đang bàn giao có nút. Không sửa QualitySettings có sẵn. Các Player/probe/build của lượt mod đã kết thúc, không cần giữ tiến trình.
- Tiếp tục qua đêm phải giữ ràng buộc mới: nút Mod game chỉ cộng tiền, không dùng lại lệnh mở khu. Dọn lại Burst PDB tự sinh ở build cash-only-mod khi chính sách cho phép; không dùng nó để chạy game.
