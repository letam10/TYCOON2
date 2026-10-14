# Dừng theo yêu cầu người dùng — 14/10/2026

**Checkpoint lịch sử.** Người dùng đã yêu cầu tiếp tục, kiểm chức năng nhanh, build
và commit/push. Trạng thái mới nằm ở mục V21 trong `Docs/CODEX_HANDOFF.md`;
không áp dụng lệnh dừng bên dưới cho đợt bàn giao mới.

Người dùng yêu cầu dừng tại đây. Không chạy thêm test, commit hoặc push.
Giữ nguyên các thay đổi chưa commit và bản Windows hiện có.

## Đã làm

- Minimap dưới bên trái, có vị trí và hướng nhân vật.
- Bản đồ lớn bằng M, bấm minimap hoặc Cài đặt → Bản đồ; mở map sẽ tạm dừng game.
- 5 điểm dịch chuyển: TYCOON/nông trại, công viên, nhà ga, bãi biển, bến cảng.
- Cài đặt đổi 10 phím bàn phím, chặn trùng phím, Esc hủy, khôi phục mặc định và tự lưu.
- 3 slot độc lập; slot 1 giữ đường dẫn save cũ; lưu nhanh dùng slot đang chơi.
- Lưu bản sao, tải slot, chơi mới và ghi đè có xác nhận; giữ checkpoint trước ở `.previous.json`.
- Game mới xuất hiện trước quầy nông trại TYCOON, theo lựa chọn đã chốt của người dùng.
- Nước xanh, caustic, gợn sóng nhỏ và bọt bờ; cát ướt và mép cát/nước cong khớp nhau.
- Sửa snapshot mới bị migration lệch vị trí; mũi tên nhân vật nằm trên các pin bản đồ.
- README đã cập nhật điều khiển và các lệnh kiểm tra hiện hành.

## Đã kiểm

- `Tools/run_unity.ps1 -Task Tests`: exit 0, **925/925 pass**, 0 fail/skip.
  XML `QA/editmode-results.xml`: 07:58:10–07:58:39 UTC 14/10/2026.
- `Tools/run_unity.ps1 -Task Build`: exit 0, Succeeded, **0 errors / 28 warnings**.
  Build lúc 08:00:20 UTC, 118.760.511 byte; report hiện hành là `QA/build-report.json`.
- `Tools/run_world_features.ps1`: Player exit 0, **117 checks pass, 0 runtime errors**.
  Lượt cuối: `work/world-map-20261014/player-20261014T080124305Z`.
  Report: `world-features-report.json`, log: `world-features.log`.
- Player đã kiểm M/đóng map/pause, map trong cài đặt, 5 điểm dịch chuyển,
  map ở 1280/1920/3840, đổi phím thật, chặn trùng và hủy/reset phím.
- Đã kiểm lưu/tải/chơi mới, giữ tiến trình đầy đủ với 106 nâng cấp và 39 nhân viên,
  ghi đè có xác nhận, file dự phòng và bảo toàn slot khác.
- Ảnh kiểm chứng mới có trong thư mục lượt Player trên. Đã xem ảnh map và bờ biển
  của lượt trước có cùng phần hình ảnh; ảnh lượt cuối đã tạo nhưng chưa mở xem lại.

## Còn chưa hoàn thành

1. **Chưa chạy Load qua tiến trình game mới** để kiểm slot đang chọn và phím tùy chỉnh
   sống qua relaunch. Lệnh đã định chạy nhưng automatic approval review chặn vì yêu cầu dừng:
   `Tools/run_world_features.ps1 -Load -OutputPath work/world-map-20261014/player-20261014T080124305Z`.
2. Chưa chạy lượt regression `Completion -Case All`, Foundation/Shift và Settings
   trên bản build mới nhất. Kết quả V20 thuộc bản trước, không dùng thay cho bản này.
3. Chưa xem lại toàn bộ ảnh cuối và UI tiếng Anh/các menu mới trên mọi độ phân giải.
4. Chưa cập nhật `Docs/CODEX_HANDOFF.md` lên V21, `DOCS_INDEX`, report nghiệm thu tổng
   và hash binary cuối. File handoff chính hiện vẫn V20.
5. `Builds/Windows/build-report.json` vẫn là report cũ; chưa chép report hiện hành
   từ `QA/build-report.json` sang. Exe đã được build mới đúng lúc 08:00:20 UTC.
6. **Chưa stage, commit hoặc push GitHub.** Repo đang ở `main`, HEAD
   `3ae6638cb9ce4de5c586ae05c7c2aaea9cebbb80`; origin `https://github.com/letam10/TYCOON2.git`.
   Có nhiều sửa/untracked từ các đợt Codex trước cần giữ làm baseline; không reset/clean.

## Bản để thử hiện có và giới hạn

`D:/GAME/TYCOON2/Builds/Windows/TYCOON2.exe` — giữ cả thư mục Windows bên cạnh exe.
Không thấy lỗi runtime trong 117 ca cuối đã chạy; chưa tuyên bố toàn bộ game đã nghiệm thu.
Build còn 28 warning về API Unity obsolete và shader TownSurface.
Không chạy benchmark/soak/campaign dài, đúng phạm vi kiểm nhanh người dùng yêu cầu.
