# TYCOON2 — Kế hoạch redesign sâu, 29/10/2026

Trạng thái: thiết kế để rà soát, chưa phải xác nhận đã triển khai. Baseline: origin/main
`3a3ad93b66b3ff014dd820b1203f135acb418e49`. Worktree riêng: `tycoon2-review-20261029/TYCOON2`.

## 1. Quyết định đã chốt trong cuộc trao đổi này

- Giữ camera hiện tại, model/rig/animation nhân vật và cơ chế logic Farm đang chạy trên main.
- Bối cảnh: thị trấn nông nghiệp ven biển, bán thực tế, đồng bộ với nhân vật hiện có.
- Trục chính: Farm → cửa hàng → chế biến → siêu thị → nhà hàng. Bakery là nhánh cung ứng đang có,
  không tự bỏ chỉ vì không xuất hiện trong cách viết tắt chuỗi.
- Phần lớn nhà/công trình phải vào được, có hoạt động phù hợp; không chỉ một vài nhà gameplay.
- Có thể thiết kế lại bố cục, mô hình, nội thất, đường, thiên nhiên, HUD/map, NPC và hoạt động ngoài Farm.
- Được tìm asset từ mọi nguồn; ghi tác giả, nguồn, license và chỉnh sửa đầy đủ. Ưu tiên nguồn miễn phí
  có quyền sử dụng thương mại để không phụ thuộc mua asset trước khi có mẫu chơi được.
- Giữ Unity 6000.6.3f1 / URP 17.6.0 / Windows 3D. Không dùng các guideline Unity 6.3 để tự hạ engine.
- Build cho người dùng chơi; commit/push main đã được yêu cầu. Không có yêu cầu upload Steam.
- Checkout gốc D:/GAME/TYCOON2 và save cũ được giữ nguyên. Save redesign sâu dùng profile riêng,
  khác cả legacy lẫn redesign-v1 vì bản 23/10 đã có người chơi thử.

**Thay đổi so với kế hoạch cũ:** PLAN.md là tài liệu tham khảo có nhiều luật lịch sử, không còn là
nguồn để ghi đè Farm hiện tại. Không đổi sức mang/tiền tay–két/máy tự động về luật cũ.
Khóa baseline bằng hash và regression hành vi trước khi thay presentation/layout.

## 2. Những gì audit đã xác nhận

| Phát hiện trên main | Bằng chứng | Hướng xử lý |
|---|---|---|
| Checkout gốc cũ 4 commit, 1.265 mục dirty/untracked | git fetch/rev-parse/status ngày 29/10 | Làm trong worktree sạch, không merge vào checkout gốc |
| Map zoom chỉ 0,65–3,5, center chưa giới hạn world bounds tại state | MapViewportState.cs | Một state có clamp, zoom 0,5–6, pan/rotate theo cùng phép biến đổi |
| Teleport expansion bỏ qua một số kiểm NavMesh/capsule | CityMapLocations.Teleport | Mọi POI dùng cùng gate access; khóa/unlock phải nằm trong dữ liệu |
| Cây phố/rừng còn dựng Crown/Cone/Cylinder | CityVegetation.cs; CityOuterDistricts.cs | Thay bằng mesh cây thật có material/LOD và kiểm sự hiện diện trong Player |
| Ba GLB mang metadata ez-tree chỉ có 40 đỉnh, 48 triangle/file | Đọc GLB accessors: 16+24 đỉnh, (48+96)/3 triangle | Không dùng nhãn generator/hash như bằng chứng chất lượng hoặc integration |
| TargetDensity/MinimumSpacing chưa được runtime dùng ngoài khai báo | rg trên Runtime; CityBiomeVegetation.Generate chỉ thấy trong tests | Nối biome profile vào placement thật, thống kê instance trong scene |
| Cutaway suy upper shell theo tên/material/độ cao 3,1m | WorldCutawayDirector.cs | Gán nhóm roof/front-wall/side-wall/interior rõ ràng trong prefab |
| HasInteriorCoverage chỉ cần >0 renderer | WorldCutawayVolume.cs | Gate lối vào, đi lại, hoạt động và camera, không chỉ renderer count |
| Tài liệu hiện hành còn trỏ handoff V21 cũ | README.md / Docs/CODEX_HANDOFF.md | Một chỉ mục release duy nhất với commit/hash/build thực tế |

