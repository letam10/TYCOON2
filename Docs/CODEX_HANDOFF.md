# CODEX_HANDOFF

Chat Codex tiếp theo phải đọc file này trước khi làm việc. Tiếp tục trực tiếp D:\GAME\TYCOON2; không tạo project mới.

## Công đoạn hiện tại

- Công đoạn 01 — AUDIT VÀ BASELINE: ĐANG DỞ, chưa hoàn tất.
- Cập nhật bàn giao: 2026-10-04, múi giờ Asia/Bangkok.
- Chưa thể kết luận baseline sạch hoặc PlayMode đạt. Không chuyển sang nội dung Farm Shop/Processing/Supermarket/Bakery/Restaurant.

## Môi trường và Git

- Unity 6000.6.3f1; URP 17.6.0; Input System 1.19.0; AI Navigation 2.0.12.
- Windows single-player offline, giao diện tiếng Việt.
- Repository D:\GAME\TYCOON2, branch main, remote https://github.com/letam10/TYCOON2.git.
- Mốc đã commit và push: 599e7ee — fix: disable conflicting prototype commerce paths.
- Những thay đổi runtime/QA tiếp theo vẫn đang ở working tree, chưa được commit; checkpoint tài liệu này được lưu riêng.

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
- Lần cập nhật tài liệu này chỉ đọc/ghi bàn giao, không khởi chạy Unity/GPU, không tạo backup và không sửa file code hoặc tài liệu khác.

## Trạng thái bàn giao

Công đoạn 01 chưa hoàn tất. Mốc 599e7ee đã push; code save/runtime/QA tiếp theo đang dở tại working tree. Chat tiếp theo đọc file này, tiếp tục đúng Công đoạn 01 và xác minh lại trước khi ghi hoàn tất.
