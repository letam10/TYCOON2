# Legacy Docs archive — 2026-10-11

This file is a verbatim consolidation of historical contract/findings documents.
Original root files were removed only after this archive was written and verified.

---

## SOURCE: FPS165_REVIEW_CONTRACT_20261008.md

# Hợp đồng rà soát FPS 165 Hz — 08/10/2026

## Phạm vi

- Root tích hợp model cà rốt và cài đặt, chạy Unity/Windows Player, ghi nghiệm thu và đóng gói.
- Agent rà soát chỉ đọc source/evidence, không chạy game hoặc Unity và không sửa code.
- Agent được tạo duy nhất `work/carrot-settings-20261008/fps-source-review.md`.
- Không tạo agent con; không ghi settings, save, journal, PlayerPrefs hoặc tài nguyên URP.

## Dữ liệu/API giữ nguyên

- Dùng `Docs/CARROT_SETTINGS_CONTRACTS_20261008.md` làm hợp đồng cài đặt và FPS.
- `GameSettingsStore.Current`: language, quality, showFps, syncDisplay, fpsLimit, renderScale,
  fullscreen, resolutionWidth, resolutionHeight. Các ID tiến trình chơi không thay đổi.
- VSync bật: `QualitySettings.vSyncCount = 1`, `Application.targetFrameRate = -1`.
- VSync tắt: vSyncCount 0, targetFrameRate lấy từ fpsLimit (mặc định 165).
- Máy thực tế: RTX 4060 Laptop, màn hình 2560×1600, 165 Hz; Player QA dùng 1920×1080.
- Không suy ra FPS đo từ giới hạn FPS; giữ frame time thật, P95/P99, worst và số hitch.
- Source đo chơi thường: CityPlaySession, CityPlayReport, CityPlayResume, CityPlayTour,
  CityOperationTiming, CityNativeCpuProbe, FpsOverlay; không sửa các file này.
- Evidence cũ chỉ là lịch sử: ordinary-play-20261008T071925547Z và
  ordinary-play-20261008T072739868Z trong work/city-expansion-20261008.
- Build đang thực hiện: work/carrot-settings-20261008/Release/TYCOON2.exe.

## Câu hỏi cần kiểm tra

1. FPS overlay có đo đúng thời gian unscaled, mức làm mượt và chi phí khi bật/tắt không?
2. Harness chơi thường có ép cấu hình khác cài đặt mặc định hoặc làm sai số đo không?
3. Frame spike khi mod tối đa được bằng chứng CPU/GPU nào giải thích, còn thiếu gì?
4. Đề xuất tối đa ba sửa đổi nhỏ nếu có bằng chứng source/evidence rõ ràng.

## Hoàn thành

- Báo cáo ≤120 dòng, chỉ rõ đường dẫn/line và phân biệt phát hiện chắc chắn với giả thuyết.
- Không cam kết 165 FPS; không thêm benchmark hoặc chạy encoder trong lúc root đo.
- Tự kiểm: file báo cáo tồn tại; không có file khác do agent thay đổi.
- Báo root đúng một lần, tối đa 15 dòng: file tạo, cách kiểm, lỗi thực tế/chưa rõ.


---

## SOURCE: FPS165_HITCH_REVIEW_20261009.md

# Maximum-mod hitch review — 2026-10-09

## Measured evidence

- Unprofiled report: `work/city-expansion-20261008/ordinary-play-20261009T001859194Z/`
  reports worst frame 756.459 ms, average 160.13 FPS, P95 6.98 ms, P99 9.22 ms over 269 s.
  Its hitch CSV records frame 15946; operation timings record `ModMaximum.Transaction`
  8.273 ms and `SaveGame.Checkpoint` 12.424 ms at that frame. First `Workers.BuildPending`
  is 21.098 ms at frame 15947.
- Profiled report: `work/city-expansion-20261008/ordinary-play-20261009T002536326Z/`
  reports worst frame 777.757 ms, average 126.71 FPS, P95 13.87 ms, P99 21.46 ms over
  269.6 s. Its hitch CSV records frame 15878; timings record `ModMaximum.Transaction`
  5.272 ms and `SaveGame.Checkpoint` 14.300 ms at that frame. First worker build is
  22.925 ms.
- The operation and hitch CSVs share frame numbers, but their `second` values differ by
  about 0.8 s at these entries. This associates the spike with the max-mod flow without
  proving which operation caused it. The logged transaction, save, and worker build times
  do not add up to either 756 or 778 ms.
- The profiled native CPU report records maximum `PlayerLoop` 241.643 ms and
  `Update.ScriptRunBehaviourUpdate` 237.994 ms. Its CPU report records `WorkerAgent.Update`
  94.637 ms, `WorkerTransport.Transport` 74.379 ms, and `GameSession.Update` 31.219 ms.
  These instrumented maxima point to main-thread work worth tracing, but do not attribute
  the 777.757 ms hitch. The profile run is not a clean FPS acceptance sample. The
  unprofiled max has no CPU profile.

## Current synchronous path (source inference)

- `GameHudModMenu.ApplyMod` closes the menu, calls `ApplyMaximum`, then saves and refreshes
  HUD/status (`GameHudModMenu.cs:81-99`). `ApplyMaximum` synchronously executes the grant
  and dispatches visual refresh (`DevelopmentAssistance.cs:20-33`).
- `GrantMaximum` walks upgrades and crews, completes progression, creates missing crews,
  ensures a truck, unlocks crops, and refreshes progression
  (`PhysicalCashTransactions.cs:38-76`). After a changed revision,
  `RuntimeTransactions.Refresh` synchronously takes a state snapshot and projects bound
  inventories (`RuntimeTransactionBindings.cs:130-142`). Command journaling writes and
  flushes before publishing (`GameplayTransactionStore.cs:52-65`); it is a remaining
  synchronous disk operation, though the measured transaction is 5.272/8.273 ms.
- The save captures checkpoint state and projects world data on the caller before the
  normal background write starts (`RuntimeTransactionBindings.cs:121-127`,
  `GameplayTransactionStore.cs:73-83`). Checkpoint timings at the mod frame are
  12.424/14.300 ms; ordinary entries elsewhere reach about 31 ms. Checkpoint read,
  recovery, or quit can wait for the writer (`CheckpointWriter.cs:39-51`,
  `GameplayTransactionStore.cs:37-49,97-100`), but no wait is evidenced on the mod path.
- Worker sync retires excess staff and constructs replacements on the main thread. It
  limits creation to two workers per frame, but each build still creates a GameObject,
  samples NavMesh, creates and initializes a model and agent, binds transaction state, and
  attaches an upgrade view (`GameSessionWorkers.cs:35-96`). First worker-build operations
  near the spikes measured 21.098/22.925 ms, far below the hitch. Deferred
  `RefreshAll(..., defer:true)` queues models (`UpgradeModelView.cs:22-38,79-96`); queue
  drain processes at most two views or about 2 ms per `LateUpdate`
  (`UpgradeVisualQueue.cs:17-30`). This removes bulk model construction from the button
  call, while individual worker setup and queued view builds remain synchronous.

## Assessment and next action

The old captures show a severe hitch near max-mod activation, but do not isolate its cause.
They do not verify the current deferred-refresh implementation, so they cannot show whether
it fixed the hitch. The source still has synchronous journal flush, transaction and save
projection, worker initialization, and bounded visual construction. Logged transaction,
checkpoint, and first worker-build durations make those measured sections alone unlikely
to explain a 0.75 s pause. Worker initialization and broader update-loop work remain risks,
not proven causes.

These captures alone do not warrant another code change. If a fresh ordinary unprofiled run
still records a long frame on max-mod, time command execution, transaction refresh,
checkpoint projection, and each worker-build substep. If the hitch tracks worker creation,
reduce the two-worker budget or split initialization across frames, then remeasure. Do not
claim the hitch is fixed until a run on the deferred-refresh source records the result.

## References

- Contracts: `Docs/MODEL_MULTIVIEW_DINING_CONTRACT_20261009.md` final performance section;
  `Docs/FPS165_REVIEW_CONTRACT_20261008.md`.
- Evidence: both runs' `ordinary-play-report.json`, `ordinary-play-hitches.csv`, and
  `operation-timings.csv` under `work/city-expansion-20261008/ordinary-play-20261009T001859194Z/`
  and `work/city-expansion-20261008/ordinary-play-20261009T002536326Z/`. Profile details
  are in the latter run's `ordinary-play-cpu.json` and `ordinary-play-native-cpu.json`.


---

## SOURCE: V10_FRAME_PACING_CONTRACT.md

# V10 frame pacing investigation

Workspace: D:/GAME/TYCOON2. Unity 6000.6.3f1, URP 17.6, Windows RTX 4060 Laptop.
Monitor verified 2560 x 1600 at 165 Hz. Player runs 1920 x 1080 windowed.
Latest binary: work/city-model-v10-20261009/Release/TYCOON2.exe.
Latest visible profile: work/city-expansion-20261008/ordinary-play-20261009T134351129Z.
VSync true, limit setting 165, actual average 213 FPS, log says vsync broken / refreshTimeDrift.
Project supports Direct3D11 only. Native BuildWindows does not configure project.
GameSettingsStore uses vSyncCount 1 with targetFrameRate -1 when syncDisplay is true.
Manual limit 165 uses Unity software timing. Hidden-window captures were black and cannot be FPS acceptance.
Root owns runtime performance changes, build, tests and evidence. Do not run a Player or Unity.

## Investigator ownership

Create/edit only Docs/V10_FRAME_PACING_FINDINGS.md, under 300 lines.
Read relevant source, current logs and official Unity/graphics documentation.
Recommend smallest evidence-based fix or controlled experiment; no implementation changes.
Distinguish driver/display overrides from engine settings and rendering throughput from present pacing.
Do not change Windows, NVIDIA, display settings, saves, project source or historical evidence.
No subagents. Report once, at most 15 lines, with sources, concrete findings and remaining uncertainty.
Self-check source paths and links, and git diff --check for owned document.


---

## SOURCE: V10_SETTLED_MOTION_CONTRACT.md

# V10 settled upgrade motion contract

## Scope

- Unity 6000.6.3f1, URP 17.6, namespace Tycoon.
- Owner may edit only UpgradeVisualMotion.cs and create UpgradeVisualMotionTests.cs plus its meta.
- Runtime path: Assets/_Game/Scripts/Runtime/UpgradeVisualMotion.cs.
- Test path: Assets/_Game/Tests/Editor/UpgradeVisualMotionTests.cs.
- Do not spawn children, change public APIs, run Unity, or edit other files.

## Behavior to preserve

- Initialize(GameSession session, bool animate, int speedLevel) signature stays unchanged.
- Construction lift and scale settle retain current timing and final transforms.
- MotorRotor and SprinklerHead rotate continuously, including after settling.
- No animation while GameSession.CanSimulate is false.
- Static pieces stop receiving transform writes after settling.
- Reinitialize must reset hasMovingParts rather than keep a previous rotor state.
- Avoid repeated calculations or transform assignments for static pieces in the settled phase.
- Keep model, material, child order, names and transaction behavior unchanged.

## Verification

- Root runs Tools/run_unity.ps1 -Task Tests after merging independent work.
- Add meaningful tests for construction final pose, settled rotor motion and reinitialize.
- If reflection is needed to exercise private Update or ApplyPose, confine it to tests.
- Source files under about 300 lines and 120 columns, one statement per line.
- Self-check syntax and git diff --check; report once, at most 15 lines.


---

## SOURCE: V10_TRAFFIC_GRIDLOCK_CONTRACT.md

# V10 road traffic gridlock fix

## Shared facts and interfaces

- Unity 6000.6.3f1, URP 17.6, checkout D:\GAME\TYCOON2.
- CityTraffic.BlocksPublicTransport(Vector3 point, Vector3 forward, float step): bool.
- CityPublicTransport.BlocksTraffic(Vector3 point, Vector3 forward, float step): bool.
- CityTrafficSafety.ActorAhead and CarAhead are existing pure safety functions.
- Vehicle routes use CityRoadRoute.Round and both directions around the town.
- Preserve all existing public signatures and real boarding/alighting behavior.
- Preserve pedestrian collision protection and separation between vehicles.
- Failed live gallery: work/items-vehicles-20261009/player-20261009T150142691Z.
- vehicle-rides-state.txt and vehicle-rides-facts.json contain actual runtime evidence.
- Sedan remained at (-106.52,0,2.41), facing -Z, never boarded or alighted.
- Vehicles queued on west road; coach nose ray hit CityVehicle_Tanker at southwest corner.
- Two directions and public transport can reciprocally stop at a rounded corner.
- Diagnose the actual blocking rule before selecting the smallest fix.

