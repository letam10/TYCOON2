using System;
using System.IO;
using UnityEngine;

namespace Tycoon
{
    public static class GameSettingsStore
    {
        const string PreferenceKey = "Tycoon2.Settings.v1";
        static string qaPath;
        static bool initialized;
        public static GameSettingsData Current { get; private set; } = new();
        public static int Revision { get; private set; }
        public static double DisplayRefreshHz
        {
            get
            {
                double hz = Screen.currentResolution.refreshRateRatio.value;
                return double.IsNaN(hz) || double.IsInfinity(hz) || hz <= 0 ? 0 : hz;
            }
        }

        public static void Initialize(bool qa, string qaDirectory)
        {
            qaPath = qa ? Path.Combine(qaDirectory, "settings.json") : null;
            Current = new GameSettingsData();
            try
            {
                string json = qa
                    ? File.Exists(qaPath) ? File.ReadAllText(qaPath) : ""
                    : PlayerPrefs.GetString(PreferenceKey, "");
                if (!string.IsNullOrEmpty(json)) JsonUtility.FromJsonOverwrite(json, Current);
            }
            catch (Exception error) when (error is IOException or ArgumentException or UnauthorizedAccessException)
            {
                Current = new GameSettingsData();
                Debug.LogWarning("Không đọc được cài đặt; dùng mặc định. " + error.Message);
            }
            Current.Validate();
            initialized = true;
            Apply();
        }

        public static void SaveAndApply()
        {
            if (!initialized) throw new InvalidOperationException("Game settings have not been initialized.");
            Current.Validate();
            string json = JsonUtility.ToJson(Current, true);
            if (qaPath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(qaPath));
                File.WriteAllText(qaPath, json);
            }
            else
            {
                PlayerPrefs.SetString(PreferenceKey, json);
                PlayerPrefs.Save();
            }
            Apply();
        }

        public static void Apply()
        {
            Current.Validate();
            QualitySettings.vSyncCount = Current.syncDisplay ? 1 : 0;
            Application.targetFrameRate = Current.syncDisplay ? -1 : Current.fpsLimit;
            if (Application.isPlaying)
            {
                if (GameSession.Instance && GameSession.Instance.Player)
                    GameKeyBindings.ApplyPlayer(GameSession.Instance.Player);
                GameSettingsGraphics.Apply(Current);
                int width = Current.resolutionWidth > 0 ? Current.resolutionWidth : Screen.width;
                int height = Current.resolutionHeight > 0 ? Current.resolutionHeight : Screen.height;
                var mode = Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                // Không truyền refresh rate: giữ nguyên tần số quét của Windows.
                if (Screen.width != width || Screen.height != height || Screen.fullScreenMode != mode)
                    Screen.SetResolution(width, height, mode);
            }
            Revision++;
            LocalizedLabel.RefreshAll();
        }
    }
}
