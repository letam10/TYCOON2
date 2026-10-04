# Hướng hình ảnh từ video người dùng

Nguồn đối chiếu: video địa phương `FDown.vn_Tai_video_Facebook_720p (HD)_7650(1).mp4`, 512×910, 30 FPS, 51,833 giây. Các frame được trích bằng OpenCV, giữ tại `QA/Reference/`. Video là quảng cáo có cắt cảnh và đoạn kết; những hệ thống không xuất hiện không thể suy ra đầy đủ từ clip.

| Dấu hiệu quan sát | Thời điểm | Hướng triển khai |
|---|---|---|
| Camera chéo từ trên xuống, theo nhân vật | 0–46s | Perspective, pitch 55°, yaw 40°, follow damping |
| Cỏ xanh sáng, cây tròn, sàn cam, tường vàng viền xanh | 0–46s | Palette thống nhất, sàn mở, tường thấp |
| Đàn bò đen trắng dày, gà, đường dẫn ra máy | 0–46s | Bò/gà nguồn người dùng, pen có fence, trạm sữa/trứng |
| Nhân vật đầu lớn, mũ và quần áo đỏ/cam | 0–46s | Model Character1, rig được bổ sung bằng Blender |
| Hàng chồng cao phía sau nhân vật | 7–12s, 25–28s | Carry stack 3D dùng lượng hàng thật, pooling |
| Tự thu/xếp khi đứng sát trạm | 5–13s, 23–28s | Auto interaction, vẫn có E trên Windows |
| Khách lấy hàng/xếp hàng, bong bóng nhu cầu | 10–18s, 28–36s | NavMesh FSM, basket thật, queue tại quầy |
| Cọc tiền xanh trên sàn và người chơi đến nhận | 13–16s, 30–34s | Checkout ghi tiền chờ; collect chuyển đúng lượng sang ví |
| Pad đen có giá, hiệu ứng mở quầy/máy | 0–46s | Kiểm tra giá/điều kiện, chống giao dịch hai lần |
| Thanh tiền gọn ở góc trên phải | 0–46s | HUD nhỏ, Việt hóa và hướng dẫn bàn phím/tay cầm |

Giữ mục tiêu đã duyệt: đủ Farm, Farm Shop, Processing, Supermarket, Bakery và Restaurant. Bakery/Restaurant cần thiết kế bổ sung vì không được mô tả rõ trong đoạn video. Không khẳng định đạt 100% khi chưa có kiểm chứng đối chiếu hình ảnh và cơ chế.

## Model người dùng

Chỉ sao chép các GLB đã kiểm tra từ `D:\APP\TYCOON\ASSET`: Character1/2/3, Cow, Chicken, Machine2. Nguồn và SHA-256 trong `ASSET/UserProvided/provenance.json`. Model gốc không bị sửa. Character có skin/animation mới, Cow/Chicken được giảm topology và thêm motion nhẹ, quầy được giảm topology. Giữ UV/base-color của nguồn. File `Item3` có chữ ký Zstandard thay vì FBX/GLB, không nhập theo đuôi giả.

Phần giấy phép của những model này chỉ xác nhận quyền sao chép cho dự án theo yêu cầu trực tiếp của người dùng; nguồn không kèm giấy phép tác giả để xác nhận quyền phân phối độc lập. Các asset Quaternius có CC0 và font Noto có OFL; hồ sơ nguồn được giữ riêng trong ASSET.