## Ownership

- Agent may edit CityTraffic.cs, CityTrafficSafety.cs, CityPublicTransport.cs,
  CityPublicTransportVehicle.cs and a new CityTrafficGridlockTests.cs plus its meta.
- Agent may create Docs/V10_TRAFFIC_GRIDLOCK_FINDINGS.md.
- Root owns all other files, builds and Player runs.
- Do not launch Unity or Player concurrently with the root shadow reproduction.
- Do not weaken ride acceptance, increase timeout, teleport vehicles or force passengers.
- Do not spawn agents, edit saves, alter staff numbers or change unrelated gameplay.
- Keep each new file below about 300 lines and preserve existing API signatures.
- Add meaningful tests for the proven defect and both travel directions.
- Self check source syntax and git diff --check; root will run Unity tests and live rides.
- Report once in at most 15 lines: changed files, checks, concrete limitations.


---

## SOURCE: V10_TRAFFIC_GRIDLOCK_FINDINGS.md

# V10 traffic corner gridlock findings

## Observed failure

- Original Player evidence: `work/items-vehicles-20261009/player-20261009T150142691Z`.
- The west-road queue included a coach whose nose cast hit the tanker at the southwest corner.
- The sedan remained at approximately `(-106.52, 0, 2.41)` without completing its ride.
- Straight-ahead center tests can classify vehicles in both directions as blockers during a rounded turn.
- The unfinished corner rule also stopped both vehicles whenever both were inside its 12 m square.
- The unfinished same-lane rule handled ordering but its 9 m gap did not separate long hulls during turns.

## Changes

- Traffic and transit use the same projected loop progress to identify a same-direction leader.
- Both travel directions use the same ordering, with loop wrap handled before checking the following gap.
- The gap is 12 m plus the next movement step, covering the extra space needed while long hulls rotate.
- Opposing traffic reserves a 15 m corner region before entering.
- If two vehicles already occupy that region, the vehicle farther through the turn proceeds.
- Equal progress gives the outer lane priority, so the decision cannot reciprocally stop both vehicles.
- Transit now checks other transit vehicles through the same rule as ordinary traffic.
- Other vehicles use their actual hull orientation when identifying travel direction at waypoint boundaries.
- Existing pedestrian checks, collider casts, public safety signatures, passenger flow and saves are preserved.

## Checks performed without launching Unity

- `git diff --check` passed for the owned runtime files, tests and this note.
- All owned source files remain under 300 lines, with source lines below 120 characters.
- Compiled the actual safety, route and test source in memory with C# `Add-Type` and math/NUnit shims.
- The source math harness passed 35 cases: the existing traffic tests plus the new gridlock tests.
- New cases cover mixed followers, both arrival orders at all four corners, stable interior priority,
  equal-progress priority, opposite straight lanes, the recorded corner and long same-lane hull separation.
- Hull checks use separating axes for full 8.6 by 2.45 m rectangles and retain rotation lag at 140 degrees/s.
- A separate 32-scenario corner motion model covered arrival offsets of 0, 3, 6 and 9 m in both orders.
- The 12 m corner region failed separation, reaching approximately -0.423 m; 15 m passed.
- With 15 m clearance and rotation lag at 7 m/s, the model minimum hull separation was 0.65 m.

## Remaining acceptance

- The source harness validates the actual decision code with substitutes for Unity math and NUnit.
  It does not establish Unity compilation, Player behavior, physics casts or successful live passenger rides.
- Root must run focused and full Unity tests, rebuild the Player and repeat the unchanged real ride acceptance.
- Confirm both directions keep moving, no long hulls overlap, and boarding/alighting succeed within the existing timeout.
- No Unity or Player process was started by this agent. No acceptance criteria, save or timeout was changed.


---

## SOURCE: V10_VISUAL_REVIEW_CONTRACT.md

# Hợp đồng kiểm hình v10

- Dự án Unity tại D:/GAME/TYCOON2, chỉ kiểm ảnh Player thực tế; không chỉnh source hay khởi chạy Player.
- Ảnh hiện tại: work/items-vehicles-20261009/player-20261009T124757000Z.
- Agent goods chỉ tạo Docs/Art/V10_GOODS_REVIEW.md; root kiểm phương tiện, máy và các khu thành phố.
- Đủ 27 IDs trong Definitions.Items; mỗi ID có item-v8-ID-1/24/48.png,
  item-single-ID-ANGLE.png và item-angles-ID-48-ANGLE.png.
- Góc: front/right/rear/left/three-quarter/top/rear-quarter/front-left/rear-right.
- Chỉ egg/milk/bottled_milk/sauce/soy_sauce dùng khay khi cầm. Một món phải hiện đúng một món.
- Thịt/bánh/phô mai/vải/ngô/cà rốt/lúa/len và hàng khác cầm trực tiếp, 2x3 theo tầng.
- Kiểm hình nhiều góc, hướng model, giao hình, điểm đỡ, nét vật liệu và bố cục gọn.
- Không sửa hoặc tạo ảnh model. Được tạo contact sheets bằng Tools/model_contact_sheet.py,
  ảnh gốc phải giữ nguyên; dùng view_image để thực sự nhìn, không suy ra từ tên file.
- Báo cáo ghi ảnh đã nhìn, phát hiện cụ thể với đường dẫn/góc; không tuyên bố đạt ảnh chưa có/chưa nhìn.
- Không spawn subagent. Không chạy Unity/Player, không commit/publish hoặc đọc/sửa save.
- Hoàn thành toàn bộ lượt kiểm, ghi báo cáo và chỉ gửi kết quả một lần tối đa 15 dòng.


---

## SOURCE: V10_PLACEMENT_REVIEW_CONTRACT.md

# V10 placement review ownership

Workspace D:/GAME/TYCOON2, Unity 6000.6.3f1 procedural game models.
Evidence: work/items-vehicles-20261009/player-20261009T124757000Z.
Review unchanged world props and furniture from all isolated and five placed views.
Exclude goods, vehicles, industrial machines and Beach/Harbor water: those are separately reviewed by root.
Include safe, storage, shelf, checkout and all six diner tables; include remaining static city landmarks.
Use original PNGs or Tools/model_contact_sheet.py to inspect every required view, not just directory counts.
Do not claim a runtime placement is valid solely from bounds/tests.
Create/edit only Docs/Art/V10_PLACEMENT_REVIEW.md and scratch contact sheets under work/v10-placement-review.
Do not edit models, existing docs, tools, saves or historical PNGs. No Unity/Player launch, no subagents.
Record exact owners/views inspected and concrete defects if visible; distinguish possible occlusion from bad geometry.
Do not fabricate completion or list defects without image evidence.
Self-check git diff --check -- Docs/Art/V10_PLACEMENT_REVIEW.md, and report once at most 15 lines.


---

## SOURCE: V10_SHADOW_REVIEW_CONTRACT.md

# Shadow artifact review contract

## Facts

- Checkout D:\GAME\TYCOON2, Unity 6000.6.3f1, URP 17.6, D3D11 RTX 4060 Laptop.
- User sees disappearing, jagged and overlapping shadows when near or far from objects.
- PC pipeline uses Forward+, GPU Resident Drawer, GPU camera occlusion, SSAO and soft shadows.
- Balanced quality sets two cascades, 2048 atlas and shadow distance 45.
- FollowCamera normal zoom is 12 through 27; gallery uses larger distances and teleports.
- Main custom shaders are TownSurface.shader and TownWater.shader.
- Final ordinary run T144927602Z passed functionally but averaged 146.517 FPS.

## Reviewer ownership

- Read runtime lighting, scene setup, capture logic, shaders and local URP package sources.
- May create Docs/V10_SHADOW_FINDINGS.md only.
- No source edits, Unity/Player launches, child agents, driver changes or save changes.
- Identify proven defects separately from hypotheses and intended distance/cascade behavior.
- Check duplicate active lights, double camera renders, shadow coordinate usage, caster passes,
  receiver normals, SSAO placement and shader keywords.
- Existing gallery work/items-vehicles-20261009/player-20261009T150142691Z is still running.
- Root creates a dedicated camera-motion reproduction and owns all implementation changes.
- Report once, at most 15 lines with evidence, exact source locations and recommended small fixes.
- AGENTS.md rules apply. Use primary documentation or package source for technical claims.


---

## SOURCE: V10_FINAL_VISUAL_CONTRACT.md

# Final v10 visual review contract

## Shared facts

- Checkout D:\GAME\TYCOON2, Unity 6000.6.3f1, URP 17.6, Windows Player.
- Review actual final Player PNGs only, no concept image or modified source capture.
- Root supplies one exact gallery directory in the delegation brief after the gallery has passed.
- Each isolated model has eight horizontal views and a supplemental top view.
- Placed models have front, right, rear, left and top views.
- Source images remain unchanged. Contact sheets may be created in assigned work folders.
- Use functions view_image to inspect local images; report occluded faces as limitations.
- Do not spawn children, run Unity/Player, change source/models or touch real saves.

## Vehicle reviewer ownership

- May create Docs/Art/V10_FINAL_VEHICLE_REVIEW.md only.
- May write scratch images under work/v10-final-vehicle-review only.
- Inspect 10 vehicle kinds, nine isolated images each, including long vehicle top framing.
- Inspect all 11 active vehicles/trains at five placed angles, 55 images.
- Check closed body, symmetrical wheels, seats, door opening, supports and terrain placement.
- Root handles machines, water and all other evidence; do not redo goods/furniture reviews.
- Use Tools/model_contact_sheet.py if convenient, without modifying original PNGs.
- Finish once, at most 15 report lines with files, checked counts and concrete open issues.

## Root ownership

- Final industrial machines: nine isolated and five placed views each.
- Harbor and Beach: nine isolated and five placed views each.
- Review root-owned fixes, build/runtime acceptance, FPS, video and package provenance.


---

## SOURCE: V10_DELIVERY_CONTRACT.md

# Hợp đồng công cụ bàn giao v10

## Quyền sở hữu

- Agent delivery chỉ sửa Tools/package_city.py, Tools/curate_model_evidence.py.
- Được tạo Tools/test_v10_delivery.py, không sửa source Unity hoặc tài liệu khác.
- Root tạo báo cáo nghiệm thu, manifest source và thực sự chạy đóng gói sau Player.
- Không spawn agent, chạy Unity/Player, commit, publish, đọc hoặc sửa save thật.

## Định dạng và giao diện

- package_city.py giữ CLI --build, --evidence, --output và thêm --source-list tùy chọn.
- Source list: UTF-8, mỗi dòng một đường dẫn tương đối workspace; không dòng trống.
- Khi có source list, đóng gói đúng danh sách, kể cả file mới chưa tracked.
- Mọi nguồn phải nằm trong workspace, tồn tại, không trùng tên không phân biệt hoa thường.
- Luôn đọc bytes checkout; từ chối Git LFS pointer, kiểm CRC/SHA256 và Windows runtime như hiện tại.
- Không có source list thì giữ hành vi git ls-files hiện tại.
- Vẫn từ chối output ngoài work/ hoặc chồng lên build/evidence, không ghi đè gói cũ.
- Không đưa source vào Windows.zip, không đưa save/journal/private/progress vào Evidence.zip.
- Metadata manifest ghi source list đã dùng và số file; commit là HEAD thực tế, giữ dirty marker.
- curate_model_evidence.py thêm --tests bắt buộc: XML full suite do root chỉ định.
- Thêm --comparison tùy chọn cho thư mục A/B FPS; copy vào comparison/ khi được cung cấp.
- Input models và ordinary phải report passed và runtimeErrors rỗng như hiện tại.
- XML phải test-run failed=0, passed>0; không dùng hardcode XML cũ.
- Không làm đẹp ảnh gốc hoặc tạo dữ liệu đạt; giữ checksum từng file được copy.

## Tài liệu root sẽ cung cấp trước khi đóng gói

