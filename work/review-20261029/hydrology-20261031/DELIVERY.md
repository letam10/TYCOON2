# Bàn giao map ảnh chụp và địa hình biển/sông — 31/10/2026

Source commit: `4b74d0f6bbdceb314495720aba48c32be4d08312` (base `3a3ad93`).
Phạm vi đã làm: map/minimap ảnh chụp thật, biển–sông–cửa sông liên tục, dời bãi công nghiệp vào
đất liền và bến phía Đông sát bờ, sửa kết nối NavMesh/đường tiếp cận, xử lý lỗi ghi save Windows.
Đây là bản sửa có thể chơi để kiểm tra; chưa hoàn thành toàn bộ kế hoạch redesign hoặc phát hành Steam.

## Thay đổi

- Camera riêng chụp 4×3 ô, atlas 4098×2786 gồm viền; world bounds và UV đi cùng từng ô.
  Ảnh, người chơi và POI dùng chung phép chiếu. Pan đã đổi đúng từ delta viewport sang world;
  zoom 0,5–6, rotation và viewport mask giữ nền/icon trong khung. Mở map cập nhật snapshot.
- Biển phủ phía Đông thay cho dải nước 6m bị đất cỏ chắn. Sông đi từ biên Bắc qua thị trấn ra biển.
  Cát/bờ dùng cùng ranh giới với nước; không ghép các hộp hoặc giữ mảng cát chữ nhật trong cửa sông.
- IndustrialYard: tâm (-200,0,315); EastMarina: tâm (230,0,-215). Giữ ID, cập nhật route, access,
  map và signage cùng dữ liệu. Có vạch qua đường tại giao tuyến rừng–công nghiệp.
- Một NavMesh toàn world thay cho hai surface chồng nhau và các link tắt khu mở rộng.
  Cầu/cầu cảng có collider và dốc; nước không có NavMesh đi bộ ngoài đường cầu được chỉ định.
- Giữ source camera gameplay, model nhân vật và Definitions/Farm rules. Bản mới dùng profile
  `coastal-redesign-v2`; không tự tải/ghi đè prototype hoặc `redesign-v1`.
- AtomicSaveFile dùng đọc snapshot có delete sharing và retry có giới hạn cho lỗi thay file tạm
  thời 32/33/1175. Lỗi kéo dài vẫn báo ra; không xóa save cũ để né lỗi.

## Kiểm chứng mới trên bản cuối

| Gate | Kết quả | Evidence |
|---|---|---|
| Unity EditMode | 1219/1219 pass, 0 fail/skip | full-tests-5.xml |
| Windows build | Succeeded, 0 errors, 34 warnings; 16:06:08 UTC | Windows/build-report.json, build-5.log |
| Hydrology Player | 85 checks pass, 0 runtime errors | player-5/hydrology-report.json |
| World/UI/save-slot Player | 6096 checks pass, 0 runtime errors | world-final-2/world-features-report.json |
| Đóng/mở process mới | 9 checks pass, giữ state/slot/bindings | world-final-2/world-features-load-report.json |
| Gói Windows | 111 file trong ZIP khớp SHA256 với build | release-receipt.json |

Hydrology chạy Direct3D11, RTX 4060 Laptop, 1920×1080; yêu cầu cửa sổ hiển thị.
Đã kiểm mốc màu 3D vào đúng pixel atlas, loại renderer nhân vật khỏi ảnh nền, world↔map khi xoay,
không đi bộ trên biển/sông, đường NavMesh tới hai POI, input đi qua cầu, đi lên cầu cảng rồi tự lên/
xuống thuyền. Đường dài Farm→industry được kiểm bằng path hoàn chỉnh; không gọi đó là playthrough toàn game.

Đã xem ảnh mới: full map, zoom/rotate, cầu, bến, bờ biển T0/T1 và cửa sông. Lỗi cát nổi ở player-4
được giữ làm bằng chứng thất bại; player-5 thêm cả kiểm footprint và pixel để xác nhận sửa.
Tập ảnh đầy đủ gồm 12 tile riêng và atlas tại player-5; repository chỉ lưu một số ảnh đại diện.

## Lệnh đã chạy (ROOT là worktree, RUN là work/review-20261029/hydrology-20261031)

```powershell
# Unity = C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe
Unity.exe -batchmode -nographics -buildTarget Win64 -projectPath ROOT `
  -runTests -testPlatform EditMode -testResults RUN/full-tests-5.xml -logFile RUN/full-tests-5.log
Unity.exe -batchmode -nographics -quit -buildTarget Win64 -projectPath ROOT `
  -executeMethod Tycoon.Editor.ProjectBuilder.BuildWindows `
  --build-output RUN/Windows/TYCOON2.exe -logFile RUN/build-5.log
RUN/Windows/TYCOON2.exe -force-d3d11 -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 `
  --qa-hydrology --qa-m0 --require-4060 --qa-output RUN/player-5 -logFile RUN/player-5/player.log
Tools/run_world_features.ps1 -BuildPath RUN/Windows/TYCOON2.exe -OutputPath RUN/world-final-2
Tools/run_world_features.ps1 -Load -BuildPath RUN/Windows/TYCOON2.exe -OutputPath RUN/world-final-2
```

Các giá trị ROOT/RUN trong lệnh là ký hiệu rút gọn cho đường dẫn thật, không phải script chép nguyên
để chạy. Log/report tương ứng giữ timestamp và kết quả thực tế.

## Bản thử

Gói: `TYCOON2-Coastal-Windows-20261031.zip` — 48.509.577 byte, 111 file runtime/hướng dẫn.
SHA256 ZIP: `D464C4EA6068854F94563987B5E2ABDE904C011EED8273C34BD5CBC5CE638A25`.
SHA256 Tycoon.Runtime.dll: `CF59113F20570FD3E7B01BAC939C93908D43DCF27AA7A0BBC42037D5F30882D9`.
Giải nén toàn bộ và chạy TYCOON2.exe; xem README-PLAY.txt. Không dùng ZIP ngày 29/10 làm bản địa hình này.

## Giới hạn

- Chưa đo lại FPS/1% low, soak hoặc campaign 180–240 phút trong phạm vi sửa này.
- Nước vẫn là shader stylized; thay toàn bộ cây/model/nội thất/hoạt động thành phố còn trong kế hoạch.
- Còn 34 warning build có sẵn. Một lượt test cũ lỗi dọn file khóa tạm; focused và full cuối đều pass,
  không có sửa cleanup để giấu lỗi và không khẳng định mọi lỗi I/O trên mọi máy đã biến mất.
- Không upload Steam. Hồ sơ quyền phân phối các model người dùng cung cấp vẫn cần đủ trước phát hành.
- Checkout gốc D:/GAME/TYCOON2 giữ nguyên công việc dở. Worktree còn metadata/settings Unity tự sinh
  ngoài allowlist; không đưa các thay đổi đó, raw download hoặc save QA vào commit.

Kế hoạch đầy đủ: ../PLAN_REDESIGN_20261029.md. Phần map ảnh chụp và phương án dời POI đã được cập nhật
vào implementation; các phase chưa triển khai không được đánh dấu hoàn thành bằng test/build pass.
