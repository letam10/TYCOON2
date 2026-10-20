Bạn kéo main mới nhất rồi Triển khai kế hoạch tuần tự, commit/push github đầy đủ mỗi giai đoạn, ghi chú handoff và báo cáo file đầy đủ, sửa chữa, cải thiện và thiết kế lại trò chơi, tôi muốn bạn làm rõ và hỏi những chỗ nào không hiểu, không biết để chính xác và chuẩn chỉnh:
"
Tôi đã chốt kế hoạch theo các lựa chọn của bạn:
- Mỹ thuật: bán thực tế stylized, PBR rõ.
- Công trình chính: có nội thất thật và cutaway.
- Nhà nền: cutaway nhẹ.
- Camera game: giữ pitch/yaw hiện tại, chỉ thu phóng.
- Bản đồ: zoom, pan, rotate, lọc và tooltip.
- Economy/save: giữ nguyên stable ID và save contract.
- Asset: cho phép mở rộng repo GitHub, kèm kiểm tra license.
- Mục tiêu: Steam PC, 1080p–1440p, 60 FPS trong gameplay thông thường.
Tôi đã xác nhận main tại máy trùng origin/main ở commit 7f32f78, nên không có commit mới hơn để kéo. Worktree đang có nhiều thay đổi cục bộ và asset chưa theo dõi; mọi thay đổi sau này phải bảo toàn chúng.
Kiến trúc hiện tại và nguyên nhân chính
- CityStreetNetwork, CityAccessPaths và WorldRouteCatalog đang tạo hình học đường, lối đi và route theo các cách khác nhau.
- Mesh đường được xoay theo hướng đoạn đường nhưng collider vẫn là hộp trục thẳng, gây lệch giữa hình ảnh và va chạm.
- Đường cong dùng nội suy có thể vượt anchor, tạo đoạn đường vượt thiết kế.
- Vỉa hè và mặt đường dựng từ các khối chữ nhật rời, chưa có nút giao, bo góc và cắt mép chung.
- Nhà phần lớn là khối đặc; cutaway hiện chỉ ẩn một phần mái/vỏ.
- Cây vẫn có nhiều phần dựng procedural low-poly; ez-tree đã được ghim commit và có output nhưng cần mở rộng sử dụng theo biome.
- Bản đồ dùng icon chữ cái, layout cố định và chưa có viewport tương tác.
- Các ô tương tác hiện diện nhiều trong không gian; logic 0,25 giây nên được giữ trong vùng logic ẩn, không biểu diễn bằng các ô đứng vô dụng.
- Test hiện có kiểm tra route, asset, cutaway và contract; chưa đủ để xác nhận toàn bộ hình học, camera, blocking, visual và trải nghiệm Steam.
Kế hoạch triển khai
Giai đoạn 0 — Đóng băng chuẩn kiến trúc và tạo baseline
Tạo một thư mục làm việc riêng trong work/ để lưu report, ảnh kiểm tra và manifest. Không reset, clean, stash hoặc ghi đè các thay đổi hiện có.
Chốt các contract dùng chung:
- WorldRouteCatalog là nguồn duy nhất cho node, width, loại route và điểm nối.
- Tất cả mesh đường, vỉa hè, collider, NavMesh, pedestrian route, traffic route và bản đồ lấy dữ liệu từ cùng polyline đã lấy mẫu.
- Mọi POI giữ nguyên stable ID.
- Tách rõ visual mesh, walkable surface, interaction trigger và blocking collider.
- Ghi lại ground height, bounds, scale, pivot và license cho từng asset mới.
Baseline gồm:
- ảnh các khu vực hiện tại;
- log console;
- route/POI manifest;
- collider và NavMesh debug view;
- build và test hiện tại làm mốc so sánh.
Giai đoạn 1 — Sửa toàn bộ hình học đường và vỉa hè
Thay cách dựng đường rời bằng bộ sinh hình học dựa trên polyline chung.
Bộ sinh mới phải:
- tạo segment theo đúng hướng và chiều rộng;
- tạo mối nối liên tục giữa các segment;
- bo góc với bán kính giới hạn theo chiều dài đoạn;
- cắt đầu/cuối vỉa hè theo nút giao;
- loại bỏ phần hình học chồng lên nhau;
- tạo junction patch hoặc round corner tại giao lộ;
- giữ chiều cao mặt đường, vỉa hè và mặt đất nhất quán;
- không cho spline vượt khỏi vùng anchor.
Collider phải nhận cùng vị trí, rotation và kích thước với mesh. Bổ sung overload collider có rotation rồi chuyển dần các đường, cầu, ramp và lối đi sang API này.
Kiểm tra tự động:
- endpoint liên tục;
- không có segment zero-length;
- không có self-intersection ngoài giao lộ hợp lệ;
- vỉa hè không vượt ra khỏi mép đường;
- collider bao đúng hình học;
- đường ray, đường cao tốc và bến cảng không bị cắt sai;
- không còn triangle spike, seam hoặc mặt phẳng nổi.
Giai đoạn 2 — Đồng bộ NavMesh, pedestrian và traffic
NavMesh được xây từ mặt đi bộ sau khi hình học đường đã ổn định.
Cần sửa:
- vùng đi bộ liên tục từ POI đến POI;
- cửa vào, cầu thang, ramp và bến tàu có link hợp lệ;
- loại bỏ vùng NavMesh xuyên qua nhà, đường xe hoặc mặt nước;
- pedestrian route bám đúng corridor;
- điểm bắt đầu/kết thúc nằm trong vùng đi được;
- traffic route dùng đúng hình học đường thay vì một path riêng;
- người đi bộ dừng đúng tín hiệu giao thông và không xuyên vật thể.
Các ô tương tác hiển thị trong ảnh sẽ được bỏ khỏi visual runtime. Mỗi đối tượng chỉ còn một vùng tương tác logic có kích thước phù hợp, debug visualization chỉ bật trong QA. Quy tắc đứng gần 0,25 giây được giữ lại cho mục tiêu hợp lệ và không bị reset liên tục bởi sai số collider.
Giai đoạn 3 — Công trình, nội thất và camera cutaway
Phân loại công trình thành hai nhóm:
- Công trình gameplay chính: nông trại, kho, cửa hàng, ngân hàng, nhà hàng, ga, bến cảng, khu thương mại và các POI có hành động riêng.
- Công trình nền: nhà dân, khối phố và façade trang trí.
Công trình gameplay chính sẽ có:
- sàn và không gian nội thất;
- cửa vào/ra rõ ràng;
- collider tường chia theo phòng;
- điểm vào, điểm thoát và điểm tương tác;
- ánh sáng, vật liệu và props tối thiểu;
- cutaway theo tầng hoặc theo shell;
- khôi phục vỏ chính xác khi người chơi rời công trình.
Nhà nền dùng cutaway nhẹ:
- ẩn mái và lớp tường che camera;
- giữ collision perimeter để người chơi không đi xuyên khối;
- không yêu cầu nội thất đầy đủ;
- không để camera chui vào mái hoặc tường.
Camera giữ nguyên pitch/yaw hiện tại. Chỉ bổ sung:
- giới hạn thu phóng hợp lý;
- chống clipping với mặt đất và công trình;
- ưu tiên nhìn thấy player khi bị che;
- fade hoặc ẩn các mặt cản giữa camera và player;
- framing lại khi vào công trình mà không xoay góc nhìn.
Giai đoạn 4 — Thiết kế lại asset và môi trường
Phong cách chung là stylized bán thực tế với PBR rõ, giữ tính dễ đọc của game tycoon.
Ưu tiên asset hero:
- cây và thảm thực vật;
- mặt nước, bờ biển và bến cảng;
- nhà ga, ngân hàng, nhà hàng, khu thương mại;
- xe tải, ô tô và tàu;
- biển báo, đèn đường, props tương tác;
- các khu vực người chơi thường nhìn gần.
Asset GitHub mới chỉ được nhập khi:
- license rõ ràng;
- có URL và commit/version;
- có quy trình import reproducible;
- có kiểm tra scale, pivot, material, collider và LOD;
- không đưa nguyên cả thư mục ASSET/Downloaded vào runtime.
Cây sẽ dùng catalog biome có seed ổn định:
- rừng: cây lớn, cây non, bụi, cỏ, đá, thân cây đổ;
- nông trại: hàng cây, hàng rào, bụi thấp;
- ven biển: cây chịu mặn, cỏ ven nước, vật liệu cát và đá;
- giữ clearance khỏi đường, công trình và route;
- dùng LOD và giới hạn collider để bảo đảm hiệu năng.
Codex ImageGen dùng cho:
- albedo;
- roughness/metallic mask;
- normal detail;
- decal biển báo;
- vật liệu đất, gỗ, bê tông, nước và bờ;
- texture variation cho props.
Mỗi texture phải có kích thước, compression, mipmap, filter và license/output record rõ ràng. ImageGen không thay thế việc kiểm tra scale, silhouette, collider và độ hợp lý của model.
Mặt nước sẽ được làm lại theo hướng stylized tự nhiên:
- shoreline có transition;
- vùng nước nông/sâu;
- phản xạ và caustic vừa phải;
- bờ không bị cắt vuông;
- bến tàu, thuyền và vật thể nổi bám đúng cao độ.
Giai đoạn 5 — Thiết kế lại bản đồ và HUD
Bản đồ lớn có MapViewportState gồm:
- tâm bản đồ;
- zoom;
- pan;
- rotation;
- POI đang chọn;
- bộ lọc danh mục.
Điều khiển:
- wheel hoặc nút để zoom;
- kéo chuột để pan;
- giữ phím hoặc nút chuột để xoay;
- nút reset về Bắc;
- nút đưa bản đồ về player;
- hỗ trợ gamepad cơ bản.
Icon chữ cái được thay bằng icon theo loại:
- nông trại;
- giao thông;
- dân cư;
- thương mại;
- công cộng;
- rừng;
- biển/cảng;
- công nghiệp;
- đường sắt.
Bổ sung:
- tooltip tên, loại và trạng thái;
- bộ lọc danh mục;
- legend;
- highlight route đến POI;
- trạng thái đã mở khóa/chưa mở khóa;
- nhãn chống chồng lấn;
- minimap gọn, dễ đọc và không che HUD.
HUD chính được sắp xếp lại theo vùng an toàn:
- nhóm tiền và trạng thái;
- nhóm menu;
- thông báo tương tác;
- hướng dẫn phím;
- minimap;
- thông báo lưu game.
Các panel không được che nhau ở 1080p và 1440p.
Giai đoạn 6 — Giữ ổn định logic, economy và save
Stable ID, economy, tiến trình và save contract hiện tại được giữ nguyên.
Chỉ bổ sung:
- migration additive nếu cần cho nội thất, map state hoặc asset version;
- validation trước khi save;
- khôi phục đúng vị trí player, hàng hóa, tiền và trạng thái POI;
- không để thay đổi visual phá save cũ;
- test load/save trước và sau khi thêm interior/cutaway.
Các logic tự động hiện tại được giữ, nhưng prompt và vùng tương tác sẽ được làm rõ để người chơi hiểu hành động đang khả dụng, bị khóa hay đang chờ điều kiện.
Giai đoạn 7 — Kiểm thử và tiêu chuẩn Steam
Mỗi tranche phải có bốn lớp kiểm tra:
1. EditMode contract: route, hình học, collider transform, POI, map transform, cutaway, vegetation profile, save compatibility.
2. Headless/build: compile, build Windows, không có exception và không có missing asset.
3. Runtime visual: đi qua từng khu, vào/ra công trình, dùng bản đồ, kiểm tra traffic/pedestrian, camera và water.
4. Hiệu năng: kiểm tra ngắn ở 1080p và 1440p, gameplay thường, khu rừng, khu đô thị, bến cảng và khu đông vật thể.
Tiêu chí đạt:
- không có đường/vỉa hè bị đứt hoặc vượt thiết kế;
- không có model nổi khỏi mặt đất;
- không có collider chặn player sai;
- người đi bộ đi đúng corridor;
- player vào được toàn bộ công trình gameplay chính;
- camera vẫn giữ góc cố định và luôn đọc được player;
- bản đồ zoom/pan/rotate và lọc hoạt động;
- không có lỗi console runtime;
- save cũ vẫn tải được;
- đạt mục tiêu khoảng 60 FPS trong gameplay thông thường trên Steam PC.
"

Referenced image files:
- [Image #1]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-1.png
- [Image #2]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-2.png
- [Image #3]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-3.png
- [Image #4]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-4.png
- [Image #5]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-5.png
- [Image #6]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-6.png
- [Image #7]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-7.png
- [Image #8]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-8.png
- [Image #9]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-9.png
- [Image #10]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-10.png
- [Image #11]: C:\Users\TAM\Documents\Codex\Codex-Desktop-Profiles\ChatGPT\codex-home\attachments\977c990b-dba2-432f-aedb-93abb62fe160\image-11.png
