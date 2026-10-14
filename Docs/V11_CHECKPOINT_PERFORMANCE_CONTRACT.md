# Hợp đồng rà checkpoint V11

## Mục tiêu

Tìm nguyên nhân CPU spike của `SaveGame.Checkpoint` tăng từ khoảng 15 lên 60–78ms.
Chỉ rà mã và ghi đề xuất có bằng chứng; không chạy Unity/Player khi benchmark đang chạy.

## Hợp đồng hiện hành

- `CheckpointSnapshot.Copy<T>(T value)` phải sao chép sâu DTO, không giữ tham chiếu world sống.
- `TransactionCore.CaptureCheckpoint()` khóa `gate` trong khi chụp snapshot nhất quán.
- `CheckpointWriter.Start(SaveData ownedSnapshot)` nhận snapshot độc lập và ghi ở `Task.Run`.
- `CheckpointWriter.Observe(bool wait)` giữ lỗi task để mọi lần drain nhìn thấy lỗi.
- ID transaction/receipt, số tiền long, stacks, reservations, orders và sequence phải được bảo toàn.
- Không đổi save version, không xóa lịch sử, không bỏ journal hoặc làm yếu lease/recovery.
- Không chuyển đối tượng Unity hoặc DTO còn được sửa sang worker.
- Không kéo dài autosave để che spike; không bỏ frame khỏi báo cáo FPS.

## Phạm vi giao

Chỉ tạo `Docs/V11_CHECKPOINT_PERFORMANCE_FINDINGS.md`, dưới 300 dòng.
Đọc `CheckpointSnapshot.cs`, `CheckpointWriter.cs`, RuntimeTransactions/checkpoint,
SaveData/TransactionState và các test checkpoint/save trong Assets/_Game.
Đọc operation-timings và CPU report của ordinary-play-20261010T015900930Z,
ordinary-play-20261010T024906901Z, ordinary-play-20261010T030724987Z nếu cần.

## Kết quả cần có

- Chỉ rõ từng bước chụp checkpoint và phần có chi phí theo số receipt/order/stack.
- Đề xuất nhỏ nhất giữ nguyên dữ liệu và semantics; chỉ ra file và signature cụ thể.
- Nêu test isolation/recovery/journal cần kiểm và lệnh native đúng của dự án.
- Không kết luận tốc độ cải thiện khi chưa đo Player.
- Không sửa source/tests, không tạo subagent, không chạy Unity/Player, không commit/publish.

## Tự kiểm

`git diff --check -- Docs/V11_CHECKPOINT_PERFORMANCE_FINDINGS.md`

Báo một lần, tối đa 15 dòng: file tạo, bằng chứng, đề xuất, vấn đề còn mở.
