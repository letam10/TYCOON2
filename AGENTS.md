# Quy tắc làm việc

## Phạm vi
- Làm đúng yêu cầu. Không tự publish/deploy/push/tạo site; không khai "người dùng yêu cầu" khi họ không yêu cầu.
- Không tự đổi phạm vi hay công nghệ (vd 3D → 2D). Nếu buộc phải đổi: hỏi trước hoặc ghi rõ trong báo cáo.
- Chỉ tắt, xóa, sửa những thứ mình tạo ra.
- Đề mới → tạo thư mục con mới và chỉ làm trong đó; không sửa hay tái sử dụng file của đề/phiên khác trừ khi được yêu cầu.

## Trước khi viết
- Nêu cách làm, ràng buộc, chỗ dễ sai. Công thức và tỉ lệ phải suy ra, không đoán số.
- Mỗi lệnh ghi tối đa 1 file, dưới ~15KB. File lớn hơn: chia module, hoặc ghi phần đầu rồi nối tiếp tại cuối hàm/section hoàn chỉnh.

## Ghi file
- Thay toàn bộ một file: ghi đè trực tiếp bằng MỘT thao tác. Không dùng Delete + Add cùng file trong một patch,
  không xóa trước rồi ghi sau (để lại khoảng file bị mất).

## Định dạng code
- Mỗi dòng một câu lệnh; dòng không quá ~120 ký tự. HTML dài thì tách nhiều dòng hoặc template riêng.
- Không nén code (nhét nhiều statement một dòng, tên biến một ký tự). Tách file/module theo kiến trúc hợp lý để code rõ ràng, dễ bảo trì.

## Subagent
- Phiên cha (`/root`) phải đọc xong đề bài / file đính kèm và chốt kiến trúc + giao diện giữa các file (tên file, hàm export, cấu trúc dữ liệu) trước khi `spawn_agent`.
- Khi gọi `spawn_agent`: chỉ giao việc viết module hoặc kiểm thử độc lập cụ thể (mỗi subagent phụ trách file riêng, không trùng với phiên cha); KHÔNG spawn subagent chỉ để đọc lại đề bài hay nghiên cứu chung chung.
- Dùng `fork_turns: "none"` và truyền đủ ngữ cảnh trong `message`: nếu viện dẫn file đính kèm ngoài workspace thì bắt buộc
  ghi **đường dẫn tuyệt đối đầy đủ**. Chỉ dùng `fork_turns: "all"` khi thật sự cần toàn bộ hội thoại (subagent khi đó
  dùng cùng model với phiên cha). Codex chỉ nhận `"none"` hoặc `"all"`.
