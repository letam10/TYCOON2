# Máy công nghiệp v10

- Thay model thân máy chung bằng các nhóm xay, chế biến sữa/sốt, sợi/vải, trộn bột, lò và bếp.
- Chi tiết gồm chân chống rung, khung, động cơ, bình chứa có thành dày, ống, bảng điều khiển và cửa.
- Rotor chức năng tách khỏi các phần tĩnh; phần tĩnh gộp theo vật liệu, dùng lại chuyển động hiện có.
- Giữ ID, recipe, inventory, collider gameplay, điểm tương tác, đèn và thanh tiến độ của trạm.
- Bộ kiểm thử dựng mọi recipe, kiểm chân chạm nền, giới hạn footprint, vật liệu dùng chung,
  rotor quay đủ vòng, mesh bình kín và các dữ liệu gameplay giữ nguyên.
- Root tiếp quản tích hợp sau khi tác vụ module bị ngắt; đã sửa test từ Add sang API TryAdd thật.
- Full suite native Unity 778/778 đạt, gồm kiểm mọi recipe và giữ dữ liệu gameplay.
- Đã kiểm 9 hướng riêng cho đủ 13 máy trong Player `player-20261009T124757000Z`.
  Phát hiện khay/mô-tơ/bảng cấp nâng bị thiếu giá đỡ; bổ sung `MachineUpgradeParts`
  với cột khay, chân mô-tơ, bảng điều khiển và bảng chất lượng chạm nền.
- Regression test kiểm giá đỡ tiếp xúc với khay/bảng và chạm nền đã đạt.
- Chụp hình kiểm sau khi animation lắp ráp hoàn tất, camera dựa kích thước model.
- FPS và ảnh sau sửa giá đỡ vẫn cần kết quả Player của binary dựng lúc 13:11:30 UTC.
