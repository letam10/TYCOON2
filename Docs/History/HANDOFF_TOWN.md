# Lịch sử bàn giao trước audit 07/10/2026

Chỉ để đối chiếu lịch sử; trạng thái hiện hành ở ../CODEX_HANDOFF.md.

## Công đoạn 14 — hoàn tất thay asset và world dressing — 2026-10-05

**Hoàn tất phạm vi audit/thay asset local được giao. Công đoạn 14 toàn bộ vẫn chưa nghiệm thu.** Không thiết kế lại gameplay core, không tạo project mới.

### Thay đổi cuối

- Thay hình ảnh 11 SKU qua cùng catalog key; stock/carry/customer goods vẫn dùng ItemPool/InventoryStack và owner thật. Nhân vật/vật nuôi có rig, animation hiện có được giữ.
- Quầy POS, kệ kho/thùng trống, bàn bán hàng, trough, chuồng, oven/kitchen, hood, bàn ghế, đồ dùng bếp và cảnh quan dùng model phù hợp đã có ở ASSET. Processing machines chuyên dụng vẫn giữ model hiện có và Rotor.
- Crop soil/lúa/lá được chuẩn hóa scale/pivot; hàng rào khớp pen boundary. Các prop phía sau, planter và cảnh quan không có collider. Đã dời sink Restaurant khỏi waiting point, đưa stock visual ra trước rack và giữ toàn bộ station/interaction/work/queue coordinates.
- Ánh sáng/palette giảm cháy sáng, cây xanh/đất nâu/lúa vàng; sàn cream/sage đọc rõ zone. Xoay mặt oven/fridge đúng hướng; nâng label máy có hood. Sửa mặt đất starter chồng nhau gây sọc nhấp nháy; ảnh kiểm tra cuối hết sọc.
- `TableMealView` chỉ đọc basket/phase/cleaning: đĩa trống lúc gọi món, món khi thật sự đã nhận, đĩa bẩn khi cần dọn. Không tạo/trừ thêm hàng, tiền hoặc cập nhật state.
- File runtime chính: `WorldDressing.cs`, `WorldFactory.cs`, `FinalBusinesses.cs`, `QaAssetLibrary.cs`, `QaDriver.cs`; CLI `Tools/run_player.ps1`; preset xem art `mod/test/asset-preview.json`. Preset chỉ ghi save QA riêng và không được dùng làm bằng chứng balance/progression.

### Kiểm chứng

| Kiểm chứng | Kết quả | Bằng chứng |
|---|---|---|
| Bảo toàn ASSET gốc | 7.553/7.553 SHA giữ nguyên | `work/art-refresh/source-preservation.json` |
| Thư viện Unity | 48/48 model, bounds/pivot/material/collider pass; 12.372 triangles | `work/art-refresh/unity-import.json` |
| Chạy lại converter | 0 model export lại | `work/art-refresh/repeat-convert.log` |
| Windows build cuối | Succeeded, 0 errors, 2 warnings, 117.716.703 bytes; 14:03 UTC | `work/art-refresh/Build/build-report.json` |
| EditMode hiện có | 118/118 pass, 0 fail/skip; 13:31 UTC | `QA/editmode-results.xml` |
| Asset tour RTX 4060/D3D11, 1080p | 172 checks pass, 0 runtime errors; 11/11 ảnh không blank/pink, đã xem từng ảnh | `work/art-refresh/player-assets-20261005T133111975Z/asset-library-report.json`, `work/art-refresh/image-checks.json` |
| Gameplay mới từ ví 0 | Gieo/tưới/grow/harvest/carry/serve/Collect pass, 18 checks/0 runtime errors; lượt cuối thực thu 20 xu, lượt trước 40 xu | `work/stage14/player-stage14visual-20261005T140520156Z/stage14-visual-report.json` |
| Layout cuối trên Player | 424 reachable, 0 overlap/failure | `work/stage14/player-stage14layout-20261005T140520156Z/layout-details.json` |
| Load save QA cũ với visual mới | Pass, wallet/stock không thay bởi tour; source chỉ đọc, fixture tạo ở output riêng | `work/art-refresh/player-assets-20261005T131608073Z/asset-library-report.json` |

Build để chơi: `work/art-refresh/Build/TYCOON2.exe`. Audit/mapping/license: `Docs/ASSET_AUDIT.md`, `Docs/asset-library-import.json`. Source/preset là metadata local; không gửi runtime save hoặc raw drop chưa track lên GitHub.

