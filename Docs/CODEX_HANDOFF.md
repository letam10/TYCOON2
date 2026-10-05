# CODEX_HANDOFF

Chat Codex tiếp theo phải đọc file này trước khi làm việc. Tiếp tục trực tiếp D:\GAME\TYCOON2; không tạo project mới.

## Công đoạn hiện tại

- Công đoạn 01 — AUDIT VÀ BASELINE: HOÀN THÀNH. Prototype mâu thuẫn trong phạm vi audit đã được chặn/sửa; baseline Editor và Windows Player đều đạt.
- Công đoạn 02 — DATA, OWNERSHIP VÀ TRANSACTION CORE: HOÀN THÀNH. Core là writer authoritative cho item/location, order, payment, purchase, crew, producer, table và machine; save v2 ghi state/receipt/dedup/outbox cùng một file.
- Công đoạn 03 — PLAYER INTERACTION: HOÀN THÀNH. Điều khiển WASD/gamepad theo camera, camera 55°, vùng thao tác và stack một loại 6/10/16/24 được kiểm tra trong Editor và Windows Player.
- Công đoạn 04 — FARM STARTER VERTICAL SLICE: HOÀN THÀNH VỀ IMPLEMENTATION. Farm cà rốt 0 vốn, FIFO order, giao từng phần, payment tại counter và player collect đã được tích hợp; giới hạn kiểm chứng Player xem mục cuối.
- Công đoạn 05 — PURCHASE & PROGRESSION: HOÀN THÀNH VỀ IMPLEMENTATION. Purchase góp dở, state hiển thị và progression gates/upgrade axes riêng đã được tích hợp; chưa nghiệm thu trọn chuỗi unlock bằng một phiên Player.
- Công đoạn 06 — FARM LIVESTOCK VERTICAL SLICE: HOÀN THÀNH VỀ IMPLEMENTATION. Tuyến thịt bò 500 xu được chơi từ ví 0 đến sản xuất, giao, payment và collect trên Windows Player; tuyến sữa/trứng thay thế chưa chạy Player.
- Công đoạn 07 — LOGISTICS VÀ EMPLOYEES: HOÀN THÀNH VỀ IMPLEMENTATION. Kho theo area/SKU, reservation, crew theo nghề/khu, cổng tuyển và worker AI đã được nối; chưa nghiệm thu trọn vòng đời nhân viên đã tuyển bằng Player.
- Công đoạn 08 — SAVE V2: HOÀN THÀNH VỀ IMPLEMENTATION. Save độc lập, transaction marker v2, nạp save cũ có chuẩn hóa, khóa input tới khi restore xong; chưa xác nhận Player đóng/mở lại.
- Công đoạn 09 — FARM SHOP: HOÀN THÀNH VỀ IMPLEMENTATION. Quầy/catalog/khách riêng và gate 2.000 xu + 50 đơn với Farm cấp 3 hoặc Animal cấp 3; chưa nghiệm thu trên Player.
- Công đoạn 10 — PROCESSING AREA: HOÀN THÀNH VỀ IMPLEMENTATION. Gate 12.000 xu + Farm Shop + Animal cấp 3 + 200 đơn, các recipe và conveyor có ownership/reservation/save; chưa nghiệm thu trọn tuyến trên Player.
- Cập nhật bàn giao: 2026-10-05, múi giờ Asia/Bangkok.

## Môi trường và Git

- Unity 6000.6.3f1; URP 17.6.0; Input System 1.19.0; AI Navigation 2.0.12.
- Windows single-player offline, giao diện tiếng Việt.
- Repository D:\GAME\TYCOON2, branch main, remote https://github.com/letam10/TYCOON2.git.
- Mốc audit đã commit và push: 3582a92 — fix: establish stage 01 interaction and ownership baseline.
- Follow-up CLI đã commit và push: 3a4a6d6 — fix: preserve default Windows build report path.
- Mốc runtime trước đó: 47e1e9f — feat: add durable ownership and transaction core.
- Mốc Công đoạn 02 đã push: fdbe67f — feat: extend authoritative transaction state.
- Công đoạn 04/05 đã commit và push: e909605 — feat: add farm starter and progression systems.
- Công đoạn 06 đã commit/push: e580495 — feat: add farm livestock route transactions.
- Công đoạn 07 đã commit/push: e0783e3 — feat: add area logistics and crew automation.
- Công đoạn 08 đã commit/push: c035cae — feat: finalize save v2 envelope and gated restore.
- Công đoạn 09 đã commit/push: 45c535c — feat: add Farm Shop progression and dedicated checkout.
- Công đoạn 10 đã commit/push: 1ba7b9d — feat: add saved logistics conveyors for processing.
- Chỉnh sửa interaction có trước nhiệm vụ trong Art, FollowCamera, GameHud, GameSession, PlayerController, Station và PlayerInteraction đã được rà soát, tích hợp vào Công đoạn 03, theo xác nhận của người dùng trước khi commit. QualitySettings.asset và ProjectSettings/ProjectSettings.asset không nằm trong commit nhiệm vụ.

