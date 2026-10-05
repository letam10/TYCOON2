# CODEX_HANDOFF

Chat Codex tiếp theo phải đọc file này trước khi làm việc. Tiếp tục trực tiếp D:\GAME\TYCOON2; không tạo project mới.

## Công đoạn hiện tại

- Công đoạn 01 — AUDIT VÀ BASELINE: HOÀN THÀNH. Prototype mâu thuẫn trong phạm vi audit đã được chặn/sửa; baseline Editor và Windows Player đều đạt.
- Công đoạn 02 — DATA, OWNERSHIP VÀ TRANSACTION CORE: HOÀN THÀNH. Core là writer authoritative cho item/location, order, payment, purchase, crew, producer, table và machine; save v2 ghi state/receipt/dedup/outbox cùng một file.
- Công đoạn 03 — PLAYER INTERACTION: HOÀN THÀNH. Điều khiển WASD/gamepad theo camera, camera 55°, vùng thao tác và stack một loại 6/10/16/24 được kiểm tra trong Editor và Windows Player.
- Cập nhật bàn giao: 2026-10-04, múi giờ Asia/Bangkok.

## Môi trường và Git

- Unity 6000.6.3f1; URP 17.6.0; Input System 1.19.0; AI Navigation 2.0.12.
- Windows single-player offline, giao diện tiếng Việt.
- Repository D:\GAME\TYCOON2, branch main, remote https://github.com/letam10/TYCOON2.git.
- Mốc audit đã commit và push: 3582a92 — fix: establish stage 01 interaction and ownership baseline.
- Follow-up CLI đã commit và push: 3a4a6d6 — fix: preserve default Windows build report path.
- Mốc runtime trước đó: 47e1e9f — feat: add durable ownership and transaction core.
- Mốc Công đoạn 02 đã push: fdbe67f — feat: extend authoritative transaction state.
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
| Machine | Input tiêu thụ sang owner escrow khi operator khởi động; output được giữ chỗ; chạy, tiến độ, hoàn tất, hỏng và sửa đi qua core. |
| Purchase/unlock | Góp dở, hoàn tất, grant và sức mang worker/player nằm trong core; điều kiện/capacity được xác minh trong runtime test. |
| Save/load | SaveData v2 có marker core; core projection, ownership, cash, order, route/reservation worker và machine lưu nguyên tử trong một file. |
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

Công đoạn 01 hoàn tất; code commit 3582a92 và CLI follow-up 3a4a6d6 đã push lên main. Handoff này được commit/push riêng. Công đoạn 02 có core code riêng tại 47e1e9f nhưng chưa tích hợp vào runtime gameplay và chưa nghiệm thu. Các thay đổi working tree có sẵn từ trước nhiệm vụ được giữ ngoài commit; kiểm tra git status trước khi tiếp tục.

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
- Handoff này được cập nhật sau kiểm tra Player/render và sẽ được commit/push riêng.
- Công đoạn 01/02/03 không còn bước bàn giao mở. Nội dung gameplay phía sau kế hoạch chưa được triển khai hoặc nghiệm thu.
- `ProjectSettings/QualitySettings.asset` là thay đổi có sẵn trước lượt này và vẫn được giữ ngoài commit. `ProjectSettings/ProjectSettings.asset` bị Unity BuildPipeline tự thêm Input Actions vào `preloadedAssets`; thay đổi build-generated này được hoàn nguyên, không đưa vào commit.
- Báo cáo EditMode/build/QA và ảnh được giữ trong `work/stage02-03/`. File save QA, binary Windows và log tạm cũng còn ở đó vì các lệnh PowerShell xóa bị Windows command policy từ chối (`Remove-Item: blocked by policy`) dù đã khoanh đường dẫn task; chúng không nằm trong Git. Không còn Unity/Player/process con nào chạy.

## Rà soát cuối 3 công đoạn — phần chưa làm và chưa nghiệm thu

### Hạng mục chưa thực hiện hoặc chưa có bằng chứng pass

- Công đoạn 01: các tiêu chí baseline có báo cáo pass; không tìm thấy lỗi acceptance còn mở trong phạm vi công đoạn.
- Công đoạn 02: chưa chạy bài xác minh save/load bằng cách đóng hẳn rồi khởi chạy lại Windows Player. Đã có test load lặp, save atomic/recovery và tương thích save cũ trong cùng tiến trình; đây chưa phải bằng chứng relaunch thực tế.
- Công đoạn 03: chưa chơi hết luồng worker đã tuyển/mở khóa trong Windows Player và chưa chạy end-to-end toàn bộ vòng đời khách nhà hàng trên Player. Worker relay, bàn và các vùng thao tác có test/integration coverage, nhưng chưa thay thế được hai lượt chơi trọn luồng này.
- Công đoạn 03: ảnh xác minh chỉ bao phủ một trạng thái; nhãn một số vùng station có vẻ chồng nhau ở góc camera đó, cần kiểm tra/chỉnh thị giác riêng.
- Xuyên suốt: chưa đo nghiệm thu mục tiêu 165 FPS ở 1080p trong thời gian dài hoặc phiên tiến trình 3–4 giờ.
- Ngoài phạm vi Công đoạn 01–03: chưa xây nội dung gameplay riêng cho Farm Shop, Processing, Supermarket, Bakery và Restaurant; các trạm/zone hiện có chỉ được giữ hoặc nối vào core và framework chung.
- Dọn artifact QA/build tạm của Công đoạn 02/03 dưới `work/stage02-03/` đã được thử nhiều lần nhưng chưa xong: lệnh `Remove-Item` bị command policy từ chối (`blocked by policy`). Các file còn lại không đưa vào Git; không còn tiến trình Unity/Player của lượt làm việc giữ chúng mở.

### Lượt thử lỗi lặp đã được khắc phục; kết quả cuối pass

- Công đoạn 02: lỗi trùng tên biến cục bộ `WorkerAgent.IsAnimal`, thiếu tham chiếu assembly Input System trong test interaction và tương thích save cũ khi Unity tạo object giao dịch rỗng đã được sửa. Bộ EditMode cuối đạt 81/81.
- Công đoạn 03: hai ảnh QA Player đầu không dùng được (Player ẩn cho ảnh đen; lần chụp kế tiếp trước khi HUD cập nhật nên hiện 0/6). Harness đã chờ thêm một frame; lượt Player cuối hiển thị 6/6 và đạt 167/167 checks.
- Công đoạn 02/03: lần Windows BuildPipeline đầu không tự thoát; tiến trình task-owned đã được kết thúc và lần build cuối có `-quit` thành công. Build cuối báo 0 lỗi và 2 cảnh báo.
- Không có lỗi compile, test hoặc Player QA nào còn được ghi là thất bại trong báo cáo cuối: Công đoạn 01 EditMode 65/65, Editor/Windows Player 180 checks mỗi lượt; Công đoạn 02/03 EditMode 81/81, Editor/Windows Player 167 checks mỗi lượt, errors rỗng. Kết quả Editor là `-nographics`; kiểm tra đồ họa nhìn thấy được chỉ có lượt Windows Player trên RTX 4060 đã nêu ở trên.

### Git của lượt rà soát này

- Trước khi sửa handoff: `main` trùng `origin/main` tại `2218095`; chỉ file này được đưa vào commit audit cuối và commit được push lên `origin/main` sau khi cập nhật mục này.
- `ProjectSettings/QualitySettings.asset` vẫn là thay đổi có sẵn của người dùng (`antiAliasing: 0` → `2`); được giữ nguyên và không đưa vào commit/push.
