# Khay hàng dễ vỡ v10

- `InventoryStack.HeldLayout` dùng `CarryContainerLayout` cho đúng năm ID:
  `egg`, `milk`, `bottled_milk`, `sauce`, `soy_sauce`.
- Player và worker đều dùng cấu hình HeldLayout hiện có trong CarryPresentation.
- Mỗi tầng là khay mở 2 cột x 3 hàng, có đáy, vách ngăn thấp và bốn cột đỡ tầng trên.
  Tầng chưa đầy chỉ hiện số model hàng thật; một món chỉ hiện một model.
- Mỗi model vẫn thuộc ItemPool và danh sách items. VisibleCount/VisibleItemId không tính khay.
- Hàng dễ vỡ có localRotation = Quaternion.identity. Scale giới hạn chiều cao mỗi món 0,20 m;
  đáy món chạm mặt trên sàn 0,012 m, khe tới vách và tầng trên dương.
- Top bao gồm cột đỡ của tầng trên cùng; RearExtent bao gồm mép sau khay.
- Khay theo owner được tái sử dụng khi tăng/giảm số lượng, đổi loại và bật lại sau disable.
  Khay thừa bị ẩn, khay bị hủy cùng owner; model hàng trả về ItemPool.
- Mesh khay cache theo ID (mỗi ID có một profile từ mesh hàng cố định); vật liệu dùng cache
  TownSurfaceMaterials. Refresh giữ cơ chế revision nên trạng thái đứng yên không tạo mesh/khay.
- Hàng khác tiếp tục dùng CarryItemLayout; kho HeldLayout=false dùng grid, không tạo khay.

## API dùng trong QA

- `CarryContainerProfile.Supports(id)` xác định năm hàng dễ vỡ.
- `new CarryContainerProfile(id, mesh)` cung cấp Scale, ItemBounds, CellX/CellZ, LayerHeight,
  Width/Depth và Position(index). `Slots = 6`, `Floor = .012f`, `Wall = .009f`.
- `CarryContainerLayout.TryLayout(owner, items, out top, out rearExtent)` sắp xếp items thật;
  `Clear()` ẩn khay; `ActiveCount` trả số tầng đang hiện.
- Tên GameObject khay bắt đầu `CarryContainer_`. QA đếm model hàng phải loại tên này.
- QA rotation hàng dễ vỡ phải so với Quaternion.identity, không dùng CarryItemLayout.For(id).Rotation.

## Kiểm chứng

- NUnit `Tycoon.Tests.CarryContainerTests`: 1/2/5/6/7/13/48 món cho cả năm ID; đúng số lượng,
  hai cột ba hàng, đúng điểm đỡ, không giao nhau giữa model hoặc tam giác mesh khay,
  giới hạn mang hàng và Top/RearExtent; toàn bộ hàng khác không có khay.
- Có kiểm tra kho/grid, đổi HeldLayout, giảm/tăng lượng, đổi SKU, rỗng, disable/enable,
  tái sử dụng model/khay và dùng chung mesh/material giữa hai owner.
- Kết quả native do agent chính chạy: 35 tổ hợp ID/số lượng đạt. Test lifecycle ban đầu thất bại
  vì EditMode không tự gọi OnDisable của MonoBehaviour runtime khi SetActive(false).
  Test đã gọi callback cleanup bằng reflection, giữ các assert reset/ẩn/tái sử dụng và bổ sung
  kiểm tra đúng tập model cũ sau bật lại. Bản sửa đã qua full suite 778/778 ngày 09/10/2026.
- Đã kiểm tra cấu trúc nguồn và định dạng. Tác vụ con không chạy Unity/Player;
  agent chính thực hiện integration và chụp góc nghiệm thu. Kiểm thử EditMode này xác minh
  nội dung callback cleanup; Player vẫn cần xác nhận Unity tự dispatch callback đúng khi disable.
- Player tích hợp `player-20261009T124757000Z`: 3.355 kiểm tra, 1.426 ảnh, 0 lỗi runtime;
  đủ 27 loại hàng ở 1/24/48 món và 9 hướng riêng/khi cầm. Kiểm hình ghi trong
  `Docs/Art/V10_GOODS_REVIEW.md`; chưa thay kết quả này bằng FPS hoặc build.
