# CODEX_HANDOFF

## V21 — Hoàn thiện bản đồ, phím, slot lưu và bàn giao Windows (14/10/2026)

**Đây là mục hiện hành.** V20 trở xuống và
`Docs/world-map-20261014/PAUSED_HANDOFF.md` là lịch sử.
Yêu cầu tiếp tục ngày 14/10 cho phép kiểm chức năng nhanh, build và commit/push GitHub.
Không chạy benchmark, soak hoặc campaign dài.

### Bản chơi và điều khiển

- Mở `D:/GAME/TYCOON2/Builds/Windows/TYCOON2.exe`; giữ nguyên cả thư mục Windows.
- WASD di chuyển, Shift chạy 10 m/s khi tay trống, M mở bản đồ, F5 lưu nhanh.
  Minimap dưới trái; bấm minimap hoặc Cài đặt → Bản đồ cũng mở map và tạm dừng game.
- Có 5 điểm dịch chuyển: TYCOON/nông trại, công viên, nhà ga, bãi biển và bến cảng.
- Cài đặt → Đổi phím: 10 thao tác, chặn trùng/phím dành riêng, Esc hủy, khôi phục mặc định.
- Cài đặt hoặc Menu → Slot lưu: 3 slot độc lập, tải/lưu bản sao/chơi mới,
  xác nhận trước ghi đè. Slot 1 giữ đường dẫn save cũ; F5 dùng slot đang chọn.
  Checkpoint trước được giữ ở `.previous.json`; game mới xuất hiện trước quầy TYCOON.

### Đã hoàn thiện từ checkpoint dừng

- Kiểm tra mở lại Player bằng tiến trình mới: slot đang dùng, phím tùy chỉnh,
  map và tiến trình lưu được phục hồi; dùng save QA riêng, không thay save người dùng.
- Bổ sung tiếng Anh cho thông báo động của map, đổi phím, lưu/tải/chơi mới và xác nhận.
  Sửa nhãn lối tắt Settings bị mất do vùng chữ thấp hơn chiều cao glyph.
- Kiểm menu Việt/Anh ở 1280×720, 1920×1080, 2560×1440 bằng cửa sổ thật.
  3840×2160 dùng render target riêng do màn hình máy này giới hạn cửa sổ ở 2560×1600;
  ảnh và assertion 4K ghi rõ `offscreen`, không nhận là native 4K.
- Sửa chữ hướng dẫn chìm trên nền cát bằng nền HUD tối. Khép khe đứng 2,5 cm
  giữa cát và nước: cát chuyển tuyến tính từ cao độ đất 0,0275 m tới mép nước 0,0525 m.
  Test đã tái hiện lỗi trước sửa; giữ x/z bờ cong và không đổi collider/gameplay.
- Sửa test deferred checkpoint so sánh cùng một vòng JSON như Snapshot, tránh sai số
  double 1 ULP khi so chuỗi. Giữ kiểm history/isolation/idempotence, không sửa runtime.
- Đưa các thay đổi source còn chờ của city/gameplay/Shift/save và asset phụ thuộc
  vào cùng đợt bàn giao. Chỉ chọn 76 file nguồn art/dependency/license được manifest dùng;
  không đưa toàn bộ kho download chưa dùng vào Git.

### Kiểm chứng trên bản build cuối

Build: `Tools/run_unity.ps1 -Task Build`, exit 0, Succeeded lúc
**09:26:55 UTC 14/10/2026**, 118.775.007 byte, **0 errors / 28 warnings**.
Report: `QA/build-report.json` và `Builds/Windows/build-report.json`.

Tests: `Tools/run_unity.ps1 -Task Tests`, exit 0, **935/935 pass, 0 fail/skip**,
09:27:14–09:27:48 UTC. XML: `QA/editmode-results.xml`.
Log build/test: `work/logs/build.log`, `work/logs/tests.log`.

- `Tools/run_world_features.ps1 -OutputPath work/final-20261014/world-features-release`:
  exit 0, **2.370 checks pass**, 0 runtime errors.
- `Tools/run_world_features.ps1 -Load -OutputPath work/final-20261014/world-features-release`:
  exit 0, **9 checks pass**, 0 runtime errors, tiến trình mới.
- `Tools/run_completion.ps1 -Case All -BuildPath Builds/Windows/TYCOON2.exe`:
  exit 0, **699 checks pass**, 0 runtime errors, 09:28:33–09:34:56 UTC (~6 phút 23 giây).
  Report: `work/completion-audit/player-all-20261014T092827598Z/completion-report.json`.
  Kiểm mốc mua/nâng cấp, nghề/crew, kho, sản xuất, truck/loader, repair/Rush và checkpoint.
- `Tools/run_completion.ps1 -Case Load -BuildPath Builds/Windows/TYCOON2.exe -LoadPath
  work/completion-audit/player-all-20261014T092827598Z/completion-midflight.json`:
  exit 0, **52 checks pass**, 0 runtime errors, tiến trình mới.
  Report: `work/completion-audit/player-load-20261014T093506262Z/completion-load-report.json`.
- Đã xem đủ 50 ảnh mới: UI/label đọc được, không cắt chữ, không ảnh trống/magenta;
  nền hướng dẫn và khe bờ đã sửa trong cả ba ảnh bờ biển.
  Báo cáo độc lập: `work/final-20261014/visual-review.json`, passed=true.
  Bản lỗi trước sửa được giữ ở `visual-review-before.json`.
- `Tools/run_sprint.ps1`: exit 0, **57 checks pass**, 0 runtime errors.
  Report: `work/shift-final-20261014/foundation-20261014T093549651Z/foundation-report.json`.
  Shift trái/phải/chéo lần lượt **9,999955 / 10,000006 / 9,999937 m/s**;
  đi thường **4,799976**, cầm hàng **3,799927 / 3,800026 m/s**.
  Đây là đo hành vi di chuyển để kiểm chức năng, không phải benchmark hiệu năng.
- `Tools/run_carrot_settings.ps1 -BuildPath Builds/Windows/TYCOON2.exe`:
  exit 0, **173 checks pass**, 0 runtime errors.
  Report: `work/carrot-settings-20261008/player-20261014T093719190Z/carrot-settings-report.json`.
  Kiểm thu hoạch/giao/bán, cài đặt đồ họa, ngôn ngữ, VSync, FPS overlay và quay lại game.

Source commit đã kiểm: `16e8218eb0a8246e085cedc6b2ded4e3409703b8`.
Audit Git index đạt: 48/48 hash nguồn + 48/48 FBX, đủ 78 đường dẫn dependency/license
(76 download mới), 35 pointer LFS hợp lệ và không phát hiện secret theo bộ mẫu quét.
Report: `work/final-20261014/source-package-audit.json`.

**Gate bàn giao đạt:** 935 EditMode + **3.360 assertion Player**, tất cả lệnh exit 0,
0 runtime errors; 50 ảnh đã được xem. Báo cáo tổng có lệnh, timestamp, nguồn,
giới hạn và hash: `QA/current-delivery-20261014.json`.
Gate tổng `work/final-20261014/write-delivery-report.ps1` đã chạy exit 0.
Repo đích: `https://github.com/letam10/TYCOON2.git`, nhánh `main`;
commit hồ sơ V21 đi sau source commit nêu trên. Không tạo release/site hay đổi visibility.

SHA256 runtime cuối:
`3757843E837684B9F1087406590DEFC086475A19A4474DD54A7A682D56E8C9B2`.
Đường dẫn: `Builds/Windows/TYCOON2_Data/Managed/Tycoon.Runtime.dll`.

### Giới hạn và dữ liệu giữ lại

- Đây là fixture chức năng ngắn, không chứng minh cân bằng campaign 180–240 phút,
  hiệu năng/FPS ổn định hay nghiệm thu toàn bộ art của game. Không benchmark/soak.
- Còn 28 warning có sẵn về Unity object API obsolete và shader TownSurface.
- Kiểm package giữ 11 GUID tham chiếu cũ chưa resolve của URP probe/volume ở baseline;
  không có GUID thiếu mới do đợt này. Build và các gate thực thi quyết định nghiệm thu.
- Giữ nguyên download chưa dùng, reference image, save/journal QA, build và evidence
  lịch sử ở máy. Chúng không phải source cần push; không reset/clean hay xóa dữ liệu.
- `Tools/verify_city_delivery.py` kiểm package V14 lịch sử, không chứng nhận Windows V21.
  Gate V21 là Tests/Build, Player chức năng và báo cáo tổng có timestamp/hash bên trên.

## V20 — Shift 10 m/s, kiểm chức năng nhanh và bản Windows (14/10/2026)

**Mục lịch sử, đã được V21 thay thế.** Bản tại thời điểm V20:
`D:/GAME/TYCOON2/Builds/Windows/TYCOON2.exe`; giữ toàn bộ thư mục Windows cạnh exe.
Không dùng các ZIP/build lịch sử làm bản Shift 10 m/s. Không publish/deploy/push.

### Đã làm

- Giữ Shift trái/phải khi di chuyển: chạy **10 m/s khi tay trống**; đi thường 4,8 m/s,
  cầm hàng/tiền/thùng 3,8 m/s theo luật sẵn có. Gamepad nhấn cần trái cũng chạy 10 m/s.
- HUD Việt/Anh có hướng dẫn Shift. Chạy chéo được chuẩn hóa; nhả phím, pause/resume
  và animation Run/Walk/CarryWalk đã được kiểm qua Input System trong Windows Player.
- Sửa lỗi loader đứng ngoài thân xe nhưng chờ mãi: `TruckHandlingPoint` suy điểm bốc/dỡ
  từ collider xe và bán kính người/agent, lấy cùng điểm NavMesh để đi và xét đến nơi.
  Collider xe vẫn hoạt động; Cargo và All đã kiểm bốc, chở, dỡ hàng thực tế.
- Cập nhật bộ QA cho luật tiền trên tay/két, ClaimCrate trước Load/Unload,
  chờ checkpoint bất đồng bộ hoàn tất trước so sánh save, kiểm riêng hai số dư tiền.
  Thời hạn transport suy từ route/speed; thời gian repair suy từ capability nhân viên.
  Đây là sửa ca kiểm đã lỗi thời, không đổi luật kinh tế hoặc tốc độ nhân viên.
- Chẩn đoán QA không đọc trạng thái NavMesh khi agent tài xế bị tắt; thêm test
  chuyển động thật qua connector processing. Script completion đọc log UTF-8.
