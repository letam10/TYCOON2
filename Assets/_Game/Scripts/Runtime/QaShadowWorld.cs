using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator ShadowWorldComparison(Transform target, UniversalRenderPipelineAsset pipeline)
        {
            // Chỉ thay trạng thái trong tiến trình QA, phục hồi mọi cấu hình khi xong.
            float savedScale = Time.timeScale;
            Time.timeScale = 1;
            Check(DevelopmentAssistance.ApplyMaximum(game), "Shadow: populate actual city");
            yield return new WaitForSecondsRealtime(3);
            Time.timeScale = 0;
            var camera = game.CameraRig;
            camera.Target = target;
            var features = Resources.FindObjectsOfTypeAll<ScriptableRendererData>()
                .SelectMany(x => x.rendererFeatures).Where(x => x && x.GetType().Name
                    == "ScreenSpaceAmbientOcclusion").Distinct().ToArray();
            var active = features.Select(x => x.isActive).ToArray();
            float split = pipeline.cascade2Split;
            float depth = pipeline.shadowDepthBias;
            float normal = pipeline.shadowNormalBias;
            float range = pipeline.shadowDistance;
            float border = pipeline.cascadeBorder;
            int cascades = pipeline.shadowCascadeCount;
            int resolution = pipeline.mainLightShadowmapResolution;
            bool occlusion = pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras;
            var centers = new[] { CityDistricts.PlayerSpawn, CityDistricts.Center("processing"),
                CityDistricts.ParkCenter };
            var labels = new[] { "spawn", "factory", "park" };
            try
            {
                foreach (string variant in new[] { "current", "legacy", "noAO", "noCull", "nearSplit", "four", "bias" })
                {
                    pipeline.shadowDistance = variant == "legacy" ? 45 : range;
                    pipeline.mainLightShadowmapResolution = variant == "legacy" ? 2048 : resolution;
                    pipeline.cascadeBorder = variant == "legacy" ? .1077586f : border;
                    pipeline.cascade2Split = variant == "nearSplit" ? .6f : split;
                    pipeline.shadowCascadeCount = variant == "four" ? 4 : variant == "legacy" ? 2 : cascades;
                    pipeline.shadowDepthBias = variant == "bias" ? .025f : depth;
                    pipeline.shadowNormalBias = variant == "bias" ? .15f : normal;
                    pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = variant != "noCull" && occlusion;
                    for (int i = 0; i < features.Length; i++)
                        features[i].SetActive(variant != "noAO" && active[i]);
                    for (int site = 0; site < centers.Length; site++)
                    {
                        camera.FocusOffset = centers[site] - target.position + Vector3.up;
                        foreach (float distance in new[] { 12f, 27f })
                        {
                            camera.Distance = distance;
                            camera.Yaw = 40;
                            camera.Snap();
                            for (int frame = 0; frame < 8; frame++) yield return null;
                            yield return CaptureShadowScreen($"world-shadow-{variant}-{labels[site]}-{distance:0}.png");
                        }
                    }
                }
                pipeline.shadowDistance = range;
                pipeline.mainLightShadowmapResolution = resolution;
                pipeline.shadowCascadeCount = cascades;
                pipeline.cascadeBorder = border;
                pipeline.shadowDepthBias = depth;
                pipeline.shadowNormalBias = normal;
                camera.FocusOffset = CityDistricts.ParkCenter - target.position + Vector3.up;
                for (int frame = 0; frame <= 60; frame++)
                {
                    float phase = frame / 60f;
                    camera.Distance = Mathf.Lerp(12, 27, Mathf.Sin(phase * Mathf.PI));
                    camera.FocusOffset = CityDistricts.ParkCenter - target.position +
                        new Vector3(Mathf.Sin(phase * Mathf.PI * 2) * 2, 1, 0);
                    camera.Snap();
                    yield return CaptureShadowScreen($"world-shadow-motion-{frame:000}.png");
                }
            }
            finally
            {
                for (int i = 0; i < features.Length; i++) features[i].SetActive(active[i]);
                pipeline.cascade2Split = split;
                pipeline.shadowCascadeCount = cascades;
                pipeline.mainLightShadowmapResolution = resolution;
                pipeline.shadowDepthBias = depth;
                pipeline.shadowNormalBias = normal;
                pipeline.shadowDistance = range;
                pipeline.cascadeBorder = border;
                pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = occlusion;
                Time.timeScale = savedScale;
            }
        }
    }
}
