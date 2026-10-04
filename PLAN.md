# TYCOON2 — kế hoạch và cổng kiểm chứng

> Người dùng đã yêu cầu thiết kế lại sau khi trực tiếp quan sát game. Trạng thái PASS ở các mốc bên dưới là bằng chứng kỹ thuật lịch sử, không phải nghiệm thu sản phẩm. Xem `Documentation/REPLAN.md`; quyết định đầu tiên về cách khách mua tại Farm Shop đang chờ trả lời. Chưa triển khai thêm cơ chế trước khi chốt thiết kế.

## Quyết định đã chốt

- Windows offline, tiếng Việt, Unity 6000.6.3f1 / URP 17.6.0.
- Năm khu: Farm, Farm Shop, Processing, Supermarket, Bakery + Restaurant.
- Asset miễn phí có giấy phép rõ; nguồn nguyên vẹn trong ASSET.
- Blender Python ưu tiên; Three.js khi đơn giản hơn; GPT Image cho bitmap cần thiết.
- Không điều khiển desktop, không tạo backup, không tự tạo GitHub repository.
- QA dùng input/gameplay thật, save riêng; không cộng tiền hoặc ép unlock.

| Mốc | Nội dung | Cổng kiểm chứng | Trạng thái |
|---|---|---|---|
| M0 | Project, actor, camera, art mẫu | compile, rig/pose, render, movement/carry | PASS nền tảng và render theo video |
| M1 | Farm + Shop | hàng vật lý, customer, checkout, worker | PASS vertical |
| M2 | Processing + vertical slice | tests, save/restart, Windows gameplay | PASS Flour/upgrade/save/restart; kiểm tra full recipe tiếp theo |
| M3 | Supermarket | nhiều shelf/lane, restock, 30 khách | Đã triển khai, chờ full QA |
| M4 | Bakery + Restaurant | recipe, bàn/gọi món/phục vụ/thu tiền | Đã triển khai, chờ full QA |
| M5 | Hoàn thiện | full progression, render QA, performance, cleanup | Đang kiểm chứng |

QA lưu bằng chứng bàn giao; work chỉ chứa file thử/cache của tác vụ. Mỗi mốc cần bằng chứng runtime, không dựa riêng vào compile.
