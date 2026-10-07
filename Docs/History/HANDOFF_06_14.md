# Lịch sử bàn giao trước audit 07/10/2026

Chỉ để đối chiếu lịch sử; trạng thái hiện hành ở ../CODEX_HANDOFF.md.

## Công đoạn 06 — FARM LIVESTOCK VERTICAL SLICE

- Producer vật nuôi có state đàn/thức ăn/chu kỳ/tái đàn trong transaction core. Ba gói `barn` (500 xu), `milk_line` (1000 xu) và `egg_line` (1000 xu) được khai báo thành route trọn gói; mỗi purchase mở trạm, đàn/chuồng và quầy bán tương ứng.
- Cho ăn dùng cà rốt từ tuyến khởi đầu. Có vùng cho ăn, chăm sóc, lấy sản phẩm và tái đàn riêng; tái đàn trừ một thức ăn, cần 60 giây và bắt đầu được khi đàn về 0.
- Thu thịt giảm đàn đúng một con cho mỗi miếng bò; hình đàn trong chuồng phản ánh population runtime.
- Máy có phase `WaitingInput → Ready → Operating → CompletedWaitingPickup`; rời vùng vận hành giữ nguyên job/progress và reservation. Input không bị tiêu thụ lần nữa khi tiếp tục; hoàn tất chờ lấy output mới về Ready.
- Farm counter và shelf chấp nhận thịt/sữa/trứng mà không cần mua quầy ẩn. Order chụp giá, giao từng phần; payment nằm ở quầy, player Collect mới tăng wallet. Timeout không trả tiền/hoàn hàng; chỉ phần đã nhận được tính loss.
- File chính: `CommerceDirector.cs`, `PlayerInteraction.cs`, `Station.cs`, `MachineStation.cs`, `TransactionCore.cs`, `TransactionState.cs`, `RuntimeTransactions.cs`, `SaveData.cs`, `WorldFactory.cs`.

## Công đoạn 07 — LOGISTICS VÀ EMPLOYEES

- Storage owner gắn với area; stack trong kho có location `area/ItemType`. Có giới hạn theo item, ngưỡng dự trữ thức ăn/nguyên liệu, reservation nguồn/đích và dòng trạng thái hiển thị hàng đang giữ/chờ nhận.
- Hire kiểm tra trạm đúng family cấp 3, 30 job do người chơi tự làm đúng nghề/khu và kho riêng. Crew được key theo Role + Area; nâng tốc độ, sức mang, số người tác động riêng từng đội.
- Worker có route cho Farmer, AnimalWorker, Restocker, Cashier, Processor/Cook/Baker, Transporter và Waiter. Giữ slot trạm, dùng reservation khi vận chuyển, chờ khi thiếu input/kho/đích hoặc máy hỏng và kiểm tra lại sau. Navigation tránh SetDestination lặp; stuck repath tối đa 3 lần rồi nghỉ 5 giây.
- HUD có mục Quản lý đội. State trạm, player job count, crew và reservation được persist trong transaction save.
- File chính: `Inventory.cs`, `ProgressionTracker.cs`, `WorkerAgent.cs`, `NavigationWorld.cs`, `GameHud.cs`; cùng cập nhật trong `Station.cs`, `TransactionCore.cs`, `RuntimeTransactions.cs`.

## Công đoạn 08 — SAVE V2

- Marker transaction save hiện là v2, tách khỏi prototype file; transaction marker v1 được normalize khi đọc. Save V2 dùng atomic replace, không tạo `.bak`.
- Load nạp projection, order/customer, crew, trạm, tồn kho và cash trước khi mở lại input; simulation clock nối từ thời điểm lưu nên không có offline progression.
- Khi scene thêm owner/station mới, load bổ sung projection/runtime state mà giữ nguyên stack, reservation, revision và journal cũ.
- Hàng khách, worker, storage/counter/machine, payment, purchase contribution, order snapshot và machine state nằm trong transaction state authoritative; Events cùng nằm trong envelope.
- File chính: `SaveData.cs`, `RuntimeTransactions.cs`, `GameSession.cs`, `PlayerController.cs`; seed: `mod/test/stage08-save-v2-seed.json`.

## Công đoạn 09 — FARM SHOP

