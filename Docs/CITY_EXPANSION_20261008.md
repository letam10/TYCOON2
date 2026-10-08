# Thành phố mở rộng — 08/10/2026

## Bản đồ và hình ảnh

Diện tích nền tăng đúng 2 lần: 200 × 136 thành 282,84 × 192,33 m.
Trang trại, cửa hàng nông sản, chế biến, siêu thị, tiệm bánh và nhà hàng có khu riêng.
Đường chính, nhánh và bến xe nối các khu; giới hạn di chuyển bao phủ bản đồ mới.
Nhà kính, silo, sân dịch vụ, phố nhà ở, sân ăn ngoài trời, cây, công viên và bờ sông bổ sung cảnh quan.
Xe đường phố, xe buýt, xe khách và taxi chạy trên làn cố định, nối với bến và vỉa hè.
NPC đi theo mạng hành lang chính; xe công cộng có cửa mở, đi tới cửa, ngồi và xuống xe thật.
Collider trang trí tránh vị trí làm việc, hàng chờ, bến xe và ô nâng cấp.
Model/rig/asset nhân vật được giữ nguyên.

27 loại hàng, tiền và kiện có mesh cầm tay chi tiết, lớn hơn bản trước.
Texture atlas gồm albedo, normal và metallic/smoothness; mỗi món dùng một submesh/material chia sẻ.
Mỗi tầng hàng có 3 cột × 2 hàng so le nhẹ; tầng trên thẳng với cột đỡ bên dưới.
Từng SKU có kích thước và góc cầm riêng; cây trồng dài nằm ngang cả khi cầm và trên kho/bàn.
Đáy mesh sau xoay chạm mặt đỡ, mỗi cột hàng đặt tiếp trên món bên dưới.
Thịt có khối lớn rõ ràng; cà rốt và lúa gọn hơn, chồng 48 món giữ chiều cao dưới 1,8 m.
Chuyển động chồng hàng được lọc theo vị trí và hướng, giới hạn độ trễ khi chạy/quay.
Nhãn số lượng nằm trên đỉnh chồng; chuyển hàng chỉ chạy sau giao dịch thành công.

## Vòng chơi khởi đầu

1. Dừng gần luống cà rốt để gieo, tưới, chờ lớn và thu hoạch.
2. Mang cà rốt tới cạnh quầy, đứng chờ để giao cho khách đầu hàng.
3. Giao/cất hết hàng còn trên tay, tới cọc tiền của quầy để thu tiền.
4. Tới vùng **Gửi tiền** của két để cất toàn bộ tiền đang cầm.
5. Tới vùng **Rút tiền** để lấy toàn bộ tiền két lên tay trước khi mua/xây/nâng cấp.

Gửi/rút chỉ diễn ra một lần trong mỗi lượt vào vùng; đi ngang không kích hoạt.
Tay chỉ giữ một loại hàng hoặc tiền. Tiền không có giới hạn sức mang.
HUD hiển thị tiền trên tay và trong két; không dùng ví.

| Sức mang | Cấp 1 | Cấp 2 | Cấp 3 | Cấp 4 |
| --- | ---: | ---: | ---: | ---: |
| Người chơi | 12 | 20 | 32 | 48 |
| Nhân viên thường | 18 | 30 | 48 | — |
| Bốc xếp | 18 | 36 | 54 | — |

Kiện tính tải theo số vật phẩm bên trong; sức chứa kho/xe và đơn khách là hệ thống riêng.
Tối đa 39 nhân viên thay cho 78, giữ đủ 26 nghề với hạn mức 1 hoặc 2 người theo nghề.
Sức mang, năng suất và tốc độ nghề tăng 1,5 lần; giá, sản lượng cơ sở và tải người chơi giữ nguyên.
Nhân viên cũ dư số lượng cất hàng và hoàn thành đặt chỗ trước khi nghỉ; không xóa hàng trên tay.

## Mod, nâng cấp và thông báo

`Mod game` xổ xuống đúng hai lựa chọn; bấm ngoài hoặc Esc đóng menu.
**Mod tiền** thêm 999.999 xu vào két mỗi lần; cần rút lên tay để dùng.
**Mod xây dựng tối đa** mở/nâng nội dung hiện hành, đội nghề, băng chuyền và xe theo định nghĩa từng hệ thống.
Bấm lặp không tạo trùng; kết quả được lưu, không giả tăng doanh thu/đơn hàng/sản lượng.

Ô nâng cấp dùng icon vẽ 2D. Dừng 0,25 giây để bắt đầu góp tiền từ tay.
Tốc độ góp đạt 5 lần sau 2,5 giây; rời/pause giữ phần đã góp nhưng đặt lại tốc độ.
Hoàn tất có phản hồi ngắn và chi tiết model theo cấp; ô cấp cuối biến mất cả tương tác.
Bong bóng khách dùng icon, số đã giao/tổng và viền tròn thể hiện kiên nhẫn xanh–vàng–đỏ.
Thông báo HUD gọn; thông tin dài đọc trong bảng chi tiết, tránh nhiều câu chữ trên đầu nhân viên.

## Save

