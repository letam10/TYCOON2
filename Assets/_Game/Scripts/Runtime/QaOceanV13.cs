using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable]
        public sealed class OceanRenderCostRun
        {
            public int sequenceIndex;
            public bool horizonEnabled;
            public bool fogEnabled;
            public int frames;
            public double seconds;
            public double cpuMs = -1;
            public double gpuMs = -1;
            public int timingSamples;
            public int cpuTimingSamples;
            public int gpuTimingSamples;
        }

        IEnumerator OceanV13Acceptance()
        {
            report.renderCostRuns = new List<OceanRenderCostRun>();
            report.renderCostScope = "Frozen wide Marina scene, horizon off/on/on/off, 2s settle + 15s each; "
                + "whole-scene CPU/GPU means, -1 means unavailable; excludes screenshots; "
                + "does not establish active gameplay FPS or 165Hz presentation";
            yield return TownWait(() => game.NavigationReady, 45, "V13 ocean navigation ready");
            var landmarks = game.GetComponentsInChildren<CityLandmark>();
            string[] ids = { "Harbor", "Beach", "Marina" };
            foreach (string id in ids)
                Check(landmarks.Count(x => x.Id == id) == 1, "V13 unique coast landmark " + id);
            var horizons = game.GetComponentsInChildren<MeshRenderer>(true)
                .Where(x => x.name == "OceanVisualHorizon").ToArray();
            Check(horizons.Length == 1, "V13 exactly one OceanVisualHorizon renderer");
            var horizon = horizons[0];
            Check(horizon.gameObject.activeInHierarchy, "V13 horizon object active in scene");
            var camera = game.CameraRig;
            var lens = camera.GetComponent<Camera>();
            float savedScale = Time.timeScale;
            bool savedControl = game.Player.CanControl;
            bool savedHorizon = horizon.enabled;
            bool savedFog = RenderSettings.fog;
            float savedFar = lens.farClipPlane;
            float savedDistance = camera.Distance;
            float savedPitch = camera.Pitch;
            float savedYaw = camera.Yaw;
            Vector3 savedFocus = camera.FocusOffset;
            bool savedOverview = camera.Overview;
            try
            {
                Time.timeScale = 0;
                game.Player.CanControl = false;
                horizon.enabled = true;
                foreach (string id in ids)
                {
                    var landmark = landmarks.Single(x => x.Id == id);
                    var bounds = ModelBounds(landmark.transform);
                    yield return CaptureModelAngles("ocean-v13-" + id, bounds.center,
                        Mathf.Max(20, bounds.size.magnitude * 1.8f), false, 270);
                }
                string[] views = { "front", "right", "rear", "left", "top" };
                Check(ids.All(id => views.All(view =>
                    report.screenshots.Contains("ocean-v13-" + id + "-" + view + ".png"))),
                    "V13 all fifteen placed coast screenshots captured");

                // Nhìn từ phía đất liền ra đông, đủ xa để thấy marina và chân trời cùng lúc.
                var marina = ModelBounds(landmarks.Single(x => x.Id == "Marina").transform);
                camera.Overview = false;
                camera.Pitch = 24;
                camera.Yaw = 90;
                camera.Distance = Mathf.Max(120, marina.size.magnitude * 1.8f);
                camera.FocusOffset = marina.center - camera.Target.position;
                camera.Snap();
                lens.farClipPlane = Mathf.Max(savedFar, 1600);
                Check(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(lens), horizon.bounds),
                    "V13 horizon intersects wide Marina camera frustum");
                RenderSettings.fog = true;
                yield return Capture("ocean-v13-Marina-wide-fog-on.png");
                RenderSettings.fog = false;
                yield return Capture("ocean-v13-Marina-wide-fog-off.png");
                RenderSettings.fog = true;
                bool[] states = { false, true, true, false };
                for (int index = 0; index < states.Length; index++)
                {
                    horizon.enabled = states[index];
                    var run = new OceanRenderCostRun
                    {
                        sequenceIndex = index,
                        horizonEnabled = states[index],
                        fogEnabled = RenderSettings.fog
                    };
                    report.renderCostRuns.Add(run);
                    yield return MeasureOceanV13(run);
                }
                Check(report.renderCostRuns.Count == 4 && report.renderCostRuns.All(x =>
                    x.frames > 0 && x.seconds >= 15), "V13 four complete 15-second ABBA frame windows");
                Check(report.renderCostRuns.All(x => x.cpuTimingSamples > 0 && x.gpuTimingSamples > 0),
                    "V13 valid CPU and GPU timing samples in every ABBA window");
            }
            finally
            {
                horizon.enabled = savedHorizon;
                camera.Distance = savedDistance;
                camera.Pitch = savedPitch;
                camera.Yaw = savedYaw;
                camera.FocusOffset = savedFocus;
                camera.Overview = savedOverview;
                camera.Snap();
                lens.farClipPlane = savedFar;
                RenderSettings.fog = savedFog;
                game.Player.CanControl = savedControl;
                Time.timeScale = savedScale;
            }
        }

        IEnumerator MeasureOceanV13(OceanRenderCostRun run)
        {
            var timings = new FrameTiming[1];
            ulong previousTimestamp = 0;
            double settleUntil = Time.realtimeSinceStartupAsDouble + 2;
            while (Time.realtimeSinceStartupAsDouble < settleUntil)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                    previousTimestamp = timings[0].frameStartTimestamp;
            }
            double cpu = 0;
            double gpu = 0;
            double started = Time.realtimeSinceStartupAsDouble;
            while (Time.realtimeSinceStartupAsDouble - started < 15)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                run.frames++;
                run.seconds = Time.realtimeSinceStartupAsDouble - started;
                if (FrameTimingManager.GetLatestTimings(1, timings) == 0
                    || timings[0].frameStartTimestamp <= previousTimestamp) continue;
                previousTimestamp = timings[0].frameStartTimestamp;
                double cpuTime = timings[0].cpuFrameTime;
                double gpuTime = timings[0].gpuFrameTime;
                bool validCpu = cpuTime > 0 && !double.IsNaN(cpuTime) && !double.IsInfinity(cpuTime);
                bool validGpu = gpuTime > 0 && !double.IsNaN(gpuTime) && !double.IsInfinity(gpuTime);
                if (validCpu)
                {
                    cpu += cpuTime;
                    run.cpuTimingSamples++;
                    run.cpuMs = cpu / run.cpuTimingSamples;
                }
                if (validGpu)
                {
                    gpu += gpuTime;
                    run.gpuTimingSamples++;
                    run.gpuMs = gpu / run.gpuTimingSamples;
                }
                if (validCpu && validGpu) run.timingSamples++;
            }
            run.seconds = Time.realtimeSinceStartupAsDouble - started;
        }
    }
}