- Farm Shop có shelf và checkout riêng, khách được route riêng, Cashier đi tới đúng shop; worker Transporter có purchase riêng để đưa hàng từ Farm tới shelf Farm Shop. Người chơi vẫn tự chở/stock/serve được.
- Gate Farm Shop: góp 2.000 xu, đạt 50 đơn thành công và có Farm cấp 3 **hoặc** Animal cấp 3; HUD đọc cùng progression state.
- Load reconcile bổ sung counter/shelf mới vào save cũ trước validate, không bỏ stack/queue cũ.
- Seed sát ngưỡng 1.990/2.000 và 49/50 cho hai hướng: `mod/test/stage09-crop-near-farm-shop.json`, `mod/test/stage09-livestock-near-farm-shop.json`.
- File chính: `Definitions.cs`, `ProgressionTracker.cs`, `CommerceDirector.cs`, `WorkerAgent.cs`, `WorldFactory.cs`, `GameSession.cs`, `RuntimeTransactions.cs`.

## Công đoạn 10 — PROCESSING AREA

- Processing gate giữ điều kiện Farm Shop, Animal cấp 3, 200 đơn và 12.000 xu. `processor` dùng worker role/machine operator hiện có.
- Wheat → Flour, Milk → Cheese, Tomato → Sauce dùng recipe/machine state hiện có. Máy phô mai và sốt vẫn có **purchase `dairy` hiển thị riêng 1.800 xu** sau khi mở Processing.
- Ba conveyor route cố định dùng owner `Conveyor`, reservation từ kho Farm/Farm Shop đến input machine, hàng đang trung chuyển là stack thật; route không cấp thêm khi đích không đủ chỗ. Cargo và reservation tiếp tục sau Save v2/load.
- Worker Transporter Farm Shop hoàn thiện tuyến Farm → Farm Shop; conveyor nối nguồn Farm/Farm Shop tới Processing.
- Seed test: `mod/test/stage10-near-processing.json`, bắt đầu ở 11.990/12.000 và 199/200.
- File chính: `ConveyorStation.cs`, `RuntimeTransactions.cs`, `TransactionCore.cs`, `TransactionState.cs`, `WorldFactory.cs`, `Definitions.cs`.

## Kiểm chứng Công đoạn 06/07

- Unity EditMode cuối: Passed 92/92, Failed 0, Skipped 0 — `QA/editmode-results.xml`. Bao gồm machine phase/pause/output reservation, meat trừ đàn, restock từ population 0, order timeout/loss, location và reservation kho, hire gate theo player job, upgrade crew cô lập theo nghề/khu và Baker/Cook gate.
- Windows BuildPipeline cuối: Succeeded, 0 errors, 2 warnings; build size 115,438,215 bytes.
- Windows Player Stage67 cuối: Passed 285 checks, 0 failure, 0 runtime error; NVIDIA GeForce RTX 4060 Laptop GPU / Direct3D11 — `work/stage06-07/player-stage67-20261005T044650212Z/stage67-report.json` và `graphics-device.json`.
- Tuyến thực chơi trên Player: ví 0 → trồng/bán cà rốt và Collect → mua gói bò 500 → cho ăn → chờ chu kỳ → thu thịt (đàn giảm) → giao đơn bò → payment ở quầy → player Collect. Cũng xác minh kho theo area/SKU và reservation nguồn/đích.
- Ảnh QA cuối: `work/stage06-07/player-stage67-20261005T044650212Z/farm-livestock-sale.png`.

## Kiểm chứng Công đoạn 08–10

- Save V2 EditMode suite: Passed 6/6, Failed 0 — `work/stage08/editmode-results.xml`; có đọc lặp envelope và transaction v1→v2.
- Gate Farm Shop EditMode: Passed 2/2 — `work/stage09/editmode-results.xml`; seed crop/livestock đạt lần lượt 49→50 đơn và top-up 1.990→2.000.
- Gate + conveyor EditMode: Passed 4/4 — `work/stage10/editmode-results.xml`; gồm hai seed Farm Shop, gate Processing 199→200/top-up 11.990→12.000, conveyor cargo save/load và giao đúng một lần.
- Windows BuildPipeline: Succeeded, 0 errors, 2 warnings, 115,447,079 bytes — `work/stage10/Build/build-report.json`. Bản build kiểm chứng nằm ở `work/stage10/Build/TYCOON2.exe`, không ghi đè `Builds/Windows/TYCOON2.exe`.
- Chưa chạy Player acceptance mới cho Stage 08–10. Build và EditMode pass chưa chứng minh Player đóng/mở lại, customer routing Farm Shop, worker/conveyor trong scene hoặc đủ ba production chain end-to-end.

### GitHub Công đoạn 06–10

