# V12 nhãn thế giới và kiểm chứng đóng gói

## Phạm vi

- Tiếp tục source TYCOON2 và các model thành phố V11 đã được kiểm hình.
- Không đổi model, shader nước, ánh sáng, collision, giao dịch, sức mang hoặc mức mô phỏng.
- Nhãn trạm ẩn không còn tạo Prompt hoặc ghi TextMesh mỗi frame.
- Khi nhãn hiện lại, nội dung cập nhật trước SetActive để tránh nháy chữ cũ.
- Billboard ngoài khoảng đọc 80 m được ẩn; vào lại tầm đọc cập nhật trong cùng frame.
- Nhãn trạm ngoài vùng hiện không tiếp tục định dạng dòng chi tiết trong BillboardLabel.
- Việc ẩn nhãn không ẩn đồ vật, NPC hoặc tắt vùng tương tác.

## Tái hiện và fixture

- `work/city-model-v12-20261010/label-visibility-red.xml`: 10 lỗi ban đầu.
- Sáu ca trạm phát hiện Prompt vẫn được đọc khi ẩn và trên đường quay lại.
- Bốn ca billboard ban đầu thiếu khởi tạo Awake trong fixture EditMode.
- Đã sửa fixture giống kiểm tra readability hiện có, rồi chạy với giới hạn xa tạm tắt.
- `billboard-initialized-red.xml`: bốn ca lỗi đúng vì renderer xa vẫn bật.
- Bản sửa 80 m đã khôi phục; full-suite 876/876 PASS, 0 skip.
- XML hiện hành: `work/city-model-v12-20261010/full-tests.xml`.
- Player V12 Mono dựng thành công lúc 04:29:45 UTC, 0 lỗi/28 warning, 118.688.703 byte.
- Ca `ordinary-play-20261010T043102004Z` PASS, 0 runtime error, 16 NPC có xương chân chuyển động.
- 60,208 s, 157,771 FPS, P95 7,586 ms, P99 9,776 ms; CPU/GPU 5,531/2,181 ms.
- Chi phí Station.LateUpdate giảm 0,128732 xuống 0,029388 ms/frame trong cặp ca cùng save.
- FPS cả lượt giảm 160,961 xuống 157,771; không kết luận sửa nhãn làm tăng FPS tổng.
- Ảnh city-overview V12 đã xem trực tiếp: hết nhãn chi tiết đè lên nhau ở góc xa.
- Hình học, shader và simulation không đổi; full-tour V11 vẫn là bằng chứng gameplay trước V12.

## Thử backend CPU

- IL2CPP dựng riêng ở Il2CppCandidate lúc 04:42:49 UTC, 158,138 s, 0 lỗi/28 warning.
- GameAssembly.dll xác nhận backend AOT; sau build Standalone đã khôi phục Mono.
- Hợp đồng và giới hạn: `Docs/V12_IL2CPP_CONTRACT.md`; rà AOT: `Docs/V12_AOT_REVIEW.md`.
- GUID file Editor mới từng dài 33 ký tự làm Unity bỏ qua; đã sửa 32 ký tự, giữ log lỗi.
- Chỉ thử chuyển backend; không đổi mức strip, shader, model, tốc độ chơi hoặc số NPC.
- Ca IL2CPP `ordinary-play-20261010T044555990Z` PASS: tiền/hàng/receipt, 377 điểm, NPC và xe.
- 60,185 s, 160,853 FPS; CPU/GPU 4,857/2,181 ms; P95 6,840/P99 9,427 ms, 0 runtime error.
- 93 NPC theo dõi, 67 đi bộ, 16 có xương chân chuyển động; giữ 39 nhân viên.
- P95 chưa đạt 6,061 ms; stable165 vẫn false, không dùng mean FPS để đổi kết luận.
- Kết quả này là diagnostic có profiler, đứng gần khu bán hàng; cần full-tour mặc định.
- Full-tour IL2CPP từ05:05:40UTC đã kết thúc exit0, không profiler, sync mặc định và save riêng.
- Thư mục: `work/city-expansion-20261008/ordinary-play-20261010T050540824Z`.
- Gameplay đạt13khu/bốn cầu tàu,377điểm,39nhân viên,706tương tác,0runtimeerror.
- Bộ đo cũ chạm150.000frame, chỉ ghi760,092s trước khi dừng lấy mẫu.
- 197,344FPS/P957,440ms chỉ thuộc phần đã ghi; không đại diện toàn lượt, không dùng nghiệm thuFPS.
- Giữ báo cáo gốc; source QA thay mảng giới hạn bằng List dự trữ300.000frame, tự tăng khi cần.
- Hai ca160.000/250.000frame đã tái hiện2FAIL vì dừng ở150.000; sau sửa full-suite878/878PASS,0skip.
- XML: `work/city-model-v12-20261010/full-tests-frame-recording.xml`.
- AotFrameCandidate đã dựng riêng sau sửa bộ đo; full-tour mới đã hoàn tất như mục cuối.
- Báo cáo mới thêm runtimeVSyncCount/runtimeTargetFrameRate; vẫn không chứng minh nhịp scanout.
- Log ghi rõ D3D11 VSync không giới hạn khung hình đúng; giữ nguyên lượt hiện tại.
- Đã xem ảnh district-restaurant và large-stack-0: bàn/ghế và cà rốt48/48 đúng trạng thái.
- Rà riêng trạng thái cửa sổ: `Docs/V12_PACING_REVIEW.md`; không kết luận sync từ cài đặt.
- BuildAudit tổng byte có cả thư mục backup C++; không dùng nó làm dung lượng gói phát hành.
- Các ca dùng 720p, 1080p, 1440p và 4K; kiểm nhãn gần hiện lại không cần frame trễ.