### GitHub, tiến trình và file trung gian

- `bdf34b7` — `feat: import curated local tycoon asset library`; đã push `origin/main`.
- `dd60c5f` — `feat: dress tycoon areas with local assets and verify play`; đã push `origin/main`. Commit tài liệu cuối có message `docs: finalize local asset refresh handoff`.
- Không đưa `ProjectSettings/QualitySettings.asset` hoặc các raw ASSET có sẵn chưa track vào commit. GPU probe report tạm ở đường dẫn QA có sẵn đã khôi phục về baseline.
- Đối chiếu 30 process records (PID/executable/creation time/parent) với tiến trình hệ thống: 0 process/helper task còn chạy. `process-history.jsonl` có một dòng cuối bị ghi cụt khi hai CLI append cùng lúc; đã đối chiếu thêm file process JSON riêng và các exit result để không bỏ sót PID. Bằng chứng: `work/art-refresh/owned-process-records.json`, `process-cleanup.json`.
- Đã dọn material derivative mồ côi qua Unity khi import. Lệnh dọn các output trung gian đã xác minh bị automatic command review từ chối với `blocked by policy`; không có lý do chi tiết hơn được trả về và không lách chặn. Các thư mục `work/art-refresh/player-assets-20261005T130637596Z`, `player-assets-20261005T131608073Z` và `Build/TYCOON2_BackUpThisFolder_ButDontShipItWithYourGame` còn tồn tại, không có process sử dụng. Không ship thư mục chẩn đoán này.
- Giữ `work/art-refresh/library-audit.json`, reports/source hash/process audit, `player-assets-20261005T133111975Z` cùng ảnh, build và các report Player cuối để xem/reproduce kết quả. Các file này nằm ngoài Git; raw ASSET không bị đổi.

## Thiết kế thị trấn mới — mốc 1 — 2026-10-05

- Đã chuyển player sang proximity 1,2 m/dwell 0,25 s; không sinh action tile. NPC giữ work slot riêng; Collect dùng cọc tiền.
- Nút hỗ trợ +999.999 và mở tuyến dùng transaction có receipt; không cộng revenue/cashCollected/jobs. Gỡ bằng define `TYCOON_DISABLE_ASSIST`.
- Farm cấp 2/3 mở ruộng ngô, đậu nành và ruộng bổ sung; đất vuông có rãnh. Yield snapshot giữ mẻ đang dở khi nâng cấp.
- EditMode/compile: 120/120 pass. Chưa nghiệm thu hình ảnh/Windows Player của mốc này; tiếp tục máy tự động, repair, logistics và polish.
- Commit/push mốc: `feat: add proximity interaction and expanding farm foundation` (hash sẽ được ghi khi chốt các mốc).

## Thiết kế thị trấn mới — mốc 2 — 2026-10-06

- Máy runtime tự chạy do simulation cập nhật; nạp liệu ghi actor khởi tạo, batch chỉ ghi một lần. Recipe snapshot/escrow giữ mẻ Bakery cũ khi đổi sang mixer → dough → oven.
- Thêm feed từ lúa mì/ngô/đậu nành, tương đậu nành, chiết sữa, cừu/len/sợi/vải, mixer và 5 món Restaurant có SKU riêng. Kitchen chọn recipe tự động theo nhu cầu bàn hoặc nút Đổi món.
- Crew chế biến chuyển sang nạp/lấy hàng. Đã cập nhật các fixture/test prototype về prerequisite Farm cấp 2, feed và mixer; không bỏ các assert bảo toàn/idempotency.
- Compile/EditMode: 122/122 pass. Các model bổ sung đang dùng hình dựng local URP; hình ảnh và player loop mới sẽ kiểm chứng tại mốc polish.
- Mốc 1: `9a13764`, đã push. Commit mốc 2: `feat: add automatic production chains and restaurant recipes`.

## Thiết kế thị trấn mới — mốc 3 — 2026-10-06

- Repairer riêng cho Farm/Processing/Bakery/Restaurant; gate station cấp 3 + 30 lần sửa chỉ bằng player, không dùng job sản xuất thay thế.
- Player 5 s, Repairer 30/20/10 s. Player tiếp quản work slot; progress theo tỷ lệ, fee mỗi incident thu một lần, pause/load giữ nguyên input/progress máy.
- Compile/EditMode: 127/127 pass, gồm takeover, ba mức thời gian, save/load và worker sai nghề bị từ chối. Đã cập nhật fixture sửa 8 s cũ thành 5 s.
- Mốc 2: `1b25892`, đã push. Commit mốc 3: `feat: add specialist repair crews and persistent repair progress`.

