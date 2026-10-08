using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class CityPlaySession
    {
        [Serializable]
        sealed class PlayReport
        {
            public bool passed;
            public string mode = "ordinary gameplay with virtual gamepad, live simulation and persistence";
            public string gpu, api;
            public int width, height, frames, workers, customers;
            public double durationSeconds, walkedMetres, averageFps, p95FrameMs, worstFrameMs;
            public double p99FrameMs;
            public int framesOver16Ms, framesOver33Ms, framesOver100Ms;
            public double averageCpuMs, averageGpuMs;
            public double fullCitySeconds, fullCityAverageFps;
            public int fullCityFrames;
            public int timingSamples, successfulInteractions;
            public bool near165Fps;
            public string[] completed, failures, runtimeErrors, screenshots;
        }

        readonly float[] frameTimes = new float[150000];
        readonly FrameTiming[] frameTiming = new FrameTiming[1];
        readonly StringBuilder timeline = new("second,frameMs,cpuMs,gpuMs,x,z,workers,customers,goods,cash\n");
        readonly StringBuilder hitches = new("frame,second,frameMs,workers,x,z\n");
        int frameCount, timingSamples;
        double elapsed, cpu, gpu;
        double fullCityElapsed;
        int fullCityFrameCount;
        float nextSample;
        readonly CityActorEvidence actors = new();

        void Update()
        {
            if (!recording || !game || !game.CanSimulate || Time.timeScale != 1) return;
            float delta = Time.unscaledDeltaTime;
            if (delta <= 0 || frameCount >= frameTimes.Length) return;
            frameTimes[frameCount++] = delta * 1000;
            elapsed += delta;
            if (delta > 1f / 30)
            {
                var point = game.Player.transform.position;
                hitches.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1:F3},{2:F3},{3},{4:F2},{5:F2}\n", Time.frameCount - 1,
                    Time.realtimeSinceStartupAsDouble, delta * 1000, game.Workers.Count, point.x, point.z);
            }
            if (game.ActiveWorkerCount == WorkforceRules.MaximumActive)
            {
                fullCityElapsed += delta;
                fullCityFrameCount++;
            }
            if (Time.realtimeSinceStartup < nextSample) return;
            nextSample = Time.realtimeSinceStartup + 1;
            actors.Sample(game);
            FrameTimingManager.CaptureFrameTimings();
            uint count = FrameTimingManager.GetLatestTimings(1, frameTiming);
            double cpuMs = count > 0 ? frameTiming[0].cpuMainThreadFrameTime : -1;
            double gpuMs = count > 0 ? frameTiming[0].gpuFrameTime : -1;
            if (cpuMs > 0 && gpuMs > 0)
            {
                cpu += cpuMs;
                gpu += gpuMs;
                timingSamples++;
            }
            var position = game.Player.transform.position;
            timeline.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                "{0:F2},{1:F3},{2:F3},{3:F3},{4:F2},{5:F2},{6},{7},{8},{9}\n",
                Time.realtimeSinceStartupAsDouble - started, delta * 1000, cpuMs, gpuMs,
                position.x, position.z, game.Workers.Count, CustomerCount(),
                game.Player.Carry.Total, game.Economy.CashInHand);
        }

        int CustomerCount() => game.GetComponentsInChildren<CustomerAgent>().Count(x => x.gameObject.activeInHierarchy);

        void FinishReport()
        {
            CityOperationTiming.Save(game.QaDirectory);
            Check(actors.Save(game.QaDirectory), "NPC đi bộ thật và chuyển động xương chân trong Player");
            Check(CityMobilityEvidence.Save(game),
                "39 nhân viên; xe buýt, xe khách, taxi có lượt lên xuống và phục hồi va chạm");
            SaveCityProfile();
            var samples = frameTimes.Take(frameCount).ToArray();
            Array.Sort(samples);
            double fps = elapsed > 0 ? frameCount / elapsed : 0;
            var report = new PlayReport
            {
                passed = failures.Count == 0 && game.RuntimeErrors.Count == 0,
                gpu = SystemInfo.graphicsDeviceName,
                api = SystemInfo.graphicsDeviceType.ToString(),
                width = Screen.width,
                height = Screen.height,
                frames = frameCount,
                durationSeconds = elapsed,
                walkedMetres = game.Player.DistanceWalked,
                averageFps = fps,
                p95FrameMs = samples.Length > 0 ? samples[(int)((samples.Length - 1) * .95)] : 0,
                p99FrameMs = samples.Length > 0 ? samples[(int)((samples.Length - 1) * .99)] : 0,
                framesOver16Ms = samples.Count(x => x > 1000f / 60),
                framesOver33Ms = samples.Count(x => x > 1000f / 30),
                framesOver100Ms = samples.Count(x => x > 100),
                worstFrameMs = samples.Length > 0 ? samples[^1] : 0,
                averageCpuMs = timingSamples > 0 ? cpu / timingSamples : -1,
                averageGpuMs = timingSamples > 0 ? gpu / timingSamples : -1,
                fullCitySeconds = fullCityElapsed,
                fullCityFrames = fullCityFrameCount,
                fullCityAverageFps = fullCityElapsed > 0 ? fullCityFrameCount / fullCityElapsed : 0,
                timingSamples = timingSamples,
                near165Fps = fps >= 148.5,
                workers = game.Workers.Count,
                customers = CustomerCount(),
                successfulInteractions = game.Player.SuccessfulInteractions,
                completed = completed.ToArray(),
                failures = failures.ToArray(),
                runtimeErrors = game.RuntimeErrors.ToArray(),
                screenshots = captures.ToArray()
            };
            File.WriteAllText(Path.Combine(game.QaDirectory, "ordinary-play-report.json"),
                JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(game.QaDirectory, "ordinary-play-timeline.csv"), timeline.ToString());
            File.WriteAllText(Path.Combine(game.QaDirectory, "ordinary-play-hitches.csv"), hitches.ToString());
            Debug.Log("CITY_PLAY_RESULT " + report.passed + " FPS=" + fps.ToString("F2") + " seconds=" + elapsed);
        }
    }
}
