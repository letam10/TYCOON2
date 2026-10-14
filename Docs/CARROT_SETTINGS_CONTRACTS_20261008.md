# Hợp đồng model cà rốt và cài đặt — 08/10/2026

## Phạm vi và phân công

- Root: HandItemProduce/HandItemMeshPrimitives/CarryItemLayout và test model liên quan.
- Root: QaDriver, QaCarrotSettingsAcceptance mới, test Player, build và tài liệu bàn giao.
- Agent settings: các file GameHud*.cs, Art.cs, GameSession.cs và file cài đặt/ngôn ngữ/FPS mới.
- Agent không sửa mesh, carry layout, QaDriver hoặc các file QA/root đang xử lý.
- Không tạo subagent con. Không chạy Unity khi root còn tích hợp code.

## API cài đặt được chốt

Namespace `Tycoon`; các kiểu public sau phải dùng đúng tên để root nghiệm thu.

```csharp
public enum GameLanguage { Vietnamese, English }
public enum GameGraphicsPreset { Low, Balanced, High }

[System.Serializable]
public sealed class GameSettingsData
{
    public GameLanguage language = GameLanguage.Vietnamese;
    public GameGraphicsPreset quality = GameGraphicsPreset.Balanced;
    public bool showFps;
    public bool syncDisplay = true;
    public int fpsLimit = 165;
    public float renderScale = 1;
    public bool fullscreen;
    public int resolutionWidth;
    public int resolutionHeight;
}

public static class GameSettingsStore
{
    public static GameSettingsData Current { get; }
    public static int Revision { get; }
    public static double DisplayRefreshHz { get; }
    public static void Initialize(bool qa, string qaDirectory);
    public static void SaveAndApply();
    public static void Apply();
}

public static class GameLocalization
{
    public static string Translate(string vietnamese);
}
```

Root có thể kiểm tra dữ liệu Current và UI, không gọi giao dịch kinh doanh để đổi cài đặt.
Cài đặt lưu riêng tiến trình chơi. QA dùng file `settings.json` trong qaDirectory riêng,
không ghi PlayerPrefs hoặc thay cài đặt thật. Build/test không được sửa tài nguyên URP đã lưu.

## UI và hành vi

- Thêm nút Cài đặt trên HUD và trong menu Esc.
- GameHud có `public void OpenSettings()`, `CloseSettings()`, `public bool SettingsOpen { get; }`.
- Bảng root có name `SettingsPanel`; mở bảng dừng simulation/interaction, đóng khôi phục đúng trạng thái.
- Dropdown: `QualityDropdown`, `ResolutionDropdown`, `LanguageDropdown`, `FrameLimitDropdown`.
- Toggle: `VSyncToggle`, `FpsToggle`; button: `ApplySettings`, `CloseSettings`.
- Có chất lượng Thấp/Cân bằng/Cao, độ phân giải thông dụng + độ phân giải native,
  cửa sổ/toàn màn hình, render scale và VSync/giới hạn FPS.
- Ngôn ngữ Việt/Anh cho HUD/menu, tên hàng và thông báo/nhãn tương tác thường gặp.
- Không đổi ID hàng, owner, receipt, đơn hàng, save và tên đối tượng phục vụ QA khi dịch.
- FPS overlay `FpsOverlay`, Text `FpsReadout`, Image filled `FpsProgress`; bật/tắt thực sự.
- Readout hiển thị FPS đo, ms/frame và Hz màn hình; không hiển thị target như FPS đã đo.
- Lấy mẫu unscaled, cập nhật UI khoảng 0,25–0,5 giây; không tạo string hoặc tìm UI mỗi frame.
- VSync bật: desktop `vSyncCount=1`, targetFrameRate=-1; giới hạn theo Hz thật.
- VSync tắt: `vSyncCount=0`, targetFrameRate=fpsLimit, mặc định 165.
- Không ép Windows đổi tần số quét, không tự giảm chất lượng lúc chơi để che số đo.
- Khi thay ngôn ngữ, nhãn tĩnh lẫn nhãn cập nhật được đổi; font hỗ trợ tiếng Việt/Anh.
- Init settings trước world/HUD; bỏ đoạn hardcode 165/VSync ở GameSession bằng Apply().
- QA mặc định cùng chất lượng Cân bằng/scale 1/VSync như bản chơi thường.

## Model cà rốt

- Dựa ảnh preview đã chốt: thân thuôn về một đầu nhọn, lá chỉ ở đầu to.
- Carrot carry scale `.84 * .90 = .756`; ModelSize `.54` giữ nguyên.
- Cà rốt riêng: 2 cột × 3 hàng, sáu củ/tầng; cùng chiều ngang qua hai tay.
- Các SKU khác giữ layout/cỡ/rotation hiện hành.
- Mesh chung gộp một renderer/submesh/material; không tạo lá/model mới mỗi frame.
- Hàng ở kho vẫn chạm mặt đỡ theo bounds sau xoay.

## Nghiệm thu

- Agent viết test cài đặt/localization/frame policy, kiểm tra `git diff --check`.
- Root chạy EditMode toàn bộ sau tích hợp, build Windows riêng và QA Player.
- Ảnh Player: cà rốt 1/24/48, hướng các củ, layout 2×3, leaf/head/tip.
- QA Player bật/tắt FPS, VI→EN→VI, áp dụng/lưu/đọc lại cài đặt, mở/đóng bằng Esc.
- Kiểm tra bảng ở 720p/1080p/1440p; FPS run bình thường vài phút, không benchmark.
- Đo thực tế RTX 4060/1080p + 165 Hz, giữ frame times và hitch; không cam kết 165 mọi frame.
