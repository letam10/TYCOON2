# QA Công đoạn 14 bằng dữ liệu sát ngưỡng

Chạy tại thư mục `D:\GAME\TYCOON2`:

```powershell
Tools/run_unity.ps1 -Task Build -BuildPath work/stage14/Build/TYCOON2.exe
Tools/run_player.ps1 -Task Stage14Milestones -BuildPath work/stage14/Build/TYCOON2.exe
```

`stage14-player-near-thresholds.json` là dữ liệu được nạp **một lần trước mỗi ca**. Ví bắt đầu ở 490, 990, 1.990, 11.990, 64.990, 99.990 hoặc 79.990 xu; các điều kiện order/batch tương ứng ở sát ngưỡng. Sau đó Player tự di chuyển, sản xuất, mang hàng, Serve, Collect và góp tiền tại purchase pad qua gameplay thật. Không cấp thêm tiền để vượt mốc trong khi ca đang chạy.

- Farm Shop: 49 → 50 đơn, 1.990 → 2.010 xu.
- Processing: 199 → 200 đơn, 11.990 → 12.010 xu.
- Supermarket: ba recipe 49 → 50 batches, 599 → 600 đơn, 64.990 → 65.010 xu.
- Restaurant: Bakery 59 → 60 đơn qua một batch bánh thật, 79.990 → 80.290 xu.
- Có vòng chăn nuôi, cook/serve/eat/payment/clean, đơn giao dở hết hạn, máy pause/load/resume, load lặp và 30 NPC thật.

Save QA riêng nằm trong `work/stage14/player-stage14milestones-<timestamp>/`; không ghi vào save người chơi. Báo cáo gồm `stage14-milestones-report.json` và `milestone-details.json`. `seededFixture=true` phân biệt rõ dữ liệu được nạp với tiến trình chơi từ 0.

`stage14-boundaries.json` phục vụ test transaction; `stage14-economy.json` ghi giá/yield/chu kỳ đang tune. `stage14-balance.json` phục vụ lượt chơi từ 0, chỉ tăng tốc đồng hồ simulation, không cấp tiền hay unlock. Các ca sát mốc không chứng minh thời lượng bình thường 180–240 phút.