## Phần đã thực hiện

- Sửa lệch API WorkerAgent.IsAnimal thành ProductionStation.Animal để giải quyết lỗi compile ban đầu.
- Bỏ Economy.TryBuy và Economy.TryCheckout prototype; giữ luồng góp tiền Contribute và tiền chờ thu tại quầy.
- Checkout và vùng serve chỉ nhận hàng từ giỏ người giao, không tự lấy hàng từ shelf khi giỏ trống.
- Sửa StorageFor để Farm và Farm Shop trỏ tới storage owner riêng; worker mới không thể mua nếu khu của họ chưa có kho.
- Worker cũ trong save có khu chưa có kho được giữ chờ, không tạo GameObject lặp hoặc ném exception; carry trong PendingWorkers vẫn được lưu.
- Thêm vùng thao tác có nhãn/màu riêng cho lấy, đặt, vận hành, giao và thu tiền trên các trạm hiện có; cashier đi tới vùng giao hàng có thể tiếp cận.
- Cashier giữ hàng cho tới khi đúng khách/đơn nhận được hàng; chỉ chuyển hàng về kho cửa hàng khi đơn không còn cần món đang mang.
- Customer pending được khôi phục sau khi NavMesh sẵn sàng kể cả QA foundation; milestone 0 chỉ chặn sinh khách mới.
- Save/load bỏ StationZone khỏi danh sách trạm bền vững, nhưng vẫn kiểm tra stable ID; thao tác action zone không làm sai số lượng stationStates.
- Sửa menu Editor để Play Baseline và Build Windows gọi đúng phương thức; BuildWindows nhận đường dẫn đầu ra riêng để build kiểm chứng không ghi đè bản build có sẵn.
- Chặn nâng cấp kind legacy và receipt không hợp lệ; receipt thanh toán và thất thoát loại trừ nhau, kể cả đơn hết hạn chưa nhận hàng.
- Inventory.Restore kiểm tra danh sách hàng trước khi thay thế, giữ hàng/reservation cũ nếu dữ liệu lỗi.
- Bổ sung validation save v2 cho owner, receipt và cash; save lỗi chặn autosave thay vì ghi đè file cũ.
- Load reset khách/worker cũ; giữ PendingCustomers/PendingWorkers/PendingDiners khi lưu lại trước khi spawn; sửa NextReceipt tăng theo các receipt đã lưu.
- Trạng thái trạm dùng stationStates, không áp lại progress prototype từ production; đầu vào máy có owner riêng dạng stationId_input.
- BuildWindows dùng scene hiện có, không gọi lại CreateScene hay cấu hình art.
- Giữ stable ID, Input System, pooling, inventory primitives và CLI hiện có. Không thêm nội dung nghiệp vụ Farm Shop/Processing/Supermarket/Bakery/Restaurant trong công đoạn này.
- RuntimeTransactions khởi tạo từ state đã lưu hoặc chuyển save v2 cũ; core dừng gameplay khi checksum/reference/schema lỗi. File save bền vững dùng chung snapshot giao dịch, receipt và outbox.

## Audit theo hệ thống

