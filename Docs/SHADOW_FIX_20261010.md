# Sửa bóng khi pan/zoom — 10/10/2026

## Đã đo trước sửa

- Windows Player hiện cửa sổ, D3D11, RTX 4060 Laptop, 1920 × 1080.
- Chụp trực tiếp khung hình màn hình ở cuối frame, không render camera lần hai.
- Chỉ có một directional light hoạt động; chưa có bằng chứng đèn bị nhân đôi.
- Camera di chuyển và zoom 12 → 27 → 12 m, vật thể và đèn đứng yên.
- Dùng tọa độ chiếu đã ghi để quy ảnh về cùng mặt đất; loại thân hộp và HUD.
- Mỗi cấu hình có 61 frame, khoảng 30.000 pixel nền chung và 2.000 pixel bóng.
- Cấu hình cũ: hai cascade, atlas 2048, khoảng 45 m, split gần 0,25.
- Dao động độ sáng vùng bóng P95: **10,936**; bước nhảy trung bình lớn nhất **2,076**.
- Một cascade 4096/70 m: P95 **1,212**; bước lớn nhất **0,280**.
- Đây là ca bóng có vật thể cố định, không phải benchmark FPS toàn thành phố.

## Thay đổi

- Một cascade ở cả ba mức: bỏ ranh giới nội bộ đổi chất lượng khi camera di chuyển.
- Low: 45 m/1024; Cân bằng: 55 m/2048; High: 90 m/4096.
- Mép xa fade 0,25 để bóng mờ dần trước giới hạn, giữ vùng zoom thông thường trong bóng.
- Giữ GPU Resident Drawer/culling; ca bật/tắt culling chưa cho thấy bóng bị nhân đôi.
- Chỉ sửa bản sao pipeline runtime, không ghi đè asset nguồn.
- Cập nhật kiểm tra UI đồ họa theo chính sách mới, giữ lựa chọn chất lượng và render scale.
- Không đổi số nguồn sáng, dữ liệu tiền, hàng hóa hoặc save.

## Bằng chứng và trạng thái

- Đối chứng trước sửa: `work/shadow-v10-20261009/player-20261009T231801527Z`.
- Báo cáo đo: `shadow-stability-analysis.json`; tọa độ/thời gian gốc: `shadow-stability.csv`.
- 4/4 test `ShadowSettingsTests` PASS tại 2026-10-09 23:23 UTC.
- Player thử 70 m/4096: `player-20261009T232704484Z`, PASS, 0 lỗi runtime.
  P95 vùng bóng 1,215 so với legacy 10,936 trong cùng binary, giảm khoảng 89%.
- Regression đầy đủ 819/819 PASS, không skip; XML `work/shadow-v10-20261010/full-editmode-results.xml`.
- Đối chứng 60 giây trên cùng save/binary: cũ 156,703 FPS, CPU/GPU 5,048/2,144 ms;
  thử 70 m/4096 đạt 154,354 FPS, CPU/GPU 5,460/2,069 ms.
- Render counter: skinned mesh 9,26 → 24,90; caster 261,51 → 405,81 khi tăng khoảng bóng.
  Vì vậy giảm mức Cân bằng xuống 55 m/2048.
- Player cấu hình cuối55m/2048: `player-20261009T234611703Z`, PASS,0lỗi runtime.
  P95 dao động độ sáng bóng0,829 so với legacy10,936, giảm khoảng92,4%;
  bước nhảy trung bình lớn nhất0,284 so với2,076. Chưa phải số liệu FPS toàn thành phố.
- Các model V11 và sửa giao thông cuối cần regression/Player mới;819test thuộc nguồn trước V11.
- Mục tiêu toàn game 165 FPS chưa được xác nhận bởi ca kiểm bóng này.

## Lệnh kiểm

```powershell
./Tools/run_unity.ps1 -Task Tests -TestFilter Tycoon.Tests.ShadowSettingsTests
./Tools/run_unity.ps1 -Task Build -BuildPath work/shadow-v10-20261010/After/TYCOON2.exe
./Tools/run_shadow_sweep.ps1 -BuildPath work/shadow-v10-20261010/After/TYCOON2.exe -Visible
python ./Tools/analyze_shadow_stability.py --evidence <thu_muc_Player>
```

## Nguồn

- [Unity: cascade và thời gian render](https://docs.unity.com/en-us/engine/6000.3/manual/lighting-overview/shadows/shadow-realtime/shadow-cascades/use).
- [Unity: khoảng bóng](https://docs.unity3d.com/6000.0/Documentation/Manual/shadow-distance.html).
- `Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8/ShaderLibrary/Shadows.hlsl`.
- `Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8/Runtime/UniversalAdditionalLightData.cs`.
