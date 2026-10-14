# TYCOON2 V14: FPS, tải và nhiệt độ phần cứng

## Cập nhật V16, đo giữa vòng chơi 10/10/2026

V16 test ngắn cùng save đạt 161,694 và 161,370 FPS; mọi lượt stable165=false.
Full tour V16 T114554647Z/PID26948 đang chạy khi cập nhật; chưa kết luận FPS toàn tour.
Đo phần cứng cùng executable TycoonTown.exe/PID26948, 132 mẫu/181,183 giây sau warm-up15 giây.
Cửa sổ UTC 11:48:05,491 đến 11:51:03,940; không ghép với FPS của hai lượt ngắn khác.

| Chỉ số V16 | Trung bình | Khoảng quan sát |
| --- | --- | --- |
| CPU riêng game, trên 16 luồng | 17,19% | 11,24-23,02% |
| CPU toàn máy | 37,61% | 18-73% |
| GPU toàn thiết bị | 34,52% | 23-49% |
| Graphics clock GPU | 2,123 GHz | 1,890-2,535 GHz |
| Nhiệt độ GPU | 62,05 C | 58-67 C |
| Công suất GPU | 34,55 W | 17,69-57,87 W |

CPU temperature/actual clock vẫn không có dữ liệu trực tiếp; Windows3801 MHz không phải boost đo thực.
Raw: work/fps-hardware-v16-20261010/samples-20261010T114746317Z/hardware-report.json.
Script đo đã sửa tên tiến trình theo ExpectedExe, vẫn bắt buộc đường dẫn executable chính xác.
V16 native 910/910 đạt; IL2CPP build0error/28warning; chưa nghiệm thu165 FPS hoặc trình bày165 Hz.

## Kết luận hiện tại

Chưa đạt 165 FPS ổn định trên toàn thành phố. Không gọi các số FPS engine là trình bày thực tế 165 Hz.
Máy đo: Ryzen 7 8845H, 8 nhân/16 luồng; NVIDIA RTX 4060 Laptop GPU, Windows, 1920 x 1080.

## Đứng tại chỗ, giới hạn phần mềm 165

Player T081545725Z: 60,189 giây, 9801 frame, Normal, Balanced, thành phố tối đa, mô phỏng 1x.
Trung bình 162,836 FPS, P95 6,186 ms, P99 7,661 ms; stable165FrameBudget=false.
26 mẫu phần cứng trong 36,321 giây sau warm-up; cùng executable/PID 17796.

| Chỉ số | Trung bình | Khoảng quan sát |
| --- | --- | --- |
| CPU game, chuẩn hóa trên 16 luồng | 15,62% | 11,96-20,92% |
| CPU toàn máy | 27,35% | 7-52% |
| GPU toàn thiết bị | 34,50% | 29-39% |
| Graphics clock GPU | 1,967 GHz | 1,890-2,535 GHz |
| Nhiệt độ GPU | 53,54 C | 52-56 C |
| Công suất GPU | 30,73 W | 28,63-45,49 W |

## Tour toàn thành phố, đồng bộ màn hình

Player T085522987Z: 937,574 giây, 172600 frame; functional PASS, exit 0, 0 runtime error.
39 nhân viên, 23 khách, 456 tương tác, đi 4645,634 m; 42/42 ảnh không blank, đã xem cảnh thực.
Trung bình 184,092 FPS; P95 8,819 ms, P99 12,261 ms, worst 587,419 ms; stable165=false.
318 frame trên 16,67 ms, 47 trên 33,33 ms, 6 trên 100 ms.
CPU/GPU frame time 5,675/2,115 ms từ 924 mẫu timing hợp lệ; không quy đổi ms thành phần trăm tải.
Log có cảnh báo swap chain không trả timestamp đúng, Unity dùng timestamp CPU.

Đo phần cứng đoạn đầu: 128 mẫu/180,583 giây; không phải trung bình của toàn bộ tour.

| Chỉ số | Trung bình | Khoảng quan sát |
| --- | --- | --- |
| CPU game | 17,44% | 6,00-24,19% |
| CPU toàn máy | 61,63% | 26-100% |
| GPU toàn thiết bị | 75,71% | 20-98% |
| Graphics clock GPU | 2,467 GHz | 1,890-2,535 GHz |
| Nhiệt độ GPU | 77,39 C | 66-84 C |
| Công suất GPU | 73,03 W | 25,53-91,84 W |

## CPU và phạm vi cảm biến

- Chưa có nhiệt độ CPU hoặc xung hiệu dụng/boost CPU được đo trực tiếp.
- Windows báo 3801 MHz; không dùng trường này để kết luận CPU giữ xung thực tế 3,80 GHz.
- HWiNFO đã cài nhưng giao diện sensor chưa active khi rà; không tự điền số ước đoán.
- CPU total và GPU total bao gồm các ứng dụng khác; CPU game được tính theo tiến trình đúng executable.
- Mẫu cuối lượt là cửa sổ riêng 11 mẫu/15,404 giây, GPU 35,27%, 2,150 GHz, 58,45 C.
- Các cửa sổ ngắn không chứng minh nhiệt độ ổn định sau chơi lâu hoặc xác định nguyên nhân mọi hitch.

## Source và kiểm chứng

- Native full suite V14: 896/896 đạt, 0 lỗi/0 bỏ qua; build IL2CPP 0 lỗi, 28 cảnh báo.
- So 303 runtime CSharp với Source.zip V13: chỉ CheckpointSnapshot.cs thay đổi.
- Các ZIP V13 giữ nguyên; Candidate V14 chưa được gọi là gói nghiệm thu cuối 165 FPS.
- Raw FPS: work/city-expansion-20261008/ordinary-play-20261010T085522987Z.
- Raw hardware: work/fps-hardware-20261010/samples-20261010T081545419Z và samples-20261010T085522643Z.
- Còn cần tối ưu/đo lại các khung hình chậm và xác minh trình bày màn hình thực tế.