| Hệ thống | Trạng thái / điểm bàn giao |
| --- | --- |
| Quyền sở hữu item | Mỗi stack có ID, owner và location. Player/worker/customer, từng kho, counter, machine/input/escrow có owner riêng; owner chỉ ghi bởi TransactionCore. |
| Inventory | Inventory scene là projection chỉ đọc của core; transfer/reservation/split/merge/expiry đi qua command; giữ primitives cho test và save v2 cũ. |
| Kho | storage_farm và storage_farm_shop được tra theo AreaId riêng; khu không có kho không fallback về kho khác. |
| Customer/order | OrderState giữ SKU/giá/đã giao/hạn/trạng thái; giao từng phần đưa hàng vào owner customer; complete/fail loại trừ nhau. |
| Giao hàng | Serve chỉ lấy từ player/worker và đúng đơn; quá hạn giữ phần hàng khách đã nhận, không hoàn trả vào kho. |
| Payment/cash | Tạo payment theo order một lần; tiền ở quầy là payment chưa thu; command collect đưa tiền vào player một lần. |
| Farm Shop | `shelf_farm_2` và `checkout_farm_shop` là lane riêng; khách/cashier route theo shop ID. Mở từ Farm cấp 3 hoặc Animal cấp 3 sau 50 đơn và góp đủ 2.000 xu. |
| Machine/logistics | Input tiêu thụ sang owner escrow khi operator khởi động; output được giữ chỗ; progress, hỏng/sửa đi qua core. Conveyor dùng owner thật và source/destination reservation; hàng đã lên belt vẫn tồn tại khi đích nghẽn và sau load. |
| Purchase/unlock | Góp dở, hoàn tất, grant và sức mang worker/player nằm trong core. Gate Farm Shop hỗ trợ hai hướng Farm/Livestock; Processing yêu cầu Farm Shop, Animal cấp 3, 200 đơn và 12.000 xu. |
| Save/load | `save-v2.json` độc lập với prototype save; core state, inventories theo owner, customer/order, machine, crew, purchase, payment và belt/reservation ghi nguyên tử. Input bị khóa tới khi restore/core init xong; không chạy offline. |
| NPC navigation | NavMesh, khách đến hàng chờ, cashier đến vùng phục vụ và worker chờ khu thiếu kho đã được kiểm tra trong Windows Player. |
| Upgrade | ItemDefinition/RecipeDefinition/UpgradeDefinition giữ stable ID/version; CrewState và PurchaseProgress là state core. |
| Worker | Crew/profession/area/capacity là core; route mang hàng dùng reservation bền vững xuyên owner worker. |
| Interaction | Camera perspective 55°, WASD/gamepad, chỉ zone đang đứng thao tác; fail/full/SKU sai hiện lý do và bảo toàn hàng. |

## Kết quả kiểm chứng thực tế

- EditMode: Passed 65/65, Failed 0, Skipped 0; work/stage01/codex-audit-20261004i/editmode-results.xml.
- Editor PlayMode headless: Passed 180/180; work/stage01/codex-audit-20261004i/play/baseline-results.json.
- Windows Standalone Player headless: Passed 180/180; work/stage01/codex-audit-20261004i/windows-player-qa/baseline-results.json.
- Build Windows từ scene hiện có: Succeeded, 115,292,815 bytes, 0 errors, 2 warnings; work/stage01/codex-audit-20261004i/windows-build-report.json. Bản build tạm đã được dọn sau khi Player QA hoàn tất.
- Các lần xác nhận runtime chạy bằng -batchmode -nographics; graphics-device.json ghi Null Device. Chưa nghiệm thu renderer RTX 4060, hình ảnh hoặc chất lượng chơi bằng cửa sổ.
- Log cũ work/logs/baseline.log từng có lỗi UnityEditor.Search.SearchDatabase; các báo cáo QA mới ghi errors=0. Giữ log cũ làm bằng chứng lịch sử, không xem lỗi Editor cũ là lỗi gameplay đã tái hiện.

## File chính

- Assets/_Game/Scripts/Runtime: RuntimeTransactions.cs, TransactionCore.cs, TransactionState.cs, Inventory.cs, Definitions.cs, OrderState.cs, GameRules.cs, GameSession.cs, Station.cs, MachineStation.cs, CommerceDirector.cs, WorkerAgent.cs, RestaurantDirector.cs, SaveData.cs.
- Assets/_Game/Scripts/Runtime/QaBaseline.cs và .meta: baseline integration harness chạy bằng --qa-baseline.
- Assets/_Game/Scripts/Editor/ProjectBuilder.cs: build từ scene hiện có và entry PlayBaseline.
- Assets/_Game/Tests/Editor: CoreTests.cs, CommerceV2Tests.cs, BaselineTests.cs và .meta.
- Assets/_Game/Scripts/Runtime/PlayerInteraction.cs: framework Pickup/Drop/Operate/Serve/Collect/Purchase và session tự thao tác theo zone.
- Tools/run_unity.ps1 và Tools/run_player.ps1: task Stage23 kiểm tra trong scene Editor/Windows Player; player có thể nhận đường dẫn build riêng.