## Thiết kế thị trấn mới — mốc 4 — 2026-10-06

- Gói xe/tài xế/bến 5.000, Processing + kho cấp 3 + 30 job vận chuyển tay hoàn thành; hand job đếm theo cả lượt giao, không theo mỗi đơn vị. Loader theo Area thuê riêng, giữ thùng/work slot, carry 1/2/3 thùng cùng SKU.
- API Pack/Claim/Load/Unload/SelectRoute/Dispatch/Tick dùng transaction. Thùng một SKU tối đa 6, xe 6 vị trí; reserve toàn bộ kho đích trước departure. Xe rỗng có thể đến kho nguồn; tuyến tự động tùy chọn, không tự bật.
- Hàng có owner Crate + parent dock/worker/truck. Snapshot lưu đường, khoảng cách đã chạy, reservations, box/crew/jobs. Chuyển thùng không tạo bản sao hàng. UI chọn hai kho/gửi xe, model xe và icon trên thùng đã có.
- Compile/EditMode: 131/131 pass, gồm gate 29→30, half delivery không tăng job, full destination giữ cargo, save/load xe giữa chuyến và retry unload không clone.
- Mốc 3: `1169255`, đã push. Commit mốc 4: `feat: add owned cargo crates and truck logistics`.
- Còn mốc 5: bố trí lại town/đường theo TruckRoutes, spawn ngoài đường chính, UI 2×, visual/animation, Windows Player + fixture QA ngắn. Chưa coi route/layout/Player của mốc 4 đã nghiệm thu.

## Thiết kế thị trấn mới — mốc 5 và chốt bản điều chỉnh — 2026-10-06

Đã triển khai năm mốc trên project hiện có. Giữ inventory/ownership, stable ID, Input System/gamepad, pooling, transaction journal và Save v2; không tạo project mới. Gameplay máy tự chạy thay thế yêu cầu đứng vận hành của prototype.

### Hệ thống và file chính

- `ProximityTarget`, `PlayerInteraction`, `InteractionFocusView`: dừng 0,25 s, khoảng cách mặt vật thể 1,2 m, raycast chặn xuyên tường, khóa hướng kho trong một lượt. Không sinh action tile. Quầy dùng FIFO và transaction giao hàng chung; cash chỉ thu bởi player ở cọc tiền. Nguyên liệu cũ không còn thuộc recipe vẫn lấy lại được; kitchen ưu tiên output của món hiện tại.
- `WorldFactory`, `TownLayout`, `WorldDressing`, `FarmPlotView`, `TownArt`: ruộng vuông/rãnh, cây ngô/đậu, Farm 1/2/3 và yield snapshot; chuồng cừu và máy mới. Dời kho/Processing/Market/Bakery; đường chính 6 m, logistics 5 m, đường khách riêng, spawn x=104 ngoài giới hạn player x=58. Pad nằm ngoài footprint và có model cây/máy/kho/người, tên và giá rõ. Các trục nâng cấp mới tái dùng pad cho cấp 3; giữ ID pad cũ.
- `Definitions`, `TransactionCore`, `AutomaticMachines`, `RepairJobs`, `DevelopmentAssistance`: feed ba nguyên liệu, milk bottler, soy sauce, wool/yarn/cloth, mixer/dough/oven, năm món có SKU riêng. Quality, speed, capacity riêng; máy 36→54→72, kho 72→144→216, không lấy station level để tăng tất cả. Animal speed độc lập với care ×2. Repair player 5 s, Repairer 30/20/10 s, normalized progress và một fee/incident.
- `WorkerAgent`: sửa đúng Role + Area, nhường player; processor/baker xoay vòng các việc nạp/lấy, ưu tiên kho trong khu và thành phẩm giữa máy. Animal care có thể chuẩn bị/lấy feed từ mixer. Cashier dùng cả hàng quầy; Loader có bounded repath, không giữ nhiều thùng khác SKU.
- `CargoState`, `CargoTransactions`, `CargoDock`, `TruckRoutes`, `TruckLogistics`: gói xe/tài xế/bến 5.000, kho Processing cấp 3 + 30 hand jobs; thùng một SKU ≤6, xe 6 thùng, source/destination rõ, destination reservation trước gửi, optional repeat mặc định tắt. Xe nhường người qua đường; một hệ thống ghi transform. Loader theo Area thuê riêng.
- `GameSession`, `RuntimeTransactions`, `CommerceDirector`, `RestaurantDirector`: restore actors trước simulation/input, clock đơn hàng đóng băng khi restore, bảo toàn FIFO/partial/price/cargo/repair/contributions. Migration layout không dịch Driver/Loader đã dùng dock mới. Không offline progression. Prototype save không bị overwrite.
- `GameHud`, `Art`: font/icon tăng 2×, reflow ba độ phân giải; wallet/revenue/thực thu/loss, stock/reserved theo khu và bảng chọn SKU có scroll, crew Role + Area, chọn nguồn/đích/gửi xe. Nút đổi món tại kitchen. Nút hỗ trợ luôn trên HUD.
- `QaTownRedesign`, `TownRedesignTests`, `CargoRedesignTests`, `Tools/run_player.ps1`, `Tools/process_record.ps1`: fixture sát ngưỡng và kiểm thử input thật; log tiến trình có mutex để không ghi cụt khi chạy độc lập đồng thời.

