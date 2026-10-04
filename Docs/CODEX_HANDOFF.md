# CODEX_HANDOFF

Chat Codex tiếp theo phải đọc file này trước khi làm việc. Tiếp tục trực tiếp D:\GAME\TYCOON2; không tạo project mới.

## Công đoạn hiện tại

- Công đoạn 01 — AUDIT VÀ BASELINE: ĐANG DỞ, chưa hoàn tất.
- Công đoạn 02 — STATE AUTHORITY VÀ TRANSACTION CORE: ĐÃ GHI THIẾT KẾ/TIÊU CHÍ; CHƯA TRIỂN KHAI, CHƯA NGHIỆM THU.
- Cập nhật bàn giao: 2026-10-04, múi giờ Asia/Bangkok.

## Môi trường và Git

- Unity 6000.6.3f1; URP 17.6.0; Input System 1.19.0; AI Navigation 2.0.12.
- Windows single-player offline, giao diện tiếng Việt.
- Repository D:\GAME\TYCOON2, branch main, remote https://github.com/letam10/TYCOON2.git.
- Mốc code mới đã commit và push: bd6f734 — chore: save stage 01 runtime and QA checkpoint; 9 file runtime/editor/test/tool. Mốc trước: 7ce0478.
- Các thay đổi runtime/QA đã được lưu và push ở checkpoint trên; cập nhật bàn giao công đoạn 02 được commit/push riêng trong phiên này.

## Phần đã thực hiện

- Sửa lệch API WorkerAgent.IsAnimal thành ProductionStation.Animal để giải quyết lỗi compile ban đầu.
- Bỏ Economy.TryBuy và Economy.TryCheckout prototype; giữ luồng góp tiền Contribute và tiền chờ thu tại quầy.
- Checkout và vùng serve chỉ nhận hàng từ giỏ người giao, không tự lấy hàng từ shelf khi giỏ trống.
- Cashier dùng giỏ riêng và tuyến lấy hàng/giao hàng qua reservation hiện có; chưa nghiệm thu tuyến này bằng runtime.
- Chặn nâng cấp kind legacy và receipt không hợp lệ; receipt thanh toán và thất thoát loại trừ nhau, kể cả đơn hết hạn chưa nhận hàng.
- Bỏ fallback kho toàn cục cho khu không tồn tại. Tuy nhiên alias farm_shop về farm còn sai và phải sửa trước khi nghiệm thu.
- Inventory.Restore kiểm tra danh sách hàng trước khi thay thế, giữ hàng/reservation cũ nếu dữ liệu lỗi.
- Bổ sung validation save v2 cho owner, receipt và cash; save lỗi chặn autosave thay vì ghi đè file cũ.
- Load reset khách/worker cũ; giữ PendingCustomers/PendingWorkers/PendingDiners khi lưu lại trước khi spawn; sửa NextReceipt tăng theo các receipt đã lưu.
- Trạng thái trạm dùng stationStates, không áp lại progress prototype từ production; đầu vào máy có owner riêng dạng stationId_input.
- BuildWindows dùng scene hiện có, không gọi lại CreateScene hay cấu hình art.
- Giữ stable ID, Input System, pooling, inventory primitives và CLI hiện có. Chưa triển khai state/transaction system mới.

## Audit theo hệ thống

| Hệ thống | Trạng thái / điểm bàn giao |
| --- | --- |
| Quyền sở hữu item | Owner là giỏ player/worker/customer, inventory trạm và input máy; chưa có transaction journal mới. |
| Inventory | Transfer/reservation và giỏ một loại được giữ; restore lỗi phải bảo toàn dữ liệu cũ. |
| Kho | Scene có storage_farm và storage_farm_shop riêng; alias hiện tại đang làm sai phân biệt vị trí. |
| Customer/order | Giữ OrderState, receipt, dòng hàng và giá chốt; không mua tự động từ shelf. |
| Giao hàng | Checkout dùng carrier inventory; tuyến cashier mới cần kiểm chứng thực tế. |
| Payment/cash | RecordPayment ghi tiền tại quầy, CollectCash chuyển sang ví một lần; không cộng thẳng bằng API prototype. |
| Machine | Giữ vận hành có actor/lease và reservation output; đã có test không tự tiến độ khi không vận hành. |
| Purchase/unlock | Giữ Contribute, chặn API mua tức thì và legacy; cần kiểm tra worker ở khu chưa có kho. |
| Save/load | Validation, reset actor, pending owner và receipt monotonic đã có test; runtime load lặp chưa chạy tới được. |
| NPC navigation | NavMesh tạo được trong headless; tuyến khách/worker chưa nghiệm thu vì test dừng ở input. |
| Upgrade | Giữ definitions/tier/crew hiện có; chưa thay hệ thống bằng state/transaction mới. |
| Worker | Sửa API Animal, vận chuyển có giỏ riêng; cần kiểm tra bảo toàn carry/reservation qua load thực tế. |
| Interaction | Serve không fallback shelf; giữ vùng làm việc/thu tiền và kiểm tra delta hữu hạn. |

