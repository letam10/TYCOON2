
PLEASE IMPLEMENT THIS PLAN: 
# TYCOON2 — Kế hoạch thiết kế lại gameplay và triển khai 
 
## 1. Định hướng và tiến trình chơi 
 
`D:\GAME\TYCOON2`, giữ Unity **6000.6.3f1**, URP **17.6.0**, Windows chơi đơn offline, tiếng Việt. Các quyết định trong cuộc trao đổi này là chuẩn gameplay mới. Những kết quả QA cũ chỉ chứng minh một phần kỹ thuật của prototype. 
 
**Vòng chơi chính:** sản xuất thủ công → mang hàng phục vụ → thu tiền tại quầy → nâng cấp trạm → thuê nhân viên → hỗ trợ các điểm nghẽn → mở rộng sản xuất và kinh doanh. 
 
### Khởi đầu và lựa chọn phát triển 
 
- Bắt đầu với **0 tiền**, ba ô cà rốt nhỏ, một quầy miễn phí và chỗ đặt hàng cơ bản. 
- Gieo và tưới cây ban đầu không tốn tiền. Tuyến cà rốt → quầy luôn hoạt động để người chơi phục hồi từ 0 tiền. 
- Cà rốt bắt đầu bán **10 xu/đơn vị**; nâng cấp chất lượng làm tăng giá bán, tách rõ với nâng tốc độ và dung lượng. 
- Người chơi được lựa chọn nâng cây trồng hoặc dành tiền mở chăn nuôi. 
- **Khoảng 500 mua trọn tuyến thịt cơ bản; khoảng 1000 mua trọn tuyến sữa hoặc trứng cơ bản.** Gói gồm chuồng, đàn ban đầu, trạm và quầy cần thiết; nguồn cây làm thức ăn phải tiếp cận được ngay. 
- Tuyến bò/gà có vị trí mua riêng, nội dung gói được hiển thị trước khi góp tiền. Trạm sơ chế ban đầu nằm trong Farm; khu Processing mở sau là bước mở rộng quy mô. 
  
### Nhịp tiến triển 
 
Các giá dưới đây là **giá khởi điểm cho bước cân bằng**. Mục tiêu nghiệm thu là mở đủ năm khu trong **180–240 phút chơi bình thường**, thông qua hoạt động thực tế. 
 
| Giai đoạn | Nội dung và điều kiện chính | Giá mở rộng ban đầu | Thời điểm mục tiêu | 
|---|---|---:|---:| 
| Farm | Cây trồng, quầy đầu tiên, lựa chọn chăn nuôi | Khởi đầu miễn phí; các tuyến 500/1000 | 0–20 phút | 
| Farm Shop | Mở cụm quầy chuyên sản phẩm; trạm khởi đầu cấp 3 và 50 đơn thành công | 2.000 | 20–45 phút | 
| Processing | Farm Shop hoạt động; một tuyến chăn nuôi cấp 3 và 200 đơn thành công | 12.000 | 60–95 phút | 
| Supermarket | Ba công thức sơ chế đã vận hành; mỗi công thức hoàn thành 50 mẻ và tổng 600 đơn thành công | 65.000 | 120–165 phút | 
| Bakery + Restaurant | Supermarket cấp 3, tổng 1.000 đơn; Restaurant tiếp tục yêu cầu Bakery bán 60 đơn và lò cấp 3 | Bakery 100.000; Restaurant 80.000 | 180–240 phút | 
 
Chỉ hiện ô mở khu khi đạt điều kiện vận hành. Mục tiêu tiếp theo vẫn được giải thích trong HUD và bảng tiến trình. Hai hướng chơi ưu tiên cây trồng hoặc ưu tiên chăn nuôi đều phải đi tiếp được. 
 
## 2. Luật vận hành được chốt 
 
### Người chơi và tương tác 
 
- WASD theo hướng camera; camera phối cảnh, pitch 55°, follow có damping nhẹ. Giữ hỗ trợ gamepad hiện có. 
- Đứng đúng vùng sẽ tự thao tác; rời vùng thì dừng. Vùng lấy, đặt, vận hành, phục vụ, thu tiền và mua phải phân biệt rõ, không chồng nhau. 
- Mỗi lần chỉ mang **một loại hàng**, thể hiện bằng chồng 3D. Sức mang khởi điểm 6, các mốc nâng 10 → 16 → 24. 
- Đang mang sai loại hoặc đã đầy thì hành động liên quan dừng và hiện lý do. Không tự đổi loại hàng hay xóa hàng. 
- Người chơi làm được mọi công việc thiết yếu trước khi thuê nhân viên, gồm phục vụ và dọn bàn. 
  
