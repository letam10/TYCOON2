# TYCOON2 — thiết kế lại đã được duyệt

Ngày 2026-10-03. Người dùng đã yêu cầu triển khai; không còn chờ câu hỏi A/B. Chuẩn mới: phục vụ tại quầy, 0 tiền, ba ô cà rốt, mang một SKU, thao tác khi đứng đúng vùng; năm khu trong 180–240 phút. Gói thịt 500 và sữa/trứng 1000 gồm tuyến cơ bản sử dụng được. Cà rốt 10 xu. Mọi thay đổi giá sau này phải có số liệu cân bằng.

## Quyết định bắt buộc
- Khách FIFO mỗi quầy; kiên nhẫn90 giây từ lúc vào hàng; đầu game1–3 đơn vị. Khách bỏ đi mang phần đã nhận, trả0; ghi thất thoát một lần. Giao đủ mới tạo khoản tiền, người chơi thu tại quầy mới vào ví.
- Trồng: gieo/tưới/lớn/thu. Chăn nuôi có thức ăn/chăm; sinh sản phẩm2x khi một người chăm; thịt giảm đàn, tái đàn cần cây và thời gian. Máy tiến triển khi có đúng một operator, ra khỏi trạm dừng.
- Kho theo khu, tuyến cố định, giữ chỗ đầu nhận. Cấm tự hoàn hàng khách vào kho khi load.
- Nhân viên theo nghề+khu; trạm cấp3 và30 lượt việc mới hiện ô thuê. Lần đầu góp tiền tại ô, về sau menu nâng tốc độ/sức mang/số người. Không lương, không đổi nghề; thiếu/đầy chờ bên trạm.
- Mua bằng góp dần; khoản góp tồn tại qua save/load; mở khóa đúng một lần.
- Restaurant: giữ bàn/gọi món/nấu/giao/ăn/trả tiền/bàn bẩn/dọn/bàn trống. Người chơi làm được trước nhân viên.
- Rush90s báo trước15s, tối đa30 khách; máy hỏng tối đa1 máy, sửa8s phí10–100. Tuyến cà rốt miễn phí luôn phục hồi kinh tế được.
- Savev2 giữ đầy đủ đơn, khách, hàng đang mang, người vận hành, tiến độ, bàn, queue, khoản góp, tiền chưa thu và sự kiện. Savev1 giữ nguyên. Không thu nhập khi đóng game.

## Cửa nghiệm thu
1. Logic inventory/reservation/order/payment/expiration/save có tests thực sự.
2. Vòng chơi thật từ0: trồng, phục vụ, thu tiền, góp dở rồi save/restart; không cấp hàng/tiền/mở khóa trongdriver.
3. Farm+Shop+Processing đạt trước khi nghiệm thu các khu cuối.
4. Hai nhánh tiến triển thực tế đến năm khu trong3–4h; thuê/nâng đội; vận hànhRestaurant; đốisoát save/restart.
5. 1080p RTX4060 Laptop mục tiêu165FPS với30khách và hệ thống hoạt động; cần graphics thật, công bố frame-time, CPU/GPU, sốactor. Báo cáo120FPS trước đây chưa chứng minh mục tiêu mới.
6. Ảnh thực tế camera55°, chồng hàng, chữ dễ đọc, luồng người không kẹt; pose/animation không kéo mesh; đốichiếu video.

## Trạng thái triển khai
Đang sửa source theo thiết kế mới. Chưa có kết luận compile/tests/build/runtime cho vòng sửa này. QA cũ không được dùng thay bằng chứng mới. Chưa nghiệm thu cân bằng3–4h, hình ảnh hoặc165FPS. Dự án hiện không có Git repository/remote, không commit/push.
