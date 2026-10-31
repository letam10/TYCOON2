# Save replacement regression — 29/10/2026

## Quan sát

Baseline Player từng lỗi File.Replace với thông báo Unable to remove the file to be replaced.
Full suite sau thay map tái hiện 6 fail liên quan checkpoint/transaction file replacement, gồm lỗi
trực tiếp và assertion sau thao tác lưu thất bại. Không có bằng chứng xác định process nào giữ file
trong lượt lỗi đầu tiên; không quy trách nhiệm cho antivirus hoặc người dùng.

## Sửa có giới hạn

AtomicSaveFile.ReadText mở snapshot với FileShare.ReadWrite | FileShare.Delete để reader của game
không chặn rename nguyên tử. Cả SaveStore và FileTransactionStore gọi AtomicSaveFile.Replace.
Chỉ retry IOException có Win32 code 32, 33 hoặc 1175; tối đa 4 lần chờ 10/20/40/80 ms (tổng 150 ms),
và chỉ khi cả temporary lẫn destination vẫn tồn tại. Giữ File.Replace; không xóa destination,
không copy đè nội dung, không lặp transaction hoặc thay đổi revision/receipt boundaries.
Lỗi quyền/disk và lock kéo dài vẫn được báo ra.

Microsoft mô tả lỗi 1175 giữ nguyên tên cả file đích lẫn file thay thế:
https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-replacefilew
Không retry mã 1176/1177 vì trạng thái tên file sau lỗi có thể khác.

## Kiểm chứng mới

AtomicSaveFileTests tạo file riêng và giữ FileShare.Read để tái hiện ReplaceFile lỗi code 32.
Sau khi reader nhả khóa, bounded retry commit bản mới và không tạo bản sao phụ.
Với khóa kéo dài, ghi vẫn thất bại và cả nội dung cũ/mới giữ nguyên trong test.
Các fault injection cũ Prepared/TemporaryFlushed/Replaced/Published vẫn chạy như trước.

map-save-tests-2.xml: 1215 total, 1215 passed, 0 failed, 0 skipped.
Đây là bằng chứng regression và nguyên tắc bảo toàn file, không chứng minh mọi lỗi I/O trên mọi máy
đã biến mất. Player slot/relaunch trên bản build cuối phải có report riêng.