## Phạm vi đã đóng trong Công đoạn 01

- Giữ nguyên nền tảng stable ID, Input System, pooling, inventory primitives, save v2, CLI và transaction core độc lập.
- Không thêm nội dung hay mở khu Farm Shop/Processing/Supermarket/Bakery/Restaurant mới; chỉ nối thao tác vào các trạm đã có trong scene.
- Các báo cáo kiểm chứng giữ tại work/stage01/codex-audit-20261004i/: EditMode XML, Editor PlayMode, Windows Player, graphics audit và Windows build summary.
- Các lượt thử hỏng trước khi baseline đạt đã được dọn. Binary build tạm và QA save tạm cũng đã được dọn; chỉ giữ báo cáo kiểm chứng cuối.
- Không có Unity/Player process do lượt này khởi chạy còn chạy. QA dùng -nographics, không chạy GPU workload.

## Trạng thái bàn giao

Cập nhật này là ghi chép lịch sử tại mốc bàn giao Công đoạn 01; thông tin Công đoạn 02/03 bên dưới đã thay thế nhận định cũ rằng transaction core chưa tích hợp runtime. Trạng thái hiện hành sau Công đoạn 04/05 được ghi tại các mục cuối tài liệu.

## Công đoạn 02 — DATA, OWNERSHIP VÀ TRANSACTION CORE — HOÀN THÀNH

### Kiến trúc hiện tại

- ItemDefinition, RecipeDefinition và UpgradeDefinition dùng stable ID/version; TransactionState v1 tham chiếu bằng ID thay vì GameObject.
- TransactionCore là writer duy nhất của TransactionState. Hệ gameplay đọc projection qua RuntimeTransactions, Inventory, Economy, OrderState và Station; mutator cũ bị chặn khi object đã bind.
- Stack có ID, item, quantity, owner và location. Player, worker, customer, từng kho/counter/machine, machine input/output và escrow có owner riêng. Take/Place/Transfer kiểm tra quyền, capacity, single-item, loại hàng, reservation và revision.
- Reserve/Release, split/merge, giao đủ/một phần, complete/fail order, payment/collect, góp/grant purchase, nâng crew, chăm/thu hoạch producer, table/diner và job machine đều dùng command key, effect ID, expected revision, receipt và outbox. Retry cùng hiệu ứng không áp lại thay đổi; xung đột key/payload bị từ chối.
- Worker giữ chỗ stock và đích qua giỏ relay; nguyên liệu machine chuyển sang owner escrow khi bắt đầu job. Dừng operator dừng tăng tiến độ; reservation đầu ra phục hồi sau load.
- SaveData v2 giữ format cũ khi transaction marker vắng mặt. Save core mới ghi TransactionState, projection, cash/customer/crew/worker và receipt qua cùng SaveStore atomic replace; checksum/reference lỗi chặn điều khiển và ghi save.
- Không có processor tự mua hàng. Player phải đứng trong Pickup/Drop/Operate/Serve/Collect/Purchase zone. Farm Shop và Farm vẫn dùng owner kho riêng.

### Công đoạn 03 — PLAYER INTERACTION — HOÀN THÀNH

- PlayerController dùng WASD và gamepad; vector chiếu lên mặt phẳng theo yaw camera, pitch 55°, follow damping, gamepad/key bindings từ Input System.
- PlayerInteractionSession chọn zone khả dụng gần nhất, thực hiện khi đứng trong vùng và gọi Exit khi rời vùng/tắt điều khiển. Pickup/Drop/Operate/Serve/Collect/Purchase là loại tách biệt; Farm, machine, counter và table đi qua cùng target framework.
- Stack player single-item, capacity 6 mặc định, tăng 10 → 16 → 24 qua purchase. InventoryStack tạo đúng số GameObject pooled theo SKU thật, không tự đổi SKU. Hàng vẫn giữ nguyên khi đích không nhận/sức mang đầy và HUD hiện nguyên nhân bằng tiếng Việt.
- QA đo nhận diện 6 item GameObject carrot và xác nhận HUD hiển thị 6/6 trong Player Windows chạy nhìn thấy được.

### File thay đổi chính

