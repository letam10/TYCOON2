# Nâng cấp bố trí và hình ảnh 07/10/2026

## Yêu cầu

Bố trí các khu hợp lý, nâng cấp cảnh quan, model, đồ vật, ánh sáng và shader.
Ô nâng cấp dùng icon vẽ theo vật hoặc nghề. Hướng hình ảnh người dùng chọn: bán thực tế.

## Thiết kế và triển khai

- Chuỗi khu: nông trại, cửa hàng nông sản, chế biến, siêu thị, tiệm bánh, nhà hàng.
- Dải ô nâng cấp theo khu; chăm vật nuôi thuộc cụm chăn nuôi.
- Loại trừ footprint thiết bị, điểm thao tác, hàng chờ, bến hàng và đường xe khi đặt ô.
- Mặt trước và bên trái công trình mở theo camera; phía sau có gạch, tường, cửa kính, mái, cột.
- Tủ, ghế, bàn phục vụ có collider; NavMesh tính cả công trình chưa mở khóa.
- Model vát cạnh, khung kho, pallet, tấm kỹ thuật, khe thoát nhiệt, bu lông và bến bốc hàng.
- Cây có thân, nhánh, tán ghép mesh; địa hình đồi dùng lưới thay cho khối cầu.
- Vật liệu gỗ, gạch, đất, cỏ, đá, nền lát, đường, mái, kim loại, kính, sứ và nước.
- Vân lấy theo tọa độ thế giới, có height tạo normal và thông số nhám, kim loại riêng.
- Giảm nhiễu nền và nối vân tại mép texture; vân gỗ có mạch ván.
- Vật liệu đồ dùng được gán trên instance; không sửa vật liệu import gốc.
- Sun, ambient ba vùng, bóng mềm, Neutral tonemapping, màu, bloom nhẹ và fog xa.
- Shader nước có normal động, Fresnel, gợn sáng và chuyển màu sát bờ.
- 81 ô dùng sprite tự vẽ 256 px có khử răng cưa, sắc độ, huy hiệu trục và cấp.
- Pad chỉ có hai mesh nền, sprite và chữ; không có prefab/model hoặc Animator xem trước.

## Nguồn chính

- Runtime/TownUpgradeLayout.cs: phân khu và đặt ô nâng cấp.
- Runtime/TownArchitecture.cs, TownFixtures.cs, TownModelParts.cs, TownProps.cs: công trình và đồ vật.
- Runtime/TownLandscape.cs, TownFoliage.cs: cảnh quan.
- Runtime/TownSurfaceMaterials.cs, TownSurfaceTexture.cs, TownPropSurfaces.cs: vật liệu.
- Runtime/TownLighting.cs: ánh sáng và post processing runtime.
- Resources/VisualRedesign/TownSurface.shader, TownWater.shader: shader có tham chiếu trong Player.
- Runtime/DrawnPurchaseIcons.cs, PurchaseIconCanvas.cs, PurchaseIconShapes*.cs: hình vẽ icon.
- Runtime/QaVisualRedesign.cs: kiểm chứng hình ảnh trong Windows Player.

Các đường dẫn Runtime và Resources ở trên thuộc Assets/_Game/Scripts và Assets/_Game tương ứng.

## Bản chạy và kiểm chứng

Bản chạy: work/visual-redesign-20261007/Release/TYCOON2.exe.
Giữ cả thư mục Release khi sao chép bản chạy.
Kết quả kiểm chứng cuối được ghi tại work/visual-redesign-20261007/acceptance.json.

### Kết quả xác nhận

- EditMode: 195/195 đạt, không lỗi hoặc bỏ qua.
- Build cuối: Succeeded, 0 lỗi, 2 cảnh báo; builtUtc 2026-10-07T09:00:43.8411540Z.
- Player cuối: RTX 4060 Laptop, Direct3D11, 340 kiểm tra, 0 lỗi runtime.
- 360 điểm thao tác/hàng chờ/bến/ô nâng cấp đi được; không có lỗi NavMesh.
- 1.309 lượt vật liệu chi tiết, 40 thành phần kiến trúc được đếm, 57 tán cây.
- 81 icon 256 px; 81 kiểm tra ô chỉ có mesh nền và hình vẽ; không chồng đường xe.
- Ảnh mặt nước cách nhau 0,75 giây có thay đổi; crop nền tĩnh không thay đổi.
- UI được chụp tại 1280x720, 1920x1080, 2560x1440.
- Vòng chơi đầy đủ: 402 kiểm tra, 1.032 thao tác, đi 1.127,36 m, 0 lỗi runtime.
- Vòng đầy đủ dùng cùng kiến trúc/collider mới, trước các chỉnh sửa trình bày và dải ô cuối.

Thư mục ảnh và báo cáo bản cuối: work/town-redesign/player-townlayout-20261007T090110009Z.
Vòng chơi đầy đủ: work/town-redesign/player-town-20261007T081838572Z.
acceptance.json lưu mã băm nguồn và assembly để đối chiếu bản dựng.

Lệnh dùng lại:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_unity.ps1 -Task Tests
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_unity.ps1 -Task Build `
  -BuildPath work/visual-redesign-20261007/Release/TYCOON2.exe
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_player.ps1 -Task TownLayout `
  -BuildPath D:/GAME/TYCOON2/work/visual-redesign-20261007/Release/TYCOON2.exe
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/run_player.ps1 -Task Town `
  -BuildPath D:/GAME/TYCOON2/work/visual-redesign-20261007/Release/TYCOON2.exe
```

Town kiểm tra ví 0, gieo/tưới/thu hoạch, bán/thu tiền, cổng xe tải, vận chuyển, sửa máy,
nhà hàng, hàng chờ, save/load và UI. Phần sau vòng ví 0 dùng fixture sát ngưỡng.
Đây là kiểm chứng vòng chơi và hình ảnh trong phạm vi nâng cấp, không phải campaign 180–240 phút.
Không suy ra FPS ổn định hoặc kết quả soak dài từ những lượt QA ngắn này.

## Tích hợp

ID station, giá, công thức, luật giao dịch và vị trí trung tâm khu được giữ theo nền hiện có.
Layout revision vẫn là 1 vì tọa độ actor của save không bị dịch thêm trong lượt này.
QualitySettings.asset, QA/build-report.json và các art download có sẵn không thuộc thay đổi này.
Hai tác vụ phụ dừng do giới hạn sử dụng; toàn bộ phần thực thi được hoàn thành trực tiếp.