- `e580495` — `feat: add farm livestock route transactions`; đã push lên `origin/main`.
- `e0783e3` — `feat: add area logistics and crew automation`; đã push lên `origin/main`.
- Các commit 08–10 (`c035cae`, `45c535c`, `1ba7b9d`) cũng đã push lên `origin/main`; handoff này ghi lại bằng chứng và backlog sau các mốc đó.
- `ProjectSettings/QualitySettings.asset` (`antiAliasing: 0` → `2`) là thay đổi người dùng có sẵn, được giữ nguyên và nằm ngoài commit/push của nhiệm vụ.

## Công đoạn 11 — SUPERMARKET

- Gate dùng 600 đơn thành công và 50 batch cho từng recipe mill/cheesemaker/saucemaker; Supermarket có giá 65.000. Các route đơn hàng chỉ chọn SKU có recipe/producer đã mở và có thể sản xuất. Đơn market tối đa 2 ItemTypes; giao nhiều SKU đi qua một `DeliverOrder` transaction, nên người chơi/worker không thể giao trùng khi cạnh tranh cùng revision.
- `dairy` vẫn là purchase Processing riêng 1.800 xu từ Công đoạn 10; cần mở nó trước khi tạo batch phô mai/sốt và đạt gate ba recipe.
- Seed sát ngưỡng: `mod/test/stage11-near-supermarket.json` (64.990/65.000, 599/600 đơn, 49/50 mỗi recipe).
- File chính: `CommerceDirector.cs`, `ProgressionTracker.cs`, `RuntimeTransactions.cs`, `TransactionCore.cs`.

## Công đoạn 12 — BAKERY VÀ RESTAURANT

- Bakery mở với Supermarket và 100.000 xu; gate prototype market cấp 3/1.000 đơn đã được gỡ. Oven/cakeoven dùng recipe Flour + Milk + Egg → Bread/Cake; kho, shelf, checkout, khách và crew Bakery riêng đã được nối.
- Restaurant cần Bakery, 60 payment đơn Bakery thành công, oven cấp 3 và 80.000 xu. Flow table/diner, patience 90 giây, payment tại checkout Restaurant và trạng thái bàn bẩn/clean có sẵn trong runtime; worker hire Restaurant còn yêu cầu player tự nấu, phục vụ và dọn ít nhất một lượt.
- Seed: `mod/test/stage12-near-bakery.json`, `mod/test/stage12-near-restaurant.json` (99.990/100.000, 79.990/80.000, 59/60 đơn Bakery, oven cấp 2/3).
- File chính: `Definitions.cs`, `ProgressionTracker.cs`, `RestaurantDirector.cs`, `Station.cs`, `TransactionCore.cs`.

## Công đoạn 13 — EVENTS VÀ BREAKDOWN

- Rush chỉ chạy sau khi có crew, báo trước 15 giây, kéo dài 90 giây và giới hạn tổng khách/khách bàn ở 30. Breakdown đếm machine batch hoàn tất, cảnh báo 15 giây, giới hạn một máy hỏng, không áp dụng lên tuyến crop cà rốt.
- Máy hỏng được sửa riêng qua Repair Zone; sửa 8 giây, phí 10–100 chỉ trừ một lần, rời vùng giữ progress. Break không xóa running job, escrow, input hay output reservation; worker hiện đúng lý do chờ người chơi sửa.
- Trạng thái cảnh báo được lưu ở `SaveData.events`; trạng thái job/repair máy nằm trong transaction state và Save v2.
- Seed: `mod/test/stage13-events-seed.json`. File chính: `GameRules.cs`, `GameSession.cs`, `MachineStation.cs`, `PlayerInteraction.cs`, `Station.cs`, `WorldFactory.cs`, `TransactionCore.cs`.

## Kiểm chứng Công đoạn 11–13

- Stage11 EditMode: Passed 2/2 — `work/stage11/editmode-results.xml`; gate 599→600, 49→50 cho ba recipe, route sản xuất đã mở, đơn nhiều SKU atomic/idempotent, purchase 64.990→65.000.
- Stage12 EditMode: Passed 1/1 — `work/stage12/editmode-results.xml`; bakery/restaurant gate theo đơn, oven level, top-up purchase, recipe availability và player-training gate.
- Stage13 EditMode: Passed 2/2 — `work/stage13/editmode-results.xml`; Rush crew gate/timer, breakdown warning/save, single-broken guard, repair fee once, rời vùng, khôi phục transaction snapshot của machine đang chạy.
- Windows BuildPipeline: Succeeded, 0 errors, 2 warnings; 115.453.743 bytes — `work/stage11-13/Build/build-report.json`. Bản kiểm chứng riêng: `work/stage11-13/Build/TYCOON2.exe`.

