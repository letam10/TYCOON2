# Hợp đồng model hàng và phương tiện V8 — 08/10/2026

## Phạm vi và quyền sở hữu file

- Root: cài đặt/FPS đã có, passenger movement, CityTraffic/PublicTransportVehicle,
  CityLife, WorldFactory/CityDressing, đường ray/ga/tàu, QA Player, build và nghiệm thu.
- Agent item: HandItemModels/Produce/Products/MeshPrimitives/SurfaceAtlas,
  CarryItemLayout, file HandItem* mới, test CarryItemLayoutTests/CityCarryTests/CarryVisualTests,
  CarrotShapeTests và ItemModelV8Tests mới, Docs/ITEM_MODEL_V8.md.
- Agent vehicle: CityVehicleModel, CityPublicTransportModel, CityVehicleRig mới,
  CityVehicleGeometry/Parts/Surface* mới, CityRailVehicleModel* mới,
  VehicleModelV8Tests mới, Docs/VEHICLE_MODEL_V8.md.
- Hai agent không sửa file root, không sửa file của nhau, không chạy Unity/Player.
- Không tạo agent con. Root chạy test/build chung sau khi cả hai đã hoàn thành.
- Mỗi file khoảng dưới 300 dòng/120 cột; giữ source ngoài thư mục build.

## Model hàng

- Giữ API HandItemModels.Supports/Build(id,parent), ID của đủ 27 hàng + cash + crate.
- Model có hình dáng riêng và chất liệu rõ ở camera chơi, thân/lá đúng sinh học.
- Carrot đã chốt: đầu nhọn, lá chỉ đầu to, scale .756 (.84 giảm 10%),
  nằm ngang qua tay, 2 cột × 3 hàng, 6 củ/tầng, cùng chiều, không đảo đầu.
- Áp dụng bố trí 2 cột × 3 hàng cho mọi hàng cầm, cùng hướng trong từng SKU.
- Đồ dài nằm ngang; thịt/đĩa/thùng/bó tiền có đáy tựa tay. Đĩa/bát chứa đồ không lật nghiêng.
- Scale ModelSize rõ và nhất quán, tổng 48 món không che toàn bộ người chơi.
- Giữ Spacing dựa bounds sau xoay; tầng trên có điểm đỡ ở cùng x/z với tầng dưới.
- Cỡ/layout không phụ thuộc thứ tự object trong pool; nhặt/giao không phát sinh mesh mới/frame.
- Bake dựa đỉnh mesh thật để max extent = ModelSize và min.y = 0,
  không dùng bounds AABB phình từ primitive đã xoay. Một shared renderer/material cho mỗi model.
- Hàng đặt trên kho/bàn có đáy chạm mặt đỡ; không sửa inventory/save/transactions/nhân vật.
- Root xử lý vị trí tay và góc camera QA nếu còn bị che bởi mũ; agent không đổi player/IK.

## API model phương tiện

Namespace Tycoon. Enum CityVehicleKind đã được root ghi trong Runtime.

```csharp
public static class CityVehicleModel
{
    public static Transform Build(string color, Transform parent, bool van);
    public static Transform Build(CityVehicleKind kind, string color, Transform parent);
}

public static class CityPublicTransportModel
{
    // 0 bus, 1 coach, 2 taxi; thêm CityVehicleRig trên root; trả cửa hành khách.
    public static Transform Build(int kind, Transform root);
}

public sealed class CityVehicleRig : UnityEngine.MonoBehaviour
{
    public CityVehicleKind Kind { get; set; }
    public UnityEngine.Vector3 DoorInsideLocal { get; set; }
    public UnityEngine.Transform[] Wheels { get; set; }
    public UnityEngine.Transform[] LeftDoors { get; set; }
    public UnityEngine.Transform[] RightDoors { get; set; }
    public float WheelRadius { get; set; }
    public void SetTravel(float speed, float delta);
    public void SetDoorOpen(float amount, int side = 1);
}

public static class CityRailVehicleModel
{
    public static Transform Build(Transform parent, string color);
}
```

- Giữ bool van wrapper; van ánh xạ SmallTruck. Model taxi sedan thật, coach khác bus.
- Chiều trước +Z, trục bánh +X; root không scale khác Vector3.one.
- DoorInsideLocal là điểm bàn chân trong cửa phải (x > 0), không phải tâm cánh cửa.
- Bus/coach dài 8.6 m, rộng 2.45 m; taxi dài 4.2 m, rộng 1.85 m.
- Bus/coach cửa phải gần z=2.6, chân y=.54; taxi cửa phải gần z=-.35, chân y=.2.
- SetTravel quay wheel pivots dựa tốc độ thực; SetDoorOpen clamp 0..1,
  bus/coach/train trượt cửa, taxi/cars mở bản lề; panel và wheel roots không bake vào hull.
- Các phương tiện đường bộ có collider + NavMeshObstacle, carving=false; không sinh collider/mesh mỗi frame.
- Cửa có khoảng mở hình học, bậc, ghế và vị trí ngồi nhìn thấy qua kính.
- Model train: đầu máy + hai toa khách, dài tổng <=32 m, rộng <=2.8 m,
  cao <=3.6 m, sàn hành khách y=.6, cửa toa đầu z khoảng -10, có cửa hai phía.
- Rig train DoorInsideLocal x=1.3, y=.6, z=-10, wheel radius=.38.
- Train collider/obstacle theo từng toa, không một box trùm khoảng trống giữa các toa.
- Không tạo thương hiệu; vật liệu dùng atlas/shared material, gộp thân theo material,
  <=12 renderer/hull và <=32 renderer/vehicle gồm chi tiết động.
- Mesh không bị rò khi world load/unload; cache shared hoặc component dispose các mesh sở hữu.

## Đường ray/ga và hành khách do root thực hiện

- Hai đường ray song song z=-61 và -67, nằm trong ground hiện tại.
- Ga có hai sân ngoài ray tại z=-57.5 và -70.5; điểm dừng đầu tàu x=10.
- Cầu đi bộ cao 4.8 m, dốc 18.5 m theo trục X, thông hai phía ga.
- Reserve hành lang rail/ga trước rải cây/nhà; ray gộp mesh, không collider trên từng tà vẹt.
- Passenger carrier interface ICityPassengerCarrier đã có; bus/coach/taxi/train dùng cùng state machine.
- Hành khách đi đến cửa, lên bậc, vào ghế, ngồi; xuống qua cửa khi dừng;
  không dịch chuyển tức thời, không trùng capsule/chiếm cùng ghế hoặc xuất hiện lại giữa đường.
- Các loại đường bộ mới chạy trên lane chính, tránh người và xe trước;
  root quản lý nhịp tăng/giảm tốc và vị trí trước khi quay.

## Mẫu hình ảnh và kiểm chứng

- Mẫu phương tiện: Docs/Art/vehicle-reference-v8.png, được imagegen tạo theo yêu cầu.
- Mẫu ảnh là hướng tạo hình; bằng chứng nghiệm thu phải là screenshot Windows Player thật.
- Agent đọc skill karpathy/Unity thích hợp; agent chỉ kiểm tra static/diff/tests source,
  root chạy EditMode tập trung và toàn bộ, build Player rồi kiểm tra chức năng/hình ảnh.
- Agent tự kiểm đủ SKU/kind, compile-compatible API và git diff --check.
- Report một lần <=15 dòng: file đã sửa, cách self-check, vấn đề còn mở.
