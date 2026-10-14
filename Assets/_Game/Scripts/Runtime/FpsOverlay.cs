using UnityEngine;
using UnityEngine.UI;

namespace Tycoon
{
    public sealed class FpsOverlay : MonoBehaviour
    {
        Text readout;
        Image progress;
        double elapsed;
        int frames;

        public void Initialize(Text label, Image bar)
        {
            readout = label;
            progress = bar;
        }

        void OnEnable()
        {
            elapsed = 0;
            frames = 0;
            if (readout) readout.text = "FPS — | — ms | — Hz";
            if (progress) progress.fillAmount = 0;
        }

        void Update()
        {
            using var updateProbe = QaFrameProbe.Measure("FpsOverlay.1.Update");

            if (!readout || !progress) return;
            elapsed += Time.unscaledDeltaTime;
            frames++;
            if (elapsed < .35) return;
            // Đo frame thực; chỉ định dạng chuỗi mỗi cửa sổ mẫu, kể cả khi game tạm dừng.
            double fps = frames / elapsed;
            double milliseconds = elapsed * 1000 / frames;
            double hz = GameSettingsStore.DisplayRefreshHz;
            readout.text = hz > 0
                ? $"{fps:0.0} FPS | {milliseconds:0.0} ms | {hz:0.#} Hz"
                : $"{fps:0.0} FPS | {milliseconds:0.0} ms | — Hz";
            double reference = GameSettingsStore.Current.syncDisplay && hz > 0
                ? hz : GameSettingsStore.Current.fpsLimit;
            progress.fillAmount = Mathf.Clamp01((float)(fps / reference));
            elapsed = 0;
            frames = 0;
        }
    }
}
