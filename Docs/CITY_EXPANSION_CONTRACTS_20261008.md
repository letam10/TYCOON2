# Hợp đồng mở rộng thành phố — 08/10/2026

## Mục tiêu đang thực hiện

Mở rộng tổng diện tích nền đúng 2 lần: 200 × 136 thành 282,842712 × 192,333044.
Tách các khu thành nơi độc lập có cảnh quan, kiến trúc, đường và hoạt động riêng.
Nâng vật phẩm cầm tay theo hai video, không sửa model/rig nhân vật.
Mục tiêu khoảng 165 FPS ở 1080p trên RTX 4060; chỉ đo khi chơi bình thường vài phút.
Commit/push GitHub và cập nhật hướng dẫn khi có bằng chứng Player.

## API dùng chung

`Runtime/CityDistricts.cs` là nguồn tọa độ chính thức, revision 2.
`CityDistricts.Offset(string area): Vector3` dịch từ tọa độ revision 1.
`CityDistricts.Center(string area): Vector3` trả trung tâm khu.
`CityDistricts.Footprint(string area): Bounds` trả phạm vi khu.
`CityDistricts.WorldBounds: Bounds` và `GroundWidth/GroundDepth` giới hạn toàn bản đồ.
`CityDistricts.PlayerSpawn`, `Gate`, `CustomerSpawn(int)` dùng khi tạo actor.
Các diện tích mới không thay số lượng hàng, giá, công thức hoặc mã giao dịch.

Main sẽ cung cấp `CityStreetNetwork.Bounds(): IEnumerable<Bounds>` cho đường xe.
Đường chính nằm tại z=-45 và z=29, trục đông x=102, nhánh bắc x=46.
Đường nhánh: x=-82,z29..65; x=-35,z10..29; x=21,z-7..29; z=91,x9..102.
Phần trang trí phải tránh đường và chừa lối đi xung quanh footprint gameplay.
Không đặt collider trang trí trong vùng trạm, ô nâng cấp, quầy, hàng chờ hoặc bến xe.
Các facade ở mép khu; mặt trước khu giữ thông thoáng cho camera và thao tác.

## Phân công file, không chồng phạm vi

### Agent city_environment

Chỉ tạo/sửa các file mới `Runtime/CityDressing*.cs`, `CityArchitecture*.cs`,
`CityProps*.cs`, `CityLandscape*.cs`, `CityStaticGeometry*.cs` và meta tương ứng.
API đầu vào `CityDressing.Build(GameSession game, Transform world): void`.
Main gọi sau khi trạm và đường đã ở tọa độ mới, trước tạo NavMesh.
Các GameObject phong cảnh nằm dưới root `CityScenery`; ưu tiên mesh gộp theo vật liệu/chunk.
Thêm greenhouse/silo/irrigation, patio, parking, loading yard, phố/công viên/bờ sông.
Mỗi khu cần khác biệt về silhouette, vật liệu và chi tiết chức năng nhìn thấy trong camera chơi.
Chỉ trang trí, không tạo item owner, giao dịch, mod, NPC/xe gameplay hoặc thay model nhân vật.
Main sở hữu layout, migration, đường, xe/NPC hoạt động và integration.

### Agent held_goods

Chỉ sửa `HandItemModels.cs`, `HandItemProduce.cs`, `HandItemProducts.cs`,
`CarryPresentation.cs`, `InventoryStack.cs`, `CarryTransferVisual.cs` và meta.
Có thể tạo `HandItemSurface*.cs`, `HandItemMesh*.cs`, shader dưới `Shaders/Carry*`,
và test `CityCarryTests.cs`; cập nhật `CarryReadabilityTests.cs`, `CarryVisualTests.cs` nếu cần.
Giữ `HandItemModels.Build(string, Transform): GameObject`, `Supports(string): bool`,
`VisibleSize`, InventoryStack public fields/Refresh/VisibleCount/VisibleItemId.
Giữ CarryPresentation.Attach/AttachWorker/HandPoint/LastItemId và luật một SKU hoặc tiền.
Nâng chi tiết đủ 27 SKU + tiền + kiện, tăng độ rõ kích thước so với 0,38 hiện tại.
Xếp gọn trước tay theo video, chuyển động mượt khi chạy/quay, nhãn trên đỉnh chồng.
Chỉ animation sau giao dịch; không tăng/giảm inventory/tiền, không thay sức mang đã chốt.
Giảm submesh/material của từng món, dùng mesh/material dùng chung, không tạo mesh mỗi frame.
Không sửa ActorView, model/rig nhân vật, PlayerController, UI hoặc dữ liệu giao dịch.