### Kiểm chứng bản cuối

| Kiểm tra | Kết quả / bằng chứng |
|---|---|
| Compile + EditMode | 135/135 pass, 0 fail; `QA/editmode-results.xml` |
| Windows build | Succeeded, 0 errors / 2 warnings, 117.843.403 bytes, 20:37 UTC ngày 05-10; `work/town-redesign/Build/build-report.json` |
| Player cuối RTX 4060 / D3D11 | 70 checks, 0 runtime errors, khoảng 3 phút 11 giây; `work/town-redesign/player-town-20261005T203933317Z/town-redesign-report.json` |
| Ví 0 | Tự gieo/tưới/grow/harvest/carry → khách đi từ ngoài đường chính → FIFO/payment → player Collect; kiểm tra stack đúng quantity, tiền hỗ trợ không tăng revenue/thực thu/orders; hỗ trợ mở cả feed mixer |
| Fixture mua xe | File `mod/test/town-redesign-near-thresholds.json`: 4.990 xu, 29 hand jobs; player giao túi để đạt job 30, bán/Collect để đạt 5.000, mua đủ xe+tài xế; đóng/chất/gửi/dỡ thật |
| Save giữa chuyến | Ba lần load giữ distance/cargo/reservation, không clone; unload đúng kho và giữ tổng quantity. Relaunch riêng pass tại `work/town-redesign/player-townload-20261005T204401358Z` (đường dẫn chính xác xem `work/player-townload-process.json`) |
| Feed/repair | Ba nguyên liệu thật → feed; break → một fee → player sửa → rời/load/tiếp tục, job solo 29→30; unit thêm sai role, takeover, 30/20/10 và retry |
| Restaurant | Cook và giao đủ năm SKU đúng bàn → eat → payment → dirty → player clean → Collect; receipt/value snapshot dùng cùng OrderState |
| Navigation/UI | 361 điểm reachable, không action tile; 30 khách thử 15 s simulation, cap/repath pass; restore khách còn đang vào town. 720p/1080p/1440p và các ảnh khu đã được xem; UI lớn không cắt dòng wallet/carry/title/support |

Fixture là dữ liệu QA riêng, không phải bằng chứng chơi hết progression bằng cash sản xuất. Có `-FixtureOnly` để bỏ lượt ví 0 đã kiểm tra và chạy nhanh sát mốc; không chạy benchmark/soak/campaign 3–4 giờ. `TownLoad -LoadPath` chỉ đọc nguồn và ghi save riêng tại output. Giữ các mốc 500/1.000 → 2.000 → 12.000 → 65.000 → 100.000 → 80.000 và order/batch prerequisites. Chỉ dùng `cashCollected` cho tốc độ tiến triển; `assistedCash` không tính. Chưa coi 180–240 phút đã pass.

### Chơi, kiểm tra và gỡ nút hỗ trợ