### Cây trồng, vật nuôi và máy 
 
- Cây: **đất trống → gieo → tưới/chăm sóc → lớn → sẵn thu hoạch**. Ban đầu từng công đoạn mất khoảng 1–2 giây; cây lớn sau khi được chăm sóc, thu hoạch cần người thao tác. 
- Bò/gà cần thức ăn và chăm sóc. Chu kỳ tạo sữa/trứng chạy khi đủ điều kiện; có người chăm tại vị trí hợp lệ thì chạy nhanh **2 lần**, không cộng dồn nhiều người. 
- Sản phẩm sẵn thu và sản phẩm đã lấy ra là hai trạng thái riêng. Các công đoạn dùng máy vẫn cần người vận hành. 
- Làm thịt tiêu thụ số con thực tế. Tái đàn dùng nông sản và thời gian dài hơn chu kỳ sữa/trứng; vẫn tái đàn được khi số con bằng 0. 
- Máy: **chờ nguyên liệu → sẵn vận hành → đang chạy → hoàn tất/chờ lấy hàng**. Mỗi máy chỉ có một người vận hành tại một thời điểm. 
- Rời máy thì giữ nguyên tiến độ và dừng chạy. Người chơi và nhân viên có thể bàn giao vận hành; nguyên liệu không bị trừ lại. 
- Máy chỉ bắt đầu khi đủ nguyên liệu và đã dành được chỗ cho đầu ra. Máy nhiều nguyên liệu có dung lượng theo từng loại để một loại không lấp kín toàn bộ đầu vào. 
- Chuỗi chính: Wheat → Flour, Milk → Cheese, Tomato → Sauce; Flour/Milk/Egg → Bread/Cake; nguyên liệu Farm và Processing → món Restaurant. 
  
### Quầy, đơn hàng và tiền 
 
- Khách đi tới quầy, xếp hàng **FIFO theo từng quầy**, yêu cầu sản phẩm và số lượng. Đầu game quầy chuyên một loại; Supermarket và Bakery mở dần đơn kết hợp. 
- Đơn đầu game có 1–3 đơn vị. Đơn kết hợp ban đầu tối đa hai loại; chỉ yêu cầu sản phẩm thuộc tuyến đã mở và có khả năng sản xuất. 
- Player có thể giao trực tiếp từ chồng đang mang. Hàng trong khoang quầy được người chơi hoặc nhân viên bán hàng lấy ra phục vụ. 
- Chỉ khách đầu hàng được nhận hàng. Người chơi và nhân viên cùng giao phải dùng chung một giao dịch kiểm tra số còn thiếu, tránh giao trùng. 
- **Kiên nhẫn mặc định 90 giây cho mọi khách**, bắt đầu khi vào hàng chờ. Giao một phần không đặt lại thời gian; nâng cấp và sự kiện không thay đổi giới hạn này. 
- **Khách hết kiên nhẫn giữ phần hàng đã nhận và trả 0 tiền.** Hàng chưa giao trong khoang quầy vẫn thuộc cửa hàng; phần bị mất được ghi là thất thoát. 
- Giao đủ trước hạn mới tạo một khoản thanh toán. Giá đơn được chốt lúc nhận đơn, không thay giữa chừng khi nâng cấp. 
- Tiền thành cọc tại quầy. **Ví chỉ tăng khi người chơi đứng vào vùng thu tiền.** Nhân viên bán hàng không tự chuyển tiền vào ví. 
- Hiển thị riêng tiền trong ví, tiền chưa thu, doanh thu đã thanh toán và hàng thất thoát. 
  
### Kho và vận chuyển 
 
- Mỗi khu có kho/bãi hàng riêng; tồn kho được ghi theo khu và loại hàng. 
- Tuyến vận chuyển được thiết kế sẵn, gồm nguồn, đích và loại hàng. Ban đầu mang tay; sau mở nhân viên và băng chuyền cho tuyến cố định. 
- Người vận chuyển giữ chỗ đầu nhận trước khi lấy hàng. Băng chuyền ngừng đưa thêm khi đầu nhận đầy; hàng trên đường vẫn được quản lý và lưu lại. 
- Nguồn nguyên liệu dùng chung có mức dự trữ cho sản xuất/thức ăn; tuyến bán lấy phần vượt mức này. Bảng quản lý hiển thị rõ số hàng đang được dành cho công việc. 
  
### Nhân viên và nâng cấp 
 