## Tham chiếu

Hai video gốc trong `C:/Users/TAM/Downloads/` đã đọc thật, lưu SHA-256.
Khung gốc/contact sheet: `work/city-expansion-20261008/references/`.
`reference-1-contact.jpg` cho cầm/xếp/chuyển hàng; video 2 cho hàng lớn, chi tiết và khu chức năng.
Giữ hướng vật liệu/ánh sáng bán thực tế, nhân vật giữ phong cách hiện có.

## Kiểm chứng và phối hợp

Tối đa hai subagent, không spawn thêm. Không chạy Unity song song, không commit hoặc push trong agent.
Mỗi file khoảng dưới 300 dòng, mỗi câu lệnh một dòng; comment tiếng Việt chỗ phức tạp.
Tự kiểm bằng `git diff --check` và kiểm tra chữ ký/public API theo hợp đồng.
Main chạy EditMode, build Windows và lượt chơi bình thường 3–5 phút sau integration.
Agent báo một lần tối đa 15 dòng: file, self-check, vấn đề còn mở; không báo status liên tục.
Main lưu gameplay frame time, ảnh/video và dữ liệu tương tác; không chạy stress benchmark.

## Kiểm tra tích hợp tiếp theo

Agent migration chỉ sửa `Assets/_Game/Scripts/Runtime/CitySaveMigration.cs`,
được tạo `CityWorkerMigration.cs` và `Assets/_Game/Tests/Editor/CityMigrationTests.cs` cùng meta.
Giữ `CitySaveMigration.Apply(SaveData): void`, `PositionOffset(Vector3): Vector3`.
Migration revision 1 sang 2 chạy đúng một lần qua TownLayout; không đổi hàng/tiền/receipt/command ID.
Workers ở bến xe phải theo nguồn/đích logistics hoặc khu đội nghề, không chọn khu gần nhất một cách sai.
Owner copies phải khớp save actor; xe đang chạy giữ phần đường đã đi, dùng đường mới.
Main sở hữu TownLayout, Player/Qa harness và CityLayoutTests, agent không sửa các file đó.

Agent traffic chỉ sửa `CityTraffic.cs`, `CityLife.cs`, được tạo `CityTrafficSafety.cs`
và `Assets/_Game/Tests/Editor/CityTrafficTests.cs` cùng meta.
Giữ CityTraffic.VehicleCount và cách tạo bốn xe; không đổi đường/nhân vật/rig/giao dịch.
Xe phải nhường player, worker, customer, diner, cư dân gần đoạn đường phía trước và xe cùng làn.
Không quét toàn scene mỗi frame, không tạo collider cản đường NPC hoặc đổi số khách thật.
Cư dân là scenery hoạt động, không có inventory/save owner/order.
Giữ việc dừng simulation khi game pause; không tạo deadlock giữa xe khác làn.

Cả hai agent đọc code data thực tế trước khi sửa, giữ file dưới khoảng 300 dòng/120 ký tự.
Tự kiểm `git diff --check`; thêm test regression có ý nghĩa, không chạy Unity đồng thời.
Main chạy toàn EditMode và Player sau khi nhận báo cáo cuối duy nhất, tối đa 15 dòng.
Không spawn agent khác, không commit/push, không sửa ngoài phạm vi, không chạy benchmark.

## Tối ưu projection inventory

Agent inventory chỉ sửa `Assets/_Game/Scripts/Runtime/Inventory.cs` và được tạo
`Assets/_Game/Tests/Editor/CityInventoryProjectionTests.cs` cùng meta.
Giữ `Inventory.Project(TransactionState): void` internal và mọi public API hiện hành.
`Revision` chỉ tăng khi dữ liệu inventory đó thực sự đổi; lệnh không liên quan không làm đổi revision.
So sánh chính xác lượng từng SKU, capacity/singleItem/limits và reservation/incoming, tránh hash có collision.
Không dùng state.revision làm revision của từng inventory, không làm lượng hàng/availability/capacity sai.
Tái dùng buffer nếu cần; không thêm copy JSON hoặc allocation lớn trên mỗi projection.
Root sở hữu RuntimeTransactionProjection và InventoryStack; agent không sửa các file đó.
Regression: lệnh riêng owner khác, đổi SKU dù tổng giữ nguyên, reservation/relay/capacity/limit,
projection lại cùng state, load state có revision bằng nhau nhưng hàng khác nhau.
Không chạy Unity/build/benchmark, không spawn/commit/push; report cuối một lần tối đa 15 dòng.

