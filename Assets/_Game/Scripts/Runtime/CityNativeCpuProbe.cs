using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace Tycoon
{
    // Chỉ bật bằng --qa-city-profile; không thay đổi cảnh, lượng NPC hoặc tốc độ mô phỏng.
    public sealed class CityNativeCpuProbe : MonoBehaviour
    {
        sealed class Recording
        {
            public ProfilerRecorder Recorder;
            public CpuWorkSample Sample;
        }

        [Serializable]
        sealed class Report
        {
            public string mode = "Native CPU markers during ordinary gameplay";
            public CpuWorkSample[] markers;
            public RenderCounter[] renderCounters;
        }

        [Serializable]
        sealed class RenderCounter
        {
            public string name;
            public long samples;
            public long total;
            public long minimum = long.MaxValue;
            public long maximum;
        }

        sealed class RenderRecording
        {
            public ProfilerRecorder Recorder;
            public RenderCounter Counter;
        }

        readonly List<Recording> recordings = new();
        readonly List<RenderRecording> renderRecordings = new();
        readonly StringBuilder longFrames = new("sampleFrame,second,marker,milliseconds\n");
        bool active;

        public void Begin()
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (var handle in handles)
            {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.UnitType == ProfilerMarkerDataUnit.Count && description.Category.Name == "Render")
                {
                    var counterRecorder = ProfilerRecorder.StartNew(description.Category, description.Name, 1);
                    if (counterRecorder.Valid)
                        renderRecordings.Add(new RenderRecording
                        {
                            Recorder = counterRecorder,
                            Counter = new RenderCounter { name = description.Name }
                        });
                    else counterRecorder.Dispose();
                    continue;
                }
                if (description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;
                string name = description.Name;
                if (!Wanted(name)) continue;
                var recorder = ProfilerRecorder.StartNew(description.Category, name, 1);
                if (!recorder.Valid)
                {
                    recorder.Dispose();
                    continue;
                }
                recordings.Add(new Recording
                {
                    Recorder = recorder,
                    Sample = new CpuWorkSample { name = name }
                });
            }
            active = true;
        }

        static bool Wanted(string name) => name is "Main Thread" or "Render Thread" or "PlayerLoop"
            or "BehaviourUpdate" or "BehaviourLateUpdate" or "Camera.Render" or "Canvas.BuildBatch"
            or "GC.Collect" or "WaitForTargetFPS" or "Gfx.WaitForPresentOnGfxThread"
            || name.Contains("ScriptRunBehaviour") || name.Contains("Animator")
            || name.Contains("DirectorUpdateAnimation") || name.Contains("AIUpdate")
            || name.Contains("NavMesh") || name.Contains("UpdateAllRenderers")
            || name.Contains("UpdateAllSkinnedMeshes") || name.Contains("UpdateRectTransform")
            || name.Contains("UpdateCanvas") || name.Contains("RenderLoop")
            || name.Contains("Shader") || name.Contains("GPUResident") || name.Contains("Culling");

        void LateUpdate()
        {
            if (!active) return;
            foreach (var recording in renderRecordings)
            {
                long value = recording.Recorder.LastValue;
                var counter = recording.Counter;
                counter.samples++;
                counter.total += value;
                counter.minimum = Math.Min(counter.minimum, value);
                counter.maximum = Math.Max(counter.maximum, value);
            }
            foreach (var recording in recordings)
            {
                double milliseconds = recording.Recorder.LastValue / 1000000d;
                recording.Sample.count++;
                recording.Sample.totalMs += milliseconds;
                recording.Sample.maximumMs = Math.Max(recording.Sample.maximumMs, milliseconds);
                if (milliseconds > 30)
                    longFrames.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                        "{0},{1:F3},{2},{3:F3}\n", Time.frameCount,
                        Time.realtimeSinceStartupAsDouble, recording.Sample.name, milliseconds);
            }
        }

        public void Finish(string directory)
        {
            if (!active) return;
            active = false;
            foreach (var recording in recordings) recording.Recorder.Dispose();
            foreach (var recording in renderRecordings) recording.Recorder.Dispose();
            File.WriteAllText(Path.Combine(directory, "ordinary-play-native-cpu.json"),
                JsonUtility.ToJson(new Report
                {
                    markers = recordings.Select(x => x.Sample).ToArray(),
                    renderCounters = renderRecordings.Select(x => x.Counter).ToArray()
                }, true));
            File.WriteAllText(Path.Combine(directory, "ordinary-play-native-long-frames.csv"), longFrames.ToString());
        }

        void OnDestroy()
        {
            if (!active) return;
            foreach (var recording in recordings) recording.Recorder.Dispose();
            foreach (var recording in renderRecordings) recording.Recorder.Dispose();
        }
    }
}
