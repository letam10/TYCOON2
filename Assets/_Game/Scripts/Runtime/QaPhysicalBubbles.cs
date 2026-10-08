using System.Collections;
using UnityEngine;

namespace Tycoon
{
    public sealed partial class QaDriver
    {
        IEnumerator PhysicalBubbleScreens()
        {
            var actor = new GameObject("QA_Bubble_Customer");
            actor.transform.position = game.Player.transform.position + Vector3.right * 2;
            Art.Model("customer", Vector3.zero, actor.transform);
            var view = OrderBubbleLayer.Acquire(actor.transform);
            var first = view.Rect;
            var lines = new[]
            {
                new OrderLine("carrot", 3, 10) { delivered = 2 },
                new OrderLine("wheat", 2, 10) { delivered = 1 }
            };
            foreach (float patience in new[] { 1f, .45f, .15f })
            {
                view.SetLines(lines, patience);
                yield return null;
                Check(view.LineCount == 2 && Mathf.Approximately(view.PatienceFraction, patience),
                    "physical bubble: two partial lines and radial patience " + patience);
                yield return Capture("bubble-two-items-" + Mathf.RoundToInt(patience * 100) + ".png");
            }
            OrderBubbleLayer.Release(ref view);
            Check(view == null, "physical bubble: release clears pooled reference");
            view = OrderBubbleLayer.Acquire(actor.transform);
            view.SetSingle("bread", 0, 4, 1);
            yield return null;
            Check(view.Rect == first && view.LineCount == 1 && view.PatienceFraction == 1,
                "physical bubble: pooled view resets second item and patience");
            yield return Capture("bubble-reused-single-item.png");
            OrderBubbleLayer.Release(ref view);
            var terminal = actor.AddComponent<OrderBubbleTerminalFeedback>();
            foreach (bool completed in new[] { true, false })
            {
                view = OrderBubbleLayer.Acquire(actor.transform);
                view.SetSingle("bread", completed ? 4 : 1, 4, completed ? 1 : 0);
                Check(terminal.Show(ref view, completed) && terminal.IsPlaying,
                    "physical bubble: terminal feedback starts " + completed);
                yield return null;
                yield return Capture(completed ? "bubble-completed.png" : "bubble-disappointed.png");
                terminal.Advance(OrderBubbleTerminalFeedback.Duration);
                Check(!terminal.IsPlaying, "physical bubble: terminal feedback ends and releases pool");
            }
            view = OrderBubbleLayer.Acquire(actor.transform);
            terminal.Show(ref view, true);
            actor.SetActive(false);
            Check(!terminal.IsPlaying && terminal.Outcome == OrderBubbleTerminalOutcome.None,
                "physical bubble: actual Player disable resets terminal pool state");
            Destroy(actor);
        }
    }
}