### GitHub Công đoạn 11–13

- `5b0a5bb` — `feat: add supermarket bakery and restaurant progression`; đã push lên `origin/main`.
- `6137994` — `feat: add rush hour and machine breakdown events`; đã push lên `origin/main`.
- Handoff ghi lại hai commit code đã push; thay đổi `QualitySettings.asset` vẫn được giữ ngoài commit.

## Chưa thực hiện được / chưa nghiệm thu sau Công đoạn 01–13

- Chưa chơi hết gói sữa 1000 xu hoặc gói trứng 1000 xu trên Windows Player; đã chạy tuyến thịt bò làm vertical slice đại diện.
- Tái đàn từ population 0 và timeout/loss chỉ theo lượng khách nhận có EditMode coverage; Player Stage67 không ép hết patience để kiểm tra nhánh timeout.
- Chưa tuyển rồi để nhiều worker chạy end-to-end trên Player. Hire gate yêu cầu level 3 + 30 job, nên bài Stage67 xuất phát từ 0 không đạt điều kiện tuyển. Role/area/gates, reservation relay, upgrade isolation có EditMode coverage; worker wait/resume theo máy hỏng/kho đầy, tranh slot và stuck recovery chưa có Player acceptance riêng.
- Chưa đóng hẳn Windows Player rồi relaunch để nghiệm thu save/load xuyên process. Save v2, migration và state vật nuôi/máy/crew được kiểm tra bằng EditMode và save/read trong lượt QA.
- Chưa soak 3–4 giờ hoặc nghiệm thu mục tiêu 165 FPS ở 1080p.
- QA Stage67 có nhiều lượt trung gian thất bại ở harness (spawn khách quá dày, đích pad sát ranh va chạm, và một đơn 3 cà rốt hết 90 giây sau khi khách nhận 2). Harness được chỉnh để tạo khách FIFO có kiểm soát, dùng vùng pad thực và ghi nhận timeout/loss đúng quy tắc; Editor cuối pass 268 checks và Player cuối pass 285. Không còn acceptance check thất bại ở lượt cuối.
- Thư mục report tạm của các lượt QA trung gian còn trong `work/stage06-07/`; lệnh xóa bị PowerShell command policy chặn. Chúng không được track và đã xác minh không còn Unity/Player process sử dụng; report Player cuối được giữ làm bằng chứng.
- Công đoạn 08: chưa chạy Windows Player tắt hẳn rồi khởi động lại để nghiệm thu save/load xuyên process; chưa gom toàn bộ state categories vào một phiên Player. EditMode đã pass envelope, migration marker, read lặp và save/read.
- Công đoạn 09: chưa nghiệm thu Player route khách FIFO vào checkout Farm Shop, stocker/cashier/transport worker và giao đơn thực. Đã pass gate hai hướng crop/livestock trong EditMode.
- Công đoạn 10: chưa nghiệm thu Player đủ ba tuyến Wheat→Flour, Milk→Cheese, Tomato→Sauce hoặc hành vi conveyor khi destination full. EditMode đã xác nhận gate, cargo ownership, save/load và giao idempotent; BuildPipeline pass.
- Công đoạn 10: purchase `dairy` hiển thị riêng với giá 1.800 xu vẫn cần để mở hai máy cheese/sauce sau Processing; chưa gộp chi phí này vào purchase Processing 12.000 xu.
- Công đoạn 11: chưa chạy Windows Player để quan sát customer routing/ngẫu nhiên hóa đơn Supermarket và người chơi/worker cùng giao đơn nhiều SKU thật; EditMode chỉ kiểm tra gate, route capability và transaction atomic.
- Công đoạn 12: chưa chạy Player toàn chuỗi Bakery sản xuất/bán Bread/Cake rồi Restaurant reserve → eat → payment → clean; chưa nghiệm thu cảm giác điều khiển/timing 90 giây hoặc Player tự làm ba thao tác trước khi hire. Gate và recipe được kiểm tra bằng EditMode.
- Công đoạn 13: chưa chạy Player chờ Rush/đếm arrival rate ở 30 khách, để machine chạy xuyên breakdown/repair tại zone thật hoặc xác nhận worker resume sau sửa; EditMode kiểm tra timer, state, fee và progress.
- Không chạy soak, benchmark, hoặc kiểm tra mục tiêu FPS theo yêu cầu test gọn.
- Unity tạo thư mục `work/stage11-13/Build/TYCOON2_BackUpThisFolder_ButDontShipItWithYourGame` trong Build. Đã xác minh Unity build process kết thúc; lệnh dọn thư mục bị command policy chặn, nên thư mục này còn lại.
- Bản build Stage11–13 được giữ ở `work/stage11-13/Build/TYCOON2.exe` để dùng cho Player acceptance sau; khi tiếp tục cần kiểm tra file và process trước khi chạy.
- Công đoạn 04/05: các lần thử Player hoàn tất purchase progression trước đây chưa đạt bằng chứng ổn định; one-shot completion chỉ có Editor/core và Stage23 integration coverage, chưa có Player end-to-end purchase gate.
- Bản kiểm chứng `work/stage10/Build/TYCOON2.exe` được giữ để chạy Player acceptance sau; ở lần làm việc liên quan tiếp theo cần kiểm tra file/process trước khi dùng và xóa khi không còn cần.

