using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class GameSettingsTests
    {
        string directory, previousSettings, previousPreferences;
        int previousVSync, previousTarget;
        const string PreferenceKey = "Tycoon2.Settings.v1";

        [SetUp]
        public void SetUp()
        {
            previousSettings = JsonUtility.ToJson(GameSettingsStore.Current);
            previousPreferences = PlayerPrefs.GetString(PreferenceKey, "");
            previousVSync = QualitySettings.vSyncCount;
            previousTarget = Application.targetFrameRate;
            directory = Path.Combine(Path.GetTempPath(), "TycoonSettings_" + Guid.NewGuid().ToString("N"));
            GameSettingsStore.Initialize(true, directory);
        }

        [TearDown]
        public void TearDown()
        {
            Assert.That(PlayerPrefs.GetString(PreferenceKey, ""), Is.EqualTo(previousPreferences),
                "QA settings must not change real player preferences.");
            JsonUtility.FromJsonOverwrite(previousSettings, GameSettingsStore.Current);
            GameSettingsStore.Apply();
            QualitySettings.vSyncCount = previousVSync;
            Application.targetFrameRate = previousTarget;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void FreshQaUsesPlayableBalancedDefaultsWithoutCreatingSaveFiles()
        {
            var settings = GameSettingsStore.Current;
            Assert.That(settings.language, Is.EqualTo(GameLanguage.Vietnamese));
            Assert.That(settings.quality, Is.EqualTo(GameGraphicsPreset.Balanced));
            Assert.That(settings.renderScale, Is.EqualTo(1));
            Assert.That(settings.syncDisplay, Is.True);
            Assert.That(settings.fpsLimit, Is.EqualTo(165));
            Assert.That(settings.showFps, Is.False);
            Assert.That(Directory.Exists(directory), Is.False);
        }

        [Test]
        public void QaPreferencesRoundTripInPrivateDirectoryAndDoNotTouchGameSave()
        {
            Directory.CreateDirectory(directory);
            string gameSave = Path.Combine(directory, "qa-save-v2.json");
            File.WriteAllText(gameSave, "existing progress");
            var settings = GameSettingsStore.Current;
            settings.language = GameLanguage.English;
            settings.quality = GameGraphicsPreset.High;
            settings.showFps = true;
            settings.syncDisplay = false;
            settings.fpsLimit = 120;
            settings.renderScale = .75f;
            settings.fullscreen = true;
            settings.resolutionWidth = 1920;
            settings.resolutionHeight = 1080;
            GameSettingsStore.SaveAndApply();
            Assert.That(File.Exists(Path.Combine(directory, "settings.json")), Is.True);
            GameSettingsStore.Initialize(true, directory);
            var restored = GameSettingsStore.Current;
            Assert.That(restored.language, Is.EqualTo(GameLanguage.English));
            Assert.That(restored.quality, Is.EqualTo(GameGraphicsPreset.High));
            Assert.That(restored.showFps, Is.True);
            Assert.That(restored.syncDisplay, Is.False);
            Assert.That(restored.fpsLimit, Is.EqualTo(120));
            Assert.That(restored.renderScale, Is.EqualTo(.75f));
            Assert.That(restored.fullscreen, Is.True);
            Assert.That(restored.resolutionWidth, Is.EqualTo(1920));
            Assert.That(restored.resolutionHeight, Is.EqualTo(1080));
            Assert.That(File.ReadAllText(gameSave), Is.EqualTo("existing progress"));
            GameSettingsStore.Initialize(true, Path.Combine(directory, "other-run"));
            Assert.That(GameSettingsStore.Current.language, Is.EqualTo(GameLanguage.Vietnamese));
        }

        [Test]
        public void DisplaySyncUsesActualRefreshPolicyAndCapOnlyAppliesWhenDisabled()
        {
            int revision = GameSettingsStore.Revision;
            GameSettingsStore.Current.syncDisplay = true;
            GameSettingsStore.Current.fpsLimit = 165;
            GameSettingsStore.Apply();
            Assert.That(QualitySettings.vSyncCount, Is.EqualTo(1));
            Assert.That(Application.targetFrameRate, Is.EqualTo(-1));
            GameSettingsStore.Current.syncDisplay = false;
            GameSettingsStore.Apply();
            Assert.That(QualitySettings.vSyncCount, Is.Zero);
            Assert.That(Application.targetFrameRate, Is.EqualTo(165));
            Assert.That(GameSettingsStore.Revision, Is.GreaterThan(revision));
        }

        [Test]
        public void InvalidPreferencesAreBoundedBeforeApplying()
        {
            var settings = new GameSettingsData
            {
                language = (GameLanguage)98,
                quality = (GameGraphicsPreset)(-4),
                fpsLimit = -1,
                renderScale = float.NaN,
                resolutionWidth = -100,
                resolutionHeight = 0
            };
            settings.Validate();
            Assert.That(settings.language, Is.EqualTo(GameLanguage.Vietnamese));
            Assert.That(settings.quality, Is.EqualTo(GameGraphicsPreset.Balanced));
            Assert.That(settings.fpsLimit, Is.EqualTo(30));
            Assert.That(settings.renderScale, Is.EqualTo(1));
            Assert.That(settings.resolutionWidth, Is.Zero);
            Assert.That(settings.resolutionHeight, Is.Zero);
        }

        [Test]
        public void EveryCatalogItemHasEnglishDisplayNameWithoutChangingIdentity()
        {
            GameSettingsStore.Current.language = GameLanguage.English;
            foreach (var item in Definitions.Items)
            {
                Assert.That(GameLocalization.Translate(item.label), Is.Not.EqualTo(item.label), item.id);
                Assert.That(GameLocalization.Translate(item.id), Is.EqualTo(item.id));
            }
            Assert.That(GameLocalization.Translate("SettingsPanel"), Is.EqualTo("SettingsPanel"));
            Assert.That(GameLocalization.Translate("receipt_123 storage_farm"), Is.EqualTo("receipt_123 storage_farm"));
        }

        [Test]
        public void ComposedHudAndInteractionLabelsSwitchInBothDirections()
        {
            const string source = "Tay trống  0/24\nChọn: Cà rốt";
            GameSettingsStore.Current.language = GameLanguage.English;
            Assert.That(GameLocalization.Translate(source), Is.EqualTo("Empty hands  0/24\nSelected: Carrot"));
            Assert.That(GameLocalization.Translate("Đặt hàng • Cà rốt"), Is.EqualTo("Deposit goods • Carrot"));
            Assert.That(GameLocalization.Translate("Gửi tiền vào két để lấy hàng."),
                Is.EqualTo("Deposit your cash in the safe to pick up goods."));
            GameSettingsStore.Current.language = GameLanguage.Vietnamese;
            Assert.That(GameLocalization.Translate(source), Is.EqualTo(source));
        }
    }
}