11 ảnh ngày 19/10 là bằng chứng lỗi lịch sử. Người dùng xác nhận đã chơi bản 23/10 và vẫn thấy lỗi.
Các lỗi đường/nhà/nước cụ thể cần được đánh dấu lại từ bản chạy mới, không suy ra đã sửa từ test pass.
Xem BASELINE_AUDIT.md cho kết quả build/Player lần này.

## 3. Thiết kế trải nghiệm

### Thị trấn có luồng đi rõ

Cụm Farm giữ gameplay nhưng được dựng lại mặt bằng nhìn thấy: luống, nước, hàng thu, quầy và kho
có đường thao tác riêng. Phố cửa hàng là điểm nối ra quảng trường. Processing và bãi hậu cần nằm
phía sau phố, xe giao hàng không xuyên hàng khách. Siêu thị/Bakery/Restaurant tạo vòng kinh doanh
quay về Farm. Khu dân cư nối phố bằng đường ngắn; bến cảng liên hệ hậu cần; rừng và bờ biển là
vành đai có đường đi thật, không trải prop thưa trên một nền cỏ khổng lồ.

Giữ stable ID nghiệp vụ; vị trí presentation được đổi qua catalog có revision. Không giữ địa hình
xấu chỉ để bảo toàn tọa độ cũ. Dùng thời gian đi thực tế để chốt khoảng cách: L = vận tốc × thời gian;
đo ở trạng thái tay trống và mang hàng. Không tăng kích thước bản đồ trước khi có mật độ nội dung đủ.

### Công trình và hoạt động (đề xuất cụ thể)

| Nhóm | Hoạt động và nội thất |
|---|---|
| Farm, kho, Farm Shop | Trồng/chăm/thu, tập kết, xếp quầy, bán và thu tiền theo logic đang có |
| Xưởng Processing | Nhận nguyên liệu, đọc trạng thái máy, lấy sản phẩm, giao tuyến hậu cần |
| Siêu thị | Quầy/kệ phân hàng, kho sau, nhân viên và hàng chờ có lối riêng |
| Bakery + Restaurant | Làm bánh, cung ứng món, phục vụ/dọn bàn; interior thấy được đường thao tác |
| Nhà dân | Người nhận đơn đặt nông sản; giao hàng tại bàn/cửa trong nhà, thanh toán một lần |
| Café/nhà nghỉ | Đơn cung ứng sản phẩm Farm/Bakery, nhận hàng và phản hồi nhu cầu |
| Kho cảng/nhà ga | Giao–nhận lô hàng và điểm vận tải, sử dụng ownership/reservation hiện có |
| Ngân hàng/văn phòng quản lý | Dịch vụ tiền/két và xem vận hành; không tự thêm vay/lãi làm đổi Farm |
| Nhà bảo vệ/xưởng dịch vụ | Bảng việc hỗ trợ tuyến kinh doanh, trạng thái hoạt động rõ |

Đây là danh mục thiết kế, chưa phải hệ thống đã viết. Nhà dân không đồng nghĩa một game sinh tồn
hoặc mô phỏng đời sống mới. Các hoạt động bổ sung phục vụ chuỗi kinh doanh, không tự thêm combat,
multiplayer, lịch mùa vụ bắt buộc hay tiền tệ thứ hai.

Mỗi nhà đặt trong vùng chơi phải có ID, loại, footprint, cửa vào/ra, loại hoạt động, nội thất và lý do
nếu không vào được. Gate: 100% công trình gameplay/named service vào được; số nhà vào được phải
lớn hơn một nửa tổng nhà đặt trong vùng chơi. Báo cáo tử số/mẫu số thực tế, không tính mái che/prop
hoặc số prefab khác nhau để thổi tỷ lệ. Các façade ngoài vùng chơi được ghi riêng.

### Hình ảnh