- `Assets/_Game/Scripts/Runtime/TransactionCore.cs`, `TransactionState.cs`, `RuntimeTransactions.cs`, `Inventory.cs`, `SaveData.cs`: transaction authority, projections, owner/location, reservation, durable journal và recovery.
- `Definitions.cs`, `OrderState.cs`, `GameRules.cs`, `GameSession.cs`, `Station.cs`, `MachineStation.cs`, `CommerceDirector.cs`, `WorkerAgent.cs`, `RestaurantDirector.cs`: order/economy/purchase/crew/machine/interaction runtime.
- `PlayerInteraction.cs`, `PlayerController.cs`, `FollowCamera.cs`, `Art.cs`, `GameHud.cs`: điều khiển, vùng, stack 3D, camera và feedback.
- `Assets/_Game/Tests/Editor/RuntimeAuthorityTests.cs`, `TransactionRuntimeRulesTests.cs`, `PlayerInteractionTests.cs`, `Tycoon.Tests.asmdef`: kiểm thử contract, save/recovery, carry, input và interaction.
- `Assets/_Game/Scripts/Runtime/QaStages.cs`, `Tools/run_unity.ps1`, `Tools/run_player.ps1`: kiểm thử tích hợp 02/03 bằng scene và Player có sẵn. Không tạo nội dung nghiệp vụ mới.

### Kết quả kiểm chứng

- Unity 6000.6.3f1 EditMode: Passed 81/81, Failed 0, Skipped 0. Báo cáo: `work/stage02-03/run-20261004/editmode-release.xml`.
- Editor PlayMode scene thật: Passed 167 checks; core revision 136, receipts 136, events 136. Chạy `-nographics` nên graphics device là Null Device. Báo cáo: `work/stage02-03/editor-stage23/stage23-results.json`.
- Windows BuildPipeline: Succeeded, 115,379,143 bytes, 0 errors, 2 warnings. Báo cáo: `work/stage02-03/run-20261004/windows-build-final2/build-report.json`.
- Probe xác nhận NVIDIA GeForce RTX 4060 Laptop GPU, Direct3D11, 1920×1080. Windows Player QA trên RTX 4060: Passed 167 checks; revision/receipt/outbox đều là 136; errors rỗng. Báo cáo và ảnh vùng mang hàng: `work/stage02-03/player-stage23-20261004T161531029Z/stage23-results.json`, `graphics-device.json` và `carry-six.png`.
- Ảnh PNG đã xem: camera perspective/scene, HUD `Cà rốt: 6/6`, stack carrot hiển thị trên player. Đây chỉ là ảnh QA tại một trạng thái; chưa đo FPS hoặc nghiệm thu hiệu năng 3–4 giờ.
- Test bao gồm owner/capacity/authority, idempotency/effect uniqueness, reservation relay, split/merge, worker route, escrow máy, order partial/timeout/settlement, payment/collect, purchase/carry upgrades, old save compatibility, crash trước/sau file replace, corrupt reference và interaction zone.
- QA chỉ lái scene đang có; các khu và nội dung Farm Shop/Processing/Supermarket/Bakery/Restaurant không được mở rộng trong hai công đoạn này.
- Build output Windows, QA save và log tạo cho lượt xác minh còn trong `work/stage02-03/`. Yêu cầu dọn các đường dẫn tạm đã bị PowerShell command policy từ chối (`Remove-Item: blocked by policy`); không có Unity, Player hoặc process con nào còn chạy. Các file này không được đưa vào Git.

### Git và trạng thái tiếp theo

- Core Công đoạn 02 đã commit/push: `fdbe67f` — `feat: extend authoritative transaction state`.
- Runtime/interaction Công đoạn 02/03 đã commit/push: `3d6ba3a` — `feat: integrate runtime transactions and player interactions`.
- Handoff tại mốc này được cập nhật sau kiểm tra Player/render; bản audit 01–03 sau đó đã được push thành `b017f62`.
- Tại thời điểm handoff Công đoạn 02/03 (2026-10-04), ba công đoạn đầu đã hoàn tất; các công đoạn sau chưa triển khai. Trạng thái này được cập nhật tại mục Công đoạn 04/05 bên dưới.
- `ProjectSettings/QualitySettings.asset` là thay đổi có sẵn trước lượt này và vẫn được giữ ngoài commit. `ProjectSettings/ProjectSettings.asset` bị Unity BuildPipeline tự thêm Input Actions vào `preloadedAssets`; thay đổi build-generated này được hoàn nguyên, không đưa vào commit.
- Báo cáo EditMode/build/QA và ảnh được giữ trong `work/stage02-03/`. File save QA, binary Windows và log tạm cũng còn ở đó vì các lệnh PowerShell xóa bị Windows command policy từ chối (`Remove-Item: blocked by policy`) dù đã khoanh đường dẫn task; chúng không nằm trong Git. Không còn Unity/Player/process con nào chạy.