Save/transaction schema 3, layout revision 2. Giữ ID cũ và receipt/effect ID để chống lặp giao dịch.
Journal cũ được phục hồi trước migration; tiền ví chuyển sang két một lần.
Vị trí player/khách/nhân viên/bến xe chuyển theo khu; nhân viên logistics theo nguồn/đích/đội nghề.
Xe đang chạy được đặt lại đường mới, giữ tỷ lệ đường đã đi và độ dài tương ứng.
Giữ hàng, tiền quầy, góp dở, đặt chỗ và tiến trình; sức mang suy ra từ cấp đã sở hữu.

## Chạy kiểm tra

Xem lệnh build/test trong [README](../README.md).
`Tools/run_city_play.ps1 -Visible` chơi vòng khởi đầu rồi đi qua các khu tối đa ít nhất bốn phút.
`-LoadPath <checkpoint>` tiếp tục chơi checkpoint trong save thử riêng, không đổi save nguồn.
`-Profile` ghi thời gian CPU trong lượt chơi bình thường; không tạo fixture hoặc chạy benchmark.
`-Motion` ghi chuỗi chuyển động cầm hàng sau khi dừng lấy mẫu FPS.
`Tools/encode_city_motion.py --frames <thư mục PNG> --output <thư mục mới>` tạo MP4/GIF cùng manifest.
Bằng chứng từng lượt gồm báo cáo, timeline CSV, ảnh native 1080p, log và checkpoint.

Kết quả chốt, phạm vi kiểm tra và số FPS được ghi trong `QA/city-acceptance-20261008.json`.
FPS trung bình không có nghĩa mọi frame đều đạt 165; báo cáo giữ cả P95 và frame chậm nhất.
Các báo cáo 07/10 thuộc bản trước; không dùng số liệu đó để nghiệm thu thành phố mới.

| Lượt Player 1080p/RTX 4060 | Thời gian | FPS trung bình | P95 | P99 | Frame chậm nhất |
| --- | ---: | ---: | ---: | ---: | ---: |
| Khởi đầu → mở tối đa → sáu khu | 266,87 s | 162,36 | 6,24 ms | 8,19 ms | 748,94 ms |
| Phục hồi save cũ → chơi sáu khu | 344,42 s | 160,47 | 7,14 ms | 9,52 ms | 53,71 ms |

Lượt đầu có một frame dài khi mở tối đa; thời gian core/save đã đo không giải thích toàn bộ frame này.
Không kết luận đã loại bỏ mọi hitch hoặc giữ 165 FPS ở mọi frame.
Lượt phục hồi bật ghi CPU; số đo vẫn là chơi simulation 1x qua input thật, không đổi số nhân vật.
Hai lượt đều 39 nhân viên, 0 lỗi runtime và có lượt lên/xuống xe buýt, xe khách, taxi.

Các lần kiểm tra model riêng dùng fixture để nhìn từng SKU ở tải 1, 24 và 48.
Ảnh 1440p/4K dùng render target đúng số pixel nếu Windows giới hạn kích thước cửa sổ;
báo cáo ghi rõ cửa sổ thực tế, render scale và phương thức chụp.

Tra cứu mở khóa và phần đã góp đọc projection trực tiếp, không tạo lại bộ sưu tập mỗi frame.
View nâng cấp nhân viên đọc đúng đội của mình. Lệnh bị từ chối/chạy lại không chiếu lại inventory.
Inventory chỉ tăng revision khi hàng, sức chứa hoặc reservation thay đổi.
Getter trạm và đội nghề dùng projection đọc; API công khai vẫn trả bản sao khi cần.
Ô nâng cấp giữ cache renderer/collider, bỏ qua component trang trí đã bị hủy cuối frame.
Đồng hồ sau load cộng thời gian đã lưu với delta đã tính trước, tránh timestamp journal lùi do sai số.

Kết quả EditMode hiện hành và Player ghi trong báo cáo nghiệm thu, kèm thời gian và hash bằng chứng.

Lượt chốt đạt 459/459 EditMode và 544 kiểm tra Player có cấu trúc:
219 chức năng, 117 migration, 10 load và 198 hình ảnh, không có lỗi runtime trong các lượt này.
Hai lượt chơi bình thường được ghi riêng, không tính vào 544 kiểm tra fixture.
360 điểm tương tác/làm việc/hàng chờ đi tới được trên NavMesh của bản đồ mới.
Lượt đầu quan sát 72 NPC, 55 có di chuyển và 49 có chuyển động xương trong cửa sổ quan sát.
Ba phương tiện đều ghi được ít nhất một lượt lên và xuống trong mỗi lượt chơi.
Save layout 1 chuyển sang 2 giữ 6 món đang cầm, 999.319 xu và 495 mã giao dịch.
Hash save nguồn giữ nguyên; save thử và journal không nằm trong gói bằng chứng.

Ảnh 720p, 1080p và 1440p đã xác nhận cửa sổ đúng kích thước.
Ảnh 4K có 3.840 × 2.160 pixel render thật, render scale 1, nhưng chưa nghiệm thu cửa sổ native 4K.
Video chuyển động cầm hàng được ghép từ ảnh Player ở 10 FPS sau thời gian lấy mẫu FPS;
video này dùng xem thao tác, không dùng chứng minh tốc độ chơi hoặc frame pacing.