## Kết quả kiểm chứng thực tế

- EditMode: Passed, 36/36 test, báo cáo QA/editmode-results.xml; lần chạy ghi trong XML kết thúc 2026-10-04 04:41:23 UTC.
- Editor PlayMode headless: Failed. Báo cáo work/stage01/editor-play/baseline-results.json ghi 89 check thành công trước điểm dừng Input System moves player in PlayMode.
- NavMesh và stable ID trong scene đã được chạy qua. Số check trên không có nghĩa toàn bộ gameplay đã được nghiệm thu.
- Log work/logs/baseline.log có ArgumentOutOfRangeException từ UnityEditor.Search.SearchDatabase; cần giữ chẩn đoán này và phân biệt với lỗi runtime game, không âm thầm xóa/bỏ qua.
- Chưa build và kiểm tra bản Windows mới cho toàn bộ thay đổi đang dở. Chưa nghiệm thu renderer, hình ảnh hoặc chơi bằng cửa sổ game.

## File chính

- Assets/_Game/Scripts/Runtime: Inventory.cs, GameRules.cs, GameSession.cs, CommerceDirector.cs, Station.cs, WorkerAgent.cs, SaveData.cs.
- Assets/_Game/Scripts/Runtime/QaBaseline.cs và .meta: bài kiểm tra đang dở, chỉ chạy bằng --qa-baseline.
- Assets/_Game/Scripts/Editor/ProjectBuilder.cs: build từ scene hiện có và entry PlayBaseline.
- Assets/_Game/Tests/Editor: CoreTests.cs, CommerceV2Tests.cs, BaselineTests.cs và .meta.
- Tools/run_unity.ps1: thêm task Baseline; vẫn giữ Import/Tests/Build/Scene.

## Việc phải làm tiếp trong Công đoạn 01

1. Sửa StorageFor: bỏ alias farm_shop về farm, dùng đúng storage_farm_shop; sửa test để xác nhận hai kho là hai owner riêng.
2. Tái hiện lỗi input trong QaBaseline, kiểm tra PlayerController.CanControl và việc mô phỏng Keyboard; chưa xác định nguyên nhân cuối cùng.
3. Kiểm tra gate NavigationReady/Milestone == 0 trong CommerceDirector.Update: hiện có thể chặn khôi phục pending customer và khách đầu game.
4. Kiểm tra purchase/spawn worker cho khu chưa có kho riêng; không cho fallback sang kho Farm và không thu tiền cho luồng không thể hoạt động.
5. Chạy lại EditMode và Editor PlayMode headless đến khi đạt; kiểm tra cashier vận chuyển, cash một lần, save/load lặp, pending actor và machine progress.
6. BuildWindows từ scene hiện có, kiểm tra runtime Windows và toàn vẹn các foundation đã giữ; chỉ commit/push mốc code khi đã kiểm chứng.
7. Dọn đúng file tạm và tiến trình do tác vụ tạo; cập nhật file này bằng trạng thái nghiệm thu thực tế.

## Dữ liệu kiểm chứng giữ cho lần tiếp tục

