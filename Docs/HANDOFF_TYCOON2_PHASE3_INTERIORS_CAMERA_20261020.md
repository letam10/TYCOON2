# TYCOON2 — Handoff Phase 3: interiors, cutaway và camera

Ngày: 2026-10-20
Base Phase 3: `860662022f6582d426b82730d89a2a81d1ef8ba3`
Remote: `https://github.com/letam10/TYCOON2.git`

## Phạm vi đã triển khai

Phase này giữ nguyên stable POI ID, transaction/economy, save contract, route catalog và motion ownership của Phase 2. Thay đổi tập trung vào khả năng đọc công trình và chống camera xuyên vật thể.

- `WorldCutawayVolume` có danh sách interior renderer riêng, footprint theo local space, giới hạn chiều cao, truy vấn point containment, trạng thái player mong muốn và restore shell theo trạng thái ban đầu.
- `WorldCutawayDirector` đăng ký interior candidate cho landmark, tránh đăng ký volume trùng, dùng cùng footprint để lookup và cập nhật reveal state.
- `CityCameraFraming.ResolveDistance` xử lý blocking distance, padding, minimum clear distance và giá trị không hữu hạn.
- `FollowCamera` dùng sphere cast để rút camera trước vật cản, giữ nguyên pitch/yaw, tách collision mask và occlusion mask, ẩn renderer nằm giữa focus và camera khi cần và khôi phục trạng thái renderer sau khi tắt occlusion.

Contract giao tiếp nằm tại `work/tycoon2-plan-20261020/phase3-interiors-camera/CONTRACT.md`.

## File thuộc allowlist Phase 3

Runtime:

- `Assets/_Game/Scripts/Runtime/CityCameraFraming.cs`
- `Assets/_Game/Scripts/Runtime/FollowCamera.cs`
- `Assets/_Game/Scripts/Runtime/WorldCutawayDirector.cs`
- `Assets/_Game/Scripts/Runtime/WorldCutawayVolume.cs`

Test:

- `Assets/_Game/Tests/Editor/FollowCameraClippingTests.cs`
- `Assets/_Game/Tests/EditMode/Phase3CutawayContractTests.cs`
- `.meta` tương ứng do Unity tạo cho hai test mới

Các asset, meta và QA file khác trong working tree được giữ nguyên.

## Kiểm chứng EditMode

### Camera

Report mới nhất sau khi thêm `Physics.SyncTransforms()` cho fixture:

- test `Tycoon.Tests.FollowCameraClippingTests.SnapPullsCameraBeforeBuildingWithoutChangingPitchOrYaw`
- `result=Passed`
- `total=1`
- `passed=1`
- `failed=0`
- duration `0.1005202` giây

Test xác nhận camera rút trước blocker, giữ pitch/yaw, ẩn renderer qua `OcclusionMask` và khôi phục renderer khi tắt hide occluders.

### Cutaway/interior

Report mới nhất:

- `result=Passed`
- `total=3`
- `passed=3`
- `failed=0`
- `skipped=0`
- duration `0.0915188` giây

Ba test xác nhận shell bị ẩn nhưng interior còn lại, footprint/height filter và đăng ký interior candidate cho gameplay landmark.

## Windows build

Build tại `work/tycoon2-plan-20261020/phase3-interiors-camera/Player/TYCOON2.exe`.

`build-report.json` mới:

- `result=Succeeded`
- `errors=0`
- `warnings=34`
- `bytes=117174879`
- `builtUtc=2026-10-20T15:28:39.1652312Z`

Log có `Build Finished, Result: Success` và `Windows build succeeded`. Các warning là warning obsolete API/shader đã tồn tại; không có build error.

## Player smoke

`run_world_features.ps1` chạy trên đúng build Phase 3, kết thúc với mã thoát 0 và `WORLD-FEATURES_QA PASS`.

Report mới tại `work/tycoon2-plan-20261020/phase3-interiors-camera/world-features/world-features-report.json`:

- `passed=true`
- `checks=5928`
- `interactions=29`
- `screenshots=78`
- `runtimeErrors=0`
- `failure` rỗng
- GPU `NVIDIA GeForce RTX 4060 Laptop GPU`
- thời gian chạy UTC `2026-10-20T15:33:17.8006261Z` đến `2026-10-20T15:33:54.8198868Z`

Log runtime không có `Exception`, `Error`, `Assertion`, `Failed` hoặc `failure`.

## Ranh giới acceptance

Phase 3 có evidence source, EditMode, Windows build và Player smoke. Smoke hiện bao phủ world features, cutaway coverage, UI, map, water, save và runtime error scan; ảnh closeup interior gameplay chính, test đầy đủ cửa/phòng/collider từng building, headed frametime ở 1080p/1440p và performance target 60 FPS vẫn cần tranche riêng trong Phase 7.

Phase 4 tiếp tục asset provenance, biome catalog, water/shoreline và LOD. Phase 5 tiếp tục MapViewportState, filter, tooltip và HUD safe area. Phase 6 tiếp tục migration/save compatibility sau khi interior/map state được mở rộng.
