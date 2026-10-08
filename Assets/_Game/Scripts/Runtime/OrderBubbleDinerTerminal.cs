namespace Tycoon
{
    public sealed partial class DinerAgent
    {
        OrderBubbleTerminalFeedback terminalFeedback;
        bool HasTerminalFeedback => terminalFeedback && terminalFeedback.IsPlaying;

        void ShowTerminalFeedback()
        {
            bool completed = Runtime?.status == OrderStatus.Complete ||
                GameSession.Instance.Economy.IsPaid(Receipt);
            if (!terminalFeedback) terminalFeedback = gameObject.AddComponent<OrderBubbleTerminalFeedback>();
            terminalFeedback.Show(ref orderBubble, completed);
        }

        void ResetTerminalFeedback()
        {
            if (terminalFeedback) terminalFeedback.ResetFeedback();
        }
    }
}