- README và DOCS_INDEX trỏ handoff hiện hành; build-report trong Windows đã cập nhật.

### Đã kiểm — cùng bản build cuối, các lệnh đều exit 0

Build: `Tools/run_unity.ps1 -Task Build`, Succeeded lúc **06:39:46 UTC 14/10**,
118.715.759 byte, **0 errors / 28 warnings**. Report: `QA/build-report.json` và
`Builds/Windows/build-report.json`; log: `work/logs/build.log`.
Tests: `Tools/run_unity.ps1 -Task Tests`, **912/912 pass, 0 fail, 0 skip**;
XML `QA/editmode-results.xml` kết thúc **06:46:21 UTC**, log `work/logs/tests.log`.

| Lệnh Player trên Builds/Windows/TYCOON2.exe | Kết quả | Report dưới work |
|---|---|---|
| `Tools/run_sprint.ps1` | 57 checks pass | `shift-final-20261014/foundation-20261014T064912346Z/foundation-report.json` |
| `Tools/run_completion.ps1 -Case All -BuildPath Builds/Windows/TYCOON2.exe` | 699 checks pass | `completion-audit/player-all-20261014T064647766Z/completion-report.json` |
| `Tools/run_completion.ps1 -Case Save -BuildPath Builds/Windows/TYCOON2.exe` | 166 checks pass | `completion-audit/player-save-20261014T064536857Z/completion-report.json` |
| `Tools/run_completion.ps1 -Case Load -BuildPath Builds/Windows/TYCOON2.exe -LoadPath work/completion-audit/player-save-20261014T064536857Z/completion-midflight.json` | 52 checks pass, process mới | `completion-audit/player-load-20261014T064612902Z/completion-load-report.json` |
| `Tools/run_carrot_settings.ps1 -BuildPath Builds/Windows/TYCOON2.exe` | 164 checks pass | `carrot-settings-20261008/player-20261014T065056429Z/carrot-settings-report.json` |

Tất cả các report Player trên có **0 runtime errors**. All chạy khoảng 6 phút 27 giây,
bao phủ mốc mua/nâng cấp tới restaurant, nghề sản xuất/bán hàng/nông trại/chăn nuôi,
thiếu nguyên liệu/kho đầy và phục hồi, vận chuyển liên khu, truck/loader, repair/Rush,
pause, save/load. Các fixture QA dùng simulation 3x và save riêng; save người dùng được giữ.
Load qua process mới giữ tiền/hàng/đơn/reservation/tiến độ/xe và tiếp tục giao hàng,
sửa chữa, dỡ cargo đúng một lần khi lặp command.

Đo khoảng cách/thời gian thật trong `sprint-measurements.json` của Foundation cuối:
Shift trái **9,999966**, phải **10,000013**, chéo **9,999938 m/s**; đi thường **4,800007**;
cầm hàng trái/phải **3,799987 / 3,800028 m/s**. Đã xem ảnh HUD bàn phím
`01a-shift-controls.png` và menu `settings-vi-1920.png`: hình mới rõ, không đen.
Settings kiểm độ phân giải 1280/1920/2560/3840, tiếng Việt/English, chất lượng, VSync,
bật/tắt FPS, lưu cài đặt và trở lại game. Bật overlay chỉ là kiểm chức năng.

Report tổng: `work/shift-final-20261014/final-report.json` có lệnh, thời gian và hash.
SHA256 `Builds/Windows/TYCOON2_Data/Managed/Tycoon.Runtime.dll`:
`0521DC7803AC6476CB2F814F8F458F5F9C671EC0984273F8D2A9E04D00705EAD`.

### Đã dọn work và kiểm riêng sau dọn

- Xóa **31 thư mục build phát sinh cũ**, 4.530.196.437 byte. Từng build có record
  `Task=Build` trỏ đúng output, report Succeeded/errors 0, chỉ chứa runtime;
  đã kiểm hash/inventory không đổi, không process dùng build và không reparse point.
- Xóa archive trùng `work/city-model-current-20261011/Delivery/Evidence.zip`,
  4.429.530.808 byte. Giữ `Delivery-20261011-docs-clean-v2/Evidence.zip` cùng parent.
  Hai archive SHA256 trùng; bản giữ lại đã kiểm CRC đủ 2.459 file và rehash sau dọn đạt.
- Tổng đã xóa **8.959.727.245 byte (~8,96 GB / 8,34 GiB)** theo độ dài file.
  Source, save, log, ảnh, ZIP còn lại và Unity Library được giữ.
- Danh sách chính xác/provenance: `cleanup-build-plan.jsonl`, `cleanup-preflight.json`;
  kết quả hậu kiểm: `cleanup-result.json`, cùng nằm trong `work/shift-final-20261014`.
  Bản Windows hiện hành vẫn nguyên vẹn. Các đường dẫn build cũ trong mục lịch sử có thể
  đã bị dọn; không coi chúng là build hiện hành hoặc tự tạo lại chỉ để hợp thức báo cáo cũ.

### Chưa kiểm / còn lưu ý / thay đổi phạm vi

- Theo yêu cầu mới nhất: **test chức năng nhanh, không benchmark dài**. Không chạy
  soak, tour dài, đo FPS ổn định hoặc campaign cân bằng 180–240 phút ở lượt này.
  Kết quả fixture chức năng không chứng minh thời lượng/cân bằng một campaign từ 0 tiền.
- Không còn lỗi runtime trong các ca đã chạy. Build còn **28 warning** về Unity object
  API obsolete và shader TownSurface; không tuyên bố warning bằng 0 hay mọi lỗi đều hết.
- Không đổi công nghệ/phạm vi game 3D. Giữ điều kiện tay trống khi chạy.
- `Tools/verify_city_delivery.py` là validator gói V14 lịch sử, dùng đường dẫn đã cũ;
  không dùng nó chứng nhận bản Windows V20. Gate hiện hành là Tests/Build và các Player
  command/report trên, đều đã exit 0.

## V19 — Bàn giao kiểm tra toàn bộ project, game và kế hoạch (11/10/2026)

**Mục lịch sử, đã được V20 thay thế.** Các checkpoint V10–V17 ở
phía dưới được giữ làm lịch sử, nhưng không được dùng để ghi đè trạng thái V18.

### 0. Dọn work ngày 11/10/2026

- Đã xóa bản build trung gian `work/build-windows-20261011`; bản hiện hành là
  `Builds/Windows` và staging `work/city-model-current-20261011/CityRepairBuild`.
- Đã xóa `work/city-model-current-20261010`, bản output cũ đã được thay thế.
- Đã xóa `work/city-model-current-20261011/DeliveryEvidence`, vì toàn bộ 2.459 file
  đã đối chiếu với `Delivery-20261011-docs-clean-v2/Evidence.zip`.
- Đã xóa các thư mục rỗng `stage01`, `stage02`, `stage08-save-tests`,
  `stage10-save-tests`, `tests` và `save-recovery` dưới `work`.
- Dung lượng giải phóng khoảng 7.071.843.945 byte. Các gói nén, save, evidence lịch sử
  được handoff tham chiếu và thư mục `items-vehicles-20261008` vẫn được giữ.

### 1. Phạm vi và nguyên tắc đọc handoff

- Checkout duy nhất: D:/GAME/TYCOON2.
- Unity 6000.6.3f1, URP 17.6.0, Windows Player, Direct3D11, chơi đơn offline.
- Hướng hình ảnh đã chốt là bán thực tế: vật liệu rõ hơn, ánh sáng chi tiết hơn,
  cỏ và đất trống trên mặt phẳng, model cầm tay có hình 3D nhận ra được.
- Quy tắc gameplay đã chốt: tay chỉ giữ một loại hàng hoặc tiền; tiền thu nằm trên
  tay; tiền trong két phải rút lên tay trước khi chi; đồ dễ vỡ mới dùng két/khay/hộp/vỉ.
- Người dùng đã bỏ mục tiêu benchmark ổn định 165 FPS. Mọi FPS/tải trong phần dưới chỉ
  là chẩn đoán của một lượt Player, không phải điều kiện chặn bàn giao.
- Không được nhầm “có source” với “đã chạy Player”, “gallery nhiều góc” với “mọi model
  được duyệt bằng mắt”, hoặc “ảnh không đen” với “đã chứng minh hiển thị liên tục”.
- Không xóa save, evidence hoặc work cũ chỉ để làm nhẹ thư mục khi chưa lập danh sách
  lưu trữ riêng. Work hiện có gần 40 GB và gồm cả lịch sử cần đối chiếu.
- Thứ tự đọc tài liệu được ghi tại `Docs/DOCS_INDEX.md`. 34 contract/findings cũ đã được
  gộp nguyên văn vào `Docs/History/LEGACY_DOCS_20261011.md`; các file package bắt buộc
  vẫn giữ nguyên đường dẫn.

### 2. Kiểm kê checkout tại thời điểm bàn giao

| Khu vực | Số file | Dung lượng byte | Vai trò |
|---|---:|---:|---|
| Assets | 1.731 | 246.731.509 | Unity source, art, runtime, test và prefab |
| ASSET | 7.553 | 429.442.129 | Thư viện nguồn đã tải, giữ license/hash |
| Docs | 105 trước khi thêm handoff này | 3.238.346 | Kế hoạch, contract, audit và hướng dẫn |
| Documentation | 2 | 6.428 | REPLAN và REFERENCE |
| Tools | 49 | 441.288 | build, test, Player, gallery, package và validator |
| QA | 102 | 36.986.087 | XML, report và evidence nền |
| ProjectSettings | 27 | 60.786 | Unity settings, URP/quality/input |
| Packages | 2 | 6.649 | manifest và lock file |
| work | 20.349 | 39.828.026.483 | build, save QA, log, report, ảnh, zip lịch sử |

Git HEAD đã kiểm tra là 3ae6638cb9ce4de5c586ae05c7c2aaea9cebbb80 với message
Upgrade city layout, physical cargo, workforce and transit. Checkout dirty có chủ ý:
nhiều runtime/test/doc/art và file tải về đang sửa hoặc untracked từ các công đoạn trước.
Không coi dirty status là lỗi phát hành và không reset các file không liên quan.

Các thư mục work quan trọng:

- city-expansion-20261008: lượt Player gameplay bình thường và evidence thành phố.
- items-vehicles-20261009: gallery item, vehicle, dining, rail và city model.
- city-model-current-20261011: build hiện hành, source staging, Delivery và evidence đã gom.
- city-model-v10 đến v18, stage*, shadow*, ocean*, save-recovery, tests: lịch sử đối chứng,
  không tự coi là build hiện hành.

