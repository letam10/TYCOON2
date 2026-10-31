# Quy ước triển khai — redesign sâu TYCOON2

Ngày 29/10/2026; baseline 3a3ad93. Đây là interface dự kiến để review trước khi viết runtime mới.
Không có subagent được tạo; root chịu trách nhiệm tích hợp, chạy Player và đánh giá hình ảnh.

## A. Phần được khóa

- FollowCamera: perspective, Pitch=55, Yaw=40, Distance=16, FocusOffset=(0.5,0.8,1.3),
  Damping=0.18, zoom hiện tại 12–27. Giữ cảm giác follow, input và tỷ lệ nhân vật trong khung hình.
- Model/rig/animation nhân vật hiện tại: không regenerate bằng ImageGen/Blender hoặc đổi skeleton.
- Farm logic: dùng implementation main làm chuẩn; snapshot Definitions, CarryLimits,
  PlayerInteraction, production/commerce và transaction trước thay đổi presentation.
  Phát hiện bug được ghi riêng và cần phân biệt với thay luật Farm.
- TransactionCore/GameplayTransactionStore tiếp tục là nơi duy nhất thay đổi hàng và tiền.
  HUD, model, animation, NPC và building presentation chỉ đọc projection/receipt.
- Các mã item, recipe, station, upgrade, owner, order và crew đang tồn tại được giữ nguyên.

## B. Dữ liệu world chung

WorldRouteCatalog vẫn là facade cho các consumer hiện có. Thêm data graph bên dưới thay vì
thêm hệ đường mới chạy song song với hệ cũ. Road/path/render/collider/NavMesh/map đọc một snapshot
world layout revision; không tự duy trì danh sách tọa độ riêng.

Dữ liệu cố định:
- RoadNode: id, worldPosition, elevationLayer, connectedEdgeIds.
- RoadEdge: id, from, to, sampledCenterline, roadWidth, sidewalkWidthLeft/Right, speedClass.
- Junction: id, incidentEdges, polygon, curbRuns, crossings, pedestrianLinks.
- BuildingDefinition: id, districtId, activityId, transform, footprint polygon, supportSurfaceId,
  entrance anchors, customer anchors, service anchors, shell groups, interiorPrefabId, access policy.
- ActivityDefinition: id, kind, buildingId, unlock predicate, input/output rules, command mapping.
- VegetationInstance: id, biomeId, seed, assetId, worldTransform, exclusion result, lodProfileId.
- AssetRecord: id, creator, sourceURL, licenseURL, license text path, revision/hash,
  derivative paths, importScale, pivot, material policy, collider policy, LOD policy, AI flag.

ID mới có namespace `town.v2.*`; ID nghiệp vụ cũ không đổi. Graph và registry sinh ổn định bằng seed
và thứ tự ordinal. Không đổi ID do vị trí, mesh hoặc material thay đổi.

## C. Module và interface dự kiến