- QA/city-model-v10-acceptance-20261009.json
- Docs/CITY_MODEL_V10_ACCEPTANCE_20261009.md
- Docs/CARRY_CONTAINERS_V10.md
- Docs/INDUSTRIAL_MODELS_V10.md
- Docs/RENDER_OPTIMIZATION_V10.md
- Docs/Art/V10_GOODS_REVIEW.md
- Docs/Art/V10_WORLD_REVIEW.md
- README.md

## Tự kiểm

- python -m py_compile Tools/package_city.py Tools/curate_model_evidence.py Tools/test_v10_delivery.py
- python Tools/test_v10_delivery.py
- Test bằng dữ liệu synthetic/temp trong work/: source mới chưa tracked được gói đúng bytes;
  source ngoài root và LFS pointer bị từ chối; private checkpoint không bị copy;
  XML fail bị từ chối; Windows runtime/CRC còn được xác nhận.
- Mỗi file khoảng dưới 300 dòng, câu lệnh riêng, không sửa/format phần không liên quan.
- Hoàn thành, tự kiểm rồi báo một lần tối đa 15 dòng: file đổi, lệnh/kết quả, vấn đề còn lại.


---

## SOURCE: V11_CARGO_RIDE_CONTRACT.md

# Cargo boarding failure in actual Player

## Shared API and evidence

- Workspace D:\GAME\TYCOON2; Unity6000.6.3f1 URP17.6.
- Original shared contract Docs/V10_TRAFFIC_GRIDLOCK_CONTRACT.md remains applicable.
- ICityPassengerCarrier has Hull:Transform, DoorsOpen:bool, DoorInsideLocal:Vector3,
  DoorOutside:Vector3, RecordBoarding():void and RecordAlighting():void.
- CityRoadVehiclePassengers.Initialize(GameSession,CityLife,CityVehicleRig):void;
  BeginStop():void; Tick(float delta):bool. Keep these signatures.
- CityLife.FindPassenger(Vector3 point,float radius) finds existing residents; no fake riders.
- CityPublicTransportPassenger.Reserve(ICityPassengerCarrier,Vector3 seatPosition):bool.
- Failed unchanged240second live gate: work/items-vehicles-20261009/player-20261010T002505325Z.
- vehicle-live-facts.json: both trains and all3public plus5other road types completed rides;
  CargoTruck0boardings/0alightings,0world errors.
- vehicle-rides-state.txt shows CargoTruck moving around route, then stopped at(-21.70,0,30.55),
  doors open, rider none, DoorOutside(-24.779,0,33.728). Diagnose NPC lookup/path first.
- Candidate Player source contains civic/park upgrades; root now adds coastline/footpaths separately.

## Ownership and constraints

- May edit CityRoadVehiclePassengers.cs, CityLife.cs ONLY FindPassenger or its directly needed helper,
  CityTraffic.cs/CityTrafficSafety.cs, CityPublicTransport.cs/CityPublicTransportVehicle.cs only if proven needed.
- May create CargoBoardingTests.cs/meta and Docs/V11_CARGO_BOARDING_FINDINGS.md.
- Root owns everything else including coastline/paths/Qa* and all Unity/Player/build runs.
- Retail agent owns RetailPropModels/Details/SafeModelDetails/SafeStation visuals/tests/docs.
- Preserve public APIs, real resident pool, collision protection and bounded pedestrian routes.
- Do not modify ride gate, extend timeout/dwell arbitrarily, force counts, spawn test riders,
  teleport vehicles/passengers, disable physics or change user saves.
- Diagnose exact lookup/path/stop failure from source and recorded data before selecting minimal fix.
- Keep files below about300lines; one statement per line and short Vietnamese comments for complex logic.
- Add meaningful regression for confirmed defect, include route/stop validation when relevant.
- No child agents, no Unity/Player launches, no publication or changes to unrelated modules.
- Self check git diff --check and syntax; root runs native tests and identical live gate.
- Report ONCE max15lines after whole task: files changed, checks, remaining issues.


---

## SOURCE: V11_CASH_CPU_REVIEW_CONTRACT.md

# Cash CPU review contract

## Goal

Read the current checkout cash getter and identify a measured safe optimization candidate.
Root runs the Player and tests. This task is read only except for its findings document.

## Existing interfaces and data

- `CheckoutStation.Cash: long` in `CommerceDirector.cs` reads legacy cash and uncollected payments.
- `RuntimeTransactions` exposes the committed projection; transaction logic owns all balances.
- Every payment has a stable counter ID and collected state; legacy values remain compatible.
- `QaFrameProbe.Measure(string)` only records when a QA profile is active.
- Newly added marker `CheckoutStation.ReadCash` scopes the getter, without changing its output.
- Source changes currently reduce RefreshCash to one getter read per visual update.
- Current Player baseline: `work/city-expansion-20261008/ordinary-play-20261010T012920265Z`.
- Full city prior measurement:147.0457FPS, CPU about6.55ms, GPU about2.04ms.

## Ownership

- May create only `Docs/V11_CASH_CPU_REVIEW.md`.
- Read runtime authority, projection, cash getter, transaction and save code as needed.
- Do not edit code, tests, assets, other docs, saves or build outputs.
- Do not run Unity or Player, spawn subagents, publish or send messages to other chats.

## Done criteria

- Trace invalidation points for a possible cached per counter cash total, including replay and restore.
- State concrete risks and smallest viable safe change; do not invent a cache without evidence.
- Explain whether repeated getter calls traverse an ever growing payment collection.
- Include exact file and function pointers, a test plan and self check with git diff --no-index --check.
- Report once at most15lines with document changed, checks and remaining issues.


---

## SOURCE: V11_CASH_CPU_REVIEW.md

# V11 checkout cash CPU review

## Finding

The measured safe candidate is to reverse the two conditions in
`CheckoutStation.Cash` so `!payment.collected` is evaluated before the counter
ID comparison. In the current source, the loop compares `payment.counter` with
`Id` even for payments already collected. Since payment records are retained
after collection, and a normal play session keeps adding records, the proportion
of collected records can grow over time. The boolean check is a cheap way to
skip those string comparisons.

This is a small, semantics-preserving candidate, not a proven Player speedup.
The supplied baseline has no before/after measurement of this predicate order.
Keep the change only if a matched Player run shows a lower `CheckoutStation.ReadCash`
cost without changing cash totals or frame metrics beyond normal run variance.

## Evidence and code path

- `Assets/_Game/Scripts/Runtime/CommerceDirector.cs:107-118`: `Cash` reads the
  legacy amount, then scans every `Authority.View.payments` record and adds
  amounts matching this counter that are not collected.
- `CommerceDirector.cs:148-160`: every unlocked station's `Update` reads Cash
  before checking whether `shownCash` changed. `Prompt` at line 130 and the
  status label in `LateUpdate` at lines 198-200 also read it.
- `work/city-expansion-20261008/ordinary-play-20261010T012920265Z/ordinary-play-cpu.json`:
  `CheckoutStation.ReadCash` was recorded 112,674 times over the stated 9,389
  frames (about 12 calls/frame), taking 112.203 ms total, or about 0.0010 ms
  per call. Its maximum was 1.6044 ms; the maximum frame measurement was
  1.6125 ms. This is measurable aggregate work, but a small average per call.
- `Assets/_Game/Scripts/Runtime/TransactionCommerce.cs:65-72`: `CreatePayment`
  appends a `PaymentState` once per completed order; no production removal was
  found. `PaymentState` has stable `id`, `counter`, `amount`, and `collected`
  fields (`TransactionState.cs:65-71`).
- `TransactionCommerce.cs:75-88`: collection marks a payment `collected = true`;
  records remain in the list. `RuntimeTransactionBindings.cs:98-107` selects
  only matching uncollected records for collection, then separately handles
  legacy cash.
- `CommerceDirector.cs:187-193` routes player collection to the transaction
  authority. `GameSessionPersistence.cs:39-61` clears station-local fallback
  cash and restores the transaction state before restoring legacy cash values.
  The runtime getter derives its amount from the current authoritative view, so
  it does not hold a stale cached total across restore.
- `RuntimeTransactionProjection.cs:27-32` independently derives saved per-counter
  cash by scanning payments and excluding collected ones. It runs during save
  projection, rather than on each rendered station update.

## Cache assessment

Do not add a cached per-counter total in this optimization. A cache would need
to stay synchronized when `CreatePayment` appends, `Collect` marks one or more
payments collected, legacy cash is restored or collected, and the authoritative
state is reconstructed. Transaction changes replace/publish the runtime view;
replay and restore can reconstruct payment state without going through the live
station getter. These are multiple invalidation paths for a small measured
per-call cost. Predicate reordering requires no invalidation and preserves the
authority as the only balance source.

## Suggested change and verification

Change the loop condition to test `!payment.collected` before comparing
`payment.counter` to `Id`. Do not otherwise alter the loop or balance source.
Check behavior for: no payments; matching and nonmatching uncollected records;
collected records for this and other counters; mixed legacy cash; collecting
twice; save/restore; and replayed payment/collection transactions. Then compare
matched ordinary-play Player runs using the same scene, route, duration, build,
and QA profile. Record getter count, total and average marker time, frame count,
FPS, and cash correctness. Keep the edit only if marker time improves repeatably
and the gameplay totals remain identical; otherwise leave the getter unchanged.


---

## SOURCE: V11_CIVIC_STREET_CONTRACT.md

# V11 civic, leisure and street model contract

## APIs and ownership

- Namespace Tycoon; Unity 6000.6.3f1, URP17.6; workspace D:\GAME\TYCOON2.
- Read Docs/V11_MODEL_CONTRACT.md for shared material and geometry rules.
- CityCivicModels.Police/Fire/Bar/Arcade(CityStaticGeometry g) remain unchanged signatures.
- CityLeisureModels.Playground/Monument(CityStaticGeometry g) remain unchanged signatures.
- CityLeisureModels.Signal(CityStaticGeometry g, Vector3 p) remains unchanged.
- CityLeisureModels.Railing(CityStaticGeometry g, Vector3 a, Vector3 b) remains unchanged.
- CityProps.Bench(g,p), Planter(g,p,width), Lamp(g,p) keep signatures and ground coordinates.
- CityLandscape.Park(g,space) alone may be edited in that file; reservation remains23x14m.
- May create CityCivicDetails.cs/meta, CityStreetPropDetails.cs/meta, focused model tests/meta,
  and Docs/V11_CIVIC_MODELS.md. All runtime files under Assets/_Game/Scripts/Runtime.
- Tests under Assets/_Game/Tests/Editor; max about300lines per new file.
- Do not edit any other part of CityProps or CityLandscape.
- Root owns CityCommercialDetails/Models, OuterDistricts, Landmarks, Coast, boats/marina,
  water shader, Art integration and all QA/build tools. Retail agent owns retail models and safe visuals.

## Model and placement constraints

- Upgrade police/fire/bar/arcade, playground/monument, planters/lamps/bench/signals/rails and park.
- Give each deliberate front/back/side construction, bevels, functional supports and material differences.
- Preserve all existing district footprints, approach points, collisions and walkable entrances.
- Police38x34, Fire38x38, Bar32x30, Arcade40x32, Playground40x32, Monument30x30 reservations.
- Keep park fountain/tree/bench placement inside23x14m; paths remain walkable and collision free.
- No new business simulations, NPC roles, real time Light components, shaders or water changes.
- Existing CityStaticGeometry Box/RotatedBox/Beam/Cylinder/Cone/Crown/Collider/Ramp/Finish APIs apply.
- Geometry batches by shared material and32m chunk. Static props do not need Update methods.
- Avoid invisible support, floating decor, details behind opaque walls and primitive clutter.
- Keep material group count restrained; textures use existing TownSurfaceMaterials.
- Colliders must match substantial occupied volume and stay clear of pedestrian lanes.
- Do not change IDs, progress, save formats, inventory, physics/nav APIs or upgrade definitions.

## Verification and reporting

- Self check git diff --check, file line limits and obvious geometry/support issues.
- Add meaningful footprint/support tests where they can catch concrete defects.
- Do not launch Unity or Player; root runs native tests,8 surrounding plus top views,
 5 placed views and Windows FPS. Do not claim visual acceptance from code alone.
