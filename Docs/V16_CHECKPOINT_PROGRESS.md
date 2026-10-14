# V16: chụp lịch sử checkpoint và phạm vi kiểm chứng

## Trạng thái

Full Unity EditMode đã đạt 910/910, 0 lỗi và 0 bỏ qua, exit 0, trong 19,5747589 giây.
Kết quả: work/city-model-v16-20261010/full-tests-final.xml và full-tests-final.log.
Đã đo V16 trong Player Windows cùng save ở thứ tự thuận và ngược; chưa đạt 165 FPS ổn định.
Không dùng các test để suy ra FPS hoặc nhiệt độ; đã đo phần cứng một đoạn giữa tour V16 riêng biệt.
Theo phạm vi mới của người dùng, benchmark stable165 đã dừng và không còn là cổng nghiệm thu.
Các số FPS cũ chỉ là dữ liệu chẩn đoán; các kiểm tra chức năng và save vẫn tiếp tục.

## V17 trace ảnh đã hoàn tác

V17 thử ghi capture-trace.csv theo frame và kiểm tra file sau một frame để tìm nguyên nhân hitch.
Player V17 đạt chức năng nhưng FPS 93,837, P95 21,724 ms, GPU frame 11,764 ms.
V16 cùng kiểu tour trước đó đạt 160,647 FPS; đây là hồi quy rõ ràng của instrumentation.
Trace ghi file chưa tồn tại sau một frame dù 44 PNG cuối lượt hợp lệ, nên phép đo chưa đủ tin cậy.
Đã xóa CityPlayCaptureTrace.cs/.meta và khôi phục đường chụp ảnh cũ.
Không giữ trace trong candidate; tài liệu V17 chỉ giữ phân tích và dữ liệu để tham khảo.

## Thay đổi

- TransactionCore.CaptureDeferredCheckpoint tách world và history dưới cùng một khóa.
- CheckpointHistoryCapture giữ trường giá trị và chuỗi bất biến, không giữ receipt/event/list nguồn sống.
- Worker Materialize tạo DTO/list riêng trước BeforeWrite và trước SaveStore.WriteCheckpoint.
- RuntimeTransactions.ProjectOwned vẫn chạy tại main thread; không đưa catalog hoặc Unity world sang worker.
- Fault injection giữ đường đồng bộ; lỗi background vẫn được Observe báo lại và giữ task lỗi.
- CaptureCheckpoint, CheckpointOwned và CheckpointWriter.Start cũ vẫn còn, không thêm overload cùng tên.
- Save schema, transaction IDs, journal replay và migration không đổi.

## Kiểm chứng đã chạy

- Module CheckpointHistoryCaptureTests: 12/12, gồm null/empty/order/long và snapshot độc lập.
- DeferredCaptureKeepsHistoryAndWorldAtSameRevisionAfterLaterAcknowledgment: đạt.
- DeferredWriterMaterializesBeforeCallbackAndRetainsBackgroundFailure: đạt.
- Full suite bao gồm recovery, later journal, retry dedup, tiền/két và fault injection: 910/910 đạt.

## V15 và đường cơ sở

V15 thử sao chép DTO đơn giản, được so với V14 ở thứ tự thuận và ngược trên cùng checkpoint.
Trung bình hai lượt: V14 159,424 FPS; V15 159,222 FPS, không có tăng FPS lặp lại được.
Đã phục hồi đúng CheckpointSnapshot.cs V14 trước khi thêm CaptureDeferredCheckpoint V16.
Bằng chứng V15 giữ tại work/city-model-v15-20261010/PairFarm và PairFarmReverse.

## Player V16 và so sánh cùng đầu vào