### 3. Bản đồ kế hoạch và trạng thái thực hiện

| Tài liệu/kế hoạch | Nội dung | Trạng thái trong V18 |
|---|---|---|
| PLAN.md | vòng chơi từ 0 tiền, farm, shop, processing, supermarket, bakery, restaurant | Luồng runtime và Player chính đã kiểm; cân bằng 180–240 phút chưa chạy dài |
| Documentation/REPLAN.md | luật đơn hàng FIFO, 90 giây, giao từng phần, save v2, ownership | Các primitive và regression hiện có; không dùng mốc lịch sử chưa xác minh |
| CITY_EXPANSION_20261008 | thành phố x2, đường chính, cỏ, NPC, xe buýt/coach/taxi | Đã dựng và đi qua trong Player hiện hành |
| PHYSICAL_CARRY/ITEM_MODEL/CARRY_CONTAINERS | hàng 3D, xếp 2x3 mỗi tầng, chỉ đồ dễ vỡ dùng container | Đã có source và gallery; cần giữ report khi chỉnh tiếp |
| V10–V13 model/city/ocean | landmark, nhà, bến tàu, nước, gallery nhiều góc | Đã hợp nhất vào source current; gallery current đạt native report |
| V14–V17 performance/history/capture | checkpoint history, shadow, capture, hitches | Capture path đã sửa; không khôi phục mục tiêu stable165 |
| CITY_REPAIR_CONTRACT_20261011 | sửa route collider và ảnh đen của một run cũ | Đã giải quyết trong build current bằng pedestrian links và capture mới |

### 4. Những phần đã triển khai trong source

**Kinh tế, tiền, két và save**

- GameSession, RuntimeTransactions, GameplayTransactionStore, SafeStation,
  CheckoutCashModels và CheckpointHistoryCapture giữ cashInHand/cashInSafe,
  giao dịch gửi/rút, idempotency và journal.
- Tiền thu từ quầy đi lên tay; tiền trong két không tự chi. HUD tách tiền đang cầm
  và tiền trong két, không dùng nhãn Ví cho luồng mới.
- Mod game có đúng hai lựa chọn: Mod tiền cộng 999.999 vào két và Mod xây dựng tối đa.
  Mod tối đa lấy cấp hiện có, cập nhật hình ảnh và không tạo trùng đối tượng.
- Save cũ replay journal trước rồi migration một lần; giữ hàng đang cầm, tiền quầy,
  tiến độ góp và stable IDs. Không có bằng chứng cho phép xóa save prototype.

**Hàng cầm tay và container**

- CarryItemLayout, CarryPresentation, CarryTransferVisual, HandItemModels,
  HandItemProduce, HandItemProducts và HandItemSurfaceAtlas dựng model 3D khi cầm,
  số lượng trên đầu chồng và hiệu ứng chuyển hàng.
- CarryContainerLayout/Profile/Geometry chỉ bật cho egg, milk, bottled_milk, sauce và
  soy_sauce. Hàng khác cầm trực tiếp; một chai/quả trong container vẫn là một món.
- Xếp hàng theo hai cột, ba hàng mỗi tầng; cà rốt, lúa và hàng dài nằm ngang, giảm
  kích thước cầm theo yêu cầu. Container test hiện hành đã đạt 60/60.

**Ô nâng cấp và UI**

- UpgradeModelParts, UpgradeModelView, UpgradeVisualMotion, UpgradeVisualQueue,
  PurchasePadView và PurchaseIconView cập nhật model theo cấp, animation ngắn,
  tiến độ góp, icon 2D và ẩn pad ở cấp cuối.
- PlayerInteraction và ProximityTarget giữ hold 0,25 giây, góp dần, tiếp tục sau
  khi hiệu ứng hoàn tất; đứng lâu tăng tốc góp theo đường cong đã chốt.
- GameHud, GameHudNotices, GameHudSettings, FpsOverlay và các widget dùng Canvas cố
  định, thông báo ngắn, icon khách, thanh kiên nhẫn và tùy chọn ngôn ngữ/đồ họa/FPS.

**Mặt đất, shader, ánh sáng và bóng**

- TownSurface.shader và TownWater.shader cung cấp cỏ trên mặt phẳng, mảng đất,
  vết đường, đá nhỏ và nước ripple/bọt nhẹ; không dùng khối đá lớn rời làm texture.
- PC_RPAsset.asset và các file render hiện hành dùng URP, shadow cascade thấp và
  occlusion để giảm tải. CityAccessPaths đánh dấu collider lối dẫn là trigger để
  không khóa CharacterController, còn collider nhà/vật cản thật vẫn giữ.
- QaRenderedFrame tách đường URP SingleCameraRequest ra khỏi gameplay, phục hồi camera,
  Canvas, RectTransform và RenderTexture trong finally để ảnh gallery không làm bẩn scene.

**Thành phố, nhà, máy, bàn ghế và giao thông**

- WorldFactory, CityDistricts, CityOuterDistricts, CityStaticGeometry,
  CityCommercialModels, CityCivicModels, CityLeisureModels, CityCoastModels,
  CityHarborHull, CityMarinaModels và CityStreetPropDetails tạo thành phố rộng/dài x2.
- Các landmark đã có trong run current: PoliceStation, FireStation, SeasideBar,
  EntertainmentHall, Playground, FoundersMonument, Harbor, Beach, CoffeeHouse,
  Bank, Hotel, Theater và Marina.
- CityRailNetwork/Station/Train/VehicleModel tạo hai đường ray, ga hai sân và tàu;
  CityPublicTransport và CityVehicleModel tạo CityBus, PassengerCoach, CityTaxi,
  Sedan, SmallTruck, CargoTruck, Tanker, Luxury và Sports.
- DiningFurniture, DinerSeating và DinerSeatingPose đặt bàn tròn với hai ghế,
  bàn vuông với ghế dài và ghế quầy gần mặt bàn. Gallery có ảnh đặt và ảnh model riêng.
- Đường đi chính, bến tàu, cầu và pedestrian links được ưu tiên; NPC không tự chọn
  đường xuyên nhà. CityAccessPaths là điểm sửa route cuối của mốc 11/10.

### 5. Bằng chứng kiểm chứng hiện hành

**Compile, test và build**

- Lệnh test:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Tests
- Kết quả gần nhất: 910/910 EditMode pass, 0 fail, 0 skip, exit 0; XML tại
  QA/editmode-results.xml. Không dùng con số cũ 173/173, 792/792 hoặc 866/866 thay thế.
- Lệnh build:
  powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\run_unity.ps1 -Task Build
  -BuildPath 'work/city-model-current-20261011/CityRepairBuild/TYCOON2.exe'
- Build hiện hành exit 0, executable nằm tại
  work/city-model-current-20261011/CityRepairBuild/TYCOON2.exe.

**Player gameplay tự nhiên**

- Report: work/city-expansion-20261008/ordinary-play-20261010T231833892Z/
  ordinary-play-report.json.
- passed=true, exit 0, 886,233 giây, 155.702 frame, 39 nhân viên, 17 khách,
  4.182,475 m di chuyển, 712 tương tác thành công và 0 runtime errors.
- GPU RTX 4060 Laptop, Direct3D11, 1920x1080. Average 175,690 FPS; full-city
  average 164,331 FPS; P95 8,259 ms; P99 11,377 ms; worst 840,399 ms.
- Average CPU frame 5,923 ms và GPU frame 2,148 ms. Đây là frame time của game,
  không phải phần trăm CPU/GPU, xung CPU/GPU hay nhiệt độ phần cứng.
- Report ghi gpuOcclusionEnabled=true, displaySync=true, displayRefreshHz=165,
  configuredFpsLimit=165 nhưng stable165FrameBudget=false. Không tuyên bố game ổn định
  165 FPS; người dùng đã yêu cầu bỏ gate này.
- Hoàn tất trong report: 377 điểm tương tác, thu hoạch/bán/thu/gửi/rút tiền thật,
  mod tiền, góp ô nâng cấp, sáu khu cũ, 13 landmark mới, hai cầu gỗ và ít nhất bốn
  phút gameplay liên tục. Đây là bounded tour, không phải playthrough 180–240 phút.

**Ảnh, NPC, route và phương tiện**

- capture-validity.json cùng thư mục Player: passed=true, 44 ảnh, 0 ảnh lỗi,
  mọi ảnh 1920x1080, màu khác rỗng.
- npc-motion-evidence.json: 98 NPC quan sát, 87 NPC đi, 82 NPC có animation,
  tổng quãng đường 113.017,253 m, passed=true.
- city-navigation.json: checkedPoints=377, blocked=[].
- city-mobility-evidence.json: 39 worker, 26 profession, capability=1.5;
  CityBus 16/16, PassengerCoach 16/15, CityTaxi 5/4 board/alight, passed=true.
- Run cũ ordinary-play-20261010T160928100Z từng timeout ở (206,0,-123) và có ảnh
  đen do PedestrianLinkCollision cùng WindowStyle Hidden. Đó là evidence lỗi lịch sử,
  không phải trạng thái current; repair docs vẫn lưu nguyên nhân và cách phân biệt.

**Gallery model, item, dining và vehicle**

- City gallery:
  work/items-vehicles-20261009/player-20261010T233459441Z/
  city-models-v11-report.json.
  passed=true, 2.032 checks, 664 ảnh, 0 runtime errors; model đặt có 5 hướng
  front/right/rear/left/top và model riêng có 9 hướng gồm three-quarter.
- Gallery này bao phủ 13 landmark, đồ đường phố, bàn ghế, bến tàu, nước và vehicle
  placed. Đây là kiểm hình học/capture, không thay thế gameplay tự nhiên.
- Item/vehicle gallery:
  work/items-vehicles-20261009/player-20261010T233931748Z/
  items-vehicles-v8-report.json.
  passed=true, 4.218 checks, 1.551 ảnh, 0 runtime errors; có count 1/24/48,
  layout 2x3, chồng hàng, model tiền, container dễ vỡ và nhiều góc.
- vehicle-live-facts.json chứng minh hai tàu có quãng đường khoảng 1.105,625 m và
  1.103,491 m, mỗi tàu 3 board/2 alight; xe công cộng và 6 xe đường bộ có boarding
  và alighting, worldErrors=0. Distance=0 ở xe đường bộ là field placeholder,
  không được gọi là quãng đường đo.