- No child agents, no messaging other chats, no publication, no user save edits.
- Report once at most15lines: files changed, actual checks and remaining issues.


---

## SOURCE: V11_MODEL_CONTRACT.md

# V11 model contracts — 10/10/2026

## Shared runtime API

- Namespace Tycoon; Unity 6000.6.3f1, URP17.6, D:\GAME\TYCOON2.
- RetailPropModels.Supports(string key): bool.
- RetailPropModels.Size(string key): Vector3, grounded dimensions in local coordinates.
- RetailPropModels.Build(string key): GameObject, root at local origin, bottom at y=0.
- Supported keys: storage_rack, display_counter, checkout_counter, supply_crate.
- Root integrates this factory in Art.Model and Art.ModelSize; agent must not edit Art.cs.
- Root skips TownPropSurfaces.Apply for these models to preserve designed material assignments.
- TownModelParts.Box(name, point, size, surface, tint, parent, yaw=0, solid=false).
- TownModelParts.Cylinder(name, point, size, surface, tint, parent).
- TownSurfaceMaterials.Get(surface,tint) returns shared cached materials.
- Static details share mesh/material and can batch; animated doors/stock remain separate.

## Placement and stock invariants

- Safe base footprint 1.6 x 1.3 m; body 1.3 x 1.05 m, safe faces local +Z.
- Safe deposit/withdraw offsets -1.65/+1.65 on X must remain unchanged.
- display_counter footprint 1.4 x 4.7 m, horizontal stock support y=1.04.
- Stock rectangle center (0,1.04,-.25), size(1.2,3.4); decorative rails must not cross goods.
- checkout body center(.3,.55,0), size(2.1,1.1,.9); stock support y=1.10.
- Checkout stock rectangle center(-.25,1.1,.1), size(1.05,.96).
- storage_rack placed at(0,.15,.65); warehouse stock floor y=.16, front must remain open.
- Current warehouse footprint3.4 x3.4, invisible body collider center(0,.55,.5), size(3.1,1.1,2.5).
- supply_crate is a static prop; preserve closed body, correct seams and sensible pallet placement.
- Keep IDs, station interactions, inventory data, save/journal and upgrade definitions unchanged.
- Do not add visible fake products; actual stock remains managed by InventoryStack.

## Style and budget

- Semi realistic warm industrial/retail style, bevels, hardware, restrained wear and shared PBR surfaces.
- Give each object deliberate silhouette, continuous supports and correct front/back/side construction.
- No primitive clutter or loose floating details; no new per frame work for static props.
- Target at most 6 material groups and about 6000 triangles per base prop.
- New files below about300 lines, one statement per line, brief Vietnamese comments.
- Root handles 8 surrounding views plus top and5 views in actual city, Windows builds and FPS.

## Agent ownership

- May create RetailPropModels.cs/meta, RetailPropDetails.cs/meta, SafeModelDetails.cs/meta.
- May edit SafeStation.cs only visual construction block; preserve zones, colliders and behavior.
- May create focused geometry/support tests in RetailPropModelTests.cs/meta and Docs/V11_RETAIL_MODELS.md.
- No other edits, no Unity/Player launches, no child agents, no save changes or publication.
- Read existing prefabs/catalog dimensions and upgrade details before deciding exact form.
- Self check git diff --check and source syntax/line lengths; root runs Unity tests and Player.
- Report once at most15 lines: changed files, self checks and concrete outstanding issues.

## Root ownership

- Commercial buildings: café, bank, hotel, theater; existing restaurant/civic/leisure upgrades.
- City props, park, beach, boats/marina, water shader, streetscape and all integration files.
- Shared CityStaticGeometry primitives batch by material and32m chunk, with physical colliders.
- GPU occlusion remains on; shadows use1 cascade, balanced55m/2048 after measured70m cost.
- Performance target165FPS is still pending measured whole city acceptance.


---

## SOURCE: V11_PERFORMANCE_RECOMMENDATIONS.md

# V11 CPU performance recommendations

## Decision

No source optimization is justified by the available measurements. Keep current behavior and collect
branch- or operation-level evidence before changing the hot paths. The measured method totals identify
where CPU time occurs, but do not show which repeated calls inside those methods dominate or what can
be removed without changing live gameplay.

## Evidence and limits

The source run is `work/city-expansion-20261008/ordinary-play-20261010T000921124Z`. Its report records
9,586 frames over 62.89 seconds, average CPU 5.778 ms/frame, average GPU 2.213 ms/frame and 152.42 FPS.
The 165 FPS stability criterion failed. This was a maximum-mod diagnostic, not a full city tour.

`ordinary-play-cpu.json` attributes 5,583.95 ms to 891,217 calls of
`ActorPhysicalMotion.1.Update` (about 0.0063 ms/call) and 1,388.93 ms to 488,886 calls of
`Station.1.LateUpdate` (about 0.0028 ms/call). These are method totals, not identified removable work.
Physical motion owns required live capsule movement, support checks, and NavMesh synchronization;
the present idle path already skips some capsule moves while preserving immediate movement. No evidence
isolates redundant operations within it.

`InventoryStack.LateUpdate` (337.44 ms) wraps `InventoryStack.Refresh` (215.96 ms), so these totals
overlap and must not be added. Refresh already exits when inventory revision and layout are unchanged.
The measurements do not establish that its remaining checks or work are avoidable.

`ordinary-play-native-cpu.json` reports 7,948.72 ms in
`PreLateUpdate.ScriptRunBehaviourLateUpdate`, 18,846.05 ms in
`Update.ScriptRunBehaviourUpdate`, and 330.46 ms in `GC.Collect`; these broad markers overlap their
child work and each other. They do not attribute allocation or native engine cost to a specific
candidate call. No estimates are summed across markers.

## Revisit criteria

Add narrow, temporary per-branch/per-operation counters or profiler samples in a separate diagnostic
run, then repeat the same ordinary-play scenario and a full city tour. Consider an edit only when the
evidence identifies repeated work with a safe unchanged behavior. Any physical-motion change must keep
the capsule enabled, immediate walking and support checks; any station/UI change must preserve live
simulation, boarding, and visible NPC animation. Recheck gameplay and frame pacing in the Player after
an edit; this report alone does not establish a successful 165 FPS result.


---

## SOURCE: V11_PERFORMANCE_REVIEW_CONTRACT.md

# V11 performance investigation contract

- Workspace D:\GAME\TYCOON2; Unity6000.6.3f1 URP17.6, D3D11 RTX4060 Laptop.
- Read only source and recorded measurements. Never launch Unity/Player or change source/settings/save.
- Only output Docs/V11_PERFORMANCE_RECOMMENDATIONS.md. No other edits, no child agents.
- Existing baseline before V11: work/city-expansion-20261008/ordinary-play-20261010T000921124Z.
- This60second maximum mod diagnostic reports152.42098FPS, CPU5.77772ms, GPU2.21308ms;
  P957.2061/P999.0958ms. It is not full city tour and165FPS has not passed.
- Read ordinary-play-report.json, ordinary-play-cpu.json, ordinary-play-native-cpu.json from that directory.
- Earlier full tour: ordinary-play-20261009T144927602Z,146.51694FPS across616.8s, CPU6.08494/GPU2.47986ms.
- Source candidates ActorPhysicalMotion.cs, ActorView.cs, Station.cs, InventoryStack.cs,
  OrderBubbleCustomerAgent.cs, CityTraffic.cs, CityPublicTransportVehicle.cs, WorkerTransport*.cs,
  CityNativeCpuProbe.cs and Camera/graphics only if measurements justify the read.
- Preserve physical capsules, immediate walking and support checks, visible NPC animation,
  live physics/navigation/boarding, inventory/save semantics and rendering consistency.
- No guessed renderer knob, magic sample reduction, hardware/driver changes or weakened acceptance.
- Find concrete repeated native/property calls or allocations with measured ownership.
- Recommend at most3surgical changes, exact file/function, expected risk and meaningful regression.
- Separate measured cost from inference; overlapping CPU markers must not be added together.
- Self check report paths/units, git diff --check. Report ONCE max15lines with file and open issues.


---

## SOURCE: V11_SNAPSHOT_OPTIMIZATION_CONTRACT.md

# Hợp đồng tối ưu snapshot V11

## Bằng chứng

CheckpointProbe, ordinary-play-20261010T031723380Z, cùng checkpoint QA cũ:
TransactionSnapshot72,879/66,029ms; CaptureWorld1,829/0,731ms;
CopyWorld1,669/1,896ms; Projection1,299/2,680ms.
Snapshot là bước cần sửa; không thêm index projection chưa có lợi ích được đo.

## Giao diện và bất biến

- `CheckpointSnapshot.Copy<T>(T value)` trả đúng runtime type, giữ mọi field hiện đang được sao chép.
- Null/string/value type vẫn giữ cách xử lý hiện hành.
- Array/list và mọi DTO con phải độc lập với đối tượng gốc trước khi gửi worker.
- Không đổi order của list, version save, receipt IDs, journal, lease, lock gate hoặc dữ liệu tiền/hàng.
- `TransactionCore.CaptureCheckpoint()` vẫn khóa gate cho snapshot nhất quán.
- Snapshot là DTO JSON acyclic; không thêm cơ chế xử lý graph/cycle ngoài phạm vi.
- Không shallow-copy các list/order/recipe; không xóa receipt/outbox/history.
- Giữ metadata recipe arrays, null reference và mọi long không bị đổi/thu hẹp.

## Thay đổi được phép

Agent chỉ sửa Assets/_Game/Scripts/Runtime/CheckpointSnapshot.cs.
Được tạo Assets/_Game/Tests/Editor/CheckpointSnapshotTests.cs và .meta.
Được tạo Docs/V11_SNAPSHOT_OPTIMIZATION.md, dưới300dòng.
Không sửa TransactionState/RuntimeTransactionProjection/GameSession/QA markers hoặc file khác.

Giải pháp nhỏ: thay `MethodInfo.Invoke` mỗi DTO bằng một open-instance delegate tới
`object.MemberwiseClone` tạo một lần; giữ vòng deep-copy reference fields như hiện tại.
Nếu delegate không hoạt động trên runtime thực tế, nêu lỗi và phương án nhỏ nhất,
không thêm code fallback giả định. Không dùng Expression.Compile/dynamic code generation.

## Test

Viết các test có ý nghĩa: bản sao giữ runtime type, mọi receipt field và long lớn;
nested DTO/list/recipe array/null; sửa gốc và bản sao không tác động bên còn lại;
danh sách receipt lớn giữ đủ ID và thứ tự. Không assert tốc độ theo đồng hồ EditMode.
Đọc CheckpointRecoveryTests.cs để giữ semantics. File dưới300dòng và comment Việt ngắn.

Root sở hữu mọi tiến trình Unity/Player và sẽ chạy native/build/đối chứng.
Agent không chạy Unity/Player, không đọc save thật, không tạo subagent, không commit/publish.
Tự kiểm `git diff --check -- Assets/_Game/Scripts/Runtime/CheckpointSnapshot.cs
Assets/_Game/Tests/Editor/CheckpointSnapshotTests.cs Docs/V11_SNAPSHOT_OPTIMIZATION.md`.
Báo một lần tối đa15dòng: file đổi, tự kiểm, rủi ro/open issues; không tuyên bố nhanh hơn khi chưa đo.


---

## SOURCE: V11_SNAPSHOT_OPTIMIZATION.md

# Snapshot checkpoint V11

## Thay đổi

`CheckpointSnapshot` tạo một open-instance `Func<object, object>` từ
`object.MemberwiseClone` khi static class được khởi tạo. Mỗi DTO gọi delegate này,
thay cho `MethodInfo.Invoke` trên từng đối tượng.

Vòng sao chép sâu các public reference field, array và list giữ nguyên.
Không thay đổi gate của `CaptureCheckpoint`, schema, receipt, outbox hoặc journal.
Không dùng dynamic compilation và không thêm fallback chưa được kiểm chứng.

## Test đã bổ sung

`CheckpointSnapshotTests` kiểm tra:

