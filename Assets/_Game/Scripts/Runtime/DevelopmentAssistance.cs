using System;
using System.Linq;

namespace Tycoon
{
    // Thêm TYCOON_DISABLE_ASSIST vào Scripting Define Symbols để gỡ nút và chặn lệnh.
    public static class DevelopmentAssistance
    {
#if TYCOON_DISABLE_ASSIST
        public static bool Enabled => false;
#else
        public static bool Enabled => true;
#endif
        public const int Grant = 999999;
        public static bool Apply(GameSession game)
        {
            if (!Enabled || game?.Transactions == null) return false;
            return game.Transactions.TryExecute(game.Transactions.Command(TransactionKind.GrantAssistance,"player"),out _);
        }
        public static bool ApplyCashOnly(GameSession game)
        {
            if (!Enabled || game?.Transactions == null) return false;
            return game.Transactions.TryExecute(game.Transactions.Command(TransactionKind.GrantModCash,"player"),out _);
        }
    }
    public sealed partial class TransactionCore
    {
        int GrantModCash(TransactionState s, TransactionCommand c)
        {
            Player(s,c.actor); Require(DevelopmentAssistance.Enabled || !runtime,"assist","Nút mod đã được gỡ khỏi bản này.");
            Require(s.money<=int.MaxValue-DevelopmentAssistance.Grant,"money-limit","Ví đã chạm giới hạn tiền.");
            s.money+=DevelopmentAssistance.Grant;
            return DevelopmentAssistance.Grant;
        }
        int GrantAssistance(TransactionState s, TransactionCommand c)
        {
            Player(s,c.actor); Require(DevelopmentAssistance.Enabled || !runtime,"assist","Nút hỗ trợ đã được gỡ khỏi bản này.");
            s.money=checked(s.money+DevelopmentAssistance.Grant);
            s.assistedCash=checked(s.assistedCash+DevelopmentAssistance.Grant);s.assisted=true;
            foreach(var d in Definitions.Upgrades.Where(x=>x.kind=="unlock"))
                if(!s.unlocked.Contains(d.id))s.unlocked.Add(d.id);
            UnlockCrops(s);return DevelopmentAssistance.Grant;
        }
        internal static void UnlockCrops(TransactionState s, bool migrate=false)
        {
            int tier=ProgressionTracker.StationLevel(s,"farm");
            if((s.assisted||tier>=2)&&!s.unlocked.Contains("feed_route"))s.unlocked.Add("feed_route");
            foreach(string id in new[]{"wheat","corn","soybean","tomato"})
            {
                bool legacy=migrate && s.contentVersion==0 && s.unlocked.Contains("farm_shop") && id is "wheat" or "tomato";
                if(s.assisted || tier >= (id=="tomato"?3:2) || legacy)
                    if(!s.unlocked.Contains("crop_"+id))s.unlocked.Add("crop_"+id);
            }
            if((s.assisted || tier>=3)&&!s.unlocked.Contains("crop_expansion3"))s.unlocked.Add("crop_expansion3");
        }
    }
}
