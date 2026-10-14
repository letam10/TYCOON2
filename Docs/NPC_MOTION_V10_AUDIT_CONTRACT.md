# Hợp đồng chẩn đoán chuyển động NPC v10

- Workspace D:/GAME/TYCOON2, Unity 6000.6.3f1, URP 17.6, Windows.
- Chỉ đọc source và bằng chứng, chỉ tạo Docs/NPC_MOTION_V10_AUDIT.md.
- Không sửa C#, test, asset, save; không chạy Unity/Player, spawn, commit hoặc publish.
- Báo cáo đang lỗi: work/city-expansion-20261008/ordinary-play-20261009T131318597Z.
- npc-motion-evidence.json: observed 98, walked 86, animated 0; không lỗi runtime.
- Player đã đi đủ 8 khu và 2 cầu gỗ, đủ 39 nhân viên và các xe có lượt lên/xuống.
- CityActorEvidence API: Sample(GameSession), bool Save(string directory).
- Track lấy các bone tên chứa Leg/Foot, so localRotation, chỉ ghi khi ActualSpeed > .4
  và ActorView.State là Walk/Run/CarryWalk; Sample đang gọi mỗi giây trong CityPlayReport.
- ActorView.Initialize dùng Animator.cullingMode=CullUpdateTransforms.
- Dự án đã có kiểm animation thật trong QaPassengerMotion/QaDiningAcceptance; đối chiếu cách đo.
- Mục tiêu: tìm nguyên nhân cụ thể (bone/pose/sampling/culling/state/controller) có bằng chứng nguồn,
  phân biệt lỗi model/animation thật với lỗi phép đo. Gợi ý sửa tối thiểu và ca kiểm có ý nghĩa.
- Nếu dữ liệu chưa đủ, ghi rõ instrumentation tối thiểu cần thêm; không đoán nguyên nhân là đã xác nhận.
- Root sở hữu sửa performance và binary; không liên lạc giữa chừng, báo một lần tối đa 15 dòng.
