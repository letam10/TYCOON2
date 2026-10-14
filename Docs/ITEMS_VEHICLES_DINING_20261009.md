# Vật phẩm, phương tiện, bàn ghế và cài đặt — 09/10/2026

## Chơi và cài đặt

- Chạy toàn bộ thư mục Windows bằng TYCOON2.exe, giữ các DLL và thư mục Data bên cạnh.
- Cài đặt: chọn Thấp/Cân bằng/Cao, độ phân giải, tỉ lệ render, tiếng Việt/English và toàn màn hình.
- Đồng bộ màn hình bật: VSync theo tần số quét màn hình. Tắt: chọn giới hạn FPS, mặc định 165.
- Thanh FPS hiển thị FPS đo được, mili giây mỗi frame và Hz màn hình; có nút bật/tắt.
- Bảng cài đặt tạm dừng mô phỏng và thao tác dưới chân. Áp dụng để lưu cấu hình riêng với tiến trình.
- FPS và độ ổn định phụ thuộc cảnh, chất lượng, độ phân giải và máy. Số đo thật nằm trong báo cáo QA.

## Hàng hóa và model

- Đủ 27 loại hàng hiện hành, tiền và kiện hàng dùng model 3D nhận ra được.
- Chồng cầm 2 cột × 3 hàng, 6 món/tầng; tầng trên có món đỡ cùng vị trí phía dưới.
- Cà rốt thuôn nhọn một đầu, lá ở đầu to; nằm ngang cùng chiều, giảm 10% so với mẫu đã chốt.
- Thịt có viền mỡ/vân, rau có lá/gân, bánh và món ăn có chi tiết, chai và túi có kiểu riêng.
- Cầm một loại hàng hoặc tiền; cất/giao hết trước khi đổi. Tiền dùng tối đa 18 bó hiển thị.
- Số tiền thực dùng 64 bit; model bó tiền không giới hạn số dư. Kiện hàng tính tải theo hàng bên trong.
- Kho, khay và thùng xe dùng mặt đỡ thực; xe giao hàng có thùng hở để kiện không xuyên mái.

## Phương tiện và ga

- Xe buýt, xe khách, taxi, xe hơi, xe tải nhỏ, xe tải hàng, xe bồn, xe sang, xe thể thao.
- Tàu có đầu máy và hai toa khách; hai đoàn hoạt động trên hai đường ray riêng.
- Bánh quay theo quãng di chuyển; cửa riêng mở khi dừng, đóng trước khi xe chạy.
- Hành khách đi tới cửa, bước lên, vào ghế, ngồi và xuống qua cửa; phục hồi va chạm sau khi xuống.
- Ga có hai sân ngoài ray, nhà vé hai cửa, cầu và dốc nối hai phía; không đi bộ xuyên đường tàu.
- Xe đường phố theo làn và góc đường bo, tăng/giảm tốc; các model có collider phù hợp.

## Bàn ghế

- Bàn tròn có đúng hai ghế đối diện, đặt gần mép bàn.
- Bàn vuông có ghế dài; quầy phục vụ có ghế phù hợp chiều cao quầy.
- Khoảng cách mép ghế đến mép bàn 0,18 m; bàn cao 1,04 m, đệm ghế cao 0,62 m trên nền.
- Quầy có ba ghế cao; giữ mô hình giao đơn hiện hành qua sáu bàn, không thêm quầy thu tiền mới.
- Vị trí tiếp cận và vị trí ngồi tách rõ; khách chuyển tư thế khi đã đến ghế.
- Giữ sáu ID bàn và một đơn hiện hành trên mỗi bàn để tương thích tiến trình.
- Đồ ăn lấy cao độ mặt bàn; không tự tạo món hoặc doanh thu qua animation.

## Nâng cấp và hiệu năng

- Tiền góp từ tay, tăng tốc sau khi đứng liên tục, giữ số đã góp khi rời ô.
- Model đổi chi tiết theo cấp; ô cuối biến mất cả hình và vùng tương tác.
- Mod tối đa cập nhật cấp và lưu ngay; phần dựng hình được chia qua các frame để giảm khựng.
- Worker tối đa 39, 26 nghề, năng lực ×1,5; cầm hàng xếp gọn và chuyển động theo tốc độ thật.
- Atlas/vật liệu dùng chung, mesh gộp và pool giảm chi phí; không tạo mesh mới trong mỗi frame cầm hàng.

## Cách kiểm lại

```powershell
.\Tools\run_unity.ps1 -Task Tests
.\Tools\run_unity.ps1 -Task Build -BuildPath work/items-vehicles-20261009/Release/TYCOON2.exe
.\Tools\run_items_vehicles.ps1 -Visible
.\Tools\run_city_play.ps1 -Visible -ShowFps
```

- QA model dùng checkpoint riêng, kiểm 27 hàng, tiền/kiện, sáu góc xe, cửa hai phía và đồ đạc nhiều góc.
- QA hành khách kiểm lượt lên/xuống của 11 phương tiện đang hoạt động và đi bộ qua hai phía ga.
- Điểm cửa xe đường phố nằm phía vỉa hè; khách chỉ bật capsule khi đã ra khỏi cửa và vùng đứng trống.
- Đồ vật bị tường che được chụp thêm bản sao mesh sáu hướng; bản sao không chứa logic gameplay.
- Kiện trên xe được chụp riêng cả sáu góc để thấy mặt đỡ và vị trí từng kiện.
- UI kiểm native 720p/1080p/1440p; ảnh 4K là render ngoài màn hình, không phải phép đo FPS 4K.
- FPS kiểm trong lượt ordinary Player simulation 1x với input, khách, nhân viên, sản xuất và save thật.
- Không dùng FPS của lượt có profiler hoặc chụp nhiều ảnh làm số liệu hiệu năng bàn giao.
- Có thể thêm -Manual165 để đo riêng cấu hình giới hạn 165 FPS khi tắt VSync.
- Nguồn mẫu video/ảnh AI định hướng tạo hình; ảnh nghiệm thu là screenshot Player thật.

## Bằng chứng bản tích hợp

Báo cáo chốt sẽ được ghi tại QA/items-vehicles-acceptance-20261009.json sau test và Player.
Các ảnh nguyên gốc và dữ liệu từng frame nằm trong work/items-vehicles-20261009 và các lượt ordinary-play.
Không đưa checkpoint riêng hoặc journal vào gói bằng chứng phát hành.
