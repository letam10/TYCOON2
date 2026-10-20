# TYCOON2 — Handoff Phase 2: NavMesh, pedestrian, traffic và interaction

Ngày: 2026-10-20
Nhánh: `main`
Base trước Phase 2: `b7073c83321a283b36a80a336f09c74da70196a9`
Remote: `https://github.com/letam10/TYCOON2.git`

## Phạm vi đã xử lý

Phase này giữ nguyên transaction/save/camera và các stable ID hiện có. Thay đổi chỉ tập trung vào ranh giới di chuyển và tương tác:

- `CityPedestrianNetwork` kiểm tra trực tiếp cả đoạn ngắn từ điểm bắt đầu tới điểm đích khi quyết định route có thể đi bộ, tránh trường hợp chỉ đích hợp lệ nhưng đoạn tiếp cận bị chắn.
- `CityRoadRoute` cung cấp vòng giao thông canonical lấy từ các corridor street đã sample (`street-main-south`, `street-main-north`, `street-east-north`) và đặt lane offset trên cùng dữ liệu route.
- `CityTraffic` ưu tiên canonical sampled loop, đồng thời giữ fallback route legacy để không làm hỏng scene hoặc save cũ.
- `PlayerInteraction` có marker `ILogicalDwellInteractionArea`, dwell logic 0,25 giây, grace 0,08 giây khi actor vừa mất vùng và hỗ trợ marker-based dwell.
- `ProximityTarget` và `WorldInteractionArea` khai báo marker interaction contract mà không thay đổi stable ID hay transaction writer.

Contract cố định nằm tại `work/tycoon2-plan-20261020/phase2-navmesh-traffic/CONTRACT.md`. Contract xác định ownership chuyển động: player controller, NavMeshAgent/actor physical motion, CityTraffic và carrier không được nhân đôi `dt` hoặc tự ghi vào hệ thống giao dịch.

## File thuộc Phase 2

Runtime:

- `Assets/_Game/Scripts/Runtime/CityPedestrianNetwork.cs`
- `Assets/_Game/Scripts/Runtime/CityRoadRoute.cs`
- `Assets/_Game/Scripts/Runtime/CityTraffic.cs`
- `Assets/_Game/Scripts/Runtime/PlayerInteraction.cs`
- `Assets/_Game/Scripts/Runtime/ProximityTarget.cs`
- `Assets/_Game/Scripts/Runtime/WorldInteractionArea.cs`

Test:

- `Assets/_Game/Tests/EditMode/Phase2PedestrianNetworkTests.cs`
- `Assets/_Game/Tests/Editor/Phase2TrafficRouteTests.cs`
- `Assets/_Game/Tests/Editor/Phase2InteractionContractTests.cs`

Unity đã tạo `.meta` tương ứng cho ba test mới. Không stage hoặc sửa các file asset/meta khác đang tồn tại trong working tree.

## Kiểm chứng mới

### EditMode

Unity Test Framework report mới tại `QA/editmode-results.xml`:

- `result=Passed`
- `total=66`
- `passed=66`
- `failed=0`
- `skipped=0`
- duration `0.428097` giây

Các test Phase 2 xác nhận short-trip pedestrian segment, canonical traffic loop dùng street samples với lane offset, signal contract và logical dwell marker/grace.

### Windows Player build

`Tools/run_unity.ps1 -Task Build` tạo build tại `work/tycoon2-plan-20261020/phase2-navmesh-traffic/Player/TYCOON2.exe`.

Report mới `work/tycoon2-plan-20261020/phase2-navmesh-traffic/Player/build-report.json`:

- `result=Succeeded`
- `errors=0`
- `warnings=34`
- `bytes=117171807`
- built UTC `2026-10-20T12:26:26.1254836Z`

34 warning là warning đã tồn tại của API obsolete/shader; không có build error.

### Player smoke và runtime evidence

`Tools/run_world_features.ps1` chạy thành công với mã thoát 0 và log kết thúc bằng `WORLD-FEATURES_QA PASS`.

Report mới `work/tycoon2-plan-20261020/phase2-navmesh-traffic/world-features/world-features-report.json`:

- `passed=true`
- `checkCount=5928`
- `interactions=30`
- `runtimeErrors=0`
- `screenshots=78`
- `distanceWalked=300.4165954589844`
- GPU `NVIDIA GeForce RTX 4060 Laptop GPU`
- 11 expansion route checks có `PathComplete`

Log `world-features.log` không có dòng `Exception`, `Error`, `Assertion`, `Failed` hoặc `failure`.

Ảnh đã kiểm tra trực tiếp gồm route industrial, path owner ring, minimap 1920 và cutaway village. Chúng xác nhận scene render được, street/path ownership có hình học và UI/minimap hoạt động. Ảnh cutaway hiện chỉ là bằng chứng coverage/render hiện có; chưa được dùng để tuyên bố đã đạt fidelity interior cuối cùng.

## Ranh giới chấp nhận

Phase 2 hiện có bằng chứng source, EditMode, build và Player smoke. Các bằng chứng này chưa thay thế cho:

- native headed performance/frametime gate ở 1080p–1440p/60 FPS;
- kiểm tra dài hạn pedestrian/traffic trong nhiều junction và nhiều seed;
- audit toàn bộ camera/interior/asset provenance của các phase sau;
- full save migration audit sau khi những phase sau thay đổi schema.

Các giới hạn trên được giữ lại để tránh gộp nhầm focused smoke thành acceptance cho toàn bộ redesign.

## Trạng thái giao tiếp cho phase tiếp theo

Phase 3 cần tiếp tục từ stable IDs và route contract này để kiểm tra camera pitch/yaw, zoom/clipping/fade và interior/cutaway fidelity. Phase 4 kiểm tra asset provenance, biomes, water và performance budget. Không reset, clean, stash hoặc ghi đè các thay đổi ngoài allowlist của Phase 2.
