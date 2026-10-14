using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        readonly List<string> featureUiFailures = new();
        Camera featureUiCaptureCamera;
        Vector2Int featureUiCaptureSize;

        IEnumerator FeatureUiAcceptance()
        {
            Check(game.Player.CanControl, "Feature UI matrix starts from normal gameplay");
            string originalSettings = JsonUtility.ToJson(GameSettingsStore.Current);
            int originalWidth = Screen.width;
            int originalHeight = Screen.height;
            var originalMode = Screen.fullScreenMode;
            featureUiFailures.Clear();
            try
            {
                foreach (var language in new[] { GameLanguage.Vietnamese, GameLanguage.English })
                {
                    foreach (var size in new[]
                    {
                        new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                        new Vector2Int(2560, 1440), new Vector2Int(3840, 2160)
                    })
                    {
                        bool english = language == GameLanguage.English;
                        string sample = (english ? "en-" : "vi-") + size.x;
                        var settings = GameSettingsStore.Current;
                        settings.language = language;
                        settings.resolutionWidth = size.x;
                        settings.resolutionHeight = size.y;
                        settings.fullscreen = false;
                        GameSettingsStore.SaveAndApply();
                        float deadline = Time.realtimeSinceStartup + 5;
                        do
                        {
                            yield return null;
                        }
                        while ((Screen.width != size.x || Screen.height != size.y)
                            && Time.realtimeSinceStartup < deadline);
                        yield return new WaitForSecondsRealtime(.15f);
                        bool nativeSize = Screen.width == size.x && Screen.height == size.y;
                        bool offscreen = size.x == 3840 && !nativeSize;
                        if (offscreen)
                        {
                            sample += "-offscreen";
                            Check(true, "Feature UI " + sample + " uses a " + size
                                + " render target; native window remains " + Screen.width + "x" + Screen.height);
                        }
                        else
                        {
                            FeatureUiAssert(nativeSize, sample + " native screen requested " + size + " actual "
                                + Screen.width + "x" + Screen.height);
                        }
                        game.Hud.OpenSettings();
                        yield return null;
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        FeatureUiCapture("ui-settings-" + sample + ".png", size,
                            () => FeatureUiSettingsLabels(english, sample));

                        FeatureClick("SettingsMapButton");
                        yield return null;
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        FeatureUiCapture("ui-map-" + sample + ".png", size,
                            () => FeatureUiPanel("WorldMapPanel", sample, english ? "CITY MAP" : "BẢN ĐỒ THÀNH PHỐ"));
                        FeatureUiButtonText("MapTravel_tycoon", english ? "TYCOON • Farm" : "TYCOON • Nông trại",
                            sample);
                        FeatureUiText(SettingsObject<Text>("MapMessage"), english
                            ? "Free travel keeps your carried goods and cash."
                            : "Dịch chuyển miễn phí, giữ nguyên hàng và tiền.", sample);
                        FeatureClick("CloseMap");
                        yield return null;
                        FeatureUiSettingsReturn(sample + " map");

                        FeatureClick("SettingsBindingsButton");
                        yield return null;
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        FeatureUiCapture("ui-bindings-" + sample + ".png", size,
                            () => FeatureUiPanel("BindingsPanel", sample, english ? "KEY BINDINGS" : "TÙY CHỈNH PHÍM"));
                        FeatureUiButtonText("ResetBindings", english ? "Restore defaults" : "Khôi phục mặc định",
                            sample);
                        string[] englishActions =
                        {
                            "Forward", "Back", "Left", "Right", "Sprint", "Withdraw cash",
                            "Select item", "Quick save", "Menu", "Map"
                        };
                        for (int index = 0; index < GameKeyBindings.Options.Length; index++)
                        {
                            var option = GameKeyBindings.Options[index];
                            string action = english ? englishActions[index] : option.label;
                            FeatureUiButtonText("Bind_" + option.id, action + "  :  "
                                + GameKeyBindings.Label(option.id), sample);
                        }
                        FeatureUiText(SettingsObject<Text>("BindingsMessage"), english
                            ? "Arrow keys and gamepad remain available."
                            : "Phím mũi tên và gamepad vẫn dùng được.", sample);
                        string originalMapBinding = GameKeyBindings.Path("map");
                        FeatureClick("Bind_map");
                        yield return null;
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        FeatureUiText(SettingsObject<Text>("BindingsMessage"), english
                            ? "Press a new key, or Esc to cancel." : "Nhấn phím mới, hoặc Esc để hủy.", sample);
                        if (size.x == 1920 || offscreen)
                            FeatureUiCapture("ui-binding-prompt-" + sample + ".png", size,
                                () => FeatureUiLabel(SettingsObject<Text>("BindingsMessage"), sample + " prompt"));
                        else
                            FeatureUiLabel(SettingsObject<Text>("BindingsMessage"), sample + " prompt");
                        yield return FeatureKey(Key.Escape);
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        FeatureUiText(SettingsObject<Text>("BindingsMessage"), english
                            ? "Key change cancelled." : "Đã hủy đổi phím.", sample);
                        if (size.x == 1920 || offscreen)
                            FeatureUiCapture("ui-binding-cancel-" + sample + ".png", size,
                                () => FeatureUiLabel(SettingsObject<Text>("BindingsMessage"), sample + " cancelled"));
                        else
                            FeatureUiLabel(SettingsObject<Text>("BindingsMessage"), sample + " cancelled");
                        FeatureUiAssert(SettingsObject<RectTransform>("BindingsPanel").gameObject.activeInHierarchy
                            && GameKeyBindings.Path("map") == originalMapBinding,
                            sample + " Escape cancels binding capture without changing control or leaving the menu");
                        FeatureClick("CloseBindings");
                        yield return null;
                        FeatureUiSettingsReturn(sample + " bindings");

                        FeatureClick("SettingsSaveSlotsButton");
                        yield return null;
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        FeatureUiCapture("ui-slots-" + sample + ".png", size,
                            () => FeatureUiPanel("SaveSlotsPanel", sample,
                                english ? "SAVE SLOTS" : "SLOT LƯU TRÒ CHƠI"));
                        for (int slot = 1; slot <= GameSaveSlots.Count; slot++)
                        {
                            FeatureUiButtonText("SaveToSlot_" + slot, english ? "Save" : "Lưu", sample);
                            FeatureUiButtonText("LoadSlot_" + slot, english ? "Load" : "Tải", sample);
                            FeatureUiButtonText("NewSlot_" + slot, english ? "New game" : "Chơi mới", sample);
                        }
                        var slotInfo = SettingsObject<RectTransform>("SaveSlot_" + game.ActiveSaveSlot)
                            .GetComponentInChildren<Text>();
                        string active = "Slot " + game.ActiveSaveSlot + (english ? " • active" : " • đang chơi");
                        FeatureUiAssert(slotInfo.text.StartsWith(active + "\n"), sample
                            + " active slot label expected " + active + " actual " + slotInfo.text.Replace('\n', ' '));
                        FeatureUiText(SettingsObject<Text>("SlotsMessage"), (english
                            ? "Quick save uses the active slot: " : "Lưu nhanh dùng slot đang chơi: ")
                            + game.ActiveSaveSlot, sample);
                        FeatureClick("CloseSaveSlots");
                        yield return null;
                        FeatureUiSettingsReturn(sample + " save slots");
                        FeatureClick("CloseSettings");
                        yield return null;
                        FeatureUiAssert(game.Player.CanControl && Time.timeScale == 1,
                            sample + " closing Settings restores gameplay input");
                    }
                }
            }
            finally
            {
                Keys();
                if (game.Hud.MapOpen)
                    game.Hud.CloseMap();
                if (SettingsObject<RectTransform>("BindingsPanel").gameObject.activeInHierarchy)
                    game.Hud.CloseBindings();
                if (SettingsObject<RectTransform>("SaveSlotsPanel").gameObject.activeInHierarchy)
                    game.Hud.CloseSaveSlots();
                game.Hud.CloseSettings();
                JsonUtility.FromJsonOverwrite(originalSettings, GameSettingsStore.Current);
                GameSettingsStore.SaveAndApply();
                // Phục hồi cửa sổ riêng vì cấu hình 0 dùng kích thước hiện tại.
                Screen.SetResolution(originalWidth, originalHeight, originalMode);
                game.Hud.OpenSettings();
                game.Hud.CloseSettings();
            }
            yield return new WaitForSecondsRealtime(.6f);
            Check(JsonUtility.ToJson(GameSettingsStore.Current) == originalSettings,
                "Feature UI matrix restores the full original settings and key bindings");
            Check(game.Player.CanControl && Time.timeScale == 1, "Feature UI matrix restores gameplay control");
            Check(Screen.width == originalWidth && Screen.height == originalHeight,
                "Feature UI matrix restores the original native resolution");
            Check(featureUiFailures.Count == 0, "Feature UI matrix: " + string.Join(" | ", featureUiFailures));
        }

        void FeatureUiCapture(string name, Vector2Int size, System.Action inspect)
        {
            string path = Path.Combine(game.QaDirectory, name);
            QaRenderedFrame.Write(path, size.x, size.y, (camera, canvases, width, height) =>
            {
                featureUiCaptureCamera = camera;
                featureUiCaptureSize = new Vector2Int(width, height);
                try
                {
                    FeatureUiAssert(camera.targetTexture.width == size.x && camera.targetTexture.height == size.y,
                        name + " uses the requested render target " + size);
                    foreach (var canvas in canvases)
                    {
                        var rectangle = (RectTransform)canvas.transform;
                        FeatureUiAssert(Mathf.Abs(rectangle.rect.width * canvas.scaleFactor - width) <= 2
                            && Mathf.Abs(rectangle.rect.height * canvas.scaleFactor - height) <= 2,
                            name + " " + canvas.name + " canvas covers the render target");
                    }
                    inspect();
                }
                finally
                {
                    featureUiCaptureCamera = null;
                    featureUiCaptureSize = Vector2Int.zero;
                }
            });
            using (var input = File.OpenRead(path))
            {
                var header = new byte[24];
                int read = input.Read(header, 0, header.Length);
                FeatureUiAssert(read == header.Length && PngDimension(header, 16) == size.x
                    && PngDimension(header, 20) == size.y, name + " PNG dimensions match " + size);
            }
            report.screenshots.Add(name);
        }

        void FeatureUiSettingsLabels(bool english, string sample)
        {
            foreach (var entry in new[]
            {
                ("SettingsMapButton", english ? "Map" : "Bản đồ"),
                ("SettingsBindingsButton", english ? "Key bindings" : "Tùy chỉnh phím"),
                ("SettingsSaveSlotsButton", english ? "Save slots • New game" : "Slot lưu • Chơi mới")
            })
            {
                var button = SettingsObject<Button>(entry.Item1);
                FeatureUiRect((RectTransform)button.transform, sample + " " + entry.Item1);
                FeatureUiLabel(button.GetComponentInChildren<Text>(), sample + " " + entry.Item1);
                FeatureUiButtonText(entry.Item1, entry.Item2, sample);
            }
        }

        void FeatureUiAssert(bool condition, string description)
        {
            if (condition)
                Check(true, "Feature UI " + description);
            else
                featureUiFailures.Add(description);
        }

        void FeatureUiSettingsReturn(string sample)
        {
            FeatureUiAssert(game.Hud.SettingsOpen && Time.timeScale == 0 && !game.Player.CanControl,
                sample + " closes to paused Settings");
        }

        void FeatureUiButtonText(string name, string expected, string sample)
        {
            FeatureUiText(SettingsObject<Button>(name).GetComponentInChildren<Text>(), expected, sample + " " + name);
        }

        void FeatureUiText(Text label, string expected, string sample)
        {
            FeatureUiAssert(label.isActiveAndEnabled && label.text == expected,
                sample + " " + label.name + " expected [" + expected + "] actual [" + label.text + "]");
        }

        void FeatureUiPanel(string name, string sample, string title)
        {
            var panel = SettingsObject<RectTransform>(name);
            FeatureUiAssert(panel.gameObject.activeInHierarchy && Time.timeScale == 0 && !game.Player.CanControl,
                sample + " " + name + " is visible and blocks gameplay input");
            var heading = panel.GetComponentsInChildren<Text>().Single(label => label.name == "Heading");
            FeatureUiText(heading, title, sample + " " + name);
            foreach (var button in panel.GetComponentsInChildren<Button>())
                FeatureUiRect((RectTransform)button.transform, sample + " " + name + "/" + button.name);
            foreach (var label in panel.GetComponentsInChildren<Text>())
            {
                // Ghim bản đồ là biểu tượng; chỉ kiểm tra vùng nút.
                if (label.transform.parent.name.StartsWith("MapPin_"))
                    continue;
                FeatureUiLabel(label, sample + " " + name + "/" + label.transform.parent.name + "/" + label.name);
            }
        }

        void FeatureUiRect(RectTransform rect, string sample)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            int width = featureUiCaptureCamera ? featureUiCaptureSize.x : Screen.width;
            int height = featureUiCaptureCamera ? featureUiCaptureSize.y : Screen.height;
            string surface = featureUiCaptureCamera ? "render target" : "native screen";
            // Canvas đổi sang camera khi chụp; tọa độ world cần chiếu vào đúng render target.
            if (featureUiCaptureCamera)
            {
                for (int index = 0; index < corners.Length; index++)
                    corners[index] = featureUiCaptureCamera.WorldToScreenPoint(corners[index]);
            }
            FeatureUiAssert(rect.rect.width > 0 && rect.rect.height > 0
                && corners.All(point => (!featureUiCaptureCamera || point.z > 0)
                    && point.x >= -1 && point.y >= -1 && point.x <= width + 1 && point.y <= height + 1),
                sample + " has a nonempty rectangle within the " + surface + " " + width + "x" + height);
        }

        void FeatureUiLabel(Text label, string sample)
        {
            if (string.IsNullOrWhiteSpace(label.text))
                return;
            FeatureUiRect(label.rectTransform, sample);
            var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
            using var visible = new TextGenerator();
            using var complete = new TextGenerator();
            bool populated = visible.Populate(label.text, settings);
            int fontSize = label.resizeTextForBestFit ? visible.fontSizeUsedForBestFit : label.fontSize;
            FeatureUiAssert(populated && visible.characterCountVisible > 0 && label.color.a > .01f && fontSize > 0,
                sample + " renders visible text");
            // Đối chiếu bản không cắt dọc, cùng cỡ chữ, để phát hiện mất dòng.
            settings.resizeTextForBestFit = false;
            settings.fontSize = fontSize;
            if (label.resizeTextForBestFit && label.font.dynamic)
            {
                // Best-fit trả cỡ pixel đã nhân scale; giữ cả font và khung ở đơn vị pixel.
                settings.generationExtents *= label.pixelsPerUnit;
                settings.scaleFactor = 1;
            }
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            complete.Populate(label.text, settings);
            FeatureUiAssert(visible.characterCountVisible == complete.characterCountVisible,
                sample + " does not truncate characters (visible=" + visible.characterCountVisible
                + ", complete=" + complete.characterCountVisible + ")");
            float requiredHeight = complete.GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
            FeatureUiAssert(requiredHeight <= label.rectTransform.rect.height + 1,
                sample + " text height " + requiredHeight.ToString("F1") + " fits "
                + label.rectTransform.rect.height.ToString("F1") + " font=" + fontSize
                + " pixelsPerUnit=" + label.pixelsPerUnit.ToString("F3"));
        }
    }
}
