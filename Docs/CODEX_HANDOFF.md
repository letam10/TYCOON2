# CODEX_HANDOFF

Chat Codex tiếp theo phải đọc file này trước khi làm việc. Tiếp tục trực tiếp D:\GAME\TYCOON2; không tạo project mới.

## Công đoạn hiện tại

- Công đoạn 01 — AUDIT VÀ BASELINE: HOÀN THÀNH. Prototype mâu thuẫn trong phạm vi audit đã được chặn/sửa; baseline Editor và Windows Player đều đạt.
- Công đoạn 02 — STATE AUTHORITY VÀ TRANSACTION CORE: code core đã có ở commit 47e1e9f nhưng chưa được nối vào gameplay runtime; chưa nghiệm thu công đoạn 02.
- Cập nhật bàn giao: 2026-10-04, múi giờ Asia/Bangkok.

## Môi trường và Git

- Unity 6000.6.3f1; URP 17.6.0; Input System 1.19.0; AI Navigation 2.0.12.
- Windows single-player offline, giao diện tiếng Việt.
- Repository D:\GAME\TYCOON2, branch main, remote https://github.com/letam10/TYCOON2.git.
- Mốc audit đã commit và push: 3582a92 — fix: establish stage 01 interaction and ownership baseline.
- Follow-up CLI đã commit và push: 3a4a6d6 — fix: preserve default Windows build report path.
- Mốc runtime trước đó: 47e1e9f — feat: add durable ownership and transaction core.
- Các thay đổi chưa commit có sẵn khi bắt đầu (Art, FollowCamera, GameHud, GameSession, PlayerController, Station, QualitySettings, PlayerInteraction.cs và .meta) được giữ lại, không đưa vào commit 3582a92.

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
- TransactionCore/TransactionStore/TransactionState và test của chúng đã tồn tại trên main từ commit 47e1e9f; GameSession, Inventory, CommerceDirector và worker gameplay hiện chưa gọi core này. Không coi core độc lập đó là công đoạn 02 đã hoàn tất.

## Audit theo hệ thống

| Hệ thống | Trạng thái / điểm bàn giao |
| --- | --- |
| Quyền sở hữu item | Runtime giữ owner riêng cho giỏ player/worker/customer, kho từng khu, trạm và input máy; Farm Shop không còn alias sang kho Farm. TransactionCore độc lập chưa nối vào gameplay. |
| Inventory | Transfer/reservation và giỏ một loại được giữ; restore lỗi phải bảo toàn dữ liệu cũ. |
| Kho | storage_farm và storage_farm_shop được tra theo AreaId riêng; khu không có kho không fallback về kho khác. |
| Customer/order | Giữ OrderState, receipt, dòng hàng và giá chốt; không mua tự động từ shelf. |
| Giao hàng | Checkout chỉ nhận carrier; PlayMode xác nhận cashier mang đúng SKU tới đúng quầy/đơn và chờ khách ở vùng phục vụ. |
| Payment/cash | RecordPayment ghi tiền tại quầy, CollectCash chuyển sang ví một lần; không cộng thẳng bằng API prototype. |
| Machine | Giữ vận hành có actor/lease và reservation output; đã có test không tự tiến độ khi không vận hành. |
| Purchase/unlock | Giữ Contribute theo thời gian giữ; chặn legacy và chặn thuê worker nếu khu chưa có kho riêng, không trừ tiền. |
| Save/load | Editor và Windows Player kiểm tra save/load lặp, cash/hàng, PendingCustomers/PendingWorkers, station state và receipt tăng đơn điệu. StationZone không được tính là station state bền vững. |
| NPC navigation | NavMesh, khách đến hàng chờ, cashier đến vùng phục vụ và worker chờ khu thiếu kho đã được kiểm tra trong Windows Player. |
| Upgrade | Giữ definitions/tier/crew hiện có; chưa thay hệ thống bằng state/transaction mới. |
| Worker | Giữ carry/reservation riêng; cashier không thả hàng chỉ vì khách chưa tới; save/load và tuyến cashier đã kiểm tra runtime. |
| Interaction | Các trạm hiện có có vùng Lấy/Đặt/Vận hành/Giao/Thu; PlayMode kiểm tra Input System, lấy/đặt owner và cashier. |

## Kết quả kiểm chứng thực tế