### Đã thử nhiều lần nhưng vẫn chưa pass

- Công đoạn 04/05: chưa có lượt Player chứng minh hoàn tất purchase progression trong cùng tuyến chơi; các lần thử bị FIFO/patience làm mất bằng chứng ổn định. Đây vẫn là acceptance chưa pass.
- Công đoạn 02/03 và 04/05: dọn report tạm bằng `Remove-Item` đã thử nhiều lần nhưng command policy chặn; artifact không được track và không còn process dùng. Chưa xử lý được việc dọn thư mục vì không được lách policy.

### Lượt thử lỗi đã sửa; kết quả cuối pass

- Công đoạn 08: lượt SaveV2 mới đầu dùng seed stack storage chưa normalize location nên validation thất bại; dùng state qua `TransactionCore` trước khi lưu, lượt cuối 6/6 pass.
- Công đoạn 10: lượt đầu lọc nhiều test class trả 0 test nên không tính là pass; chuyển sang suite StageProgressionTests. Một assertion sau load yêu cầu một wheat stack duy nhất trong khi transfer giữ hai stack hợp lệ; đổi sang tổng lượng theo owner/item, lượt cuối 4/4 pass.
- Công đoạn 10: compile đầu dùng nhầm `Inventory.AvailableAboveReserve`; sửa sang API của `StorageStation`, BuildPipeline cuối succeeded với 0 errors.
- Công đoạn 11–13: compile đầu thiếu `System.Linq` cho breakdown batch count; bổ sung import. Lượt test đầu của batch-delivery dùng fixture thiếu stack location nên transaction validation từ chối; fixture đã đặt đúng owner location. Kết quả cuối Stage11 2/2, Stage12 1/1, Stage13 2/2 pass; BuildPipeline 0 errors.

## Công đoạn 14 — đang thực hiện, mốc transaction / interaction

- Sửa worker có reservation của SKU thứ hai bị bỏ qua khi giao multi-item order; thêm regression dùng worker inventory thật. Đếm job tự chế biến theo batch player hoàn tất, không lấy số lượt đặt nguyên liệu để mở hire.
- Thêm `cashCollected` vào Economy/transaction/Save v2: chỉ tăng ở Collect, không tăng khi tạo payment; giữ giá snapshot của order. HUD tiếng Việt có tiền chưa thu, doanh thu, thực thu, loss, kho/reservation và đội theo nghề/khu.
- Sửa lỗi thời gian chạy dài: runtime không serialize lại toàn bộ receipt/outbox mỗi frame. Save v2 dùng checkpoint + journal command có checksum, flush trước publish, replay trước input/simulation; giữ lịch sử idempotency đầy đủ. Đây là sửa lỗi persistence, không tạo offline progression. File `.journal` là phần bắt buộc của save chưa checkpoint, không phải file tạm.
- Purchase vẫn nhận tiền từng phần; đứng lại 1 giây mới bắt đầu để đi ngang pad không góp nhầm. Rời vùng giữ tiền đã góp. Regression kiểm tra delay, partial, retry, crash trước/sau commit, tail chưa commit và checkpoint.
- File chính: `TransactionCore.cs`, `TransactionRuntimeCopy.cs`, `GameplayTransactionStore.cs`, `RuntimeTransactions.cs`, `TransactionState.cs`, `SaveData.cs`, `Inventory.cs`, `Station.cs`, `MachineStation.cs`, `ProgressionTracker.cs`, `GameHud.cs`; fixture `mod/test/stage14-boundaries.json`.
- Kiểm chứng mốc: EditMode 117/117 pass, 0 fail, 0 skip (`QA/editmode-results.xml`); compile và layout NavMesh pass. Windows build đầu Stage14: Succeeded, 0 errors, 2 warnings (`work/stage14/Build/build-report.json`). Player visual đầu trên RTX 4060/Direct3D11 pass vòng ví 0 → gieo/tưới/thu → giao → Collect (`work/stage14/player-stage14visual-20261005T092655022Z/`).
- Chưa chốt Công đoạn 14: lượt progression đầu dừng vì đi ngang pad khác góp 16 xu, purchase cây trồng cấp 3 còn thiếu 7 xu. Đã sửa delay và harness tiếp tục purchase dở; cần chạy lại. Mục tiêu 180–240 phút và toàn vòng 0 → Restaurant chưa pass. Animation/layout/economy và QA Player cuối đang tiếp tục.
- Git mốc này: `a2c843f` — `fix: harden runtime transactions and purchase interactions`; đã push `origin/main`. `QualitySettings.asset` của người dùng tiếp tục nằm ngoài commit.

