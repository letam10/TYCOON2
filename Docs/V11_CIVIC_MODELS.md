# V11 civic, leisure và cảnh quan

## Phạm vi đã sửa

- Police/fire/bar/arcade: mái có gờ chắn và nắp vát cạnh, thoát nước, mặt sau có cửa kỹ thuật,
  nẹp cửa kính hướng đúng ra ngoài, thiết bị mái có chân tiếp xúc mặt mái.
- Police: tháp mái, kính, chân anten và mái che có thanh chống; fire: khung cửa cuốn và trụ nước.
- Bar: dầm, giằng mái hiên; arcade: máy có mặt sau, màn nghiêng, nút, khe xu và mái che có cột.
  Các máy nằm hai bên để giữ lối đi giữa từ điểm approach tới mặt tiền.
- Ghế: mặt gỗ vát cạnh, chân bắt bulông, thanh đỡ liên tục tới lưng ghế và tay vịn.
- Bồn cây: thành rỗng, đất thấp hơn mép, thân cây chạm đất; đèn: đế, nắp kỹ thuật, giằng và kính sát vỏ.
- Playground: chân neo, giằng khung xích đu, lan can tháp, chân đỡ và đuôi máng trượt.
- Monument: chi tiết bệ, bulông bảng đồng, ngọn đồng dùng vật liệu metal; collider bậc theo khối tròn.
- Signal/railing: gá lắp và che kính, chân lan can và cột cuối luôn có kể cả nhịp lẻ.
- Park: sân vòng nối bốn lối đi quanh đài phun nước, bồn có thành, collider theo bồn tròn;
  cây và ghế nằm trong lô 23 x 14 m, giữ điểm trung tâm cũ.

## Kiểm chứng

- `git diff --check` đã chạy: không có lỗi whitespace; chỉ cảnh báo line ending ở file sẵn có ngoài phạm vi.
- Các file runtime mới/sửa chuyên biệt và test đều dưới 300 dòng, không dòng nào dài hơn 120 ký tự.
- Thêm `CityCivicModelTests`: 6 footprint, 4 lối vào, thanh đỡ lưng ghế bằng raycast mesh,
  cột cuối lan can, đường vòng công viên và mặt vát không có tam giác suy biến (14 test cases).
- Chưa chạy Unity hoặc Player theo hợp đồng giao việc; luồng chính chạy EditMode, nhiều góc nhìn và FPS.
- Không thay đổi ID, progression, kho, save, mô phỏng, shader nước hoặc thêm Light component.

## File

- `CityCivicModels.cs`, `CityCivicDetails.cs`, `CityLeisureModels.cs`, `CityStreetPropDetails.cs`.
- Chỉ thân `CityProps.Planter/Bench/Lamp` và `CityLandscape.Park` được sửa trong hai file dùng chung.
- `Assets/_Game/Tests/Editor/CityCivicModelTests.cs` và metadata cho ba file mới.