- EditMode: Passed 65/65, Failed 0, Skipped 0; work/stage01/codex-audit-20261004i/editmode-results.xml.
- Editor PlayMode headless: Passed 180/180; work/stage01/codex-audit-20261004i/play/baseline-results.json.
- Windows Standalone Player headless: Passed 180/180; work/stage01/codex-audit-20261004i/windows-player-qa/baseline-results.json.
- Build Windows từ scene hiện có: Succeeded, 115,292,815 bytes, 0 errors, 2 warnings; work/stage01/codex-audit-20261004i/windows-build-report.json. Bản build tạm đã được dọn sau khi Player QA hoàn tất.
- Các lần xác nhận runtime chạy bằng -batchmode -nographics; graphics-device.json ghi Null Device. Chưa nghiệm thu renderer RTX 4060, hình ảnh hoặc chất lượng chơi bằng cửa sổ.
- Log cũ work/logs/baseline.log từng có lỗi UnityEditor.Search.SearchDatabase; các báo cáo QA mới ghi errors=0. Giữ log cũ làm bằng chứng lịch sử, không xem lỗi Editor cũ là lỗi gameplay đã tái hiện.

## File chính

- Assets/_Game/Scripts/Runtime: Inventory.cs, GameRules.cs, GameSession.cs, CommerceDirector.cs, Station.cs, WorkerAgent.cs, SaveData.cs.
- Assets/_Game/Scripts/Runtime/QaBaseline.cs và .meta: baseline integration harness chạy bằng --qa-baseline.
- Assets/_Game/Scripts/Editor/ProjectBuilder.cs: build từ scene hiện có và entry PlayBaseline.
- Assets/_Game/Tests/Editor: CoreTests.cs, CommerceV2Tests.cs, BaselineTests.cs và .meta.
- Tools/run_unity.ps1: thêm task Baseline; vẫn giữ Import/Tests/Build/Scene.

## Phạm vi đã đóng trong Công đoạn 01

- Giữ nguyên nền tảng stable ID, Input System, pooling, inventory primitives, save v2, CLI và transaction core độc lập.
- Không thêm nội dung hay mở khu Farm Shop/Processing/Supermarket/Bakery/Restaurant mới; chỉ nối thao tác vào các trạm đã có trong scene.
- Các báo cáo kiểm chứng giữ tại work/stage01/codex-audit-20261004i/: EditMode XML, Editor PlayMode, Windows Player, graphics audit và Windows build summary.
- Các lượt thử hỏng trước khi baseline đạt đã được dọn. Binary build tạm và QA save tạm cũng đã được dọn; chỉ giữ báo cáo kiểm chứng cuối.
- Không có Unity/Player process do lượt này khởi chạy còn chạy. QA dùng -nographics, không chạy GPU workload.

## Trạng thái bàn giao

Công đoạn 01 hoàn tất; code commit 3582a92 và CLI follow-up 3a4a6d6 đã push lên main. Handoff này được commit/push riêng. Công đoạn 02 có core code riêng tại 47e1e9f nhưng chưa tích hợp vào runtime gameplay và chưa nghiệm thu. Các thay đổi working tree có sẵn từ trước nhiệm vụ được giữ ngoài commit; kiểm tra git status trước khi tiếp tục.

## Công đoạn 02 — STATE AUTHORITY VÀ TRANSACTION CORE

### Trạng thái và phạm vi

- TransactionCore, TransactionState và FileTransactionStore đã được thêm ở commit 47e1e9f; đây là code core độc lập, chưa được gọi từ GameSession, Inventory, CommerceDirector hoặc hệ thống gameplay khác.
- Công đoạn 02 CHƯA HOÀN THÀNH và CHƯA NGHIỆM THU đầu-cuối. Công đoạn 01 commit 3582a92 chỉ sửa baseline runtime cũ, không tích hợp TransactionCore.
- Mục tiêu: mỗi hiệu ứng nghiệp vụ chỉ xảy ra một lần, tài sản không mất/nhân đôi khi retry hoặc recovery, mọi thay đổi authoritative có một writer.
- Definition là cấu hình có version, runtime state tham chiếu bằng ID ổn định; không coi tham chiếu GameObject, thứ tự danh sách hoặc tên hiển thị là định danh bền vững.
- API command/state và file store đã có; còn phải nối mọi writer gameplay vào core, xác nhận recovery barrier, uniqueness xuyên hệ thống và kiểm thử crash/retry ở ranh giới thực tế.