| File/nhóm | Trách nhiệm và chữ ký chính |
|---|---|
| WorldRouteGraph.cs | Build(WorldLayoutDefinition): WorldRouteGraph; TryGetEdge(id, out RoadEdge) |
| WorldStreetGeometry/WorldStreetMesh | Build junction + edge surfaces từ graph; trả render/collision cùng vertices |
| WorldPathSurfaceGeometry/Mesh | Footpath/sidewalk clipping; dùng junction boundaries chung |
| WorldGrounding.cs | TryResolveSupport(footprint, pivot, supportId, out PlacementResult) |
| TownBuildingCatalog.cs | Definitions; Validate(WorldLayoutDefinition, out failures) |
| TownBuildingBuilder.cs | Build(BuildingDefinition, parent): TownBuildingRuntime |
| TownBuildingRuntime.cs | Entry/Exit/Activity anchors; Interior; ShellGroups; AccessState |
| WorldCutawayVolume/Director | Register groups tường/mái tường minh; RequestReveal(playerBounds, cameraRay) |
| WorldVisibilityState.cs | Claim(renderer, ownerId); Release(renderer, ownerId); một writer enabled-state |
| TownActivityCatalog.cs | Định nghĩa riêng hoạt động, mở khóa, command handler; không giữ mutable cash/inventory |
| TownActivityInteraction.cs | TryInteract(context, delta): InteractionResult; gửi command qua transaction |
| TransactionTownActivities.cs | Validate/Apply(command,state): receipt; dùng chung revision/dedup/checkpoint |
| CityVegetation/WorldVegetationCatalog | Instantiate mesh thực theo biome profile, spacing, exclusions, LOD |
| CityWaterSurface + TownWater.shader | Mesh bờ/độ sâu và shader nước; baseline cao độ thống nhất |
| MapViewportState/CityMapGraphic | Cùng transform cho fill/road/icon/hit-test; bounds, zoom, filter |
| GameHudMap/GameHudLayout | Layout và input/tooltip; không tính state kinh tế riêng |
| Tools/asset_import_* | Nhập có chọn lọc, manifest, LOD/material/prefab; không copy nguyên kho download |
| Tests/Editor/*Redesign* | Tests hành vi, integration và regression từ lỗi tái hiện |

Tên trên là contract để triển khai; không tạo file rỗng để làm checklist trông hoàn thành.
Nếu module hiện có đáp ứng đúng trách nhiệm thì sửa module đó, không thêm wrapper dùng một lần.

## D. Hình học và navigation

1. Unit Unity = meter; lấy collider/mesh bound thật của nhân vật và cargo để tính clearance.
2. Khoảng rộng tối thiểu hành lang = 2 × capsuleRadius + 2 × skinWidth + clearance đã kiểm.
   Lối hai chiều dùng tổng hai capsule và biên tránh nhau; kiểm cả hàng đang mang nếu có collider.
3. Offset edge từ centerline và pháp tuyến ngang. Tại junction trim edge ở polygon chung;
   hợp nhất road polygon, vỉa hè chỉ chạy bên ngoài road boundary, không vắt ngang nút giao.
4. Đường cong dùng bán kính phù hợp corridor. Bước góc từ sai số dây cung:
   deltaAngle = 2 × acos(1 - epsilon / radius), với 0 < epsilon < radius.
   Sample theo chiều dài cung; collider/map/NPC đọc cùng samples, không tự nội suy khác.
5. Mặt đỡ xác định theo supportSurfaceId. Offset pivot từ transformed mesh bounds để đáy chạm sàn;
   nhiều điểm support phát hiện nhà nghiêng/lơ lửng, không ép mọi object về y=0.
6. NavMesh bake sau placement/collider; kiểm Complete path tới approach, cửa, station và exit.
   NPC/path smoothing không rời corridor; ưu tiên quầy/khách và hậu cần ở hai phía khác nhau.
7. Nhà không vượt bờ/corridor; nếu footprint giao nhau thì reject layout thay vì phủ trang trí lên.

## E. Nội thất/cutaway

Prefab phải tách Roof, OccludingWalls, Structure, Interior, Collision, Doors, InteractionAnchors.
Không suy nội thất bằng renderer thấp hơn 3,1m. Chỉ roof/tường đang che player được ẩn/fade;
interior vẫn sáng và có collider. Không SetActive(false) parent chứa collision/interaction.
FollowCamera và cutaway không được cùng phục hồi renderer mỗi frame; có claim owner rõ ràng.
Exit, teleport, reload, disable/destroy building phải trả visibility về trạng thái gốc.

Góc camera giữ nguyên. Dựa player/cargo bounds và ray/capsule visibility thực tế để xác định tường
che; test ở cửa, góc phòng, sát tường, sau quầy, mang stack lớn và lúc NPC đi qua.

## F. Hoạt động bổ sung

Đề xuất activity kinds: residential_delivery, cafe_supply, lodge_supply, harbor_dispatch,
warehouse_transfer, bank_service, business_management. Các hoạt động trồng/chăn nuôi/bán Farm
vẫn gọi handler cũ. Khách giao hàng mới dùng order ownership, reservation và payment giống contract
hiện có, nhưng ID và hàng chờ riêng, không chèn làm thay đổi queue Farm.

ActivityState: id, buildingId, orderId/ownerId, status, progress, revision, consumedReceiptIds.
Không tạo tiền trực tiếp bằng cách bấm décor; món đã giao có chủ sở hữu và kết quả terminal.
Tính tick/speed chỉ tại simulation owner; presentation chỉ dùng dt để animate.

## G. Save và kiểm chứng

Profile mới đề xuất `coastal-redesign-v2`. Đọc profile này khi chạy bản mới; không tự migrate hoặc
nạp redesign-v1/legacy. Layout revision mới lưu buildingId/entryId và world position; nếu vị trí
không hợp lệ sau layout update, fallback tới entry an toàn cùng district và báo rõ, giữ hàng/tiền.

Mỗi gate ghi: commit, timestamp UTC, profile, executable/runtime/content hashes, screen size,
GPU, actor count, simulation speed, command, observed outcome, failure reason, evidence paths.
Một ảnh đẹp hoặc 1208 tests pass không thay thế vào cửa/thao tác/save/restart thật.
Không thay threshold/test fixture để biến lỗi runtime thành pass; sửa expectation chỉ khi chứng minh
expectation trái với contract đã chốt và phải giữ test cho hành vi người chơi thực tế.