Gỗ, vữa, gạch ngói và đá lát có scale texture thống nhất; roughness/normal vừa phải; giảm bề mặt
nhiễu hạt và màu phẳng. Cây có cành/tán tự nhiên, tầng bụi/cỏ/rừng, LOD; nước có depth/shore fade,
normal đa hướng và foam nhẹ, không mép slab. Nội thất đủ ánh sáng, quầy và đồ vật đúng tỷ lệ nhân vật.
Ảnh coastal-town-concept.png là định hướng mỹ thuật bằng ImageGen, không phải ảnh game đã build;
nhân vật trong ảnh minh họa không thay model/rig thực tế được giữ nguyên.

HUD ưu tiên vùng nhìn của người chơi: tiền/hàng/mục tiêu gọn, chỉ hiện hành động liên quan khi tới
trạm; thông báo không phủ nhân vật. Bản đồ dùng ảnh chụp trực tiếp thế giới 3D từ trên xuống, chia ô chính xác theo world bounds; icon thống nhất, label tránh
chồng, tooltip, filter, focus player/north reset; minimap cắt đúng viewport ở mọi zoom.

## 4. Thứ tự triển khai và điều kiện chuyển giai đoạn

| Giai đoạn | Đầu ra | Gate trước khi chuyển tiếp |
|---|---|---|
| P0: baseline thật | Source/save manifest, build main mới, ảnh đối chiếu, danh sách lỗi tái hiện | Hash/commit rõ; không ghi đè checkout/save cũ |
| P1: khóa Farm + quy ước dữ liệu | Farm regression, world/building/activity/asset contract, blockout | Farm không đổi quy tắc; cửa, đường, hoạt động có owner rõ |
| P2: Farm + phố mẫu hoàn chỉnh | Farm, Farm Shop, một Processing, một nhà dân, đoạn đường và nội thất thật | Chơi từ 0 tới bán/thu/nâng đầu; đi vào/ra; cutaway; save–đóng–mở |
| P3: nền thành phố | Road graph/junction/sidewalk/terrain, access anchors, NavMesh | Không gap/overflow/chồng sai; mesh–collider khớp; mọi entry tới được |
| P4: toàn chuỗi và nhà dân | Các khu kinh doanh, nội thất và hoạt động; registry đầy đủ | Mọi activity dùng giao dịch; tỷ lệ enterable đạt; không phòng rỗng giả |
| P5: nghệ thuật và môi trường | Asset thật, cây/rừng/nước, vật liệu, ánh sáng, âm thanh phù hợp | Prefab thực dùng đúng nguồn; review gần/xa trong Player; LOD không pop lỗi |
| P6: NPC và vận tải | Queue, walking lanes, giao hàng, boarding/alighting | Không xuyên nhà, wrong approach hoặc kẹt kéo dài; đối soát hàng |
| P7: map/HUD/onboarding | Map tự do, UI gọn, mục tiêu và feedback rõ | Chuột/gamepad, filter/tooltip/travel, safe area, localized labels |
| P8: bản cho người dùng test | Windows folder/ZIP, hướng dẫn, evidence, source commit trên main | Build/test/runtime/persistence/visual/performance riêng đều đạt |

P2 là cửa kiểm thiết kế và cách chơi, không chỉ compile. Nếu mẫu chưa đẹp/chưa dễ chơi thì sửa mẫu
trước khi nhân ra toàn thành phố. Các giai đoạn sau có thể chuẩn bị dữ liệu độc lập, nhưng không
được ghi pass bằng việc đếm class, field, manifest hoặc ảnh không đen.

## 5. Nghiệm thu

- Hình học: gap endpoint ≤0,05m; mesh/collider lệch ≤0,02m; không NaN/Infinity/triangle suy biến;
  không nền/đường/vỉa hè chồng sai; chân model chạm mặt đỡ, không chỉ transform.y = 0.
- Nhà: từ đường đi qua cửa, tới activity, thao tác, ra khỏi nhà bằng input; thử cả mang hàng.
  Camera không đổi pitch/yaw; roof/wall cutaway đúng nhà, renderer khôi phục đúng, collider vẫn còn.
- Farm: khóa luật hiện tại bằng snapshot/regression; không phục hồi luật 6→10→16→24 hoặc operator
  cũ trong PLAN.md. Dwell 0,25s; giữ các quy tắc tay hàng/tiền và progression hiện hành.
- Ngoài Farm: đơn giao dân/café/logistics có state, command id, receipt, terminal state;
  giao lặp/load lại không sinh hàng/tiền. Thiếu hàng/sai hàng/đích đầy báo rõ.