### Đối chiếu mã nguồn hiện có

| Yêu cầu | Bằng chứng hiện tại | Kết luận |
| --- | --- | --- |
| Definition version và ID ổn định | Definitions/GameCatalog có ID dạng string; SaveData có schema version 2. Definition chưa có version riêng và runtime còn giữ tham chiếu trực tiếp. | Một phần; schema version của save không thay cho definition version. |
| Một writer cho mỗi trường authoritative | Inventory/Economy có API mutator nhưng Player, Station, Worker, Commerce và GameSession gọi trực tiếp; Cash, order/job/purchase state còn được sửa theo từng luồng. | Chưa có mô hình authority và command boundary thống nhất. |
| Ownership và split/merge | InventorySave có ID owner; customer basket, worker carry và machine input được lưu riêng; validation phát hiện một số owner trùng/hỏng. ItemAmount mới là product ID/count, chưa có item/stack identity và location registry. | Một phần; chưa có split/merge gắn reservation trong transaction. |
| Transaction core | TransactionCore xử lý TransactionCommand, revision, actor, invariant và các loại transaction; không có tham chiếu runtime từ hệ thống gameplay. | Core độc lập đã có; gameplay chưa tích hợp. |
| Mutation/receipt/dedup/outbox nguyên tử | FileTransactionStore ghi TransactionState qua temp + replace; SaveStore gameplay vẫn là snapshot riêng và receipt economy riêng. | Chưa có một commit boundary dùng xuyên gameplay. |
| Idempotency và business uniqueness | Core có receipt/key/fingerprint và effect ID; Economy runtime chống lặp receipt ở một số nhánh. | Một phần trong core độc lập; chưa xuyên suốt runtime. |
| Complete/fail và định danh nghiệp vụ | OrderState có trạng thái trả tiền/hết hạn; validation/test chặn receipt đồng thời paid/lost. Các hiệu ứng chưa chung transaction và chưa có đầy đủ ID cho delivery/contribution/purchase grant. | Một phần; chưa nghiệm thu cạnh tranh. |
| Save/load và recovery | Save v2 bảo toàn nhiều inventory, cash, receipt, machine, worker, customer; có SaveBlocked và diagnostics. Reservation chưa là record bền vững có expiry; SaveBlocked chỉ chặn save, gameplay chưa bị chặn. | Một phần; chưa có recovery barrier. |
| Test tuần tự/cạnh tranh/crash-retry | Có TransactionCoreTests/TransactionRecoveryTests và 65 EditMode đạt; PlayMode/Windows Player đạt baseline 180 check. Các bài runtime hiện vẫn đi qua gameplay cũ. | Chưa có nghiệm thu tích hợp/cạnh tranh/crash công đoạn 02. |

### Thiết kế authority cần triển khai

Tên dưới đây mô tả trách nhiệm dự kiến, chưa phải các class đã tồn tại:

| State authoritative | Writer duy nhất dự kiến | Hệ thống khác làm gì |
| --- | --- | --- |
| Definition/config version | Catalog loader/migration đã kiểm tra | Đọc theo definition ID và version; không sửa runtime để bù config lỗi. |
| Item/stack, owner/location, capacity, reservation | Inventory/ownership authority qua transaction core | Player, worker, station, conveyor gửi command transfer/reserve/split/merge. |
| Order state và delivery/escrow | Order authority qua transaction core | Checkout/customer/worker gửi command deliver/complete/fail. |
| Money, pending cash, payment/contribution | Economy authority qua transaction core | UI/checkout/purchase gửi command; không tăng tiền hoặc trừ tiền trực tiếp. |
| Production job, cycle, consume/output | Production authority qua transaction core | Machine/operator gửi command start/advance/complete theo job/cycle ID. |
| Purchase entitlement/grant | Purchase authority qua transaction core | UI gửi command contribute/grant; grant và khoản đóng góp cùng boundary thích hợp. |
| Receipt, command dedup, outbox | Transaction core | Caller đọc receipt; publisher đọc outbox, không thực thi lại mutation. |
| Recovery lifecycle | Recovery coordinator | Gameplay chỉ nhận command sau khi snapshot/journal/reference đã được kiểm tra. |

### Ranh giới transaction và retry