Gói bàn giao nằm trong `work/city-expansion-20261008/Delivery-final-20261008`:
`Windows.zip`, `Source.zip`, `Evidence.zip` và `manifest.json`.
Giải nén Windows.zip thành thư mục riêng; chạy EXE cùng các DLL và thư mục dữ liệu đi kèm.
Manifest ghi commit, SHA-256, số file và kiểm tra CRC của từng ZIP.
Source.zip lấy byte asset thật trong checkout, không đóng gói Git LFS pointer.

## Nền, cỏ và độ mượt

Bỏ lưới lát ô nhỏ và nhiễu hạt dày trên nền/đường; dùng biến thiên rộng, độ tương phản thấp.
Vật liệu đất, cỏ, đường và nền công trình vẫn phân biệt bằng màu và độ nhám.
Cỏ là hiệu ứng texture/shader trên cùng mặt đất, không dựng model, card hoặc cụm cỏ 3D.
Nền nông trại và chuồng dùng bề mặt cỏ; luống trồng vẫn là đất canh tác.
Nét cỏ ngẫu nhiên, biến thiên rộng và mảng đất trống không tạo lưới lặp.
Sỏi nhỏ nằm trong shader mặt đất, không dựng những khối đá độc lập trên nền.
Cây có ba kiểu tán/rễ/cành; vật cản có collider thật.
Đường và vỉa hè có vết rạn thưa được lọc theo kích thước pixel.

Camera cập nhật sau chuyển động cầm hàng; nhãn và bong bóng cập nhật sau camera.
Nhãn tiếng Việt dùng glyph thật, viền tương phản và cỡ theo pixel; HUD không diễn giải rich text.
Player và NPC dùng capsule vật lý; NPC điều hướng bằng NavMesh nhưng chân chạy theo tốc độ thực.
NavMesh chỉ bước cao 0,15 m, phù hợp capsule 0,16 m, tránh chọn đường xuyên qua máng tưới.
Chuyển clip đi/cầm hàng giữ pha bước; nhặt/giao và lắp phần nâng cấp có chuyển động ngắn.
Bong bóng tránh bố trí lại khi nội dung không đổi; viền kiên nhẫn có 512 bước hình ảnh,
trong khi dữ liệu thời gian vẫn giữ giá trị đầy đủ.
Model nhặt/giao và hiệu ứng thu tiền/xây dựng được tái sử dụng; component bay nghỉ khi model trở lại chồng hàng.
Dựng tối đa hai nhân viên mỗi frame, vẫn giữ đủ số đội đã mở và chờ phục hồi hoàn tất trước khi chơi.
Chi tiết cấp trên đồng phục gộp thành một mesh/material chia sẻ; giữ đủ dây, sọc và huy hiệu.
Nhân viên chỉ cập nhật tốc độ và sức mang khi cấp thay đổi, chỉ xóa đường khi đang có đường đi.

Journal giữ một handle ghi trong suốt phiên và vẫn flush bền vững trước publish từng giao dịch.
Checkpoint chụp dữ liệu tách rời trên main thread rồi validate, serialize JSON gọn và ghi nguyên tử ở nền.
Chỉ một checkpoint chạy tại một thời điểm; journal giữ giao dịch mới trong suốt phiên.
Thoát/load chờ checkpoint xong; lúc đóng store mới replay phần đuôi và compact journal dưới khóa.
Thông báo phân biệt Đang lưu và Đã lưu; checkpoint lỗi được ghi nhận và chặn phục hồi không an toàn.
Test kiểm tra ghi tiếp sau checkpoint, reader đồng thời, chặn writer khác và chống lặp sau phục hồi.
Báo cáo chơi ghi thêm P99 và số frame trên 16,67 ms, 33,33 ms và 100 ms.

## File chính

- `CityDistricts`, `CityExpansion`, `CityStreetNetwork`: tọa độ, các khu và đường.
- `CityDressing`, `CityArchitecture`, `CityProps`, `CityLandscape`: cảnh quan và kiến trúc.
- `CityStaticGeometry*`: gộp mesh theo vật liệu và chunk 32 m.
- `CityLife`, `CityTraffic*`, `CityVehicleModel`: cư dân và xe đường phố.
- `CityPedestrian*`, `CityPublicTransport*`: hành lang NPC, bến, lên/xuống xe.
- `Workforce*`: hạn mức 39 người, năng lực và bảo toàn hàng khi nhân viên nghỉ.
- `CitySaveMigration`, `CityWorkerMigration`: chuyển layout an toàn.
- `HandItem*`, `CarryPresentation`, `InventoryStack`: vật phẩm cầm, texture và xếp hàng.
- `CityPlay*`, `Tools/run_city_play.ps1`: lượt chơi thật qua input/UI và báo cáo.

Hai video người dùng được xem trực tiếp; SHA-256 và khung tham chiếu lưu tại
`work/city-expansion-20261008/references`. Video nguồn không đưa vào GitHub.
