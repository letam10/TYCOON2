# Render và mở rộng thành phố — v10

## Cơ chế đã đối chiếu tài liệu chính thức

- Frustum culling: Unity bỏ các renderer ngoài khung nhìn của camera.
- GPU occlusion culling: URP dùng độ sâu khung hiện tại/trước đó để loại vật bị che khuất.
  Thuật toán bảo thủ giúp hạn chế vật bật/tắt khi quay camera; vẫn phải kiểm Player.
- GPU Resident Drawer và instancing: nhiều vật dùng chung mesh/material được gom lệnh vẽ.
  GPU vẫn xử lý hình học/pixel của các bản sao nhìn thấy, không phải chỉ render một vật duy nhất.
- Phím nhân bản không bảo đảm batching. Blender dùng Alt+D để tạo linked duplicate,
  Shift+D cho duplicate thường. Trong Unity, kiểm mesh/material và shader mới xác nhận chia sẻ được.
- Samples và light bounces của Blender Cycles khác render realtime URP. Không có tham số
  mặc định 4096 samples hoặc 12 tia sáng trong game này để hạ trực tiếp xuống 1024/4.
- Baked occlusion cổ điển phù hợp cảnh tĩnh bake trước. Thành phố này dựng runtime nên dùng
  frustum theo chunk và thử GPU occlusion, không dựa vào dữ liệu occlusion bake chưa tồn tại.

## Thay đổi đang kiểm chứng

- Thành phố 565.685424 × 384.666088 m, gấp đôi mỗi chiều; giữ revision bố trí cũ và tọa độ trạm.
- Tám khu ngoại vi: cảnh quan sở cảnh sát, sở cứu hỏa, bar, khu giải trí, sân chơi,
  tượng đài, bến tàu và bãi biển. Đây là model/cảnh quan, chưa thêm mô phỏng nghề cảnh sát/cứu hỏa.
- Hình tĩnh gộp theo vật liệu và ô 32 m; không gộp cả thành phố thành một renderer khổng lồ.
- Shader TownSurface/TownWater thêm biến thể DOTS để tham gia GPU Resident Drawer.
- Bật GPU occlusion trên PC pipeline; có cờ QA `--qa-disable-gpu-occlusion` để đo A/B cùng binary.
- Chế độ cân bằng dùng tối đa hai cascade bóng. Low/High giữ lựa chọn riêng.
- Nước dùng hai sóng normal và Fresnel, không ray tracing, không planar reflection thời gian thực.
  Bỏ tọa độ bờ sông hardcode sai; bãi biển có ranh giới bờ riêng.
- Mesh tĩnh giải phóng bản CPU ở Player thường; lượt kiểm hình giữ readable để kiểm đỉnh/normal.
- 8 hướng quanh model và một ảnh trên cao bổ sung; trong game chụp 5 góc gồm trên cao.

## Bằng chứng hiện có và phần còn phải đo

- Player trước khi ghép v10: `work/items-vehicles-20261009/player-20261009T060620890Z`.
  Rides-only PASS, 0 runtime errors, cả 11 xe/tàu có lượt lên và xuống thực tế.
- Kết quả đó không thay thế kiểm thử model, thành phố và FPS sau khi ghép v10.
- Chưa xác nhận 165 FPS ổn định. Cần số liệu từ Player RTX 4060 Laptop, 1080p,
  cảnh đầy đủ và đo P95/P99/spike bên cạnh FPS trung bình; cần ghi riêng chi phí khởi tạo/NavMesh.
- Ca mod hiện cửa sổ sau tối ưu icon: 162,292 FPS, P95 6,094 ms; khựng quanh mod giảm
  từ 774,936 xuống 86,385 ms. Ca có profiler, chưa dùng làm FPS bàn giao toàn thành phố.
- Cửa sổ ẩn từng cho ảnh đen; FPS của các lượt đó không chứng minh render thực tế.
  Lượt bàn giao bắt buộc hiện cửa sổ, 1080p, 1x, không profiler và không chụp gallery đồng thời.

## Nguồn

- https://docs.unity3d.com/6000.0/Documentation/Manual/urp/gpu-culling.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/urp/gpu-resident-drawer.html
- https://docs.unity3d.com/ja/6000.0/Manual/OcclusionCulling.html
- https://docs.unity3d.com/ja/6000.0/Manual/GPUInstancing.html
- https://docs.blender.org/manual/en/4.2/render/cycles/render_settings/sampling.html
- https://docs.blender.org/manual/en/5.0/render/cycles/render_settings/light_paths.html
- https://docs.blender.org/manual/id/4.5/scene_layout/object/editing/duplicate_linked.html
- https://docs.blender.org/manual/en/4.3/scene_layout/object/editing/duplicate.html
