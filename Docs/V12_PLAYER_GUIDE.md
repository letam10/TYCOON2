# Hướng dẫn bản Windows V12

## Khởi động và thao tác

Giải nén toàn bộ Windows.zip rồi chạy TYCOON2.exe trong thư mục vừa giải nén.
Giữ các thư mục dữ liệu và DLL bên cạnh chương trình; không chỉ chép file exe.

| Thao tác | Điều khiển |
| --- | --- |
| Di chuyển | WASD, phím mũi tên, cần trái gamepad |
| Chạy khi tay trống | Shift, nhấn cần trái |
| Nhặt, giao, xây | Dừng ổn định 0,25 giây gần vùng tương tác |
| Chọn hàng trong kho | Q hoặc RB |
| Đổi khoảng nhìn | Cuộn chuột hoặc cần phải |
| Lưu | F5 hoặc Start |
| Menu | Esc hoặc Back |

## Tiền và hàng trên tay

Tay giữ một loại hàng hoặc tiền. Giao/cất hết hàng trước khi thu hoặc rút tiền.
Gửi tiền vào két hoặc tiêu hết trước khi lấy loại hàng mới.
Két miễn phí gần điểm xuất phát có hai vùng gửi và rút riêng.
Mỗi lần vào vùng chuyển toàn bộ số tiền tương ứng; đứng yên không gửi/rút lặp.
Mọi khoản chi dùng tiền đang cầm. Tiền trong két cần rút lên tay trước khi xây.
Tiền cầm không giới hạn sức mang; số bó model có giới hạn để giữ hiệu năng.

Mod game có đúng hai dòng: Mod tiền cộng999.999xu vào két;
Mod xây dựng tối đa mở/nâng nội dung hiện hành và lưu vào tiến trình.
Doanh thu vẫn tính từ bán hàng thực tế.

Hàng cầm xếp hai cột × ba hàng mỗi tầng. Cà rốt nằm ngang cùng hướng,
thuôn về đầu nhỏ, lá chỉ ở đầu lớn; model đã thu nhỏ theo yêu cầu.
Chỉ trứng, sữa và chai/lọ dễ vỡ vào vỉ, khay hoặc két; số món phản ánh số hàng thật.
Thịt, phô mai, bánh mì, ngô, bó lúa, vải và len giữ model riêng.

## Nâng cấp và thông báo

Đứng ổn định0,25giây mới bắt đầu góp tiền. Tốc độ góp tăng dần khi đứng liên tục.
Rời ô giữ số tiền đã góp. Hoàn thành có animation và chi tiết model theo cấp.
Ô ở cấp cuối biến mất cả hình ảnh và tương tác. Icon ô là hình vẽ2D.
Bong bóng khách dùng icon, số đã giao/tổng cần và viền kiên nhẫn.
Nhãn ngoài khoảng đọc được ẩn; vào gần cập nhật nội dung trong cùng frame.

## Thành phố và model

Chiều rộng và chiều dài thành phố gấp đôi bản cũ.
Két, thùng, kệ, quầy, kho, máy và các khu cảnh quan đã được nâng chi tiết.
Café, ngân hàng, khách sạn, nhà hát và marina có model, collider và lối tiếp cận.
Các công trình cảnh quan này chưa có hệ thống kinh doanh riêng.
Có công viên, biển, bến tàu, công trình cảnh quan/civic và đồ đường phố.
Nhà ga có hai đường ray và cầu/dốc cho lối qua hai bên.
NPC dùng đường/vỉa hè và lên/xuống phương tiện tại điểm dừng.
Bàn tròn có hai ghế sát bàn; bàn vuông dùng ghế dài; có ghế tại quầy.

Nước có biến thiên sóng, ripple và bọt được lọc theo khoảng cách/kích thước pixel.
Bóng chất lượng Cân bằng dùng một cascade55m/2048 để tránh chuyển bóng chồng.
Model giống nhau chia sẻ mesh/material; đồ tĩnh được gom theo vùng.

## Cài đặt và hiệu năng

Nút Cài đặt đổi Việt/English, độ phân giải, chất lượng, đồng bộ màn hình,
giới hạn FPS và bật/tắt thanh FPS. Mặc định đồng bộ theo màn hình.
Không xem giá trị165trong cài đặt là bằng chứng game luôn đạt165FPS.

Native V12 có878/878test đạt,0skip. Model V11 có664ảnh kiểm nhiều hướng,
2058checks; hình học không đổi trong bản sửa nhãn V12.
Ảnh model riêng gồm tám hướng quanh vật cộng góc trên; khi đặt có năm góc.
Lượt gallery tạm dừng simulation, không đại diện hiệu năng khi chơi.

Diagnostic IL2CPP gần khu bán hàng:160,853FPS/60,185s, P956,840ms;
CPU/GPU4,857/2,181ms. Lượt này có profiler và không thay thế full-tour.
Mục tiêu165FPS ổn định chưa đạt; kết quả toàn thành phố ghi trong báo cáo nghiệm thu V12.
Log Direct3D có cảnh báo VSync; cấu hình bật sync không đủ chứng minh nhịp hiển thị thực tế.
Giữ các frame khựng, không lấy FPS mã hóa video làm FPS game.

## Source và kiểm chứng

Đây là bản kiểm V12, chưa nghiệm thu165FPS ổn định hoặc nhịp hiển thị/VSync trên màn hình mở khóa.
Bộ đo cũ từng cắt ở150.000frame; bộ đo mới có test160.000/250.000frame để tránh mất phần cuối lượt.
Mở source bằng Unity6000.6.3f1 và scene Assets/_Game/Scenes/Tycoon.unity.
Nếu clone từ Git, chạy git lfs pull để nhận asset thực.

```powershell
./Tools/run_unity.ps1 -Task Tests
./Tools/run_unity.ps1 -Task Build -Il2Cpp `
  -BuildPath work/city-model-v12-20261010/AotFrameCandidate/TYCOON2.exe
./Tools/run_city_play.ps1 -Visible `
  -BuildPath work/city-model-v12-20261010/AotFrameCandidate/TYCOON2.exe
```

Lệnh QA dùng save riêng. Tiến trình người chơi không được dùng cho ca tự động.
Chi tiết phiên bản và các giới hạn nằm trong V12_LABEL_PERFORMANCE_ACCEPTANCE.md.