- Build: `work/town-redesign/Build/TYCOON2.exe`; không ship thư mục chẩn đoán `TYCOON2_BackUpThisFolder_ButDontShipItWithYourGame`.
- CLI: `Tools/run_unity.ps1 -Task Tests/Build`, `Tools/run_player.ps1 -Task Town [-FixtureOnly]`, `-Task TownLayout`, `-Task TownLoad -LoadPath <save QA>`. Các harness Stage14 cũ còn dùng helper zone, chưa dùng để nghiệm thu proximity mới; chọn Town cho bản này.
- Gỡ hỗ trợ: thêm **`TYCOON_DISABLE_ASSIST`** vào Scripting Define Symbols của Windows rồi build lại. `DevelopmentAssistance.Enabled=false` bỏ HUD và từ chối command runtime mới; journal cũ vẫn replay được để không mất tài sản đã có. QA mới kiểm tra nút cần build có hỗ trợ. Tiền trợ giúp và flag `assisted` đã lưu riêng.
- Mốc 1 `9a13764`, mốc 2 `1b25892`, mốc 3 `1169255`, mốc 4 `e98b4b9`: đã push `origin/main`. Mốc 5: **`e854d9d` — `feat: finish rural town layout and verify player workflows`**, đã push `origin/main`. Bản ghi Git/cleanup cuối nằm ở commit tài liệu `docs: record town redesign acceptance and cleanup status`.
- Giữ ngoài commit: QualitySettings có sẵn, raw ASSET chưa track, build/save/log QA. Log thử lỗi khởi tạo đã giữ excerpt ở `work/town-redesign/initialization-failure.txt`; bản chạy tiếp đã thay log lớn bằng log mới.

## Những gì chưa thực hiện được / chưa nghiệm thu — cập nhật cuối Công đoạn 14

- **Công đoạn 14: chưa pass lượt liên tục new game 0 → Restaurant bằng sản xuất/thu tiền thực tế, không nạp state. Chưa pass mục tiêu mở toàn chuỗi trong 180–240 phút.** Preset sát ngưỡng xác nhận từng gate/gameplay nhưng không thay thế hai tiêu chí này; chưa chốt acceptance toàn Công đoạn 14.
- **Công đoạn 14 — đã xử lý nhiều lần nhưng lượt progression đầy đủ vẫn chưa pass:** lượt đầu góp nhầm pad khi đi ngang; lượt tiếp theo mang nhầm cà rốt khi cần sữa; lượt resume bị tolerance dừng 0,25 m quá chặt. Đã sửa dwell, chỉ thao tác khi dừng, bỏ pause tại NavMesh corner trung gian, kiểm tra SKU lại trong harness và tolerance cuối 0,4 m. Player visual/near-threshold cuối pass các hành vi sửa; lượt hợp lệ dài nhất đạt Farm Shop khoảng phút 38,3 rồi dừng ở khoảng phút 77, chưa tới Restaurant. Theo yêu cầu mới đã chuyển sang dữ liệu sát mốc để chốt kiểm tra nhanh.
- **Công đoạn 07/10/11/12:** chưa có lượt Player cuối nghiệm thu tất cả đội Role + Area cùng làm việc, mọi tuyến vận chuyển full destination/thiếu nguồn/reservation đang đi sau relaunch, player+worker cùng phục vụ nhiều SKU trong cảnh thật. Có regression transaction và các lượt trước, chưa coi là acceptance trọn hệ thống.
- **Công đoạn 13:** chưa nghiệm thu Player trọn Rush Hour báo trước 15 giây → rush 90 giây → hồi phục; breakdown giữa batch → phí/repair tại zone → worker resume. Ca 30 khách hiện tại chỉ kiểm tra congestion 15 giây simulation, không chứng minh toàn sự kiện hoặc traffic dài hạn.
- **Công đoạn 08:** relaunch đã xác nhận ví/thực thu/unlock; partial timeout, máy đang chạy và load lặp đã pass trong cùng process. Chưa có relaunch riêng bao phủ đồng thời mọi worker job, cargo/reservation, herd và event đang dở.
- **Công đoạn 12/14:** vòng Restaurant cook/serve/clean bằng player đã pass; chưa nghiệm thu toàn bộ recipe Bread/Cake cùng crew Bakery/Restaurant trong Player cuối. Clip work hiện là biến thể từ animation nguồn; độ tự nhiên từng action, mật độ props và mức giống video reference chưa được người dùng nghiệm thu ở toàn bộ khu.
- **Công đoạn 14:** full-stock, shortage, service/worker bottleneck có kiểm tra core và mã chờ; chưa có đủ ca Player cuối cho toàn tổ hợp. Không chạy soak/benchmark/FPS dài hạn theo yêu cầu test gọn.
- **Công đoạn 04/05 — acceptance cũ về purchase từng bị thử nhiều lần chưa pass:** các mốc purchase nay đã pass bằng fixture sát ngưỡng trên Windows Player; yêu cầu toàn progression từ 0 vẫn nằm ở Công đoạn 14 chưa pass như trên. Lỗi fixture Input System trong EditMode đã được thay bằng kiểm tra input/interaction trong Player thật; bộ EditMode cuối 118/118 pass.
- **Dọn file tạm, xuyên nhiều công đoạn:** lệnh PowerShell dọn vẫn bị command policy chặn. Các artifact được liệt kê ở trên để lần tiếp tục xử lý đúng đường dẫn; không có process task còn giữ chúng mở.

