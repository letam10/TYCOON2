# TYCOON2 V14 - hướng dẫn bản kiểm trên Windows

## Khởi chạy

- Chạy TYCOON2.exe trong thư mục có UnityPlayer.dll, GameAssembly.dll và TYCOON2_Data.
- Không di chuyển riêng file exe ra khỏi các file đi kèm.
- Các thao tác chơi và nội dung thành phố theo Docs/V13_PLAYER_GUIDE.md.
- V14 giữ model và bố trí của bản kiểm V13; thay đổi runtime là tối ưu sao chép lịch sử checkpoint.

## Tiền và hàng

- Tay giữ một loại hàng hoặc tiền; giao/cất hàng trước khi lấy tiền.
- Tiền thu được nằm trên tay; đến vùng Gửi tiền của két để cất vào két.
- Đến vùng Rút tiền để lấy tiền lên tay rồi mua, xây hoặc nâng cấp.
- Tiền cầm không bị giới hạn sức mang; tiền trong két chưa thể dùng trực tiếp.
- Mod tiền cộng 999.999 xu vào két; Mod xây dựng tối đa lưu vào tiến trình hiện tại.
- Khay/két/vỉ/hộp chỉ dùng cho nhóm dễ vỡ đã định nghĩa; số ô có hàng theo số thật đang cầm.

## Đồ họa và FPS

- Dùng phần cài đặt đồ họa để chọn chất lượng, giới hạn FPS và đồng bộ màn hình.
- Có thể bật/tắt hiển thị FPS; số HUD tức thời khác với trung bình của cả lượt chơi.
- Lượt đo ngắn V14: 1080p, Balanced, giới hạn phần mềm 165, đứng tại chỗ, mô phỏng 1x.
- Trung bình 162,836 FPS; P95 6,186 ms; P99 7,661 ms; chưa đạt ngưỡng ổn định 165 FPS.
- CPU game trung bình 15,62%; toàn máy 27,35%; GPU toàn thiết bị 34,50% trong 26 mẫu.
- GPU graphics trung bình 1,967 GHz; GPU 53,54 C, khoảng 52-56 C trong cửa sổ đo ngắn.
- Windows báo CPU 3801 MHz; chưa đo được xung boost thực tế và nhiệt độ CPU.
- Các số trên không đại diện cho toàn thành phố hay nhiệt độ sau thời gian chơi dài.
- Lượt tour 937,574 giây đạt trung bình 184,092 FPS; P95 8,819 ms, P99 12,261 ms; chưa ổn định 165.
- Không ghép nhiệt độ/tải của một cửa sổ phần cứng với FPS của cấu hình hoặc thời gian khác.

## Save và bản kiểm

- Sao chép receipt/event nhanh hơn vẫn giữ đầy đủ lịch sử, số nguyên 64 bit và đối tượng độc lập.
- Không xóa lịch sử giao dịch, đổi ID save hoặc kéo dài autosave để che khựng.
- Native checkpoint tests: 24/24 đạt, gồm 9 trường hợp mới; toàn bộ suite V14 đạt 896/896, 0 bỏ qua.
- Candidate IL2CPP dựng 10/10/2026 08:13:12 UTC: 0 lỗi, 28 cảnh báo.
- Player Normal ngắn đạt kiểm chức năng, 20 ảnh không blank và có NPC chuyển động xương.
- Kiểm ảnh không blank chỉ loại ca không render; không tự chứng minh chất lượng hay 165 Hz thực tế.

## Phạm vi bàn giao

- Các ZIP V13 đã đóng băng trong work/city-model-v13-20261010/Delivery là bản V13.
- Candidate V14 chưa được gọi là gói nghiệm thu cuối hoặc 165 FPS ổn định.
- Báo cáo đo: Docs/V14_PERFORMANCE_FINDINGS.md.
- Rà cảm biến: Docs/V14_HARDWARE_SENSOR_AVAILABILITY.md.
- Rà yêu cầu: Docs/V14_REQUIREMENT_EVIDENCE.md.
- Dữ liệu ngắn có render: work/fps-hardware-20261010/RENDERED_MEASUREMENT_081545.md.