- Runtime type khi đầu vào có kiểu khai báo `object`; mọi field receipt và `long` lớn.
- DTO lồng nhau, danh sách, metadata recipe, mảng recipe và null.
- Sửa nguồn hoặc snapshot không thay đổi dữ liệu của bên còn lại.
- 20.000 receipt giữ đủ ID, thứ tự và giá trị; từng entry là đối tượng độc lập.
- Null, string, value type, mảng rỗng và mảng null giữ semantics hiện tại.

Test truy cập helper internal qua reflection trong test assembly;
reflection này không nằm trong vòng clone runtime.

## Kiểm chứng và giới hạn

Agent triển khai chỉ kiểm tra diff, độ dài file và định dạng.
Agent chính sở hữu việc chạy EditMode, build và Player trên Unity 6000.6.3f1.
Cần kết quả runtime để xác nhận việc tạo delegate tới method protected trên backend dùng thực tế.
Chưa tuyên bố cải thiện thời gian; phải đo lại cùng checkpoint sau khi test/build đạt.


---

## SOURCE: V11_VISUAL_CITY_REVIEW_CONTRACT.md

# V11 actual city capture review contract

- Read only actual candidate Player images and report, not source rewrites.
- Evidence D:/GAME/TYCOON2/work/items-vehicles-20261009/player-20261010T001752914Z.
- Player city-models-v11-report.json PASS, RTX4060 Laptop,1080p,0runtime errors.
- Review all13landmarks: PoliceStation,FireStation,SeasideBar,EntertainmentHall,Playground,
  FoundersMonument,Harbor,Beach,CoffeeHouse,Bank,Hotel,Theater,Marina.
- Each has city-single-ID-{front,right,rear,left,three-quarter,rear-quarter,front-left,rear-right,top}.png.
- Each has city-placed-ID-{front,right,rear,left,top}.png in actual city.
- Create only Docs/Art/V11_CITY_VISUAL_REVIEW.md, plus review contact sheets under evidence/review
  with exclusive prefix v11-city-audit. Root owns all code, other docs and prop/station review.
- May use Tools/model_contact_sheet.py --evidence PATH --pattern 'city-single-*.png'
  --name v11-city-audit-isolated --columns3 --per-page18 --wide and similar placed pattern.
- Inspect actual pictures with view_image; no code-only approval or counts-only conclusions.
- Judge silhouette, front/back continuity, floating parts, obvious overlaps, ground contact,
  sidewalk/building alignment, accessible approach and obvious footprint problems.
- If a face is too small/occluded, say unproven; do not invent invisible detail or functional NPC behavior.
- Do not edit images except unmodified contact sheet assembly; originals stay intact.
- No Unity/Player launches, new tests, source/settings/save edits, child agents or publication.
- Self check exact paths and git diff --check. Report ONCE max15lines: artifacts, defects, limitations.


---

## SOURCE: V12_CPU_RENDER_CONTRACT.md

# V12 CPU/render diagnostic contract

## Existing state

- Checkout: D:\GAME\TYCOON2, Unity 6000.6.3f1, URP 17.6, Windows RTX 4060 Laptop.
- Goal remains active. Stable 165 FPS requires a 6.061 ms frame budget.
- Latest release is work/city-model-v11-20261010/Release/TYCOON2.exe.
- Latest fresh full tour is work/city-expansion-20261008/ordinary-play-20261010T034124820Z.
- Full loaded city: 149.008 FPS, P95 10.115 ms, CPU 6.763 ms, GPU 2.075 ms.
- Current packages are immutable snapshots under work/city-model-v11-20261010/Delivery.
- Gameplay simulation, collisions, animation, goods, saves and real NPC transport must remain intact.
- Existing scopes use QaFrameProbe.Measure(string) and CityOperationTiming.Measure(GameSession,string).
- Frame data, marker totals and overlapping parent/child scopes must not be summed as independent costs.
- V11 gallery freezes simulation; it is not an FPS acceptance run.
- Native tests and a build do not establish visual quality or stable frame pacing.

## Read-only render review assignment

- Read the latest profiled run ordinary-play-20261010T033534095Z and relevant rendering source/settings.
- Compare report fields and identify actual measured CPU/render costs with units and sample scope.
- Inspect repeated static/dynamic render construction and existing batching/culling coverage.
- Recommend a code edit only if evidence identifies removable work without weakening gameplay or visuals.
- Write only Docs/V12_CPU_RENDER_FINDINGS.md, fewer than 120 lines, no runtime changes.
- Do not run Unity, launch a Player, generate assets, touch packages, publish or start subagents.
- Verify file line count and git diff --check for the owned report.
- Report once, at most 15 lines, with changed file, checks, evidence and unresolved attribution.

## Main-agent work

- Main owns package extraction and Player smoke with a separate QA save/output path.
- Main may add narrow diagnostic markers to ActorPhysicalMotion and Station only after reading current code.
- Markers are diagnostic-only; no simulation frequency, collision or visual fidelity changes.
- Any optimized candidate gets native regression checks and an actual visible Player comparison.
- Preserve package V11 provenance when newer diagnostic source is created.


---

## SOURCE: V12_CPU_RENDER_FINDINGS.md

# V12 CPU and render findings

## Scope and result

- Read-only review of `work/city-expansion-20261008/ordinary-play-20261010T033534095Z`.
- The 60.194 s capture contains 9,059 frames at 1920x1080 on an RTX 4060 Laptop,
  Direct3D 11, with 39 workers and 24 customers.
- `ordinary-play-report.json` reports 150.498 average FPS, P95 8.405 ms,
  P99 11.015 ms, worst 203.508 ms, average CPU 6.497 ms and GPU 2.073 ms.
- The target budget is 6.061 ms. This capture is marked `near165Fps: true` but
  `stable165FrameBudget: false`; its P95 exceeds budget by 2.344 ms.
- This run is a stationary full-city interval (`walkedMetres: 0`), so it does
  not represent the separate full-tour result in the V12 contract.

## Measured CPU and render costs

- Native markers average 6.617 ms in `Main Thread` and 6.615 ms in `PlayerLoop`
  across the same 9,059 frames. `WaitForTargetFPS` averages 0.137 ms.
- Native `Update.ScriptRunBehaviourUpdate` averages 2.206 ms/frame; late-update
  scripts average 0.936 ms/frame; `PreUpdate.AIUpdate` averages 0.432 ms/frame.
- `RenderLoop` averages 0.789 ms/frame, with an 8.163 ms maximum. This is a
  native marker, not a per-camera or per-pass breakdown. GPU time is separately
  reported as 2.073 ms average.
- Frame render counters average 58.83 SetPass calls (max 61), 960,326 triangles
  (max 1,102,915), 1,019,469 vertices (max 1,241,538), 297.47 shadow casters
  (max 351), and 18.17 visible skinned meshes (max 21).
- The capture also averages 26 render textures and 28.99 render-texture changes.
  These aggregate counters do not attribute cost to a renderer or pass.
- Native `GC.Collect` averages 0.040 ms/frame, while its maximum is 8.367 ms.
  The worst frame in the report is 203.508 ms; the long-frame CSV samples
  include startup/restore and checkpoint frames, so they do not identify a
  recurring steady-state render cause.
- `ordinary-play-cpu.json` includes high-frequency scopes such as
  `ActorPhysicalMotion.1.Update` (842,089 calls, 6,047.495 ms total) and
  `ActorPhysicalMotion.3.CapsuleMove` (442,954 calls, 4,151.264 ms total).
  These are instrumentation totals over the capture, not independent frame
  costs; parent and child scopes overlap and must not be added.

## Render construction and settings

- `CityStaticGeometry.Add` buckets static primitives by shared material and
  32 m X/Z cell. `Finish` combines each bucket into one mesh/renderer, marks
  the object static, and enables shadow casting and receiving.
- `CityDressing.Build` builds the central city geometry once, then calls
  `Finish`. Outer landmarks, access paths, ocean edge, and streetscape use
  separate geometry roots/managers, so their chunks cannot combine with the
  central root or with each other.
- This static build path is construction-time work during world creation or
  restore; it is not repeated every frame. Dynamic NPCs, vehicles, goods, and
  business visuals are created/updated through separate runtime components.
- PC URP source settings use render scale 1, MSAA 2, depth and opaque textures,
  main-light shadows, 45 m shadow distance, and one cascade. The run report also
  records one cascade, render scale 1, and disabled display sync.

## Recommendation and attribution limit

- The capture proves substantial main-thread CPU activity and a nontrivial
  render workload, but does not isolate a removable render path. Keep NPC
  simulation, capsule collisions, goods, water, shadows, and boarding intact.
- No code or setting optimization is evidence-supported by these aggregate
  counters alone. Preserve the material/cell batching until a Profiler or
  Frame Debugger capture attributes CPU/render-thread time and draw calls to
  concrete chunks, shadows, skinned meshes, or passes. Then compare a narrow
  candidate in the same loaded city and visible Player conditions.
- Attribution remains unresolved: native markers do not split render passes;
  render counters are whole-frame totals; high-frequency custom scopes overlap;
  and the working source tree is dirty, so inspected source cannot be proven
  byte-identical to the profiled V11 release from this evidence alone.


---

## SOURCE: V12_PACING_REVIEW_CONTRACT.md

# V12 pacing review contract

## Goal and current facts

- Workspace: D:/GAME/TYCOON2; Unity 6000.6.3f1, URP17.6, Windows D3D11.
- RTX4060 Laptop; Windows display 2560x1600 at165Hz; QA Player1920x1080.
- Goal165FPS with P95 frame duration <=1000/165ms remains unmet.
- Full fresh IL2CPP tour is running PID2280, exec session96444. Do not interrupt it.
- Run folder: work/city-expansion-20261008/ordinary-play-20261010T050540824Z.
- Player log explicitly reports broken VSync which does not limit frame rate properly.
- GameSettingsStore.Apply sets vSyncCount1,targetFrameRate-1 when syncDisplay=true.
- Only --qa-benchmark uncaps via GameSession.Awake; run_city_play does not pass that flag.
- Earlier ABBA pacing found no consistent winner; preserve raw frame spikes.

## Review scope and output

- Review only code, launch flags, resolution/fullscreen transitions and official Unity references.
- Identify a concrete cause or a bounded experiment; distinguish settings from observed presentation.
- May create/edit only Docs/V12_PACING_REVIEW.md, under300lines and120columns.
- Do not edit runtime, tests, ProjectSettings, packages, other documentation or user settings.
- Do not start Unity, a Player, profiler, compiler, package/hash job or network downloader.
- Do not change the165FPS gate, omit hitch frames, disable collisions/animation or alter saves.
- Do not spawn subagents. No progress messages to parent; report once, at most15lines.

## Shared interfaces and evidence

- GameSettingsStore.Current: GameSettingsData with syncDisplay,fpsLimit,fullscreen,resolutionWidth/Height.
- QualitySettings.vSyncCount and Application.targetFrameRate are runtime values, not measuredFPS.
- Screen.currentResolution.refreshRateRatio.value is reported display refresh.
- Tools/run_city_play.ps1 launches a standalone QA Player; inspect actual argument list.
- CityPlayReport/CityFrameSampler own QA frame output; inspect fields without changing report format.
- Existing evidence: Docs/V11_PACING_FINDINGS.md, Docs/V12_LABEL_PERFORMANCE_ACCEPTANCE.md.
- Self-check: confirm file lines/columns and git diff --check for your file only.
- Final report: output path, verified facts, recommended next experiment, unresolved limitations.


---

## SOURCE: V13_DELIVERY_CONTRACT.md

# V13 delivery preparation contract

- Workspace D:/GAME/TYCOON2, Unity6000.6.3f1, URP17.6, Windows IL2CPP.
- Do not alter models, shaders, save, scenes, settings or runner source in this task.
- Allowed agent file: work/city-model-v13-20261010/prepare_delivery.py only.
- Agent must not execute copying/hashing/packaging while gameplay benchmark is running.
- Root will execute preparation after current Player exits.

## Inputs

- Final build: work/city-model-v13-20261010/FinalRelease/build-report.json.
- Final tests: work/city-model-v13-20261010/full-tests-final.xml,887PASS,0FAIL,0skip.
- Ocean final: work/ocean-v13/player-20261010T062809251Z/ocean-v13-report.json.
- Gameplay final: work/city-expansion-20261008/ordinary-play-20261010T063058411Z/ordinary-play-report.json.
- Gameplay prior: work/city-expansion-20261008/ordinary-play-20261010T053833445Z/ordinary-play-report.json.
- Historical model evidence: work/city-model-v11-20261010/Evidence.
- Source base whitelist: work/city-model-v12-20261010/SourceList.txt.
- Use package_city.checked/private_evidence/EVIDENCE_SUFFIXES/RELEASE_DOCS for safe paths and filtering.