- **Công đoạn 14 — asset refresh:** đã hoàn tất 48 model phù hợp và compile/play như bảng trên. Chưa nghiệm thu nghệ thuật với người dùng, chưa retarget/deformation-test mọi character/animation trong raw library, chưa kiểm tra FPS 165 hoặc congestion dài với art mới. Rig/clip cũ được giữ; hướng nhìn/pose ngồi theo ghế mới và độ tự nhiên mọi action vẫn cần nghiệm thu riêng.
- **CLI/process audit:** một dòng history bị cụt do append đồng thời; audit cuối đã khôi phục phạm vi từ process JSON riêng và xác nhận 0 process task. Chưa sửa logger để hỗ trợ append đồng thời; lần tiếp tục nên chạy CLI tuần tự hoặc bổ sung lock.
- **Dọn output asset:** automatic command review vẫn chặn việc xóa ba thư mục trung gian/chẩn đoán nêu trên. Đây là phần cleanup chưa thực hiện được; các report/ảnh giữ để đối chiếu đã được nêu mục đích riêng.

### Bản điều chỉnh thị trấn — chưa nghiệm thu / thử nhiều lần chưa pass

- **Chưa pass** lượt liên tục ví 0 → Restaurant và mục tiêu 180–240 phút bằng cash thực thu; chưa chạy campaign dài theo yêu cầu kiểm thử gọn. Giá/gate và các trục tách biệt đã triển khai; cần nghiệm thu cân bằng trong lượt chơi dài riêng.
- **Chưa nghiệm thu Player toàn tổ hợp** các nghề/khu cùng hoạt động, Loader/Repairer mọi mức nâng cấp, mọi recipe Processing/Bakery dưới full-stock/shortage/congestion, crew đổi việc sau relaunch và Rush Hour đủ 15+90 s. Có core tests và vòng player/manual mới; không nhận là đã pass mọi tổ hợp.
- **Chưa nghiệm thu nghệ thuật với người dùng:** hình dựng local của cừu/máy/xe/món mới, độ tự nhiên mọi action và mức giống video reference. Giữ rig/clip cũ; chưa retarget toàn raw library. Không benchmark FPS hoặc soak dài.
- **Các lượt thử đã thất bại nhiều lần nhưng ca cuối đã pass:** thiếu key model thùng khi bootstrap; fixture dùng CaptureSaveData không có core; wait point bị wall/storage chặn; input harness dừng ngoài 1,2 m; harness đi thẳng vào xe; view carry kiểm tra trước LateUpdate; fixture diner thiếu owner. Đã sửa và 70 checks / 0 runtime errors pass trong Player cuối. Đây không còn là lỗi đang mở của những ca đã chạy.
- **Các yêu cầu vẫn chưa pass sau nhiều lượt:** toàn progression và toàn mục tiêu thời lượng như danh sách Công đoạn 14 bên trên. Các harness zone cũ chưa chuyển hết sang proximity; tool compile/build/Town đã phù hợp, không dùng harness cũ làm bằng chứng gameplay mới.
- **Cleanup:** automatic command review từ chối lệnh xóa bằng `blocked by policy`, không nêu thêm lý do; không lách chặn. Các artifact debug/empty resources và thư mục thử thất bại được giữ ngoài Git nếu chưa dọn được. Audit `work/town-redesign/process-audit.json` đối chiếu 280 records/PID/executable/creation time: 0 Unity/Player task còn chạy; chỉ lệnh PowerShell kiểm tra chính nó xuất hiện và đã thoát sau khi ghi report. Không có helper hoặc tiến trình cần giữ. Thư mục debug chính xác: `work/town-redesign/Build/TYCOON2_BackUpThisFolder_ButDontShipItWithYourGame`; lệnh dọn đã bị automatic approval review chặn (`blocked by policy`), không lách. Alias Resources tạm của bộ test đã hết sau TearDown; không xóa catalog gốc.
