# Rà soát frame khựng khi bật mod xây tối đa — 08/10/2026

## Cập nhật sau triển khai và lượt Player cuối

Phân tích bên dưới là lịch sử của lượt 03:45 UTC, trước khi giảm đội ngũ và chia dựng model.
Lượt cuối 07:19 UTC có 39 nhân viên, trung bình 162,36 FPS trong 266,87 giây.
Frame mod tối đa vẫn dài 748,94 ms. Marker transaction 6,56 ms và checkpoint 10,33 ms
không giải thích toàn bộ thời gian này; chưa có kết luận nguyên nhân chính.
Lượt phục hồi 07:27 UTC chạy 344,42 giây, trung bình 160,47 FPS, frame dài nhất 53,71 ms.
Model nâng cấp và nhân viên đã chia batch, checkpoint ghi nền với journal/lease giữ an toàn.
Mục tiêu 39 nhân viên và năng lực 1,5 lần là yêu cầu thiết kế hiện hành của người dùng.
Số 78 và các call path đồng bộ bên dưới chỉ mô tả phiên bản trước, không áp dụng để bàn giao.

## Kết luận từ bằng chứng hiện có

- **Đã đo:** lượt chơi thường 243,32 giây ở 1920×1080, RTX 4060 Laptop; average 157,57 FPS,
  full-city average 155,54 FPS, worst frame 781,44 ms, 4 frame trên 100 ms
  (`work/city-expansion-20261008/ordinary-play-20261008T034544802Z/ordinary-play-report.json:11-25`).
- **Đã đo:** timeline ghi 781,438 ms ở giây 72,30; cùng mẫu có CPU 2,193 ms/GPU 0 ms,
  workers 2. Mẫu tiếp theo ở 73,31 có workers 78 và frame 6,060 ms
  (`.../ordinary-play-timeline.csv:74-75`). Đây là dấu hiệu thời điểm khớp với chuyển trạng thái,
  không phải profiler attribution: timeline chỉ có 197 mẫu trong 243 giây, không có marker click.
- Average FPS tốt không loại trừ hitch đơn lẻ; không kết luận gameplay không khựng.

## Nguyên nhân có khả năng cao

1. **Tạo lại hình nâng cấp đồng bộ trên frame bấm nút — nghi vấn mạnh nhất.** Nút gọi
   `ApplyMod` → `ApplyMaximum` → `UpgradeModelView.RefreshAll` ngay trong callback
   (`GameHudModMenu.cs:81-99`, `DevelopmentAssistance.cs:20-25`). `RefreshAll` duyệt mọi trạm
   đủ loại và toàn bộ workers (`UpgradeModelView.cs:19-37`); mỗi view thay cây `PersistentUpgradeDetails`
   rồi gọi `UpgradeModelParts.Build/Crew` (`UpgradeModelView.cs:88-103`). Lượt này đồng thời mở khóa
   roster 78 worker theo timeline. Số object/mesh tạo lại có thể gây spike, nhưng hiện chưa đo duration.

2. **Lệnh mod sao chép và cập nhật state giao dịch lớn.** `TransactionCore.Execute` tạo draft
   `RuntimeCopy(true, ...)`, áp command rồi commit (`TransactionCore.cs:58-72`); `GrantMaximum` đi qua
   mọi upgrade, cập nhật purchase/crew, sau đó refresh progression (`PhysicalCashTransactions.cs:38-74`).
   Transaction store flush journal đồng bộ bằng `Flush(true)` (`GameplayTransactionStore.cs:42-51`),
   rồi runtime refresh snapshot và projection inventory (`RuntimeTransactionBindings.cs:123-135`).
   Đây là công việc đồng bộ trên đường click; chưa có profile cho đúng frame để định lượng phần nào.

3. **Checkpoint save lớn ngay sau lệnh.** `ApplyMod` gọi `game.SaveGame()` sau khi mod thành công
   (`GameHudModMenu.cs:94`). `SaveGame` gọi `Transactions.Checkpoint`; checkpoint chiếu toàn state ra save,
   ghi save rồi xóa/flush journal (`GameSession.cs:194-198`, `RuntimeTransactionBindings.cs:121-122`,
   `GameplayTransactionStore.cs:53-61`). File save trong evidence hiện khoảng 8,3 MB. Ghi file đồng bộ
   có thể chờ I/O; kích thước file không chứng minh 781 ms do disk.

## Thay đổi tối thiểu và kiểm chứng đề nghị

- Trước khi đổi persistence, đo riêng trong một lượt ordinary-play cùng thao tác: `GrantMaximum`/core
  execute, `RefreshAll`, `SaveGame/Checkpoint`; lưu thời lượng từng đoạn và marker click/frame.
- Nếu model rebuild chiếm spike, giữ transaction commit/journal/checkpoint nguyên vẹn và chia riêng
  dựng model trạm/worker thành batch qua frames sau khi state đã persist. Không giảm roster, đồ họa,
  hoặc bỏ flush/checkpoint để che triệu chứng. Nếu save/flush chiếm spike, cần thiết kế ghi snapshot
  an toàn theo lease và atomic replace trước khi chuyển I/O khỏi main thread.
- Chạy lại đúng 3–5 phút ở 1080p, đủ 78 workers; ghi max frame time và các frame >100 ms quanh click,
  cùng CPU/GPU và thời lượng ba đoạn trên. So sánh trước/sau; không dùng average FPS làm tiêu chí duy nhất.
- `GameRules.ApplyProgression()` không phải nghi phạm trực tiếp trong runtime giao dịch: nhánh đầu
  trả ngay khi `Transactions != null` (`GameRules.cs:136-147`). `GrantMaximum` gọi
  `RefreshProgression(state)` trong transaction state riêng.

## Giới hạn

Chưa có profiler trace hoặc timestamp click gắn với frame 72,30; CPU/GPU sample thấp không định vị
được stall. Ba nguyên nhân trên là suy luận từ call path đồng bộ và sự kiện gần thời điểm hitch,
không phải kết luận định lượng. Chỉ lượt chơi ordinary đã có mới được dùng; không chạy benchmark.