## Rà soát cuối 3 công đoạn — phần chưa làm và chưa nghiệm thu

### Hạng mục chưa thực hiện hoặc chưa có bằng chứng pass

- Công đoạn 01: các tiêu chí baseline có báo cáo pass; không tìm thấy lỗi acceptance còn mở trong phạm vi công đoạn.
- Công đoạn 02: chưa chạy bài xác minh save/load bằng cách đóng hẳn rồi khởi chạy lại Windows Player. Đã có test load lặp, save atomic/recovery và tương thích save cũ trong cùng tiến trình; đây chưa phải bằng chứng relaunch thực tế.
- Công đoạn 03: chưa chơi hết luồng worker đã tuyển/mở khóa trong Windows Player và chưa chạy end-to-end toàn bộ vòng đời khách nhà hàng trên Player. Worker relay, bàn và các vùng thao tác có test/integration coverage, nhưng chưa thay thế được hai lượt chơi trọn luồng này.
- Công đoạn 03: ảnh stage02/03 chỉ bao phủ một trạng thái và có nhãn vùng station trông gần nhau. Sau đó đã giới hạn nhãn theo vùng đang hoạt động; ảnh Player Công đoạn 04/05 được xem lại, không còn hiện tượng chồng nhãn rõ trong góc chụp đó. Chưa có rà soát hình ảnh toàn scene ở nhiều góc.
- Xuyên suốt: chưa đo nghiệm thu mục tiêu 165 FPS ở 1080p trong thời gian dài hoặc phiên tiến trình 3–4 giờ.
- Tại mốc rà soát 01–03, chưa có nội dung Farm Starter hay progression. Công đoạn 04/05 sau đó bổ sung tuyến Farm cà rốt và điều kiện unlock; nội dung gameplay riêng của Farm Shop, Processing, Supermarket, Bakery và Restaurant vẫn chưa được xây (đúng giới hạn Công đoạn 05).
- Dọn artifact QA/build tạm của Công đoạn 02/03 dưới `work/stage02-03/` đã được thử nhiều lần nhưng chưa xong: lệnh `Remove-Item` bị command policy từ chối (`blocked by policy`). Các file còn lại không đưa vào Git; không còn tiến trình Unity/Player của lượt làm việc giữ chúng mở.

### Lượt thử lỗi lặp đã được khắc phục; kết quả cuối pass

- Công đoạn 02: lỗi trùng tên biến cục bộ `WorkerAgent.IsAnimal`, thiếu tham chiếu assembly Input System trong test interaction và tương thích save cũ khi Unity tạo object giao dịch rỗng đã được sửa. Bộ EditMode cuối đạt 81/81.
- Công đoạn 03: hai ảnh QA Player đầu không dùng được (Player ẩn cho ảnh đen; lần chụp kế tiếp trước khi HUD cập nhật nên hiện 0/6). Harness đã chờ thêm một frame; lượt Player cuối hiển thị 6/6 và đạt 167/167 checks.
- Công đoạn 02/03: lần Windows BuildPipeline đầu không tự thoát; tiến trình task-owned đã được kết thúc và lần build cuối có `-quit` thành công. Build cuối báo 0 lỗi và 2 cảnh báo.
- Không có lỗi compile, test hoặc Player QA nào còn được ghi là thất bại trong báo cáo cuối: Công đoạn 01 EditMode 65/65, Editor/Windows Player 180 checks mỗi lượt; Công đoạn 02/03 EditMode 81/81, Editor/Windows Player 167 checks mỗi lượt, errors rỗng. Kết quả Editor là `-nographics`; kiểm tra đồ họa nhìn thấy được chỉ có lượt Windows Player trên RTX 4060 đã nêu ở trên.

### Git của lượt rà soát này

