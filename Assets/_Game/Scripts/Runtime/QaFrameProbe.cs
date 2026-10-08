using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tycoon
{
    public static class QaFrameProbe
    {
        static Dictionary<string, CpuWorkSample> samples;
        static readonly double MillisecondsPerTick = 1000d / Stopwatch.Frequency;
        static bool active;
        static int generation;

        public static void Reset()
        {
            active = false;
            generation++;
            samples?.Clear();
        }

        public static void Start()
        {
            samples ??= new Dictionary<string, CpuWorkSample>(StringComparer.Ordinal);
            active = true;
        }

        public static void Stop()
        {
            active = false;
            generation++;
        }

        public static CpuWorkSample[] Snapshot()
        {
            if (samples == null) return Array.Empty<CpuWorkSample>();
            var snapshot = new CpuWorkSample[samples.Count];
            int index = 0;
            foreach (var sample in samples.Values)
            {
                snapshot[index++] = new CpuWorkSample
                {
                    name = sample.name,
                    count = sample.count,
                    totalMs = sample.totalMs,
                    maximumMs = sample.maximumMs
                };
            }
            Array.Sort(snapshot, CompareSamples);
            return snapshot;
        }

        static int CompareSamples(CpuWorkSample first, CpuWorkSample second)
        {
            int total = second.totalMs.CompareTo(first.totalMs);
            return total != 0 ? total : StringComparer.Ordinal.Compare(first.name, second.name);
        }

        public static Scope Measure(string name)
        {
            if (!active) return default;
            if (!samples.TryGetValue(name, out var sample))
            {
                sample = new CpuWorkSample { name = name };
                samples.Add(name, sample);
            }
            return new Scope(sample, generation);
        }

        // Chạy trên luồng Unity; scope lồng nhau có thời gian trùng, không cộng thành tổng frame.
        public readonly struct Scope : IDisposable
        {
            readonly CpuWorkSample sample;
            readonly long started;
            readonly int epoch;

            internal Scope(CpuWorkSample sample, int epoch)
            {
                this.sample = sample;
                this.epoch = epoch;
                started = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                if (sample == null || !active || epoch != generation) return;
                double elapsed = (Stopwatch.GetTimestamp() - started) * MillisecondsPerTick;
                sample.count++;
                sample.totalMs += elapsed;
                sample.maximumMs = Math.Max(sample.maximumMs, elapsed);
            }
        }
    }
}