## Outputs and data

- QA/city-model-v13-acceptance-20261010.json, under300lines, schema3.
- Include exact test totals, current build info/path, current ocean report/path and renderCostRuns.
- Include concise current and prior gameplay summaries with raw report path references.
- Include history scope:664V11modelimages,2058checks, geometry unchanged except coastal context/water fog.
- stable165Accepted=false and actualPresentationCadenceVerified=false; desktop unlock is pending.
- All inputs must pass with no runtimeErrors; abort if final Player report is missing or failed.
- Preserve actual frame counts and percentiles; never claim frozen gallery or engine FPS proves scanout.
- Evidence folder fresh under work/city-model-v13-20261010/Evidence, never overwrite existing.
- Copy historical model evidence, both current final Player dirs, prior full-tour dir, native red/green XML,
  all RELEASE_DOCS and final build report; exclude private save/journal/progress and unsupported file types.
- Record SHA256,bytes,path for each evidence file in evidence-index.json.
- SourceList.txt must inherit V12 whitelist, add exactly owned V13 runtime/tests/meta/docs/index.
- Additional owned files: CityOceanHorizon.cs/.meta, QaOceanV13.cs/.meta, OceanContinuityTests.cs/.meta,
  Docs/V13_OCEAN_CONTINUITY.md, V13_OCEAN_QA_CONTRACT.md, V13_OCEAN_QA_REVIEW.md,
  V13_PLAYER_GUIDE.md, V13_DELIVERY_CONTRACT.md and QA/city-model-v13-acceptance-20261010.json.
- All runtime paths above are under Assets/_Game/Scripts/Runtime except tests under Assets/_Game/Tests/Editor.
- Do not add unrelated ASSET downloads, AGENTS.md, private saves or code into Windows build output.
- Self check only py_compile and diff whitespace; do not run preparation or Unity or Player.
- Root uses existing Tools/package_city.py afterwards to create Windows.zip,Source.zip,Evidence.zip and manifest.


---

## SOURCE: V14_ACCEPTANCE_SENSOR_CONTRACT.md

# V14 acceptance and sensor audit contract

## Shared scope

- Workspace: D:\GAME\TYCOON2, Unity 6000.6.3f1, URP 17.6, Windows IL2CPP Candidate.
- Goal remains the full user brief: fragile-only containers, eight isolated views/five placement views,
  double width/length city with new districts, NPC and real Player performance, matched delivery.
- Candidate: work/city-model-v14-20261010/Candidate/TYCOON2.exe.
- Live tour: work/city-expansion-20261008/ordinary-play-20261010T085522987Z, PID 35148.
- The live tour must finish. Do not launch heavy tests, builds, compression, another Player or UI automation.
- Preserve unrelated dirty files, existing save IDs and all previous evidence.
- No subagents, publishing, commits, downloads, installs or process termination.

## Acceptance audit owner

- Owns only Docs/V14_REQUIREMENT_EVIDENCE.md.
- Read existing acceptance reports, raw report metadata, gallery index and current source as needed.
- Map each explicit goal requirement to exact evidence, version and scope.
- Distinguish code/test coverage, frozen gallery, live Player, thermal measurement and physical display.
- Do not equate V13 full tests with current V14 full tests; V14 currently has 24 targeted checkpoint tests.
- V14 short Normal run: ordinary-play-20261010T081545725Z; 20 captures, functional PASS, stable165 false.
- Delivery ZIPs under V13 Delivery are frozen V13 packages, not current V14.
- Mark missing/weak evidence honestly and list next required checks. Do not edit code or fabricate acceptance.
- Keep the report below 300 lines with links relative to the project and lines near 120 columns.

## Sensor audit owner

- Owns only Docs/V14_HARDWARE_SENSOR_AVAILABILITY.md.
- Read measure.ps1 and current hardware reports under work/fps-hardware-20261010.
- Inspect available local sensor interfaces read-only and use light probes only.
- Seek actual CPU temperature/effective clock sources; Windows 3801 MHz is not per-core live boost proof.
- Do not infer CPU temperature from GPU, ACPI ambient values, base clocks or performance counters.
- Do not load drivers, install tools, run stress tests or change power/graphics settings.
- If an installed supported interface yields a genuine CPU sensor, document exact source and semantics.
- If unavailable, record verified probes and retain null rather than guessing.
- CPU process usage is normalized over 16 logical processors; total CPU and GPU include other apps.
- Keep report below 300 lines. A sensor found now is not retrospective data for earlier runs.

## Self-check and report

- Check file line count and git diff --check for the owned file.
- Reference only existing paths; report absent files explicitly.
- Complete the audit and report once, at most 15 lines: owned file, checks, findings and open issues.


---

## SOURCE: V14_DELIVERY_HITCH_CONTRACT.md

# V14 delivery and hitch audit shared contract

## Scope and ownership

- Workspace D:/GAME/TYCOON2, Windows Unity 6000.6.3f1, URP 17.6, native IL2CPP Candidate.
- The full user objective remains active; stable 165 FPS and actual 165 Hz presentation are not accepted.
- Existing V13 ZIPs and all private saves/journals must remain unchanged.
- At most two agents; no child agents, publishing, commits, process termination or GUI control.
- Do not launch a Player, Unity, tests, preparation or compression without root executing that step.

## Delivery preparation owner

- Own only Tools/create_city_v14_evidence.py. Function: prepare(root: Path) -> dict.
- Root will run it after performance sampling ends. Agent only py_compile and whitespace self-checks.
- Create fresh work/city-model-v14-20261010/Evidence; refuse existing directory rather than overwrite.
- Reuse eligible V13 Evidence bytes; copy current Player tour, current native tests/build metadata,
  current normal short run and hardware data with exact per-window scopes.
- Inputs: work/city-model-v13-20261010/Evidence;
  work/city-expansion-20261008/ordinary-play-20261010T085522987Z;
  work/city-expansion-20261008/ordinary-play-20261010T081545725Z;
  work/fps-hardware-20261010;
  work/city-model-v14-20261010/full-tests-final.xml and full-tests-final.log;
  work/city-model-v14-20261010/Candidate/build-report.json;
  work/city-model-v14-20261010/runtime-source-comparison.json;
  QA/city-model-v14-acceptance-20261010.json.
- Validate current tour passed, zero runtime errors, capture gate passed with 42 images,
  native 896/896 zero failed/skipped and build Succeeded. Do not invent performance acceptance.
- Use package_city.checked/private_evidence/EVIDENCE_SUFFIXES/RELEASE_DOCS filtering.
- Copy only eligible data, exclude save/journal/progress/private data and tool source from Evidence.
- Include current RELEASE_DOCS. Do not rewrite their acceptance fields or historical version labels.
- Write concise evidence-index.json with relative path, bytes, SHA256; split index chunks under 300 lines.
- Do not add unrelated downloads or AGENTS.md. Do not edit SourceList, QA, docs or runtime files.
- Keep script under 300 lines, lines near 120 columns. Report once, max 15 lines.

## Hitch audit owner

- Own only Docs/V14_HITCH_DIAGNOSIS.md. Read current tour CSV/report/log and relevant current source.
- Match hitch frame numbers to operation timings; distinguish restore/QA, autosave, gameplay and unknown.
- Do not claim every spike comes from checkpoint; do not add nested timing scopes as independent totals.
- Describe evidence for any remaining CPU path and propose the smallest concrete next measured experiment.
- Consider device utilization versus process CPU and timing fallback without guessing thermal throttling.
- No code changes, tests, builds, heavy archive reads, threshold reductions or fabricated counter values.
- Self-check owned file line count, referenced paths and git diff --check. Report once, max 15 lines.


---

## SOURCE: V14_PERFORMANCE_NPC_CONTRACT.md

# V14 performance and NPC diagnostic contract

## Scope

Continue the current Unity 6000.6.3f1 / URP 17.6 source in D:/GAME/TYCOON2.
Preserve the existing V13 packages and unrelated dirty files. No publishing or driver changes.
Target remains verified smooth 165 FPS, all models, world, NPCs, and Windows delivery.
Do not interpret a passing functional report or an engine FPS average as presentation acceptance.

## Shared existing interfaces

- `CityPlaySession` is a partial MonoBehaviour, launched by `Tools/run_city_play.ps1`.
- `CityActorEvidence.Sample(GameSession game)` observes live NPCs during the recording interval.
- `CityActorEvidence.Save(string directory)` writes NPC motion evidence and returns acceptance.
- `CityPlaySession.FinishReport()` records this result in `ordinary-play-report.json`.
- `CityNativeCpuProbe.Begin()` / `Finish(string directory)` record native CPU markers and counters.
- `--qa-city-profile true` enables instrumentation only; no changes to simulation speed or population.
- `-Manual165` sets targetFrameRate 165, VSync 0; default mode requests display synchronization.
- `-Visible` launches a normal window; the default launcher uses WindowStyle Hidden.

## Authoritative evidence

- V13 packaged executable: work/city-model-v13-20261010/PackageSmoke/TYCOON2.exe.
- Long gameplay: work/city-expansion-20261008/ordinary-play-20261010T063058411Z.
- Capped stationary run: work/city-expansion-20261008/ordinary-play-20261010T073717087Z.
- The capped run has 68 walked NPCs and zero animated NPCs; functional acceptance failed.
- The long run passed NPC checks but stable165FrameBudget is false.
- Current hardware telemetry is background-window data; it does not prove visible-game load.
- CPU actual clock and temperature are unavailable. GPU whole-device telemetry is available.

## Work ownership

Main agent owns performance measurements, runtime performance changes, focused tests, and the handoff.
NPC diagnostic agent may create/edit only Docs/V14_NPC_DIAGNOSTIC.md.
The diagnostic agent may read source and existing QA evidence without modifying runtime or test files.
Do not run Unity, a Player, builds, hardware samplers, or other heavy tools in the diagnostic agent.
No child agents. Report once with at most 15 lines after finishing.

## NPC diagnostic deliverable

Explain the failed animation evidence using concrete source paths and both Player reports.
Check Animator culling, visibility, renderer state, sampled transforms, and the evidence acceptance rule.
Distinguish a demonstrated gameplay defect from inadequate background-window animation evidence.
Recommend the smallest next reproducible test or fix without weakening existing acceptance requirements.
Self-check the markdown file length stays below 300 lines; verify every cited path exists.

## Performance verification

Preserve raw frame times and hitch records. No removal of slow frames from acceptance.
Attribute expensive work using CPU scopes and operation timing, then change only the measured path.
Compare the same city checkpoint, executable settings, population, resolution, and launch mode.
Visible-window presentation and CPU sensor validation remain separate requirements.

## Checkpoint history module assignment

The checkpoint agent owns only these code files:
- Assets/_Game/Scripts/Runtime/CheckpointSnapshot.cs
- Assets/_Game/Tests/Editor/CheckpointHistoryTests.cs and its .meta file
- Docs/V14_CHECKPOINT_HISTORY.md

Existing `CheckpointSnapshot.Copy<T>(T value)` and `TransactionCore.CaptureCheckpoint()` signatures stay unchanged.
`TransactionReceipt` is sealed and contains strings, long revision/cashAmount, and int amount only.
`TransactionEvent` is sealed and contains strings, long revision, and List<string> consumers only.
Optimize CopyValue for these DTOs and their concrete List types without reflection per history entry.
Preserve null entries, exact order, every field, null consumers, and two-way mutable-object isolation.
The generic reflective copier must continue to handle all other DTO types unchanged.
No sharing receipt/event objects between source and snapshot; strings may be shared because immutable.
Schema tests must fail if either optimized DTO later acquires a mutable field the fast path does not copy.
Use the existing cached Clone delegate or explicit copies; no Reflection.Emit, expressions, or JIT-only APIs.