| Nghề | Công việc cố định | 
|---|---| 
| Nông dân | Gieo, tưới, thu hoạch, đưa về điểm tập kết Farm | 
| Chăm vật nuôi | Cho ăn, chăm sóc, thu sữa/trứng, vận hành phần việc chăn nuôi được giao | 
| Vận chuyển | Chuyển hàng trên tuyến giữa các khu | 
| Xếp hàng | Đưa hàng từ kho khu tới khoang quầy/đầu vào được chỉ định | 
| Bán hàng | Lấy hàng trong khoang, phục vụ khách đầu hàng, nhận thanh toán tại quầy | 
| Chế biến | Vận hành trạm Processing được giao | 
| Đầu bếp/thợ bánh | Vận hành bếp và lò theo công thức/đơn hàng | 
| Phục vụ | Mang món đúng bàn và dọn bàn | 
 
- Đội được định danh bằng **nghề + khu**. Nâng cấp Farm Shop không tự nâng nhân viên Supermarket. 
- Ô thuê đầu tiên chỉ xuất hiện sau khi trạm liên quan đạt cấp 3 và hoàn thành 30 lượt công việc tương ứng. Toàn bộ điều kiện này phải làm được bằng người chơi. 
- Thuê lần đầu bằng ô góp tiền; sau đó ô biến mất và mở mục quản lý đội. 
- Menu đội có tốc độ, sức mang và thêm người. Chỉ hiển thị nâng cấp phù hợp nghề; số người không vượt các vị trí công việc đã mở. 
- Nhân viên giữ nghề và phạm vi được định sẵn; không có lương định kỳ. 
- Thiếu nguyên liệu, đầy đích hoặc máy hỏng: nhân viên chờ tại điểm bên cạnh trạm và báo đúng nguyên nhân. Khi điều kiện được giải quyết, họ tiếp tục việc đang chờ. 
  
### Ô mua và Restaurant 
 
- Ô công trình/nâng cấp/thuê đầu tiên nhận tiền từng phần. Rời ô hoặc hết tiền thì dừng; khoản đã góp được giữ qua save/load. 
- Hoàn tất khoản góp chỉ mở khóa một lần. Ô mua nằm ngoài vị trí công trình sẽ xuất hiện để tránh nhốt người chơi. 
- Nút nâng nhân viên trong menu mua một lần khi đủ tiền, hiển thị rõ giá và thay đổi trước khi bấm. 
- Restaurant: **giữ bàn → khách ngồi/gọi món → chuẩn bị nguyên liệu → người vận hành nấu → phục vụ đúng bàn → ăn → thanh toán → bàn bẩn → người dọn → bàn trống**. 
- Chờ món áp dụng giới hạn kiên nhẫn chung; khi đã nhận đủ và bắt đầu ăn thì ngừng đếm kiên nhẫn. Tiền sau bữa ăn tập kết tại điểm thu của Restaurant. 
- Bàn chỉ được tái sử dụng sau khi dọn xong. Người chơi làm được cả nấu, phục vụ và dọn trước khi có nhân viên. 
  
### Sự kiện 
 
- **Giờ cao điểm:** tăng tốc độ khách đến trong 90 giây, báo trước 15 giây; tổng khách hoạt động tối đa 30. Bắt đầu xuất hiện sau khi đã mở đội nhân viên đầu tiên. 
- **Máy hỏng:** hao mòn theo số mẻ, có cảnh báo trước; tối đa một sự cố máy tại một thời điểm. Máy đang chạy giữ nguyên nguyên liệu và tiến độ. 
- Người chơi đứng sửa; mặc định 8 giây, phí nhỏ 10–100 xu tùy trạm. Tiền sửa chỉ thu một lần; rời đi giữ tiến độ sửa. 
- Tuyến cà rốt khởi đầu không bị sự cố khóa hoàn toàn. Khách bỏ đi, mất hàng đã giao và gián đoạn sản xuất là hậu quả thực tế của việc xử lý chậm. 
  
## 3. Dữ liệu, AI và save/load 
 
### Thay đổi trong code 
 
Giữ phần nền tảng inventory, stable ID, Input System, pooling và công cụ CLI còn phù hợp. Thay các luồng tự mua hàng, kho chung, máy tự chạy và mua tức thì bằng luật mới. 
 
Các giao diện dữ liệu cần bổ sung: 
 