- Mốc audit 01–03 trước khi cập nhật khi đó ở `2218095`; tài liệu audit được commit thành `b017f62` và đã nằm trên `origin/main`. Mã Công đoạn 04/05 hiện được commit/push thành `e909605`.
- `ProjectSettings/QualitySettings.asset` vẫn là thay đổi có sẵn của người dùng (`antiAliasing: 0` → `2`); được giữ nguyên và không đưa vào commit/push.

## Công đoạn 04 — FARM STARTER VERTICAL SLICE — HOÀN THÀNH VỀ IMPLEMENTATION

- Scene khởi đầu có ba ô cà rốt, gieo không mất tiền; chăm sóc/tưới, grow và harvest đi qua transaction core. Tuyến này vẫn hoạt động khi wallet bằng 0.
- Farm customer tạo order FIFO 1–3 cà rốt, giá 10 xu/củ được snapshot lúc tạo order, patience 90 giây từ lúc vào queue. Giao có thể từng phần; khách hết hạn giữ hàng đã nhận, không trả hàng về kho, chỉ ghi loss với số đã nhận. Đơn chưa nhận hàng không tạo loss.
- Hàng player đặt vào stock counter có owner riêng; Serve hỗ trợ giao từ carry hoặc từ counter. Đủ order tạo payment chưa thu tại counter; chỉ Collect Zone do player kích hoạt mới tăng wallet.
- `CommerceDirector.cs`, `PlayerInteraction.cs`, `Station.cs`, `WorldFactory.cs`, `Inventory.cs`, `OrderState.cs` và `RuntimeTransactions.cs` nối vòng farm–carry–counter/direct serve–payment–collect. `QaDriver.cs`, `Tools/run_unity.ps1` và `Tools/run_player.ps1` có `--qa-stage45`.

## Công đoạn 05 — PURCHASE & PROGRESSION — HOÀN THÀNH VỀ IMPLEMENTATION

- Purchase state hiển thị Locked / Available / Contributing / Purchased; góp tiền theo phần khi player đứng trong pad, dừng khi rời vùng hoặc hết tiền, giữ phần đã góp và chỉ grant một lần.
- Purchase pad được đặt ngoài footprint khu xây dựng. HUD chiếu mục tiêu tiếp theo và blockers kể cả khi pad đang Locked/chưa xuất hiện.
- `ProgressionTracker.cs` tổng hợp điều kiện trạm, order/job thành công, recipe batch và unlock. `Definitions.cs` chứa gates; `UpgradeAxis` tách riêng Quality/Value, Speed và Capacity, không dùng một level để tăng tất cả.
- `TransactionCore.cs`, `RuntimeTransactions.cs`, `GameRules.cs` và `GameHud.cs` là các điểm nối transaction, projection, validation và HUD. Đã giữ hệ thống purchase/save và state cũ tương thích qua migration đang có.
- Mở Farm Shop → Processing → Supermarket → Bakery → Restaurant mới dừng ở điều kiện/progression tracker; không xây nội dung gameplay các khu sau trong công đoạn này.

### GitHub

- Implementation Công đoạn 04/05: `e909605` — `feat: add farm starter and progression systems`; đã push lên `origin/main`.
- Các mốc handoff/audit trước đó: `b017f62` (rà soát Công đoạn 01–03); `e909605` là code HEAD trước lần cập nhật handoff này.
- Nội dung handoff này ghi nhận code commit `e909605`; `ProjectSettings/QualitySettings.asset` vẫn là thay đổi người dùng và nằm ngoài commit code (`antiAliasing: 0` → `2`).

### File và bằng chứng Công đoạn 04/05

