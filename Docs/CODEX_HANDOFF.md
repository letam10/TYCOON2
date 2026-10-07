# CODEX_HANDOFF

Cập nhật 07/10/2026, Asia/Bangkok. Tiếp tục trực tiếp tại D:\GAME\TYCOON2.
Đây là trạng thái hiện hành; các bản bàn giao cũ đã chuyển nguyên nội dung sang Docs/History.

## Phạm vi hiện tại

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
- Khởi đầu ví 0, ba luống cà rốt, cà rốt 10 xu; tuyến miễn phí giúp phục hồi khi hết tiền.
- Mang một SKU; sức mang 6 → 10 → 16 → 24. Kho từng khu có owner riêng.
- Đơn FIFO, kiên nhẫn 90 giây; quá hạn giữ phần đã nhận và trả 0; ghi thất thoát đúng một lần.
- Giao đủ mới tạo tiền chờ thu. Chỉ player thu tiền vào ví; cashier không tự cộng ví.
- Mod game chỉ cộng 999.999 vào ví; không mở khu, cấp hàng hoặc tăng doanh thu/cashCollected.
- Các mốc mở khu và điều kiện vận hành giữ trong Definitions/ProgressionTracker.
- Save v2 + journal giữ receipt, dedup, reservation và state; không tiến triển offline.

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
