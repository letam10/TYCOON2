# Chỉnh địa lý biển và sông — yêu cầu 29/10/2026

Cập nhật 31/10: đã triển khai phương án dời hai POI, kiểm Player và sửa cửa sông. Source commit: 4b74d0f. Kết quả cuối ở hydrology-20261031/DELIVERY.md.
Các phát hiện nguồn bên dưới ghi baseline 3a3ad93 trước khi sửa.
Không dùng sửa texture map để che lỗi địa hình.

## Bằng chứng nguồn

- WorldFactory.Build tạo một hộp Grass phủ toàn bộ WorldBounds 1131,370848 × 769,332176 m,
  tâm (8,-0,13,22), mặt trên -0,005m.
- CityOceanEdge.Build đặt outerWaterStart = bounds.max.x + 3. CityOceanHorizon.Build chỉ dựng
  mặt biển chính từ đó ra xa. Trong world, Strip(261,267,...) chỉ rộng 6m; bãi biển có một
  mảng nước riêng x≈247–267, cảng/marina cũng có mảng riêng.
- CityLandscape.River dùng 13 hộp dài 21m tại z=103. Nếu CityDressingSpace.Take thất bại,
  vòng lặp bỏ cả đoạn nước, bờ và cầu. Không có nguồn sông, cửa sông hoặc graph dòng chảy liên tục.
- Bãi công nghiệp đang ở (410,0,300), footprint 180×140; bến phía Đông (420,0,-120),
  footprint 180×110. Mở biển về phía Đông mà không xử lý hai khu sẽ đặt chúng vào nước.
- Tọa độ còn lặp trong WorldRouteCorridor, WorldPoiAccessCatalog, CityMapLocations và
  WorldExpansionDressing. Khi đổi vị trí phải cập nhật qua dữ liệu chung, không sửa riêng icon.

## Đã chốt

Camera gameplay, nhân vật và logic Farm giữ nguyên. Biển/sông phải được sửa trong geometry,
collider, NavMesh và route thật, sau đó hệ map tự chụp lại từng ô đúng world bounds.
Ghi chú ban đầu được giữ để truy nguyên; trạng thái commit/push cuối xem báo cáo bàn giao.

## Quyết định đã chốt

Người dùng đã chọn: dời bãi công nghiệp vào đất liền; bến phía Đông đặt sát đường bờ mới.
Đã thực hiện: IndustrialYard (-200,0,315); EastMarina (230,0,-215).

Đây là lựa chọn bố cục world, không phải xin lại quyền sửa/build/push vốn đã được yêu cầu.
Các thay đổi tọa độ được triển khai sau câu trả lời xác nhận của người dùng.

## Quy ước cho implementation tiếp theo

- Một WorldHydrologyLayout định nghĩa shoreline, river samples, mouth, water elevation và
  coast/river banks. Ocean, river, nav exclusions, shoreline materials và validators cùng dùng nó.
- Biển là vùng liên tục từ bờ đến ngoài tầm nhìn. Ranh giới land/water có đường cong hợp lý,
  cát/bờ chuyển tiếp; không để dải cỏ chắn phía ngoài biển hoặc dải nước hình chữ nhật vô nghĩa.
- Sông đi từ thượng lưu tới cửa biển theo đường cong liên tục, bề rộng thay đổi có chủ đích.
  Mọi đoạn đều được dựng; gặp đường phải dựng cầu/đổi tuyến, không skip đoạn nước.
- Cửa sông và biển cùng cao độ; mép cuối river mesh dùng chính điểm giao shoreline để không gap.
- Công trình/quay có nền đỡ; đường/đường sắt qua nước có cầu, deck/collider/approach/ramp thực.
- Không có NavMesh hoặc đường đi bộ xuyên nước ngoài cầu/bến được chỉ định.
- Shader giảm caustic lưới đều, dùng variation theo world space và vùng nông; wave/foam không
  chạy xuyên đất. Các pass color/depth/normals dùng cùng displacement.
- Di chuyển POI vẫn giữ ID, cập nhật entry/exit/map/route/collider đồng thời; không đổi Farm.

## Nghiệm thu bắt buộc

- Test river continuity, mouth-to-ocean seam, grounding POI, finite/nondegenerate mesh.
- Kiểm tuyến từ Farm tới công nghiệp/bến sau đổi layout, bridge clearance và không đi bộ trên biển.
- Ảnh native camera cũ tại bờ, dọc sông, cửa sông, cầu, bến/công nghiệp; ảnh top-down toàn vùng
  và các ô map mới. T0/T1 kiểm sóng không lặp như ô bàn cờ, không có seam lộ hoặc z-fighting.
- Build/test/Player/save-restart mới cho source sau sửa; không tái dùng report map trước sửa để
  kết luận đạt bản địa hình mới. Không tự chạy benchmark dài.

## Checkpoint đã có để tiếp tục

MapPhotoWindows/build-report.json: 0 errors, 34 warnings, 2026-10-29T07:52:58Z.
map-save-tests-2.xml: 1215/1215 pass. map-player-final: 38 checks, no runtime errors.
world-final: 6132 checks pass; process reload: 9 checks pass.
Gói TYCOON2-MapPhoto-Windows-20261029.zip đã kiểm SHA256 110 file; đây vẫn là bản trước sửa biển/sông.
Checkpoint ban đầu ở codex/tycoon2-review-20261029; xem báo cáo bàn giao cho commit mới. Không đụng checkout bẩn D:/GAME/TYCOON2.
