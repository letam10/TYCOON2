using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace Tycoon
{
    // Đo các frame Player đang chạy thật; không dùng timeScale để suy ra FPS.
    public sealed class QaPresentationAudit : MonoBehaviour
    {
        [Serializable] sealed class Sample
        {
            public int width, height, frames;
            public float medianMs, p95Ms, maximumMs;
        }
        [Serializable] sealed class Audit
        {
            public string device, api, note;
            public long managedMiB, allocatedMiB;
            public Sample[] samples;
        }
        readonly Dictionary<Vector2Int,List<float>> frames = new();
        float started;
        void Start() => started = Time.realtimeSinceStartup;
        void Update()
        {
            if (Time.realtimeSinceStartup - started < 3 || !GameSession.Instance.NavigationReady) return;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (!frames.TryGetValue(size, out var values)) frames[size] = values = new List<float>(4096);
            // Giới hạn RAM của bộ đo ngay cả khi dùng preview qua đêm.
            if (values.Count < 32768 && Time.unscaledDeltaTime > 0) values.Add(Time.unscaledDeltaTime * 1000);
        }
        void OnApplicationQuit() => Write();
        public void Write()
        {
            var samples = new List<Sample>();
            foreach (var pair in frames)
            {
                if (pair.Value.Count == 0) continue;
                pair.Value.Sort(); int count = pair.Value.Count;
                samples.Add(new Sample { width = pair.Key.x, height = pair.Key.y, frames = count,
                    medianMs = pair.Value[(count - 1) / 2], p95Ms = pair.Value[Mathf.CeilToInt(count * .95f) - 1], maximumMs = pair.Value[count - 1] });
            }
            var report = new Audit { device = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(),
                managedMiB = GC.GetTotalMemory(false) / 1048576,
                allocatedMiB = Profiler.GetTotalAllocatedMemoryLong() / 1048576, samples = samples.ToArray(),
                note = "Mẫu frame theo độ phân giải, bỏ 3 giây khởi tạo. Frame time có frame limiter, resize và thao tác QA; không phải GPU time, benchmark 165 FPS hoặc soak dài hạn." };
            File.WriteAllText(Path.Combine(GameSession.Instance.QaDirectory, "presentation-audit.json"), JsonUtility.ToJson(report, true));
        }
    }
}