## Công đoạn 14 — mốc polish và QA sát ngưỡng — 2026-10-05

**Trạng thái: compile/Windows Player và các ca sát ngưỡng đã pass; chưa nghiệm thu toàn bộ Công đoạn 14.** Lượt cuối dùng file dữ liệu trong `mod/test` theo yêu cầu, không chạy benchmark. Tiền để vượt ngưỡng được kiếm bằng sản xuất → giao hàng → player Collect sau khi nạp preset.

### Hệ thống đã thay đổi

- Layout: đưa ba ô cà rốt, kho và quầy khởi đầu gần nhau; tách phía khách và điểm làm việc/chờ phía sau. Dời chuồng, kho, máy, quầy, purchase pad; mở cổng 4 m giữa tường. NavMesh bake cả footprint công trình chưa unlock. Vùng thao tác chỉ hiện khi trạm tương ứng mở. Bổ sung các pad crew Bakery/Restaurant bị thiếu và sửa purchase conveyor Bakery để có tuyến bột tới oven thật.
- Navigation: giảm radius NPC xuống 0,38 m, dùng avoidance mức vừa; customer/diner phát hiện đứng kẹt, repath tối đa ba lần rồi nghỉ 5 giây. Chỉ NavMeshAgent điều khiển transform NPC. Queue Supermarket và điểm work/wait/interaction tách riêng.
- Camera/animation: perspective pitch 55°, distance 18, follow damping 0,18; đã đối chiếu video reference có sẵn. Giữ idle/walk/carry/pickup/drop và thêm bảy clip work từ animation nguồn, nối Farming/AnimalCare/Operate/Cashier/Cooking/Serving/Cleaning cho player/worker. Tốc độ walk lấy từ chuyển động thực tế; không sửa FBX nguồn.
- Interaction: dừng di chuyển trong zone mới thao tác, tránh đi ngang kho tự lấy sai SKU. Thu hoạch cả batch chỉ thành công khi đủ sức mang; từ chối giữ nguyên cây và hàng. Farmer ưu tiên SKU thiếu, dừng khi kho đã đủ để không làm đầy toàn kho bằng cà rốt và chặn wheat/tomato.
- Economy: cà rốt vẫn 10 xu; harvest cơ bản cà rốt 2, wheat/tomato 4; Capacity của Farm tăng yield riêng. Milk/egg cycle cơ bản 12 giây, chăm ×2; thịt vẫn tiêu thụ một con. Giá ứng viên: flour 120, cheese 240, sauce 120, bread 300, cake/meal 600. `cashCollected` chỉ tăng khi player thu payment; giá từng order vẫn snapshot. Đây là số đang tune, chưa xác nhận mục tiêu 180–240 phút.
- Save/transaction: cập nhật yield/cycle trong runtime state và migration save cũ; giải phóng escrow rỗng sau khi máy hoàn tất. Các sửa durability, receipt/idempotency, batch do player hoàn tất, purchase dwell và HUD đã ghi tại mốc `a2c843f` phía trên. Journal chưa checkpoint là dữ liệu save bắt buộc.
- CLI/QA: thêm layout, visual, progression và near-threshold Player runners. `-LoadPath` từ chối file không tồn tại, tránh báo nhầm pass khi thực tế tạo new game. Preset chỉ ghi save QA riêng; `milestone-details.json` có `seededFixture=true`.

### File chính

