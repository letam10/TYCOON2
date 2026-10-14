# V14: kết quả kiểm tra sau đóng gói

Đạt kiểm tra tính toàn vẹn lúc `2026-10-10T09:52:29.770977+00:00`.
Không chạy Player trong tác vụ này; `playerExecuted=false`, `stable165Accepted=false`.

## Đầu ra

- Báo cáo: `work/city-model-v14-20261010/Delivery/POST_PACKAGE_CHECKS.json`.
- Bản giải nén: `work/city-model-v14-20261010/PackageSmoke`.
- Executable đã đối chiếu byte: `PackageSmoke/TYCOON2.exe`.
- Công cụ: `Tools/verify_city_delivery.py`; kiểm thử: `Tools/test_verify_city_delivery.py`.

## Đã xác minh

| Gói | Số file | Byte ZIP | Byte giải nén | SHA256 và CRC |
| --- | ---: | ---: | ---: | --- |
| Windows.zip | 54 | 52723361 | 134812653 | Đạt |
| Source.zip | 2065 | 589121167 | 676688298 | Đạt |
| Evidence.zip | 1154 | 1166939535 | 1177861366 | Đạt |

- Cả ba SHA256 khớp manifest; đọc toàn bộ entry để kiểm CRC thực tế.
- Tên entry an toàn trên Windows, không trùng khi bỏ phân biệt hoa thường,
  không có symlink, tên thiết bị dành riêng hoặc xung đột file/thư mục.
- Windows khớp đủ 26 file từ Candidate và 28 tài liệu từ checkout.
- Windows không có entry source, save, journal, private, progress hoặc debug bị cấm.
- 54 file giải nén khớp byte ZIP; thư mục đích được tạo mới bên dưới workspace/work.
- Source khớp chính xác 2065 đường dẫn trong SourceList và byte checkout;
  không có Git LFS pointer chưa mở rộng.
- Evidence có 1154 file, gồm 954 PNG được kiểm cấu trúc và giải mã đầy đủ.
  Không có entry save/journal/private/progress theo quy tắc đường dẫn.
- SHA256 của ZIP và manifest được đối chiếu lại sau khi giải nén; các gói không bị thay đổi.

## Kiểm chứng công cụ

`python Tools/test_verify_city_delivery.py`: 22/22 kiểm thử đạt.
Bao gồm fixture hợp lệ, CRC sai dù SHA manifest khớp, tên nguy hiểm/NUL,
entry trùng, symlink, sai tổng byte/số file, source hoặc runtime lệch byte,
LFS pointer, PNG giả/thiếu, dữ liệu riêng và đường dẫn giải nén không hợp lệ.

`python Tools/verify_city_delivery.py`: exit code 0, `passed=true`.
Công cụ chỉ ghi report sau khi mọi bước đạt và từ chối ghi đè report/thư mục đã có.
Do đó lệnh này cố ý từ chối lần chạy kế tiếp trên cùng đầu ra đã được xác minh.

## Giới hạn bằng chứng

PASS ở đây xác nhận byte, cấu trúc gói và khả năng giải mã PNG.
Nó không xác nhận Player đã chạy từ ZIP, hình ảnh/gameplay đúng,
165 FPS ổn định, 165 Hz trình chiếu thực tế hoặc hoàn tất toàn bộ mục tiêu.
Đánh giá dữ liệu riêng trong gói dựa trên tên/đường dẫn bị cấm,
không phải kiểm toán mọi chuỗi văn bản trong log, báo cáo hoặc ảnh.
