using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Tycoon
{
    public readonly struct CityOperationTiming : IDisposable
    {
        static readonly StringBuilder rows = new("frame,second,operation,milliseconds\n");
        readonly GameSession game;
        readonly string operation;
        readonly long started;

        CityOperationTiming(GameSession game, string operation)
        {
            this.game = game && game.IsQa ? game : null;
            this.operation = operation;
            started = Stopwatch.GetTimestamp();
        }

        public static CityOperationTiming Measure(GameSession game, string operation) => new(game, operation);

        public void Dispose()
        {
            if (!game) return;
            double milliseconds = (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
            rows.AppendFormat(CultureInfo.InvariantCulture, "{0},{1:F3},{2},{3:F3}\n",
                UnityEngine.Time.frameCount, UnityEngine.Time.realtimeSinceStartupAsDouble,
                operation, milliseconds);
        }

        public static void Save(string directory)
        {
            File.WriteAllText(Path.Combine(directory, "operation-timings.csv"), rows.ToString());
        }
    }
}