- `ItemDefinition`, `RecipeDefinition`, `UpgradeDefinition`: dữ liệu cấu hình riêng với trạng thái đang chơi; chứa cấp, giá, công thức và điều kiện mở khóa. 
- `OrderState`: ID đơn/khách/quầy, hàng yêu cầu, hàng đã giao, giá đã chốt, kiên nhẫn còn lại và trạng thái kết thúc. 
- `PurchaseProgress`: ID ô mua, tổng giá, tiền đã góp và trạng thái hoàn tất. 
- `CrewState`: nghề, khu, số người, cấp tốc độ và sức mang. 
- Trạng thái trạm và công việc: nguyên liệu đang dùng, chỗ đã giữ, người vận hành, tiến độ, đầu ra, tình trạng hỏng/sửa. 
  
Mỗi trạng thái chỉ có một nơi chịu trách nhiệm cập nhật. Chuyển hàng, giao hàng, thanh toán, thu tiền và hoàn tất mua đều phải chống thực hiện hai lần. 
 
### Di chuyển và tránh kẹt 
 
- Bố trí luồng khách phía trước quầy, hậu cần phía sau; điểm thao tác, xếp hàng và chờ thiếu hàng là các vị trí riêng. 
- Kiểm tra đường tới mọi ô mua và trạm ở từng trạng thái mở khóa. Lối giao nhau đủ cho hai chiều di chuyển. 
- Agent điều khiển vị trí NPC; animation nhận tốc độ thực tế để blend. Tránh nhiều hệ thống cùng kéo transform. 
- Khi bị cản, NPC tính lại đường có giới hạn và nhường vị trí; nếu không tới được thì báo trạng thái chặn đường. QA phải phát hiện kẹt kéo dài. 
- AI quyết định theo nhịp cố định; chuyển động và animation cập nhật mượt theo từng frame. 
  
### Save v2 
 
Lưu đúng vị trí sở hữu của hàng và tiền: trên người chơi/nhân viên/khách, trong từng kho/quầy/máy, trên băng chuyền, tiền từng quầy và khoản góp từng ô. 
 
Lưu thêm thứ tự hàng chờ, đơn giao dở, kiên nhẫn, giá đơn, bàn ăn, trạng thái dọn, đàn vật nuôi, chu kỳ, mẻ đang chạy, công việc nhân viên, sự kiện và sửa chữa. 
 
- Khách đã nhận hàng không bị hoàn hàng về kho khi load. 
- Đơn đã trả tiền hoặc đã thất bại không được giải quyết lần nữa. 
- Đóng game thì thời gian mô phỏng dừng; mở lại tiếp tục thời gian còn lại. 
- Khôi phục và kiểm tra trạng thái trước khi cho simulation/input chạy. 
- Dùng save v2 riêng; giữ nguyên save prototype hiện có. Không tự tạo bản sao lưu. 
  
## 4. Bố cục, đồ họa, animation và giao diện 
 
### Bố cục và tài sản 
 
