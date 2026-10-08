using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tycoon
{
    [Serializable]
    public sealed class CpuWorkSample
    {
        public string name;
        public long count;
        public double totalMs;
        public double maximumMs;
    }

    public static class QaCpuProbe
    {
        internal enum Work
        {
            RuntimeExecute,
            CoreExecute,
            DraftCopy,
            Apply,
            StoreCommit,
            Refresh,
            ViewSnapshot,
            InventoryProjection
        }

        static readonly object Gate = new();
        static readonly List<CpuWorkSample> Samples = new();
        static readonly Dictionary<TransactionKind, CpuWorkSample> Commands = new();
        static readonly CpuWorkSample[] WorkSamples;
        static readonly CpuWorkSample UnknownCommand;
        static readonly double MillisecondsPerTick = 1000d / Stopwatch.Frequency;
        static volatile bool active;
        static int generation;

        static QaCpuProbe()
        {
            WorkSamples = new[]
            {
                Add("RuntimeTransactions.Execute"),
                Add("TransactionCore.Execute"),
                Add("TransactionCore.DraftCopy"),
                Add("TransactionCore.Apply"),
                Add("TransactionCore.StoreCommit"),
                Add("RuntimeTransactions.Refresh"),
                Add("RuntimeTransactions.ViewSnapshot"),
                Add("RuntimeTransactions.InventoryProjection")
            };
            foreach (TransactionKind kind in Enum.GetValues(typeof(TransactionKind)))
                Commands.Add(kind, Add("TransactionCore.Execute." + kind));
            UnknownCommand = Add("TransactionCore.Execute.UnknownKind");
        }

        static CpuWorkSample Add(string name)
        {
            var sample = new CpuWorkSample { name = name };
            Samples.Add(sample);
            return sample;
        }

        public static void Reset()
        {
            lock (Gate)
            {
                active = false;
                generation++;
                foreach (var sample in Samples)
                {
                    sample.count = 0;
                    sample.totalMs = 0;
                    sample.maximumMs = 0;
                }
            }
        }

        public static void Start()
        {
            lock (Gate)
                active = true;
        }

        public static void Stop()
        {
            lock (Gate)
            {
                active = false;
                generation++;
            }
        }

        public static CpuWorkSample[] Snapshot()
        {
            lock (Gate)
            {
                var snapshot = new CpuWorkSample[Samples.Count];
                for (int i = 0; i < Samples.Count; i++)
                {
                    var sample = Samples[i];
                    snapshot[i] = new CpuWorkSample
                    {
                        name = sample.name,
                        count = sample.count,
                        totalMs = sample.totalMs,
                        maximumMs = sample.maximumMs
                    };
                }
                return snapshot;
            }
        }

        internal static Scope Measure(Work work)
        {
            if (!active)
                return default;
            return new Scope(WorkSamples[(int)work], generation);
        }

        internal static Scope Measure(TransactionKind kind)
        {
            if (!active)
                return default;
            return new Scope(Commands.TryGetValue(kind, out var sample) ? sample : UnknownCommand, generation);
        }

        // Các scope lồng nhau và tính cả lệnh thất bại; không cộng chúng thành tổng độc lập.
        internal readonly struct Scope : IDisposable
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
                if (sample == null)
                    return;
                double elapsed = (Stopwatch.GetTimestamp() - started) * MillisecondsPerTick;
                lock (Gate)
                {
                    if (!active || epoch != generation)
                        return;
                    sample.count++;
                    sample.totalMs += elapsed;
                    sample.maximumMs = Math.Max(sample.maximumMs, elapsed);
                }
            }
        }
    }
}
