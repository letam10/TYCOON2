# Model nhiều góc và bàn ghế — 09/10/2026

## Quyền sở hữu

- Root: QaDriver, QaItemVehicleAcceptance, QaVehicleGallery, QA nhiều góc mới,
  camera kiểm tra, Tools, build/Player, hình ảnh nghiệm thu và lỗi phương tiện nhìn thấy.
- Agent dining: WorldDressing.Table và phần TableMealView, FinalBusinesses,
  RestaurantDirector/DinerAgent, CityProps.Terrace, DiningFurniture* mới,
  DinerSeating* mới, DiningFurnitureTests mới, Docs/DINING_FURNITURE_V9.md.
- Không sửa những phần WorldDressing khác, giao dịch, save, inventory, camera, người chơi hoặc phương tiện.
- Không tạo agent con; không chạy Unity/Player khi root đang kiểm tra.

## Dữ liệu giữ nguyên

- Namespace Tycoon. TableStation.Id, Requirement, AreaId, Inventory và số sáu bàn giữ nguyên.
- Một TableStation vẫn có một đơn hàng hiện hành; hai ghế là bố trí hình ảnh, không nhân đôi doanh thu.
- TableStation.Seat vẫn là điểm tiếp cận có đường NavMesh hoàn chỉnh.
- CityExpansion.Apply dịch Seat và root bằng cùng offset; các anchor con dùng local position.
- Tiền, receipt, order, journal, tiến độ, thời gian kiên nhẫn và lưu game giữ hợp đồng hiện tại.

## API thêm nếu cần

```csharp
public enum DiningTableShape { Round, Square }
public sealed class DiningFurniture : UnityEngine.MonoBehaviour
{
    public DiningTableShape Shape;
    public UnityEngine.Transform[] Seats;
    public UnityEngine.Transform Approach;
    public float TableTop;
}
```

- Root tìm metadata để chụp nhiều góc và kiểm vị trí ngồi, không phụ thuộc thứ tự renderer.
- WorldDressing.Table(TableStation) giữ nguyên chữ ký, chọn round/square ổn định theo ID table_0..5.
- Bàn tròn đúng hai ghế đối diện. Bàn vuông có hai ghế dài đối diện.
- Mép trước ghế cách mép bàn khoảng 0.12–0.25 m; chân/đệm/lưng ghế cân, không xuyên mặt bàn.
- Tạo chi tiết gỗ, khung kim loại, đệm, chân và mép bo vừa đủ; gộp mesh theo vật liệu nếu phù hợp.
- Chân furniture chạm nền y=.06, mặt bàn cùng cao độ đồ ăn; cập nhật TableMealView từ TableTop.
- Ghế quầy thuộc khu restaurant, gần mép quầy; không chắn checkout queue/cash/worker/service zones.
- Terrace tròn cũng đúng hai ghế và giảm khoảng cách theo mép thực.

## Khách ngồi và lối đi

- Khách đi bộ tới Approach/Seat, dừng rồi chuyển tư thế ngồi mượt; hướng mặt nhìn bàn.
- Model ngồi đúng ghế; không làm khách đứng giữa ghế và bàn hoặc đổi Walk/Sit mỗi frame.
- Nếu cần vào footprint ghế, tạm ngừng NavMesh/capsule khi ngồi và phục hồi tại Approach khi rời.
- Save/load, pool reuse, phase 0/1/2/3/4 và feedback phải phục hồi Agent/body đúng trạng thái.
- Không warp người chơi hoặc làm thông thoáng giả bằng cách tắt collider bàn/ghế toàn thời gian.
- Lối phục vụ phía -Z và lối đi giữa bàn rộng, xét player + hàng cầm và worker capsule.

## Kiểm chứng agent

- Test hình bàn/đúng số ghế/độ sát/mặt đỡ/chân nền/collider và metadata nhất quán.
- Kiểm compile API bằng công cụ sẵn có; git diff --check.
- Mỗi file dưới khoảng 300 dòng, mỗi dòng dưới khoảng 120 cột.
- Chỉ báo cáo khi xong, tối đa 15 dòng: file đã sửa, kiểm gì, vấn đề còn mở.
- Root chạy Unity EditMode và Player thật; agent không tuyên bố nghiệm thu hình ảnh từ source.