1. Nhận command có actor, command type, idempotency key, payload fingerprint, business effect ID và expected versions của state liên quan.
2. Tra durable dedup/receipt trước khi thực thi. Cùng key, cùng payload trả receipt đã commit; cùng key nhưng payload khác phải báo xung đột.
3. Kiểm tra quyền, precondition, version, capacity, ownership, reservation còn hiệu lực và invariant. Nếu chưa commit, lỗi không được để lại thay đổi một phần.
4. Tính write set cho tất cả aggregate liên quan, gồm mutation, receipt, command dedup và outbox. Không gọi API có side effect ngoài boundary trong quá trình chuẩn bị.
5. Commit bền vững nguyên tử theo cơ chế đã chọn. Kiểm thử phải chứng minh crash ở mỗi boundary không tạo state nửa cũ/nửa mới.
6. Trả receipt đã lưu sau commit. Publish event đi qua outbox; crash sau commit hoặc lúc publish không rollback nghiệp vụ đã commit.
7. Consumer lưu event ID đã xử lý cùng hiệu ứng của consumer. Redelivery có thể xảy ra, hiệu ứng chỉ xảy ra một lần.
8. Business uniqueness tách khỏi command dedup: một job cycle chỉ consume/output một lần; một order chỉ có một terminal settlement; một payment/delivery/contribution/purchase grant chỉ có một hiệu ứng dù command ID khác nhau.

### Ownership, reservation và recovery

- Mỗi item/stack phải có một owner/location hợp lệ và ID bền vững; hàng trên conveyor, carry, machine input/output, customer basket và escrow đều có location rõ ràng.
- Transfer cập nhật owner/location và reservation liên quan trong cùng transaction; không remove rồi add theo hai commit riêng.
- Split/merge bảo toàn tổng số lượng và cập nhật record reservation: không tạo reservation vượt quantity, mất liên kết hoặc gắn reservation vào stack đã bị xóa.
- Reservation có ID, holder, mục tiêu, lượng, expiry và trạng thái. Reserve/use/release đi qua core; release lặp lại không trả capacity/lượng lần thứ hai.
- Load đối chiếu expiry từ dữ liệu bền vững theo chính sách thời gian được ghi rõ; chỉ reservation còn hiệu lực mới được sử dụng.
- Recovery phải phục hồi ownership, reservation, escrow, payment/dedup/outbox và tiến trình job trước khi mở nhận command.
- Nếu version/reference/ownership/invariant hỏng: giữ gameplay ở trạng thái recovery lỗi và báo diagnostics có ID cụ thể. Không tự tạo, xóa, hoàn tiền hoặc reset tài sản để làm save có vẻ hợp lệ.

### Ma trận kiểm thử nghiệm thu bắt buộc

Mỗi trường hợp cần có assertion trên state, quantity/money, version, receipt, business effect record và event ID; không chỉ kiểm tra giá trị trả về:

| Tình huống | Kết quả bắt buộc |
| --- | --- |
| Retry cùng command/key/payload | Trả receipt đã commit, không mutation/publish hiệu ứng lần hai. |
| Cùng key, payload khác | Báo xung đột; state không thay đổi. |
| Command ID khác, cùng business effect ID | Chỉ có một hiệu ứng nghiệp vụ. |
| Hai actor tranh cùng item/capacity/reservation/version | Chỉ kết quả hợp lệ được commit; không âm quantity/capacity và không double spend. |
| Split/merge khi có reservation | Bảo toàn lượng; reservation hợp lệ và cập nhật nguyên tử. |
| Hai lệnh complete/fail cùng order | Chỉ một terminal settlement; payment và loss loại trừ nhau. |
| Production complete bị gọi lặp | Chỉ một lần consume input và một lần tạo output cho job/cycle. |
| Conveyor chuyển item khi save/load | Item có đúng một owner/location sau recovery. |
| Crash trước hoặc giữa chuẩn bị commit | Không có hiệu ứng một phần; recovery về một state hợp lệ. |
| Crash sau commit, trước trả receipt | Retry trả receipt đã lưu, không thực thi lại. |
| Crash khi publish event | Outbox phát lại; consumer deduplicate theo event ID. |
| Reserve hết hạn sau load | Chỉ reservation còn hiệu lực được sử dụng; release không chạy hai lần. |
| Save có reference hỏng | Chặn gameplay, báo diagnostics; không tự tạo/xóa tài sản. |
| Crash/retry payment, delivery, contribution, purchase grant | Business ID bền vững bảo đảm không thanh toán, giao hàng, đóng góp hoặc grant lần hai. |

