using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed partial class GameHud
    {
        GameObject settingsPanel, fpsPanel;
        Dropdown qualityDropdown, resolutionDropdown, languageDropdown, frameLimitDropdown;
        Toggle syncToggle, fpsToggle, fullscreenToggle;
        Slider renderScaleSlider;
        Text renderScaleValue, settingsMessage;
        Button settingsClose;
        ScreenMode settingsReturnMode;
        readonly List<Vector2Int> settingsResolutions = new();
        readonly int[] frameLimits = { 30, 60, 90, 120, 144, 165, 240, 360 };
        public bool SettingsOpen => mode == ScreenMode.Settings;

        void BuildSettings()
        {
            settingsPanel = Modal("SettingsPanel");
            var board = ManagementBoard(settingsPanel.transform, "CÀI ĐẶT",
                "Đồ họa, ngôn ngữ và tốc độ khung hình");
            Rect((RectTransform)board.transform, new(.14f, .06f), new(.86f, .94f), Vector2.zero, Vector2.zero);
            var map = Button(board.transform, "Bản đồ", OpenMap);
            map.name = "SettingsMapButton";
            Rect((RectTransform)map.transform, new(.04f, .815f), new(.32f, .865f), Vector2.zero, Vector2.zero);
            var bindings = Button(board.transform, "Tùy chỉnh phím", OpenBindings);
            bindings.name = "SettingsBindingsButton";
            Rect((RectTransform)bindings.transform, new(.36f, .815f), new(.64f, .865f), Vector2.zero, Vector2.zero);
            var slots = Button(board.transform, "Slot lưu • Chơi mới", OpenSaveSlots);
            slots.name = "SettingsSaveSlotsButton";
            Rect((RectTransform)slots.transform, new(.68f, .815f), new(.96f, .865f), Vector2.zero, Vector2.zero);
            // Hàng lối tắt thấp hơn nút thường; chừa đủ chiều cao cho chữ Việt và Anh.
            foreach (var shortcut in new[] { map, bindings, slots })
                Inset(shortcut.GetComponentInChildren<Text>().rectTransform, 4);
            qualityDropdown = SettingsDropdown(board.transform, "QualityDropdown", "Chất lượng", 0,
                new[] { "Thấp", "Cân bằng", "Cao" });
            var native = Screen.currentResolution;
            foreach (var resolution in new[]
            {
                new Vector2Int(1280, 720), new(1600, 900), new(1920, 1080), new(2560, 1440),
                new(Screen.width, Screen.height), new(native.width, native.height)
            })
                if (resolution.x > 0 && resolution.y > 0 && !settingsResolutions.Contains(resolution))
                    settingsResolutions.Add(resolution);
            var resolutionLabels = new List<string>();
            foreach (var resolution in settingsResolutions)
                resolutionLabels.Add($"{resolution.x} × {resolution.y}");
            resolutionDropdown = SettingsDropdown(board.transform, "ResolutionDropdown", "Độ phân giải", 1,
                resolutionLabels.ToArray());
            fullscreenToggle = SettingsToggle(board.transform, "FullscreenToggle", "Toàn màn hình", 2);
            syncToggle = SettingsToggle(board.transform, "VSyncToggle", "Đồng bộ màn hình (VSync)", 3);
            frameLimitDropdown = SettingsDropdown(board.transform, "FrameLimitDropdown", "Giới hạn FPS", 4,
                Array.ConvertAll(frameLimits, value => value + " FPS"));
            syncToggle.onValueChanged.AddListener(value => frameLimitDropdown.interactable = !value);
            SettingsRowLabel(board.transform, "Tỷ lệ dựng hình", 5);
            renderScaleSlider = SettingsSlider(board.transform, "RenderScaleSlider", 5);
            renderScaleValue = Text("RenderScaleValue", board.transform, "100%", 22,
                TextAnchor.MiddleRight, Ink);
            SettingsRowRect(renderScaleValue.rectTransform, 5, .88f, .96f);
            renderScaleSlider.onValueChanged.AddListener(value => renderScaleValue.text = $"{value * 100:0}%");
            languageDropdown = SettingsDropdown(board.transform, "LanguageDropdown", "Ngôn ngữ", 6,
                new[] { "Tiếng Việt", "English" });
            fpsToggle = SettingsToggle(board.transform, "FpsToggle", "Hiển thị FPS", 7);
            settingsMessage = Text("SettingsMessage", board.transform, "", 18, TextAnchor.MiddleCenter, Muted);
            Rect(settingsMessage.rectTransform, new(.04f, .105f), new(.96f, .14f), Vector2.zero, Vector2.zero);
            var apply = Button(board.transform, "Áp dụng", ApplySettings, true);
            apply.name = "ApplySettings";
            Rect((RectTransform)apply.transform, new(.18f, .025f), new(.48f, .093f), Vector2.zero, Vector2.zero);
            settingsClose = Button(board.transform, "Đóng", CloseSettings);
            settingsClose.name = "CloseSettings";
            Rect((RectTransform)settingsClose.transform, new(.52f, .025f), new(.82f, .093f),
                Vector2.zero, Vector2.zero);
            settingsPanel.SetActive(false);
            BuildFpsOverlay();
        }

        public void OpenSettings()
        {
            if (SettingsOpen) return;
            settingsReturnMode = mode;
            LoadSettingsControls();
            ShowScreen(ScreenMode.Settings);
            Select(settingsClose);
        }

        public void CloseSettings()
        {
            if (!SettingsOpen) return;
            ShowScreen(settingsReturnMode);
            if (mode == ScreenMode.Pause) Select(continueButton);
        }

        void LoadSettingsControls()
        {
            var data = GameSettingsStore.Current;
            qualityDropdown.SetValueWithoutNotify((int)data.quality);
            languageDropdown.SetValueWithoutNotify((int)data.language);
            int index = settingsResolutions.IndexOf(new Vector2Int(
                data.resolutionWidth > 0 ? data.resolutionWidth : Screen.width,
                data.resolutionHeight > 0 ? data.resolutionHeight : Screen.height));
            resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
            index = Array.IndexOf(frameLimits, data.fpsLimit);
            frameLimitDropdown.SetValueWithoutNotify(index >= 0 ? index : 5);
            frameLimitDropdown.interactable = !data.syncDisplay;
            syncToggle.SetIsOnWithoutNotify(data.syncDisplay);
            fpsToggle.SetIsOnWithoutNotify(data.showFps);
            fullscreenToggle.SetIsOnWithoutNotify(data.fullscreen);
            renderScaleSlider.SetValueWithoutNotify(data.renderScale);
            renderScaleValue.text = $"{data.renderScale * 100:0}%";
            SetText(settingsMessage, "Thay đổi được lưu khi bấm Áp dụng.");
            RefreshSettingsOptions();
        }

        void ApplySettings()
        {
            var data = GameSettingsStore.Current;
            data.quality = (GameGraphicsPreset)qualityDropdown.value;
            data.language = (GameLanguage)languageDropdown.value;
            data.resolutionWidth = settingsResolutions[resolutionDropdown.value].x;
            data.resolutionHeight = settingsResolutions[resolutionDropdown.value].y;
            data.fpsLimit = frameLimits[frameLimitDropdown.value];
            data.renderScale = renderScaleSlider.value;
            data.syncDisplay = syncToggle.isOn;
            data.showFps = fpsToggle.isOn;
            data.fullscreen = fullscreenToggle.isOn;
            try
            {
                GameSettingsStore.SaveAndApply();
                RefreshSettingsOptions();
                RefreshPlayingHud();
                RefreshStatus();
                RefreshFpsVisibility();
                SetText(settingsMessage, "Đã lưu cài đặt.");
            }
            catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException)
            {
                SetText(settingsMessage, "Không lưu được: " + error.Message);
            }
        }

        void RefreshSettingsOptions()
        {
            string[] names = { "Thấp", "Cân bằng", "Cao" };
            for (int i = 0; i < names.Length; i++)
                qualityDropdown.options[i].text = GameLocalization.Translate(names[i]);
            qualityDropdown.RefreshShownValue();
        }

        void BuildFpsOverlay()
        {
            fpsPanel = Panel("FpsOverlay", safeArea, Surface, .95f);
            TopLeft((RectTransform)fpsPanel.transform, 16, 94, 350, 50);
            var label = Text("FpsReadout", fpsPanel.transform, "FPS — | — ms | — Hz", 18,
                TextAnchor.MiddleCenter, Ink);
            Rect(label.rectTransform, Vector2.zero, Vector2.one, new(8, 10), new(-8, -3));
            var track = Panel("FpsTrack", fpsPanel.transform, "#102C27");
            Rect((RectTransform)track.transform, Vector2.zero, new(1, 0), new(10, 6), new(-10, 10));
            var bar = Panel("FpsProgress", track.transform, Accent).GetComponent<Image>();
            Rect(bar.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bar.type = Image.Type.Filled;
            bar.fillMethod = Image.FillMethod.Horizontal;
            bar.fillOrigin = 0;
            fpsPanel.AddComponent<FpsOverlay>().Initialize(label, bar);
            RefreshFpsVisibility();
        }

        void RefreshFpsVisibility()
        {
            if (fpsPanel && fpsPanel.activeSelf != GameSettingsStore.Current.showFps)
                fpsPanel.SetActive(GameSettingsStore.Current.showFps);
        }
    }
}
