using System;
using System.IO;
using UnityEngine;

namespace Tycoon
{
    public static class GameSaveSlots
    {
        public const int Count = 3;
        public const string RedesignProfileId = "redesign-v1";
        [Serializable] sealed class Selection { public int slot = 1; }

        public static string ProfileDirectory(string baseDirectory, string profileId)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                throw new ArgumentException("Save profile base directory is required.", nameof(baseDirectory));
            if (string.IsNullOrWhiteSpace(profileId)
                || profileId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || profileId is "." or "..")
                throw new ArgumentException("Save profile id is invalid.", nameof(profileId));
            return Path.Combine(baseDirectory, profileId);
        }

        public static string RedesignDirectory(string baseDirectory)
        {
            return ProfileDirectory(baseDirectory, RedesignProfileId);
        }

        public static string PathFor(string directory, int slot, bool qa)
        {
            if (slot < 1 || slot > Count) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory, slot == 1 ? qa ? "qa-save-v2.json" : "save-v2.json"
                : "save-slot-" + slot + ".json");
        }

        public static int ReadActive(string directory)
        {
            string path = Path.Combine(directory, "active-slot.json");
            if (!File.Exists(path)) return 1;
            try { return Mathf.Clamp(JsonUtility.FromJson<Selection>(File.ReadAllText(path)).slot, 1, Count); }
            catch (Exception error) when (error is IOException or ArgumentException)
            { Debug.LogWarning("Không đọc được slot đang dùng: " + error.Message); return 1; }
        }

        public static void Remember(string directory, int slot)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "active-slot.json"), JsonUtility.ToJson(new Selection { slot = slot }));
        }
    }
}