- Đưa các quy tắc ghi file ở trên (≤15KB, không Delete + Add) vào `message` để subagent làm đúng ngay từ đầu.
- **Subagent con TUYỆT ĐỐI KHÔNG**: gọi `spawn_agent` đẻ thêm subagent cấp cháu, quét ổ đĩa ngoài thư mục dự án (`D:\`, `C:\Users\...`), hay lặp `Get-ChildItem` / `send_message` / `list_agents` / `wait_agent` để theo dõi phiên cha. Hoàn thành xong việc được giao → tự kiểm tra → gửi 1 báo cáo kết quả duy nhất rồi kết thúc ngay.
- Phiên cha khi chờ subagent dùng `wait_agent` với `timeout_ms >= 60000`, không poll ngắn lặp lại.

## Kiểm chứng
- Chạy thật như người dùng sẽ dùng; đọc lỗi thật (console, log, output test).
- Cú pháp, grep, HTTP 200 KHÔNG phải là kiểm chứng.
- Gặp lỗi: tìm nguyên nhân từ thông báo lỗi cụ thể rồi mới sửa; không sửa theo phỏng đoán.
  Không thấy được lỗi thì tạo cách để thấy (mở trực tiếp, in lỗi ra màn hình).
- Sau mỗi lần sửa: chạy lại và lấy bằng chứng MỚI (log có thời gian sau lần sửa, ảnh chụp mới). Bỏ qua log cũ.
- Đọc kết quả rồi mới làm tiếp; không gộp lệnh dọn dẹp với lệnh kiểm tra.
- Dự án có lệnh verify: chỉ báo xong khi lệnh đó exit 0.

## Báo cáo
- Kèm lệnh và kết quả thật. Ghi rõ: đã làm / đã kiểm / chưa kiểm / còn lỗi / đã đổi gì so với yêu cầu.

## Luật bắt buộc khi thực hiện và yêu cầu trước khi làm việc

### 1. Suy nghĩ trước khi lập trình

- Không tự đoán khi giả định ảnh hưởng đến kết quả; nêu rõ các giả định.
- Nếu yêu cầu có nhiều cách hiểu, trình bày các cách hiểu liên quan và hỏi các điểm cần chốt thay vì âm thầm chọn một cách.
- Nếu có cách đơn giản hơn, phải nêu và đề xuất cách đó; có thể phản biện yêu cầu khi cần.
- Nếu còn điều chưa rõ hoặc mâu thuẫn, hỏi lại điều đó, chỉ ra điểm chưa rõ và yêu cầu làm rõ.

### 2. Ưu tiên sự đơn giản

- Chỉ triển khai những gì yêu cầu cần; không thêm tính năng suy đoán.
- Không tạo abstraction dùng một lần.
- Không thêm cấu hình khi chưa được yêu cầu.
- Không thêm xử lý lỗi cho các tình huống không bao giờ xảy ra trong phạm vi đã xác định.
- Nếu giải pháp dài và phức tạp hơn mức cần thiết, hãy rút gọn.
- Nếu code chỗ đó hay chức năng đó phức tạp, nhớ thêm comment tiếng Việt ngắn gọn để người dùng cũng hiểu.

### 3. Thay đổi có phạm vi chính xác

- Chỉ sửa những gì cần cho yêu cầu; không tự tiện cải thiện code hoặc định dạng lân cận.
- Không refactor phần không bị hỏng.
- Giữ phong cách hiện có của dự án; nếu có cách khác thì nêu ngắn gọn khi lập kế hoạch.
- Nếu phát hiện dead code không liên quan, nêu lại nhưng không tự xóa.
- Chỉ xóa import, biến hoặc hàm mồ côi do chính thay đổi hiện tại tạo ra; không xóa dead code có sẵn nếu chưa được yêu cầu.
- Mỗi dòng thay đổi phải truy nguyên trực tiếp về yêu cầu của người dùng.

### 4. Thực thi theo mục tiêu và kiểm chứng

**Xác định tiêu chí thành công. Lặp lại cho đến khi được kiểm chứng.**

Chuyển yêu cầu thành các mục tiêu có thể kiểm chứng:

- “Thêm kiểm tra dữ liệu” → “Viết test cho đầu vào không hợp lệ, rồi làm cho test đạt”.
- “Sửa lỗi” → “Viết test tái hiện lỗi, rồi làm cho test đạt”.
- “Refactor phần X” → “Bảo đảm test đạt trước và sau khi refactor”.

- Với tác vụ nhiều bước, nêu một kế hoạch ngắn kèm cách kiểm chứng trước khi thực hiện:
  1. Bước → kiểm chứng.
  2. Bước → kiểm chứng.
  3. Bước → kiểm chứng.
- Sau khi thay đổi, lặp lại việc kiểm tra cho đến khi tiêu chí thành công được xác nhận; không kết luận chỉ dựa trên giả định, ghi chú ngắn gọn các bước và kiểm chứng để tránh mất thông tin khi ngữ cảnh bị rút gọn (context compaction) hoặc khi xảy ra sự cố.

### 5. Quy tắc giao tiếp và tiết kiệm token

- Với tác vụ đơn giản, dùng phán đoán và trả lời ngắn gọn; không bắt buộc quy trình đầy đủ của các tác vụ lớn.
- Với tác vụ nhiều bước, được phép gửi kế hoạch ngắn và các cập nhật cần thiết; không gửi diễn giải trung gian hoặc log không giúp kiểm chứng kết quả.
- Gom tất cả các câu hỏi cần người dùng quyết định vào một lần hỏi khi có thể, nhưng bắt buộc phải hỏi rõ ràng, chính xác trước khi thực hiện nếu câu trả lời có ảnh hưởng đến hướng làm.
- Sau khi đã đủ thông tin, thực hiện liên tục đến khi hoàn thành và trả kết quả đầy đủ, súc tích.
- Không diễn giải các bước làm và cách làm, thực hiện một mạch đến khi hoàn thành nhiệm vụ không plan hay giải thích gì trong lúc thực hiện nếu người dùng không cần. Và câu trả lời/đầu ra đủ mà ngắn gọn không xuống dòng hoặc thêm ký tự dư thừa làm tốn token. Không tự động test hay Benchmark nếu người dùng không yêu cầu.