- QA/editmode-results.xml: bằng chứng test hiện có.
- work/logs/tests.log và work/logs/baseline.log: log compile/test và lỗi runtime cần đọc.
- work/stage01/editor-play/baseline-results.json và dữ liệu QA cùng thư mục: cần để tái hiện lần chạy thất bại; kiểm tra và dọn khi đã hết cần.
- Các lần Unity QA dùng -batchmode -nographics; không dùng kết quả này để kết luận renderer RTX 4060 hoặc chất lượng hình ảnh.
- Phiên lưu/bàn giao 2026-10-04 bảo toàn code đã có, chạy lại EditMode CPU bằng -nographics và cập nhật riêng file này; không khởi chạy công việc GPU, không tạo backup.

## Trạng thái bàn giao

Công đoạn 01 chưa hoàn tất. Code runtime/QA đã commit và push tại bd6f734; công đoạn 02 đã ghi thiết kế/tiêu chí nhưng chưa triển khai và chưa nghiệm thu.

## Công đoạn 02 — STATE AUTHORITY VÀ TRANSACTION CORE

### Trạng thái và phạm vi

- ĐÃ GHI THIẾT KẾ VÀ TIÊU CHÍ NGHIỆM THU; CHƯA TRIỂN KHAI, CHƯA NGHIỆM THU.
- Các thay đổi code ở checkpoint bd6f734 thuộc công đoạn 01. Không có bằng chứng để gọi chúng là transaction core của công đoạn 02.
- Mục tiêu: mỗi hiệu ứng nghiệp vụ chỉ xảy ra một lần, tài sản không mất/nhân đôi khi retry hoặc recovery, mọi thay đổi authoritative có một writer.
- Definition là cấu hình có version, runtime state tham chiếu bằng ID ổn định; không coi tham chiếu GameObject, thứ tự danh sách hoặc tên hiển thị là định danh bền vững.
- Chưa chốt cấu trúc API và cơ chế lưu transaction bền vững. Khi triển khai phải chọn, ghi rõ và kiểm chứng ranh giới commit; thao tác ghi JSON snapshot hiện có không tự chứng minh nguyên tử cho nghiệp vụ.

### Đối chiếu mã nguồn hiện có

| Yêu cầu | Bằng chứng hiện tại | Kết luận |
| --- | --- | --- |
| Definition version và ID ổn định | Definitions/GameCatalog có ID dạng string; SaveData có schema version 2. Definition chưa có version riêng và runtime còn giữ tham chiếu trực tiếp. | Một phần; schema version của save không thay cho definition version. |
| Một writer cho mỗi trường authoritative | Inventory/Economy có API mutator nhưng Player, Station, Worker, Commerce và GameSession gọi trực tiếp; Cash, order/job/purchase state còn được sửa theo từng luồng. | Chưa có mô hình authority và command boundary thống nhất. |
| Ownership và split/merge | InventorySave có ID owner; customer basket, worker carry và machine input được lưu riêng; validation phát hiện một số owner trùng/hỏng. ItemAmount mới là product ID/count, chưa có item/stack identity và location registry. | Một phần; chưa có split/merge gắn reservation trong transaction. |
| Transaction core | Inventory transfer, payment, contribution, production và purchase còn là thao tác riêng; chưa có expected version, quyền actor và invariant trước commit ở một core. | Chưa triển khai. |
| Mutation/receipt/dedup/outbox nguyên tử | SaveStore validate và ghi snapshot qua file .tmp; receipt thanh toán được lưu. Chưa có durable command receipt, command dedup record hoặc outbox. | Chưa triển khai theo tiêu chí này. |
| Idempotency và business uniqueness | Economy chống lặp receipt ở một số nhánh; upgrade kiểm tra unlocked. Chưa có idempotency key/payload fingerprint và business effect ID xuyên suốt. | Một phần; không đủ cho command ID khác nhau hoặc crash sau commit. |
| Complete/fail và định danh nghiệp vụ | OrderState có trạng thái trả tiền/hết hạn; validation/test chặn receipt đồng thời paid/lost. Các hiệu ứng chưa chung transaction và chưa có đầy đủ ID cho delivery/contribution/purchase grant. | Một phần; chưa nghiệm thu cạnh tranh. |
| Save/load và recovery | Save v2 bảo toàn nhiều inventory, cash, receipt, machine, worker, customer; có SaveBlocked và diagnostics. Reservation chưa là record bền vững có expiry; SaveBlocked chỉ chặn save, gameplay chưa bị chặn. | Một phần; chưa có recovery barrier. |
| Test tuần tự/cạnh tranh/crash-retry | 36 EditMode hiện có kiểm tra baseline, inventory, checkout, save v2; PlayMode baseline vẫn fail. Chưa có fault injection và transaction acceptance suite. | Chưa có nghiệm thu công đoạn 02. |

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

