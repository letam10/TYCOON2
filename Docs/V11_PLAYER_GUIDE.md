# Hướng dẫn bản Windows V11

## Chơi

Giải nén toàn bộ Windows.zip vào một thư mục rồi chạy TYCOON2.exe.
Giữ TYCOON2_Data, MonoBleedingEdge và UnityPlayer.dll cạnh chương trình.

| Thao tác | Điều khiển |
| --- | --- |
| Di chuyển | WASD, phím mũi tên, cần trái gamepad |
| Chạy khi tay trống | Shift, nhấn cần trái |
| Nhặt/giao/xây | Dừng ổn định 0,25 giây gần vùng tương tác |
| Đổi loại hàng lấy từ kho | Q hoặc RB |
| Zoom | Cuộn chuột hoặc cần phải |
| Lưu | F5 hoặc Start |
| Menu | Esc hoặc Back |

## Tiền, hàng và nâng cấp

Tay giữ một loại hàng hoặc tiền. Giao/cất hết hàng trước khi thu/rút tiền;
gửi tiền vào két hoặc tiêu hết trước khi lấy hàng mới.
Két miễn phí ở khu khởi đầu có vùng gửi và rút riêng; mỗi lần vào vùng chuyển toàn bộ số tiền.
Đứng yên không chuyển lặp. Mọi khoản chi lấy từ tiền đang cầm.
Tiền trong két cần rút lên tay. Tiền không bị giới hạn sức mang.

Mod game có hai dòng: Mod tiền cộng 999.999 xu vào két;
Mod xây dựng tối đa mở/nâng nội dung hiện hành và lưu vào tiến trình.
Mod không thay doanh thu bán hàng thật.

Góp tiền tăng tốc dần khi đứng lâu trên ô; rời ô giữ số đã góp.
Hoàn thành nâng cấp có hiệu ứng và chi tiết model theo cấp.
Ô cấp cuối biến mất cả hình ảnh và tương tác.
Icon ô nâng cấp là hình vẽ 2D.

Hàng cầm xếp 2 cột × 3 hàng mỗi tầng; củ dài nằm ngang cùng hướng.
Cà rốt có đầu nhỏ thuôn và lá ở đầu lớn.
Chỉ trứng, sữa và chai/lọ dễ vỡ vào vỉ/khay/két; số món trong khay bằng số thật.
Thịt, phô mai, bánh mì, ngô, bó lúa, vải và len giữ model riêng.

## Thành phố

Thành phố rộng và dài gấp đôi bản cũ, có 13 khu cảnh quan.
Café, ngân hàng, khách sạn, nhà hát và marina là cảnh quan có collider/lối đi;
chưa có hệ thống kinh doanh riêng cho các công trình này.
Dùng đường và vỉa hè; nhà ga có cầu/dốc qua hai đường ray.
NPC lên/xuống xe buýt, xe khách, taxi và các phương tiện đường phố tại điểm dừng.
Bàn tròn có hai ghế gần bàn; bàn vuông có ghế dài và khu quầy có ghế ngồi.

## Cài đặt

Nút Cài đặt cho phép đổi ngôn ngữ Việt/English, độ phân giải, chất lượng,
đồng bộ màn hình, giới hạn FPS và bật/tắt thanh FPS.
Mặc định đồng bộ theo màn hình. Chất lượng Cân bằng dùng bóng 55m/2048/một cascade.
Nước dùng shader opaque với ripple/bọt lọc theo khoảng cách và kích thước pixel.

## Kết quả và giới hạn hiện tại

Native hiện có 866 test PASS,0 skip. Bản Windows đã kiểm vòng chơi từ đầu,
thu/gửi/rút/chi tiền, 686 tương tác, 377 điểm đường đi và 11 phương tiện lên/xuống thật.
Model thành phố có ảnh riêng 8 hướng quanh vật cộng góc trên và 5 góc đặt trong game.
Gallery tạm dừng simulation khi chụp; FPS gallery không đại diện vòng chơi.

Ca Release từ đầu trên RTX4060 Laptop ở1080p đạt159,49FPS trong900,99giây;
đủ nhân viên149,01FPS,P9510,11ms và P9915,22ms. Mục tiêu165FPS ổn định chưa đạt.
Snapshot đã giảm chi phí,nhưng checkpoint định kỳ và các frame dài còn gây khựng.
Số liệu đầy đủ và phiên bản nguồn/bản dựng nằm trong V11_CITY_MODEL_ACCEPTANCE.md.
Không dùng FPS của video mã hóa làm FPS trò chơi.

## Source và kiểm tra

Mở source bằng Unity6000.6.3f1 và scene Assets/_Game/Scenes/Tycoon.unity.
Nếu clone Git, chạy git lfs pull để nhận asset thật.

```powershell
./Tools/run_unity.ps1 -Task Tests
./Tools/run_unity.ps1 -Task Build -BuildPath work/city-model-v11-20261010/Release/TYCOON2.exe
./Tools/run_city_play.ps1 -Visible -Manual165 `
  -BuildPath work/city-model-v11-20261010/Release/TYCOON2.exe
```

Lệnh QA dùng save riêng. Save người chơi không được dùng cho các ca nghiệm thu tự động.