- water-detail-performance.json có bốn mẫu detail 0/1 trong gallery beach; đây là
  ABBA render detail có giới hạn, không phải benchmark 165 hoặc soak test.

### 6. Gói bàn giao và checksum

Manifest: work/city-model-current-20261011/Delivery/manifest.json.

- Windows.zip: 138 file, nén 47.501.942 byte, SHA256
  05960c49dd6331f8107094be2bb8eb0a011ae77c7782c9a612147c5732b9f644, CRC verified.
- Source.zip: 9.618 file, nén 660.437.238 byte, SHA256
  be804b5c7194f0328f001a738d8c064bd45605c14973fd27509917cf57b35fe1, CRC verified.
- Evidence.zip: 2.459 file, nén 4.429.530.808 byte, SHA256
  4428178a8da4c70b4fdebf42245a31c58fe086c31702107fe9295965f130148e, CRC verified.
- SourceList.txt có 9.618 source entries. Delivery ghi tracked_checkout_dirty=true;
  package commit là 3ae6638cb9ce4de5c586ae05c7c2aaea9cebbb80.
- package_city.py đã tạo và kiểm CRC/SHA. verify_city_delivery.py hiện hardcode V14,
  nên không ghi nó là validator của Delivery current. Generic validate_city_capture.ps1
  cũng không phù hợp gallery vì tìm report ở layout ordinary; native reports ở trên mới
  là nguồn cho gallery.

### 7. Điều còn mở hoặc không được tuyên bố

1. Không có stable 165 FPS, nhiệt độ/xung CPU thật hoặc GPU/CPU percent từ report
   current. Chỉ có CPU/GPU frame ms và thông tin GPU/API/display.
2. Chưa có một campaign 180–240 phút từ 0 để chốt cân bằng kinh tế, giá và pacing.
   Tour 886 giây chứng minh luồng tương tác, không chứng minh thời gian mở khóa mục tiêu.
3. Chưa có nghiệm thu độc lập trên mọi độ phân giải cho toàn bộ gameplay. Gallery có
   mẫu 1280x720/1920x1080/2560x1440 trong presentation audit; 44 ảnh Player current
   là 1920x1080.
4. Gallery không tự chứng minh topology, texture PBR hoặc vật liệu đúng cho tất cả
   27 hàng ở mọi góc mắt người. Khi chỉnh model, phải giữ report và chụp lại 8 góc
   riêng/5 góc đặt.
5. Instancing không phải do phím Ctrl+D: nhân bản cùng mesh/material chỉ giúp GPU
   instancing khi renderer, material, SRP Batcher/GPU Resident Drawer và culling cho
   phép. Cần Frame Debugger/Profiler nếu tối ưu tiếp.
6. Samples 4096/1024 là tham số bake/render offline; không tự đổi thành FPS real-time.
   Không giảm shadow/light/sample thêm chỉ để đạt con số FPS đã hủy.
7. Work gần 40 GB chứa nhiều bản lịch sử. Chỉ archive/xóa sau khi tạo retention list
   và xác nhận Delivery current giải nén được; bản bàn giao này không xóa artifact.
8. Worktree dirty và các raw ASSET/download vẫn được giữ. Không reset, clean hoặc
   đổi remote/push nếu người dùng chưa yêu cầu.

### 8. Cách chat sau tiếp tục

1. Đọc phần V18 này, sau đó đọc contract đúng phạm vi trước khi sửa.
2. Mở build current để xem gameplay: TYCOON2.exe trong CityRepairBuild.
3. Nếu sửa runtime, chạy Tests rồi Build; nếu sửa Player/visual, chạy bounded Player
   và ghi report mới cạnh report hiện hành, không ghi đè evidence cũ.
4. Nếu sửa model, chạy gallery items/city, kiểm file count/ảnh không đen và ghi rõ
   góc nào là isolated, góc nào là placed.
5. Nếu sửa NPC/route, phải kiểm destination clearance, final distance, nonblank capture
   và animation metric trong cửa sổ hiển thị; không dùng run hidden/black làm proof.
6. Không chạy benchmark stable165 trừ khi người dùng yêu cầu lại. Có thể ghi frame time
   chẩn đoán trong report mà không biến thành acceptance gate.
7. Cuối công đoạn cập nhật chính file Docs/CODEX_HANDOFF.md, thêm ngày/version và
   liên kết report; giữ lịch sử V10–V17 bên dưới để truy nguyên.

Cập nhật 10/10/2026, Asia/Bangkok. Tiếp tục trực tiếp tại D:\GAME\TYCOON2.
Đây là trạng thái hiện hành; các bản bàn giao cũ đã chuyển nguyên nội dung sang Docs/History.

## V16 checkpoint hiện hành

## Phạm vi kiểm chứng hiện hành

- Theo chỉ đạo mới, không tiếp tục benchmark hoặc dùng mục tiêu 165 FPS ổn định làm điều kiện nghiệm thu.
- Các số FPS/tải/nhiệt đã ghi chỉ giữ làm thông tin chẩn đoán, không dùng để chặn bàn giao chức năng.
- Tiếp tục kiểm tra gameplay, save/load, vật chứa, model, NPC, giao thông, thành phố và gói bàn giao.
- Trace chụp ảnh V17 đã hoàn tác vì lượt Player trace giảm còn 93,837 FPS và GPU frame 11,764 ms.
- Hai tài liệu V17 vẫn giữ bằng chứng chẩn đoán, không coi bản trace là candidate phát hành.
- Vật chứa hiện chỉ hỗ trợ egg, milk, bottled_milk, sauce và soy_sauce; hàng khác cầm trực tiếp.
- CarryContainerTests hiện hành đạt 60/60, 0 lỗi/0 bỏ qua, exit 0; kết quả ở work/carry-container-current/results.xml.
- Test bao phủ số lượng 1,2,5,6,7,13,48; đổi loại; tái sử dụng khay; hình học không giao cắt.

- V16 chụp history bất biến cùng revision dưới khóa, dựng receipt/event DTO ở worker trước khi ghi.
- Giữ API CaptureCheckpoint/Start cũ; ProjectOwned vẫn ở main thread, worker không đọc world sống.
- Module history 12/12 đạt; full native V16 910/910 đạt, 0 lỗi/0 bỏ qua, exit 0.
- Full suite: work/city-model-v16-20261010/full-tests-final.xml, duration 19,5747589 giây.
- Hai test tích hợp mới: DeferredCheckpointTests.cs, giữ revision và lỗi worker qua nhiều lần drain.
- V16 IL2CPP build 11:33:32 UTC, 71,033 giây, 0 error/28 warning; bản riêng Candidate/TycoonTown.exe.
- ABBA cùng save 39 nhân viên, 1080p/Balanced/cap165: V14 161,525/157,989; V16 161,694/161,370 FPS.
- Snapshot trung bình bốn mẫu: V14 11,734 ms, V16 9,077 ms; không coi đây là bảo đảm giảm mọi hitch.
- Cả bốn Player exit 0, functional/NPC PASS, 20/20 ảnh không blank; V16 đã xem ảnh farm thực tế.
- Mọi lượt ngắn stable165=false; tour V16 T114554647Z/PID26948 đang chạy, session67265 cần resume.
- Hardware V16 session16444 đã exit0:132mẫu/181,183s, samples-20261010T114746317Z, đúngPID26948.
- CPUgame17,19%/CPUtotal37,61%; GPUtotal34,52%/2,123GHz/62,05C(58-67)/34,55W trong đoạn giữa tour.
- CPUtemp/effectiveclock chưa có; field Windows3801MHz không phải clock đo thực; presentation165Hz=false.
- Tools/measure_game_hardware.ps1 đã sửa process name từ ExpectedExe, vẫn so executable path chính xác.
- Docs/V14_HARDWARE_AND_FPS.md phân biệt phần V16 mới và số V14 cũ; không ghép các cửa sổ khác nhau.
- V15 thử copy DTO đơn giản không tăng FPS lặp lại được trong ABBA; đã phục hồi trước khi tích hợp V16.
- Giữ goal active. V16 là candidate đã kiểm bước đầu; cần tour toàn thành phố và xác minh hiển thị 165 Hz.

## V14 đã đóng gói, chưa nghiệm thu 165 FPS

- Tour V14 Normal/1080p/sync đã kết thúc:ordinary-play-20261010T085522987Z,PID35148,exit0.
- PASS937,574s/172600frame/184,092FPS,39nhân viên,456tương tác,42ảnh không blank.
- P958,819ms/P9912,261ms,worst587,419ms;318frame>16,67ms/47>33,33ms/6>100ms;stable165=false.
- Hardware180s đã xong:samples-20261010T085522643Z,128mẫu,CPUgame17,44%/CPUtotal61,63%.
- GPUtotal75,71%,graphics2,467GHz,temp77,39C(66-84),power73,03W;chỉ đoạn đầu,khác cap165 ngắn.
- Log tour có IDXGISwapChain timestamp fallback; chưa chứng minh trình bày165Hz.
- Agent audit xong:Docs/V14_REQUIREMENT_EVIDENCE.md và Docs/V14_HARDWARE_SENSOR_AVAILABILITY.md.
- HWiNFO có cài nhưng sensor interface chưa active;CPUtemp/effectiveclock chưa có dữ liệu thật.
- Docs/V14_PLAYER_GUIDE.md đã có; Source.zip V14 gồm 2065 file theo whitelist, không gồm save riêng.
- QA/city-model-v14-acceptance-20261010.json có5hash đã đối chiếu;objectiveComplete=false.
- Báo cáo FPS/tải/nhiệt: Docs/V14_HARDWARE_AND_FPS.md; Delivery V14 đã có ba ZIP và manifest.
- SHA/CRC kiểm đúng: Windows 54 file, Source 2065 file, Evidence 1154 file; 954 PNG giải mã được.
- Tools/verify_city_delivery.py kiểm gói exit 0; 22/22 test checker đạt. Báo cáo POST_PACKAGE_CHECKS.json.
- Player giải nén PackageSmoke đã chạy T101001772Z exit 0, chức năng PASS, 20 ảnh không blank.
- PackageSmoke: 162,390 FPS, P95 6,330 ms/P99 8,141 ms; 64 NPC đi, 11 có animation; stable165=false.
- Tools/measure_game_hardware.ps1 mới yêu cầu executable/scope/output,output dướiwork/,97dòng.
- Tools/package_city.py thêm docs V14;python Tools/test_package_city.py PASS3/3.
- Native full V14 PASS896/896,0failed/0skip;work/city-model-v14-20261010/full-tests-final.xml,exit0.
- So Source.zip V13 với303runtimeCSharp:chỉCheckpointSnapshot.cs thay đổi;runtime-source-comparison.json.
- Script đo mới chạy thật11mẫu/15,404s cuối lượt,cùngPID;GPU35,27%/2,150GHz/58,45C(56-62).
- Candidate 08:13:12 UTC buildSucceeded, 0 error/28 warning; ZIP V14 đã có, chưa nghiệm thu 165 ổn định.
- Giữ goal active;chi tiết hardware/đo:Docs/V14_PERFORMANCE_FINDINGS.md và work/fps-hardware-20261010.