- Runtime: `WorldFactory.cs`, `FinalBusinesses.cs`, `NavigationWorld.cs`, `FollowCamera.cs`, `Art.cs`, `PlayerController.cs`, `Station.cs`, `WorkerAgent.cs`, `CommerceDirector.cs`, `RestaurantDirector.cs`, `Definitions.cs`, `TransactionState.cs`, `TransactionCore.cs`, `RuntimeTransactions.cs`, `ConveyorStation.cs`.
- Animation/editor: `Assets/_Game/Art/Animations/Player_*.anim`, `Assets/_Game/Art/Imported/Models/player.controller`, `Assets/_Game/Scripts/Editor/Stage14Tools.cs`.
- QA: `QaDriver.cs`, `QaStage14.cs`, `QaStage14Play.cs`, `QaStage14Milestones.cs`, `Stage14Tests.cs`, `Tools/run_unity.ps1`, `Tools/run_player.ps1`.
- Dữ liệu: `mod/test/stage14-boundaries.json`, `stage14-economy.json`, `stage14-balance.json`, `stage14-player-near-thresholds.json`; hướng dẫn chạy `mod/test/README_STAGE14.md`.

### Kiểm chứng cuối

| Kiểm chứng | Kết quả | Bằng chứng |
|---|---|---|
| Unity EditMode toàn bộ | 118/118 pass; 0 fail/skip | `QA/editmode-results.xml`, 2026-10-05 11:23 UTC |
| Windows build | Succeeded; 0 errors, 2 warnings; 115.678.495 bytes | `work/stage14/Build/build-report.json`, 11:22 UTC |
| Layout trên Windows Player | 424 điểm trạm/pad/work/wait/seat/queue reachable; 0 overlap/failure | `work/stage14/player-stage14layout-20261005T105115195Z/layout-details.json` |
| New game ví 0 trên RTX 4060 | Pass; gieo/tưới/thu/carry/serve/Collect; đi ngang pickup không lấy nhầm | `work/stage14/player-stage14visual-20261005T105119599Z/stage14-visual-report.json` |
| Preset sát ngưỡng, gameplay Player thật | 87 checks pass; 0 runtime errors; khoảng 92 giây thời gian thực | `work/stage14/player-stage14milestones-20261005T111621404Z/stage14-milestones-report.json` |
| Đóng/mở lại Windows Player | 15 checks pass; restore ví/thực thu/unlock trước input; RTX 4060/D3D11, 1920×1080 | `work/stage14/player-stage14visual-20261005T112257057Z/stage14-visual-report.json` |

Ảnh starter, carry và Restaurant/overview đã xem lại. Lượt relaunch dùng `work/stage14/player-stage14milestones-20261005T110523261Z/qa-save-v2.json`: ví 890, thực thu 18.600 và các khu đã mở đúng dữ liệu đã lưu. Giá trị lịch sử trong preset không được dùng làm bằng chứng tốc độ kiếm tiền từ 0.

| Purchase | Ví nạp | Ví sau bán/Collect thật | Chi phí | Ví sau mua |
|---|---:|---:|---:|---:|
| Meat route | 490 | 510 | 500 | 10 |
| Milk route | 990 | 1.010 | 1.000 | 10 |
| Farm Shop | 1.990 | 2.010 | 2.000 | 10 |
| Processing (`mill`) | 11.990 | 12.010 | 12.000 | 10 |
| Supermarket | 64.990 | 65.010 | 65.000 | 10 |
| Bakery | 99.990 | 100.010 | 100.000 | 10 |
| Restaurant | 79.990 | 80.290 | 80.000 | 290 |

Farm Shop 49→50 và Processing 199→200 đơn qua sale thật; Supermarket chạy cả ba máy 49→50 batches rồi bán đơn thứ 600; Bakery 59→60 đơn bằng bánh player vận hành oven. Gói meat/milk tiếp tục cho ăn → chăm → lấy → bán → Collect; meat giảm đàn đúng một con. Restaurant player cook → giao đúng bàn → eat/payment → dirty → clean → Collect.

Regression Player cuối còn xác nhận: rời máy giữ input/job/progress, load rồi tiếp tục đúng batch; đơn 3 cà rốt nhận 2 rồi hết đủ 90 giây giữ 2 ở owner customer, ghi loss 2 và 0 payment; load/retry không ghi loss lần hai; ba lần load không clone hàng/tiền; 30 customer có model/NavMesh thật tồn tại đồng thời, không vượt cap và repath được giới hạn trong ca congestion ngắn. Unit suite kiểm tra thêm payment/timeout sát deadline, reserve hết nguồn, capacity, retry, purchase partial/one-shot và journal recovery.

