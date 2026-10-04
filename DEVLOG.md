# DEVLOG

## 2026-10-02 — Khởi tạo

- Xác minh đúng D:\GAME\TYCOON2: ASSET trống, không có Git/remote.
- Unity 6000.6.3f1 và module Windows hiện hữu; template URP 17.6.0.
- Blender 5.2.0 LTS và RTX 4060 Laptop 8188 MiB hiện hữu.
- Lần kiểm tra trước: ChatGPT.exe PID 19632 sử dụng GPU; không dừng tiến trình ngoài tác vụ.
- Chưa có mốc gameplay đạt QA; chuẩn bị project và pipeline nguồn CC0.

## 2026-10-03 — video, asset và vertical slice

- Video 51,833 giây đã được trích frame/đối chiếu; chọn GLB người dùng, giữ nguyên nguồn và SHA-256, giảm topology/giữ UV/texture, thêm rig/animation. Sửa transform reparent làm actor bị co.
- Camera chéo, palette xanh/cam/vàng/blue, herd bò/gà, stack cao, auto proximity và UI gọn.
- NavMesh customer pooling/checkout, tiền tại quầy, Farmer/Restocker/Cashier/Processor, recipe machine và save atomic có hàng transit.
- Foundation PASS; tests 9/9; vertical PASS với 77 giao dịch thật, 1.416 xu doanh thu, 4 worker, Flour và machine upgrade. Restart/load PASS; runtime log sạch.
- Chỉ sau gate vertical mới thêm Supermarket/Bakery/Restaurant, recipe reserves, Cook/Waiter và lưu trạng thái bàn. Full QA/performance chưa xác nhận. Không có Git/remote nên không commit/push.

## 2026-10-03 — triển khai thiết kế lại v2
Đã chốt đủ gameplay và được yêu cầu triển khai. Bắt đầu tách đơn hàng/receipt/thất thoát, inventory một SKU và reservation, góp tiền, đội theo khu, vận hành có người và savev2. Nguồn chuẩn tại Documentation/REPLAN.md. Source đang tích hợp; chưa coi compile hoặc gameplay đạt. Giữ nguyên nguồn asset và save prototype; không tạo backup. Không có Git remote.
