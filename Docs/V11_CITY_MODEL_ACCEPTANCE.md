# Nâng model thành phố V11 — 10/10/2026

## Phạm vi đang thực hiện

- Model két sắt, thùng hàng, kệ, quầy và kho giữ dữ liệu hàng/tiền thật cùng vùng tương tác cũ.
- Café có bàn tròn và hai ghế đơn đặt sát; ngân hàng, khách sạn và nhà hát có lô riêng.
- Thêm marina giữa cảng và bãi biển, hai cầu gỗ và dốc nối bờ, quầy vé, thuyền đánh cá/du thuyền.
- Tàu cảng dùng thân kín; cabin, cửa, kính, lan can, phao và thiết bị có hỗ trợ liên tục.
- Police/fire/bar/arcade/playground/monument và đồ đường phố/công viên đang nâng ở nhóm riêng.
- Tổng13landmark; không thêm hệ thống kinh doanh mới cho công trình trang trí.
- Nước thêm ripple nhỏ và bọt sát bờ, lọc theo pixel/khoảng cách để nhìn xa ít rung.

## Tối ưu giữ trong thiết kế

- Hình học tĩnh gom theo vật liệu và ô32m; culling theo khu thay vì gộp toàn thành phố thành một mesh.
- Vật liệu dùng cache chung và instancing; không thêm Light hoặc Update cho từng vật trang trí.
- Shader nước opaque, không SSR/ray tracing/refraction toàn màn hình hoặc cập nhật mesh CPU mỗi frame.
- Các ripple giảm ở xa và khi tần suất vượt kích thước pixel.
- Cấu hình bóng Cân bằng55m/2048/một cascade đã có đối chứng riêng trong SHADOW_FIX_20261010.md.
- Mục tiêu165FPS còn phải đo trên thành phố hiện hành; số liệu cũ không xác nhận bản V11.

## Kiểm chứng cần thực hiện

1. Native Unity tests: hình học, footprint, ghế café, thuyền, traffic23cases và regression toàn bộ.
2. Build Windows D3D11; test thực tế13khu và đường tới hàng chờ/két/cầu gỗ.
3. `run_items_vehicles.ps1 -CityModels -Visible` lấy ảnh model riêng9góc và trong game5góc.
4. Xem ảnh để sửa lỗi rõ ràng; ảnh đủ số lượng không tự chứng minh hình khối đẹp/đặt đúng.
5. LiveVehicleAcceptance giữ nguyên ngưỡng và chứng minh11phương tiện có lên/xuống thật.
6. Nước ABBA15giây/lượt, loại thời gian chụp ảnh khỏi đo; lưu GPU/CPU khi FrameTiming có dữ liệu.
7. FPS ordinary Player thực tế; chụp chuyển động với timestamp thật rồi mã hóa video theo thời gian đó.

## Các lệnh

```powershell
./Tools/run_unity.ps1 -Task Tests
./Tools/run_unity.ps1 -Task Build -BuildPath work/city-model-v11-20261010/Release/TYCOON2.exe
./Tools/run_items_vehicles.ps1 -Visible -CityModels `
  -BuildPath work/city-model-v11-20261010/Release/TYCOON2.exe `
  -LoadPath work/city-expansion-20261008/ordinary-play-20261009T144927602Z/qa-save-v2.json
./Tools/run_items_vehicles.ps1 -Visible -RidesOnly `
  -BuildPath work/city-model-v11-20261010/Release/TYCOON2.exe
