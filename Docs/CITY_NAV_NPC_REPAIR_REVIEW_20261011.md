# Rà soát route Player và animation NPC — 11/10/2026

## Phạm vi và run được dùng

Đây là rà soát read-only theo `Docs/CITY_REPAIR_CONTRACT_20261011.md`. Run có thẩm quyền là
`work/city-expansion-20261008/ordinary-play-20261010T160928100Z`. Không chạy Unity/Player,
không sửa runtime/test/build và không chạy benchmark 165 FPS.

## Kết luận ngắn

- **Player timeout là lỗi đích đến va chạm/độ khả dụng của route**, không phải thiếu NavMesh path.
  377 điểm NavMesh pass và `blocked: []`, nhưng điểm cuối `(206, 0, -123)` nằm trong hình hộp
  `PedestrianLinkCollision` (xấp xỉ 198.975–206.025, z -124.3–-121.7). Player dừng ở
  `(205.868, 0.075, -122.646)`, cách đích khoảng 0.38 m, đúng dạng capsule không thể tiến vào
  vùng collider.
- **Deadline 95 s làm route dài sát/qua giới hạn, nhưng không phải cách sửa gốc.** Tổng chiều dài
  12 đoạn path là khoảng **481.55 m**. Ở tốc độ không mang hàng 6.2 m/s, thời gian lý tưởng là
  77.67 s; ở 3.8 m/s khi mang hàng là 126.72 s. Input từng frame, quay hướng và clearance làm
  thời gian thực dài hơn. Tăng deadline sẽ vẫn không làm một đích nằm trong collider reachable.
- **0/84 animated NPC chưa chứng minh gameplay animation hỏng.** Run được khởi động với
  `WindowStyle Hidden`; cả 17 ảnh đều 1920×1080 nhưng đen đồng nhất (`maximumChannel: 0`).
  `ActorView.Initialize` đặt `Animator.cullingMode = CullUpdateTransforms`; `CityActorEvidence`
  chỉ đo thay đổi `localRotation` của bone có tên `Leg`/`Foot` khi sample 1 Hz. Khi actor bị
  renderer cull hoặc không có frame hiển thị, phép đo xương có thể luôn bằng 0 dù agent vẫn đi.
  Artifact lại ghi 84 NPC đi được và tổng quãng đường 75,399.67 m, phù hợp với lỗi đo/render
  evidence hơn là mọi NPC đều đứng yên về chân.

## Bằng chứng chi tiết

| Claim | Evidence |
|---|---|
| Route có path hoàn chỉnh | `blocked-walk.json`: 13 corners, path status được tạo từ `CalculatePath`; `city-navigation.json`: `checkedPoints=377`, `blocked=[]`. |
| Route bị kẹt ở đích | `blocked-walk.json`: `position=(205.868,0.075,-122.646)`, `destination=(206,0,-123)`, obstacle `PedestrianLinkCollision` center `(202.5,0.01,-123)`, size `(7.05,0.08,2.6)`. |
| Timeout do deadline trong code | `Assets/_Game/Scripts/Runtime/CityPlayMovement.cs`: `deadline = Time.realtimeSinceStartup + 95`; timeout lưu target, corner và path rồi ném `InvalidOperationException`. |
| NPC thực sự di chuyển | `npc-motion-evidence.json`: `observedNpcs=94`, `walkedNpcs=84`, `totalNpcDistance=75399.6689`, `animatedNpcs=0`. |
| Animation metric phụ thuộc xương transform | `Assets/_Game/Scripts/Runtime/CityActorEvidence.cs`: sample mỗi giây; chỉ cộng `legMotion` khi state Walk/Run/CarryWalk và so sánh `localRotation`. |
| Culling/capture limitation có thật | `Assets/_Game/Scripts/Runtime/ActorView.cs`: `CullUpdateTransforms`; `capture-validity.json`: 17/17 ảnh black/uniform; `Player.log`: Player hidden, D3D11 RTX 4060, failure chỉ ở NPC motion assertion. |
| Simulation vẫn chạy lâu và nhanh | `ordinary-play-report.json`: 570.429 s, 178,726 frames, 313.32 average FPS, 2,289.82 m Player walk; đây không phải bằng chứng animation render đúng. |

## Sửa hẹp, trung thực

1. **Navigation:** sửa dữ liệu/đích approach của landmark tại `(206,0,-123)` để nằm ngoài
   `PedestrianLinkCollision` với clearance cho CharacterController, hoặc sửa collider/navmesh
   ownership để đích là vùng đứng hợp lệ. Giữ `Walk` kiểm tra `PathComplete` và ngưỡng `.22`.
   Chỉ tăng 95 s sau khi đích đã reachable, nếu bounded run chứng minh phần thời gian còn lại là
   do route length hợp lệ; không dùng deadline lớn hơn để che collider.
2. **NPC animation evidence:** trước hết chạy lại một bounded Player với cửa sổ hiển thị và ảnh
   nonblank. Nếu animation cần được chứng minh trong QA khi actor ngoài camera, dùng override QA
   hẹp (`AlwaysAnimate`) hoặc metric state/normalized-time riêng cho evidence; không đổi culling
   gameplay toàn cục chỉ vì run hidden cho `legMotion=0`. Nếu run visible vẫn cho 0, khi đó mới
   điều tra Animator controller/state names và bone binding như lỗi gameplay thật.

## Kiểm chứng bắt buộc sau khi sửa

- Bounded route case tới cùng landmark: assert `PathComplete`, final distance ≤ `.22 m`, không có
  `blocked-walk.json` timeout, và capture approach nonblank.
- Bounded visible Player case: giữ 1 Hz actor sample nhưng phải có nonblank capture; xác nhận ít
  nhất 8 NPC `walkedNpcs` và 8 `animatedNpcs`, đồng thời ghi rõ culling mode/visibility. Không
  gọi kết quả hidden/black là animation proof.
- Chạy compile/EditMode và bounded correctness tour theo integration contract; tách riêng các
  kết quả đó khỏi visual acceptance. Không rerun stable-165 benchmark.

## Giới hạn kết luận

Run hiện tại chứng minh route timeout và evidence không đạt; nó **chưa** chứng minh controller
animation hỏng trong một Player có render hiển thị. Cũng chưa có bằng chứng mới sau khi đích,
collider hoặc capture path được sửa.
