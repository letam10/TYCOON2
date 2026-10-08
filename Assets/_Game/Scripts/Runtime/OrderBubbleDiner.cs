using UnityEngine;

namespace Tycoon
{
    public sealed partial class DinerAgent
    {
        OrderBubbleView orderBubble;

        void LateUpdate()
        {
            if (Phase is not (1 or 2) || !Table)
            {
                OrderBubbleLayer.Release(ref orderBubble);
                return;
            }
            orderBubble ??= OrderBubbleLayer.Acquire(transform);
            int delivered = Runtime != null ? Runtime.lines[0].delivered : Basket.Count(WantedItem);
            orderBubble.SetSingle(WantedItem, delivered, 1,
                Phase == 2 ? 1 : PatienceLeft / OrderState.Patience);
        }

        void OnDisable()
        {
            OrderBubbleLayer.Release(ref orderBubble);
        }
    }
}
