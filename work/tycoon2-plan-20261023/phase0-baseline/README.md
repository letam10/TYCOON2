# TYCOON2 Phase 0 — đồng bộ và baseline an toàn

Phase 0 được thực hiện trong managed worktree từ `origin/main` tại `1e2329a249c62c133b7d11535733c9055a2c9951`. Checkout bẩn `D:\GAME\TYCOON2` được giữ nguyên; status và hash của checkout đó nằm trong các file manifest cạnh tài liệu này.

Profile redesign dùng ID `redesign-v1`. Ordinary play ghi slot dưới thư mục profile mới, QA vẫn dùng thư mục riêng, và save prototype cũ không bị đọc hoặc ghi đè. `MapAndSlotPersistenceTests` kiểm tra cả path isolation và nội dung hai slot.

Các blocker baseline đã được xử lý theo nguyên nhân:

- Shader runtime được resolve từ Resources khi Player không resolve được URP Lit theo tên.
- Font NotoSans và asset EZ Tree/FBX LFS được hydrate từ repo trước khi import.
- Route pedestrian authored edge không bị validator loại sai; passenger có leg NavMesh ngắn tới cửa xe và restore trên sidewalk cao.
- Table meal view, carry atlas, kính xe và typography không còn tạo exception khi asset hoặc shader runtime chưa sẵn sàng.

Evidence cuối nằm trong `phase0-final-gates.json`, `phase0-final-editmode-results.xml`, `phase0-final-build-report.json` và `../phase0-player-final-2/`.

Gate cuối tại `2026-10-23T13:08:37Z`:

- Full EditMode: `1208/1208` passed, `0` failed.
- Windows build: exit `0`, errors `0`, warnings `5`, executable SHA256 `96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873`.
- Player smoke: exit `0`, `WORLD-FEATURES_QA PASS`, RTX 4060 Laptop, `runtimeErrors=[]`, `6132` checks, fresh map/minimap/HUD/cutaway/water/route/save screenshots.

Phase 0 source, test, build and Player gates are green. Native headed visual review, a measured 60-second FPS/1% low run, process-close/reopen persistence and Steam package verification remain separate gates and are recorded independently when executed.
