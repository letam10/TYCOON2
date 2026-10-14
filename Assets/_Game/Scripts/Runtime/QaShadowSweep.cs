using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable]
        sealed class ShadowFacts
        {
            public string capture = "ScreenCapture of actual visible Player, no extra camera render";
            public string[] directionalLights;
            public float distance, normalBias, depthBias, cascadeBorder, pipelineDepthBias, pipelineNormalBias;
            public float nearSplit;
            public int cascades, resolution;
        }

        IEnumerator ShadowSweep()
        {
            yield return TownWait(() => game.NavigationReady, 45, "shadow navigation ready");
            var camera = game.CameraRig;
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var sun = RenderSettings.sun;
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(x => x.enabled && x.gameObject.activeInHierarchy && x.type == LightType.Directional)
                .Select(x => x.name + ":" + x.shadows).ToArray();
            Check(lights.Length == 1, "Exactly one active directional light");
            File.WriteAllText(Path.Combine(game.QaDirectory, "shadow-facts.json"), JsonUtility.ToJson(new ShadowFacts
            {
                directionalLights = lights,
                distance = pipeline.shadowDistance,
                cascades = pipeline.shadowCascadeCount,
                resolution = pipeline.mainLightShadowmapResolution,
                normalBias = sun.shadowNormalBias,
                depthBias = sun.shadowBias,
                cascadeBorder = pipeline.cascadeBorder,
                pipelineDepthBias = pipeline.shadowDepthBias,
                pipelineNormalBias = pipeline.shadowNormalBias,
                nearSplit = pipeline.cascade2Split
            }, true));
            var board = new GameObject("ShadowInspection").transform;
            board.position = new Vector3(0, 0, 700);
            var ground = Art.Box("ShadowGround", new Vector3(0, -.1f, 0),
                new Vector3(40, .2f, 40), "#A3AE9A", board);
            Art.Box("SingleShadowCaster", new Vector3(0, 1.5f, 0),
                new Vector3(2, 3, 2), "#DEB482", board);
            var renderer = ground.GetComponent<MeshRenderer>();
            var town = new Material(renderer.sharedMaterial);
            town.SetFloat("_BumpScale", 0);
            town.SetFloat("_IrregularDetail", 0);
            var lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lit.SetColor("_BaseColor", new Color(.64f, .68f, .6f));
            lit.SetFloat("_Smoothness", .2f);
            var savedTarget = camera.Target;
            bool savedEnabled = camera.enabled;
            bool savedOverview = camera.Overview;
            bool savedOcclusion = pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras;
            bool savedFog = RenderSettings.fog;
            float savedScale = Time.timeScale;
            float savedDistance = camera.Distance;
            float savedPitch = camera.Pitch;
            float savedYaw = camera.Yaw;
            Vector3 savedFocus = camera.FocusOffset;
            var timestamps = new StringBuilder("frame,elapsedSeconds\n");
            try
            {
                Time.timeScale = 0;
                RenderSettings.fog = false;
                camera.enabled = false;
                camera.Target = board;
                camera.Overview = false;
                camera.FocusOffset = Vector3.up;
                camera.Pitch = 55;
                camera.Yaw = 40;
                foreach (bool occlusion in new[] { true, false })
                {
                    pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = occlusion;
                    foreach (var material in new[] { town, lit })
                    {
                        renderer.sharedMaterial = material;
                        foreach (float distance in new[] { 12f, 19f, 27f, 48f })
                        {
                            camera.Distance = distance;
                            camera.Snap();
                            for (int i = 0; i < 8; i++) yield return null;
                            string surface = material == town ? "town" : "lit";
                            yield return CaptureShadowScreen($"shadow-{surface}-cull-{occlusion}-{distance:0}.png");
                        }
                    }
                }
                renderer.sharedMaterial = town;
                pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = savedOcclusion;
                double motionStart = Time.realtimeSinceStartupAsDouble;
                for (int i = 0; i <= 30; i++)
                {
                    camera.Distance = Mathf.Lerp(12, 27, i / 30f);
                    camera.Snap();
                    string name = $"shadow-motion-{i:000}.png";
                    yield return CaptureShadowScreen(name);
                    timestamps.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                        "{0},{1:F6}\n", name, Time.realtimeSinceStartupAsDouble - motionStart);
                }
                File.WriteAllText(Path.Combine(game.QaDirectory, "shadow-timestamps.csv"), timestamps.ToString());
                yield return ShadowStabilitySequence(board, pipeline);
                yield return ShadowWorldComparison(savedTarget, pipeline);
            }
            finally
            {
                camera.Target = savedTarget;
                camera.Distance = savedDistance;
                camera.Pitch = savedPitch;
                camera.Yaw = savedYaw;
                camera.FocusOffset = savedFocus;
                camera.Overview = savedOverview;
                camera.enabled = savedEnabled;
                camera.Snap();
                pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = savedOcclusion;
                RenderSettings.fog = savedFog;
                Time.timeScale = savedScale;
                Destroy(board.gameObject);
                Destroy(town);
                Destroy(lit);
            }
        }

        IEnumerator CaptureShadowScreen(string name)
        {
            yield return new WaitForEndOfFrame();
            var pixels = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Check(pixels.GetPixels32().Any(x => x.r > 8 || x.g > 8 || x.b > 8),
                    "Visible shadow capture " + name);
                File.WriteAllBytes(Path.Combine(game.QaDirectory, name), pixels.EncodeToPNG());
                report.screenshots.Add(name);
            }
            finally
            {
                Destroy(pixels);
            }
        }
    }
}
