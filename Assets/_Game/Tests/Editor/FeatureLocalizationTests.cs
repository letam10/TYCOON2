using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class FeatureLocalizationTests
    {
        [TestCase("Slot 3 • đang chơi", "Slot 3 • active")]
        [TestCase("Lưu nhanh dùng slot đang chơi: 3", "Quick save uses the active slot: 3")]
        [TestCase("Nhấn phím mới, hoặc Esc để hủy.", "Press a new key, or Esc to cancel.")]
        [TestCase("Đã hủy đổi phím.", "Key change cancelled.")]
        [TestCase("Đã lưu phím mới.", "New key saved.")]
        [TestCase("Đã khôi phục phím mặc định.", "Default keys restored.")]
        [TestCase("Phím đã được dùng: Forward", "Key already in use: Forward")]
        [TestCase("Đã đến Beach", "Arrived at Beach")]
        [TestCase("Đã lưu vào slot 2", "Saved to slot 2")]
        [TestCase("Bấm lại Chơi mới ở slot 2 để ghi đè. Save trước sẽ được giữ dự phòng.",
            "Press New game again in slot 2 to overwrite. The previous save will be kept as a backup.")]
        public void NewFeatureFeedbackUsesTheSelectedLanguage(string source, string expected)
        {
            GameLanguage previous = GameSettingsStore.Current.language;
            try
            {
                GameSettingsStore.Current.language = GameLanguage.English;
                Assert.That(GameLocalization.Translate(source), Is.EqualTo(expected));
                GameSettingsStore.Current.language = GameLanguage.Vietnamese;
                Assert.That(GameLocalization.Translate(source), Is.EqualTo(source));
            }
            finally
            {
                GameSettingsStore.Current.language = previous;
            }
        }
    }
}