Đây là tiêu chí thiết kế và nghiệm thu. Phiên lưu/bàn giao này đã kiểm tra code baseline hiện có nhưng không triển khai hoặc chạy transaction acceptance suite; chưa được xác nhận công đoạn 02 hoàn tất.

## Phân tích lại dự án sau checkpoint 2026-10-04

- Dự án hiện là prototype Unity Windows offline có nhiều luồng gameplay, catalog, inventory/economy, worker/customer, production và save v2. Có nền tảng để tiếp tục; chưa đủ bằng chứng cho bản chơi hoàn chỉnh hoặc hệ thống giao dịch chịu crash.
- Công đoạn 01 vẫn ĐANG DỞ: EditMode đạt, nhưng PlayMode baseline có 89 check thành công trước khi fail di chuyển bằng Input System. Chưa nghiệm thu đầy đủ production, worker/conveyor, contribution/unlock và save/load trong runtime.
- Công đoạn 02 ở mức thiết kế và gap audit. Các guard baseline hiện có là tiền đề; chúng chưa thay cho transaction core, ownership registry và recovery barrier.
- Rủi ro chính khi mở rộng: effect qua nhiều writer, nghiệp vụ commit riêng lẻ, chỉ receipt cục bộ chống trùng, reservation không có durable expiry, load lỗi chưa chặn gameplay, thiếu kiểm thử crash/cạnh tranh.
- Ownership kho shop chưa đạt thiết kế baseline: GameRules.StorageFor hiện alias farm_shop sang farm dù scene có storage_farm_shop riêng. Test hiện xác nhận alias này; cần sửa code/test để hai kho là hai owner riêng rồi kiểm tra luồng restock/worker bằng runtime.
- Ghi nhận khi audit: attribute MenuItem("TYCOON/Build Windows") hiện gắn vào PlayBaseline trong ProjectBuilder; cần tách lại đúng menu trước khi dùng menu để build. Chưa sửa trong phiên chỉ lưu code/bàn giao này.
- Thứ tự tiếp tục: (1) đóng baseline Input System và kiểm tra các vòng gameplay còn thiếu; (2) chốt authority, ID/version và commit protocol cho công đoạn 02; (3) triển khai từng mốc transaction/ownership/reservation/order/economy/production/outbox với test thích hợp và commit/push riêng; (4) chạy đủ ma trận crash/retry/cạnh tranh rồi mới đánh dấu công đoạn 02 hoàn tất.
- Không đưa phần trăm tiến độ tổng dự án: chưa có nghiệm thu toàn bộ nội dung của PLAN.md và bản Windows sau checkpoint này.

## Xác nhận lưu và kiểm chứng trong phiên này

- Thư mục làm việc và lưu code: D:\GAME\TYCOON2, branch main. Worktree db04 không có code mới hơn; không chép đè từ worktree vào các file đã sửa ở D:.
- Đã lưu 9 file runtime/editor/test/tool (bao gồm QaBaseline.cs và .meta), kiểm tra SHA-256 không thay đổi trong lượt test, rồi commit/push checkpoint code bd6f734.
- EditMode đã chạy lại bằng Unity 6000.6.3f1 với -batchmode -nographics: 36/36 Passed, Failed 0, Skipped 0, Unity exit code 0; hoàn tất 2026-10-04 14:35:46 Asia/Bangkok. Renderer là Null Device, không chạy công việc GPU.
- Kết quả này xác nhận baseline EditMode của checkpoint code; không xác nhận PlayMode/build Windows/công đoạn 02.
- File kết quả/log của lượt kiểm tra mới nằm trong thư mục tạm riêng work/persist-stage01-20261004 và được dọn sau khi trích kết quả. Báo cáo QA/work cũ của dự án được giữ nguyên.
- Unity PID 30888 và helper của lượt kiểm tra đã thoát sau test; không có server/player/GPU job của phiên này cần giữ chạy.
