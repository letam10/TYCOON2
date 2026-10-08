using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tycoon
{
    public sealed class HudNoticeBuffer
    {
        public const int MaximumCharacters = 94;
        public const int MaximumPending = 3;
        const float Duration = 2.8f;
        readonly Queue<string> pending = new();
        readonly Dictionary<string, float> recent = new();
        readonly List<string> expired = new();
        public string Current { get; private set; } = "";
        public int PendingCount => pending.Count;
        float until;

        public static string Compact(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "";
            string value = Regex.Replace(message, @"\s+", " ").Trim();
            if (value.Length > MaximumCharacters)
                value = value.Substring(0, MaximumCharacters - 1).TrimEnd() + "…";
            if (value.Length <= 47) return value;
            int split = value.LastIndexOf(' ', 47);
            if (split < 30) split = 47;
            return value.Substring(0, split).TrimEnd() + "\n" + value.Substring(split).TrimStart();
        }

        public bool Push(string message, float now)
        {
            string text = Compact(message);
            if (text.Length == 0 || text == Current || pending.Contains(text)) return false;
            if (recent.TryGetValue(text, out float allowedAt) && now < allowedAt) return false;
            expired.Clear();
            foreach (var entry in recent)
                if (entry.Value <= now) expired.Add(entry.Key);
            foreach (string key in expired) recent.Remove(key);
            if (recent.Count >= 32)
            {
                string oldest = null;
                float oldestTime = float.PositiveInfinity;
                foreach (var entry in recent)
                {
                    if (entry.Value >= oldestTime) continue;
                    oldest = entry.Key;
                    oldestTime = entry.Value;
                }
                if (oldest != null) recent.Remove(oldest);
            }
            recent[text] = now + 6;
            if (Current.Length == 0) Show(text, now);
            else
            {
                if (pending.Count == MaximumPending) pending.Dequeue();
                pending.Enqueue(text);
            }
            return true;
        }

        public void Tick(float now)
        {
            if (Current.Length == 0 || now < until) return;
            if (pending.Count > 0) Show(pending.Dequeue(), now);
            else Current = "";
        }

        void Show(string text, float now)
        {
            Current = text;
            until = now + Duration;
        }
    }
}