## Model thành phố V13 bản kiểm 10/10/2026

- Hướng dẫn hiện hành: Docs/V13_PLAYER_GUIDE.md; phạm vi nước: Docs/V13_OCEAN_CONTINUITY.md.
- Native887/887PASS,0skip; work/city-model-v13-20261010/full-tests-final.xml.
- IL2CPP FinalRelease dựng06:27:31UTC,0error/28warning; backend dự án đã phục hồi Mono.
- Gallery player-20261010T062809251Z:15góc ven biển/hai fog toggles,PASS,0runtimeerror; đã xem ảnh.
- Biển xa và nền cỏ nối tiếp sửa khoảng xám ở góc bên, không thêm collider/NavMesh.
- ABBA toàn cảnh Marina GPU:1,431/1,618/1,628/1,461ms; chân trời tăng khoảng0,177ms trong cảnh này.
- Lượt V13 ordinary-play-20261010T063058411Z đã PASS:186748frame/935,489s,39nhân viên,456tương tác.
- EngineFPS199,626;P956,914ms/P999,183ms,worst603,454ms;stable165=false,0runtimeerror.
- Gói V13 Delivery đã tạo/kiểm SHA+CRC;WindowsZIP giải nén khớp45file,IL2CPP có GameAssembly.dll.
- Ca cap165 chạy nền T073717087Z:163,697FPS,worst192,297ms;NPC animation evidence FAIL,0runtimeerror.
- Hardware nềnT073717087Z bị loại cho nghiệm thu chơi:ảnh scenery-farm đen,toàn lượt0FrameTiming/0NPCanimated.
- Chưa có xung CPU thực tế/nhiệt độ CPU;không dùng số chạy nền để kết luận tải khi chơi hiển thị.
- V14 đã tối ưu copy history;24/24native tests(gồm9mới),IL2CPP Candidate exit0;PlayerNormal T081545725Z PASS.
- 162,836FPS/P956,186/P997,661ms;20ảnh đạt,10NPCanimated;checkpoint snapshot11,237/8,789ms;stable165=false.
- Đo gần165 có render:CPUgame15,62%,CPUtotal27,35%,GPU34,5%,1,967GHz,53,54C(52-56);26mẫu/36,321s.
- Hợp đồng/đo: Docs/History/LEGACY_DOCS_20261011.md (SOURCE:
  V14_PERFORMANCE_NPC_CONTRACT) và Docs/V14_PERFORMANCE_FINDINGS.md; CPU temp/boost chưa đo được.
- Native red và các vòng ảnh lỗi được giữ;V13 source/Windows/evidence ở work/city-model-v13-20261010/Delivery.
- Model khác giữ geometry V11:664ảnh/2058checks,27loại hàng,13landmark và11loại phương tiện.
- V12 sau sửa bộ đo PASS194.505frame/880,977s,706tương tác,39nhân viên,377điểm,0runtimeerror.
- EngineFPS220,783/toàn lượt và207,242/đầy thành phố;P956,468ms/P998,641ms; chưa chứng minh scanout.
- V12 WindowsZIP giải nén khớp42file; smoke PASS; ZIP có SHA/CRC giữ nguyên.
- Chi tiết V12: Docs/V12_LABEL_PERFORMANCE_ACCEPTANCE.md và Delivery/POST_PACKAGE_CHECKS.json.
- Trình bày thực tế165Hz còn chờ xác nhận;UI chưa có xác nhận mở khóa,stable165 chưa đạt,goal active.
- Phạm vi và lệnh: `Docs/V11_CITY_MODEL_ACCEPTANCE.md`; native hiện hành866/866PASS,0skip.
  Lưu tại `work/city-model-v11-20261010/full-tests-string-list.xml`.
- Két/kệ/quầy/thùng chi tiết hơn; café, ngân hàng, khách sạn, nhà hát và marina đưa tổng landmark lên13.
- Café có hai ghế sát mỗi bàn tròn; thuyền có thân kín, cabin, lan can, phao và thiết bị trên boong.
- Civic/leisure/công viên/đồ đường phố đã được sửa hỗ trợ và chi tiết; giữ business/save ID.
- Candidate `work/city-model-v11-20261010/Candidate`:buildSucceeded,0error,28warning.
- Gallery `player-20261010T001752914Z`:PASS2055checks/655ảnh,0runtimeerror; riêng9góc/đặt5góc.
- Gallery tạm dừng simulation khi chụp; không dùng nó để chứng minh gameplay hoặc FPS toàn thành phố.
- Lượt phương tiện `player-20261010T002505325Z`FAIL CargoTruck0lên/0xuống;10xe/tàu khác đạt.
- Nhóm traffic đang sửa lookup/reserve/path; không sửa ngưỡng240s hoặc tạo hành khách giả để đạt.
- Sau Candidate đã thêm lối nối vỉa hè, xoay café, biển ngoài và noise sóng giảm lưới lặp.
- AccessCandidate đang dựng; cần gallery/ABBA lại và full-tour13khu/bốn cầu tàu trên source hiện hành.
- Nước Candidate GPU off/on/on/off1,944/1,950/1,955/1,946ms; noise mới chưa được đo.
- Bóng cân bằng55m/2048/1cascade có đối chứng ổn định; xem `Docs/SHADOW_FIX_20261010.md`.
- Trước V11 diagnostic152,421FPS, P957,206ms; mục tiêu165FPS còn chưa đạt.
- Gallery AccessCandidate đạt2062checks/664ảnh,0runtimeerror; video nước61frame có timestamp thật.
- Full-tour AccessCandidate `ordinary-play-20261010T010240947Z`PASS;377điểm thông,13khu/bốn cầu tàu.
  147,05FPS/939,61s,39nhân viên,456tương tác;165FPS chưa đạt.
- Source sau AccessCandidate thêm dấu đo capsule/idle/sync và thay thùng thu tiền cùng bó tiền ở quầy.
- Ba test thùng/tiền đã native PASS. CashCandidate diagnostic sau đó FAIL năm điểm thu bị chặn.
- Điểm thu chuyển ra trước thùng; test tái hiện2FAIL rồi nhóm liên quan18/18PASS.
  Bằng chứng `cash-navigation-red.xml`/`cash-navigation-green.xml`; cần Player377điểm lại.
- Source sửa renderer mặt đỡ trùng ở kệ/quầy, POS chìm và khung kho trùng; chưa gallery mới.
- Đã thêm CityPlay.CheckCityNavigation vào operation-timings.csv để truy nguyên hitch đầu lượt.
- PlacementCandidate gallery `player-20261010T015328423Z`PASS2058checks/664ảnh,0runtimeerror.
- Diagnostic PlacementCandidate `ordinary-play-20261010T015709724Z`PASS377điểm thông;
  154,845FPS/62,295s,39nhân viên/24khách,CPU5,334/GPU2,074ms.
- Full-tour khởi đầu `ordinary-play-20261010T015900930Z`PASS899,733s/159,341FPS;
  686tương tác,39nhân viên,4.179,34m,13khu/bốn cầu tàu,thu/gửi/rút tiền thật,0runtimeerror.
- PacingCandidate dựng02:28:27UTC; thêm đo ResumeCity/LoadGame/Snapshot sau bản kiểm hình.
- Gate11phương tiện PacingCandidate PASS `player-20261010T023025621Z`,0runtimeerror.
  CargoTruck2lên/2xuống; hai tàu đi1.078,028m và1.075,906m,3lên/2xuống mỗi tàu.
  Xe đường bộ có lượt lên/xuống thật; distance0 là placeholder,không là quãng đường đo.
- Mặc định syncDisplay=true; các sốFPS vừa nêu dùng Manual165 cap phần mềm, stable165=false.
  Cần so sánh VSync bằng cùng build/save; `Docs/V11_PACING_FINDINGS.md` chỉ là rà đọc code.
- ABBA pacing PacingCandidate/save cũ đang đo; ba lượt146,899/100,963/145,497FPS,
  sync/manual/manual. Chưa đủ cơ sở đổi mặc định; dữ liệu raw giữ cả hitch phục hồi/lưu.
- Rà checkpoint có hợp đồng `Docs/V11_CHECKPOINT_PERFORMANCE_CONTRACT.md`;
  không xóa receipt/journal hoặc kéo dài autosave để che khựng.
- ABBA đã hoàn tất:sync/manual/manual/sync146,899/100,963/145,497/127,580FPS;
  cả bốn stable165=false;chưa đổi mặc định. Raw `work/city-model-v11-20261010/pacing-abba.json`.
- CheckpointProbe đo Snapshot72,879/66,029ms;World capture/copy/projection chỉ khoảng1–3ms.
- Receipt QA đổi sangHashSet5,179ms;giảm overheadQA,không dùng để tuyên bố tăngFPS game.
- Source tối ưu MemberwiseClone delegate,giữ deep copy;4test mới về isolation/type/long/20.000receipt.
  Native864/864PASS;Release đã dựng và đang đo đối chứng checkpoint trên save riêng.
- Chỉ mục ảnh/video `work/city-model-v11-20261010/evidence-index-placement.json` đã tạo.
  664ảnh,61frame nước;phải giữ phiên bản capture và build tương ứng trong hồ sơ cuối.
- Release đầu snapshot delegatePASS154,368FPS nhưngSnapshot52–59ms còn khựng.
- QA checkpoint có9.277event với9.277consumer list rỗng;source copy List<string> bằng newList trực tiếp.
  Thêm2test null/duplicate/order/isolation;compiler access failure đã sửa test bằng reflection.
  Native866/866PASS,0skip;Release đang dựng lại trước đối chứng và full-tour cuối.