```

## Trạng thái

- Native focused44/44PASS, không skip: `work/city-model-v11-20261010/focused-tests.xml`.
- Regression đầu837test có1kỳ vọng kích thước cũ; sửa ghế1,18→1,21m và bồn0,66→0,68m,
  thêm kiểm đáy collider chạm nền. Lượt sau đạt850/850,0skip:
  `work/city-model-v11-20261010/full-tests-after-access.xml`.
- Candidate Windows buildSucceeded,0error,28warning,118.677.695bytes.
- Gallery `player-20261010T001752914Z`:PASS,2055checks,655ảnh,0runtimeerror.
  Đã xem đủ9góc két, kệ, quầy, kho,10đồ đường phố/thuyền; có5góc đặt của các trạm.
  Đây là gallery có tạm dừng simulation khi chụp, không phải bằng chứng vòng chơi.
- Lượt11phương tiện `player-20261010T002505325Z`:FAIL vì CargoTruck0lên/0xuống.
  10phương tiện khác có lên/xuống; giữ nguyên gate240s và đang sửa ca CargoTruck.
- Candidate chưa chứa lối nối mới, hướng café mới, biển ngoài mới hoặc noise sóng mới.
  Phải dựng lại và kiểm ảnh/đường đi/FPS sau các sửa này.
- Nước Candidate ABBA off/on/on/off15s mỗi lượt:GPU1,944/1,950/1,955/1,946ms.
  FPS196,659/191,938/183,923/190,392; không quy toàn bộ chênh lệchCPU thành chi phí shader.
  Noise mới và phần biển mở rộng phải đo lại; số liệu này chỉ thuộc Candidate.
- Đối chứng trước V11 ở55m/2048: `ordinary-play-20261010T000921124Z`,PASS,0lỗi;
 152,421FPS, CPU5,778/GPU2,213ms, P957,206/P999,096ms; diagnostic62,89s.
- Chỉ save QA riêng được dùng. Bản Windows cuối, FPS V11 và nghiệm thu hình ảnh còn chờ.

## Bản kiểm đường nối và biển ngoài

- `AccessCandidate/TYCOON2.exe` dựng00:57:02UTC,0error/28warning,118.684.767bytes.
- Gallery `player-20261010T005735703Z`:PASS2062checks/664ảnh,0runtimeerror.
- Đã xem5góc đặt café/cảng/marina/bãi biển,9góc thùng và3khoảng cách nước.
- Video nước61frame,6,812129s,8,807819FPS mã hóa theo timestamp; không phảiFPS trò chơi.
- ABBA15s mỗi lượt:off200,898FPS/GPU1,896ms;on197,427/1,937;
  on197,789/1,906;off193,999/1,926. Đo phần ripple/foam bật tắt tại cùng góc bãi biển.
- FPS góc bãi biển không thay thế full-tour; chênh lệchGPU nhỏ có nhiễu giữa lượt đo.
- Full-tour `ordinary-play-20261010T010240947Z` đã PASS, không profiler:
  939,61s/138.165frame,147,05FPS, CPU6,549/GPU2,044ms,39nhân viên/17khách.
  Đi4.645,49m,456tương tác thật,377điểm thông; đủ13landmark/bốn cầu tàu.
  P959,397ms/P9912,387ms; frame lớn nhất2.424,06ms chưa xác định đầy đủ nguyên nhân.
- Source sau bản dựng thêm dấu đo capsule/idle/sync; cần dựng lại trước ca CPU chi tiết.
- Sau AccessCandidate phát hiện thùng thu tiền cũ là cube không collider; source đã dùng thùng chi tiết,
  giữ vị trí/kích thước1,1×0,7×0,7m và thêm collider. Tiền dùng mesh bó tiền chung, chạm mặt đỡ0,7m.
- Getter tiền chỉ đọc một lần trong mỗi lần cập nhật hình ảnh; số tiền và giao dịch vẫn theo authority.
- Native853/853PASS,0skip sau thêm ba test thùng/bó tiền:
  `work/city-model-v11-20261010/full-tests-after-cash.xml`.
- CashCandidate dựng01:23:24UTC,118.686.975bytes,0error/28warning.
- Diagnostic `ordinary-play-20261010T012920265Z`:FAIL do năm điểm thu tiền bị chặn;
  147,71FPS/63,56s, CPU6,050/GPU2,237ms, P957,748/P9910,224ms,0runtimeerror.
- Test bổ sung tái hiện hai lỗi điểm thu trong collider và đường NavMesh không hoàn chỉnh:
  `cash-navigation-red.xml`; chuyển điểm đứng ra trước thùng, giữ collider và dữ liệu giao dịch.
- Lượt sau18/18PASS ở `cash-navigation-green.xml`; cần Player kiểm377điểm và thu tiền thật.
- Đã sửa renderer mặt đỡ trùng ở kệ/quầy, POS chìm trong mặt quầy và khung kho trùng.
  Source các sửa này chưa có gallery Player mới; không dùng ảnh AccessCandidate để xác nhận.
- `CheckoutStation.ReadCash` chỉ khoảng0,012ms/frame trong diagnostic; chưa thêm cache tiền.

## Bản kiểm vị trí và collider quầy mới

- PlacementCandidate dựng01:53:15UTC,118.686.975bytes,0error/28warning.
- Gallery `player-20261010T015328423Z`:PASS2058checks/664ảnh,0runtimeerror.
  Đã xem năm góc quầy/kho; renderer mặt đỡ trùng và khung kho trùng đã bỏ, POS nâng lên mặt quầy.
- ABBA nước mới GPU off/on/on/off1,833/1,876/1,872/1,881ms;
  FPS209,658/201,688/203,991/200,607. Chỉ góc bãi biển, không là FPS toàn thành phố.
- Diagnostic `ordinary-play-20261010T015709724Z`:PASS377điểm,không bị chặn,
  đủ39nhân viên/24khách,0runtimeerror.154,845FPS/62,295s,CPU5,334/GPU2,074ms.
- P956,895ms/P999,054ms; stable165FrameBudget=false; chưa nghiệm thu165FPS ổn định.
- CityPlay.CheckCityNavigation22,006ms; chưa giải thích frame2300,555ms lúc phục hồi.
  Source thêm dấu đo ResumeCity/LoadGame/Snapshot để kiểm chứng nguyên nhân ở bản dựng kế tiếp.
- Ca chơi từ đầu `ordinary-play-20261010T015900930Z`PASS trên PlacementCandidate:
  899,733s/143.364frame,159,341FPS,CPU5,279/GPU1,938ms;0runtimeerror.
  Đi4.179,34m,686tương tác,39nhân viên/18khách; đủ13khu và bốn cầu tàu.
  Thu tiền từ quầy mới, gửi/rút két, mod tiền và góp nâng cấp đều qua thao tác player/UI thật.
  P957,462ms/P9910,164ms; stable165FrameBudget=false, chưa đạt165FPS ổn định.
- Native nguồn hiện hành860/860PASS,0skip tại `full-tests-current-source.xml`.
- PacingCandidate dựng02:28:27UTC,118.687.487bytes,21,552s,0error/28warning.
  Chứa thêm dấu đo ResumeCity/LoadGame/Snapshot; hình học không đổi sau PlacementCandidate.
- Gate11phương tiện PASS `player-20261010T023025621Z` trên PacingCandidate,0runtimeerror.
  Hai tàu có3lượt lên/2lượt xuống mỗi tàu,đi1.078,028m và1.075,906m.
  Bus6/6,coach4/4,taxi2/2,sedan1/1,small truck2/2,cargo truck2/2,tanker2/2,luxury1/1,sports4/4.
  Giữ nguyên deadline; chỉ nhận boarding/alighting thực. Distance xe đường bộ là placeholder0,
  không dùng trường này để tuyên bố quãng đường xe đã chạy.
- Đối chứng pacing cùng PacingCandidate/save QA cũ,1080p,Profile+ModOnly,không FPS overlay:
  sync lượt1:146,899FPS/P959,170ms/P9911,973ms; manual lượt1:100,963/19,149/30,406;
  manual lượt2:145,497/8,515/11,085; sync lượt2:127,580/11,386/14,911.
  Các lượt này là diagnostic đứng tại chỗ,không thay full-tour; mặc định sync chưa thay đổi.
- LoadGame khoảng855–930ms,snapshot145–147ms; ResumeCity gồm thời gian chờ qua frame,
  không cộng marker lồng nhau. Check navigation khoảng22ms,checkpoint cuối lượt59–78ms.
- Bảng raw `work/city-model-v11-20261010/pacing-abba.json`; cả bốn lượt stable165=false.
- CheckpointProbe dựng03:16:38UTC,118.687.999bytes,0error/28warning.
  Diagnostic `ordinary-play-20261010T031723380Z`PASS,39nhân viên/24khách,377điểm thông:
  60,218s/156,664FPS,P957,644ms/P9910,728ms,CPU6,216/GPU2,238ms;stable165=false.
- Receipt QA dùng HashSet giữ đủ ID; marker5,179ms. Đây là giảm chi phí QA,không là tối ưu game.
- Checkpoint.TransactionSnapshot72,879/66,029ms;CaptureWorld1,829/0,731ms;
  CopyWorld1,669/1,896ms;Projection1,299/2,680ms. Không thêm index projection chưa cần thiết.
- Source sau CheckpointProbe thay Invoke bằng delegate clone một lần,giữ deep copy.
  Thêm4test isolation/type/long/nested/null/20.000receipt; native và Player còn đang kiểm.
- Video nước PlacementCandidate61frame,6,675353s,8,988289FPS mã hóa theo timestamp;
  SHA256b63fb808b855b02e3f73dfd7afe98a87f0caea91300b013156a5f9ec6f96e124.
  Tốc độ video không phảiFPS game. Chỉ mục `evidence-index-placement.json` kiểm đủ ảnh/gate,
  không tự xác nhận đẹp về mỹ thuật hay165FPS ổn định.
- Release đầu dựng03:24:26UTC,0error/28warning;clone delegate native864/864PASS.
  Diagnostic `ordinary-play-20261010T032512886Z`PASS,154,368FPS/60,187s;
  Snapshot59,482/52,094ms;checkpoint71,668/54,687ms. Chưa đạt165ổn định.
- Save đối chứng có9.277receipt/9.277event,263owner/96stack/56order;
  toàn bộ9.277consumer list đang rỗng. Đường copy List<string> đổi sang constructor trực tiếp,
  vẫn tạo list riêng và giữ null/duplicate/thứ tự;không xóa history hoặc dùng chung list.
- Hai test mới lúc đầu lỗi quyền truy cập helper nội bộ;log `string-list-compile-red.log` được giữ.
  Test đổi sang reflection,API runtime giữ nguyên. Lượt mới866/866PASS,0skip:
  `work/city-model-v11-20261010/full-tests-string-list.xml`;Release đang dựng lại.
- Release hiện hành dựng03:33:25UTC,118.687.999bytes,39,488s,0error/28warning.
  Diagnostic `ordinary-play-20261010T033534095Z`PASS,377điểm/39nhân viên/24khách,
  60,194s/150,498FPS,P958,405ms/P9911,015ms;stable165=false.
  Snapshot26,152/17,727ms;checkpoint44,714/20,074ms.
  So với CheckpointProbe72,879/66,029ms,snapshot giảm ở hai mẫu được đo;
  không quy chênh lệchFPS giữa các lượt thành tăng tốc toàn game.
- Full-tour khởi đầu Release đang chạy `ordinary-play-20261010T034124820Z`,không Profile,
  mặc định syncDisplay=true,1080p,save riêng. Chưa dùng ca đang chạy để nghiệm thu.
- Danh sách source `work/city-model-v11-20261010/source-files.txt` có2.015file;
  loại QA/build-report.json cũ và không lấy asset downloads/AGENTS/overnight ngoài phạm vi.
- Full-tour Release `ordinary-play-20261010T034124820Z`PASS,0runtimeerror,900,990s/143.701frame.
  685tương tác thật,4.172,56m,39nhân viên/18khách,13landmark/bốn cầu tàu.
  Thu/gửi/rút/chi tiền,mod/upgrade,48hàng và kho thật đều PASS.
  Mặc định sync=true,165Hz,1080p:159,492FPS toàn lượt;đủ nhân viên149,008FPS/800,855s.
  P9510,115ms/P9915,217ms,worst2924,458ms;1065frame>16ms,130>33ms,16>100ms.
  CPU6,763/GPU2,075ms ở872mẫu;stable165=false,không nghiệm thu165FPS ổn định.
- Runtime DXGI báo GetFrameStatistics không trả timestamp đúng và dùng CPU timestamp fallback.
  Giữ log nguyên trạng;không đổi cấu hình/di chuyển cửa sổ giữa phép đo để làm đẹp kết quả.
- Cuối full-tour checkpoint19,559/25,005/23,868/21,415ms tại789–912s;
  còn khựng và frame dài khác. Tối ưu snapshot không giải quyết toàn bộ frame pacing.