- File runtime mới/chính: `Assets/_Game/Scripts/Runtime/ProgressionTracker.cs` (+ `.meta`); `Definitions.cs`, `GameRules.cs`, `TransactionCore.cs`, `RuntimeTransactions.cs`, `Inventory.cs`, `OrderState.cs`, `CommerceDirector.cs`, `PlayerInteraction.cs`, `Station.cs`, `WorldFactory.cs`, `GameHud.cs`, `QaDriver.cs`.
- Test chính: `Assets/_Game/Tests/Editor/BaselineTests.cs`, `CommerceV2Tests.cs`, `CoreTests.cs`, `PlayerInteractionTests.cs`, `RuntimeAuthorityTests.cs`.
- EditMode: Passed 86/86, Failed 0, Skipped 0 — `work/stage04-05/run-20261005/editmode-acceptance-final.xml`.
- Editor Stage23 integration: Passed, errors rỗng, revision/receipts/events 136 — `work/stage04-05/editor-stage23-acceptance/stage23-results.json` (Null Device).
- Editor Stage45: Passed 106 checks, runtime errors 0 — `work/stage04-05/editor-stage45-acceptance/stage45-report.json` (Null Device).
- Windows BuildPipeline: Succeeded, 115,410,055 bytes, 0 errors, 2 warnings — `work/stage04-05/windows-build-final/build-report.json`.
- Windows Player Stage45 trên RTX 4060 Laptop GPU / Direct3D11, 1920×1080: Passed 101 checks, runtime errors 0, 180 interactions — `work/stage04-05/player-stage45-20261005T020712223Z/stage45-report.json`; ảnh `farm-starter-sale.png` đã được xem lại.
- Các report, ảnh, build output và QA save do lượt này tạo còn trong `work/stage04-05/`; không đưa vào Git. Lệnh dọn artifact bị PowerShell command policy từ chối; đã kiểm tra không còn Unity/Player process thuộc lượt chạy này.

## Chưa thực hiện được, chưa nghiệm thu và lượt thử chưa pass

### Chưa thực hiện hoặc chưa có bằng chứng nghiệm thu

- Công đoạn 02: chưa đóng hẳn Windows Player rồi khởi chạy lại để nghiệm thu save/load xuyên process. Save atomic/recovery, load lặp và save cũ mới có bằng chứng test trong process.
- Công đoạn 03: chưa chơi trọn tuyến worker đã tuyển/mở khóa và chưa nghiệm thu trọn lifecycle Restaurant trong Windows Player; có test/integration coverage nhưng chưa có lượt chơi end-to-end.
- Công đoạn 04: timeout 90 giây, loss và xử lý đơn hết hạn có test EditMode; Player QA không chờ đủ 90 giây để chứng minh hành vi timeout ngoài runtime test.
- Công đoạn 04/05: Player QA cuối chứng minh tuyến trồng–thu hoạch–mang–bán–thu tiền và góp purchase từng phần, save/load, rời pad rồi tiếp tục trồng; chưa hoàn tất purchase trong một phiên Player acceptance cuối.
- Công đoạn 05: chưa chạy end-to-end chuỗi unlock Farm Shop → Processing → Supermarket → Bakery → Restaurant. Nội dung gameplay của các khu này nằm ngoài phạm vi đã giao.
- Toàn dự án: chưa đo hiệu năng 165 FPS ở 1080p trong thời gian dài hoặc phiên 3–4 giờ.

### Đã thử nhiều lần nhưng vẫn chưa pass/hoàn tất

- Công đoạn 04/05: đã thử lặp lại việc hoàn tất purchase trong Player, nhưng hàng FIFO và các đơn khách hết patience khiến lượt chơi không cho bằng chứng ổn định về hoàn tất purchase; mục tiêu Player acceptance này vẫn chưa pass. Purchase completion/one-shot có EditMode và Stage23 integration coverage, không thay thế được Player end-to-end.
- Công đoạn 02/03 và 04/05: đã thử nhiều lần xóa file tạm bằng PowerShell, nhưng `Remove-Item` bị command policy chặn (`blocked by policy`). Artifact còn trong `work/stage02-03/` và `work/stage04-05/`, không được track và không còn process sử dụng; không thử lách policy.

### Các lỗi lặp đã sửa và lần cuối đã pass

- Công đoạn 04/05: QA drop ban đầu giả định toàn bộ cà rốt phải nằm ở counter, nhưng player đi ngang vùng Serve và giao hàng cho FIFO customer. Harness được sửa để kiểm tra bảo toàn ownership và cash thay vì giả định tồn kho bất biến; Player acceptance cuối pass.
- Công đoạn 04/05: pooled customer inventory chưa bind lại từng gây NullReference khi refresh transaction. Projection hiện chỉ cập nhật inventory đang bind; Editor và Windows Player report cuối đều có runtime errors bằng 0.
- Công đoạn 04/05: expectation cũ yêu cầu tạo loss record cho đơn chưa nhận hàng; test được sửa đúng quy tắc zero delivered = zero loss, order vẫn terminal. EditMode cuối pass 86/86.
- Công đoạn 02/03: các lỗi compile/test, ảnh QA và BuildPipeline được ghi ở mục rà soát trước đó đã được sửa hoặc chạy lại thành công; không còn failed check trong báo cáo cuối được liệt kê.

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