- Map: 0,5–6; clamp pan; finite state; vị trí world↔map round-trip; icon/label không chồng
  ở cấp zoom mục tiêu; không teleport tới locked/blocked POI hay bỏ qua collider ở expansion.
- Visual: ảnh mới cùng bookmark ở Farm, junction, nhà dân, Processing, phố, rừng, bờ biển,
  full map/minimap; clip đi bộ và vào/ra để phát hiện pop/flicker, không chỉ ảnh tĩnh.
- Persistence: save, thoát process, mở process mới; hàng, tiền, progression, activity, vị trí
  trong/ngoài nhà đúng; profile cũ giữ nguyên hash. Không dùng save fixture thay bằng chứng này.
- Performance: native 1920×1080, 1x, 30 khách, worker/traffic/water/autosave hoạt động, không mod
  hoặc overlay; đo 60s: average ≥60 FPS, 1% low ≥55. Đây là mục tiêu dự án, không phải quy định Steam.
- Build: 0 error, focused tests pass, 0 runtime exception trong tuyến nghiệm thu; warning ghi rõ.
  Chỉ chạy benchmark cuối hoặc khi có vấn đề hiệu năng; không chạy soak dài tự động.
- Campaign 180–240 phút chưa được kiểm chứng từ lịch sử. Không ghi đạt bằng fixture gần mở khóa;
  chỉ cân bằng ngoài Farm sau khi có dữ liệu chơi thực, không thay Farm ngầm.

## 6. Bàn giao và Git

Một thư mục Windows độc lập, exe + Data + DLL; manifest có commit, hashes của runtime/content,
không chỉ hash Unity launcher TYCOON2.exe vốn có thể giống nhau giữa nhiều bản build.
Có file hướng dẫn profile/controls/test route và known issues. Build main cũ để đối chiếu phải
được đặt tên baseline, không được gọi là redesign mới. Chỉ stage source/asset/license/evidence
thuộc lần này. Fetch main trước push, không force; nếu main đổi phải tích hợp và kiểm lại phần ảnh hưởng.

Steam cần review store/build và hồ sơ nội dung riêng. Khi ImageGen asset thực sự được đóng gói,
ghi vào danh mục AI content để dùng cho Content Survey. Bản concept tham khảo không được đưa vào
Player làm cảnh nền hoặc dùng quảng cáo như screenshot game thật.

## 7. Tài liệu đi kèm

- IMPLEMENTATION_CONTRACT.md: module, interface, dữ liệu và ownership dự kiến.
- ASSET_SOURCES.md: nguồn/tác giả/license đã tra và pipeline nhập thật.
- BASELINE_AUDIT.md: bằng chứng lần kiểm tra này, giới hạn, lệnh tái hiện.
- concept-prompt.md / coastal-town-concept.png: prompt và mẫu mỹ thuật bằng công cụ tích hợp.

## Bổ sung đã chốt: map ảnh chụp thực tế

Theo yêu cầu mới ngày 29/10, nền map/minimap chụp từng ô bằng camera orthographic riêng. Mỗi ô có world bounds và UV xác định; pan/zoom/rotate, icon và player dùng chung phép biến đổi. Ảnh được cập nhật từ world đang chơi, không dùng hình sinh bởi AI hoặc ảnh ghép ước lượng. Xem MAP_CAPTURE_CONTRACT.md. Người dùng cho phép phân công worker và tái giao việc khi worker xong; công cụ worker chưa được cấp trong phiên hiện tại.


## Cập nhật triển khai 31/10/2026

Đã hoàn thành phần map ảnh chụp và sửa địa hình biển/sông theo lựa chọn dời bãi công nghiệp vào đất
liền, bến phía Đông sát bờ. Kèm sửa ghi save và profile coastal-redesign-v2. Source 4b74d0f;
1219 tests, 85 hydrology checks, 6096 world checks và 9 reload checks pass.
Xem [DELIVERY.md](hydrology-20261031/DELIVERY.md). Các phase thay toàn bộ asset, nội thất và hoạt động
thành phố vẫn là kế hoạch, chưa được nghiệm thu hoàn tất.