## Gói V11 và ca phục hồi

- Ba ZIP V11 đã kiểm SHA256/CRC; xem manifest tại Delivery của V11.
- Windows.zip giải nén thành PackageSmoke; từng file trùng byte với Release.
- Ca `ordinary-play-20261010T041105107Z` dùng save cuối full-tour tại marina.
- Phục hồi tiền/hàng/receipt và 377 điểm đều đạt, không runtime error.
- Ca đó FAIL kiểm chân: 67 NPC đã đi nhưng camera đứng ở mép thành phố, không lấy được xương chuyển động.
- Không sửa ngưỡng kiểm chân và không bật animation cho mọi NPC ngoài màn hình.
- Ca `ordinary-play-20261010T041955551Z` dùng save gần khu bán hàng, cùng Windows giải nén.
- Ca này PASS chuyển động chân, 39 nhân viên và lên/xuống xe; 60,189 s, 160,96 FPS.
- Ca đứng tại chỗ và có profiler không thay thế full-tour hay chứng minh 165 FPS ổn định.

## Cơ sở model và hiệu năng trước V12

- Gallery V11 PlacementCandidate: 664 ảnh, 2058 checks, 0 runtime errors.
- Kiểm riêng gồm tám hướng quanh vật và một góc trên; công trình đặt trong game có năm góc.
- Gate phương tiện: 11 loại xe/tàu có hành khách thật lên/xuống.
- Full-tour V11 cuối: 900,990 s, 39 nhân viên, 685 tương tác, 0 runtime errors.
- Trung bình cả lượt 159,49 FPS; phần đầy thành phố 149,01 FPS, P95 10,115 ms.
- Snapshot hai mẫu sau tối ưu: 26,152/17,727 ms; vẫn còn frame lưu bị khựng.
- Model V11 không thay đổi trong V12 nên bằng chứng hình học vẫn được giữ đúng phạm vi.
- Kết quả V12 mới phải được ghi riêng; không lấy FPS của V11 làm FPS bản mới.
- Stable165FrameBudget vẫn chưa đạt; goal còn active.

## Bản kiểm sau sửa bộ đo

- AotFrameCandidate dựng05:26:04UTC,73,697s,0error/28warning.
- Diagnostic052632035ZPASS,377điểm,39nhân viên,tiền/hàng/receipt và NPC/xe;0runtimeerror.
- 10.372frame/60,204s;172,282FPS/P958,348ms;stable165false.
- Runtime vSyncCount1,targetFrameRate-1,display165Hz; không dùng các giá trị này thay scanout thật.
- Công cụ Windows chụp ra màn hình khóa lúc thử quan sát cửa sổ, chưa thực hiện di chuyển.
- Theo skill Computer Use, dừng thao tác UI và đã yêu cầu người dùng mở khóa qua câu hỏi async.
- FPS diagnostic trên desktop khóa không đại diện trải nghiệm đồng bộ165Hz khi chơi trên màn hình.
- Full-tour `ordinary-play-20261010T053833445Z` đã PASS:194.505frame/880,977s;0runtimeerror.
- Ghi đủ cả lượt sau giới hạn150.000 cũ:706tương tác,377điểm,39nhân viên,13khu/bốn cầu tàu.
- EngineFPS220,783 toàn lượt;207,242 phần thành phố đầy;P956,468ms/P998,641ms.
- Không profiler;runtimevSync1,target-1;stable165false và desktop vẫn khóa.
- Đây là số liệu engine và gameplay, chưa chứng minh nhịp trình bày165Hz trên màn hình.
- Báo cáo sau đóng gói ở `Delivery/POST_PACKAGE_CHECKS.json`; các ZIP V12 giữ nguyên.
