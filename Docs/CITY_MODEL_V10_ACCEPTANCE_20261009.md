# Model, thành phố và hiệu năng v10 — 09/10/2026

## Thao tác mới

- Nút **Cài đặt**: chất lượng Low/Cân bằng/High, độ phân giải, render scale, toàn màn hình,
  tiếng Việt/English, đồng bộ màn hình, giới hạn FPS và bật/tắt thanh FPS.
- Muốn đo giới hạn 165 bằng phần mềm: tắt đồng bộ màn hình, đặt giới hạn 165 và bật thanh FPS.
  Chế độ này khác VSync; FPS thực tế phải đọc từ số đo, không suy ra từ giá trị giới hạn.
- Tay chỉ cầm một loại hàng hoặc tiền. Tiền thu nằm trên tay; két có vùng gửi và rút riêng.
- Hàng trực tiếp xếp 2 cột × 3 hàng theo tầng; cà rốt thuôn nhỏ về một đầu, lá chỉ ở đầu lớn.
- Chỉ `egg`, `milk`, `bottled_milk`, `sauce`, `soy_sauce` dùng vật chứa khi cầm.
  Một quả/chai hiện đúng một quả/chai; khay tầng chưa đầy không có hàng giả.
- Thịt, phô mai, bánh mì, vải, ngô, bó lúa, len và các hàng khác cầm trực tiếp.
- **Mod game** có đúng hai dòng: Mod tiền và Mod xây dựng tối đa; tiến trình tối đa được lưu.

## Nội dung đã tích hợp

- 27 loại hàng, tiền và kiện hàng; model đứng riêng, bố cục trên kho/bàn và bố cục cầm riêng.
- Máy nghiền, thức ăn, phô mai, sốt, chiết xuất, đóng sữa, kéo sợi, dệt, trộn bột, lò và bếp
  có thân, chân, motor, đường ống, khay và bộ phận chức năng; bộ phận quay giữ riêng khi batching.
- Nâng cấp thêm chi tiết có chân/giá đỡ; ô cuối biến mất cả hình ảnh và collider.
- Bàn tròn có hai ghế, bàn vuông có ghế dài; ghế quầy và tư thế ngồi đã có kiểm Player.
- Đội ngũ tối đa 39, đủ 26 nghề, năng lực ×1,5; đi theo mạng đường và vùng tương tác riêng.
- Xe buýt, xe khách, taxi, xe hơi, xe tải nhỏ/lớn, xe bồn, xe sang, xe thể thao và hai đoàn tàu.
- Hai đường ray, nhà ga hai sân, cầu và dốc đi qua hai phía; hành khách thực sự lên/xuống.
- Thành phố 565,685424 × 384,666088 m: gấp đôi từng chiều, diện tích gấp bốn so với mốc trước v10.
- Tám khu cảnh quan: cảnh sát, cứu hỏa, bar, giải trí, sân chơi, tượng đài, bãi biển, bến tàu.
  Chưa bổ sung nghề cảnh sát/cứu hỏa hay hệ thống kinh doanh bar.

## Tối ưu render

- Frustum culling, mesh/material dùng chung, gộp tĩnh theo vật liệu và ô 32 m.
- GPU Resident Drawer và GPU occlusion trên PC; biến thể DOTS cho shader mặt đất/nước.
- Bóng chuyển sang một cascade và khoảng xa fade; nước dùng normal/Fresnel, không ray tracing hay planar reflection.
  Đối chứng và trạng thái: [SHADOW_FIX_20261010.md](SHADOW_FIX_20261010.md).
- Không dùng samples/bounces của Blender để giả làm tham số render realtime của URP.
- Chi tiết kỹ thuật và nguồn chính thức: [RENDER_OPTIMIZATION_V10.md](RENDER_OPTIMIZATION_V10.md).

## Kiểm chứng hiện tại

- Full EditMode: **792/792 PASS**, không skip, sau tối ưu đơn hàng/capsule/animation máy.
  XML: `work/city-model-v10-20261009/editmode-results.xml`.
- Windows build thành công: `work/city-model-v10-20261009/Release/TYCOON2.exe`.
- Ca chẩn đoán hiện cửa sổ sau sửa: `ordinary-play-20261009T135924656Z`, PASS, 0 runtime errors.
  Thu → bán → cầm tiền → gửi/rút két → góp nâng cấp → mod tối đa đều đi qua input/UI thật.
