using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tycoon
{
    public static partial class GameLocalization
    {
        static readonly Dictionary<string, string> English = BuildEnglish();
        static readonly Regex Phrases = new(@"(?<![\p{L}\p{N}_])(?:" +
            string.Join("|", English.Keys.OrderByDescending(key => key.Length).Select(Regex.Escape)) +
            @")(?![\p{L}\p{N}_])", RegexOptions.CultureInvariant);

        public static string Translate(string vietnamese)
        {
            if (string.IsNullOrEmpty(vietnamese) || GameSettingsStore.Current.language == GameLanguage.Vietnamese)
                return vietnamese ?? "";
            if (English.TryGetValue(vietnamese, out string translation)) return translation;
            // Chỉ dịch chuỗi hiển thị; biên từ giữ nguyên SKU, ID và số tiền trong chuỗi ghép.
            return Phrases.Replace(vietnamese, match => English[match.Value]);
        }

        static Dictionary<string, string> BuildEnglish()
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            AddMenus(entries);
            AddFeatureMenus(entries);
            AddGameplay(entries);
            AddVehicles(entries);
            return entries;
        }
    }
}
