# TYCOON2 Docs index

Updated: 2026-10-14

Đây là chỉ mục đọc nhanh cho chat sau. Tài liệu hiện hành được ưu tiên theo thứ tự
dưới đây; tài liệu lịch sử không được dùng để ghi đè trạng thái mới nhất.

## Ưu tiên 1 — nguồn sự thật của dự án

1. `Docs/CODEX_HANDOFF.md` — mục V21: bản đồ, đổi phím, slot lưu, hồi quy nhanh và bàn giao Windows.
   `QA/current-delivery-20261014.json` ghi lệnh, kết quả, thời gian, hash của đợt bàn giao.
   `Docs/world-map-20261014/PAUSED_HANDOFF.md` chỉ ghi checkpoint dừng trước khi tiếp tục.

## Ưu tiên 2 — phạm vi sửa hiện hành

2. `Docs/CITY_REPAIR_CONTRACT_20261011.md` — contract sửa capture, route và NPC.

## Ưu tiên 3 — bằng chứng sửa hiện hành

3. `Docs/CITY_CAPTURE_REPAIR_20261011.md` và
   `Docs/CITY_NAV_NPC_REPAIR_REVIEW_20261011.md` — kết quả source review và giới hạn
   Player hiện hành. Đọc cả hai như một nhóm evidence.

## Sau ba file chính

- `Documentation/REPLAN.md` — luật gameplay và save v2 cấp dự án.
- `Docs/CITY_EXPANSION_20261008.md` — bố trí thành phố, khu, giao thông và landmark.
- `Docs/CARRY_CONTAINERS_V10.md`, `Docs/INDUSTRIAL_MODELS_V10.md`,
  `Docs/RENDER_OPTIMIZATION_V10.md` — yêu cầu vật phẩm, máy và render.
- `Docs/V11_PLAYER_GUIDE.md` đến `Docs/V14_PLAYER_GUIDE.md` — hướng dẫn và giới hạn
  của các gói kiểm; không xem chúng là bằng chứng Player hiện hành nếu V21 đã có số mới hơn.
- Các file V11–V14 được `Tools/package_city.py` liệt kê vẫn phải giữ nguyên đường dẫn,
  vì chúng là đầu vào của gói Evidence.

## Tài liệu được gộp vào lịch sử

`Docs/History/LEGACY_DOCS_20261011.md` chứa nguyên văn 34 contract/findings cũ của V10–V17
và hồ sơ benchmark 165 FPS đã hủy. Các bản rời tương ứng đã được loại khỏi thư mục `Docs`
gốc sau khi kiểm tra archive; nội dung không bị bỏ đi.

## Quy tắc giữ và xóa

- Không xóa `CODEX_HANDOFF.md`, contract sửa hiện hành, report Player, save, evidence hoặc
  tài liệu được `package_city.py` yêu cầu.
- Không dùng mục tiêu stable 165 FPS làm điều kiện nghiệm thu; chỉ giữ số liệu chẩn đoán.
- Khi tạo checkpoint mới, cập nhật handoff trước rồi cập nhật chỉ mục nếu thứ tự đọc thay đổi.
- File lịch sử mới phải được gộp vào archive hoặc chuyển vào `Docs/History`, không để thêm
  một nhóm V-number rời ở thư mục gốc nếu không có tham chiếu thực thi.