- Khựng khi mod: trước sửa khoảng 774,936 ms; sau sửa khung chậm nhất quanh mod khoảng 86,385 ms.
  Bỏ vẽ lại icon cho ô đã hoàn tất; vẫn giữ điểm neo khi khởi tạo bố cục lần đầu.
- Ca ngắn 104,866 giây đạt 162,292 FPS trung bình, P95 6,094 ms, P99 7,329 ms.
  Đây là ca có profiler, chưa thay thế lượt chơi toàn thành phố không profiler.
- Lượt chơi đầy đủ `ordinary-play-20261009T140232595Z`: PASS, 635,243 giây, 2.936,326 m,
  1.038 tương tác, đủ sáu khu kinh doanh/tám khu mới/hai cầu bến tàu, 0 runtime errors.
- FPS lượt đầy đủ: **147,361**, P95 **9,418 ms**, P99 **12,031 ms**;
  CPU/GPU trung bình **6,172/2,060 ms**. Chưa đạt mục tiêu khoảng 165 FPS.
- Đã sửa tra cứu đơn hàng: lập chỉ mục theo ID mỗi projection, không quét lịch sử và tạo chuỗi cho từng đơn.
  Ba test kiểm đơn mới, giao một phần và projection mới đều PASS.
- Ca profiler cùng save đầy đủ sau sửa: `ordinary-play-20261009T142946144Z`, PASS, 0 runtime errors,
  151,562 FPS gồm khởi tạo, P95 7,783 ms; CPU/GPU 5,987/2,273 ms.
  Chi phí Update khách giảm từ khoảng 0,569 xuống 0,076 ms/frame trong hai ca chẩn đoán.
  Ca này đứng tại chỗ 60 giây, chưa thay thế lượt chơi toàn thành phố sau sửa.
- Capsule đứng yên vẫn bật va chạm; mọi bước ngang áp dụng ngay, kiểm lại mặt sàn tối đa mỗi 80 ms.
  Test mất sàn, đổi vị trí và bắt đầu đi sát tường đều PASS.
- Animation nâng cấp hoàn tất ngừng ghi vị trí/tỷ lệ chi tiết tĩnh; rotor tiếp tục quay.
- Player `ordinary-play-20261009T144927602Z`: PASS, 0 lỗi runtime, 616,796 giây, visible,
  manual165, không profiler; trung bình 146,517 FPS, P95 9,969 ms, P99 14,250 ms.
- Gallery `player-20261009T150142691Z` FAIL tại lượt đi xe sedan; không xem là nghiệm thu PASS.
  Đang sửa kẹt ở góc tuyến. Gallery cuối, đóng gói và báo cáo nghiệm thu vẫn chưa hoàn tất.

## Kiểm hình và giới hạn bằng chứng

- Gallery trước sửa cuối: 1.426 ảnh, 3.355 kiểm tra, 0 lỗi runtime, Player PASS.
  Mỗi model có tám hướng quanh vật và ảnh trên cao bổ sung; đặt trong game có năm góc.
- Toàn bộ 27 hàng đã được nhìn riêng/cầm từ chín góc và số lượng 1/24/48.
- Két/kho/kệ/quầy/sáu bàn và sáu công trình tĩnh đã được nhìn ở các góc được giao.
- Root đã kiểm các phương tiện, 13 máy và tám khu mới; phát hiện và sửa chân đỡ nâng cấp,
  họa tiết nước và khoảng cách camera gallery. Cần ảnh bản mới để xác nhận các sửa này.
- Một số góc đặt bị vật khác che; không kết luận mặt bị che đẹp chỉ từ số lượng ảnh.
- 4K là render offscreen; UI native được thử ở 720p/1080p/1440p.
- Player ẩn từng cho ảnh đen và Animator culling không chạy xương chân.
  Dữ liệu đó không dùng nghiệm thu hình ảnh/FPS; Player hiện cửa sổ đã xác nhận NPC có animation.
- VSync D3D11 hiện cửa sổ từng báo refresh drift và không khóa 165.
  Không thay đổi thiết lập NVIDIA/Windows; xem [V10_FRAME_PACING_FINDINGS.md](V10_FRAME_PACING_FINDINGS.md).
- Chưa kết luận 165 FPS tuyệt đối ở mọi frame, không có mọi bug hoặc mọi tuyến camera đã được kiểm.