- Release hiện hành03:33:25UTC,0error/28warning. Diagnostic033534095ZPASS150,498FPS;
  Snapshot17,727–26,152ms thay66,029–72,879ms ở baseline,checkpoint20,074–44,714ms.
  Không tuyên bố tăngFPS game từ phép so một cặp lượt;stable165=false.
- Full-tour Release mặc định sync đang chạy `ordinary-play-20261010T034124820Z`,PID12224/session10147.
  Dùng save riêng,không Profile/overlay/manual override;đã qua vòng thu/gửi/rút/chi tiền.
- Source list2.015file đã tạo;chưa nén gói khi Player FPS đang chạy.
- Full-tour Release034124820ZPASS900,990s/143.701frame,685tương tác,4.172,56m,
  đủ13khu/bốn cầu tàu,39nhân viên/18khách,0runtimeerror;PID12224/session10147đã kết thúc.
  Sync=true165Hz1080p:159,492FPS toàn lượt/149,008FPS đủ nhân viên;
  P9510,115/P9915,217ms,worst2924,458ms;stable165=false. DXGI timestamp warning giữ nguyên.
  Checkpoint cuối lượt khoảng19–25ms;còn frame dài khác chưa giải thích hết.
- Không xem850test hoặc gallery AccessCandidate là bằng chứng cho các sửa source sau bản dựng.
- Không đóng goal, không publish/push; chỉ dùng save QA riêng và giữ các dirty file không liên quan.

## Thành phố và model v10 hiện hành 09/10/2026

- Hướng dẫn: `Docs/CITY_MODEL_V10_ACCEPTANCE_20261009.md`; build: `work/city-model-v10-20261009/Release`.
- Thành phố 565,685424 × 384,666088 m, gấp đôi mỗi chiều; thêm tám khu ngoại vi và nước nhẹ.
- 27 hàng; chỉ egg/milk/bottled_milk/sauce/soy_sauce dùng vật chứa khi cầm, đúng số lượng thực.
- Máy công nghiệp mới, phần nâng cấp có chân/giá đỡ; hàng cầm 2 cột × 3 hàng theo tầng.
- Windows build thành công; EditMode **792/792 PASS**, không skip, XML tại `work/city-model-v10-20261009`.
- Mod diagnostic hiện cửa sổ PASS: `ordinary-play-20261009T135924656Z`, 0 runtime errors.
- Bỏ vẽ lại icon cho ô cấp cuối đã hoàn tất; giữ điểm neo icon trong bước dựng bố cục đầu tiên.
- Khựng quanh mod giảm từ khoảng 774,936 ms xuống 86,385 ms; ca profiler đạt 162,292 FPS trung bình.
- Lượt chơi đầy đủ hiện cửa sổ không profiler PASS: `ordinary-play-20261009T140232595Z`.
- 635,243 giây, 2.936,326 m, 1.038 tương tác, đủ mọi khu/hai cầu bến tàu, 0 runtime errors.
- FPS 147,361, P95 9,418 ms, CPU/GPU 6,172/2,060 ms; chưa đạt 165, đang tìm chi phí CPU.
- Chỉ mục đơn hàng mỗi projection bỏ quét lịch sử/tạo chuỗi từng đơn; thêm ba regression test.
- Ca CPU sau sửa: `ordinary-play-20261009T142946144Z`, 151,562 FPS gồm khởi tạo, P95 7,783 ms.
- Update khách giảm 0,569 → 0,076 ms/frame; các ca đứng tại chỗ không thay thế full-tour.
- Capsule nghỉ kiểm sàn mỗi 80 ms, mọi bước ngang xử lý ngay; rotor chỉ ghi chuyển động cần thiết.
- Full-suite 792 PASS và build đã xong; visible full-tour đang chạy `ordinary-play-20261009T144927602Z`.
- Ca 60 giây đứng tại chỗ có thể thiếu lượt xe lên/xuống, không thay thế nghiệm thu phương tiện.
- Harness thêm timestamp CSV cho video cầm hàng; ca mới cần chụp lại để encode đúng thời gian thực.
- Gallery mới còn cần chạy để xác nhận chân nâng cấp, nước và 55 góc đặt thực tế của 11 xe/tàu.
- Goods và props/bàn ghế đã có kiểm hình riêng; xem `Docs/Art/V10_*_REVIEW.md`.
- Cửa sổ ẩn từng cho ảnh đen và Animator culling không chạy chân: không dùng FPS đó để nghiệm thu render.
- VSync D3D11 từng báo refresh drift; chế độ manual165 đo riêng, không đổi NVIDIA/Windows.
- Không overwrite save thật; giữ nguyên thay đổi có sẵn tại QA/build-report.json, AGENTS.md,
  ASSET/Downloaded, ASSET/downloaded_summary.json và mod/test/overnight-campaign.json.
- Chưa đóng goal hoặc kết luận 165 FPS ổn định tuyệt đối; đóng gói sau khi Player và kiểm hình bản mới hoàn tất.

## Lịch sử thành phố 08/10/2026

- Hướng dẫn hiện hành: [CITY_EXPANSION_20261008.md](CITY_EXPANSION_20261008.md), README tại root.
- Nền tăng gấp đôi diện tích; sáu khu độc lập, đường nối, cảnh quan, cư dân và xe đường phố.
- Xe buýt, xe khách, taxi có hành khách lên/xuống; NPC theo mạng hành lang đường/vỉa hè chính.
- Hàng cầm lớn và chi tiết hơn, atlas PBR dùng chung, chồng đủ sức mang và lọc chuyển động.
- Giữ model/rig/asset nhân vật; số liệu chốt tại QA/city-acceptance-20261008.json.
- Build hiện hành: work/city-expansion-20261008/Release/TYCOON2.exe.
- Các lượt đo mới là chơi bình thường qua gamepad ảo, simulation 1x, save riêng; không benchmark.
- Tiền cầm tay/két, hai mod và save schema 3 là cơ chế hiện hành.
- Đội ngũ 39 người thay cho 78, đủ 26 nghề; năng lực ×1,5, tải thường 18/30/48, bốc xếp 18/36/54.
- Giảm nhân viên giữ hàng/đặt chỗ trước khi nghỉ; journal cũ replay theo quy tắc cũ rồi mới migration.
- Cỏ, mảng đất trống và sỏi nhỏ là shader mặt đất; cây ba kiểu, bỏ khối đá độc lập và lưới nền dày.
- Hiệu ứng nhặt/giao và thu tiền dùng pool; dựng nhân viên hai người/frame, journal giữ handle ghi.
- EditMode 459/459, không fail/skip, 08/10 07:16:58–07:17:06 UTC; Player đã nghiệm thu lại.
- Player: 219 kiểm tra chức năng, 117 migration, 10 load và 198 hình ảnh; không lỗi runtime.
- Chơi bình thường 266,87/344,42 giây: 162,36/160,47 FPS trên RTX 4060, 1080p, simulation 1x.
- Có một frame 748,94 ms khi mod tối đa; chưa xác định toàn bộ nguyên nhân. Ảnh 4K render offscreen.
- Báo cáo chốt: `QA/city-acceptance-20261008.json`; hướng dẫn: `Docs/CITY_EXPANSION_20261008.md`.
- Capsule player/NPC chặn vật thể, NavMesh climb 0,15 m; đi bộ dựa tốc độ thực và giữ pha clip.
- Hàng 3 cột × 2 hàng mỗi tầng; thịt lớn, cà rốt/lúa nằm ngang, kho/bàn đỡ đúng đáy mesh.
- Nhãn tiếng Việt có viền/cỡ pixel đồng nhất; cache vật liệu/mesh phục hồi khi tài nguyên đã bị hủy.
- Các mục audit và báo cáo phía dưới giữ lịch sử của các bản trước.

## Nâng cấp bố trí và hình ảnh 07/10/2026

- Yêu cầu mới: bố trí khu và nâng hình ảnh theo hướng bán thực tế; ô nâng cấp dùng icon vẽ 2D.
- Đã bổ sung kiến trúc, nội thất, cảnh quan, vật liệu, ánh sáng và shader; gom ô theo khu.
- Bản chạy mới: `work/visual-redesign-20261007/Release/TYCOON2.exe`.
- Thiết kế, nguồn và lệnh QA: [VISUAL_REDESIGN_20261007.md](VISUAL_REDESIGN_20261007.md).
- Kết quả cuối: `work/visual-redesign-20261007/acceptance.json`.
- EditMode 195/195; build Windows thành công, 0 lỗi và 2 cảnh báo.
- Player cuối trên RTX 4060/D3D11: 340 kiểm tra, 360 điểm đi được, 81 icon 256 px, 0 lỗi runtime.
- Vòng chơi đầy đủ: 402 kiểm tra, 1.032 thao tác, save/load đạt; báo cáo ghi rõ phạm vi bản dựng.

## Audit trước lần nâng cấp hình ảnh

- Yêu cầu: audit toàn bộ game, hoàn thiện lỗi/phần dở, kiểm tra test case, cập nhật bàn giao và commit/push.
- Lượt chốt này chạy EditMode và build Windows headless; không mở Player, không benchmark hoặc soak.
- Công đoạn 01–13 đã có implementation và regression theo từng hệ thống.
- Công đoạn 14 đã có implementation/polish và các harness kiểm chứng ngắn; chưa nghiệm thu toàn campaign.
- Không kết luận không còn mọi bug hoặc đạt mọi tổ hợp chỉ từ test/build.

## Môi trường và thiết kế hiện hành

- Unity 6000.6.3f1, URP 17.6.0; Windows chơi đơn offline, tiếng Việt.
- Repository D:\GAME\TYCOON2; main; remote https://github.com/letam10/TYCOON2.git.
- Mốc trước audit: 736dd29; commit implementation/test: 4a4b98e; khóa save: 06e7723. Bản sửa save có sẵn đã được giữ và kiểm chứng.
- WASD/mũi tên/gamepad; camera 55 độ; dừng 0,25 giây trong tầm 1,2 m để thao tác.
- Thiết kế thị trấn mới dùng proximity, máy tự động, xe/thùng hàng và đội theo nghề + khu.
  Quy định máy cần operator trong PLAN/REPLAN là thiết kế lịch sử đã được thay thế.
