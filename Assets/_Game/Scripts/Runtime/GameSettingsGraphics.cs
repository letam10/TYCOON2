using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tycoon
{
    public static class GameSettingsGraphics
    {
        static UniversalRenderPipelineAsset ownedPipeline;
        static RenderPipelineAsset originalPipeline;
        static int originalMsaa;

        public static void Apply(GameSettingsData settings)
        {
            if (!ownedPipeline)
            {
                var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (!source) return;
                originalPipeline = QualitySettings.renderPipeline;
                ownedPipeline = Object.Instantiate(source);
                ownedPipeline.name = source.name + " (Runtime Settings)";
                ownedPipeline.hideFlags = HideFlags.DontSave;
                originalMsaa = source.msaaSampleCount;
                // Chá»‰ sá»­a báº£n sao runtime; renderer data vÃ  tÃ i nguyÃªn trong Assets giá»¯ nguyÃªn.
                QualitySettings.renderPipeline = ownedPipeline;
            }
            bool low = settings.quality == GameGraphicsPreset.Low;
            bool high = settings.quality == GameGraphicsPreset.High;
            // Cờ QA cho phép đo A/B cùng bản Player, không đổi thiết lập người chơi.
            bool disableOcclusion = System.Array.Exists(System.Environment.GetCommandLineArgs(),
                x => x == "--qa-disable-gpu-occlusion");
            ownedPipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = !disableOcclusion;
            ownedPipeline.renderScale = settings.renderScale;
            ownedPipeline.msaaSampleCount = low ? 1 : high ? 4 : originalMsaa;
            // Một cascade bỏ ranh giới bóng nhảy khi pan/zoom; atlas lớn giữ đường viền rõ.
            ownedPipeline.shadowDistance = low ? 45 : high ? 90 : 55;
            ownedPipeline.shadowCascadeCount = 1;
            ownedPipeline.mainLightShadowmapResolution = low ? 1024 : high ? 4096 : 2048;
            ownedPipeline.cascadeBorder = .25f;
        }

        public static void Release()
        {
            if (!ownedPipeline) return;
            if (QualitySettings.renderPipeline == ownedPipeline) QualitySettings.renderPipeline = originalPipeline;
            if (Application.isPlaying) Object.Destroy(ownedPipeline);
            else Object.DestroyImmediate(ownedPipeline);
            ownedPipeline = null;
            originalPipeline = null;
        }
    }
}
