# TYCOON2 Phase 3 contract — interiors, cutaway và camera

Phase 3 chỉ xử lý khả năng đọc công trình và camera quanh công trình. Không thay đổi stable POI ID, `TransactionCore`, `SaveData`, schema save, route catalog hoặc ownership chuyển động của Phase 2.

## Contract nguồn

- `FollowCamera` là owner duy nhất của pitch, yaw, distance, focus và vị trí camera.
- `CityCameraFraming` chỉ tính khoảng overview từ bounds/lens; không được ghi trực tiếp vào camera transform.
- `WorldCutawayVolume` là owner duy nhất của trạng thái shell/interior reveal và phải khôi phục đúng trạng thái renderer/object ban đầu.
- `WorldCutawayDirector` là owner duy nhất của việc chọn volume theo player position; không tạo trigger logic thứ hai.
- Công trình gameplay chính phải có interior renderer/prop candidate, cửa vào và collision perimeter rõ; công trình nền chỉ yêu cầu lightweight shell cutaway.
- Camera không được tự xoay khi bổ sung clipping/fade; pitch/yaw hiện tại phải giữ nguyên khi zoom hoặc vào công trình.

## Worker partitions

### Worker A — camera

Được sửa: `Assets/_Game/Scripts/Runtime/FollowCamera.cs`, `Assets/_Game/Scripts/Runtime/CityCameraFraming.cs`, một test Editor camera mới trong `Assets/_Game/Tests/Editor/`.

Mục tiêu: giới hạn zoom hợp lý, tránh clipping gần ground/building, fade/ẩn vật cản theo ray từ camera tới focus khi có thể, giữ pitch/yaw, và test invariant không xoay camera do clipping.

### Worker B — interior/cutaway

Được sửa: `Assets/_Game/Scripts/Runtime/WorldCutawayVolume.cs`, `Assets/_Game/Scripts/Runtime/WorldCutawayDirector.cs`, một test EditMode mới trong `Assets/_Game/Tests/EditMode/`.

Mục tiêu: bảo đảm shell/interior state restore, footprint và trigger/filter không chồng logic, phân biệt shell trên với interior renderer, và có contract cho gameplay building coverage.

## Quy tắc chung

- Không chạy Unity, build, Player, browser hoặc server trong worker.
- Không tạo agent con, không commit, không push.
- Mỗi worker chỉ sửa file thuộc partition và test của chính mình.
- Root sẽ đọc diff, tích hợp, chạy `node`/Unity gates phù hợp, kiểm tra ảnh runtime, rồi commit/push bằng allowlist.
- Nếu runtime evidence chưa chứng minh clipping, interior fidelity hoặc performance, handoff phải ghi rõ là chưa đạt.
