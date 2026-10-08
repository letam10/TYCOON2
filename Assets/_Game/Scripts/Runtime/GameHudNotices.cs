using UnityEngine;
namespace Tycoon
{
    public sealed partial class GameHud
    {
        readonly HudNoticeBuffer notices = new();
        string observedNotice;
        float observedUntil;
        void RefreshNotices()
        {
            float now = Time.unscaledTime;
            notices.Tick(now);
            if (observedNotice != game.Toast || observedUntil != game.ToastUntil)
            {
                observedNotice = game.Toast;
                observedUntil = game.ToastUntil;
                if (Time.time < observedUntil) notices.Push(observedNotice, now);
            }
            SetText(toast, notices.Current);
            toastRoot.SetActive(notices.Current.Length > 0);
        }
    }
}
