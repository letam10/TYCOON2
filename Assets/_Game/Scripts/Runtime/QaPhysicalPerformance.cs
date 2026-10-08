using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable]
        sealed class PhysicalPerformance
        {
            public string gpu;
            public string api;
            public int width;
            public int height;
            public int frames;
            public int customers;
            public int workers;
            public int activeWorkers;
            public int carried;
            public float seconds;
            public float averageFps;
            public float p95FrameMs;
            public float maximumFrameMs;
            public float averageMainThreadMs;
            public float averageGpuMs;
            public float averageDrawCalls;
            public float averageGcBytes;
            public bool target60Fps;
            public bool mainThreadCounterAvailable;
            public bool gpuTimingAvailable;
            public bool drawCallCounterAvailable;
            public bool gcCounterAvailable;
            public CpuWorkSample[] workSamples;
            public CpuWorkSample[] frameWorkSamples;
            public int renderers;
            public int visibleRenderers;
            public int textMeshes;
            public int animators;
        }

        QaFrameProbe.Scope renderingScope;

        void BeginRenderProbe(ScriptableRenderContext context, Camera[] cameras)
        {
            renderingScope = QaFrameProbe.Measure("Rendering.Frame");
        }

        void EndRenderProbe(ScriptableRenderContext context, Camera[] cameras)
        {
            renderingScope.Dispose();
        }

        IEnumerator PhysicalPerformanceAcceptance(TransactionState baseline)
        {
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.6f);
            PhysicalCarryFixture(baseline, "carrot", 48);
            Time.timeScale = 1;
            game.Commerce.enabled = true;
            game.Restaurant.enabled = true;
            var counter = game.Checkouts.First(x => x.ShopId == "farm");
            for (int i = game.Commerce.ActiveCount; i < 30; i++)
                TownCustomer(counter, "carrot", 3, 300);
            yield return new WaitForSeconds(2);
            Check(game.Workers.All(x => x.enabled), "physical performance: all workers active");
            var timings = new List<float>(4096);
            var frameTiming = new FrameTiming[1];
            using var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            using var allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            double mainTotal = 0;
            double drawTotal = 0;
            double allocationTotal = 0;
            double gpuTotal = 0;
            int gpuFrames = 0;
            float began = Time.realtimeSinceStartup;
            QaCpuProbe.Reset();
            QaCpuProbe.Start();
            QaFrameProbe.Reset();
            QaFrameProbe.Start();
            RenderPipelineManager.beginFrameRendering += BeginRenderProbe;
            RenderPipelineManager.endFrameRendering += EndRenderProbe;
            // Chỉ đo vòng chơi đang chạy, chụp ảnh sau khi kết thúc cửa sổ đo.
            while (Time.realtimeSinceStartup - began < 10)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                timings.Add(Time.unscaledDeltaTime * 1000);
                if (main.Valid) mainTotal += main.LastValue;
                if (draws.Valid) drawTotal += draws.LastValue;
                if (allocations.Valid) allocationTotal += allocations.LastValue;
                if (FrameTimingManager.GetLatestTimings(1, frameTiming) > 0 && frameTiming[0].gpuFrameTime > 0)
                {
                    gpuTotal += frameTiming[0].gpuFrameTime;
                    gpuFrames++;
                }
            }
            QaCpuProbe.Stop();
            QaFrameProbe.Stop();
            RenderPipelineManager.beginFrameRendering -= BeginRenderProbe;
            RenderPipelineManager.endFrameRendering -= EndRenderProbe;
            timings.Sort();
            var performance = new PhysicalPerformance
            {
                gpu = SystemInfo.graphicsDeviceName,
                api = SystemInfo.graphicsDeviceType.ToString(),
                width = Screen.width,
                height = Screen.height,
                frames = timings.Count,
                customers = game.Commerce.ActiveCount,
                workers = game.Workers.Count,
                activeWorkers = game.Workers.Count(x => x.enabled),
                carried = game.Player.Carry.Total,
                seconds = Time.realtimeSinceStartup - began,
                p95FrameMs = timings[Mathf.CeilToInt(timings.Count * .95f) - 1],
                maximumFrameMs = timings[timings.Count - 1],
                averageMainThreadMs = (float)(mainTotal / timings.Count / 1000000),
                averageDrawCalls = (float)(drawTotal / timings.Count),
                averageGcBytes = (float)(allocationTotal / timings.Count),
                averageGpuMs = gpuFrames > 0 ? (float)(gpuTotal / gpuFrames) : 0,
                mainThreadCounterAvailable = main.Valid,
                gpuTimingAvailable = gpuFrames > 0,
                drawCallCounterAvailable = draws.Valid && drawTotal > 0,
                gcCounterAvailable = allocations.Valid && allocationTotal > 0,
                workSamples = QaCpuProbe.Snapshot(),
                frameWorkSamples = QaFrameProbe.Snapshot(),
                renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length,
                visibleRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Count(x => x.isVisible),
                textMeshes = FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Length,
                animators = FindObjectsByType<Animator>(FindObjectsSortMode.None).Length
            };
            performance.averageFps = performance.frames / performance.seconds;
            performance.target60Fps = performance.averageFps >= 60;
            File.WriteAllText(Path.Combine(game.QaDirectory, "physical-performance.json"),
                JsonUtility.ToJson(performance, true));
            yield return Capture("physical-crowd-full-carry.png");
            Check(performance.width == 1920 && performance.height == 1080,
                "physical performance: actual 1920x1080 framebuffer");
            Check(performance.activeWorkers == 78 && performance.customers >= 30 && performance.carried == 48,
                "physical performance: 78 workers 30 customers and 48 carried items");
            Check(performance.target60Fps, "physical performance: mean at least 60 FPS");
        }
    }
}