- Khởi đầu tiền tay/két đều 0, ba luống cà rốt, cà rốt 10 xu; tuyến miễn phí giúp phục hồi.
- Mang một SKU hoặc tiền; sức mang 12 → 20 → 32 → 48. Kho từng khu có owner riêng.
- Đơn FIFO, kiên nhẫn 90 giây; quá hạn giữ phần đã nhận và trả 0; ghi thất thoát đúng một lần.
- Giao đủ tạo tiền chờ thu; player thu lên tay, có thể gửi/rút toàn bộ tại két.
- Mod game có hai dòng: thêm 999.999 vào két hoặc mở/nâng tối đa theo định nghĩa hiện có.
- Các mốc mở khu và điều kiện vận hành giữ trong Definitions/ProgressionTracker.
- Save schema 3/layout 2 + journal giữ receipt, dedup, reservation và state; không tiến triển offline.

## Lỗi đã xử lý trong đợt hoàn thiện

1. Save cũ còn các purchase pad đã bỏ làm Play bị chặn.
   Chỉ bỏ bookkeeping/owner rỗng của pad catalog legacy; dữ liệu có hàng/reservation vẫn bị từ chối.
2. Load từng ghi migration xuống file trước khi kiểm tra owner/station.
   Nay ValidateSaveOwners đạt mới checkpoint; save sai được giữ nguyên byte và chặn autosave.
3. Catalog thay đổi nhưng không thêm stable ID khiến machine/counter giữ cấu hình cũ sau load.
   ReconcileWorld luôn đối chiếu requirement, recipe options, capacity/accepts, yield/cycle và ghi khi có thay đổi.
4. Processing còn yêu cầu mua dairy dù gói máy đã gộp vào khu.
   Cheese/Sauce và gate Supermarket dùng mill; dairy giữ ID legacy để bảo toàn save cũ.
5. Journal còn khoản mua dairy bị từ chối khi replay sau đổi catalog.
   Chỉ recovery chấp nhận lệnh cũ; gameplay mới vẫn từ chối. Test góp dở/góp đủ/hoàn tất và replay lặp.
6. ItemPool có thể lấy lại GameObject đã bị hủy cùng worker/customer sau load.
   Bỏ phần tử Unity-null khi Take và bỏ qua Return cho object đã chết; có regression riêng.
7. Actor/khách mới có thể đổi trạng thái khi timeScale = 0.
   Customer, diner, worker và spawn mới chặn khi deltaTime = 0; vẫn cho restore actor đang pending.
8. Restaurant Milestone 0 từng chặn cả restore diner.
   Chỉ chặn sinh khách mới sau restore, giữ save/QA tạm dừng nhất quán.
9. Harness Stage14 còn đi qua action zone cũ; Town còn bấm nút hỗ trợ cũ.
   Chuyển helper sang điểm proximity và click Mod game qua EventSystem, kiểm tra unlock không đổi.
10. Nền Farm chồng Restaurant và bảng Farm nằm sai vị trí.
    Chỉnh phạm vi nền và bảng; không đổi gameplay gate hoặc stable ID.

11. Migration từng ghi save trước khi lấy khóa độc quyền của transaction store.
    Lấy khóa trước đọc/migrate, giữ cùng handle đến store; tiến trình khác đang giữ save thì không đổi file.
    Regression trước sửa: 172 pass/1 fail; sau sửa: 173 pass/0 fail. Khóa vẫn được giữ qua repeated load.

## Kiểm chứng mới trong lượt chốt

| Kiểm tra | Kết quả | Bằng chứng |
| --- | --- | --- |
| Unity EditMode toàn bộ | 173/173 pass, 0 fail, 0 skip | QA/editmode-results.xml |
| Build Windows headless | Succeeded, 0 errors, 2 warnings, 117.982.843 bytes | work/audit-20261007/Build/build-report.json |
| Logger ghi song song | 2 tiến trình, 50/50 JSON records duy nhất | work/audit-20261007/logger-result.json |
| Snapshot kết quả và hash | Lưu XML/hash, build, các report Player có sẵn | QA/audit-20261007.json |
| Audit kinh tế/tiến trình | Chưa xác định thêm bug chắc chắn ngoài các sửa đã liệt kê | work/audit-20261007/economy-audit.md |

Test phủ inventory/owner, tranh chấp và idempotency, đơn/payment/timeout, góp mua/tiến trình,
production/recipe/crew, sửa máy/Rush, crate/truck/conveyor, save/journal/migration, input và icon.
Các exception ở ca save sai là LogAssert được mong đợi; XML là nguồn kết luận pass/fail.

## Các ca Player đã có sẵn trước lượt chốt

Bảng này đối chiếu artifact có thật trong work/completion-audit, không phải lượt Player mới.
Mỗi báo cáo chứng minh phiên bản tại thời điểm chạy; không thay thế nghiệm thu toàn game hiện hành.
Tất cả các hàng dưới ghi passed=true, runtimeErrors=0; metadata/hash nằm ở QA/audit-20261007.json.

| Nhóm | Checks | Thư mục dưới work/completion-audit |
| --- | ---: | --- |
| Progression sát ngưỡng | 80 | player-progression-20261007T060857549Z |
| Processing/Bakery/Market crew, thiếu nguồn/đầy đích | 80 | player-crews-20261007T015850351Z |
| Farmer, chăn nuôi từ đàn 0, Restaurant cook/waiter | 44 | player-roles-20261007T020439296Z |
| Loader, crates, truck và quyền sở hữu khi đầy/chặn/load | 163 | player-cargo-20261007T061623718Z |
| Breakdown, Repairer, Rush đủ 15 + 90 giây | 114 | player-events-20261007T020917501Z |
| Save tổng hợp và load lặp | 151 | player-save-20261007T060718982Z |
| Relaunch state | 47 | player-load-20261007T061607695Z |
| Relaunch rồi hoàn thành worker/repair/cargo | 52 | player-load-20261007T061907934Z |

Fixture cấp trạng thái sát ngưỡng để cô lập từng cơ chế, không chứng minh chơi từ 0 đến cuối.
Các report thất bại ban đầu vẫn giữ để đối chiếu; chúng đã có ca pass thay thế theo từng nhóm.
Không có kết quả All pass cho toàn bộ nhóm trong cùng một phiên; không suy diễn từ các lượt riêng.

## Trạng thái từng công đoạn

| Công đoạn | Kết luận hiện tại |
| --- | --- |
| 01–03 Audit/core/interaction | Đã triển khai; regression ownership/input/zone/proximity có kiểm chứng |
| 04–06 Farm/purchase/livestock | Có starter/gates/chăn nuôi; còn nghiệm thu hai nhánh chơi dài từ 0 |
| 07 Logistics/crew | Có ca nghề/khu, thiếu/đầy, loader/cargo; chưa chứng minh mọi tổ hợp đồng thời |
| 08 Save v2 | Migration/journal/corrupt-save/replay có regression; có artifact relaunch state và resume |
| 09–11 Farm Shop/Processing/Supermarket | Gate và các recipe/chuyển hàng có test; có ca sát ngưỡng |
| 12 Bakery/Restaurant | Bread/Cake/cook/waiter có harness; chưa mọi recipe/tải đồng thời ở bản cuối |
| 13 Events/breakdown | Rules test và artifact Rush/Repairer đủ chu kỳ có sẵn |
| 14 Polish/end-to-end | Chưa nghiệm thu campaign liên tục 180–240 phút, đồ họa toàn game hoặc hiệu năng |

## Phần còn hoãn và giới hạn

- Không chạy mới campaign ví 0 → Restaurant bằng toàn tiền sản xuất thực thu.
  Mục tiêu mở toàn chuỗi trong 180–240 phút vẫn CHƯA NGHIỆM THU.
- Không chạy benchmark 165 FPS, soak dài, traffic dài hoặc mọi tổ hợp crew/recipe/congestion.
- Không xác nhận nghiệm thu nghệ thuật, input vật lý native hay mọi animation/rig trong raw library.
- Một số harness lịch sử ngoài Completion/Town còn giả định thế giới cũ; không dùng làm acceptance mới.
- Không dọn hàng loạt work/cache/output/raw assets. Các mục cleanup lịch sử vẫn giữ ngoài Git.
- Dữ liệu save chơi thật không bị sửa bởi lượt chốt; test dùng thư mục tạm hoặc fixture QA riêng.
- QualitySettings có sẵn, QA/build-report.json cũ, raw ASSET, AGENTS.md và preset qua đêm không thuộc commit audit.

## Lệnh tiếp tục

Chạy tuần tự, không mở hai Unity instance cùng project:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_unity.ps1 -Task Tests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_unity.ps1 -Task Build `
    -BuildPath work/audit-20261007/Build/TYCOON2.exe
```

Khi có yêu cầu chạy Player riêng, Tools/run_completion.ps1 hỗ trợ các Case:
Progression, Crews, Roles, Cargo, Events, Save, Load hoặc All.
Dùng -BuildPath trỏ tới build mới; Load cần -LoadPath tới save QA của ca Save/Cargo.
Harness có giới hạn thời gian và ghi completion-report.json hoặc completion-load-report.json.
Chưa khởi chạy các lệnh Player này trong lượt chốt hiện tại.

## File chính và lịch sử

- Runtime: GameSession, RuntimeTransactions, TransactionCore/State, GameplayTransactionStore,
  GameSessionSaveCompatibility, CommerceDirector, RestaurantDirector, WorkerAgent, Art.
- QA mới: QaCompletion*.cs, QaSaveRecovery, SaveRecoveryPlayVerification, Tools/run_completion.ps1,
  mod/test/completion-*.json, CompletionMigrationTests, RetiredPurchaseJournalTests,
  ItemPoolReloadTests và SaveWorldCompatibilityTests.
- Trình bày/Mod/Play recovery trước đó: Docs/CODEX.md.
- Lịch sử không sửa nội dung: History/HANDOFF_01_05.md, History/HANDOFF_06_14.md, History/HANDOFF_TOWN.md.
- Các nhận định cũ như thiếu remote, worker chưa có ca kiểm tra hoặc logger chưa có mutex
  không đại diện trạng thái mới; logger hiện có mutex; kiểm tra hai tiến trình ghi đồng thời 50 bản ghi đã đạt.

## Final delivery evidence — 2026-10-17T00:55:59Z

### Current result