### GitHub và artifact

- `a2c843f` — `fix: harden runtime transactions and purchase interactions`; đã push `origin/main`.
- `b738066` — `feat: polish world flow and add near-threshold player QA`; đã push `origin/main`.
- Tài liệu cập nhật ở commit kế tiếp có message `docs: record stage 14 QA and remaining acceptance`. Remote: `https://github.com/letam10/TYCOON2.git`, branch `main`.
- Build để tiếp tục chơi: `work/stage14/Build/TYCOON2.exe`. Report/ảnh và save QA được giữ làm bằng chứng; không track binary/save vào Git. Save của lượt 0→Farm Shop còn tại `work/stage14/player-stage14-20261005T103237113Z/qa-save-v2.json` cùng journal, phục vụ tiếp tục acceptance còn thiếu.
- `ProjectSettings/QualitySettings.asset` có sẵn của người dùng và các file mới `ASSET/Downloaded/**`, `ASSET/downloaded_summary.json` từ công việc khác được giữ ngoài commit này.
- Đã đối chiếu PID/executable/thời điểm/parent với lịch sử task: không còn Unity/Player/helper của lượt này chạy; không có process cần giữ. Lệnh dọn các file tạm đã xác minh bị cơ chế tự động duyệt lệnh từ chối với `blocked by policy`; không lách chặn. Thư mục debug Unity `work/stage14/Build/TYCOON2_BackUpThisFolder_ButDontShipItWithYourGame`, ảnh contact sheet `work/stage14/reference-1971.png`, save tạo nhầm `work/stage14/player-stage14milestones-20261005T110523261Z/qa-save.json` và `.journal`, cùng các thư mục rỗng `QA/Evidence/stage14*` còn tồn tại, không còn process sử dụng. Khi có cơ chế dọn được cho phép, chỉ xóa đúng các artifact này.

## Công đoạn 14 — audit và tích hợp ASSET local — 2026-10-05

**Mốc thư viện đã kiểm chứng; mốc world dressing/QA đã hoàn tất như phần tiếp theo.** Làm trực tiếp project hiện có, không thêm nội dung/gameplay khu mới.

- Rà soát/hash toàn bộ 7.553 file ASSET (409,5 MiB), 3.668 file model gồm các định dạng khác nhau; 177 nhóm SHA trùng. Kiểm tra GLB/glTF metadata và dependencies, OBJ geometry counts, FBX header; không phát hiện lỗi trong phạm vi đó. Không tuyên bố đã kiểm tra deformation của toàn bộ rig.
- Chọn 48 model phù hợp từ Kenney/KayKit/Quaternius; 12.372 triangles tổng thư viện, tối đa 1.612/model, 58 renderers/73 material slots trong Unity. Model tĩnh không có collider, pivot sát đất và bounds khớp manifest. Texture/material dùng URP; giữ license CC0 và SHA nguồn.
- Giữ nhân vật/vật nuôi đang có rig, stable IDs, Input System, pooling, inventory/transaction/save. Thư viện fantasy, xe/nhà công nghiệp và character Sushi không được dùng khi không hợp chức năng/style hoặc thiếu license cục bộ.
- File chính: `Assets/_Game/Art/Library/**`, `GameCatalog.cs`, `GameCatalog.asset`, `AssetLibraryBuilder.cs`, `ProjectBuilder.cs`, `Tools/audit_asset_library.py`, `Tools/prepare_library_assets.py`, `Tools/run_unity.ps1`, `Docs/ASSET_AUDIT.md`, `Docs/asset-library-import.json`.
- Kiểm chứng: import 48/48 bounds/pivot/material/collider pass; Windows build và Player RTX 4060 pass; 118/118 EditMode pass tại 13:31 UTC. Ảnh toàn bộ model được chọn đã xem. Runtime/new-game và path evidence được chốt ở mốc tiếp theo.
- Git mốc thư viện: `bdf34b7` — `feat: import curated local tycoon asset library`; đã push `origin/main` (52 LFS objects, khoảng 1,4 MB).
- Raw ASSET có sẵn và `QualitySettings.asset` có sẵn của người dùng nằm ngoài commit. Chỉ commit derivative cần cho game cùng metadata/licenses/CLI. Build không phụ thuộc thư mục raw ASSET; tái xử lý art trên máy khác cần nguồn local tương ứng.