Build IL2CPP thành công 11:33:32,752 UTC, 71,033 giây, 977639013 byte, 0 lỗi/28 cảnh báo.
Player V16: work/city-model-v16-20261010/Candidate/TycoonTown.exe.
Đường cơ sở là Player giải nén từ Windows.zip V14, không phải checkout đã đổi.
Save cùng SHA 0056f2850dd894e9e9f27f731d3f81d62fc8d3ba7db7fd2ed7da5c7efee4765f.
Mỗi lượt Normal/1920x1080/Balanced/1x/cap165, Direct3D11, RTX4060 Laptop, 39 nhân viên.
Settings giống nhau trong từng cặp. Bốn Player exit 0, functional/NPC PASS, mỗi lượt 20 ảnh không blank.
Đã xem trực tiếp ảnh scenery-farm.png của ordinary-play-20261010T113527670Z.

| Thứ tự | Bản | FPS trung bình | P95 ms | P99 ms | Worst ms | Snapshot gameplay/final ms |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 1 | V14 | 161,525 | 6,631 | 9,024 | 208,946 | 9,112 / 10,206 |
| 2 | V16 | 161,694 | 6,549 | 8,708 | 206,047 | 8,527 / 6,872 |
| 3 | V16 | 161,370 | 6,711 | 8,854 | 197,310 | 9,301 / 11,609 |
| 4 | V14 | 157,989 | 7,656 | 9,848 | 236,098 | 10,154 / 17,463 |

Trung bình hai lượt FPS: V14 159,757; V16 161,532. Chênh lệch chỉ là quan sát trong các lượt này.
Snapshot bốn mẫu mỗi bản: V14 11,734 ms, V16 9,077 ms; không đủ kết luận toàn bộ nguyên nhân hitch.
Mọi lượt stable165FrameBudget=false. Đây là ca đứng tại chỗ, không phải tour chơi V16 toàn thành phố.
HUD trên ảnh thuộc pha capture/lưu sau đo; không dùng số tức thời đó thay trung bình cửa sổ 60 giây.
Báo cáo đầy đủ: work/city-model-v16-20261010/PairFarm và PairFarmReverse/comparison.json.

## Còn phải kiểm tra

- Tour V16 qua toàn thành phố, thay góc camera, tương tác và phục hồi save sau chơi.
- Đo khung hình chậm và trình bày 165 Hz thực tế; engine FPS chưa chứng minh đồng bộ màn hình.
- Các mẫu Snapshot giảm trong ABBA nhưng vẫn cần kiểm tra khi lịch sử giao dịch lớn hơn.
- Tour toàn thành phố và thời gian khung hình chậm; chưa được gọi là nghiệm thu 165 ổn định.
- Nhiệt độ và xung CPU hiệu dụng hiện chưa có cảm biến trực tiếp; không tự điền giá trị.

## Báo cáo phần cứng hiện có

V16: samples-20261010T114746317Z, PID26948/TycoonTown.exe, 132 mẫu/181,183 giây.
CPU game17,19% (11,24-23,02), CPUtotal37,61% (18-73), GPUtotal34,52% (23-49).
GPU2,123 GHz (1,890-2,535), 62,05 C (58-67), 34,55 W (17,69-57,87).
UTC11:48:05,491 đến11:51:03,940 thuộc đoạn giữa tour T114554647Z, không phải hai lượt ngắn ABBA.
V16 tour còn đang chạy khi cập nhật; chưa có FPS trung bình cả tour hoặc thermal soak dài hạn.
Tools/measure_game_hardware.ps1 sửa tìm tên process từ ExpectedExe và vẫn so đường dẫn chính xác.
Script đã chạy thật exit0 và đúng PID26948; CPUtemp/effectiveclock vẫn null, presentation165Hz=false.
Docs/V14_HARDWARE_AND_FPS.md thêm phần V16 riêng, giữ nguyên số V14 cùng nguồn dữ liệu cũ.
V14 lượt gần165: 162,836 FPS; CPUgame15,62%, GPUtotal34,50%, 1,967 GHz, 53,54 C.
V14 phần cứng36,321 giây trong lượt FPS60,189 giây; không gán sang V16 hoặc nhiệt dài hạn.
