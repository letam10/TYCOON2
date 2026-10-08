using UnityEngine;
using UnityEngine.AI;

namespace Tycoon
{
    public sealed class OrderBubbleTerminalFeedback : MonoBehaviour
    {
        public const float Duration = .6f;
        public bool IsPlaying => remaining > 0;
        public OrderBubbleTerminalOutcome Outcome => bubble?.TerminalOutcome ?? OrderBubbleTerminalOutcome.None;
        OrderBubbleView bubble;
        NavMeshAgent pausedAgent;
        float remaining;

        public bool Show(ref OrderBubbleView source, bool completed)
        {
            if (IsPlaying) return false;
            bubble = source ?? OrderBubbleLayer.Acquire(transform);
            source = null;
            bubble.ShowTerminal(completed);
            remaining = Duration;
            pausedAgent = GetComponent<NavMeshAgent>();
            if (pausedAgent && pausedAgent.enabled && pausedAgent.isOnNavMesh) pausedAgent.isStopped = true;
            var customer = GetComponent<CustomerAgent>();
            var diner = GetComponent<DinerAgent>();
            bool carrying = customer ? customer.Basket.Total > 0 : diner && diner.Basket.Total > 0;
            GetComponentInChildren<ActorView>()?.SetMotion(0, carrying);
            return true;
        }

        void Update()
        {
            var game = GameSession.Instance;
            if (game && (!game.CanSimulate || game.Hud && !game.Hud.AllowsPlayerControl)) return;
            Advance(Time.deltaTime);
        }

        public void Advance(float delta)
        {
            if (!IsPlaying || delta <= 0 || !float.IsFinite(delta)) return;
            remaining = Mathf.Max(0, remaining - delta);
            if (remaining == 0) ResetFeedback();
        }

        public void ResetFeedback()
        {
            remaining = 0;
            OrderBubbleLayer.Release(ref bubble);
            if (pausedAgent && pausedAgent.enabled && pausedAgent.isOnNavMesh) pausedAgent.isStopped = false;
            pausedAgent = null;
        }

        void OnDisable()
        {
            ResetFeedback();
        }
    }
}