Self-check: `Tools/run_unity.ps1 -Task Tests -TestFilter Checkpoint` must exit 0 with no failed tests.
Preserve its XML under work/city-model-v14-20261010/checkpoint-tests.xml for the main agent.
Check `git diff --check` for owned code, and keep files below 300 lines.
Only the checkpoint agent may run Unity until it reports completion; main agent does read-only analysis.
Keep the V13 package and save schema unchanged. Do not publish, change Player settings, or spawn agents.


---

## SOURCE: V14_POST_PACKAGE_CONTRACT.md

# V14: kiểm tra sau đóng gói và phạm vi công việc

## Đường dẫn cố định

- Workspace: `D:/GAME/TYCOON2`.
- Delivery: `work/city-model-v14-20261010/Delivery`.
- Candidate: `work/city-model-v14-20261010/Candidate`.
- Source list: `work/city-model-v14-20261010/SourceList.txt`.
- Extraction: `work/city-model-v14-20261010/PackageSmoke`.
- Report: `work/city-model-v14-20261010/Delivery/POST_PACKAGE_CHECKS.json`.

## Hợp đồng kiểm tra

`verify(root: Path, delivery: Path, candidate: Path, extraction: Path) -> dict`

- Không sửa, ghi đè hoặc tạo lại bất kỳ ZIP/manifest đã có.
- Xác minh SHA256, số file và tổng byte của cả ba ZIP theo manifest.
- Xác minh CRC và tên entry an toàn, không trùng tên khi bỏ phân biệt hoa thường.
- Windows chỉ chứa runtime và tài liệu QA; không có source code, save, journal hoặc debug symbol.
- Entry runtime Windows phải khớp byte với Candidate; tài liệu đóng gói khớp checkout tại lúc kiểm tra.
- Source phải khớp SourceList và byte checkout; phát hiện Git LFS pointer chưa mở rộng.
- Evidence phải không chứa save/journal/private và có ảnh PNG thực tế.
- Chỉ giải nén Windows vào đường dẫn mới bên dưới workspace/work; từ chối đường dẫn đã tồn tại.
- Chỉ viết report khi các bước đã đạt; ghi rõ kiểm byte không phải bằng chứng Player đã chạy.
- Các file mới dưới 300 dòng, một câu lệnh mỗi dòng; giữ nguyên nguồn/game/save hiện có.

## Format report

`schemaVersion: 1`, `verifiedUtc: ISO UTC`, `passed: bool`, `packages: object`.
`windows: {runtimeFilesMatched, documentFilesMatched, extraction, executable}`.
`source: {filesMatched, lfsPointersFound}`.
`evidence: {fileCount, pngCount, privateFilesFound}`.
`playerExecuted: false`, `stable165Accepted: false`.

## Quyền sở hữu

- Agent kiểm gói: Tools/verify_city_delivery.py, Tools/test_verify_city_delivery.py,
  Docs/V14_POST_PACKAGE_CHECKS.md và output report/PackageSmoke nêu trên.
- Root: chẩn đoán hiệu năng/NPC, chạy Player sau kiểm gói, QA và handoff mới.
- Agent không chạy Player, không thay runtime, không sửa handoff, không spawn agent khác.

## Self-check

`python Tools/test_verify_city_delivery.py`: fixture hợp lệ và gói lỗi phải được kiểm đúng.
`python Tools/verify_city_delivery.py`: report đạt và giải nén khớp Candidate.
Không coi PASS kiểm gói là nghiệm thu 165 FPS hoặc toàn bộ mục tiêu.


---

## SOURCE: V15_CHECKPOINT_COPY_CONTRACT.md

# Đối chứng V15: sao chép history cho checkpoint

## Mục tiêu

Giảm thời gian checkpoint trên main thread, giữ toàn bộ receipt/event để chống giao dịch trùng.
Không kết luận đạt 165 FPS từ benchmark riêng hoặc từ trung bình FPS.

## Phạm vi thay đổi cho phép

- CheckpointSnapshot.cs: fast path Receipt và Event, không thay generic fallback.
- Receipt: sao chép đủ 9 field trực tiếp bằng object initializer.
- Event: gọi ShallowCopy đã có rồi sao chép consumers riêng; giữ null hoặc empty đúng nghĩa.
- Không cắt history, đổi schema, đổi thứ tự, sửa tiền, bỏ event hoặc chia sẻ list mutable.
- Không thay đồ họa, số NPC, tốc độ game, logic journal, thời gian autosave hoặc scene.
- Không ghi đè Candidate/Delivery V14; build mới dưới work/city-model-v15-20261010.

## Kiểm chứng đúng dữ liệu

CheckpointHistoryTests khóa 9 field receipt và 6 field event; tất cả case phải đạt.
Giữ money/revision/cashAmount 64 bit, null entry, list order và two-way isolation.
Chạy thêm checkpoint async/recovery/save tests hiện hành trước khi build.

## Kiểm chứng Player

- Dùng cùng checkpoint, binary IL2CPP, Normal window, 1080p, Balanced, Manual165.
- Mỗi lượt ghi CPU marker, operation timing, FrameTiming và ảnh không blank.
- Giữ native full-result cùng report, kể cả khi có kiểm tra NPC thất bại do góc nhìn.
- Đối chứng snapshot bằng cùng SavePath input hash; không dùng checkpoint cuối lượt mới làm đầu lượt sau.
- Chỉ tính scope Gameplay sau restore/navigation; vẫn báo hitch startup riêng.
- So thời gian checkpoint thật; không suy ra CPU utilization từ milliseconds.
- Nếu fast path không cải thiện hoặc sai dữ liệu, hoàn nguyên riêng thay đổi V15 đó.

## Dữ liệu nền đã quan sát

ordinary-play-20261010T092458912Z có 21.285 receipt và 21.285 outbox event.
TransactionSnapshot 13,621 ms trong gameplay, 11,581 ms khi kết thúc ca.
Tổng SaveGame.Checkpoint tương ứng 18,351 và 13,774 ms.
Native profile ca này chưa đạt animation NPC; camera/player x244,79 z-28,01 ngoài khu farm.
Đây là giả thuyết cần đối chứng, không phải bằng chứng toàn bộ NPC đang lỗi animation.

## Điều kiện nghiệm thu tổng

Kiểm gói và source PASS không phải nghiệm thu 165 FPS.
Mục tiêu thành phố/model/NPC/Player/bằng chứng vẫn giữ nguyên toàn bộ phạm vi.


---

## SOURCE: V16_HISTORY_CAPTURE_CONTRACT.md

# V16: history bất biến cho checkpoint nền

## Mục tiêu và số liệu nền

V15 ABBA không có cải thiện FPS lặp lại; fast path V15 đã được hoàn nguyên riêng.
V14/V15 đều không đạt stable165; không đổi đồ họa, đội ngũ, tốc độ hoặc history để đạt gate.
Checkpoint farm có 9739 receipt/outbox event; snapshot gameplay khoảng 10-16 ms.
Main thread chụp world và history cùng revision; worker tạo DTO rồi serialize save hiện hành.
Không đổi schema, ID, tiền 64 bit, journal/recovery hoặc API snapshot cũ.

## API mới cho module history

Namespace Tycoon, `internal sealed class CheckpointHistoryCapture`.
`internal static CheckpointHistoryCapture Create(List<TransactionReceipt> receipts,
    List<TransactionEvent> events)`.
`internal void Materialize(TransactionState ownedState)`.

- Create chạy khi caller giữ lock TransactionCore; chỉ dữ liệu managed, không dùng Unity API.
- Receipt/event chụp đủ field bằng private readonly struct arrays, không giữ DTO reference nguồn.
- Consumer string được chụp vào một flat array cùng offset/count/null flag cho từng event.
- Không tạo một DTO/list consumer cho mỗi record trên main thread.
- String bất biến có thể giữ reference; mọi list/array mutable nguồn phải độc lập.
- Giữ null history list, empty list, null entry, order, duplicate entry và null consumer element.
- Giữ đủ 9 field receipt và 6 field event; revision/cashAmount là long, không qua double.
- Materialize tạo receipt/event DTO và consumer list mới trong ownedState.
- Có thể Materialize lại vào state khác; hai output và nguồn đều độc lập hai chiều.
- Module dưới 300 dòng; comments tiếng Việt ngắn cho đoạn khó.

## Quyền sở hữu

- Agent: CheckpointHistoryCapture.cs/.meta và CheckpointHistoryCaptureTests.cs/.meta.
- Runtime path: Assets/_Game/Scripts/Runtime.
- Test path: Assets/_Game/Tests/Editor.
- Agent được viết Docs/V16_HISTORY_MODULE.md và work/city-model-v16-20261010/history-tests.xml/.log.
- Agent không sửa file hiện có, không sửa production integration, không chạy Player hoặc spawn agent.
- Root tích hợp sau khi agent hoàn tất: CheckpointSnapshot, RuntimeTransactionBindings,
  GameplayTransactionStore và CheckpointWriter; giữ fault-path đồng bộ và drain/recovery.
- Trong lúc agent self-check Native tests, root không chạy Player/build/test khác.

## Kiểm chứng module

Tests dùng reflection cho internal class như CheckpointHistoryTests hiện hành.
Test đủ field, tiền lớn hơn 2^53, revision gần long.MaxValue, null/empty/order/duplicate.
Test đổi nguồn sau Create, đổi output sau Materialize, Materialize hai lần và history 20000 entries.
Không assert performance bằng ngưỡng thời gian trong unit tests.
Native Unity EditMode filter Tycoon.Tests.CheckpointHistoryCaptureTests phải PASS.
Unity 6000.6.3f1 tại C:/Program Files/Unity/Hub/Editor, chạy Hidden batchmode/nographics.
Output tests ở work/city-model-v16-20261010; không ghi đè kết quả V14/V15.

## Nghiệm thu tích hợp thuộc root

Chụp state không history cùng revision, giữ scalar/world độc lập rồi worker Materialize history.
Fault path Materialize trước ghi đồng bộ; failure vẫn hiện qua checkpoint.Observe/drain.
Worker không đọc world sống hoặc TransactionCore sau Create.
Full save/recovery regressions rồi IL2CPP Player cùng checkpoint/quality/frame cap.
Chỉ giữ thay đổi nếu thời gian checkpoint thực tế cải thiện và giao dịch/recovery giữ đúng.
Không gọi module PASS là nghiệm thu toàn thành phố hoặc 165 FPS.


---

## SOURCE: V16_HISTORY_MODULE.md

# V16: module capture history

Ngày kiểm chứng: 2026-10-10. Phạm vi: module managed độc lập và native Unity EditMode tests.

## API và quyền sở hữu

- `CheckpointHistoryCapture.Create(List<TransactionReceipt>, List<TransactionEvent>)` chạy khi caller giữ lock.
- `Materialize(TransactionState ownedState)` thay hai history list trong state do worker sở hữu.
- Capture giữ mảng `readonly struct` riêng cho receipt/event và một mảng string consumer phẳng.
- Mỗi event giữ offset/count/null flag; không giữ DTO hoặc list nguồn.
- `Create` chỉ cấp phát các mảng và capture, không tạo DTO hoặc consumer list theo từng record.
- `Materialize` tạo DTO/list mới mỗi lần, cho phép worker độc lập đọc cùng capture bất biến.
- Null list, empty list, null entry, duplicate/order, null consumer và mọi field được bảo toàn.
- Revision/cashAmount luôn là `long`; không chuyển qua JSON/double trong module này.
- Các field world/scalar khác của ownedState không bị thay đổi.

## Files

- `Assets/_Game/Scripts/Runtime/CheckpointHistoryCapture.cs` và `.meta`: module, 162 dòng C#.
- `Assets/_Game/Tests/Editor/CheckpointHistoryCaptureTests.cs` và `.meta`: 248 dòng C#.
- Cả hai file C# không có dòng trên 120 ký tự.

## Kiểm chứng native

Unity `6000.6.3f1`, chạy bằng `Start-Process -WindowStyle Hidden`, process PID 33664, exit code 0.
Executable: `C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe`.

```text
-batchmode -nographics -projectPath D:/GAME/TYCOON2
-runTests -testPlatform EditMode -testFilter Tycoon.Tests.CheckpointHistoryCaptureTests
-testResults D:/GAME/TYCOON2/work/city-model-v16-20261010/history-tests.xml
-logFile D:/GAME/TYCOON2/work/city-model-v16-20261010/history-tests.log
```

