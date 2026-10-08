# Hợp đồng sửa hình ảnh, animation và va chạm — yêu cầu mới 08/10

## Yêu cầu đang có hiệu lực

- Nền tự nhiên phủ cỏ bằng vật liệu/procedural trên cùng mặt đất, không dùng cụm model cỏ.
- Có vùng đất trơ ngẫu nhiên, đá rải rác, vỉa hè và vài vết nứt nhẹ trên đường.
- Cây và đồ vật còn đơn giản được bổ sung hình dáng, vật liệu, chi tiết có chức năng.
- Thịt cầm to rõ như video; cà rốt/lúa nhỏ hơn một chút, nằm thấp và so le hai hàng.
- Hiển thị đủ số hàng trong sức mang, không giảm tải hoặc bỏ model để tăng FPS.
- Sửa đi bộ NPC, xuyên đồ vật, font/màu/kích thước; thêm animation người chơi/NPC và nâng cấp.
- Giữ rig/mesh nhân vật hiện hành; thay đổi animator/chuyển động được phép.
- Giữ sáu khu, diện tích x2, save/journal, ID, tiền cầm/két, caps và hai mod đã triển khai.
- Chỉ kiểm tra hiệu năng bằng chơi bình thường 1x vài phút, đủ NPC/đồ họa, không benchmark.

## Giao diện và định dạng dùng chung

- `CityDressing.Build(GameSession game, Transform world)` tạo cảnh, kết thúc bằng geometry.Finish.
- `CityLandscape.Build(CityStaticGeometry g, CityDressingSpace space)` dựng cảnh quan.
- `CityDressingSpace.Take(Vector3 center, float width, float depth, string name)` giữ lối tương tác.
- `TownSurfaceMaterials.Get(string surface, string tint)` trả vật liệu cache.
- Surface IDs giữ nguyên: grass, soil, asphalt, paving, wood, stone, metal, roof, water.
- `HandItemModels.Build(string id, Transform parent)` trả một GO tên chính xác SKU,
  MeshFilter, một MeshRenderer/submesh/material atlas chung; pool dùng tên đó.
- `HandItemModels.Supports(string id)` và 27 ID hàng cùng cash/crate giữ nguyên.
- `InventoryStack.Refresh()` theo Inventory.Revision; không dựng lại chồng nếu dữ liệu không đổi.
- Caps player 12/20/32/48, worker 12/20/32, loader 12/24/36; tiền long không giới hạn.
- Layout hai hàng chỉ áp dụng chồng trên tay, không thay đổi sức chứa kho/xe hoặc giao dịch.
- API profile mới nếu cần: `CarryItemLayout.For(string id)` trả struct có rows, columns,
  scale, rotation, spacing; khai báo cùng module dưới 300 dòng, sử dụng ở InventoryStack.
- `UpgradeModelView.RefreshAll(GameSession game, bool animate)` phải hoạt động ngay trong EditMode;
  Player có thể chia dựng visual qua frame, dữ liệu cấp phải phản ánh state ngay.
- `UpgradeModelParts.Build(...)` và `Crew(...)` giữ đủ chi tiết theo từng trục/cấp.
- Model/animation chỉ phản ánh giao dịch đã commit; không tự sửa tiền/hàng/cấp.
- Root sẽ thêm `ActorPhysicalMotion.Attach(NavMeshAgent)` và `Navigation.ActualSpeed(agent)`.
  Module cảnh quan/vật phẩm không phụ thuộc hoặc sửa code motion.

## Phân quyền file

### Agent terrain_surface_v3

Được sửa: CityDressing.cs, CityLandscape.cs, CityProps.cs, TownSurfaceMaterials.cs,
TownSurfaceTexture.cs, Resources/VisualRedesign/TownSurface.shader.
Được tạo module CityGroundSurface*, CityVegetation* và test tương ứng.
Được bỏ CityGroundCover/CityGrassGeometry/TownGrass và test chỉ phục vụ các cụm cỏ do lần này tạo.
Không sửa CityStaticGeometry*, Navigation*, model cầm, UpgradeModel*, ActorView hoặc UI.
Không tạo collider cỏ. Cây/đá/đồ vật đặc thêm collider hợp kích thước; giữ Take và lối đi.

### Agent carry_upgrade_v3

Được sửa HandItemModels/Produce/Products/SurfaceAtlas/MeshPrimitives,
InventoryStack.cs, CarryPresentation.cs, UpgradeModelView.cs, UpgradeModelParts.cs.
Được tạo CarryItemLayout*, UpgradeVisual* và test trực tiếp của module.
Được cập nhật CityCarryTests, CarryReadabilityTests, CarryVisualTests cho yêu cầu layout mới.
Không sửa terrain/shader Town*, Navigation*, ActorView, Worker/Customer, GameHud,
DevelopmentAssistance, GameSession, store hoặc giao dịch.
Giữ tên SKU, pool, animation transfer đã commit và sức mang; không chỉnh asset/rig nhân vật.

### Root

Sở hữu NPC/physics/animation nhân vật, UIFont/Billboard/HUD, persistence/performance,
QA runtime, scripts, docs tích hợp, build, ảnh, commit/push và đóng gói.

## Tự kiểm tra và bàn giao

- Không spawn agent khác. Tối đa hai agent hoạt động.
- Không chạy Unity/Player/compiler nặng khi root kiểm tra Player.
- Có thể kiểm tra source/diff, đọc installed package, tính bounds/quota bằng script nhẹ.
- Root chạy EditMode, build và Player sau khi hai module hoàn tất.
- Mỗi file dưới khoảng 300 dòng, mỗi statement riêng, khoảng 120 ký tự/dòng.
- Báo cáo một lần tối đa 15 dòng: file sửa, API mới, cách kiểm tra, vấn đề còn lại.
- Bằng chứng chốt phải cùng binary; ảnh fixture riêng với lượt FPS bình thường.

## Nguồn tham khảo

- https://docs.blender.org/manual/en/latest/render/shader_nodes/textures/noise.html
- https://docs.blender.org/manual/en/latest/render/shader_nodes/textures/voronoi.html
- Hai video và khung mẫu đã lưu trong work/city-expansion-20261008/references.
- Báo cáo Player vừa qua: ordinary-play-20261008T040600342Z, không dùng như kết quả bản mới.