## Root kiểm nhiều góc

### Phân công lại sau lỗi capacity

- Root tự làm model, WorldDressing, FinalBusinesses, CityProps, DiningFurnitureTests và metadata.
- Agent dining chỉ làm RestaurantDirector/DinerAgent và DinerSeating* mới, DinerSeatingTests và docs.
- DiningFurniture.cs đã được root tạo. Shape có Round, Square, Counter.
- Seats là anchor TÂM HÔNG trên đệm, local y khoảng .51, forward hướng bàn; không phải điểm bàn chân.
- TableTop là cao độ local của root DiningFurniture. Root nằm tại y=.06 trên TableStation.
- Approach là marker NavMesh cạnh ghế (local x khoảng 2.04,z=.65,y=0).
- TableStation.Seat = Approach.position và được CityExpansion dịch cùng root.
- Ghế/bench hai phía x ±1.2; Primary seat index 0 ở x dương, nhìn -X.
- Quầy trang trí có metadata Counter và ba ghế; không thêm TableStation/receipt/doanh thu.
- Agent không chỉnh DiningFurniture.cs hoặc file model/root. Không chạy Unity hoặc tạo agent con.
- Agent căn hông từ bone animator thực, giữ tư thế Sit ổn định, phục hồi nav/capsule khi rời/load/pool.
- Agent tự kiểm compile/static rồi report một lần; root kiểm cả hình ngồi và gameplay Player.

- 10 phương tiện: trước, sau, trái, phải, góc chéo, trên và cả hai phía cửa mở.
- 27 hàng: từng chiếc và 48 món trong tay, ít nhất bốn hướng.
- Tiền, kiện hàng, máy, kho, két, bàn ghế, ga: ảnh từ nhiều hướng thực tế.
- Kiểm đỉnh/normal hữu hạn, support contact và spacing; ảnh thật là căn cứ sửa hình.
- Chạy ordinary Player 1080p với FPS bật, ghi FPS/P95/P99/hitch thực, không suy ra 165 từ cap.

### Sửa normal qua nghiệm thu Player

- Agent item v8 chỉ chỉnh HandItemModels.cs, HandItemCarrotGeometry.cs, HandItemSculptGeometry.cs,
  HandItemTubeGeometry.cs, HandItemMeshPrimitives.cs và ItemModelV8Tests.cs nếu cần sửa nguồn normal.
- Thêm kiểm chứng normal cuối của 29 model đều hữu hạn, không zero và chuẩn hóa đơn vị.
- Không đổi kích thước đã chốt, layout, màu, gameplay, cache API hay camera của root.
- Root giữ QA nhiều góc, model bàn ghế, motion, báo cáo, build và Player.
- Lỗi Player thật: valid normals carrot isolated/HandMesh_carrot; test trước chỉ kiểm length.

### Rà khựng mod tối đa độc lập

- Agent perf chỉ đọc runtime, log và CSV; kết quả lịch sử nằm trong
  Docs/History/LEGACY_DOCS_20261011.md (SOURCE: FPS165_HITCH_REVIEW_20261009.md).
- Không chỉnh runtime/tests/tools hoặc chạy Unity, Player, profiler, benchmark; không tạo agent con.
- Root đang chạy QA hình ảnh, sau đó ordinary Player không profiler để đo frame thật.
- Lượt cũ: work/city-expansion-20261008/ordinary-play-20261009T001859194Z (unprofiled),
  ordinary-play-20261009T002536326Z (profiled, không dùng FPS làm số bàn giao).
- Hai lượt có khựng mod tối đa 756/778 ms. Max transaction/save chỉ khoảng 8/12 ms.
- Root đã đổi UpgradeModelView.RefreshAll(game,false,true) trong ApplyMaximum;
  UpgradeVisualQueue giới hạn hai view hoặc khoảng 2 ms/frame. Chưa có số đo bản mới.
- Phân biệt bằng chứng đo với suy luận source; nêu các đoạn còn đồng bộ trên đường gọi mod,
  điều kiện gây khựng và đề xuất sửa cụ thể nếu số đo mới vẫn cao. Không tự kết luận đã hết lag.
- Report một lần tối đa 15 dòng, root chỉ kiểm vấn đề còn mở; docs dưới khoảng 300 dòng.