Kết quả XML: **12 passed, 0 failed, 0 skipped**, duration `0.7748742` giây.
Tests dùng reflection để truy cập internal API và kiểm tra:

- Bốn tổ hợp null/empty của hai history list.
- Mọi field receipt/event, null record/scalar/consumer, thứ tự và duplicate reference.
- Tiền/revision tại `9007199254740993`, `long.MaxValue`, `long.MinValue`.
- Sửa mọi scalar nguồn và consumer/list sau Create nhưng trước Materialize trên worker.
- Hai worker Materialize cùng capture; sửa output/nguồn không ảnh hưởng nhau hoặc lần Materialize sau.
- Materialize thay history trong ownedState và giữ world/scalar đang có.
- 20.000 receipt và event với null record, null/empty/nonempty consumer xen kẽ để kiểm tra offset.

## Giới hạn nghiệm thu

Module chưa được nối vào production trong phạm vi này. Root tiếp tục chụp world/history cùng revision,
vật chất hóa trên worker, giữ fault-path/drain/recovery rồi chạy save/recovery và IL2CPP Player.
Kết quả module không chứng minh cải thiện checkpoint/FPS hoặc stable165; cần phép đo Player tương ứng.
Không có lỗi mở trong phạm vi module.


---

## SOURCE: V17_HITCH_CAPTURE_REVIEW_CONTRACT.md

# V17: điều tra hitch và ảnh QA trên bản V16

## Phạm vi chung

- Checkout D:/GAME/TYCOON2, Unity6000.6.3f1/URP17.6, giữ toàn bộ thay đổi hiện có.
- V16 chỉ là candidate, chưa đạt stable165. Không chỉnh ngưỡng hoặc loại frame theo độ chậm.
- Player T114554647Z đã terminal exit0, không khởi động lại job vì hết thời gian quan sát.
- Hai phần độc lập, chỉ đọc source/runtime evidence và viết tài liệu riêng.
- Không chạy Unity, Player, benchmark hoặc sửa source; subagent không tạo subagent.

## Đầu vào bất biến

Directory work/city-expansion-20261008/ordinary-play-20261010T114554647Z.
Candidate work/city-model-v16-20261010/Candidate/TycoonTown.exe.
ordinary-play-report.json: averageFps, p95FrameMs, p99FrameMs, worstFrameMs,
framesOver16Ms, framesOver33Ms, framesOver100Ms, durationSeconds, stable165FrameBudget.
operation-timings.csv: frame, second, operation, milliseconds.
ordinary-play-native-long-frames.csv: sampleFrame, second, marker, milliseconds.
ordinary-play-native-cpu.json: markers[] name/count/totalMs/maximumMs và renderCounters[].
ordinary-play-cpu.json: đọc schema thực tế, không đoán từ tên file.
Không giả định sampleFrame của native là cùng frame của operation CSV trước khi đọc collector.
Không cộng timer cha/con chồng nhau và không đổi milliseconds thành phần trăm tải phần cứng.

## Phần A: CPU và hitch

Được tạo/sửa duy nhất Docs/V17_HITCH_TIMELINE.md, dưới300 dòng và120 ký tự mỗi dòng.
Đọc CityNativeCpuProbe, CityOperationTiming và các recorder liên quan để kiểm tra mốc thời gian.
Xác định nguyên nhân được chứng minh, nguyên nhân chỉ là giả thuyết và dữ liệu còn thiếu.
Đối chiếu hai frame trên100ms và các frame trên33ms với source và các phase QA.
Không kết luận screenshot, GC, autosave hoặc OS scheduling là nguyên nhân nếu thiếu dữ liệu.
Nêu một thay đổi nhỏ có bằng chứng hỗ trợ mạnh nhất cùng cách kiểm tra cần thiết.

## Phần B: chụp ảnh QA

Được tạo/sửa duy nhất Docs/V17_CAPTURE_REVIEW.md, dưới300 dòng và120 ký tự mỗi dòng.
Đọc đường chụp ảnh thật dùng bởi CityPlaySession/Movement/Motion và đường recording FPS.
Phân biệt QA capture với autosave hoặc thao tác gameplay thật; giữ đầy đủ bằng chứng ảnh.
Nếu đề xuất asyncGPU/readback/encoding, kiểm chứng API bằng docs Unity chính thức và ghi nguồn.
Nêu thread safety, lifetime RenderTexture/buffer, orientation, UI và cách báo lỗi ghi ảnh.
Không thay implementation, không tải plugin và không dùng API theo trí nhớ chưa kiểm chứng.

## Tự kiểm tra và bàn giao

Đọc lại tài liệu mình viết, kiểm tra số liệu với nguồn, dưới300 dòng và không khẳng định165 đã đạt.
Chỉ báo cáo một lần tối đa15 dòng: file đổi, nguồn đã kiểm, kết luận, vấn đề còn mở.
Không đọc lại hay thay tài liệu của phần còn lại; mọi file ngoài ownership chỉ đọc.


---

## SOURCE: V17_HITCH_TIMELINE.md

# V17 hitch timeline (V16 candidate)

## Scope and clock rules

Capture: `ordinary-play-20261010T114554647Z` (terminal exit 0). This is evidence from
one ordinary-play run; it does not prove stable 165 FPS. No Player or benchmark was rerun.

`ordinary-play-report.json` records `Time.unscaledDeltaTime` per frame. Its `hitches` rows
are emitted from `CityPlaySession.Update` with `Time.frameCount - 1` and realtime seconds.
`ordinary-play-native-long-frames.csv` is different: `CityNativeCpuProbe.LateUpdate`
reads `ProfilerRecorder.LastValue` and labels the sample with the current `Time.frameCount`
and realtime clock. It is a marker sample, not a second frame-time recorder.

`operation-timings.csv` uses `Stopwatch.GetTimestamp()` for elapsed milliseconds, but labels
the completed scope with Unity's current frame and realtime seconds. Nested scopes overlap;
parent and child values must not be added. `ordinary-play-cpu.json` is the custom per-frame
probe output and has `maximumFrame` fields, whereas `ordinary-play-native-cpu.json` stores
native marker aggregates. These clocks and labels are correlated by nearby frame/time, not
assumed identical.

## Run-level result

- 141,632 frames / 881.634 s; average 160.647 FPS; p95 7.076 ms; p99 9.365 ms.
- Worst report frame 431.648 ms; 64 frames >16 ms, 9 >33 ms, 2 >100 ms.
- Average CPU/GPU timings are 4.884/1.933 ms for 821 valid samples.
- `stable165FrameBudget=false`; the run is near 165 FPS but is not a stable-165 pass.

## Correlated hitch rows

| Report frame/time | report ms | native evidence | custom operation evidence | assessment |
|---|---:|---|---|---|
| 3 / 4.008 s | 431.648 | no matching usable native row | none | Cause unresolved. |
| 4 / 4.113 s | 106.395 | Main Thread 106.389; PlayerLoop 106.386 | none | Both paths see it. |
| 10516 / 68.349 s | 79.768 | Main Thread/PlayerLoop 79.783/79.794 | none | Attribution missing. |
| 10878-10879 / 70.625-70.709 s | 84.834/84.980 | 85.488/84.949 and 84.951 | none | Attribution missing. |
| 16166 / 102.979 s | 36.862 | PlayerLoop 36.853 | Mod 3.170; visual 3.073; save 5.833 | Timers are partial. |
| 16167 / 102.994 s | 56.634 | PlayerLoop 56.629 | Workers 15.906; visuals 0.626 | Strong worker association. |
| 16168 / 102.995 s | below 33 ms | Update 48.051; Loop 84.949 | Workers 4.946; visuals 0.732 | Partial timer. |
| 80172 / 507.311 s | 59.211 | PlayerLoop 59.217 | none | Attribution missing. |
| 121259 / 759.375 s | 45.509 | PlayerLoop 45.515 | none | Attribution missing. |
The native list also has 134879 / 843.506 s at 30.014 ms, below the report's >33 ms
threshold. It is near the checkpoint at frame 134878: `SaveGame.Checkpoint` 19.383 ms,
including `TransactionSnapshot` 11.522 ms. This is supporting evidence for checkpoint
cost, not proof that the complete 30 ms frame is checkpoint work.

## What is proved

1. The dominant measured stall is on the CPU/main-thread side: every corroborated long
   frame has a native `PlayerLoop`/`Main Thread` sample, while the run average GPU time is
   1.933 ms and `Gfx.WaitForPresentOnGfxThread` peaks at 15.463 ms. This does not prove a
   GPU cause.
2. The 102.979-102.995 s cluster coincides with the ModMaximum action and deferred worker
   and visual work. `Workers.BuildPending` is intentionally capped at two workers per
   frame, and `UpgradeVisuals.BuildPending` is a deferred LateUpdate queue. The association
   is strong for this cluster, but the timers are not additive and do not cover all native
   script-update time.
3. Checkpoint work grows with the city: `SaveGame.Checkpoint` is 5.804 ms at 63.420 s,
   7.910 ms at 183.432 s, 11.137 ms at 663.470 s, 17.963 ms at 783.477 s, and 19.383 ms
   at 843.492 s. `TransactionSnapshot` grows from 3.918 to 11.522 ms. This supports
   checkpoint/snapshot cost as a recurring small-frame contributor, not as the cause of
   the 431.648 ms startup outlier.
4. `GC.Collect` is zero in native aggregate data. That rules out an observed explicit
   `GC.Collect` marker, but does not prove that no allocation or collection occurred.

## Hypotheses and missing evidence

- The 431.648 ms report row is likely outside the usable native probe sample window or a
  frame-label/recorder warm-up mismatch: native starts in `BeginCityProfile`, samples in
  `LateUpdate`, and `LastValue` is not a frame-time value. The adjacent report frame 4 is
  independently corroborated at 106.389 ms. There is no evidence to name screenshot,
  autosave, GC, OS scheduling, or GPU work as the 431 ms cause.
- The 431.648 vs 106.389 ms difference (325.259 ms) cannot be assigned to a hidden
  operation: no operation row spans frame 3/4, and native markers do not expose the missing
  wait. A future capture needs a frame ID plus per-frame native marker values collected at
  the same callback, and explicit probe start/stop markers.
- Frames 10516, 10878-10879, 80172, and 121259 have no matching operation timer. Their
  likely script/worker/render causes remain hypotheses until a same-frame breakdown exists.
- The custom CPU file's maxima (for example `GameSession.Update` 19.446 ms at frame 134878,
  `WorkerAgent.Update` 17.172 ms at 66181) are useful phase clues, but are not proof that
  those phases equal the full native frame duration.

## One small next fix (recommendation only)

Reduce checkpoint snapshot work first: make one bounded `TransactionSnapshot` copy per
checkpoint and reuse that immutable snapshot for the enclosing save path, while retaining
save semantics. The repeated growth and the 19.383 ms checkpoint near the late long frame
are the strongest recurring evidence for a small, reviewable change. Do not claim it fixes
startup or the 102.995 s worker/visual cluster.

Verification for that change: run the same ordinary tour once with the existing FPS gate
and unchanged save cadence; compare `SaveGame.Checkpoint`, `TransactionSnapshot`, report
p95/worst, and >33/>100 ms counts. Add a same-frame native marker export (frame ID,
PlayerLoop, Main Thread, Update.ScriptRunBehaviourUpdate) so the next capture can test
whether checkpoint reductions affect a native hitch. Keep report, native, and operation
clocks separate and do not sum overlapping timers.

## Sources checked

- `Docs/V17_HITCH_CAPTURE_REVIEW_CONTRACT.md`
- `Assets/_Game/Scripts/Runtime/CityNativeCpuProbe.cs`
- `Assets/_Game/Scripts/Runtime/CityOperationTiming.cs`
- `Assets/_Game/Scripts/Runtime/CityPlayReport.cs`
- `Assets/_Game/Scripts/Runtime/CityPlaySession.cs`
- `Assets/_Game/Scripts/Runtime/CityPlayResume.cs`
- `Assets/_Game/Scripts/Runtime/GameSession.cs`, `GameSessionWorkers.cs`,
  `UpgradeVisualQueue.cs`, `DevelopmentAssistance.cs`
- Capture JSON/CSV: report, hitches, native long frames, native CPU, custom CPU,
  operation timings, timeline, navigation and mobility evidence.





