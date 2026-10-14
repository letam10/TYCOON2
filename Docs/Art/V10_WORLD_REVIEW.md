# Kiểm hình phương tiện, máy và thành phố v10

## Ảnh nguồn đã nhìn

- `work/items-vehicles-20261009/player-20261009T124757000Z` là ảnh Player thật.
- Đã nhìn cả 90 ảnh riêng của 10 loại xe/tàu qua `review/vehicle-angles-01..04.jpg`
  và `review/vehicle-wide-01..05.jpg`; ảnh rộng dùng để kiểm đầy đủ xe dài.
- Đã nhìn 117 ảnh riêng của 13 máy qua `review/machines-isolated-01..07.jpg`.
- Đã nhìn 72 ảnh riêng của 8 khu ngoại vi qua `review/city-isolated-01..04.jpg`.
- Mỗi bộ gồm 8 hướng quanh vật và ảnh trên cao; ảnh nguồn luôn giữ nguyên.

## Phát hiện và xử lý

- Khay, mô-tơ và bảng cấp nâng của máy có phần lơ lửng so với thân máy mới.
  `MachineUpgradeParts` bổ sung giá/cột/chân chạm nền và giữ nguyên hiệu ứng cấp nâng.
- Camera kiểm riêng xe buýt/xe khách/xe tải/xe bồn/tàu quá sát khi nhìn trên cao.
  Khoảng cách mới dựa bounds thật, thay khoảng cách hardcode.
- Nước bãi biển/bến tàu có giao sóng dạng ô lặp; shader mới dùng pha lệch và biến thiên chậm,
  lọc theo độ lớn pixel để giảm mô-típ lặp và rung hình ở xa.
- Cầu gỗ bến tàu cao hơn step của capsule; thêm dốc và kiểm đi bộ thật ra hai cầu trong lượt mới.

## Giới hạn hiện tại

- Cần ảnh của binary cuối để xác nhận các sửa hình ảnh nêu trên.
- Cần nhìn 5 góc đặt thật của đủ xe, trạm, kho/bàn/ghế và 8 khu, không suy từ bộ ảnh riêng.
- Không kết luận FPS hoặc mọi đường đi từ ảnh tĩnh. Báo cáo Player ghi riêng va chạm, mô phỏng và FPS.