## Đóng gói

Agent release chỉ tạo `Tools/package_city.py` và có thể tạo `Tools/test_package_city.py`.
Python standard library; CLI `--build <dir> --evidence <dir> --output <dir>`.
Chạy từ dự án, các thư mục input/output phải ở trong `work/`; không xóa hoặc ghi đè gói có sẵn.
Tạo Windows.zip từ build, Source.zip từ git ls-files đọc asset thực trong checkout (không đóng LFS pointer),
Evidence.zip từ ảnh/log/báo cáo/CSV, loại save/journal khỏi evidence và debug/backup khỏi Windows.
Đưa README, hướng dẫn thành phố và QA/city-acceptance-20261008.json vào gói Windows nếu có.
Manifest ghi HEAD commit, số file, dung lượng, SHA-256 từng zip; kiểm tra CRC và entry exe/Data/DLL.
Giữ tất cả file nguồn/asset có sẵn; không stage/commit/push hoặc chạy Unity/game/benchmark.
Tự kiểm bằng --help và smoke test thư mục temp bên trong work (source zip có asset thật).
File dưới khoảng 300 dòng/120 ký tự; report cuối duy nhất tối đa 15 dòng.

## Clip chuyển động cầm hàng

Agent motion chỉ tạo `Tools/encode_city_motion.py`.
CLI `--frames <dir> --output <dir>`, input/output ở trong `work/`; không xóa hoặc ghi đè file có sẵn.
Đọc `frame-NNNN.png` theo số tăng dần, giữ nguyên hình gốc; không dùng video tham chiếu làm video trò chơi.
Tạo `carry-motion.mp4` bằng OpenCV mp4v, 10 fps, cùng độ phân giải ảnh gốc.
Tạo `carry-motion.gif` bằng Pillow, rộng tối đa 960 px, 10 fps; giữ tỷ lệ và toàn bộ khung hình.
Đọc lại MP4 kiểm frame count/kích thước; kiểm GIF số frame và thời gian, ghi manifest JSON gồm SHA-256.
Chỉ giảm kích thước ảnh preview GIF, không sửa ảnh PNG nguồn hoặc thay đổi báo cáo FPS.
Python hệ thống đã có OpenCV/Pillow; không cài package hoặc tìm codec bên ngoài.
Tự kiểm bằng --help, py_compile và chuỗi PNG thử trong work; không chạy Unity/game/benchmark.
Không sửa source Runtime/docs khác, không spawn/commit/push; báo cáo cuối duy nhất tối đa 15 dòng.

## Rà chi phí UI và navigation

Agent review chỉ đọc code Runtime và báo cáo chơi 1080p, không sửa file.
Xem GameHud*, Station.cs, MachineStation.cs, OrderBubble*, BillboardLabel và Navigation*.
Tìm chi phí lặp mỗi frame sau khi mod tối đa: JSON copy, refresh chữ/mesh ẩn, path hoặc ResetPath dư.
Contract gameplay: 78 workers, đủ khách/cư dân/xe; giữ model/rig/assets nhân vật và giao dịch/save.
Không đề xuất giảm dân số, đóng băng simulation, giảm độ phân giải hoặc benchmark để đạt FPS.
Main sở hữu mọi sửa Runtime, profiler và guide; review không chạy Unity/Player hoặc spawn.
Report cuối một lần tối đa 15 dòng, kèm file:dòng, bằng chứng nguồn và giới hạn suy luận.

## Báo cáo nghiệm thu tổng hợp