- Thiết kế cụm công việc gần nhau theo tuyến sản xuất → tập kết → quầy. Mỗi khu có cổng khách và đường hậu cần rõ ràng. 
- Video là chuẩn đối chiếu camera, tỷ lệ nhân vật, mật độ đạo cụ và nhịp chuyển động; không dùng chi tiết trang trí để che lối đi hoặc điểm tương tác. 
- Phân biệt rõ trạng thái khóa, có thể mua, đang góp và đã mở. Giá 500/1000 là giá mua trọn tuyến cơ bản tương ứng, không phát sinh thêm ô bắt buộc để tuyến đó bắt đầu hoạt động. 
- Ưu tiên hoàn thiện một tuyến mẫu trước khi nhân rộng sang các khu khác. 
- Có các trạng thái đi, chờ, nhận/giao hàng, thao tác nghề và mang hàng; hướng nhìn bám mục tiêu tương tác. 
- Hàng đang mang phải khớp dữ liệu thực tế. Hiệu ứng nhận hàng, thu tiền và mua ô chỉ phát khi giao dịch đã được xác nhận. 
- Khách bỏ đi vẫn mang phần hàng đã nhận; không phát tiền hoặc hiệu ứng hoàn hàng. 
- Hiển thị tiền, tiến trình góp ô, tồn kho và nguyên nhân tuyến đang dừng. 
- Bảng nhân viên tách theo nghề và khu. Nâng cấp một nhóm không tự áp dụng cho nghề hoặc khu khác. 
- Báo cáo tách doanh thu đã thu, chi phí và hàng thất thoát. Hàng khách mang đi không trả tiền được ghi theo loại, số lượng và nguyên nhân; không tính thành doanh thu. 
- Khi khách còn đang mua, hàng đã nhận là hàng đang giữ, chưa phải thất thoát. Chỉ chốt thất thoát khi đơn thất bại. 
- Đánh giá riêng chi phí mở tuyến, năng lực sản xuất, tốc độ phục vụ, sức chứa và kiên nhẫn của khách. 
- Với mốc 500/1000, kiểm tra thời gian tích lũy bằng doanh thu thực thu, không lấy tổng giá trị hàng đã giao làm thu nhập. 
- Khách bỏ đi không trả tiền cho cả phần đã nhận. Phần hàng đó rời khỏi hệ thống khi khách rời khu; phần chưa giao vẫn ở vị trí sở hữu hiện tại. 
- Dùng giá vốn để đánh giá thiệt hại; giá bán kỳ vọng chỉ là chỉ số cơ hội doanh thu bị mất, không cộng thêm vào chi phí để tránh tính trùng. 
- Chạy các kịch bản đủ hàng, thiếu hàng, nghẽn phục vụ và thiếu nhân viên. Điều chỉnh để người chơi thấy rõ nguyên nhân thất thoát và có cách khắc phục. 
- Không cân bằng dựa trên việc tải lại để phục hồi hàng hoặc nhận lại tiền. 
1. Rà soát code hiện có: luồng sở hữu hàng, kết thúc đơn, thu tiền và lưu/tải. Xác định chỗ đang trả hàng khách cầm về kho. 
2. Hoàn thiện giao dịch hàng/tiền và trạng thái đơn; bảo đảm mỗi kết quả chỉ được ghi nhận một lần. 
3. Làm một tuyến mẫu đầy đủ: mua trọn tuyến → sản xuất → vận chuyển → phục vụ → thu tiền hoặc thất thoát. 
4. Bổ sung nhân viên và nâng cấp riêng theo nghề/khu; kiểm tra điều phối và đường đi. 
5. Triển khai Save v2 và kiểm thử các trạng thái chuyển tiếp trước khi mở rộng nội dung. 
6. Nhân rộng các tuyến, hoàn thiện giao diện, animation và báo cáo. 
7. Chạy cân bằng kinh tế, kiểm thử hồi quy và đo hiệu năng trên cấu hình mục tiêu. 
Mỗi bước phải có bản chơi kiểm chứng được; không coi hiệu ứng hoặc giao diện hoàn tất là bằng chứng logic đã đúng. 
- Khách nhận đủ hàng và thanh toán: doanh thu ghi một lần, hàng không còn trong tồn kho. 
- Khách nhận một phần rồi bỏ đi: không thu tiền, không hoàn hàng, báo cáo ghi đúng phần đã nhận. 
- Khách chưa nhận gì rồi bỏ đi: không phát sinh thất thoát hàng. 
- Thanh toán và hết kiên nhẫn cùng nhịp: chỉ một kết quả hợp lệ được chốt theo quy tắc xử lý thống nhất. 
- Lưu trước giao hàng, sau mỗi lần giao một phần, trước/sau thanh toán và khi khách đang bỏ đi. 
- Tải lại giữ đúng hàng khách cầm, thời gian còn lại, hàng chờ và trạng thái đơn. 
- Lưu khi đơn đã thất bại nhưng khách chưa ra khỏi khu: tải lại không ghi thất thoát lần hai và không trả hàng về kho. 
- Tải lặp cùng một bản lưu không sinh thêm hàng, tiền hoặc khoản thất thoát. 
- Kiểm tra góp ô, nâng cấp nhân viên, vận chuyển dở và mẻ sản xuất dở không bị nhân đôi hay mất trạng thái. 
- Save prototype không bị ghi đè; dữ liệu không hợp lệ phải được báo rõ thay vì âm thầm đưa hàng về kho. 
- Mua mốc 500/1000 mở đủ các thành phần cơ bản của tuyến tương ứng; không thu tiền hai lần khi tương tác lặp. 
- Nâng cấp đúng nghề, đúng khu, đúng mức giá; lưu/tải giữ nguyên phạm vi tác động. 
- Mọi trạng thái mở khóa đều có đường tiếp cận hợp lệ; kiểm tra khi khách đông và hậu cần hoạt động đồng thời. 
- Chơi được vòng lặp hoàn chỉnh mà không cần thao tác sửa trạng thái thủ công. 
- Đối soát được hàng đầu kỳ, hàng tạo ra, hàng còn giữ, hàng tiêu thụ và hàng thất thoát. 
- Tiền thực thu khớp giao dịch thanh toán; báo cáo thất thoát khớp hàng khách bỏ đi mang theo. 
- Lưu/tải không thay đổi kết quả kinh tế của cùng một trạng thái mô phỏng.