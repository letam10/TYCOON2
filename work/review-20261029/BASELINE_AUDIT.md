# Baseline thực tế ngày 29/10/2026

Source baseline: 3a3ad93b66b3ff014dd820b1203f135acb418e49, lấy bằng git fetch origin main.
Checkout D:/GAME/TYCOON2 vẫn ở 1e2329a, 1.265 mục dirty/untracked; không merge/reset/clean/stash.
Worktree mới dùng nhánh codex/tycoon2-review-20261029. Unity 6000.6.3f1, URP 17.6.0.

## Lệnh và kết quả mới

- Unity.exe -batchmode -nographics -quit -buildTarget Win64 -projectPath <worktree>
  -executeMethod Tycoon.Editor.ProjectBuilder.BuildWindows
  --build-output work/review-20261029/Windows/TYCOON2.exe
  Kết quả Succeeded, 0 error / 34 warning, builtUtc 2026-10-29T07:12:40.4158766Z.
  build.log kết thúc return code 0. Build report ghi 309,617 giây; cold import trước build là thời gian riêng.
- Unity.exe -batchmode -nographics -buildTarget Win64 -projectPath <worktree>
  -runTests -testPlatform EditMode -testResults work/review-20261029/tests.xml
  Kết quả 1208/1208 pass, không fail/skip. Không dùng -quit với -runTests.
- Tools/run_world_features.ps1 -BuildPath work/review-20261029/Windows/TYCOON2.exe
  -OutputPath work/review-20261029/player-baseline
  Player exit 2: confirmed new-game overwrite không khôi phục slot mới; runtime có
  IOException: Unable to remove the file to be replaced tại SaveStore.WriteCore/File.Replace,
  từ CheckpointWriter. Lượt này chạy cùng lúc EditMode, không có bằng chứng đó là nguyên nhân.
- Chạy lại cùng command, OutputPath=work/review-20261029/player-isolated, sau khi EditMode xong:
  Player exit 0; 6132 checks, 78 captures, runtimeErrors=[]; 07:18:54–07:19:32 UTC.
  Không sửa code giữa hai lượt. Vì vậy lỗi save đầu tiên là hiện tượng không ổn định chưa xác định
  nguyên nhân, không được báo đã sửa chỉ vì lượt sau pass.

SHA256 Tycoon.Runtime.dll của baseline:
A0A5DAF4C06D57CAAADC0EFF01A0177FAC715D363212F5B08C61B7AAEA92047E

## Đã xem ảnh mới từ Player baseline

- minimap-1920.png: minimap đã cắt trong khung; phần tràn đường như ảnh 19/10 không còn ở cảnh này.
  Farm vẫn nhiều pad hình chữ nhật, nền/prop đơn giản; chưa thể coi đạt redesign sâu.
- street-surface-cross.png: junction mẫu đã liền mặt đường; vẫn có cây tán khối và NPC trên cỏ.
  Một ảnh này không chứng minh toàn bộ route/NPC đều đạt hoặc đều lỗi.
- expansion-center-forest_reserve.png: cây cone/sphere, phân bố thưa/đều, nền đơn giản.
- expansion-center-north_village.png: ba công trình trên nền rất lớn, ít nội dung/đạo cụ.
- cutaway-VillageCutaway.png: ảnh có shell bị ẩn nhưng khu vực tương tác/interior chưa hữu dụng.
- map-1920.png: map vector còn hình chữ nhật placeholder; label/icon nhỏ; zoom/pan/rotate đã có UI.
- clear-water-a.png: nước màu phẳng với lưới caustic đều; không đủ cơ sở gọi đạt mỹ thuật mới.

## Phát hiện nguồn bổ sung

- WorldVegetationCatalog.TargetDensity/MinimumSpacing chưa được consumer runtime dùng để đặt cây.
- CityBiomeVegetation.Generate chỉ có caller trong tests tại lần rg audit này.
- farm-tree/forest-tree/coastal-tree GLB: mỗi file 16+24 đỉnh, 48+96 index = 48 triangle;
  metadata tự ghi ez-tree nhưng không chứng minh generator chạy hoặc mesh được Player dùng.
- Cutaway dùng tên/độ cao và HasInteriorCoverage chỉ đếm renderer; cần thay bằng entry/navigation/activity gate.
- MapViewportState zoom 0,65–3,5; CityMapLocations bỏ qua một số check ở expansion.

## Phạm vi và giới hạn

Không benchmark, soak hoặc campaign dài ở baseline. Không nhận 6132 checks là nghiệm thu art,
Steam hay toàn trò chơi. Chưa sửa runtime save lỗi không ổn định. World photographs trong thay đổi
map tiếp theo là implementation riêng, có log/report/hash riêng sau baseline này.

Build Windows ở thư mục Windows là bản main để đối chiếu, không phải bản redesign sâu mới.
MapPhotoWindows là bản làm riêng phần map ảnh chụp và phải được kiểm riêng trước bàn giao.