### Điều kiện hoàn tất công đoạn 02

- Definition là dữ liệu cấu hình có version; runtime state chỉ tham chiếu definition bằng ID ổn định.
- Mỗi trường authoritative có đúng một hệ thống được quyền cập nhật. Hệ thống khác gửi command, không sửa state trực tiếp.
- Mỗi item hoặc stack có owner/location hợp lệ; split/merge bảo toàn số lượng và cập nhật reservation liên quan trong cùng transaction.
- Mọi API nghiệp vụ đi qua transaction core, kiểm tra quyền, precondition, version và invariant trước commit.
- Mutation, receipt, deduplication record và outbox được commit nguyên tử. Lỗi trước commit không để lại hiệu ứng một phần.
- Idempotency key chống retry cùng command; business uniqueness chống lặp cùng hiệu ứng bằng các command ID khác nhau.
- Complete/fail order loại trừ nhau. Payment, delivery, contribution và purchase grant có định danh nghiệp vụ bền vững.
- Save/load bảo toàn ownership, reservation, escrow, payment và tiến trình job; recovery hoàn tất trước khi nhận command mới.
- Toàn bộ kiểm thử nghiệm thu chạy qua cả trường hợp tuần tự, cạnh tranh và crash/retry.

Đây là tiêu chí nghiệm thu. TransactionCore đã có implementation và test độc lập, nhưng chưa được nối vào gameplay và chưa chạy ma trận nghiệm thu đầu-cuối; không đánh dấu công đoạn 02 hoàn tất.

## Phân tích lại dự án sau khi đóng Công đoạn 01

- Baseline gameplay hiện được kiểm tra trên Editor và Windows Standalone Player. Kết quả không xác nhận hình ảnh, renderer RTX 4060 hoặc chất lượng chơi qua cửa sổ vì các lượt chạy dùng -nographics.
- TransactionCore ở commit 47e1e9f có command, actor, revision, receipt/dedup/effect fingerprint, outbox và file store; các hệ thống gameplay chưa gọi core nên authority thực tế vẫn phân tán trong API runtime cũ.
- Rủi ro còn lại cho Công đoạn 02: writer gameplay chưa hợp nhất, commit core chưa làm authority của GameSession, recovery barrier và reservation expiry chưa nối vào load/gameplay; cần kiểm tra crash/retry và cạnh tranh xuyên runtime.
- Scene hiện có nhiều khu prototype đã khóa. Công đoạn 01 chỉ bổ sung vùng thao tác/guard baseline, không mở rộng nội dung các khu đó.
- Working tree vẫn có thay đổi đã tồn tại trước nhiệm vụ ở Art.cs, FollowCamera.cs, GameHud.cs, GameSession.cs, PlayerController.cs, Station.cs, QualitySettings.asset và PlayerInteraction.cs/.meta. Chúng được giữ ngoài commit 3582a92; kiểm tra git status trước khi tiếp tục.
- Công đoạn 01 không còn việc mở. Bước tiếp theo thuộc Công đoạn 02: tích hợp authority/transaction core vào runtime theo tiêu chí bên dưới, không tuyên bố hoàn tất cho đến khi có acceptance tests đầu-cuối.
- Không đưa phần trăm tiến độ tổng dự án: chưa nghiệm thu toàn bộ nội dung PLAN.md.

## Xác nhận lưu và kiểm chứng

- Repository D:\GAME\TYCOON2, branch main; code audit 3582a92 và CLI follow-up 3a4a6d6 đã push origin/main thành công.
- Unity 6000.6.3f1 EditMode: 65/65 Passed, 0 Failed, 0 Skipped.
- Editor PlayMode baseline: 180/180 Passed; Windows Standalone Player baseline: 180/180 Passed.
- Windows BuildPipeline: Succeeded, 115,292,815 bytes, 0 errors, 2 warnings. Build output riêng và QA save tạm đã được dọn sau khi lưu báo cáo; BuildWindows mặc định vẫn ghi report vào QA/build-report.json.
- Báo cáo cuối nằm trong work/stage01/codex-audit-20261004i/. Tài liệu này được cập nhật và push bằng commit docs riêng sau hai commit code ở trên. Unity/Player/helper do lượt kiểm chứng khởi chạy đã thoát; không có process nào cần giữ chạy.
- Các lượt compile/test/build/player dùng -nographics; không chạy GPU workload.