Agent acceptance chỉ tạo `Tools/create_city_acceptance.py`, không sửa Runtime/docs/QA hiện tại.
CLI bắt buộc `--fresh <dir> --resume <dir> --visual <dir> --migration <dir>`
`--tests <xml> --build <dir> --legacy-source <file> --output <json>`.
Đọc dữ liệu JSON/XML và hash bằng standard library; không tải mạng, cài package, chạy Unity/Player.
Nguồn ordinary có `ordinary-play-report.json`, resume có thêm `city-restore.json`;
visual có báo cáo QA và `physical-resolutions.json`; migration có báo cáo QA.
Đọc file báo cáo thật trong work để xác định tên/schema; không giả định mọi báo cáo cùng schema.
Mục tiêu 165 FPS, giới hạn chấp nhận gần mục tiêu 148.5 FPS; kiểm riêng FPS fullCity ở resume.
Fresh và resume phải passed, errors/failures rỗng, duration >=240s, 1920x1080, GPU chứa RTX 4060.
Resume phải 78 workers, fullCitySeconds >=180, fullCityAverageFps >=148.5.
Test XML phải Passed/0 failed; build Succeeded/0 errors; visual và migration phải passed.
Resolution phải đủ 1280x720/1920x1080/2560x1440/3840x2160, PNG/render đúng kích thước,
renderScale 1, sample failures rỗng; giữ nativeWindowValidated/offscreen thành giới hạn công khai.
Restore giữ từng số dư/tổng hàng, layout 1->2, giữ receipts; giữ hash save nguồn.
JSON output gồm passed, checks, software/hardware, số liệu thô, paths/hashes và limitations.
Không ghi nội dung save/journal/token vào report, không đổi số FPS hoặc gán fixture là chơi thật.
Output phải file mới trong QA hoặc work; input phải nằm trong checkout, không ghi đè.
Tự kiểm --help, py_compile và smoke với report fixture, kiểm ca fullCity FPS thấp bị từ chối.
Không stage/commit/push/spawn; file dưới khoảng 300 dòng/120 ký tự; report cuối <=15 dòng.
# Phản hồi nền, cỏ và độ mượt — 08/10

User feedback: paving/road/block textures repeat densely and cause visual discomfort;
ground lacks visible grass; gameplay and animation feel jerky. Preserve the character model/rig/assets.

## Surface and grass ownership

- Agent owns only `TownSurfaceTexture.cs`, `TownSurfaceMaterials.cs`, `TownSurface.shader`.
- It may create `CityGroundCover.cs`, `CityGrassGeometry.cs`, `TownGrass.shader` and matching .meta files.
- It may create `CityGroundCoverTests.cs` and matching .meta; no existing test fixture edits.
- Root owns `CityDressing.cs` integration, camera/carry/gameplay, UI performance, save/load and other tests.
- Existing surface APIs stay unchanged: `Get(string surface)` and `Get(string surface, string tint)`.
- New entry point: `public static void CityGroundCover.Build(Transform parent, CityDressingSpace space)`.
- Root calls it after all existing scenery placement and before geometry.Finish().
- Use `space.Take` to protect roads, stations, docks, queue corridors and interaction areas.
- Grass must have no collider and must be excluded from NavMesh sources.
- Grass uses shared materials and meshes grouped into chunks; no per-blade GameObjects or Update calls.
- Preserve navigation through every current district and do not change transactions/save IDs or character assets.
- Lower contrast and remove dense repeated paving/ceramic grids; retain useful material differences.
- Grass is visible 3D ground cover with irregular size, spacing and color, bounded for target RTX 4060.
- Any wind is subtle and uses the shader, with no sharp flicker or screen-space checker detail.
- New files below about 300 lines, one statement per line and about 120 columns.
- Self-check source diff and simple geometry tests; do not launch Unity/Player while root owns execution.
- Report once with changed files, checks and integration issues; never spawn another agent.
# Review lần khựng khi bật mod — bổ sung 08/10

- Root sở hữu thay đổi runtime và đóng gói. Reviewer chỉ đọc source và bằng chứng.
- Entry point: `DevelopmentAssistance.ApplyMaximum(GameSession game) : bool`.
- Giao dịch: `game.Transactions.TryExecute(TransactionCommand, out TransactionResult)`.
- Thành công gọi `UpgradeModelView.RefreshAll(GameSession game, bool animate)`.
- HUD: `GameHudModMenu.cs`; progression: `GameRules.cs`; store: `GameplayTransactionStore.cs`.
- Bằng chứng: ordinary-play-20261008T034544802Z, 243,32 giây; FPS 157,57/155,54 full city.
- Frame 781,44 ms ở giây 72,30, ngay sau bấm mod; có 4 frame trên 100 ms.
- Không chạy benchmark, Unity, Player, compiler hoặc profiler khi root đang kiểm chứng.
- Giữ flush trước publish, save/journal/receipt, đủ nhân viên và hình ảnh cấp; không giảm đồ họa.
- Reviewer chỉ được tạo `Docs/CITY_MOD_HITCH_REVIEW_20261008.md` tối đa 150 dòng.
- Báo cáo một lần: 2–3 nguyên nhân có dẫn dòng, giải pháp tối thiểu, rủi ro và test cần thiết.
