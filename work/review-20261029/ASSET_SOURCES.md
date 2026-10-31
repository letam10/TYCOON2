# Nguồn asset và quy trình nhập — 29/10/2026

Các mục dưới là shortlist đã kiểm trang nguồn, chưa phải danh mục đã import vào redesign.
Mỗi asset thực nhập phải có record riêng: tên asset, tác giả, nguồn gốc, URL tải, license đầy đủ,
ngày lấy, version/commit, SHA256, file gốc, file dẫn xuất và những chỉnh sửa đã thực hiện.

| Nhóm | Nguồn/tác giả | License quan sát | Vai trò dự kiến |
|---|---|---|---|
| Cây procedural | Daniel Greenheck, https://github.com/dgreenheck/ez-tree | MIT ở commit dcf309bd86bd521083d9c70f01f2de45fdc7c457 | Tạo mesh cành/lá thực, seed/LOD, không chỉ gắn metadata vào khối primitive |
| Nature kit | Quaternius, https://quaternius.com/packs/ultimatestylizednature.html | CC0; 63 model, FBX/OBJ/Blend/glTF | Bụi/cỏ/hoa/đá và cây cần đối chiếu style |
| PBR vật liệu | ambientCG, https://ambientcg.com/ và https://docs.ambientcg.com/license/ | CC0 1.0 | Gỗ, vữa, đá lát, đất; record tác giả cụ thể từ metadata asset trước nhập |
| PBR/HDRI/model | Poly Haven, https://polyhaven.com/license | CC0 | Vật liệu/ánh sáng/đạo cụ; lưu tên artist theo từng asset |
| Ví dụ gỗ bến tàu | https://polyhaven.com/a/wood_planks_grey | Chính sách CC0 của Poly Haven | Candidate weathered wood; chưa tải, chưa xác nhận artist trong record |

Giấy phép ez-tree đã kiểm trực tiếp:
https://raw.githubusercontent.com/dgreenheck/ez-tree/dcf309bd86bd521083d9c70f01f2de45fdc7c457/LICENSE
Copyright (c) 2024 Daniel Greenheck. Giữ notice MIT cùng phần nguồn/asset liên quan.
Không suy quyền texture bên thứ ba từ license mã generator nếu upstream chỉ định license riêng.

## Quy trình có bằng chứng

1. Chọn một asset mẫu đủ gần để thấy silhouette/material; không tải nguyên kho chưa dùng.
2. Lưu license/tác giả/hash. CC0 vẫn ghi credit vì người dùng đã yêu cầu đầy đủ nguồn/tác giả.
3. Chuyển/import vào Unity thật bằng pipeline hỗ trợ định dạng; GLB không tự trở thành model Unity
   chỉ vì được chép vào Assets. Kiểm prefab chứa MeshFilter/MeshRenderer/material đúng.
4. Đo bounds, scale, pivot; collider thân/đế; LOD mesh thật và screen coverage. Không dùng list
   khoảng cách trong JSON để thay bằng chứng LOD hoạt động.
5. Đặt vào scene, đếm renderer/instance, chụp gần/xa trong Player, kiểm import/build dependency.
6. Chỉ asset vượt visual/runtime gate mới vào allowlist. Ghi rõ asset thay thế hoặc được giữ.

## ImageGen

Dùng công cụ tích hợp cho texture/decal/sign/icon/reference và kiểm lại khi gắn lên geometry thật.
Không dùng ImageGen tạo ảnh thay cho cảnh 3D, và không dùng cho nền map chụp world mà người dùng
vừa yêu cầu. coastal-town-concept.png chỉ là mẫu định hướng; nhân vật minh họa không thay model cũ.
Prompt sẽ được lưu riêng; file nguồn tại generated_images được giữ nguyên.

## Asset nhân vật được giữ

ASSET/UserProvided/provenance.json hiện chỉ ghi quyền sao chép theo yêu cầu người dùng,
chưa có giấy phép tác giả riêng xác nhận quyền phân phối. Đây là hồ sơ cần bổ sung trước phát hành
Steam; không tự thay nhân vật hoặc kết luận giấy phép thương mại đã đủ.

## Steam

Review store/build là bước riêng: https://partner.steamgames.com/doc/store/review_process
AI asset được đóng gói cho người chơi cần kê trong Content Survey:
https://partner.steamgames.com/doc/gettingstarted/contentsurvey
Không nhận build local hay test pass là Valve đã duyệt. Lần này chưa upload depot/store.
