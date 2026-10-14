using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        [Serializable]
        sealed class WaterDetailRun
        {
            public float detail;
            public int frames;
            public double seconds, averageFps, p95Ms, cpuMs, gpuMs;
            public int timingSamples;
        }

        [Serializable]
        sealed class WaterDetailReport
        {
            public string scope = "Visible beach camera, live city, repeated ABBA; excludes capture time";
            public string gpu;
            public int width, height;
            public WaterDetailRun[] runs;
        }

        IEnumerator WaterDetailComparisonV11()
        {
            var ocean = TownSurfaceMaterials.Get("ocean", "#387E86");
            float savedDetail = ocean.GetFloat("_Detail");
            var camera = game.CameraRig;
            float savedDistance = camera.Distance;
            float savedPitch = camera.Pitch;
            float savedYaw = camera.Yaw;
            Vector3 savedFocus = camera.FocusOffset;
            bool savedOverview = camera.Overview;
            var runs = new List<WaterDetailRun>();
            try
            {
                camera.Overview = false;
                camera.Pitch = 42;
                camera.Yaw = 235;
                camera.Distance = 27;
                camera.FocusOffset = new Vector3(252, .1f, 70) - camera.Target.position;
                camera.Snap();
                yield return TownWait(() => game.NavigationReady, 45, "V11 water navigation ready");
                foreach (float detail in new[] { 0f, 1f, 1f, 0f })
                {
                    ocean.SetFloat("_Detail", detail);
                    yield return new WaitForSecondsRealtime(2);
                    var frameTimes = new List<double>();
                    var timings = new FrameTiming[1];
                    double cpu = 0;
                    double gpu = 0;
                    int samples = 0;
                    ulong previousTimestamp = 0;
                    double started = Time.realtimeSinceStartupAsDouble;
                    while (Time.realtimeSinceStartupAsDouble - started < 15)
                    {
                        FrameTimingManager.CaptureFrameTimings();
                        yield return null;
                        frameTimes.Add(Time.unscaledDeltaTime * 1000d);
                        if (FrameTimingManager.GetLatestTimings(1, timings) == 0
                            || timings[0].frameStartTimestamp == previousTimestamp
                            || timings[0].gpuFrameTime <= 0) continue;
                        previousTimestamp = timings[0].frameStartTimestamp;
                        cpu += timings[0].cpuMainThreadFrameTime;
                        gpu += timings[0].gpuFrameTime;
                        samples++;
                    }
                    double seconds = Time.realtimeSinceStartupAsDouble - started;
                    frameTimes.Sort();
                    runs.Add(new WaterDetailRun
                    {
                        detail = detail,
                        frames = frameTimes.Count,
                        seconds = seconds,
                        averageFps = frameTimes.Count / seconds,
                        p95Ms = frameTimes[Math.Min(frameTimes.Count - 1, (int)(frameTimes.Count * .95))],
                        cpuMs = samples > 0 ? cpu / samples : 0,
                        gpuMs = samples > 0 ? gpu / samples : 0,
                        timingSamples = samples
                    });
                }
                File.WriteAllText(Path.Combine(game.QaDirectory, "water-detail-performance.json"),
                    JsonUtility.ToJson(new WaterDetailReport
                    {
                        gpu = SystemInfo.graphicsDeviceName,
                        width = Screen.width,
                        height = Screen.height,
                        runs = runs.ToArray()
                    }, true));
                ocean.SetFloat("_Detail", 1);
                foreach (float distance in new[] { 12f, 27f, 48f })
                {
                    camera.Distance = distance;
                    camera.Snap();
                    yield return Capture("water-v11-" + distance + ".png");
                }
                camera.Distance = 18;
                camera.Snap();
                string folder = Path.Combine(game.QaDirectory, "water-motion-frames");
                Directory.CreateDirectory(folder);
                var timestamps = new StringBuilder("frame,elapsedSeconds\n");
                double motionStart = Time.realtimeSinceStartupAsDouble;
                for (int frame = 0; frame < 61; frame++)
                {
                    yield return new WaitForSecondsRealtime(.025f);
                    yield return new WaitForEndOfFrame();
                    string name = frame.ToString("D3") + ".png";
                    var texture = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(folder, name), texture.EncodeToPNG());
                    UnityEngine.Object.Destroy(texture);
                    timestamps.Append(name).Append(',').AppendLine(
                        (Time.realtimeSinceStartupAsDouble - motionStart).ToString("F6",
                            System.Globalization.CultureInfo.InvariantCulture));
                }
                File.WriteAllText(Path.Combine(folder, "capture-timestamps.csv"), timestamps.ToString());
                Check(runs.All(x => x.frames > 0), "V11 water ABBA has four real frame sequences");
            }
            finally
            {
                ocean.SetFloat("_Detail", savedDetail);
                camera.Distance = savedDistance;
                camera.Pitch = savedPitch;
                camera.Yaw = savedYaw;
                camera.FocusOffset = savedFocus;
                camera.Overview = savedOverview;
                camera.Snap();
            }
        }
    }
}