- Full EditMode suite: `Tools/run_unity.ps1 -Task Tests`, Unity PID 19944, exit code 0.
- Fresh XML: `QA/editmode-results.xml`, start `2026-10-17T00:55:26Z`, end `2026-10-17T00:55:59Z`, top level `1066/1066 passed`, `0 failed`, `0 inconclusive`, `0 skipped`.
- Windows build: `Tools/run_unity.ps1 -Task Build`, Unity PID 10788, report `QA/build-report.json`, `result=Succeeded`, `errors=0`, `warnings=32`, `bytes=117077295`, `builtUtc=2026-10-17T00:49:31Z`.
- Required build location: `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe`.
- Player smoke: `Tools/run_player.ps1 -Task Stage67`, report `D:\GAME\TYCOON2\work\stage06-07\player-stage67-20261017T005009173Z\stage67-report.json`, `passed=true`, started `2026-10-17T00:50:16.2629066Z`, finished `2026-10-17T00:54:13.7082244Z`, NVIDIA GeForce RTX 4060 Laptop GPU, `interactions=1165`, no failure. Evidence includes `farm-livestock-sale.png`, `qa-save-v2.json`, `presentation-audit.json`, `navigation-route.txt` and `graphics-device.json`.

### Latest QA/runtime scope

`Assets/_Game/Scripts/Runtime/QaRedesign.cs` was corrected to follow the runtime contract without changing economy, stable IDs or persistence writers: canonical `animal_feed`, feed delta rather than timing-sensitive exact count, deterministic beef order through `CustomerSave`, animal-feed storage reserve, and acceptance of transaction schema 2 or 3. The Player smoke reached crop harvest, feedmill input/output, beef feeding and harvest, customer beef order and payment, storage reservation/release and transaction SaveV2/v3 state.

### Release checks and limits

- `git diff --check` reports trailing whitespace and line-ending warnings in pre-existing dirty model `.meta` files; they were not edited, cleaned or staged.
- `git lfs status` and tracked-file audit were run. Secret scan returned no matching secret pattern (`git grep` exit 1 means no match). Large raw downloads, QA history, logs, Library, work outputs and unknown `.meta` files remain preserved and excluded from the allowlist.
- GPU/OceanV13 timing was not run; on D3D11 it remains diagnostic unavailable and is not treated as a release blocker.
- SHA256, final allowlisted staging, commit and push are still pending at the time of this handoff append. Do not claim release complete until those operations and their post-push checks succeed.

## Final delivery release evidence — 2026-10-17T01:01:34Z
- status: PASS for focused Stage 4R/6/7/9/10 checks, full EditMode, required Windows build, bounded Player Stage67 smoke and release push.
- focused: Stage4RRouteCarryTests exit 0 with 4/4; full EditMode exit 0 with 1066/1066 passed, 0 failed, 0 skipped; no runtime error was reported by the final Player report.
- build: `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe`, QA/build-report.json result Succeeded, errors 0, warnings 32, bytes 117077295; SHA256 `96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873`.
- Player: `Tools/run_player.ps1 -Task Stage67` exit 0; `work/stage06-07/player-stage67-20261017T005009173Z/stage67-report.json` passed true, interactions 1165; evidence includes farm-livestock-sale.png, qa-save-v2.json, presentation-audit.json, navigation-route.txt and graphics-device.json. The bounded route covered farm, feedmill, industrial production, customer queue/order/payment, carry and storage reserve/release, SaveV2/v3 state, and world presentation checks.
- source scope: visual water/material, coast/shore, vegetation, vehicle/traffic signal, map/label, route/access, cutaway/boarding/carry and SaveV2 compatibility changes plus their focused tests are in commit `f9d4b705080e4083178abbda0aa123151a7abf1d`.
- commit/push: `Complete world redesign visual and access contracts`; `git push origin main` exit 0; `HEAD` equals `origin/main`, ahead/behind 0/0. This documentation update is the follow-up release record.
- cleanup: no path was deleted. Evidence was retained. The remaining dirty/untracked AGENTS.md, model `.meta` files, ASSET/Downloaded, QA historical captures, saves and work artifacts were preserved because ownership was not proven or they are required evidence.
- warnings/limits: build warnings 32; historical GPU timing remains diagnostic unavailable on D3D11 and was not rerun; no benchmark, soak, overnight campaign, GPU timing or `run_completion -Case All` was run; staged allowlist diff-check exited 0 while preserved unrelated working-tree paths remain dirty.
- next: post-release review only; do not start another redesign stage automatically.

## Latest visual tranche release preparation 2026-10-17T09-43-18+07:00
- status: source regression and Windows build pass; release staging and push are pending.
- scope: latest visual changes cover TownSurfaceMaterials, CityVegetation, CityVehicleSurface, CityTrafficSignalVisual, WorldExpansionWayfinding, WorldFactory interaction palette, FollowCamera overview framing, QaStage14 capture alignment and TownWater shader. Stable IDs, economy, routes, colliders and SaveV2 writers were not changed.
- tests: `Tools/run_unity.ps1 -Task Tests` exit 0; fresh `QA/editmode-results.xml` at 2026-10-17T02:43:18Z reports 1066 total, 1066 passed, 0 failed, 0 inconclusive, 0 skipped. `Tycoon.Tests.CityCameraFramingTests` also exited 0 with all three aspect cases, and the visual focused suites recorded 23/23 material checks and 6/6 wayfinding checks.
- build: `Tools/run_unity.ps1 -Task Build` exit 0; `QA/build-report.json` builtUtc 2026-10-17T02:33:09.5772115Z, result Succeeded, errors 0, warnings 32, bytes 117080559. Required path is `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe`; SHA256 is `96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873`.
- Player evidence: latest bounded `Tools/run_player.ps1 -Task Stage14Visual` exited 2. The fresh overview image `work/stage14/player-stage14visual-20261017T023421913Z/02-final-layout.png` now shows the town, routes, coast and water after the FollowCamera far clip fix. The report fails later in `FinalGetItem` because field producer totals remain zero and the player cannot receive real carrot stock; `runtimeErrors` is empty. This is recorded as a warning and is not claimed as a Player pass.
- limits: no benchmark, soak, long campaign, GPU timing or `run_completion -Case All` was run. Existing dirty and untracked model metadata, ASSET/Downloaded, QA history, saves and work artifacts remain excluded from the release allowlist.
- release next: run diff/LFS/secret/large-file checks, stage only the reviewed allowlist, commit, push origin/main without force, verify HEAD equals origin/main, then append the final commit and push evidence here.

## Release push confirmation 2026-10-17T02:47:00Z
- source commit: `1790b81` (`Finalize visual realism tranche and Windows build evidence`), created from the reviewed 11 path allowlist.
- push: `git push origin main` exit 0; remote accepted `4ce6dee..1790b81 main -> main` without force.
- build: `D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe`, report `QA/build-report.json` result Succeeded, errors 0, warnings 32, bytes 117080559, builtUtc `2026-10-17T02:33:09.5772115Z`, SHA256 `96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873`.
- tests: fresh full EditMode XML at `2026-10-17T02:43:18Z` reports 1066/1066 passed, 0 failed, 0 inconclusive, 0 skipped.
- Player warning: latest Stage14Visual report exited 2 at `FinalGetItem` because producer totals were zero and real carrot stock was not obtained; camera overview evidence passed visually, but carry/delivery smoke is not claimed as pass.
- preservation: unrelated dirty/untracked metadata, ASSET/Downloaded, historical QA, saves and work artifacts remain outside the commit.

## Final delivery handoff 2026-10-17T05:40:00Z
- source commit: 9337caa Finalize expansion visuals and Windows build evidence. The allowlist contains GameHudMap.cs, QaWorldFeatures.cs, WorldExpansionModels.cs and PLAN_WORLD_REDESIGN_20261015.md only.
- tests: focused WorldExpansionModelsTests 7/7, VisualMaterialTests 23/23 and Stage9MapWayfindingTests 4/4 all exited 0. Full Tools/run_unity.ps1 -Task Tests exited 0 with QA/editmode-results.xml total 1066, passed 1066, failed 0, skipped 0 and inconclusive 0.
- build: Tools/run_unity.ps1 -Task Build exited 0. QA/build-report.json result Succeeded, errors 0, warnings 5, bytes 117085167, builtUtc 2026-10-17T05:32:18Z. Required path D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe. SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873.
- Player evidence: corrected Tools/run_world_features.ps1 command with relative BuildPath Builds/Windows/TYCOON2.exe exited 0. Fresh report work/final-world-features-20261017T053218Z-retry/world-features-report.json reports passed=true, interactions=30, screenshots=61 and runtimeErrors=0. Fresh images include industrial, north village, east marina, south highway, forest reserve and two cutaway captures.
- release checks: git diff --check and staged diff-check exited 0; git lfs status exited 0; secret scan returned no match; origin/main was fetched and was 0 ahead and 0 behind before commit. No force push, reset, clean, stash or delete was used.
- warnings and limits: five existing build warnings remain. Fresh images show improved industrial detail and forest density, while north village, east marina and south highway remain visually sparse or broad in the bounded capture. No benchmark, GPU timing, OceanV13 timing, soak, overnight campaign, run_completion All or long campaign was run. The world-features harness did not emit a Player.log file in its output folder; process output contained WORLD-FEATURES_QA PASS and no runtime error marker.
- preserved paths: AGENTS.md, model metadata, ASSET/Downloaded, QA historical evidence, saves, work outputs and QA/build-report.json were left dirty or untracked and were not staged because ownership or release scope was not proven.
- next stage: focus only on the remaining expansion POI presentation warning, keeping stable IDs, routes, colliders, economy and SaveV2 unchanged.

## Expansion visual density followup 2026-10-17 06:19:10 UTC
- source commit: 81ae0749d4aba84ab4fd0060f0e21a2d2d182a3a Increase expansion POI visual density.
- files: Assets/_Game/Scripts/Runtime/WorldExpansionModels.cs and the matching plan evidence in Docs/PLAN_WORLD_REDESIGN_20261015.md.
- tests: WorldExpansionModelsTests exit 0 with 7 total and 7 passed.
- build: Windows Player build exit 0, result Succeeded, errors 0, warnings 34, bytes 117088751, executable D:\GAME\TYCOON2\Builds\Windows\TYCOON2.exe, SHA256 96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873.
- Player: work/visual-expansion-service-20261017T140000Z/world-features-report.json passed true with 31 interactions, 61 screenshots and 0 runtime errors. SouthHighway rendererCount 129, colliderCount 4 and modelChildCount 120. The fresh highway image shows both service canopies outside the lane.
- limits: no benchmark, GPU timing, soak, overnight campaign or run_completion All. Unrelated dirty and untracked files remain preserved and unstaged.

